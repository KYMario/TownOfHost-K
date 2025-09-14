using AmongUs.GameOptions;
using UnityEngine;
using Hazel;
using TownOfHost.Roles.Core;
using System;
using static TownOfHost.Modules.SelfVoteManager;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Crewmate;

public sealed class ShrineMaiden : RoleBase, ISelfVoter
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(ShrineMaiden),
            player => new ShrineMaiden(player),
            CustomRoles.ShrineMaiden,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            9600,
            SetupOptionItem,
            "SM",
            "#b7282e",
            (3, 4),
            introSound: () => GetIntroSound(RoleTypes.Scientist)
        );
    public ShrineMaiden(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Max = OptionMaximum.GetFloat();
        IsReport = false;
        count = 0;
        MeetingUsedcount = 0;
        cantaskcount = Optioncantaskcount.GetFloat();
        Awakened = !OptAwakening.GetBool() || cantaskcount < 1;
        Votemode = (VoteMode)OptionVoteMode.GetValue();
        onemeetingmaximum = Option1MeetingMaximum.GetFloat();
        OnikuId = byte.MaxValue;
    }

    private static OptionItem OptionMaximum;
    private static OptionItem OptionVoteMode;
    private static OptionItem Optioncantaskcount;
    private static OptionItem Option1MeetingMaximum;
    static OptionItem OptAwakening;
    bool Awakened;
    public float Max;
    public VoteMode Votemode;
    int count;
    float cantaskcount;
    float onemeetingmaximum;
    float MeetingUsedcount;
    static bool IsReport;
    static byte OnikuId;

    enum Option
    {
        TellMaximum,
        AbilityVotemode,
    }
    public enum VoteMode
    {
        NomalVote,
        SelfVote,
    }

    private static void SetupOptionItem()
    {
        OptionMaximum = IntegerOptionItem.Create(RoleInfo, 11, Option.TellMaximum, new(1, 99, 1), 1, false)
            .SetValueFormat(OptionFormat.Times);
        OptionVoteMode = StringOptionItem.Create(RoleInfo, 12, Option.AbilityVotemode, EnumHelper.GetAllNames<AbilityVoteMode>(), 1, false);
        Optioncantaskcount = IntegerOptionItem.Create(RoleInfo, 14, GeneralOption.cantaskcount, new(0, 99, 1), 5, false);
        Option1MeetingMaximum = IntegerOptionItem.Create(RoleInfo, 15, GeneralOption.MeetingMaxTime, new(0, 99, 1), 0, false)
            .SetValueFormat(OptionFormat.Times);
        OptAwakening = BooleanOptionItem.Create(RoleInfo, 16, GeneralOption.AbilityAwakening, false, false);
    }
    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(count);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        count = reader.ReadInt32();
    }
    public override void OnReportDeadBody(PlayerControl _, NetworkedPlayerInfo target)
    {
        if (target == null)
        {
            IsReport = false;
        }
        else
        {
            IsReport = true;
            OnikuId = target.PlayerId;
        }
    }
    public override void AfterMeetingTasks()
    {
        if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;
        IsReport = false;//いらない気がするけど一応保険
        OnikuId = byte.MaxValue;
    }
    public override void OnStartMeeting() => MeetingUsedcount = 0;
    public override string GetProgressText(bool comms = false, bool gamelog = false) => Utils.ColorString(!MyTaskState.HasCompletedEnoughCountOfTasks(cantaskcount) ? Color.gray : Max <= count ? Color.gray : Color.cyan, $"({Max - count})");
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (IsReport && Player.IsAlive() && isForMeeting && Awakened && seer.PlayerId == seen.PlayerId && Canuseability() && Max > count && MyTaskState.HasCompletedEnoughCountOfTasks(cantaskcount))
        {
            var mes = $"<color={RoleInfo.RoleColorCode}>{(Votemode == VoteMode.SelfVote ? GetString("SelfVoteRoleInfoMeg") : GetString("NomalVoteRoleInfoMeg"))}</color>";
            return isForHud ? mes : $"<size=40%>{mes}</size>";
        }
        return "";
    }
    bool ISelfVoter.CanUseVoted() => Canuseability() && IsReport && MyTaskState.HasCompletedEnoughCountOfTasks(cantaskcount) && (MeetingUsedcount < onemeetingmaximum || onemeetingmaximum == 0);

    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (!Canuseability()) return true;
        if (IsReport && Max > count && Is(voter) && MyTaskState.HasCompletedEnoughCountOfTasks(cantaskcount) && (MeetingUsedcount < onemeetingmaximum || onemeetingmaximum == 0))
        {
            if (Votemode == VoteMode.NomalVote)
            {
                if (Player.PlayerId == votedForId || votedForId == SkipId) return true;
                ShrineMaidenAbility(votedForId);
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
                        ShrineMaidenAbility(votedForId);
                    SetMode(Player, status is VoteStatus.Self);
                    return false;
                }
            }
        }
        return true;
    }
    public void ShrineMaidenAbility(byte votedForId)
    {
        var target1 = PlayerCatch.GetPlayerById(OnikuId);
        var target2 = PlayerCatch.GetPlayerById(votedForId);
        if (!target2.IsAlive()) return;
        count++;
        MeetingUsedcount++;

        Logger.Info($"Player: {Player.name},Target1: {target1.name}Target2: {target2.name}", "ShrineMaiden");
        var role1 = target1.GetTellResults(Player);
        var role2 = target2.GetTellResults(Player);
        SendRPC();
        var t1 = role1.GetCustomRoleTypes();
        var t2 = role2.GetCustomRoleTypes();
        var madmate = Options.MadTellOpt().GetCustomRoleTypes();
        //マッドならimpにする
        if (t1 == CustomRoleTypes.Madmate) t1 = madmate is CustomRoleTypes.Madmate ? CustomRoleTypes.Impostor : madmate;
        if (t2 == CustomRoleTypes.Madmate) t2 = madmate is CustomRoleTypes.Madmate ? CustomRoleTypes.Impostor : madmate;

        if (t1 == t2)
        {
            Utils.SendMessage(string.Format(GetString("ShrineMaidencollect"), UtilsName.GetPlayerColor(target1, true), UtilsName.GetPlayerColor(target2, true)) + (onemeetingmaximum != 0 ? string.Format(GetString("RemainingOneMeetingCount"), Math.Min(onemeetingmaximum - MeetingUsedcount, Max - count)) : string.Format(GetString("RemainingCount"), Max - count)) + (Votemode == VoteMode.SelfVote ? "\n\n" + GetString("VoteSkillFin") : ""), Player.PlayerId);
        }
        else
        {
            Utils.SendMessage(string.Format(GetString("ShrineMaidennotcollect"), UtilsName.GetPlayerColor(target1, true), UtilsName.GetPlayerColor(target2, true)) + (onemeetingmaximum != 0 ? string.Format(GetString("RemainingOneMeetingCount"), Math.Min(onemeetingmaximum - MeetingUsedcount, Max - count)) : string.Format(GetString("RemainingCount"), Max - count)) + (Votemode == VoteMode.SelfVote ? "\n\n" + GetString("VoteSkillFin") : ""), Player.PlayerId);
        }
    }
    public override CustomRoles Misidentify() => Awakened ? CustomRoles.NotAssigned : CustomRoles.Crewmate;
    public override bool OnCompleteTask(uint taskid)
    {
        if (MyTaskState.HasCompletedEnoughCountOfTasks(cantaskcount)) Awakened = true;
        return true;
    }
}