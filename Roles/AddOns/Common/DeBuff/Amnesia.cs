using System.Collections.Generic;
using System.Linq;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    //いつかクソゲーにはなるけど全員の役職分からない状態で試合させたい。
    public static class Amnesia
    {
        private static readonly int Id = 18200;
        public static List<byte> playerIdList = new();
        public static OptionItem OptionCanRealizeDay;
        public static OptionItem OptionRealizeDayCount;
        public static OptionItem OptionCanRealizeTask;
        public static OptionItem OptionRealizeTaskCount;
        public static OptionItem OptionCanRealizeKill;
        public static OptionItem OptionRealizeKillcount;
        public static OptionItem OptionRealizeImpostorCount;
        public static OptionItem OptionDontCanUseAbility;
        public static OptionItem OptionDefaultKillCool;
        public static bool dontcanUseability;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Amnesia);
            AddOnsAssignData.Create(Id + 10, CustomRoles.Amnesia, true, true, true, true);
            OptionDontCanUseAbility = BooleanOptionItem.Create(Id + 40, "AmnesiaDontCanUseAbility", true, TabGroup.Addons, false).SetParentRole(CustomRoles.Amnesia).SetParent(CustomRoleSpawnChances[CustomRoles.Amnesia]);
            OptionDefaultKillCool = BooleanOptionItem.Create(Id + 41, "AmnesiaDefaultKillCool", true, TabGroup.Addons, false).SetParentRole(CustomRoles.Amnesia).SetParent(OptionDontCanUseAbility);
            OptionCanRealizeDay = BooleanOptionItem.Create(Id + 50, "AmnesiaCanRealizeDay", true, TabGroup.Addons, false).SetParentRole(CustomRoles.Amnesia).SetParent(CustomRoleSpawnChances[CustomRoles.Amnesia]);
            OptionRealizeDayCount = IntegerOptionItem.Create(Id + 51, "AmnesiaRealizeDayCount", new(1, 99, 1), 4, TabGroup.Addons, false).SetParentRole(CustomRoles.Amnesia).SetParent(OptionCanRealizeDay).SetValueFormat(OptionFormat.day);
            OptionCanRealizeTask = BooleanOptionItem.Create(Id + 52, "AmnesiaCanRealizeTask", true, TabGroup.Addons, false).SetParentRole(CustomRoles.Amnesia).SetParent(CustomRoleSpawnChances[CustomRoles.Amnesia]);
            OptionRealizeTaskCount = IntegerOptionItem.Create(Id + 53, "AmnesiaRealizeTaskCount", new(1, 255, 1), 4, TabGroup.Addons, false).SetParentRole(CustomRoles.Amnesia).SetParent(OptionCanRealizeTask);
            OptionCanRealizeKill = BooleanOptionItem.Create(Id + 54, "AmnesiaCanRealizeKill", true, TabGroup.Addons, false).SetParentRole(CustomRoles.Amnesia).SetParent(CustomRoleSpawnChances[CustomRoles.Amnesia]);
            OptionRealizeKillcount = IntegerOptionItem.Create(Id + 55, "AmnesiaRealizeKillcount", new(1, 15, 1), 2, TabGroup.Addons, false).SetParentRole(CustomRoles.Amnesia).SetParent(OptionCanRealizeKill);
            OptionRealizeImpostorCount = IntegerOptionItem.Create(Id + 56, "AmnesiaRealizeImpostorCount", new(0, 3, 1), 1, TabGroup.Addons, false).SetParentRole(CustomRoles.Amnesia).SetParent(CustomRoleSpawnChances[CustomRoles.Amnesia]).SetZeroNotation(OptionZeroNotation.Off);
        }

        public static void Init()
        {
            playerIdList = new();
            dontcanUseability = OptionDontCanUseAbility.GetBool();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static void RemoveAmnesia(byte playerId)
        {
            playerIdList.Remove(playerId);
            PlayerState.GetByPlayerId(playerId).RemoveSubRole(CustomRoles.Amnesia);
            UtilsGameLog.AddGameLog("Amnesia", string.Format(Translator.GetString("Am.log"), UtilsName.GetPlayerColor(playerId)));
        }
        /// <summary>
        /// アムネシアの能力削除が適応されている状態か
        /// </summary>
        /// <param name="player"></param>
        /// <returns>trueなら使用不可</returns>
        public static bool CheckAbilityreturn(PlayerControl player) => player is null || (playerIdList.Contains(player?.PlayerId ?? byte.MaxValue) && dontcanUseability);

        /// <summary>
        /// 能力が使用できる状態か
        /// </summary>
        /// <param name="player"></param>
        /// <returns>trueなら使用可能</returns>
        public static bool CheckAbility(PlayerControl player) => player == null || playerIdList.Contains(player?.PlayerId ?? byte.MaxValue) is false || !dontcanUseability || playerIdList.Count == 0;

        public static void CheckImpostorCount()
        {
            if (OptionRealizeImpostorCount.GetBool() is false) return;
            if (PlayerCatch.AliveImpostorCount > OptionRealizeImpostorCount.GetInt()) return;
            foreach (var imp in PlayerCatch.AllAlivePlayerControls.Where(pc => pc.GetCustomRole().IsImpostor() && pc.Is(CustomRoles.Amnesia)))
            {
                RemoveAmnesia(imp.PlayerId);
            }
        }
    }
}