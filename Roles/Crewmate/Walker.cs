using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using Hazel;

namespace TownOfHost.Roles.Crewmate;

public sealed class Walker : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Walker),
            player => new Walker(player),
            CustomRoles.Walker,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            12100,
            SetupOptionItem,
            "wa",
            "#057a2c",
            (8, 2)
        );
    public Walker(PlayerControl player)
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
    }
    float timer;
    public int completeroom;
    SystemTypes? TaskRoom;
    PlainShipRoom TaskPSR;
    Vector2 RoomArrow;
    enum OptionName
    {
        WalkerWalkTaskCount
    }
    public static OptionItem WalkTaskCount;
    static void SetupOptionItem()
    {
        WalkTaskCount = IntegerOptionItem.Create(RoleInfo, 10, OptionName.WalkerWalkTaskCount, (1, 99, 1), 5, false);
        OverrideTasksData.Create(RoleInfo, 15, tasks: (true, 1, 0, 0));
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost || completeroom == WalkTaskCount.GetInt()) return;

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
                    Logger.Info($"{TaskRoom}に{player.name}が来たよ", "Walker");
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
                    Logger.Info($"{TaskRoom}にはもう既にいたから変更するよ", "Walker");
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

        Logger.Info($"NextTask : {TaskRoom}", "Walker");
        _ = new LateTask(() => UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player), 0.3f, "WalkerChengeRoom", null);
    }
    void ReceiveRoom(MessageReader reader)
    {
        var roomId = (SystemTypes)reader.ReadByte();
        TaskPSR = ShipStatus.Instance.AllRooms.FirstOrDefault(x => x.RoomId == roomId);

        if (TaskPSR == null)
        {
            Logger.Error($"{roomId}の部屋を見つけることができませんでした ShipStatus:{ShipStatus.Instance.name}", "Walker ReceiveRoom");
            return;
        }

        if (RoomArrow != Vector2.zero) GetArrow.Remove(Player.PlayerId, RoomArrow);

        MyTaskState.Update(Player);
        RoomArrow = TaskPSR.transform.position;
        GetArrow.Add(Player.PlayerId, RoomArrow);
        TaskRoom = TaskPSR.RoomId;

        Logger.Info($"NextTask : {TaskRoom}", "Walker");
    }

    void ReceiveCompleteRoom(MessageReader reader)
    {
        TaskRoom = null;
        TaskPSR = null;
        completeroom = reader.ReadInt32();
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
        if (isForMeeting || seer != seen || completeroom == WalkTaskCount.GetInt() || TaskRoom == null) return "";
        return $"<color=#057a2c>{GetArrow.GetArrows(seer, [RoomArrow])} {string.Format(GetString("FoxRoomMission"), $"<color=#cccccc><b>{GetString($"{TaskRoom}")}<b></color>")}</color>";
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