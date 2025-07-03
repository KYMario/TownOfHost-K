using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;
public sealed class ShapeKiller : RoleBase, IImpostor, ISidekickable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(ShapeKiller),
            player => new ShapeKiller(player),
            CustomRoles.ShapeKiller,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            25300,
            SetUpOptionItem,
            "shk",
            from: From.TownOfHost_Y
        );

    public ShapeKiller(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        canDeadReport = optionCanDeadReport.GetBool();

        shapeTarget = null;
    }

    private static OptionItem optionCanDeadReport;

    enum OptionName
    {
        ShapeKillerCanDeadReport
    }

    private static bool canDeadReport;

    private PlayerControl shapeTarget = null;

    private static void SetUpOptionItem()
    {
        optionCanDeadReport = BooleanOptionItem.Create(RoleInfo, 10, OptionName.ShapeKillerCanDeadReport, true, false);
    }

    public override void OnShapeshift(PlayerControl target)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            return;
        }

        var shapeshifting = !Is(target);
        if (!shapeshifting)
        {
            shapeTarget = null;
        }
        else
        {
            shapeTarget = target;
        }

        Logger.Info($"{Player.GetNameWithRole()}のターゲットを {target?.GetNameWithRole()} に設定", "ShepeKillerTarget");
    }

    public static bool DummyReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (target == null) return false;
        if (reporter == null || !reporter.Is(CustomRoles.ShapeKiller)) return false;
        if (reporter.PlayerId == target.PlayerId) return false;

        var shapeKiller = (ShapeKiller)reporter.GetRoleClass();
        if (shapeKiller.shapeTarget != null && (canDeadReport || shapeKiller.shapeTarget.IsAlive()))
        {
            RPC.ReportDeadBodyForced(shapeKiller.shapeTarget, target);
            Logger.Info($"ShapeKillerの偽装通報 player: {shapeKiller.shapeTarget?.name}, target: {target?.PlayerName}", "ShepeKillerReport");
            return true;
        }

        return false;
    }
}