using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Impostor
{
    public static class LastImpostor
    {
        private static readonly int Id = 19400;
        public static byte currentId = byte.MaxValue;
        public static OptionItem KillCooldown;
        public static OptionItem GiveKillCooldown;
        public static readonly string[] Givekillcooldownmode =
        {
            "ColoredOff","GiveKillcoolShort","AllGiveKillCoolShort","ColoredOn"
        };
        //ゲッサー
        public static OptionItem GiveGuesser; public static bool giveguesser;
        public static OptionItem CanGuessTime; public static OptionItem OwnCanGuessTime;
        public static OptionItem ICanGuessVanilla; public static OptionItem ICanGuessNakama; public static OptionItem ICanGuessTaskDoneSnitch;
        public static OptionItem ICanWhiteCrew; public static OptionItem AddShotLimit;
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
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.LastImpostor, new(1, 1, 1), fromtext: "<color=#000000>From:</color><color=#00bfff>TownOfHost</color></size>");
            GiveKillCooldown = StringOptionItem.Create(Id + 7, "Givekillcoondown", Givekillcooldownmode, 3, TabGroup.Addons, false).SetParentRole(CustomRoles.LastImpostor).SetParent(CustomRoleSpawnChances[CustomRoles.LastImpostor]);
            KillCooldown = FloatOptionItem.Create(Id + 8, "KillCooldown", new(0f, 180f, 1f), 15f, TabGroup.Addons, false).SetParentRole(CustomRoles.LastImpostor).SetParent(GiveKillCooldown)
                .SetValueFormat(OptionFormat.Seconds);
            OverrideKilldistance.Create(Id + 5, TabGroup.Addons, CustomRoles.LastImpostor);
            GiveGuesser = BooleanOptionItem.Create(Id + 11, "GiveGuesser", false, TabGroup.Addons, false).SetParentRole(CustomRoles.LastImpostor).SetParent(CustomRoleSpawnChances[CustomRoles.LastImpostor]);
            CanGuessTime = IntegerOptionItem.Create(Id + 12, "CanGuessTime", new(1, 15, 1), 3, TabGroup.Addons, false).SetParent(GiveGuesser).SetParentRole(CustomRoles.LastImpostor)
                .SetValueFormat(OptionFormat.Players);
            AddShotLimit = BooleanOptionItem.Create(Id + 9, "AddShotLimit", false, TabGroup.Addons, false).SetParent(GiveGuesser).SetParentRole(CustomRoles.LastImpostor);
            OwnCanGuessTime = IntegerOptionItem.Create(Id + 13, "OwnCanGuessTime", new(1, 15, 1), 1, TabGroup.Addons, false).SetParent(GiveGuesser).SetParentRole(CustomRoles.LastImpostor)
                    .SetValueFormat(OptionFormat.Players);
            ICanGuessVanilla = BooleanOptionItem.Create(Id + 15, "CanGuessVanilla", true, TabGroup.Addons, false).SetParent(GiveGuesser).SetParentRole(CustomRoles.LastImpostor);
            ICanGuessNakama = BooleanOptionItem.Create(Id + 16, "CanGuessNakama", true, TabGroup.Addons, false).SetParent(GiveGuesser).SetParentRole(CustomRoles.LastImpostor);
            ICanGuessTaskDoneSnitch = BooleanOptionItem.Create(Id + 17, "CanGuessTaskDoneSnitch", false, TabGroup.Addons, false).SetParent(GiveGuesser).SetParentRole(CustomRoles.LastImpostor);
            ICanWhiteCrew = BooleanOptionItem.Create(Id + 18, "CanWhiteCrew", false, TabGroup.Addons, false).SetParent(GiveGuesser).SetParentRole(CustomRoles.LastImpostor);
            GiveManagement = BooleanOptionItem.Create(Id + 19, "GiveManagement", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.LastImpostor]).SetParentRole(CustomRoles.LastImpostor);
            PercentGage = BooleanOptionItem.Create(Id + 20, "PercentGage", false, TabGroup.Addons, false).SetParent(GiveManagement).SetParentRole(CustomRoles.LastImpostor);
            RoughPercentage = BooleanOptionItem.Create(Id + 21, "RoughPercentage", false, TabGroup.Addons, false).SetParent(PercentGage).SetParentRole(CustomRoles.LastImpostor);
            ManagementCanSeeComms = BooleanOptionItem.Create(Id + 22, "CanUseActiveComms", false, TabGroup.Addons, false).SetParent(GiveManagement).SetParentRole(CustomRoles.LastImpostor);
            ManagementCanSeeMeeting = BooleanOptionItem.Create(Id + 23, "CanseeMeeting", false, TabGroup.Addons, false).SetParent(GiveManagement).SetParentRole(CustomRoles.LastImpostor);
            GiveWatching = BooleanOptionItem.Create(Id + 24, "GiveWatching", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.LastImpostor]).SetParentRole(CustomRoles.LastImpostor);
            GiveSeeing = BooleanOptionItem.Create(Id + 25, "GiveSeeing", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.LastImpostor]).SetParentRole(CustomRoles.LastImpostor);
            SeeingCanSeeComms = BooleanOptionItem.Create(Id + 26, "CanUseActiveComms", true, TabGroup.Addons, false).SetParent(GiveSeeing).SetParentRole(CustomRoles.LastImpostor);
            GiveAutopsy = BooleanOptionItem.Create(Id + 27, "GiveAutopsy", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.LastImpostor]).SetParentRole(CustomRoles.LastImpostor);
            AutopsyCanSeeComms = BooleanOptionItem.Create(Id + 28, "CanUseActiveComms", true, TabGroup.Addons, false).SetParent(GiveAutopsy).SetParentRole(CustomRoles.LastImpostor);
            GiveTiebreaker = BooleanOptionItem.Create(Id + 29, "GiveTiebreaker", false, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.LastImpostor]).SetParentRole(CustomRoles.LastImpostor);
        }
        public static void Init()
        {
            giveguesser = GiveGuesser.GetBool();
            currentId = byte.MaxValue;
        }
        public static void Add(byte id) => currentId = id;
        public static void SetKillCooldown(PlayerControl player)
        {
            if (currentId == byte.MaxValue) return;
            var roleClass = player.GetRoleClass();
            switch (Givekillcooldownmode[GiveKillCooldown.GetValue()])
            {
                case "GiveKillcoolShort"://短くなる場合のみ
                    if (KillCooldown.GetFloat() < Main.AllPlayerKillCooldown[currentId] &&
                        ((roleClass as IImpostor)?.CanBeLastImpostor ?? true))//かつラスポスキルク受け取る
                        Main.AllPlayerKillCooldown[currentId] = KillCooldown.GetFloat();
                    break;
                case "AllGiveKillCoolShort"://ラスポルでキルク恩恵受け取るかに関わらず短くなるなら貰う
                    if (KillCooldown.GetFloat() < Main.AllPlayerKillCooldown[currentId])
                        Main.AllPlayerKillCooldown[currentId] = KillCooldown.GetFloat();
                    break;
                case "ColoredOn":
                    if ((roleClass as IImpostor)?.CanBeLastImpostor ?? true)
                        Main.AllPlayerKillCooldown[currentId] = KillCooldown.GetFloat();
                    break;
            }
        }
        public static bool CanBeLastImpostor(PlayerControl pc)
        {
            if (!pc.IsAlive() || pc.Is(CustomRoles.LastImpostor) || !pc.Is(CustomRoleTypes.Impostor))
            {
                return false;
            }
            if (pc.GetRoleClass() is IImpostor impostor)
            {
                return true;
            }
            return true;
        }
        public static void SetSubRole()
        {
            //ラストインポスターがすでにいれば処理不要
            if (currentId != byte.MaxValue) return;
            if (CurrentGameMode == CustomGameMode.HideAndSeek
            || !CustomRoles.LastImpostor.IsPresent() || PlayerCatch.AliveImpostorCount != 1)
                return;
            foreach (var pc in PlayerCatch.AllAlivePlayerControls)
            {
                if (CanBeLastImpostor(pc))
                {
                    pc.RpcSetCustomRole(CustomRoles.LastImpostor);
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
    }
}