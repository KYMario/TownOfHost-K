using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class Guarding
    {
        private static readonly int Id = 16800;
        private static Color RoleColor = UtilsRoleText.GetRoleColor(CustomRoles.Guarding);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "ζ");
        public static List<byte> playerIdList = new();
        private static OptionItem OptionAddGuard;
        public static int HaveGuard;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Guarding, fromtext: UtilsOption.GetFrom(From.TownOfHost_Y));
            AddOnsAssignData.Create(Id + 10, CustomRoles.Guarding, true, true, true, true);
            ObjectOptionitem.Create(Id + 51, "AddonOption", true, "", TabGroup.Addons).SetOptionName(() => "Role Option").SetSubRoleOptionItem(CustomRoles.Guarding);
            OptionAddGuard = IntegerOptionItem.Create(Id + 50, "AddGuardCount", new(1, 10, 1), 1, TabGroup.Addons, false).SetSubRoleOptionItem(CustomRoles.Guarding);
        }
        public static void Init()
        {
            playerIdList = new();
            HaveGuard = OptionAddGuard.GetInt();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
    }
}