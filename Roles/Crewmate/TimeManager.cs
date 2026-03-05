using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Crewmate
{
    public sealed class TimeManager : RoleBase, IMeetingTimeAlterable
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(TimeManager),
                player => new TimeManager(player),
                CustomRoles.TimeManager,
                () => RoleTypes.Crewmate,
                CustomRoleTypes.Crewmate,
                11600,
                SetupOptionItem,
                "tm",
                "#6495ed",
                (7, 2),
                from: From.TownOfHost
            );
        public TimeManager(PlayerControl player)
        : base(
            RoleInfo,
            player
        )
        {
            IncreaseMeetingTime = OptionIncreaseMeetingTime.GetInt();
            myaddtime = 0;
        }
        private static OptionItem OptionIncreaseMeetingTime;
        enum OptionName
        {
            TimeManagerIncreaseMeetingTime,
        }
        public static int IncreaseMeetingTime;
        int myaddtime;
        public bool RevertOnDie => true;

        private static void SetupOptionItem()
        {
            OptionIncreaseMeetingTime = IntegerOptionItem.Create(RoleInfo, 10, OptionName.TimeManagerIncreaseMeetingTime, new(5, 30, 1), 15, false)
                .SetValueFormat(OptionFormat.Seconds);
        }

        public int CalculateMeetingTimeDelta()
        {
            if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return 0;
            var sec = IncreaseMeetingTime * MyTaskState.CompletedTasksCount;
            return sec;
        }
        public override string GetProgressText(bool comms = false, bool gamelog = false)
        {
            var time = CalculateMeetingTimeDelta();
            return time > 0 ? Utils.ColorString(UtilsRoleText.GetRoleColor(CustomRoles.TimeManager).ShadeColor(0.5f), $"+{time}s") : "";
        }
        public override void OnStartMeeting()
        {
            if (Player.IsAlive()) return;
            myaddtime += CalculateMeetingTimeDelta();
        }
        public override void CheckWinner(GameOverReason reason)
        {
            Achievements.RpcCompleteAchievement(Player.PlayerId, 1, achievements[0], myaddtime);
            Achievements.RpcCompleteAchievement(Player.PlayerId, 1, achievements[1], myaddtime);
            Achievements.RpcCompleteAchievement(Player.PlayerId, 1, achievements[2], myaddtime);
        }
        public static System.Collections.Generic.Dictionary<int, Achievement> achievements = new();
        [Attributes.PluginModuleInitializer]
        public static void Load()
        {
            var n1 = new Achievement(RoleInfo, 0, 60, 0, 0);
            var l1 = new Achievement(RoleInfo, 1, 300, 0, 1);
            var sp1 = new Achievement(RoleInfo, 2, 1800, 0, 2);
            achievements.Add(0, n1);
            achievements.Add(1, l1);
            achievements.Add(2, sp1);
        }
    }
}