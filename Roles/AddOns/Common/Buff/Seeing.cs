using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class Seeing
    {
        private static readonly int Id = 17700;
        private static Color RoleColor = UtilsRoleText.GetRoleColor(CustomRoles.Seeing);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "☯");
        public static List<byte> playerIdList = new();
        public static OptionItem OptionCanSeeActiveComms;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Seeing, fromtext: UtilsOption.GetFrom(From.TownOfHost_Y));
            AddOnsAssignData.Create(Id + 10, CustomRoles.Seeing, true, true, true, true);
            ObjectOptionitem.Create(Id + 51, "AddonOption", true, "", TabGroup.Addons).SetOptionName(() => "Role Option").SetSubRoleOptionItem(CustomRoles.Seeing);
            OptionCanSeeActiveComms = BooleanOptionItem.Create(Id + 50, "CanUseActiveComms", true, TabGroup.Addons, false).SetSubRoleOptionItem(CustomRoles.Seeing);
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