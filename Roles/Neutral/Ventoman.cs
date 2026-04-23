/*using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Neutral;

public sealed class Ventoman : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Ventoman),
            player => new Ventoman(player),
            CustomRoles.Ventoman,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Neutral,
            38500,
            SetupOptionItem,
            "vma",
            "#FF4500",
from: From.TownOfHost_K
        );
    public Ventoman(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        VentWin = OptionventWin.GetInt();
        Cooldown = OptionCooldown.GetFloat();
    }
    private static OptionItem OptionventWin;
    private static OptionItem OptionCooldown;
    private enum OptionName
    {
        VentWin,
        Cooldown
    }
    private int VentWin;
    private static float Cooldown;
    int Vent = 0;
    private static void SetupOptionItem()
    {
        OptionventWin = IntegerOptionItem.Create(RoleInfo, 11, OptionName.VentWin, new(1, 999, 1), 100, false)
        .SetValueFormat(OptionFormat.Times);
        OptionCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.Cooldown, new(0f, 180f, 0.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = Cooldown;
        AURoleOptions.EngineerInVentMaxTime = 0;
    }
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        Vent++;
        Logger.Info($"{Player.GetRealName()}は{Vent}回ベントに入りました", nameof(Ventoman));
        if (Vent >= VentWin)
        {
            Win();
        }
        return true;
    }
    private void Win()
    {
        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Ventoman);
        CustomWinnerHolder.WinnerIds.Add(Player.PlayerId);
    }
    public override string GetProgressText(bool comms = false, bool gamelog = false)
    => Utils.ColorString(RoleInfo.RoleColor, $"({Vent}/{VentWin})");
}*/
