
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Hazel;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using UnityEngine;

namespace TownOfHost;

public class RoomTaskAssign
{
    public static Dictionary<byte, RoomTaskAssign> AllRoomTasker = new();
    public byte playerid; public PlayerState Mystate;
    public float timer;
    public int completeroom;
    public SystemTypes? TaskRoom;
    public PlainShipRoom TaskPSR;
    public Vector2 RoomArrow;

    public RoomTaskAssign(byte playerid)
    {
        this.playerid = playerid;
        timer = 0;
        completeroom = 0;
        TaskRoom = null;
        TaskPSR = null;
        RoomArrow = new Vector2(50, 50);
        Mystate = playerid.GetPlayerState();
    }

    public void fixupdate()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if ((playerid.GetPlayerControl()?.GetRoleClass() as IRoomTasker)?.IsAssignRoomTask() is not true) return;

        //TaskRoomがnullの場合、再設定する
        if (!TaskRoom.HasValue)
        {
            ChengeRoom();
        }
        else //ある場合
        {
            if (Mystate.HasSpawned) timer += Time.fixedDeltaTime;
            if (timer <= 0.1f) return; //変わってすぐは処理しない...

            var player = playerid.GetPlayerControl();
            var nowroom = player.GetPlainShipRoom(true);
            if (nowroom == null) return;

            if (TaskRoom == nowroom.RoomId)
            {
                if (timer > 0.5f)
                {
                    Logger.Info($"{TaskRoom}に{player.name}が来たよ", "RoomTaskAssign");
                    TaskRoom = null;
                    TaskPSR = null;
                    completeroom++;
                    timer = 0;
                    RPC.PlaySoundRPC(playerid, Sounds.TaskComplete);

                    (player?.GetRoleClass() as IRoomTasker)?.OnComplete(completeroom);
                }
                else
                    Logger.Info($"{TaskRoom}にはもう既にいたから変更するよ", "RoomTaskAssign");
                ChengeRoom();
            }
        }
    }
    void ChengeRoom()
    {
        if ((playerid.GetPlayerControl()?.GetRoleClass() as IRoomTasker)?.IsAssignRoomTask() is not true) return;
        List<PlainShipRoom> rooms = new();
        ShipStatus.Instance.AllRooms.Where(room => room?.RoomId is not null and not SystemTypes.Hallway && room?.RoomId != TaskRoom).Do(r => rooms.Add(r));

        var rand = IRandom.Instance;
        TaskPSR = rooms[rand.Next(0, rooms.Count)];
        if (RoomArrow != Vector2.zero) GetArrow.Remove(playerid, RoomArrow);

        RoomArrow = TaskPSR.transform.position;
        GetArrow.Add(playerid, RoomArrow);
        TaskRoom = TaskPSR.RoomId;

        (playerid.GetPlayerControl()?.GetRoleClass() as IRoomTasker)?.ChangeRoom(TaskPSR);

        Logger.Info($"NextTask : {TaskRoom}", "Walker");
        _ = new LateTask(() => UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: playerid.GetPlayerControl()), 0.3f, "RoomTaskAssign_ChangeRoom", null);
    }
    public void ReceiveRoom(MessageReader reader)
    {
        var roomId = (SystemTypes)reader.ReadByte();
        TaskPSR = ShipStatus.Instance.AllRooms.FirstOrDefault(x => x.RoomId == roomId);

        if (TaskPSR == null)
        {
            Logger.Error($"{roomId}の部屋を見つけることができませんでした ShipStatus:{ShipStatus.Instance.name}", "RoomTaskAssign_ReceiveRoom");
            return;
        }

        if (RoomArrow != Vector2.zero) GetArrow.Remove(playerid, RoomArrow);

        RoomArrow = TaskPSR.transform.position;
        GetArrow.Add(playerid, RoomArrow);
        TaskRoom = TaskPSR.RoomId;

        Logger.Info($"NextTask : {TaskRoom}", "RoomTaskAssign_ReceiveRoom");
    }

    public void ReceiveCompleteRoom(MessageReader reader)
    {
        TaskRoom = null;
        TaskPSR = null;
        completeroom = reader.ReadInt32();
    }

    public string GetLowerText(PlayerControl seer, string colorcode = "#ffffff")
    {
        if (TaskRoom == null || (playerid.GetPlayerControl()?.GetRoleClass() as IRoomTasker)?.IsAssignRoomTask() is not true) return "";
        return $"<{colorcode}>{GetArrow.GetArrows(seer, [RoomArrow])}" +
            $"{string.Format(Translator.GetString("FoxRoomMission"), $"<color=#cccccc><b>{Translator.GetString($"{TaskRoom}")}<b></color>")}</color>";
    }

    public void OnStartMeeting()
    {
        timer = 0;
        TaskRoom = null;
        TaskPSR = null;
        GetArrow.Remove(playerid, RoomArrow);
        RoomArrow = Vector2.zero;
    }
}