using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Madmate;

public sealed class MadWorker : RoleBase, IKillFlashSeeable, IDeathReasonSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(MadWorker),
            player => new MadWorker(player),
            CustomRoles.MadWorker,
            () => OptionCanVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
            CustomRoleTypes.Madmate,
            8400,
            SetupOptionItem,
            "mw",
            OptionSort: (4, 3)
        );
    public MadWorker(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => (CannotWinAtDeath && player.Data.IsDead) ? HasTask.False : HasTask.ForRecompute
    )
    {
        canSeeKillFlash = Options.MadmateCanSeeKillFlash.GetBool();
        canSeeDeathReason = Options.MadmateCanSeeDeathReason.GetBool();

        ventCooldown = OptionVentCooldown.GetFloat();
        CannotWinAtDeath = true;
    }
    private static OptionItem OptionCanVent;
    private static OptionItem OptionVentCooldown;
    enum OptionName
    {
        VentCooldown
    }
    private static bool canSeeKillFlash;
    private static bool canSeeDeathReason;
    private static bool CannotWinAtDeath;
    private static float ventCooldown;
    private static void SetupOptionItem()
    {
        SoloWinOption.Create(RoleInfo, 9);
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 10, GeneralOption.CanVent, false, false);
        OptionVentCooldown = FloatOptionItem.Create(RoleInfo, 12, OptionName.VentCooldown, new(0f, 180f, 0.5f), 0f, false, OptionCanVent)
                .SetValueFormat(OptionFormat.Seconds);
        OverrideTasksData.Create(RoleInfo, 20);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = ventCooldown;
        AURoleOptions.EngineerInVentMaxTime = 0;
    }
    public override bool OnCompleteTask(uint taskid)
    {
        if (IsTaskFinished && !(CannotWinAtDeath && !Player.IsAlive()))
        {
            CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Impostor, Player.PlayerId, hantrole: CustomRoles.MadWorker);
        }
        return true;
    }
    public bool? CheckKillFlash(MurderInfo info) => canSeeKillFlash;
    public bool? CheckSeeDeathReason(PlayerControl seen) => canSeeDeathReason;
    public override CustomRoles TellResults(PlayerControl player) => Options.MadTellOpt();
}
