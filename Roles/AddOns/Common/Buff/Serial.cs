using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class Serial
    {
        private static readonly int Id = 17800;
        private static Color RoleColor = UtilsRoleText.GetRoleColor(CustomRoles.Serial);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "∂");
        public static List<byte> playerIdList = new();
        public static OptionItem KillCooldown;

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Serial);
            AddOnsAssignDataOnlyKiller.Create(Id + 10, CustomRoles.Serial, true, true, true, true);
            ObjectOptionitem.Create(Id + 51, "AddonOption", true, "", TabGroup.Addons).SetOptionName(() => "Role Option").SetSubRoleOptionItem(CustomRoles.Serial);
            KillCooldown = FloatOptionItem.Create(Id + 50, "KillCooldown", new(0f, 180f, 0.5f), 25f, TabGroup.Addons, false).SetValueFormat(OptionFormat.Seconds).SetSubRoleOptionItem(CustomRoles.Serial);
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