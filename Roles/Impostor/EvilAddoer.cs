using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor
{
    public sealed class EvilAddoer : RoleBase, IImpostor
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(EvilAddoer),
                player => new EvilAddoer(player),
                CustomRoles.EvilAddoer,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Impostor,
                3500,
                SetupOptionItem,
                "EA",
                OptionSort: (2, 4),
                from: From.TownOfHost_K
            );
        public EvilAddoer(PlayerControl player)
        : base(
            RoleInfo,
            player
        )
        {
        }
        static OptionItem KillCoolDown;
        private static void SetupOptionItem()
        {
            KillCoolDown = FloatOptionItem.Create(RoleInfo, 3, GeneralOption.KillCooldown, new(0, 180, 0.5f), 30f, false).SetValueFormat(OptionFormat.Seconds);
            OverrideKilldistance.Create(RoleInfo, 4);
            RoleAddAddons.Create(RoleInfo, 5, DefaaultOn: true);
        }
        public float CalculateKillCooldown() => KillCoolDown.GetFloat();
    }
}