using System;
using System.Linq;
using UnityEngine;
using AmongUs.GameOptions;
using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

using TownOfHost.Roles.Core;
using static TownOfHost.Modules.SelfVoteManager;
using Hazel;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Crewmate;

public sealed class Satellite : RoleBase, ISelfVoter
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Satellite),
            player => new Satellite(player),
            CustomRoles.Satellite,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            11300,
            SetupOptionItem,
            "Sat",
            "#00e1ff",
            (6, 3),
            introSound: () => DestroyableSingleton<AutoOpenDoor>.Instance.OpenSound
        );
    public Satellite(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
    }
    private static OptionItem OptionMaximum; static int maximum;
    private static OptionItem OptiontaskCount; static int skillusetaskcount;
    private static OptionItem Option1MeetingMaximum; static int meetingmaximum;
    private static OptionItem OptionAwakening;
    bool IsAwaken;
    int UsedSkillCount;
    int MeetingUsedSkillCount;
    private static Dictionary<byte, LocationData> AllPlayerLocationData;
    private static Dictionary<byte, SystemTypes?> SentPlayers;
    enum OptionName
    {
        SatelliteCount
    }
    public override void Add()
    {
        maximum = OptionMaximum.GetInt();
        skillusetaskcount = OptiontaskCount.GetInt();
        meetingmaximum = Option1MeetingMaximum.GetInt();
        IsAwaken = OptionAwakening.GetBool() && OptiontaskCount.GetInt() > 0;

        UsedSkillCount = 0;
        MeetingUsedSkillCount = 0;

        SentPlayers = new();
        AllPlayerLocationData = new(GameData.Instance.PlayerCount);

        foreach (var pc in PlayerCatch.AllPlayerControls)
        {
            AllPlayerLocationData.TryAdd(pc.PlayerId, new LocationData(pc.PlayerId));
        }
    }

    static void SetupOptionItem()
    {
        OptionMaximum = IntegerOptionItem.Create(RoleInfo, 10, OptionName.SatelliteCount, new(1, 99, 1), 2, false)
            .SetValueFormat(OptionFormat.Times);
        Option1MeetingMaximum = IntegerOptionItem.Create(RoleInfo, 11, GeneralOption.MeetingMaxTime, new(0, 99, 1), 1, false)
            .SetValueFormat(OptionFormat.Times)
            .SetZeroNotation(OptionZeroNotation.Infinity);
        OptiontaskCount = IntegerOptionItem.Create(RoleInfo, 12, GeneralOption.cantaskcount, new(0, 99, 1), 5, false);
        OptionAwakening = BooleanOptionItem.Create(RoleInfo, 13, GeneralOption.AbilityAwakening, false, false);
    }

    class LocationData
    {
        public byte PlayerId;
        public HashSet<SystemTypes> visitedLocations;
        public LocationData(byte playerId)
        {
            PlayerId = playerId;
            visitedLocations = new();
        }
    }

    private bool CanUseAbility => GameStates.introDestroyed && UsedSkillCount <= maximum && MyTaskState.HasCompletedEnoughCountOfTasks(skillusetaskcount);

    public override void OnFixedUpdate(PlayerControl player)
    {
        if (Utils.IsActive(SystemTypes.Comms) || !AmongUsClient.Instance.AmHost || player.IsAlive() is false) return;
        // 検出された当たり判定の格納用に使い回す配列 変換時の負荷を回避するためIl2CppReferenceArrayで扱う
        Il2CppReferenceArray<Collider2D> colliders = new(45);
        // 各部屋の人数カウント処理
        foreach (var room in ShipStatus.Instance.AllRooms)
        {
            var roomId = room.RoomId;
            // 通路か当たり判定がないなら何もしない
            if (room.roomArea == null) continue;

            ContactFilter2D filter = new()
            {
                useLayerMask = true,
                layerMask = Constants.LivingPlayersOnlyMask,
                useTriggers = true,
            };
            // 検出された当たり判定の数 検出された当たり判定はここでcollidersに格納される
            var numColliders = room.roomArea.OverlapCollider(filter, colliders);

            // 検出された各当たり判定への処理
            for (var i = 0; i < numColliders; i++)
            {
                var collider = colliders[i];
                // 生きてる場合
                if (!collider.isTrigger && !collider.CompareTag("DeadBody"))
                {
                    var playerControl = collider.GetComponent<PlayerControl>();
                    if (playerControl.IsAlive() && playerControl.GetPlayerState().HasSpawned)
                    {
                        var locationData = AllPlayerLocationData[playerControl.PlayerId];
                        locationData.visitedLocations.Add(roomId);
                    }
                }
            }
        }
    }
    bool ISelfVoter.CanUseVoted() => Canuseability() && !IsAwaken && CanUseAbility && (MeetingUsedSkillCount <= meetingmaximum || meetingmaximum == 0);
    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (Is(voter) && !IsAwaken && CanUseAbility && (MeetingUsedSkillCount <= meetingmaximum || meetingmaximum == 0))
        {
            if (CheckSelfVoteMode(Player, votedForId, out var status))
            {
                switch (status)
                {
                    case VoteStatus.Self:
                        Utils.SendMessage(string.Format(GetString("SkillMode"), GetString("Mode.CrewSatellite"), GetString("Vote.CrewSatellite")) + GetString("VoteSkillMode"), Player.PlayerId);
                        break;
                    case VoteStatus.Skip:
                        Utils.SendMessage(GetString("VoteSkillFin"), Player.PlayerId);
                        break;
                    case VoteStatus.Vote:
                        UseAbility(votedForId);
                        break;
                }
                SetMode(Player, status is VoteStatus.Self);
                return false;
            }
        }
        return true;
    }

    public void UseAbility(byte votedForId)
    {
        if (Utils.IsActive(SystemTypes.Comms))
        {
            Utils.SendMessage(string.Format(GetString("SatelliteModeInfoFall") + string.Format(GetString("EvilSateliteSkillInfo3"), maximum - UsedSkillCount), Player.PlayerId, $"<{RoleInfo.RoleColorCode}>{string.Format(GetString("SatelliteTitle"), UtilsName.GetPlayerColor(votedForId))}"), Player.PlayerId);
            return;
        }

        var systemTypes = SentPlayers.TryGetValue(votedForId, out var sentdata) ? sentdata : null;

        if (systemTypes != null)
        {
            Utils.SendMessage(string.Format(GetString("SatelliteModeInfoAgain"), UtilsName.GetPlayerColor(votedForId), GetString($"{systemTypes}")) + string.Format(GetString("EvilSateliteSkillInfo3"), maximum - UsedSkillCount), Player.PlayerId, $"<{RoleInfo.RoleColorCode}>{string.Format(GetString("SatelliteTitle"), UtilsName.GetPlayerColor(votedForId))}");
            return;
        }

        if (AllPlayerLocationData.TryGetValue(votedForId, out var locationData))
        {
            systemTypes = locationData.visitedLocations.OrderBy(x => Guid.NewGuid()).FirstOrDefault();
            SentPlayers.Add(votedForId, systemTypes);
            UsedSkillCount++;
            MeetingUsedSkillCount++;
            SendRPC();
        }

        Logger.Info($"{Player.Data.GetLogPlayerName()} => {PlayerCatch.GetPlayerInfoById(votedForId).GetLogPlayerName()} $({systemTypes}) , {maximum - UsedSkillCount} / {meetingmaximum - MeetingUsedSkillCount}", "Satellite");
        Utils.SendMessage(string.Format(GetString("SatelliteModeInfo"), UtilsName.GetPlayerColor(votedForId), GetString($"{systemTypes}")) + string.Format(GetString("EvilSateliteSkillInfo3"), maximum - UsedSkillCount), Player.PlayerId, $"<{RoleInfo.RoleColorCode}>{string.Format(GetString("SatelliteTitle"), UtilsName.GetPlayerColor(votedForId))}");
    }

    public override string GetProgressText(bool comms = false, bool GameLog = false) => $" <{RoleInfo.RoleColorCode}>({maximum - UsedSkillCount})</color>";
    public override void AfterMeetingTasks()
    {
        SentPlayers.Clear();
        MeetingUsedSkillCount = 0;
        foreach (var locationData in AllPlayerLocationData.Values)
        {
            locationData.visitedLocations.Clear();
        }
    }
    public override CustomRoles Misidentify() => IsAwaken ? CustomRoles.Crewmate : CustomRoles.NotAssigned;
    public override bool OnCompleteTask(uint taskid)
    {
        if (MyTaskState.HasCompletedEnoughCountOfTasks(skillusetaskcount))
        {
            if (IsAwaken)
            {
                if (!Utils.RoleSendList.Contains(Player.PlayerId))
                    Utils.RoleSendList.Add(Player.PlayerId);
            }
            IsAwaken = false;
        }
        return true;
    }

    public void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(UsedSkillCount);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        UsedSkillCount = reader.ReadInt32();
    }
}