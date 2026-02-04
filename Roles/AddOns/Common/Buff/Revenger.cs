using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class Revenger
    {
        private static readonly int Id = 17600;
        private static Color RoleColor = UtilsRoleText.GetRoleColor(CustomRoles.Revenger);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "я");
        public static List<byte> playerIdList = new();
        public static OptionItem RevengeToImpostor;
        public static OptionItem RevengeToCrewmate;
        public static OptionItem RevengeToMadmate;
        public static OptionItem RevengeToNeutral;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Revenger, fromtext: UtilsOption.GetFrom(From.TownOfHost_Y));
            AddOnsAssignData.Create(Id + 10, CustomRoles.Revenger, true, true, true, true);
            ObjectOptionitem.Create(Id + 54, "AddonOption", true, "", TabGroup.Addons).SetOptionName(() => "Role Option").SetSubRoleOptionItem(CustomRoles.Revenger);
            RevengeToImpostor = BooleanOptionItem.Create(Id + 50, "NekoKabochaImpostorsGetRevenged", true, TabGroup.Addons, false).SetSubRoleOptionItem(CustomRoles.Revenger);
            RevengeToCrewmate = BooleanOptionItem.Create(Id + 51, "RevengeToCrewmate", true, TabGroup.Addons, false).SetSubRoleOptionItem(CustomRoles.Revenger);
            RevengeToMadmate = BooleanOptionItem.Create(Id + 52, "NekoKabochaMadmatesGetRevenged", true, TabGroup.Addons, false).SetSubRoleOptionItem(CustomRoles.Revenger);
            RevengeToNeutral = BooleanOptionItem.Create(Id + 53, "RevengeToNeutral", true, TabGroup.Addons, false).SetSubRoleOptionItem(CustomRoles.Revenger);
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