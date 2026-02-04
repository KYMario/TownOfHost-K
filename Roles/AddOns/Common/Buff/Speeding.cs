using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class Speeding
    {
        private static readonly int Id = 17900;
        private static Color RoleColor = UtilsRoleText.GetRoleColor(CustomRoles.Speeding);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "∈");
        public static List<byte> playerIdList = new();
        private static OptionItem OptionSpeed;
        public static float Speed;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Speeding);
            AddOnsAssignData.Create(Id + 10, CustomRoles.Speeding, true, true, true, true);
            ObjectOptionitem.Create(Id + 51, "AddonOption", true, "", TabGroup.Addons).SetOptionName(() => "Role Option").SetSubRoleOptionItem(CustomRoles.Autopsy);
            OptionSpeed = FloatOptionItem.Create(Id + 50, "AddSpeed", new(0.25f, 10f, 0.25f), 0.5f, TabGroup.Addons, false).SetSubRoleOptionItem(CustomRoles.Speeding);
        }
        public static void Init()
        {
            playerIdList = new();
            Speed = OptionSpeed.GetFloat();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
    }
}