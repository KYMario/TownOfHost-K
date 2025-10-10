using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Hazel;
using HarmonyLib;
using UnityEngine;
using AmongUs.GameOptions;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Modules.SelfVoteManager;

namespace TownOfHost.Roles.Neutral;

public sealed class Missioneer : RoleBase, IKiller, ISelfVoter, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Missioneer),
            player => new Missioneer(player),
            CustomRoles.Missioneer,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Neutral,
            23200,
            SetupOptionItem,
            "Ms",
            "#b1ae8f",
            (5, 6),
            true,
            introSound: () => GetIntroSound(RoleTypes.Detective)
        );
    public Missioneer(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.ForRecompute
    )
    {
        NowMissionLists = new();
        NowPoint = 0;
        NowMission = MissionList.Non;
        target = byte.MaxValue;
        Gotovent = false;
        Room = null;
        AddWin = false;
        Seted = false;
        ventpos = (Vector3.zero, -1);
        timer = 0;

        KillCooldown = OptionKillCoolDown.GetFloat();
        meetingassignmentcount = OptionMeetingAssignmentCount.GetInt();
        WinAssignmentpoint = OptionWinAssignmentPoint.GetInt();
        AddWinAssignmentpoint = OptionAddWinAssignmentPoint.GetInt();

        lv1point = Option1LvPoint.GetInt();
        lv2point = Option2lvpoint.GetInt();
        lv3point = Option3lvpoint.GetInt();
        lv4point = Option4lvpoint.GetInt();
        killpoint = OptionKillpoint.GetInt();
        movepoint = Optionmovepoint.GetInt();
        taskpoint = OptionTaskpoint.GetInt();
    }
    int NowPoint; bool AddWin;
    MissionList NowMission; byte target; SystemTypes? Room; bool Gotovent; (Vector3 ventpos, int id) ventpos;
    Dictionary<byte, MissionList> NowMissionLists; bool Seted;
    float timer;

    static OptionItem OptionKillCoolDown; static float KillCooldown;
    static OptionItem OptionMeetingAssignmentCount; static int meetingassignmentcount;
    static OptionItem OptionWinAssignmentPoint; static int WinAssignmentpoint;
    static OptionItem OptionAddWinAssignmentPoint; static int AddWinAssignmentpoint;
    static OptionItem Option1LvPoint; static int lv1point;
    static OptionItem Option2lvpoint; static int lv2point;
    static OptionItem Option3lvpoint; static int lv3point;
    static OptionItem Option4lvpoint; static int lv4point;
    static OptionItem OptionKillpoint; static int killpoint;
    static OptionItem Optionmovepoint; static int movepoint;
    static OptionItem OptionTaskpoint; static int taskpoint;


    enum MissionList
    {
        Non = -1,
        Kill = 0, KillToVent, KillRoom, KillPlayer,
        GoRoom = 10, GoVent, SeePlayer, MorePlayer,
        Task = 20, Vote, AllTaskComp, Report
    }
    enum OptionName
    {
        MissioneerWinAssignmntPoint, MissioneerAddWinAssignmntPoint,
        MissioneerMeetingAssignmentcount,
        Missioneerlv1point, Missioneerlv2point, Missioneerlv3point, Missioneerlv4point,
        MissioneerKillpoint, MissioneerMovepoint, MissioneerTaskpoint,
    }

    private static void SetupOptionItem()
    {
        OptionKillCoolDown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, OptionBaseCoolTime, 20f, false).SetValueFormat(OptionFormat.Seconds);
        OptionWinAssignmentPoint = IntegerOptionItem.Create(RoleInfo, 11, OptionName.MissioneerWinAssignmntPoint, new(0, 300, 1), 30, false).SetZeroNotation(OptionZeroNotation.Off);
        OptionAddWinAssignmentPoint = IntegerOptionItem.Create(RoleInfo, 20, OptionName.MissioneerAddWinAssignmntPoint, new(0, 300, 1), 20, false).SetZeroNotation(OptionZeroNotation.Off);
        SoloWinOption.Create(RoleInfo, 21);
        OptionMeetingAssignmentCount = IntegerOptionItem.Create(RoleInfo, 12, OptionName.MissioneerMeetingAssignmentcount, new(0, 5, 1), 3, false);
        Option1LvPoint = IntegerOptionItem.Create(RoleInfo, 13, OptionName.Missioneerlv1point, new(0, 25, 1), 0, false);
        Option2lvpoint = IntegerOptionItem.Create(RoleInfo, 14, OptionName.Missioneerlv2point, new(0, 25, 1), 1, false);
        Option3lvpoint = IntegerOptionItem.Create(RoleInfo, 15, OptionName.Missioneerlv3point, new(0, 25, 1), 3, false);
        Option4lvpoint = IntegerOptionItem.Create(RoleInfo, 16, OptionName.Missioneerlv4point, new(0, 25, 1), 5, false);
        OptionKillpoint = IntegerOptionItem.Create(RoleInfo, 17, OptionName.MissioneerKillpoint, new(0, 25, 1), 5, false);
        Optionmovepoint = IntegerOptionItem.Create(RoleInfo, 18, OptionName.MissioneerMovepoint, new(0, 25, 1), 2, false);
        OptionTaskpoint = IntegerOptionItem.Create(RoleInfo, 19, OptionName.MissioneerTaskpoint, new(0, 25, 1), 1, false);
        OverrideTasksData.Create(RoleInfo, 22, tasks: (true, 0, 1, 2));
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = 0;
        AURoleOptions.EngineerInVentMaxTime = 0;
        opt.SetVision(false);
    }
    public override bool CanTask() => !(NowMission is not MissionList.Non && (int)NowMission < 10);
    public override RoleTypes? AfterMeetingRole => NowMission is not MissionList.Non && (int)NowMission < 10 ? RoleTypes.Impostor : RoleTypes.Engineer;
    bool IKiller.CanKill => NowMission is not MissionList.Non && (int)NowMission < 10;
    public bool CanUseKillButton() => NowMission is not MissionList.Non && (int)NowMission < 10;         //↓こうしないとエンジニアベントが弾かれる
    public bool CanUseImpostorVentButton() => (NowMission is not MissionList.Non && (int)NowMission < 10) || !Player.IsModClient();
    public override bool CanUseAbilityButton() => !(NowMission is not MissionList.Non && (int)NowMission < 10);
    public bool CanUseSabotageButton() => false;
    public float CalculateKillCooldown() => KillCooldown;


    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;

        if (NowMission is MissionList.Kill or MissionList.KillPlayer or MissionList.KillRoom or MissionList.KillToVent)
        {
            switch (NowMission)
            {
                case MissionList.Kill: ComplateMission(); return;
                case MissionList.KillToVent:
                    Gotovent = true;
                    return;
                case MissionList.KillPlayer:
                    if (target.PlayerId == this.target)
                    {
                        ComplateMission();
                        return;
                    }
                    break;
                case MissionList.KillRoom:
                    if (target.GetPlainShipRoom()?.RoomId == Room)
                    {
                        ComplateMission();
                        return;
                    }
                    break;
                default: break;
            }
        }
        info.DoKill = false;
    }
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (Gotovent && NowMission is MissionList.KillToVent)
        {
            ComplateMission();
        }
        if (NowMission is MissionList.GoVent && ventpos.id == ventId)
        {
            ComplateMission();
        }
        return true;
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (Is(reporter) && target is not null && NowMission is MissionList.Report) ComplateMission();

        NowMissionLists = GetMissionList(Math.Min(meetingassignmentcount, PlayerCatch.AllAlivePlayersCount - 1));
        Seted = false;
        timer = 0;
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (AmongUsClient.Instance.AmHost is false || player.IsAlive() is false) return;

        if (NowMission is MissionList.MorePlayer or MissionList.SeePlayer)
        {
            if (timer < 10)
            {
                timer += Time.fixedDeltaTime;
                return;
            }
            var mypos = player.GetTruePosition();
            List<PlayerControl> NeerPlayers = new();

            foreach (var pc in PlayerCatch.AllAlivePlayerControls)
            {
                if (Vector2.Distance(pc.GetTruePosition(), mypos) < 2.5f)
                {
                    NeerPlayers.Add(pc);
                }
            }

            if (NowMission is MissionList.MorePlayer)
            {
                if (NeerPlayers.Count > 3)//コード上は自身も含まれるので3
                {
                    ComplateMission();
                }
            }
            if (NowMission is MissionList.SeePlayer)
            {
                if (NeerPlayers.Any(pc => pc.PlayerId == target))
                {
                    ComplateMission();
                }
            }
        }
        if (NowMission is MissionList.GoRoom)
        {
            if (Player.GetPlainShipRoom()?.RoomId == Room) ComplateMission();
        }
    }
    public override bool OnCompleteTask(uint taskid)
    {
        if (MyTaskState.RemainingTasksCount <= 0 && NowMission is MissionList.AllTaskComp)
        {
            ComplateMission();
        }
        if (NowMission is MissionList.Task)
        {
            ComplateMission();
        }
        return true;
    }
    public override bool VotingResults(ref NetworkedPlayerInfo Exiled, ref bool IsTie, Dictionary<byte, int> vote, byte[] mostVotedPlayers, bool ClearAndExile)
    {
        if (NowMission is MissionList.Vote && vote.TryGetValue(Player.PlayerId, out var count))
        {
            if (2 <= count) ComplateMission();
        }
        return false;
    }

    bool ISelfVoter.CanUseVoted() => Seted is false;
    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (!Canuseability()) return true;
        if (Is(voter) && !Seted)
        {
            if (CheckSelfVoteMode(Player, votedForId, out var status))
            {
                if (status is VoteStatus.Self)
                    ShowMissionList();
                if (status is VoteStatus.Skip)
                    Utils.SendMessage(GetString("VoteSkillFin"), Player.PlayerId);
                if (status is VoteStatus.Vote)
                    SetMission(votedForId);
                SetMode(Player, status is VoteStatus.Self);
                return false;
            }
        }
        return true;
    }
    void ShowMissionList()
    {
        StringBuilder sb = new();
        sb.Append($"<size=80%>{GetString("MissioneerMeg")}");
        foreach (var mission in NowMissionLists)
        {
            var data = PlayerCatch.GetPlayerInfoById(mission.Key);
            var point = GetPoint(mission.Value);
            sb.Append($"{Palette.GetColorName(data.DefaultOutfit.ColorId)} → {GetString($"mission.{mission.Value}")}　{point.All}<size=60%>({point.lv}+{point.category})</size>\n");
        }
        Utils.SendMessage(sb.ToString(), Player.PlayerId);
    }
    Dictionary<byte, MissionList> GetMissionList(int missioncount)
    {
        Dictionary<byte, MissionList> MissionLists = [];
        var list = EnumHelper.GetAllValues<MissionList>();
        var aliveplayerlist = PlayerCatch.AllAlivePlayerControls.Where(pc => pc.PlayerId != Player.PlayerId).ToList();
        for (var i = 0; i < missioncount; i++)
        {
            if (aliveplayerlist.Count < i) break;
            var pc = aliveplayerlist[i];
            var chance = IRandom.Instance.Next(list.Count());
            var mission = list[chance];

            if (mission is MissionList.Non || pc.PlayerId == Player.PlayerId)
            {
                i--;
                continue;
            }
            MissionLists.Add(pc.PlayerId, mission);
        }

        return MissionLists;
    }
    void SetMission(byte votedForId)
    {
        if (NowMissionLists.TryGetValue(votedForId, out var mission))
        {
            if (ventpos.ventpos != Vector3.zero)
            {
                GetArrow.Remove(Player.PlayerId, ventpos.ventpos);
                ventpos = (Vector3.zero, -1);
            }
            Seted = true;
            target = byte.MaxValue;
            Gotovent = false;
            Room = null;
            NowMission = mission;
            Player.Data.RpcSetTasks(Array.Empty<byte>());
            MyTaskState.CompletedTasksCount = 0;

            if (mission is MissionList.KillPlayer or MissionList.SeePlayer)
            {
                var players = PlayerCatch.AllAlivePlayerControls.Where(pc => pc.PlayerId != Player.PlayerId);
                players = players.OrderBy(x => Guid.NewGuid());
                var targetpc = players.ToArray()[IRandom.Instance.Next(players.Count())];
                target = targetpc.PlayerId;
            }
            if (mission is MissionList.GoRoom or MissionList.KillRoom)
            {
                List<SystemTypes> rooms = new();
                ShipStatus.Instance.AllRooms.Where(room => room?.RoomId is not null and not SystemTypes.Hallway).Do(r => rooms.Add(r.RoomId));

                var rand = IRandom.Instance;
                Room = rooms[rand.Next(0, rooms.Count)];
            }
            if (mission is MissionList.GoVent)
            {
                List<Vent> vents = new();
                ShipStatus.Instance.AllVents.Do(r => vents.Add(r));

                var rand = IRandom.Instance;
                var vent = vents[rand.Next(0, vents.Count)];
                ventpos = (vent.transform.position, vent.Id);

                GetArrow.Add(Player.PlayerId, ventpos.ventpos);
            }

            Utils.SendMessage(string.Format(GetString("MissioneerSetMissionMeg"), GetString($"mission.{NowMission}")), Player.PlayerId);
            SendRPC();
        }
        else
            Utils.SendMessage(GetString("VoteSkillFin"), Player.PlayerId);
    }

    (int All, int category, int lv) GetPoint(MissionList mission)
    {
        var nowlv = 0;
        var lv = 0;
        var category = 0;
        if (mission < MissionList.GoRoom)//キル系統
        {
            category += killpoint;
            lv = (int)mission;
        }
        else if (mission < MissionList.Task)//移動系
        {
            category += movepoint;
            lv = (int)mission - 10;
        }
        else //タスク系
        {
            category += taskpoint;
            lv = (int)mission - 20;
        }

        switch (nowlv)
        {
            case 0: lv += lv1point; break;
            case 1: lv += lv2point; break;
            case 2: lv += lv3point; break;
            case 3: lv += lv4point; break;
            default: Logger.Error($"UnKnownLv. {nowlv}", "Missioneer"); break;
        }
        return (lv + category, category, lv);
    }
    void ComplateMission()
    {
        if (AmongUsClient.Instance.AmHost is false) return;
        //ポイント追加
        var getpoint = GetPoint(NowMission).All;
        Logger.Info($"ComplateMisson:{NowPoint + getpoint} ({NowPoint} + {getpoint}) / {NowMission}", "Missioneer");
        NowPoint += getpoint;

        UtilsGameLog.AddGameLog("Missioneer", string.Format(GetString("MissioneerAddpoint"), getpoint, NowPoint));
        //勝利条件チェック
        if (WinAssignmentpoint <= NowPoint && WinAssignmentpoint is not 0)//単独
        {
            if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Missioneer, Player.PlayerId, true))
            {
                CustomWinnerHolder.NeutralWinnerIds.Add(Player.PlayerId);
            }
        }
        if (AddWinAssignmentpoint <= NowPoint && WinAssignmentpoint is not 0)//追加
        {
            AddWin = true;
        }

        //リセット処理
        NowMission = MissionList.Non;

        if (ventpos.ventpos != Vector3.zero)
        {
            GetArrow.Remove(Player.PlayerId, ventpos.ventpos);
            ventpos = (Vector3.zero, -1);
        }
        target = byte.MaxValue;
        Gotovent = false;
        Room = null;
        SendRPC();
        Player.RpcProtectedMurderPlayer();
        RPC.PlaySoundRPC(Player.PlayerId, Sounds.TaskComplete);
        UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: [Player]);
    }

    public override string GetProgressText(bool comms = false, bool GameLog = false)
        => $"<{RoleInfo.RoleColorCode}>({NowPoint}/{(AddWinAssignmentpoint is not 0 && !AddWin ? AddWinAssignmentpoint : WinAssignmentpoint)})";
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (seen != seer) return "";
        var mark = AddWin ? Utils.AdditionalWinnerMark : "";
        if (isForMeeting || NowMission is not MissionList.GoVent) return mark;
        return GetArrow.GetArrows(Player, ventpos.ventpos) + mark;
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (seen == seer && NowMission is not MissionList.Non)
        {
            var add = "";
            if (NowMission is MissionList.KillPlayer or MissionList.SeePlayer)
            {
                add = $"\nTarget:{target.GetPlayerControl()?.GetPlayerColor() ?? "???"}";
            }
            if (NowMission is MissionList.GoRoom or MissionList.KillRoom)
            {
                add = $"\nRoom:{GetString($"{Room}")}";
            }
            return $"<size=80%><{RoleInfo.RoleColorCode}>{GetString($"mission.{NowMission}")} {add}</color>";
        }
        return "";
    }

    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write((int)NowMission);
        sender.Writer.Write(NowPoint);
        sender.Writer.Write(target);
        sender.Writer.Write(Gotovent);
        sender.Writer.Write(Room is null ? -5 : (int)Room);
        sender.Writer.Write(AddWin);
        sender.Writer.Write(ventpos.id);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        NowMission = (MissionList)reader.ReadInt32();
        NowPoint = reader.ReadInt32();
        target = reader.ReadByte();
        Gotovent = reader.ReadBoolean();
        var room = reader.ReadInt32();
        Room = room is -5 ? null : (SystemTypes)room;
        AddWin = reader.ReadBoolean();
        var id = reader.ReadInt32();
        ventpos = (ShipStatus.Instance.AllVents[id].transform.position, id);
    }

    public bool CheckWin(ref CustomRoles winnerRole) => AddWin && Player.IsAlive();
}