using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Madmate;

public sealed class Madmate : RoleBase, IKillFlashSeeable, IDeathReasonSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Madmate),
            player => new Madmate(player),
            CustomRoles.Madmate,
            () => OptionCanVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
            CustomRoleTypes.Madmate,
            7400,
            SetupOptionItem,
            "mm",
            OptionSort: (1, 0),
            introSound: () => GetIntroSound(RoleTypes.Impostor),
                assignInfo: new RoleAssignInfo(CustomRoles.Madmate, CustomRoleTypes.Madmate)
                {
                    AssignCountRule = new(0, 15, 1)
                },
            from: From.au_libhalt_net
        );
    public Madmate(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        canSeeKillFlash = Options.MadmateCanSeeKillFlash.GetBool();
        canSeeDeathReason = Options.MadmateCanSeeDeathReason.GetBool();
    }
    private static OptionItem OptionCanVent;
    private static bool canSeeKillFlash;
    private static bool canSeeDeathReason;
    public static void SetupOptionItem()
    {
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 10, GeneralOption.CanVent, false, false);
        RoleAddAddons.Create(RoleInfo, 20);
    }
    public bool? CheckKillFlash(MurderInfo info) => canSeeKillFlash;
    public bool? CheckSeeDeathReason(PlayerControl seen) => canSeeDeathReason;
    public override CustomRoles TellResults(PlayerControl player) => Options.MadTellOpt();
}
