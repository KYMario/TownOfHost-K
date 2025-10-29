using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Options;
namespace TownOfHost.Roles.AddOns.Neutral
{
    public static class LastNeutral
    {
        private static readonly int Id = 19800;
        public static byte currentId = byte.MaxValue;
        public static OptionItem KillCooldown;
        public static OptionItem GiveKillCooldown;
        public static readonly string[] Givekillcooldownmode =
        {
            "ColoredOff","GiveKillcoolShort","AllGiveKillCoolShort","ColoredOn"
        };
        public static OptionItem ChKilldis;
        //追加勝利
        public static OptionItem GiveOpportunist;
        public static OptionItem CanCrewWin;
        public static OptionItem CanTaskWin;
        //ゲッサー
        public static OptionItem GiveGuesser;
        public static OptionItem CanGuessTime; public static OptionItem OwnCanGuessTime;
        public static OptionItem ICanGuessVanilla; public static OptionItem ICanGuessTaskDoneSnitch; public static OptionItem ICanWhiteCrew;
        public static OptionItem AddShotLimit;
        //マネジメント
        public static OptionItem GiveManagement;
        public static OptionItem ManagementCanSeeComms; public static OptionItem PercentGage; public static OptionItem ManagementCanSeeMeeting;
        public static OptionItem RoughPercentage;
        //ウォッチング
        public static OptionItem GiveWatching;
        //シーイング
        public static OptionItem GiveSeeing;
        public static OptionItem SeeingCanSeeComms;
        //オートプシー
        public static OptionItem GiveAutopsy;
        public static OptionItem AutopsyCanSeeComms;
        //タイブレーカー
        public static OptionItem GiveTiebreaker;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.LastNeutral, new(1, 1, 1));
            GiveKillCooldown = StringOptionItem.Create(Id + 6, "Givekillcoondown", Givekillcooldownmode, 3, TabGroup.Addons, false).SetParentRole(CustomRoles.LastNeutral).SetParent(CustomRoleSpawnChances[CustomRoles.LastNeutral]);
            KillCooldown = FloatOptionItem.Create(Id + 8, "KillCooldown", new(0f, 180f, 1f), 15f, TabGroup.Addons, false).SetParentRole(CustomRoles.LastNeutral).SetParent(GiveKillCooldown)
                .SetValueFormat(OptionFormat.Seconds);
            ChKilldis = BooleanOptionItem.Create(Id + 7, "ChKilldis", false, TabGroup.Addons, false).SetParentRole(CustomRoles.LastNeutral).SetParent(CustomRoleSpawnChances[CustomRoles.LastNeutral]);
            OverrideKilldistance.Create(Id + 5, TabGroup.Addons, CustomRoles.LastNeutral);
            GiveOpportunist = BooleanOptionItem.Create(Id + 10, "GiveOpportunist", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.LastNeutral]).SetParentRole(CustomRoles.LastNeutral);
            CanCrewWin = BooleanOptionItem.Create(Id + 30, "LastNeutralCanCrewWin", false, TabGroup.Addons, false).SetParent(GiveOpportunist).SetParentRole(CustomRoles.LastNeutral);
            CanTaskWin = BooleanOptionItem.Create(Id + 31, "LastNeutralCantaskWin", false, TabGroup.Addons, false).SetParent(GiveOpportunist).SetParentRole(CustomRoles.LastNeutral);
            GiveGuesser = BooleanOptionItem.Create(Id + 11, "GiveGuesser", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.LastNeutral]).SetParentRole(CustomRoles.LastNeutral);
            CanGuessTime = IntegerOptionItem.Create(Id + 12, "CanGuessTime", new(1, 15, 1), 3, TabGroup.Addons, false).SetParent(GiveGuesser).SetParentRole(CustomRoles.LastNeutral)
                .SetValueFormat(OptionFormat.Players);
            AddShotLimit = BooleanOptionItem.Create(Id + 13, "AddShotLimit", false, TabGroup.Addons, false).SetParent(GiveGuesser).SetParentRole(CustomRoles.LastNeutral);
            OwnCanGuessTime = IntegerOptionItem.Create(Id + 14, "OwnCanGuessTime", new(1, 15, 1), 1, TabGroup.Addons, false).SetParent(GiveGuesser).SetParentRole(CustomRoles.LastNeutral)
                    .SetValueFormat(OptionFormat.Players);
            ICanGuessVanilla = BooleanOptionItem.Create(Id + 16, "CanGuessVanilla", true, TabGroup.Addons, false).SetParent(GiveGuesser).SetParentRole(CustomRoles.LastNeutral);
            ICanGuessTaskDoneSnitch = BooleanOptionItem.Create(Id + 17, "CanGuessTaskDoneSnitch", false, TabGroup.Addons, false).SetParent(GiveGuesser).SetParentRole(CustomRoles.LastNeutral);
            ICanWhiteCrew = BooleanOptionItem.Create(Id + 18, "CanWhiteCrew", false, TabGroup.Addons, false).SetParent(GiveGuesser).SetParentRole(CustomRoles.LastNeutral);
            GiveManagement = BooleanOptionItem.Create(Id + 19, "GiveManagement", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.LastNeutral]).SetParentRole(CustomRoles.LastNeutral);
            PercentGage = BooleanOptionItem.Create(Id + 20, "PercentGage", false, TabGroup.Addons, false).SetParent(GiveManagement).SetParentRole(CustomRoles.LastNeutral);
            RoughPercentage = BooleanOptionItem.Create(Id + 21, "RoughPercentage", false, TabGroup.Addons, false).SetParent(PercentGage).SetParentRole(CustomRoles.LastNeutral);
            ManagementCanSeeComms = BooleanOptionItem.Create(Id + 22, "CanUseActiveComms", false, TabGroup.Addons, false).SetParent(GiveManagement).SetParentRole(CustomRoles.LastNeutral);
            ManagementCanSeeMeeting = BooleanOptionItem.Create(Id + 23, "CanseeMeeting", false, TabGroup.Addons, false).SetParent(GiveManagement).SetParentRole(CustomRoles.LastNeutral);
            GiveWatching = BooleanOptionItem.Create(Id + 24, "GiveWatching", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.LastNeutral]).SetParentRole(CustomRoles.LastNeutral);
            GiveSeeing = BooleanOptionItem.Create(Id + 25, "GiveSeeing", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.LastNeutral]).SetParentRole(CustomRoles.LastNeutral);
            SeeingCanSeeComms = BooleanOptionItem.Create(Id + 26, "CanUseActiveComms", true, TabGroup.Addons, false).SetParent(GiveSeeing).SetParentRole(CustomRoles.LastNeutral);
            GiveAutopsy = BooleanOptionItem.Create(Id + 27, "GiveAutopsy", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.LastNeutral]).SetParentRole(CustomRoles.LastNeutral);
            AutopsyCanSeeComms = BooleanOptionItem.Create(Id + 28, "CanUseActiveComms", true, TabGroup.Addons, false).SetParent(GiveAutopsy).SetParentRole(CustomRoles.LastNeutral);
            GiveTiebreaker = BooleanOptionItem.Create(Id + 29, "GiveTiebreaker", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.LastNeutral]).SetParentRole(CustomRoles.LastNeutral);
        }
        public static void Init() => currentId = byte.MaxValue;
        public static void Add(byte id) => currentId = id;
        public static void SetKillCooldown(PlayerControl player)
        {
            if (currentId == byte.MaxValue) return;
            var roleclass = player.GetRoleClass();
            switch (Givekillcooldownmode[GiveKillCooldown.GetValue()])
            {
                case "GiveKillcoolShort"://短くなる場合のみ
                    if (KillCooldown.GetFloat() < Main.AllPlayerKillCooldown[currentId] &&
                        (roleclass is ILNKiller))//かつラスポスキルク受け取る
                        Main.AllPlayerKillCooldown[currentId] = KillCooldown.GetFloat();
                    break;
                case "AllGiveKillCoolShort"://ラスポルでキルク恩恵受け取るかに関わらず短くなるなら貰う
                    if (KillCooldown.GetFloat() < Main.AllPlayerKillCooldown[currentId])
                        Main.AllPlayerKillCooldown[currentId] = KillCooldown.GetFloat();
                    break;
                case "ColoredOn":
                    if (roleclass is ILNKiller)
                        Main.AllPlayerKillCooldown[currentId] = KillCooldown.GetFloat();
                    break;
            }
        }
        public static bool CanBeLastNeutral(PlayerControl pc)
        {
            if (!pc.IsAlive() || pc.Is(CustomRoles.LastNeutral) || !pc.Is(CustomRoleTypes.Neutral))
            {
                return false;
            }
            return true;
        }
        public static void SetSubRole()
        {
            if (currentId != byte.MaxValue) return;
            if (CurrentGameMode == CustomGameMode.HideAndSeek
            || !CustomRoles.LastNeutral.IsPresent() || PlayerCatch.AliveNeutalCount != 1)
                return;
            foreach (var pc in PlayerCatch.AllAlivePlayerControls)
            {
                if (CanBeLastNeutral(pc))
                {
                    pc.RpcSetCustomRole(CustomRoles.LastNeutral);
                    Add(pc.PlayerId);
                    if (AmongUsClient.Instance.AmHost)
                    {
                        SetKillCooldown(pc);
                        pc.SyncSettings();
                        UtilsNotifyRoles.NotifyRoles();
                    }
                    UtilsGameLog.LastLogRole[pc.PlayerId] = "<b>" + Utils.ColorString(UtilsRoleText.GetRoleColor(pc.GetCustomRole()), Translator.GetString("Last-")) + UtilsGameLog.LastLogRole[pc.PlayerId] + "</b>";
                    break;
                }
            }
        }
        public static bool CheckAddWin(PlayerControl pc, GameOverReason reason)
        {
            if ((pc.Is(CustomRoles.LastNeutral) && GiveOpportunist.GetBool()) is false) return false;
            if (reason.Equals(GameOverReason.CrewmatesByTask) && !CanTaskWin.GetBool()) return false;
            if (reason.Equals(GameOverReason.CrewmatesByVote) && !CanCrewWin.GetBool()) return false;

            if (pc.GetCustomRole() is CustomRoles.Terrorist or CustomRoles.Madonna) return false;

            if (pc.IsAlive() && !pc.IsLovers())
            {
                Logger.Info($"{pc.Data.GetLogPlayerName()} : LastNeutralで追加勝利", "LastNeutral");
                CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.LastNeutral);
                return true;
            }
            return false;
        }
    }
}