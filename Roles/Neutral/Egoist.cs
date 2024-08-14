using System.Linq;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Neutral;
public sealed class Egoist : RoleBase, ISidekickable, ILNKiller, ISchrodingerCatOwner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Egoist),
            player => new Egoist(player),
            CustomRoles.Egoist,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Neutral,
            50600,
            SetupOptionItem,
            "eg",
            "#5600ff",
            canMakeMadmate: () => OptionCanCreateSideKick.GetBool(),
            countType: CountTypes.Impostor,
            assignInfo: new RoleAssignInfo(CustomRoles.Egoist, CustomRoleTypes.Neutral)
            {
                AssignRoleType = CustomRoleTypes.Impostor,
                IsInitiallyAssignableCallBack =
                    () => Main.RealOptionsData.GetInt(Int32OptionNames.NumImpostors) > 1,
                AssignCountRule = new(1, 1, 1)
            },
            from: From.TownOfHost
        );
    public Egoist(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCooldown = OptionKillCooldown.GetFloat();
        CanCreateSideKick = OptionCanCreateSideKick.GetBool();
    }

    static OptionItem OptionKillCooldown;
    static OptionItem OptionCanCreateSideKick;
    static OptionItem OptionNameColor;

    private static float KillCooldown;
    public static bool CanCreateSideKick;
    private static PlayerControl egoist;
    enum Op { EgoistNameColor }

    public SchrodingerCat.TeamType SchrodingerCatChangeTo => SchrodingerCat.TeamType.Egoist;

    private static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionCanCreateSideKick = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.CanCreateSideKick, false, false);
        OptionNameColor = BooleanOptionItem.Create(RoleInfo, 12, Op.EgoistNameColor, false, false);
        RoleAddAddons.Create(RoleInfo, 13);
    }
    public override void Add()
    {
        foreach (var impostor in Main.AllPlayerControls.Where(pc => pc.Is(CustomRoleTypes.Impostor)))
        {
            if (OptionNameColor.GetBool()) NameColorManager.Add(impostor.PlayerId, Player.PlayerId);
            else NameColorManager.Add(impostor.PlayerId, Player.PlayerId, "#ff1919");
        }
        egoist = Player;
    }
    public override void OnDestroy()
    {
        egoist = null;
    }
    public float CalculateKillCooldown() => KillCooldown;
    public bool CanUseSabotageButton() => true;
    public static bool CheckWin()
    {
        if (Main.AllAlivePlayerControls.All(p => !p.Is(RoleTypes.Impostor)) &&
            egoist.IsAlive()) //インポスター全滅でエゴイストが生存
        {
            Win();
            return true;
        }

        return false;
    }
    private static void Win()
    {
        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Egoist);
        CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Egoist);
    }
    public bool CanMakeSidekick() => CanCreateSideKick;
    public void ApplySchrodingerCatOptions(IGameOptions option)
    {
        option.SetVision(true);
    }
}