using System.Collections.Generic;
using AmongUs.GameOptions;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Modules.SelfVoteManager;

namespace TownOfHost.Roles.Crewmate;

public sealed class AmateurTeller : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(AmateurTeller),
            player => new AmateurTeller(player),
            CustomRoles.AmateurTeller,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            18100,
            SetupOptionItem,
            "AT",
            "#6b3ec3"
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
        kakusei = !Kakusei.GetBool() || OptionCanTaskcount.GetInt() < 1;
        UseTarget = byte.MaxValue;
        Votemode = (VoteMode)OptionVoteMode.GetValue();
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
    static OptionItem Kakusei;
    static OptionItem TargetCanseeArrow;
    static OptionItem TargetCanseePlayer;
    static OptionItem AbilityUseTurnCanButton;
    public VoteMode Votemode;
    static bool canusebutton;
    static bool canseerole;
    static int maximum;
    static int cantaskcount;
    static bool targetcanseearrow;
    static bool targetcanseeplayer;
    int count;
    bool kakusei;
    bool Use;
    byte UseTarget;
    List<byte> Targets = new();
    Dictionary<byte, CustomRoles> Divination = new();
    static HashSet<AmateurTeller> tellers = new();

    enum Option
    {
        Ucount,
        Votemode,
        tRole,
        AmateurTellerTargetCanseeArrow,
        AmateurTellerCanUseAbilityTurnButton,
        AmateurTellerTargetCanseePlayer
    }
    public enum VoteMode
    {
        uvote,
        SelfVote,
    }

    public override void Add()
    {
        AddS(Player);
        tellers.Add(this);
    }
    public override void OnDestroy()
    {
        tellers.Clear();
    }
    private static void SetupOptionItem()
    {
        OptionMaximum = FloatOptionItem.Create(RoleInfo, 10, Option.Ucount, new(1f, 99f, 1f), 1f, false)
            .SetValueFormat(OptionFormat.Times);
        OptionVoteMode = StringOptionItem.Create(RoleInfo, 11, Option.Votemode, EnumHelper.GetAllNames<VoteMode>(), 1, false);
        OptionRole = BooleanOptionItem.Create(RoleInfo, 12, Option.tRole, true, false);
        TargetCanseePlayer = BooleanOptionItem.Create(RoleInfo, 13, Option.AmateurTellerTargetCanseePlayer, true, false);
        TargetCanseeArrow = BooleanOptionItem.Create(RoleInfo, 14, Option.AmateurTellerTargetCanseeArrow, true, false, TargetCanseePlayer);
        AbilityUseTurnCanButton = BooleanOptionItem.Create(RoleInfo, 15, Option.AmateurTellerCanUseAbilityTurnButton, true, false);
        OptionCanTaskcount = FloatOptionItem.Create(RoleInfo, 16, GeneralOption.cantaskcount, new(0, 99, 1), 5, false);
        Kakusei = BooleanOptionItem.Create(RoleInfo, 17, GeneralOption.UKakusei, true, false);
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
    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (!Canuseability()) return true;
        if (maximum > count && Is(voter) && MyTaskState.HasCompletedEnoughCountOfTasks(cantaskcount) && (!Use))
        {
            var target = PlayerCatch.GetPlayerById(votedForId);
            if (Votemode == VoteMode.uvote)
            {
                if (Player.PlayerId == votedForId || votedForId == SkipId) return true;
                Uranai(votedForId);
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
                        Uranai(votedForId);
                    SetMode(Player, status is VoteStatus.Self);
                    return false;
                }
            }
        }
        return true;
    }
    public void Uranai(byte votedForId)
    {
        var target = PlayerCatch.GetPlayerById(votedForId);
        if (!target.IsAlive()) return;
        count++;
        Use = true;
        UseTarget = target.PlayerId;
        TargetArrow.Add(target.PlayerId, Player.PlayerId);
        Utils.SendMessage(Utils.GetPlayerColor(target.PlayerId) + GetString("AmatruertellerTellMeg"), Player.PlayerId);
    }
    public override CustomRoles Jikaku() => kakusei ? CustomRoles.NotAssigned : CustomRoles.Crewmate;
    public override bool OnCompleteTask(uint taskid)
    {
        if (MyTaskState.HasCompletedEnoughCountOfTasks(cantaskcount))
        {
            if (kakusei == false)
                if (!Utils.RoleSendList.Contains(Player.PlayerId))
                    Utils.RoleSendList.Add(Player.PlayerId);
            kakusei = true;
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
        if (isForMeeting && Player.IsAlive() && kakusei && seer.PlayerId == seen.PlayerId && Canuseability() && maximum > count && MyTaskState.HasCompletedEnoughCountOfTasks(cantaskcount))
        {
            var mes = $"<color={RoleInfo.RoleColorCode}>{(Votemode == VoteMode.SelfVote ? GetString("SelfVoteRoleInfoMeg") : GetString("NomalVoteRoleInfoMeg"))}</color>";
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