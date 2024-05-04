using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class Opener
    {
        private static readonly int Id = 70900;
        private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Opener);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "п");
        public static List<byte> playerIdList = new();
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Opener);
            AddOnsAssignData.Create(Id + 10, CustomRoles.Opener, true, true, true, true);
        }
        public static void Init()
        {
            playerIdList = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static bool IsEnable => playerIdList.Count > 0;
        public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);

    }
}