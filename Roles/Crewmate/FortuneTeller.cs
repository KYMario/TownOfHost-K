using System.Collections.Generic;
using System;
using AmongUs.GameOptions;
using UnityEngine;
using Hazel;

using TownOfHost.Roles.Core;
using static TownOfHost.Modules.SelfVoteManager;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Crewmate;

public sealed class FortuneTeller : RoleBase, ISelfVoter
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(FortuneTeller),
            player => new FortuneTeller(player),
            CustomRoles.FortuneTeller,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            9200,
            SetupOptionItem,
            "fo",
            "#6b3ec3",
            (3, 0),
            introSound: () => GetIntroSound(RoleTypes.Scientist)
        );
    public FortuneTeller(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Max = OptionMaximum.GetFloat();
        Divination.Clear();
        count = 0;
        MeetingUsedcount = 0;
        Awakened = !OptAwakening.GetBool() || OptionCanTaskcount.GetInt() < 1;
        Votemode = (AbilityVoteMode)OptionVoteMode.GetValue();
        rolename = Optionrolename.GetBool();
        srole = OptionRole.GetBool();
        cantaskcount = OptionCanTaskcount.GetFloat();
        onemeetingmaximum = Option1MeetingMaximum.GetFloat();
    }

    public static OptionItem OptionMaximum;
    public static OptionItem OptionVoteMode;
    public static OptionItem Optionrolename;
    public static OptionItem OptionRole;
    public static OptionItem OptionCanTaskcount;
    public static OptionItem Option1MeetingMaximum;
    public static OptionItem OptAwakening;
    public float Max;
    public AbilityVoteMode Votemode;
    public bool rolename;
    public bool srole;
    int count;
    float cantaskcount;
    float onemeetingmaximum;
    float MeetingUsedcount;
    bool Awakened;
    Dictionary<byte, CustomRoles> Divination = new();

    enum Option
    {
        TellMaximum,
        AbilityVotemode,
        TellerCanSeeRolename, //占った相手の名前の上に占い結果を表示するかの設定
        TellRole, //占い時役職を表示するか、陣営を表示するかの設定
    }

    private static void SetupOptionItem()
    {
        OptionMaximum = IntegerOptionItem.Create(RoleInfo, 10, Option.TellMaximum, new(1, 99, 1), 1, false)
            .SetValueFormat(OptionFormat.Times);
        OptionVoteMode = StringOptionItem.Create(RoleInfo, 11, Option.AbilityVotemode, EnumHelper.GetAllNames<AbilityVoteMode>(), 1, false);
        Optionrolename = BooleanOptionItem.Create(RoleInfo, 12, Option.TellerCanSeeRolename, true, false);
        OptionRole = BooleanOptionItem.Create(RoleInfo, 13, Option.TellRole, true, false);
        OptionCanTaskcount = IntegerOptionItem.Create(RoleInfo, 14, GeneralOption.cantaskcount, new(0, 99, 1), 5, false);
        Option1MeetingMaximum = IntegerOptionItem.Create(RoleInfo, 15, GeneralOption.MeetingMaxTime, new(0, 99, 1), 0, false)
            .SetValueFormat(OptionFormat.Times).SetZeroNotation(OptionZeroNotation.Infinity);
        OptAwakening = BooleanOptionItem.Create(RoleInfo, 16, GeneralOption.AbilityAwakening, false, false);
    }

    private void SendRPC(byte targetid, CustomRoles role)
    {
        using var sender = CreateSender();
        sender.Writer.Write(count);
        sender.Writer.Write(targetid);
        sender.Writer.WritePacked((int)role);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        count = reader.ReadInt32();
        Divination[reader.ReadByte()] = (CustomRoles)reader.ReadPackedInt32();
    }
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (Divination.ContainsKey(seen.PlayerId) && rolename)
        {
            if (srole)
                return $"<color={UtilsRoleText.GetRoleColorCode(Divination[seen.PlayerId])}>" + GetString(Divination[seen.PlayerId].ToString());
            else return GetString(Divination[seen.PlayerId].GetCustomRoleTypes().ToString());
        }
        return "";
    }
    public override string GetProgressText(bool comms = false, bool gamelog = false) => Utils.ColorString(!MyTaskState.HasCompletedEnoughCountOfTasks(cantaskcount) ? Color.gray : Max <= count ? Color.gray : Color.cyan, $"({Max - count})");
    public override void OnStartMeeting() => MeetingUsedcount = 0;
    bool ISelfVoter.CanUseVoted() => Canuseability() && Max > count && MyTaskState.HasCompletedEnoughCountOfTasks(cantaskcount) && (MeetingUsedcount < onemeetingmaximum || onemeetingmaximum == 0);
    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (!Canuseability()) return true;
        if (Max > count && Is(voter) && MyTaskState.HasCompletedEnoughCountOfTasks((int)cantaskcount) && (MeetingUsedcount < onemeetingmaximum || onemeetingmaximum == 0))
        {
            var target = PlayerCatch.GetPlayerById(votedForId);
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
        var target = PlayerCatch.GetPlayerById(votedForId);
        if (!target.IsAlive()) return;//死んでるならここで処理を止める。
        count++;//全体のカウント
        MeetingUsedcount++;//1会議のカウント
        var role = target.GetTellResults(Player); //結果を変更するかチェック
        var lasttext = GetString("Skill.Tellerfin") + (role.IsCrewmate() ? "!" : "...");
        Divination[votedForId] = role;
        SendRPC(votedForId, role);
        Utils.SendMessage(string.Format(GetString("Skill.Teller"), UtilsName.GetPlayerColor(target, true), srole ? "<b>" + GetString($"{role}").Color(UtilsRoleText.GetRoleColor(role)) + "</b>" : GetString($"{role.GetCustomRoleTypes()}")) + lasttext + $"\n\n" + (onemeetingmaximum != 0 ? string.Format(GetString("RemainingOneMeetingCount"), Math.Min(onemeetingmaximum - MeetingUsedcount, Max - count)) : string.Format(GetString("RemainingCount"), Max - count) + (Votemode == AbilityVoteMode.SelfVote ? "\n\n" + GetString("VoteSkillFin") : "")), Player.PlayerId);
        Logger.Info($"Player: {Player.name},Target: {target.name}, count: {count}", "FortuneTeller");
    }
    public override CustomRoles Misidentify() => Awakened ? CustomRoles.NotAssigned : CustomRoles.Crewmate;
    public override bool OnCompleteTask(uint taskid)
    {
        if (MyTaskState.HasCompletedEnoughCountOfTasks(cantaskcount))
        {
            if (Awakened == false)
                if (!Utils.RoleSendList.Contains(Player.PlayerId))
                    Utils.RoleSendList.Add(Player.PlayerId);
            Awakened = true;
        }
        return true;
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (isForMeeting && Player.IsAlive() && Awakened && seer.PlayerId == seen.PlayerId && Canuseability() && Max > count && MyTaskState.HasCompletedEnoughCountOfTasks(cantaskcount))
        {
            var mes = $"<color={RoleInfo.RoleColorCode}>{(Votemode == AbilityVoteMode.SelfVote ? GetString("SelfVoteRoleInfoMeg") : GetString("NomalVoteRoleInfoMeg"))}</color>";
            return isForHud ? mes : $"<size=40%>{mes}</size>";
        }
        return "";
    }
}