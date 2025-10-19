using System.Collections.Generic;
using AmongUs.GameOptions;
using UnityEngine;
using Hazel;
using TownOfHost.Roles.Core;
using System;
using static TownOfHost.Modules.SelfVoteManager;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Crewmate;

public sealed class PonkotuTeller : RoleBase, ISelfVoter
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(PonkotuTeller),
            player => new PonkotuTeller(player),
            CustomRoles.PonkotuTeller,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            9300,
            SetupOptionItem,
            "po",
            "#6b3ec3",
            (3, 1),
            introSound: () => GetIntroSound(RoleTypes.Scientist)
        );
    public PonkotuTeller(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        collect = Optioncollect.GetInt();
        Max = OptionMaximum.GetFloat();
        Divination.Clear();
        count = 0;
        MeetingUsedcount = 0;
        TellRole = OptionRole.GetBool();
        rolename = Optionrolename.GetBool();
        cantaskcount = Optioncantaskcount.GetFloat();
        Votemode = (AbilityVoteMode)OptionVoteMode.GetValue();
        onemeetingmaximum = Option1MeetingMaximum.GetFloat();
        Awakened = !OptAwakening.GetBool() || cantaskcount < 1;
        if (!OptionNonAlignFortuneTeller.GetBool())
        {
            rolename = FortuneTeller.Optionrolename.GetBool();
            TellRole = FortuneTeller.OptionRole.GetBool();
            cantaskcount = FortuneTeller.OptionCanTaskcount.GetFloat();
            Votemode = (AbilityVoteMode)FortuneTeller.OptionVoteMode.GetValue();
            onemeetingmaximum = FortuneTeller.Option1MeetingMaximum.GetFloat();
            Awakened = !FortuneTeller.OptAwakening.GetBool();
            Max = FortuneTeller.OptionMaximum.GetFloat();
        }
        GameTell = new();
    }//fix
    static OptionItem OptionNonAlignFortuneTeller;
    private static OptionItem Optioncollect;
    private static OptionItem OptionMaximum;
    private static OptionItem OptionRole;
    private static OptionItem OptionVoteMode;
    private static OptionItem Optioncantaskcount;
    private static OptionItem Option1MeetingMaximum;
    private static OptionItem Optionrolename;
    private static OptionItem OptionDontChengeGame;
    static OptionItem OptAwakening;
    static OptionItem MeisFT;
    bool Awakened;
    public float collect;
    public float Max;
    public bool TellRole;
    public bool rolename;
    public AbilityVoteMode Votemode;
    int count;
    float cantaskcount;
    float onemeetingmaximum;
    float MeetingUsedcount;
    Dictionary<byte, CustomRoles> Divination = new();
    Dictionary<byte, CustomRoles> GameTell = new();

    enum Option
    {
        TellerCollectRect,
        TellMaximum,
        AbilityVotemode,
        TellerCanSeeRolename,
        TellRole,
        PonkotuTellerFTOption,
        PonkotuTellerMyisFT,
        PonkotuDontChengeGame
    }

    private static void SetupOptionItem()
    {
        Optioncollect = FloatOptionItem.Create(RoleInfo, 10, Option.TellerCollectRect, new(0f, 100f, 2f), 70f, false)
            .SetValueFormat(OptionFormat.Percent);
        OptionNonAlignFortuneTeller = BooleanOptionItem.Create(RoleInfo, 17, Option.PonkotuTellerFTOption, true, false);
        OptionMaximum = IntegerOptionItem.Create(RoleInfo, 11, Option.TellMaximum, new(1, 99, 1), 1, false, OptionNonAlignFortuneTeller)
            .SetValueFormat(OptionFormat.Times);
        OptionVoteMode = StringOptionItem.Create(RoleInfo, 12, Option.AbilityVotemode, EnumHelper.GetAllNames<AbilityVoteMode>(), 1, false, OptionNonAlignFortuneTeller);
        Optionrolename = BooleanOptionItem.Create(RoleInfo, 19, Option.TellerCanSeeRolename, true, false, OptionNonAlignFortuneTeller);
        OptionRole = BooleanOptionItem.Create(RoleInfo, 13, Option.TellRole, true, false, OptionNonAlignFortuneTeller);
        Optioncantaskcount = IntegerOptionItem.Create(RoleInfo, 14, GeneralOption.cantaskcount, new(0, 99, 1), 5, false, OptionNonAlignFortuneTeller);
        Option1MeetingMaximum = IntegerOptionItem.Create(RoleInfo, 15, GeneralOption.MeetingMaxTime, new(0, 99, 1), 0, false, OptionNonAlignFortuneTeller)
            .SetValueFormat(OptionFormat.Times).SetZeroNotation(OptionZeroNotation.Infinity);
        OptAwakening = BooleanOptionItem.Create(RoleInfo, 16, GeneralOption.AbilityAwakening, false, false, OptionNonAlignFortuneTeller);
        MeisFT = BooleanOptionItem.Create(RoleInfo, 18, Option.PonkotuTellerMyisFT, false, false);
        OptionDontChengeGame = BooleanOptionItem.Create(RoleInfo, 20, Option.PonkotuDontChengeGame, false, false);
    }
    private void SendRPC(byte targetId, CustomRoles role)
    {
        using var sender = CreateSender();
        sender.Writer.Write(count);
        sender.Writer.Write(targetId);
        sender.Writer.WritePacked((int)role);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        count = reader.ReadInt32();
        Divination[reader.ReadByte()] = (CustomRoles)reader.ReadPackedInt32();
    }
    public override void OnStartMeeting() => MeetingUsedcount = 0;
    public override string GetProgressText(bool comms = false, bool gamelog = false) => Utils.ColorString(!MyTaskState.HasCompletedEnoughCountOfTasks(cantaskcount) ? Color.gray : Max <= count ? Color.gray : Color.cyan, $"({Max - count})");
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
    bool ISelfVoter.CanUseVoted() => Canuseability() && Max > count && MyTaskState.HasCompletedEnoughCountOfTasks(cantaskcount) && (MeetingUsedcount < onemeetingmaximum || onemeetingmaximum == 0);

    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (!Canuseability()) return true;
        if (Max > count && Is(voter) && MyTaskState.HasCompletedEnoughCountOfTasks((int)cantaskcount) && (MeetingUsedcount < onemeetingmaximum || onemeetingmaximum == 0))
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
        if (GameTell.TryGetValue(votedForId, out var telledrole) && OptionDontChengeGame.GetBool())
        {
            Logger.Info($"Player: {Player.name},Target: {target.name}, count: {count}(再掲)", "PonkotuTeller");
            var lasttext = GetString("Skill.Tellerfin") + (telledrole.IsCrewmate() ? "!" : "...");
            if (MeisFT.GetBool()) Utils.SendMessage(string.Format(GetString("Skill.Teller"), UtilsName.GetPlayerColor(target, true), TellRole ? "<b>" + GetString($"{telledrole}").Color(UtilsRoleText.GetRoleColor(telledrole)) + "</b>" : GetString($"{telledrole.GetCustomRoleTypes()}")) + lasttext + $"\n\n" + (onemeetingmaximum != 0 ? string.Format(GetString("RemainingOneMeetingCount"), Math.Min(onemeetingmaximum - MeetingUsedcount, Max - count)) : string.Format(GetString("RemainingCount"), Max - count) + (Votemode == AbilityVoteMode.SelfVote ? "\n\n" + GetString("VoteSkillFin") : "")), Player.PlayerId);
            else Utils.SendMessage(string.Format(GetString("Skill.Teller"), UtilsName.GetPlayerColor(target, true), TellRole ? "<b>" + GetString($"{telledrole}").Color(UtilsRoleText.GetRoleColor(telledrole)) + "</b>" : GetString($"{telledrole.GetCustomRoleTypes()}")) + $"..?" + $"\n\n" + (onemeetingmaximum != 0 ? string.Format(GetString("RemainingOneMeetingCount"), Math.Min(onemeetingmaximum - MeetingUsedcount, Max - count)) : string.Format(GetString("RemainingCount"), Max - count) + (Votemode == AbilityVoteMode.SelfVote ? "\n\n" + GetString("VoteSkillFin") : "")), Player.PlayerId);
            return;
        }
        count++;
        MeetingUsedcount++;
        if (chance < collect)
        {
            Logger.Info($"Player: {Player.name},Target: {target.name}, count: {count}(成功)", "PonkotuTeller");
            var FtR = target.GetTellResults(Player); //結果を変更するかチェック
            var role = target.GetTellResults(null); //結果を変更するかチェック
            SendRPC(votedForId, role);
            GameTell.TryAdd(votedForId, role);
            var lasttext = GetString("Skill.Tellerfin") + (role.IsCrewmate() ? "!" : "...");
            if (MeisFT.GetBool()) Utils.SendMessage(string.Format(GetString("Skill.Teller"), UtilsName.GetPlayerColor(target, true), TellRole ? "<b>" + GetString($"{role}").Color(UtilsRoleText.GetRoleColor(role)) + "</b>" : GetString($"{role.GetCustomRoleTypes()}")) + lasttext + $"\n\n" + (onemeetingmaximum != 0 ? string.Format(GetString("RemainingOneMeetingCount"), Math.Min(onemeetingmaximum - MeetingUsedcount, Max - count)) : string.Format(GetString("RemainingCount"), Max - count) + (Votemode == AbilityVoteMode.SelfVote ? "\n\n" + GetString("VoteSkillFin") : "")), Player.PlayerId);
            else Utils.SendMessage(string.Format(GetString("Skill.Teller"), UtilsName.GetPlayerColor(target, true), TellRole ? "<b>" + GetString($"{role}").Color(UtilsRoleText.GetRoleColor(role)) + "</b>" : GetString($"{role.GetCustomRoleTypes()}")) + $"..?" + $"\n\n" + (onemeetingmaximum != 0 ? string.Format(GetString("RemainingOneMeetingCount"), Math.Min(onemeetingmaximum - MeetingUsedcount, Max - count)) : string.Format(GetString("RemainingCount"), Max - count) + (Votemode == AbilityVoteMode.SelfVote ? "\n\n" + GetString("VoteSkillFin") : "")), Player.PlayerId);
        }
        else
        {
            var tage = new List<PlayerControl>(PlayerCatch.AllPlayerControls);
            tage.Remove(target);
            var rand = IRandom.Instance;
            var P = tage[rand.Next(0, tage.Count)];
            var role = target.GetTellResults(null); //結果を変更するかチェック

            GameTell.TryAdd(votedForId, role);
            Logger.Info($"Player: {Player.name},Target: {target.name}, count: {count}(失敗)", "PonkotuTeller");
            var lasttext = GetString("Skill.Tellerfin") + (role.IsCrewmate() ? "!" : "...");
            SendRPC(votedForId, role);
            if (MeisFT.GetBool()) Utils.SendMessage(string.Format(GetString("Skill.Teller"), UtilsName.GetPlayerColor(target, true), TellRole ? "<b>" + GetString($"{role}").Color(UtilsRoleText.GetRoleColor(role)) + "</b>" : GetString($"{role.GetCustomRoleTypes()}")) + lasttext + $"\n\n" + (onemeetingmaximum != 0 ? string.Format(GetString("RemainingOneMeetingCount"), Math.Min(onemeetingmaximum - MeetingUsedcount, Max - count)) : string.Format(GetString("RemainingCount"), Max - count) + (Votemode == AbilityVoteMode.SelfVote ? "\n\n" + GetString("VoteSkillFin") : "")), Player.PlayerId);
            else Utils.SendMessage(string.Format(GetString("Skill.Teller"), UtilsName.GetPlayerColor(target, true), TellRole ? "<b>" + GetString($"{role}").Color(UtilsRoleText.GetRoleColor(role)) + "</b>" : GetString($"{role.GetCustomRoleTypes()}")) + $"..?" + $"\n\n" + (onemeetingmaximum != 0 ? string.Format(GetString("RemainingOneMeetingCount"), Math.Min(onemeetingmaximum - MeetingUsedcount, Max - count)) : string.Format(GetString("RemainingCount"), Max - count) + (Votemode == AbilityVoteMode.SelfVote ? "\n\n" + GetString("VoteSkillFin") : "")), Player.PlayerId);
        }
    }
    public override CustomRoles Misidentify() => Awakened ? (MeisFT.GetBool() ? CustomRoles.FortuneTeller : CustomRoles.NotAssigned) : CustomRoles.Crewmate;
    public override bool OnCompleteTask(uint taskid)
    {
        if (MyTaskState.HasCompletedEnoughCountOfTasks((int)cantaskcount))
        {
            if (Awakened == false)
                if (!Utils.RoleSendList.Contains(Player.PlayerId))
                    Utils.RoleSendList.Add(Player.PlayerId);
            Awakened = true;
        }
        return true;
    }
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (Divination.TryGetValue(seen.PlayerId, out var role) && rolename)
        {
            if (TellRole) return $"<color={UtilsRoleText.GetRoleColorCode(role)}>" + GetString(role.ToString());
            else return GetString(role.GetCustomRoleTypes().ToString());
        }
        return "";
    }
}