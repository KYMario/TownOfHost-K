using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class SlowStarter
    {
        private static readonly int Id = 19500;
        private static Color RoleColor = UtilsRoleText.GetRoleColor(CustomRoles.SlowStarter);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "Ｓs");
        public static List<byte> playerIdList = new();
        private static OptionItem CanKillImpostorCount;
        private static OptionItem CanKillDay;
        public static bool cankill;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.SlowStarter, new(1, 3, 1));
            AddOnsAssignData.Create(Id + 10, CustomRoles.SlowStarter, false, false, true, false);
            ObjectOptionitem.Create(Id + 52, "AddonOption", true, "", TabGroup.Addons).SetOptionName(() => "Role Option").SetSubRoleOptionItem(CustomRoles.SlowStarter);
            CanKillImpostorCount = IntegerOptionItem.Create(Id + 50, "MafiaCanKillImpostorCount", new(1, 3, 1), 2, TabGroup.Addons, false).SetSubRoleOptionItem(CustomRoles.SlowStarter);
            CanKillDay = IntegerOptionItem.Create(Id + 51, "MafiaCanKillDay", new(0, 30, 1), 0, TabGroup.Addons, false).SetZeroNotation(OptionZeroNotation.Off).SetSubRoleOptionItem(CustomRoles.SlowStarter).SetValueFormat(OptionFormat.day);
        }
        static int cankillcount;
        public static void Init()
        {
            cankillcount = CanKillImpostorCount.GetInt();
            cankill = false;
            playerIdList = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static bool CanUseKill() => PlayerCatch.AliveImpostorCount <= cankillcount || cankill;
        public static void OnStartMeeting()
        {
            if (CanKillDay.GetInt() == 0) return;

            if (CanKillDay.GetInt() <= UtilsGameLog.day) cankill = true;
        }
    }
}