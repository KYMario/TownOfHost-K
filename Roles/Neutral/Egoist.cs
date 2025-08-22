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
            13700,
            SetupOptionItem,
            "eg",
            "#5600ff",
            (2, 4),
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

    static Egoist __egoist;
    static OptionItem OptionKillCooldown;
    static OptionItem OptionCanCreateSideKick;
    static OptionItem OptionNameColor;

    private static float KillCooldown;
    public static bool CanCreateSideKick;
    private static PlayerControl egoist;
    enum Op { EgoistNameColor }

    public ISchrodingerCatOwner.TeamType SchrodingerCatChangeTo => ISchrodingerCatOwner.TeamType.Egoist;

    private static void SetupOptionItem()
    {
        SoloWinOption.Create(RoleInfo, 9, defo: 1);
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 0.5f), 25f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionCanCreateSideKick = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.CanCreateSideKick, false, false);
        OptionNameColor = BooleanOptionItem.Create(RoleInfo, 12, Op.EgoistNameColor, false, false);
        RoleAddAddons.Create(RoleInfo, 13);
    }
    public override void Add()
    {
        foreach (var impostor in PlayerCatch.AllPlayerControls.Where(pc => pc.Is(CustomRoleTypes.Impostor)))
        {
            if (impostor.Is(CustomRoles.Amnesiac)) continue;
            if (OptionNameColor.GetBool()) NameColorManager.Add(impostor.PlayerId, Player.PlayerId);
            else NameColorManager.Add(impostor.PlayerId, Player.PlayerId, "#ff1919");
        }
        __egoist = this;
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
        if (PlayerCatch.AllAlivePlayerControls.All(p => !p.Is(CustomRoleTypes.Impostor)) && egoist.IsAlive()) //インポスター全滅でエゴイストが生存
        {
            Win();
            return true;
        }

        return false;
    }
    private static void Win()
    {
        if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Egoist, byte.MaxValue, true))
        {
            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Egoist);
            CustomWinnerHolder.NeutralWinnerIds.Add(__egoist.Player.PlayerId);
        }
    }
    public bool CanMakeSidekick() => CanCreateSideKick;
    public void ApplySchrodingerCatOptions(IGameOptions option)
    {
        option.SetVision(true);
    }
}