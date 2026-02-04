using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class MagicHand
    {
        private static readonly int Id = 17100;
        private static Color RoleColor = UtilsRoleText.GetRoleColor(CustomRoles.MagicHand);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "ж");
        public static List<byte> playerIdList = new();
        public static OptionItem KillDistance;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.MagicHand);
            AddOnsAssignDataOnlyKiller.Create(Id + 10, CustomRoles.MagicHand, true, true, true, true);
            ObjectOptionitem.Create(Id + 8, "AddonOption", true, "", TabGroup.Addons).SetOptionName(() => "Role Option").SetSubRoleOptionItem(CustomRoles.MagicHand);
            KillDistance = StringOptionItem.Create(Id + 9, "KillRenge", EnumHelper.GetAllNames<OverrideKilldistance.KillDistance>(), 0, TabGroup.Addons, false).SetSubRoleOptionItem(CustomRoles.MagicHand);
        }
        public static void Init()
        {
            playerIdList = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
    }
}