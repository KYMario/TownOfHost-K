using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Madmate;

public sealed class MadTracker : RoleBase, IKillFlashSeeable, IDeathReasonSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(MadTracker),
            player => new MadTracker(player),
            CustomRoles.MadTracker,
            () => RoleTypes.Tracker,
            CustomRoleTypes.Madmate,
            7800,
            SetupOptionItem,
            "mt",
            OptionSort: (2, 2),
            introSound: () => GetIntroSound(RoleTypes.Impostor),
            from: From.TownOfHost_K
        );
    public MadTracker(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        canSeeKillFlash = Options.MadmateCanSeeKillFlash.GetBool();
        canSeeDeathReason = Options.MadmateCanSeeDeathReason.GetBool();
    }
    private static bool canSeeKillFlash;
    private static bool canSeeDeathReason;
    public static OptionItem TrackerCooldown;
    public static OptionItem TrackerDelay;
    public static OptionItem TrackerDuration;
    public static void SetupOptionItem()
    {
        TrackerCooldown = FloatOptionItem.Create(RoleInfo, 3, "TrackerCooldown", new(0f, 180f, 0.5f), 15f, false)
        .SetValueFormat(OptionFormat.Seconds);
        TrackerDelay = FloatOptionItem.Create(RoleInfo, 4, "TrackerDelay", new(0f, 180f, 0.5f), 5f, false)
                .SetValueFormat(OptionFormat.Seconds);
        TrackerDuration = FloatOptionItem.Create(RoleInfo, 5, "TrackerDuration", new(0f, 180f, 1f), 5f, false)
                .SetValueFormat(OptionFormat.Seconds).SetZeroNotation(OptionZeroNotation.Infinity);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.TrackerCooldown = TrackerCooldown.GetFloat();
        AURoleOptions.TrackerDelay = TrackerDelay.GetFloat();
        AURoleOptions.TrackerDuration = TrackerDuration.GetFloat();
    }
    public bool? CheckKillFlash(MurderInfo info) => canSeeKillFlash;
    public bool? CheckSeeDeathReason(PlayerControl seen) => canSeeDeathReason;
    public override CustomRoles TellResults(PlayerControl player) => Options.MadTellOpt();
}
