using AmongUs.GameOptions;
using Hazel;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using UnityEngine;

namespace TownOfHost.Roles.Madmate;

public sealed class MadBetrayer : RoleBase, IKiller, ISchrodingerCatOwner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(MadBetrayer),
            player => new MadBetrayer(player),
            CustomRoles.MadBetrayer,
            () => OptionCanVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
            CustomRoleTypes.Madmate,
            22800,
            SetupOptionItem,
            "MBet",
            "#8b2551",
            (4, 2),
            true,
            introSound: () => GetIntroSound(RoleTypes.Shapeshifter),
            assignInfo: new RoleAssignInfo(CustomRoles.MadBetrayer, CustomRoleTypes.Madmate)
            {
                AssignCountRule = new(1, 1, 1)
            }
        );
    public MadBetrayer(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.ForRecompute
    )
    {
        KillCooldown = OptionKillCoolDown.GetFloat();
        ImpostorRevealTaskCount = OptionImpostorRevealTaskCount.GetInt();
        CanVent = OptionCanVent.GetBool();
        CanUseSabotage = OptionCanUseSabotage.GetBool();
        HasImpostorVision = OptionHasImpostorVision.GetBool();

        IsBetray = false;
        IsImpostorReveal = false;
    }
    static bool IsBetray;
    bool IsImpostorReveal;

    static OptionItem OptionKillCoolDown; static float KillCooldown;
    static OptionItem OptionImpostorRevealTaskCount; static int ImpostorRevealTaskCount;
    static OptionItem OptionCanVent; static bool CanVent;
    static OptionItem OptionCanUseSabotage; static bool CanUseSabotage;
    static OptionItem OptionHasImpostorVision; static bool HasImpostorVision;
    enum OptionName
    {
        MadBetrayerImpostorRevealTaskcount,
    }

    static void SetupOptionItem()
    {
        SoloWinOption.Create(RoleInfo, 9, defo: 2);
        OptionKillCoolDown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, OptionBaseCoolTime, 20f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionImpostorRevealTaskCount = IntegerOptionItem.Create(RoleInfo, 11, OptionName.MadBetrayerImpostorRevealTaskcount, new(0, 99, 1), 5, false)
                .SetZeroNotation(OptionZeroNotation.Off);
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.CanVent, true, false);
        OptionCanUseSabotage = BooleanOptionItem.Create(RoleInfo, 13, GeneralOption.CanUseSabotage, false, false);
        OptionHasImpostorVision = BooleanOptionItem.Create(RoleInfo, 14, GeneralOption.ImpostorVision, true, false);
        OverrideTasksData.Create(RoleInfo, 15);
        RoleAddAddons.Create(RoleInfo, 40, MadMate: true);
    }
    public ISchrodingerCatOwner.TeamType SchrodingerCatChangeTo => ISchrodingerCatOwner.TeamType.Betrayer;

    public override void ApplyGameOptions(IGameOptions opt)
    {
        if (IsBetray)
        {
            opt.SetVision(HasImpostorVision);
        }
        else
            opt.SetVision(false);
    }
    public static void CheckCount(ref int crew, ref int betrayer)
    {
        if (IsBetray)
        {
            betrayer++;
        }
        else
        {
            crew++;
        }
    }
    public static bool IsMadmate() => IsBetray is false;

    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        if (IsImpostorReveal is false) return "";
        seen ??= seer;
        var role = seen.GetCustomRole();
        if (role.GetCustomRoleTypes() is CustomRoleTypes.Impostor || role is CustomRoles.WolfBoy or CustomRoles.Egoist)
            return "<#ff1919>★</color>";
        return "";
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (Is(seen) is false || isForMeeting) return "";
        if (IsTaskFinished && !IsBetray)
            return $"<{RoleInfo.RoleColorCode}>{GetString("MadBetrayerLowerText")}</color>";
        return "";
    }
    public override void OverrideTrueRoleName(ref Color roleColor, ref string roleText)
    {
        if (IsBetray) roleText = GetString("Betrayer");
    }
    public override string GetProgressText(bool comms = false, bool GameLog = false) => IsBetray ? $"<{RoleInfo.RoleColorCode}>★</color>" : "";

    public override RoleTypes? AfterMeetingRole => IsTaskFinished || IsBetray ? RoleTypes.Impostor : RoleTypes.Engineer;
    bool IKiller.CanUseImpostorVentButton() => CanVent && IsTaskFinished;
    public override bool CanUseAbilityButton() => CanVent;
    bool IKiller.CanUseSabotageButton() => IsBetray && CanUseSabotage;
    float IKiller.CalculateKillCooldown() => KillCooldown;
    bool IKiller.CanUseKillButton() => IsTaskFinished || IsBetray;

    public override bool OnCompleteTask(uint taskid)
    {
        if (IsImpostorReveal is false && MyTaskState.HasCompletedEnoughCountOfTasks(ImpostorRevealTaskCount) && ImpostorRevealTaskCount is not 0)
        {
            IsImpostorReveal = true;
            _ = new LateTask(() => UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player), 0.2f, "SetImpostorKillCool", true);
        }
        if (IsTaskFinished)
        {
            HasAbility = false;
            if (!AmongUsClient.Instance.AmHost) return true;
            Player.RpcSetRoleDesync(RoleTypes.Impostor, Player.GetClientId());
            _ = new LateTask(() => Player.SetKillCooldown(force: true), 0.2f, "SetImpostorKillCool", true);
        }
        return true;
    }
    void IKiller.OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AppearanceTuple;
        if (IsBetray is true)
        {
            return;
        }

        if (IsTaskFinished is false || (IsBetray is false && target.GetCustomRole().IsImpostor() is false))
        {
            info.DoKill = false;
            return;
        }
        if (target.GetCustomRole().IsImpostor() && IsBetray is false)
        {
            info.KillPower = 3;
            IsBetray = true;
            SendRPC();
            UtilsGameLog.AddGameLog("MadBetrayer", GetString("MadBetrayerLog"));

            _ = new LateTask(() => Player.SetKillCooldown(force: true), 0.2f, "SetImpostorKillCool", true);
        }
    }

    public void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(IsBetray);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        IsBetray = reader.ReadBoolean();
    }
}
