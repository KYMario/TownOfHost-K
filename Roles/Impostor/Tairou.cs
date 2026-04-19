using AmongUs.GameOptions;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor
{
    public sealed class Tairou : RoleBase, IImpostor
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Tairou),
                player => new Tairou(player),
                CustomRoles.Tairou,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Impostor,
                6000,
                SetupOptionItem,
                "t",
                OptionSort: (6, 10)
            );
        public Tairou(PlayerControl player)
            : base(
                RoleInfo,
                player
            )
        {
            TairoDeathReason = OptionTairoDeathReason.GetBool();
            TairouNotify = OptionTairouNotify.GetBool();
        }
        public static OptionItem OptionTairoDeathReason;
        public static OptionItem OptionTairouNotify;
        enum OptionName
        {
            TairoDeathReason,
            TairouNotify
        }
        public static bool TairoDeathReason;
        public static bool TairouNotify;
        private static void SetupOptionItem()
        {
            OptionTairoDeathReason = BooleanOptionItem.Create(RoleInfo, 10, OptionName.TairoDeathReason, true, false);
            OptionTairouNotify = BooleanOptionItem.Create(RoleInfo, 11, OptionName.TairouNotify, true, false);
        }
        public override CustomRoles TellResults(PlayerControl player)
        {
            if (player is not null) Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[0]);
            return CustomRoles.Crewmate;
        }
        public override bool OnCheckMurderAsTarget(MurderInfo info)
        {
            if (info.AttemptKiller.GetCustomRole() is CustomRoles.Sheriff or CustomRoles.SwitchSheriff or CustomRoles.WolfBoy)
                Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[1]);
            return true;
        }
        public override string MeetingAddMessage()
        {
            if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return "";
            if (Player.IsAlive() && TairouNotify)
            {
                string TairouTitle = $"<size=90%><color=#ff0000>{GetString("Message.TairouTitle")}</size></color>";
                return TairouTitle + "\n<size=70%>" + GetString("Message.TairouAlive") + "</size>\n";
            }
            return "";
        }
        public static System.Collections.Generic.Dictionary<int, Achievement> achievements = new();
        [Attributes.PluginModuleInitializer]
        public static void Load()
        {
            var n1 = new Achievement(RoleInfo, 0, 1, 0, 0);
            var n2 = new Achievement(RoleInfo, 1, 1, 0, 0);
            achievements.Add(0, n1);
            achievements.Add(1, n2);
        }
    }
}