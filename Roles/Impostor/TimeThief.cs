using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor
{
    public sealed class TimeThief : RoleBase, IMeetingTimeAlterable, IImpostor
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(TimeThief),
                player => new TimeThief(player),
                CustomRoles.TimeThief,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Impostor,
                4700,
                SetupOptionItem,
                "tt",
                OptionSort: (4, 1),
                from: From.TownOfHost
            );
        public TimeThief(PlayerControl player)
        : base(
            RoleInfo,
            player
        )
        {
            KillCooldown = OptionKillCooldown.GetFloat();
            DecreaseMeetingTime = OptionDecreaseMeetingTime.GetInt();
            ReturnStolenTimeUponDeath = OptionReturnStolenTimeUponDeath.GetBool();
        }
        private static OptionItem OptionKillCooldown;
        private static OptionItem OptionDecreaseMeetingTime;
        private static OptionItem OptionReturnStolenTimeUponDeath;
        enum OptionName
        {
            TimeThiefDecreaseMeetingTime,
            TimeThiefReturnStolenTimeUponDeath
        }
        public static float KillCooldown;
        public static int DecreaseMeetingTime;
        public static bool ReturnStolenTimeUponDeath;

        public bool RevertOnDie => ReturnStolenTimeUponDeath;

        private static void SetupOptionItem()
        {
            OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 0.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionDecreaseMeetingTime = IntegerOptionItem.Create(RoleInfo, 11, OptionName.TimeThiefDecreaseMeetingTime, new(0, 100, 1), 20, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionReturnStolenTimeUponDeath = BooleanOptionItem.Create(RoleInfo, 12, OptionName.TimeThiefReturnStolenTimeUponDeath, true, false);
        }
        public float CalculateKillCooldown() => KillCooldown;
        public int CalculateMeetingTimeDelta()
        {
            if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return 0;
            var sec = -(DecreaseMeetingTime * MyState.GetKillCount(true));
            return sec;
        }
        public override string GetProgressText(bool comms = false, bool gamelog = false)
        {
            var time = CalculateMeetingTimeDelta();
            return time < 0 ? Utils.ColorString(Palette.ImpostorRed.ShadeColor(0.5f), $"{time}s") : "";
        }
        public override void CheckWinner(GameOverReason reason)
        {
            var sec = DecreaseMeetingTime * MyState.GetKillCount(true);
            Achievements.RpcCompleteAchievement(Player.PlayerId, 1, achievements[0], sec);
            Achievements.RpcCompleteAchievement(Player.PlayerId, 1, achievements[1], sec);
            if ((Main.NormalOptions.DiscussionTime + Main.NormalOptions.VotingTime - Options.LowerLimitVotingTime.GetFloat()) <= sec)
                Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[2]);
        }
        public static System.Collections.Generic.Dictionary<int, Achievement> achievements = new();
        [Attributes.PluginModuleInitializer]
        public static void Load()
        {
            var n1 = new Achievement(RoleInfo, 0, 60, 0, 0);
            var l1 = new Achievement(RoleInfo, 1, 300, 0, 1);
            var sp1 = new Achievement(RoleInfo, 2, 1, 0, 2);
            achievements.Add(0, n1);
            achievements.Add(1, l1);
            achievements.Add(2, sp1);
        }
    }
}