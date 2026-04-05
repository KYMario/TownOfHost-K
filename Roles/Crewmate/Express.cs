using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;

public sealed class Express : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Express),
            player => new Express(player),
            CustomRoles.Express,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            22300,
            SetupOptionItem,
            "exp",
            "#00ffff",
            (4, 3),
            from: From.TownOfHost_Y
        );

    public Express(PlayerControl player)
        : base(RoleInfo, player)
    {
        speed = OptionSpeed.GetFloat();
    }

    private static OptionItem OptionSpeed;

    enum OptionName
    {
        ExpressSpeed
    }

    private static float speed;

    private static void SetupOptionItem()
    {
        OptionSpeed = FloatOptionItem.Create(RoleInfo, 10, OptionName.ExpressSpeed, new(1.5f, 10f, 0.25f), 3.0f, false)
            .SetValueFormat(OptionFormat.Multiplier);
    }

    // ★ 速度を加算方式に変更
    public override void Add()
    {
        Main.AllPlayerSpeed[Player.PlayerId] += speed;
    }
    public override void ChengeRoleAdd()
    {
        // Express が外れた瞬間に速度を戻す
        Main.AllPlayerSpeed[Player.PlayerId] -= speed;

        base.ChengeRoleAdd();
    }

}
