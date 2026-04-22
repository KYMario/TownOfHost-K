using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class Powerful
    {
        private static readonly int Id = 24600;
        private static Color RoleColor = UtilsRoleText.GetRoleColor(CustomRoles.Powerful);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "∠");
        public static List<byte> playerIdList = new();
        public static OptionItem AdditionalVote;

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Powerful);
            AddOnsAssignDataOnlyKiller.Create(Id + 10, CustomRoles.Powerful, true, true, true, true);
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