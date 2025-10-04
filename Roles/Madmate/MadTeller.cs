using AmongUs.GameOptions;
using UnityEngine;
using Hazel;
using System;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

using static TownOfHost.Modules.SelfVoteManager;

namespace TownOfHost.Roles.Madmate;

public sealed class MadTeller : RoleBase, IKillFlashSeeable, IDeathReasonSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(MadTeller),
            player => new MadTeller(player),
            CustomRoles.MadTeller,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Madmate,
            8100,
            SetupOptionItem,
            "Mt",
            OptionSort: (3, 1),
            introSound: () => GetIntroSound(RoleTypes.Scientist)
        );
    public MadTeller(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.ForRecompute
    )
    {
        collect = Optioncollect.GetInt();
        Max = OptionMaximum.GetFloat();
        count = 0;
        MeetingUsedcount = 0;
        srole = OptionRole.GetBool();
        canSeeKillFlash = Options.MadmateCanSeeKillFlash.GetBool();
        canSeeDeathReason = Options.MadmateCanSeeDeathReason.GetBool();
        Votemode = (AbilityVoteMode)OptionVoteMode.GetValue();
        onemeetingmaximum = Option1MeetingMaximum.GetFloat();
        MyTaskState.NeedTaskCount = OptionTaskTrigger.GetInt();
    }
    private static bool canSeeKillFlash;
    private static bool canSeeDeathReason;
    private static OptionItem OptionTaskTrigger;
    private static OptionItem Optioncollect;
    private static OptionItem OptionMaximum;
    private static OptionItem OptionRole;
    private static OptionItem OptionVoteMode;
    private static OptionItem Option1MeetingMaximum;
    public bool? CheckKillFlash(MurderInfo info) => canSeeKillFlash;
    public bool? CheckSeeDeathReason(PlayerControl seen) => canSeeDeathReason;
    public override CustomRoles TellResults(PlayerControl player) => Options.MadTellOpt();
    public static float collect;
    public bool srole;
    public float Max;
    public AbilityVoteMode Votemode;
    int count;
    float onemeetingmaximum;
    float MeetingUsedcount;

    enum Option
    {
        TellerCollectRect,
        TellMaximum,
        TellRole,
        AbilityVotemode
    }

    private static void SetupOptionItem()
    {
        Optioncollect = FloatOptionItem.Create(RoleInfo, 10, Option.TellerCollectRect, new(0f, 100f, 2f), 100f, false)
            .SetValueFormat(OptionFormat.Percent);
        OptionMaximum = IntegerOptionItem.Create(RoleInfo, 11, Option.TellMaximum, new(1, 99, 1), 1, false)
            .SetValueFormat(OptionFormat.Times);
        OptionVoteMode = StringOptionItem.Create(RoleInfo, 12, Option.AbilityVotemode, EnumHelper.GetAllNames<AbilityVoteMode>(), 1, false);
        OptionRole = BooleanOptionItem.Create(RoleInfo, 13, Option.TellRole, true, false);
        Option1MeetingMaximum = IntegerOptionItem.Create(RoleInfo, 14, GeneralOption.MeetingMaxTime, new(0, 99, 1), 0, false)
            .SetValueFormat(OptionFormat.Times).SetZeroNotation(OptionZeroNotation.Infinity);
        OptionTaskTrigger = IntegerOptionItem.Create(RoleInfo, 15, GeneralOption.TaskTrigger, new(0, 99, 1), 1, false).SetValueFormat(OptionFormat.Pieces);
        OverrideTasksData.Create(RoleInfo, 20);
    }

    public override void Add() => AddSelfVotes(Player);
    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(count);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        count = reader.ReadInt32();
    }
    bool Check() => MyTaskState.HasCompletedEnoughCountOfTasks(OptionTaskTrigger.GetInt());
    public override void OnStartMeeting() => MeetingUsedcount = 0;
    public override string GetProgressText(bool comms = false, bool gamelog = false) => Utils.ColorString(!Check() ? Color.gray : Max <= count ? Color.gray : Color.cyan, $"({Max - count})");
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (isForMeeting && Player.IsAlive() && seer.PlayerId == seen.PlayerId && Canuseability() && Max > count && Check())
        {
            var mes = $"<color={RoleInfo.RoleColorCode}>{(Votemode == AbilityVoteMode.SelfVote ? GetString("SelfVoteRoleInfoMeg") : GetString("NomalVoteRoleInfoMeg"))}</color>";
            return isForHud ? mes : $"<size=40%>{mes}</size>";
        }
        return "";
    }
    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (!Canuseability()) return true;
        if (Max > count && Is(voter) && Check() && (MeetingUsedcount < onemeetingmaximum || onemeetingmaximum == 0))
        {
            if (Votemode == AbilityVoteMode.NomalVote)
            {
                if (Player.PlayerId == votedForId || votedForId == SkipId) return true;
                UseTellAbility(votedForId);
                return false;
            }
            else
            {
                if (CheckSelfVoteMode(Player, votedForId, out var status))
                {
                    if (status is VoteStatus.Self)
                        Utils.SendMessage(string.Format(GetString("SkillMode"), GetString("Mode.Divied"), GetString("Vote.Divied")) + GetString("VoteSkillMode"), Player.PlayerId);
                    if (status is VoteStatus.Skip)
                        Utils.SendMessage(GetString("VoteSkillFin"), Player.PlayerId);
                    if (status is VoteStatus.Vote)
                        UseTellAbility(votedForId);
                    SetMode(Player, status is VoteStatus.Self);
                    return false;
                }
            }
        }
        return true;
    }
    public void UseTellAbility(byte votedForId)
    {
        int chance = IRandom.Instance.Next(1, 101);
        var target = PlayerCatch.GetPlayerById(votedForId);
        if (!target.IsAlive()) return;
        count++;
        MeetingUsedcount++;
        if (chance <= collect || collect is 100)
        {
            Logger.Info($"Player: {Player.name},Target: {target.name}, count: {count}(成功)", "MadTeller");
            var role = target.GetTellResults(Player); //結果を変更するかチェック
            var lasttext = "です" + (role.IsImpostorTeam() ? "!" : "...");
            SendRPC();
            Utils.SendMessage(string.Format(GetString("Skill.Teller"), UtilsName.GetPlayerColor(target, true), srole ? "<b>" + GetString($"{role}").Color(UtilsRoleText.GetRoleColor(role)) + "</b>" : GetString($"{role.GetCustomRoleTypes()}")) + lasttext + $"\n\n" + (onemeetingmaximum != 0 ? string.Format(GetString("RemainingOneMeetingCount"), Math.Min(onemeetingmaximum - MeetingUsedcount, Max - count)) : string.Format(GetString("RemainingCount"), Max - count) + (Votemode == AbilityVoteMode.SelfVote ? "\n\n" + GetString("VoteSkillFin") : "")), Player.PlayerId);
        }
        else
        {
            Logger.Info($"Player: {Player.name},Target: {target.name}, count: {count}(失敗)", "MadTeller");
            SendRPC();
            Utils.SendMessage(string.Format(GetString("Skill.MadTeller"), UtilsName.GetPlayerColor(target, true)) + "\n\n" + (onemeetingmaximum != 0 ? string.Format(GetString("RemainingOneMeetingCount"), Math.Min(onemeetingmaximum - MeetingUsedcount, Max - count)) : string.Format(GetString("RemainingCount"), Max - count) + (Votemode == AbilityVoteMode.SelfVote ? "\n\n" + GetString("VoteSkillFin") : "")), Player.PlayerId);
        }
    }
    public override bool OnCompleteTask(uint taskid)
    {
        if (Check())
        {
            Player.MarkDirtySettings();
        }

        return true;
    }
}