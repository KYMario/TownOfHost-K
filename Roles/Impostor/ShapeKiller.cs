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
            22500,
            SetUpOptionItem,
            "shk",
            OptionSort: (6, 5),
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
        l1reprter = byte.MaxValue;
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

    public static void SetDummyReport(ref PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (target == null) return;
        if (reporter == null || !reporter.Is(CustomRoles.ShapeKiller)) return;
        if (reporter.PlayerId == target.PlayerId) return;

        var shapeKiller = (ShapeKiller)reporter.GetRoleClass();
        if (shapeKiller.shapeTarget != null && (canDeadReport || shapeKiller.shapeTarget.IsAlive()))
        {
            // 通報者書き換え
            reporter = shapeKiller.shapeTarget;
            shapeKiller.l1reprter = reporter.PlayerId;
            if (target.PlayerId.GetPlayerState().MainRole is CustomRoles.Bait or CustomRoles.Insider)
                Achievements.RpcCompleteAchievement(shapeKiller.Player.PlayerId, 0, achievements[0]);
            Logger.Info($"ShapeKillerの偽装通報 player: {shapeKiller.shapeTarget?.name}, target: {target?.PlayerName}", "ShepeKillerReport");
        }
    }
    public override void OnExileWrapUp(NetworkedPlayerInfo exiled, ref bool DecidedWinner)
    {
        if ((exiled?.PlayerId ?? byte.MaxValue) == l1reprter)
            Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[1]);
    }
    byte l1reprter;
    public static System.Collections.Generic.Dictionary<int, Achievement> achievements = new();
    [Attributes.PluginModuleInitializer]
    public static void Load()
    {
        var n1 = new Achievement(RoleInfo, 0, 1, 0, 0);
        var l1 = new Achievement(RoleInfo, 1, 1, 0, 1);
        achievements.Add(0, n1);
        achievements.Add(1, l1);
    }
}