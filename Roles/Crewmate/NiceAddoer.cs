using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate
{
    public sealed class NiceAddoer : RoleBase
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(NiceAddoer),
                player => new NiceAddoer(player),
                CustomRoles.NiceAddoer,
                () => RoleTypes.Crewmate,
                CustomRoleTypes.Crewmate,
                8600,
                SetupOptionItem,
                "NA",
                "#87cefa",
                (1, 1),
                from: From.TownOfHost_K
            );
        public NiceAddoer(PlayerControl player)
        : base(
            RoleInfo,
            player
        )
        {
        }
        private static void SetupOptionItem()
        {
            RoleAddAddons.Create(RoleInfo, 5, DefaaultOn: true);
        }
    }
}