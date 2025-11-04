using System;
using System.Collections.Generic;

using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost
{
    public class OverrideKilldistance
    {
        public static Dictionary<CustomRoles, OverrideKilldistance> AllData = new();
        public CustomRoles Role { get; private set; }
        public int IdStart { get; private set; }
        public OptionItem Killdistance;
        public enum KillDistance
        {
            KDShort,//ショート
            KDMidium,//ミドル
            KDLong//ロング
        }

        public OverrideKilldistance(int idStart, TabGroup tab, CustomRoles role, CustomRoles RoleName = CustomRoles.NotAssigned)
        {
            this.IdStart = idStart;
            this.Role = role;
            var rolename = RoleName == CustomRoles.NotAssigned ? role : RoleName;
            Dictionary<string, string> replacementDic = new() { { "%role%", Utils.ColorString(UtilsRoleText.GetRoleColor(rolename), UtilsRoleText.GetRoleName(rolename)) } };
            Killdistance = StringOptionItem.Create(IdStart, "Killdistance", EnumHelper.GetAllNames<KillDistance>(), 0, tab, false)
            .SetParent(CustomRoleSpawnChances[role]).SetParentRole(role);
            Killdistance.ReplacementDictionary = replacementDic;

            role = RoleName == CustomRoles.NotAssigned ? role : RoleName;

            if (!AllData.ContainsKey(role)) AllData.Add(role, this);
            else Logger.Warn("重複したCustomRolesを対象とするOverrideKilldistanceが作成されました", "OverrideKilldistance");
        }
        public static OverrideKilldistance Create(int idStart, TabGroup tab, CustomRoles role)
        {
            return new OverrideKilldistance(idStart, tab, role);
        }
        /// <summary>
        /// キルディスタンスの上書き設定。
        /// </summary>
        /// <param name="idStart">ID</param>
        /// <param name="tab">タブ</param>
        /// <param name="role">設定に出すロール</param>
        /// <param name="RoleName">設定名(ユニット用)</param>
        public static OverrideKilldistance Create(SimpleRoleInfo roleInfo, int idOffset, CustomRoles rolename = CustomRoles.NotAssigned)
        {
            return new OverrideKilldistance(roleInfo.ConfigId + idOffset, roleInfo.Tab, roleInfo.RoleName, rolename);
        }
    }
    /// <summary>
    /// ID+4?まで使うから重複注意
    /// </summary>
    public class OverrideTasksData
    {
        public static Dictionary<CustomRoles, OverrideTasksData> AllData = new();
        public static Dictionary<CustomRoles, CustomRoles> RoleNames = new(); //1人までしか対応していない、
        public CustomRoles Role { get; private set; }
        public int IdStart { get; private set; }
        public OptionItem doOverride;
        public OptionItem numCommonTasks;
        public OptionItem numLongTasks;
        public OptionItem numShortTasks;

        /// <summary>
        /// タスクの上書き設定。
        /// </summary>
        /// <param name="idStart">ID(+4...?まで使用するので注意)</param>
        /// <param name="tab">タブ</param>
        /// <param name="role">設定に出すロール</param>
        /// <param name="RoleName">設定名(ユニット用)</param>
        public OverrideTasksData(int idStart, TabGroup tab, CustomRoles role, CustomRoles RoleName = CustomRoles.NotAssigned, (bool defo, int common, int Long, int Short)? tasks = null, OptionItem parent = null)
        {
            this.IdStart = idStart;
            this.Role = role;
            var rolename = RoleName == CustomRoles.NotAssigned ? role : RoleName;
            Dictionary<string, string> replacementDic = new() { { "%role%", Utils.ColorString(UtilsRoleText.GetRoleColor(rolename), UtilsRoleText.GetRoleName(rolename)) } };
            doOverride = BooleanOptionItem.Create(idStart++, "doOverride", tasks?.defo ?? false, tab, false).SetParent(parent is null ? CustomRoleSpawnChances[role] : parent).SetParentRole(role)
                .SetValueFormat(OptionFormat.None);
            doOverride.ReplacementDictionary = replacementDic;
            numCommonTasks = IntegerOptionItem.Create(idStart++, "roleCommonTasksNum", new(0, 99, 1), tasks?.common ?? 3, tab, false).SetParent(doOverride).SetParentRole(role)
                .SetValueFormat(OptionFormat.Pieces);
            numCommonTasks.ReplacementDictionary = replacementDic;
            numLongTasks = IntegerOptionItem.Create(idStart++, "roleLongTasksNum", new(0, 99, 1), tasks?.Long ?? 3, tab, false).SetParent(doOverride).SetParentRole(role)
                .SetValueFormat(OptionFormat.Pieces);
            numLongTasks.ReplacementDictionary = replacementDic;
            numShortTasks = IntegerOptionItem.Create(idStart++, "roleShortTasksNum", new(0, 99, 1), tasks?.Short ?? 3, tab, false).SetParent(doOverride).SetParentRole(role)
                .SetValueFormat(OptionFormat.Pieces);
            numShortTasks.ReplacementDictionary = replacementDic;

            role = RoleName == CustomRoles.NotAssigned ? role : RoleName;

            if (!AllData.ContainsKey(role)) AllData.Add(role, this);
            else Logger.Warn("重複したCustomRolesを対象とするOverrideTasksDataが作成されました", "OverrideTasksData");
        }
        /// <summary>
        /// タスクの上書き設定。
        /// </summary>
        /// <param name="idStart">ID(+4..?まで使用するので注意)</param>
        /// <param name="tab">タブ</param>
        /// <param name="role">設定に出すロール</param>
        /// <param name="RoleName">設定名(ユニット用)</param>
        public static OverrideTasksData Create(SimpleRoleInfo roleInfo, int idOffset, CustomRoles rolename = CustomRoles.NotAssigned, (bool defo, int common, int Long, int Short)? tasks = null, OptionItem parent = null)
        {
            return new OverrideTasksData(roleInfo.ConfigId + idOffset, roleInfo.Tab, roleInfo.RoleName, rolename, tasks, parent);
        }
    }
    public class SoloWinOption
    {
        public static Dictionary<CustomRoles, SoloWinOption> AllData = new();
        public CustomRoles Role { get; private set; }
        public int IdStart { get; private set; }
        public OptionItem OptionWin;
        public SoloWinOption(int idStart, TabGroup tab, CustomRoles role, CustomRoles RoleName = CustomRoles.NotAssigned, Func<bool> show = null, int defo = 0)
        {
            if (show == null)
            {
                show = new(() => true);
            }
            this.IdStart = idStart;
            this.Role = role;
            var rolename = RoleName == CustomRoles.NotAssigned ? role : RoleName;
            Dictionary<string, string> replacementDic = new() { { "%role%", UtilsRoleText.GetCombinationName(rolename) } };
            if (tab is TabGroup.MainSettings)
            {
                OptionWin = IntegerOptionItem.Create(IdStart, "SoloWinOption", new(0, 50, 1), defo, tab, false)
                .SetEnabled(show)
                .SetColor(UtilsRoleText.GetRoleColor(role));
            }
            else
            {
                OptionWin = IntegerOptionItem.Create(IdStart, "SoloWinOption", new(0, 50, 1), defo, tab, false)
                .SetParent(CustomRoleSpawnChances[role])
                .SetParentRole(role)
                .SetEnabled(show);
            }
            OptionWin.ReplacementDictionary = replacementDic;

            role = RoleName == CustomRoles.NotAssigned ? role : RoleName;

            if (!AllData.ContainsKey(role)) AllData.Add(role, this);
            else Logger.Warn("重複したCustomRolesを対象とするSoloWinOptionが作成されました", "SoloWinOption");
        }
        public static SoloWinOption Create(int idStart, TabGroup tab, CustomRoles role, Func<bool> show = null, int defo = 0)
        {
            return new SoloWinOption(idStart, tab, role, show: show, defo: defo);
        }
        /// <summary>
        /// キルディスタンスの上書き設定。
        /// </summary>
        /// <param name="idStart">ID</param>
        /// <param name="tab">タブ</param>
        /// <param name="role">設定に出すロール</param>
        /// <param name="RoleName">設定名(ユニット用)</param>
        public static SoloWinOption Create(SimpleRoleInfo roleInfo, int idOffset, CustomRoles rolename = CustomRoles.NotAssigned, Func<bool> show = null, int defo = 0)
        {
            return new SoloWinOption(roleInfo.ConfigId + idOffset, roleInfo.Tab, roleInfo.RoleName, rolename, show, defo: defo);
        }
    }
    public class WinOption
    {
        public static void SetupCustomOption()
        {
            SoloWinOption.Create(1010, TabGroup.MainSettings, CustomRoles.Impostor);
            SoloWinOption.Create(1011, TabGroup.MainSettings, CustomRoles.Crewmate);
            SoloWinOption.Create(1012, TabGroup.MainSettings, CustomRoles.Jackal);
        }
    }
}