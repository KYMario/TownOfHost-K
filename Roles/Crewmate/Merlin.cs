using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Impostor;
using UnityEngine;

namespace TownOfHost.Roles.Crewmate;

public sealed class Merlin : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Merlin),
            player => new Merlin(player),
            CustomRoles.Merlin,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            15900,
            null,
            "mer",
            "#8cc2ff",
            (2, 1),
            combination: CombinationRoles.AssassinandMerlin
        );
    public Merlin(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        completeroom = 0;
        timer = 0;
        TaskRoom = null;
        TaskPSR = null;
        RoomArrow = Vector2.zero;
        worktask = Assassin.OptionMerlinWorkTask.GetInt();
    }
    public static int worktask;
    float timer;
    public int completeroom;
    SystemTypes? TaskRoom;
    PlainShipRoom TaskPSR;
    Vector2 RoomArrow;

    public override void Add()
    {
        foreach (var impostor in PlayerCatch.AllPlayerControls.Where(player => player.Is(CustomRoleTypes.Impostor) || player.GetCustomRole() is CustomRoles.Egoist))
        {
            NameColorManager.Add(Player.PlayerId, impostor.PlayerId, "#ff1919");
        }
        Assassin.MarlinIds.Add(Player.PlayerId);
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost || completeroom == worktask) return;

        //TaskRoomがnullの場合、再設定する
        if (!TaskRoom.HasValue)
        {
            ChengeRoom();
        }
        else //ある場合
        {
            if (MyState.HasSpawned) timer += Time.fixedDeltaTime;
            if (timer <= 0.1f) return; //変わってすぐは処理しない...

            var nowroom = player.GetPlainShipRoom(true);
            if (nowroom == null) return;

            if (TaskRoom == nowroom.RoomId)
            {
                if (timer > 0.5f)
                {
                    Logger.Info($"{TaskRoom}に{player.name}が来たよ", "Merlin");
                    TaskRoom = null;
                    TaskPSR = null;
                    completeroom++;
                    timer = 0;
                    MyTaskState.Update(player);
                    RPC.PlaySoundRPC(Player.PlayerId, Sounds.TaskComplete);
                    SendRPC_CompleteRoom();
                    CheckFin();
                }
                else
                    Logger.Info($"{TaskRoom}にはもう既にいたから変更するよ", "Merlin");
                ChengeRoom();
            }
        }
    }
    void CheckFin()
    {
        if (MyTaskState.CompletedTasksCount < MyTaskState.AllTasksCount) return;
        UtilsGameLog.AddGameLog("Task", string.Format(Translator.GetString("Taskfin"), UtilsName.GetPlayerColor(Player, true)));
    }
    void ChengeRoom()
    {
        List<PlainShipRoom> rooms = new();
        ShipStatus.Instance.AllRooms.Where(room => room?.RoomId is not null and not SystemTypes.Hallway && room?.RoomId != TaskRoom).Do(r => rooms.Add(r));

        var rand = IRandom.Instance;
        TaskPSR = rooms[rand.Next(0, rooms.Count)];
        if (RoomArrow != Vector2.zero) GetArrow.Remove(Player.PlayerId, RoomArrow);

        RoomArrow = TaskPSR.transform.position;
        GetArrow.Add(Player.PlayerId, RoomArrow);
        TaskRoom = TaskPSR.RoomId;

        SendRPC_ChengeRoom();

        Logger.Info($"NextTask : {TaskRoom}", "Merlin");
        _ = new LateTask(() => UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player), 0.3f, "MerlinChengeRoom", null);
    }
    void ReceiveRoom(MessageReader reader)
    {
        var roomId = (SystemTypes)reader.ReadByte();
        TaskPSR = ShipStatus.Instance.AllRooms.FirstOrDefault(x => x.RoomId == roomId);

        if (TaskPSR == null)
        {
            Logger.Error($"{roomId}の部屋を見つけることができませんでした ShipStatus:{ShipStatus.Instance.name}", "Merlin ReceiveRoom");
            return;
        }

        if (RoomArrow != Vector2.zero) GetArrow.Remove(Player.PlayerId, RoomArrow);

        RoomArrow = TaskPSR.transform.position;
        GetArrow.Add(Player.PlayerId, RoomArrow);
        TaskRoom = TaskPSR.RoomId;

        Logger.Info($"NextTask : {TaskRoom}", "Merlin");
    }

    void ReceiveCompleteRoom(MessageReader reader)
    {
        TaskRoom = null;
        TaskPSR = null;
        completeroom = reader.ReadInt32();
        MyTaskState.Update(Player);
    }

    public override void OnStartMeeting()
    {
        timer = 0;
        TaskRoom = null;
        TaskPSR = null;
        GetArrow.Remove(Player.PlayerId, RoomArrow);
        RoomArrow = Vector2.zero;
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (isForMeeting || seer != seen || completeroom == worktask || TaskRoom == null) return "";
        return $"<color=#8cc2ff>{GetArrow.GetArrows(seer, [RoomArrow])} {string.Format(GetString("FoxRoomMission"), $"<color=#cccccc><b>{GetString($"{TaskRoom}")}<b></color>")}</color>";
    }

    public void SendRPC_CompleteRoom()
    {
        using var sender = CreateSender();
        sender.Writer.WritePacked((int)RPC_Types.CompleteRoom);
        sender.Writer.Write(completeroom);
    }

    public void SendRPC_ChengeRoom()
    {
        using var sender = CreateSender();
        sender.Writer.WritePacked((int)RPC_Types.ChengeRoom);
        sender.Writer.Write((byte)TaskPSR.RoomId);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        switch ((RPC_Types)reader.ReadPackedInt32())
        {
            case RPC_Types.ChengeRoom:
                ReceiveRoom(reader);
                break;
            case RPC_Types.CompleteRoom:
                ReceiveCompleteRoom(reader);
                break;
        }
    }

    enum RPC_Types
    {
        ChengeRoom,
        CompleteRoom
    }
}