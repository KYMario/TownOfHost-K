using System.Collections.Generic;
using AmongUs.GameOptions;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Modules.SelfVoteManager;
using Hazel;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Crewmate;

public sealed class AmateurTeller : RoleBase, ISelfVoter
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(AmateurTeller),
            player => new AmateurTeller(player),
            CustomRoles.AmateurTeller,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            9400,
            SetupOptionItem,
            "AT",
            "#6b3ec3",
            (3, 2)
        );
    public AmateurTeller(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Targets.Clear();
        Divination.Clear();
        count = 0;
        Use = false;
        Awakened = !OptAwakening.GetBool() || OptionCanTaskcount.GetInt() < 1;
        UseTarget = byte.MaxValue;
        Votemode = (AbilityVoteMode)OptionVoteMode.GetValue();
        CustomRoleManager.MarkOthers.Add(OtherArrow);
        maximum = OptionMaximum.GetInt();
        cantaskcount = OptionCanTaskcount.GetInt();
        targetcanseearrow = TargetCanseeArrow.GetBool();
        targetcanseeplayer = TargetCanseePlayer.GetBool();
        canseerole = OptionRole.GetBool();
        canusebutton = AbilityUseTurnCanButton.GetBool();
    }

    static OptionItem OptionMaximum;
    static OptionItem OptionVoteMode;
    static OptionItem OptionRole;
    static OptionItem OptionCanTaskcount;
    static OptionItem OptAwakening;
    static OptionItem TargetCanseeArrow;
    static OptionItem TargetCanseePlayer;
    static OptionItem AbilityUseTurnCanButton;
    public AbilityVoteMode Votemode;
    static bool canusebutton;
    static bool canseerole;
    static int maximum;
    static int cantaskcount;
    static bool targetcanseearrow;
    static bool targetcanseeplayer;
    int count;
    bool Awakened;
    bool Use;
    byte UseTarget;
    List<byte> Targets = new();
    Dictionary<byte, CustomRoles> Divination = new();
    static HashSet<AmateurTeller> tellers = new();

    enum Option
    {
        TellMaximum,
        AbilityVotemode,
        TellRole,
        AmateurTellerTargetCanseeArrow,
        AmateurTellerCanUseAbilityTurnButton,
        AmateurTellerTargetCanseePlayer
    }

    public override void Add()
    {
        tellers.Add(this);
    }
    public override void OnDestroy()
    {
        tellers.Clear();
    }
    private static void SetupOptionItem()
    {
        OptionMaximum = IntegerOptionItem.Create(RoleInfo, 10, Option.TellMaximum, new(1, 99, 1), 1, false)
            .SetValueFormat(OptionFormat.Times);
        OptionVoteMode = StringOptionItem.Create(RoleInfo, 11, Option.AbilityVotemode, EnumHelper.GetAllNames<AbilityVoteMode>(), 1, false);
        OptionRole = BooleanOptionItem.Create(RoleInfo, 12, Option.TellRole, true, false);
        TargetCanseePlayer = BooleanOptionItem.Create(RoleInfo, 13, Option.AmateurTellerTargetCanseePlayer, true, false);
        TargetCanseeArrow = BooleanOptionItem.Create(RoleInfo, 14, Option.AmateurTellerTargetCanseeArrow, true, false, TargetCanseePlayer);
        AbilityUseTurnCanButton = BooleanOptionItem.Create(RoleInfo, 15, Option.AmateurTellerCanUseAbilityTurnButton, true, false);
        OptionCanTaskcount = IntegerOptionItem.Create(RoleInfo, 16, GeneralOption.cantaskcount, new(0, 99, 1), 5, false);
        OptAwakening = BooleanOptionItem.Create(RoleInfo, 17, GeneralOption.AbilityAwakening, false, false);
    }
    public override bool NotifyRolesCheckOtherName => true;
    public override string GetProgressText(bool comms = false, bool gamelog = false) => Utils.ColorString(!MyTaskState.HasCompletedEnoughCountOfTasks(cantaskcount) ? Color.gray : maximum <= count ? Color.gray : Color.cyan, $"({maximum - count})");
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        Use = false;
        TargetArrow.Remove(UseTarget, Player.PlayerId);
        Targets.Add(UseTarget);
        UseTarget = byte.MaxValue;
    }
    public override bool CancelReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target, ref DontReportreson reportreson)
    {
        if (UseTarget != byte.MaxValue && reporter.PlayerId == Player.PlayerId && target == null && !canusebutton)
        {
            reportreson = DontReportreson.CantUseButton;
            return true;
        }
        return false;
    }
    bool ISelfVoter.CanUseVoted() => Canuseability() && maximum > count && MyTaskState.HasCompletedEnoughCountOfTasks(cantaskcount) && (!Use);
    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (!Canuseability()) return true;
        if (maximum > count && Is(voter) && MyTaskState.HasCompletedEnoughCountOfTasks(cantaskcount) && (!Use))
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
        if (!target.IsAlive()) return;
        count++;
        Use = true;
        UseTarget = target.PlayerId;
        TargetArrow.Add(target.PlayerId, Player.PlayerId);
        Utils.SendMessage(UtilsName.GetPlayerColor(target.PlayerId) + GetString("AmatruertellerTellMeg"), Player.PlayerId);
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
    public static string OtherArrow(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        seen ??= seer;
        if (isForMeeting) return "";
        if (!targetcanseeplayer) return "";

        foreach (var tell in tellers)
        {
            if (seer.PlayerId == tell.UseTarget && seer == seen)
            {
                var ar = "";
                if (seer.GetCustomRole().GetCustomRoleTypes() is not CustomRoleTypes.Crewmate)
                {
                    if (targetcanseearrow) ar = $"\n{TargetArrow.GetArrows(seer, tell.Player.PlayerId)}";
                    return $"<color=#6b3ec3>★{ar}</color>";
                }
            }
            else if (seer.PlayerId == tell.UseTarget && seen == tell.Player)
                return "<color=#6b3ec3>★</color>";
        }
        return "";
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (isForMeeting && Player.IsAlive() && Awakened && seer.PlayerId == seen.PlayerId && Canuseability() && maximum > count && MyTaskState.HasCompletedEnoughCountOfTasks(cantaskcount))
        {
            var mes = $"<color={RoleInfo.RoleColorCode}>{(Votemode == AbilityVoteMode.SelfVote ? GetString("SelfVoteRoleInfoMeg") : GetString("NomalVoteRoleInfoMeg"))}</color>";
            return isForHud ? mes : $"<size=40%>{mes}</size>";
        }
        return "";
    }
    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, ref bool enabled, ref Color roleColor, ref string roleText, ref bool addon)
    {
        if (!Player.IsAlive()) return;
        if (UseTarget == seen.PlayerId) return;
        if (Targets.Contains(seen.PlayerId))
        {
            addon = false;
            if (!canseerole)
            {
                enabled = true;
                switch (seen.GetCustomRole().GetCustomRoleTypes())
                {
                    case CustomRoleTypes.Crewmate:
                    case CustomRoleTypes.Madmate:
                        roleColor = Palette.CrewmateBlue;
                        roleText = GetString("Crewmate");
                        break;
                    case CustomRoleTypes.Impostor:
                        roleColor = ModColors.ImpostorRed;
                        roleText = GetString("Impostor");
                        break;
                    case CustomRoleTypes.Neutral:
                        roleColor = ModColors.NeutralGray;
                        roleText = GetString("Neutral");
                        break;
                }
            }
            else
            {
                enabled = true;
            }
        }
    }
}