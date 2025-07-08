using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AmongUs.GameOptions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

using TownOfHost.Attributes;
using TownOfHost.Roles.Core;
using TownOfHost.Modules;

[assembly: AssemblyFileVersionAttribute(TownOfHost.Main.PluginVersion)]
[assembly: AssemblyInformationalVersionAttribute(TownOfHost.Main.PluginVersion)]
namespace TownOfHost
{
    [BepInPlugin(PluginGuid, "Town Of Host-K", PluginVersion)]
    [BepInIncompatibility("jp.ykundesu.supernewroles")]
    [BepInProcess("Among Us.exe")]
    public class Main : BasePlugin
    {
        // == プログラム設定 / Program Config ==
        // modの名前 / Mod Name (Default: Town Of Host)
        public static readonly string ModName = "Town Of Host-K";
        // modの色 / Mod Color (Default: #00bfff)
        public static readonly string ModColor = "#00c1ff";
        // 公開ルームを許可する / Allow Public Room (Default: true)
        public static readonly bool AllowPublicRoom = true;
        // フォークID / ForkId (Default: OriginalTOH)
        public static readonly string ForkId = "TOH-K";
        // Discordボタンを表示するか / Show Discord Button (Default: true)
        public static readonly bool ShowDiscordButton = true;
        // Discordサーバーの招待リンク / Discord Server Invite URL (Default: https://discord.gg/W5ug6hXB9V)
        public static readonly string DiscordInviteUrl = "https://discord.gg/5DPqH8seFq";
        // ==========
        public const string OriginalForkId = "OriginalTOH"; // Don't Change The Value. / この値を変更しないでください。
        // == 認証設定 / Authentication Config ==
        // デバッグキーの認証インスタンス
        public static HashAuth DebugKeyAuth { get; private set; }
        public static HashAuth ExplosionKeyAuth { get; private set; }
        // デバッグキーのハッシュ値
        public const string DebugKeyHash = "8e5f06e453e7d11f78ad96b2ca28ff472e085bdb053189612a0a2e0be7973841";
        // 部屋爆破キーのハッシュ値
        public const string ExplosionKeyHash = "e7d88aaf7ea075752792089196d9441c838e6ff47432a719fad6e17cd50a441e";
        // デバッグキーのソルト
        public const string DebugKeySalt = "59687b";
        // デバッグキーのコンフィグ入力
        public static ConfigEntry<string> DebugKeyInput { get; private set; }
        public static ConfigEntry<string> ExplosionKeyInput { get; private set; }

        // ==========
        //Sorry for many Japanese comments.
        public const string PluginGuid = "com.kymario.townofhost-k";
        public const string PluginVersion = "51.13.29.35";//ほんとはx.y.z表記にしたかったけどx.y.z.km.ks表記だと警告だされる
        public const string PluginShowVersion = "51.13.29<sub>.35</sub>";
        public const string ModVersion = ".29";//リリースver用バージョン変更

        /// 配布するデバッグ版なのであればtrue。リリース時にはfalseにすること。
        public static bool DebugVersion = false;

        // サポートされている最低のAmongUsバージョン
        public static readonly string LowestSupportedVersion = "2025.4.20";
        // このバージョンのみで公開ルームを無効にする場合
        public static readonly bool IsPublicAvailableOnThisVersion = false;
        public Harmony Harmony { get; } = new Harmony(PluginGuid);
        public static Version version = Version.Parse(PluginVersion);
        public static BepInEx.Logging.ManualLogSource Logger;
        public static bool CanModClients => false;//参加者もMod導入していいか
        public static bool hasArgumentException = false;
        public static string ExceptionMessage;
        public static bool ExceptionMessageIsShown = false;
        public static string credentialsText;
        public static NormalGameOptionsV09 NormalOptions => GameOptionsManager.Instance.currentNormalGameOptions;
        public static HideNSeekGameOptionsV09 HideNSeekSOptions => GameOptionsManager.Instance.currentHideNSeekGameOptions;
        //Client Options
        public static ConfigEntry<string> HideName { get; private set; }
        public static ConfigEntry<string> HideColor { get; private set; }
        public static ConfigEntry<bool> ForceJapanese { get; private set; }
        public static ConfigEntry<bool> JapaneseRoleName { get; private set; }
        public static ConfigEntry<float> MessageWait { get; private set; }
        public static ConfigEntry<bool> ShowResults { get; private set; }
        public static ConfigEntry<bool> Hiderecommendedsettings { get; private set; }
        public static ConfigEntry<bool> UseWebHook { get; private set; }
        public static ConfigEntry<bool> UseYomiage { get; private set; }
        public static ConfigEntry<bool> CustomName { get; private set; }
        public static ConfigEntry<bool> ShowGameSettingsTMP { get; private set; }
        public static ConfigEntry<bool> CustomSprite { get; private set; }
        public static ConfigEntry<bool> HideSomeFriendCodes { get; private set; }
        public static ConfigEntry<bool> AutoSaveScreenShot { get; private set; }
        public static ConfigEntry<float> MapTheme { get; private set; }
        public static ConfigEntry<bool> ViewPingDetails { get; private set; }
        public static ConfigEntry<bool> DebugChatopen { get; private set; }
        public static ConfigEntry<bool> DebugSendAmout { get; private set; }
        public static ConfigEntry<bool> DebugTours { get; private set; }
        public static ConfigEntry<bool> ShowDistance { get; private set; }
        public static Dictionary<byte, PlayerVersion> playerVersion = new();
        //Preset Name Options
        public static ConfigEntry<string> Preset1 { get; private set; }
        public static ConfigEntry<string> Preset2 { get; private set; }
        public static ConfigEntry<string> Preset3 { get; private set; }
        public static ConfigEntry<string> Preset4 { get; private set; }
        public static ConfigEntry<string> Preset5 { get; private set; }
        public static ConfigEntry<string> Preset6 { get; private set; }
        public static ConfigEntry<string> Preset7 { get; private set; }
        public static ConfigEntry<string> SKey { get; private set; }
        //Other Configs
        public static ConfigEntry<string> BetaBuildURL { get; private set; }
        public static ConfigEntry<float> LastKillCooldown { get; private set; }
        public static ConfigEntry<float> LastShapeshifterCooldown { get; private set; }
        public static ConfigEntry<bool> LastKickModClient { get; private set; }
        public static OptionBackupData RealOptionsData;
        public static Dictionary<byte, Vector2> AllPlayerLastkillpos = new();
        public static Dictionary<byte, string> AllPlayerNames = new();
        public static Dictionary<(byte, byte), string> LastNotifyNames;
        public static Dictionary<byte, Color32> PlayerColors = new();
        public static Dictionary<byte, CustomDeathReason> AfterMeetingDeathPlayers = new();
        public static Dictionary<CustomRoles, string> roleColors;
        public static Dictionary<byte, List<uint>> AllPlayerTask = new();
        public static List<byte> winnerList;
        public static List<int> clientIdList;
        public static List<(string, byte, string)> MessagesToSend;
        public static int MegCount;
        public static Dictionary<byte, float> AllPlayerKillCooldown = new();
        public static bool HnSFlag = false;
        public static bool showkillbutton = false;
        public static bool RTAMode = false;
        public static byte RTAPlayer = 0;
        public static bool EditMode = false;
        public static int page = 0;
        public static bool AssignSameRoles = false;
        public static Dictionary<byte, int> KillCount = new();
        public static string Alltask;
        public static byte LastSab;
        public static SystemTypes SabotageType;
        public static bool NowSabotage;
        public static float sabotagetime;
        public static (float, float) Time;
        public static Dictionary<byte, int> Guard;
        public static int GameCount = 0;
        public static bool SetRoleOverride = true;
        /// <summary>ラグを考慮した奴。アジア、カスタム、ローカルは200ms(0.2s),他は400ms(0.4s)</summary>
        public static float LagTime = 0.2f;
        //public static bool TaskBattleOptionv = false;
        public static int FeColl;
        public static bool IntroHyoji;
        public static bool DontGameSet;
        public static bool CanUseAbility;
        public static CustomRoles HostRole = CustomRoles.NotAssigned;

        /// <summary>
        /// 基本的に速度の代入は禁止.スピードは増減で対応してください.
        /// </summary>
        public static Dictionary<byte, float> AllPlayerSpeed = new();
        public const float MinSpeed = 0.0001f;
        public static Dictionary<byte, bool> CheckShapeshift = new();
        public static Dictionary<byte, byte> ShapeshiftTarget = new();
        public static Dictionary<byte, CustomDeathReason> HostKill = new();
        public static bool VisibleTasksCount;
        public static string nickName = "";
        public static string lobbyname = "";
        public static float DefaultCrewmateVision;
        public static float DefaultImpostorVision;
        public static bool DebugAntiblackout = true;

        public const float RoleTextSize = 2f;
        public static Main Instance;
        public override void Load()
        {
            GameCount = 0;
            Instance = this;

            //Client Options
            HideName = Config.Bind("Client Options", "Hide Game Code Name", "Town Of Host-K");
            HideColor = Config.Bind("Client Options", "Hide Game Code Color", $"{ModColor}");
            ForceJapanese = Config.Bind("Client Options", "Force Japanese", false);
            JapaneseRoleName = Config.Bind("Client Options", "Japanese Role Name", true);
            ShowResults = Config.Bind("Result", "Show Results", true);
            Hiderecommendedsettings = Config.Bind("Client Options", "Hide recommended settings", false);
            UseWebHook = Config.Bind("Client Options", "UseWebHook", false);
            UseYomiage = Config.Bind("Client Options", "UseYomiage", false);
            CustomName = Config.Bind("Client Options", "CustomName", true);
            ShowGameSettingsTMP = Config.Bind("Client Options", "Show GameSettings", true);
            CustomSprite = Config.Bind("Client Options", "CustomSprite", true);
            HideSomeFriendCodes = Config.Bind("Client Options", "Hide Some Friend Codes", false);
            AutoSaveScreenShot = Config.Bind("Client Options", "Auto Save Autro ScreenShot", false);
            MapTheme = Config.Bind("Client Options", "MapTheme", AmongUs.Data.Settings.AudioSettingsData.DEFAULT_MUSIC_VOLUME);
            ViewPingDetails = Config.Bind("Client Options", "View Ping Details", false);
            DebugChatopen = Config.Bind("Client Options", "Debug Chat open", false);
            DebugSendAmout = Config.Bind("Client Options", "Debug Send Amout", false);
            DebugTours = Config.Bind("Client Options", "DebugTours", false);
            ShowDistance = Config.Bind("Client Options", "Show Distance", false);
            DebugKeyInput = Config.Bind("Authentication", "Debug Key", "");
            ExplosionKeyInput = Config.Bind("Authentication", "Explosion Key", "");

            Logger = BepInEx.Logging.Logger.CreateLogSource("TownOfHost-K");
            TownOfHost.Logger.Enable();
            TownOfHost.Logger.Disable("NotifyRoles");
            TownOfHost.Logger.Disable("SendRPC");
            TownOfHost.Logger.Disable("ReceiveRPC");
            TownOfHost.Logger.Disable("SwitchSystem");
            TownOfHost.Logger.Disable("CustomRpcSender");
            //TownOfHost.Logger.isDetail = true;

            // 認証関連-初期化
            DebugKeyAuth = new HashAuth(DebugKeyHash, DebugKeySalt);
            ExplosionKeyAuth = new HashAuth(ExplosionKeyHash, DebugKeySalt);

            // 認証関連-認証
            DebugModeManager.Auth(DebugKeyAuth, DebugKeyInput.Value);

            winnerList = new();
            VisibleTasksCount = false;
            MessagesToSend = new List<(string, byte, string)>();

            Preset1 = Config.Bind("Preset Name Options", "Preset1", "Preset_1");
            Preset2 = Config.Bind("Preset Name Options", "Preset2", "Preset_2");
            Preset3 = Config.Bind("Preset Name Options", "Preset3", "Preset_3");
            Preset4 = Config.Bind("Preset Name Options", "Preset4", "Preset_4");
            Preset5 = Config.Bind("Preset Name Options", "Preset5", "Preset_5");
            Preset6 = Config.Bind("Preset Name Options", "Preset6", "Preset_6");
            Preset7 = Config.Bind("Preset Name Options", "Preset7", "Preset_7");
            SKey = Config.Bind("Other", "countdata", "141c2e1c");
            BetaBuildURL = Config.Bind("Other", "BetaBuildURL", "");
            MessageWait = Config.Bind("Other", "MessageWait", 1f);
            LastKillCooldown = Config.Bind("Other", "LastKillCooldown", (float)30);
            LastShapeshifterCooldown = Config.Bind("Other", "LastShapeshifterCooldown", (float)30);
            LastKickModClient = Config.Bind("Other", "LastKickModClientValue", false);

            PluginModuleInitializerAttribute.InitializeAll(true);
            Blacklist.FetchBlacklist();

            IRandom.SetInstance(new NetRandomWrapper());

            hasArgumentException = false;
            ExceptionMessage = "";

            try
            {

                roleColors = new Dictionary<CustomRoles, string>()
                {
                    // マッドメイト役職
                    {CustomRoles.SKMadmate, "#ff1919"},
                    //特殊クルー役職
                    //ニュートラル役職
                    {CustomRoles.Emptiness ,"#221d26"},
                    //HideAndSeek
                    {CustomRoles.HASFox, "#e478ff"},
                    {CustomRoles.HASTroll, "#00ff00"},
                    //TaskBattle
                    {CustomRoles.TaskPlayerB, "#9adfff"},
                    // GM
                    {CustomRoles.GM, "#ff5b70"},

                    //属性
                    {CustomRoles.LastImpostor, "#ff1919"},
                    {CustomRoles.LastNeutral,"#cccccc"},
                    {CustomRoles.Workhorse, "#00ffff"},
                    {CustomRoles.Twins, "#80cf48"},

                    {CustomRoles.watching, "#800080"},
                    {CustomRoles.Speeding, "#33ccff"},
                    {CustomRoles.Moon,"#ffff33"},
                    {CustomRoles.Guesser,"#999900"},
                    {CustomRoles.Lighting,"#ec6800"},
                    {CustomRoles.Management,"#cee4ae"},
                    {CustomRoles.Connecting,"#96514d"},
                    {CustomRoles.Serial,"#ff1919"},
                    {CustomRoles.PlusVote,"#93ca76"},
                    {CustomRoles.Opener,"#007bbb"},
                    {CustomRoles.Revenger,"#ffcc99"},
                    {CustomRoles.seeing,"#61b26c"},
                    {CustomRoles.Autopsy,"#80ffdd"},
                    {CustomRoles.Tiebreaker,"#00552e"},
                    {CustomRoles.Guarding, "#7b68ee"},
                    {CustomRoles.MagicHand , "#dea785"},
                    //{CustomRoles.AntiTeleporter,"#dea785"},
                    //デバフ
                    {CustomRoles.NonReport,"#006666"},
                    {CustomRoles.Notvoter,"#6c848d"},
                    {CustomRoles.Water,"#003f8e"},
                    {CustomRoles.Clumsy,"#942343"},
                    {CustomRoles.Slacker,"#980098"},
                    {CustomRoles.Elector,"#544a47"},
                    {CustomRoles.Transparent,"#7b7c7d"},
                    {CustomRoles.Amnesia,"#4682b4"},
                    {CustomRoles.SlowStarter,"#ff00ff"},
                    {CustomRoles.InfoPoor ,"#555647"},

                    //第三属性
                    {CustomRoles.Amanojaku,"#005243"},
                    {CustomRoles.Faction,"#695328"},
                    {CustomRoles.Lovers, "#ff6be4"},
                    {CustomRoles.RedLovers, "#d70035"},
                    {CustomRoles.YellowLovers, "#fac559"},
                    {CustomRoles.BlueLovers, "#6c9bd2"},
                    {CustomRoles.GreenLovers, "#00885a"},
                    {CustomRoles.WhiteLovers, "#fdede4"},
                    {CustomRoles.PurpleLovers, "#af0082"},
                    {CustomRoles.MadonnaLovers, "#f09199"},
                    {CustomRoles.OneLove , "#ff7961"},

                    // 幽霊役職
                    {CustomRoles.Ghostbuttoner,"#d0af4c"},
                    {CustomRoles.GhostNoiseSender, "#5aa698"},
                    {CustomRoles.GhostReseter , "#a87a71"},
                    {CustomRoles.GhostRumour , "#707cab"},
                    {CustomRoles.GuardianAngel,"#7cc0fc"},
                    {CustomRoles.DemonicTracker,"#824880"},
                    {CustomRoles.DemonicCrusher,"#522886"},
                    {CustomRoles.DemonicVenter ,"#635963"},
                    {CustomRoles.AsistingAngel,"#8da0b6"},

                    {CustomRoles.NotAssigned, "#ffffff"}
                };

                var type = typeof(RoleBase);
                var roleClassArray =
                CustomRoleManager.AllRolesClassType = Assembly.GetAssembly(type)
                    .GetTypes()
                    .Where(x => x.IsSubclassOf(type)).ToArray();

                foreach (var roleClassType in roleClassArray)
                    roleClassType.GetField("RoleInfo")?.GetValue(type);
            }
            catch (ArgumentException ex)
            {
                TownOfHost.Logger.Error("エラー:Dictionaryの値の重複を検出しました", "LoadDictionary");
                TownOfHost.Logger.Exception(ex, "LoadDictionary");
                hasArgumentException = true;
                ExceptionMessage = ex.Message;
                ExceptionMessageIsShown = false;
            }
            TownOfHost.Logger.Info($"{Application.version}", "AmongUs Version");
            TownOfHost.Logger.Info($"{ModName} v.{PluginVersion}", "ModPluginVersion");
            var handler = TownOfHost.Logger.Handler("GitVersion");
            handler.Info($"{nameof(ThisAssembly.Git.Branch)}: {ThisAssembly.Git.Branch}");
            handler.Info($"{nameof(ThisAssembly.Git.BaseTag)}: {ThisAssembly.Git.BaseTag}");
            handler.Info($"{nameof(ThisAssembly.Git.Commit)}: {ThisAssembly.Git.Commit}");
            handler.Info($"{nameof(ThisAssembly.Git.Commits)}: {ThisAssembly.Git.Commits}");
            handler.Info($"{nameof(ThisAssembly.Git.IsDirty)}: {ThisAssembly.Git.IsDirty}");
            handler.Info($"{nameof(ThisAssembly.Git.Sha)}: {ThisAssembly.Git.Sha}");
            handler.Info($"{nameof(ThisAssembly.Git.Tag)}: {ThisAssembly.Git.Tag}");

            ClassInjector.RegisterTypeInIl2Cpp<ErrorText>();

            Harmony.PatchAll();
            Application.quitting += new Action(UtilsOutputLog.SaveNowLog);
            Application.quitting += new Action(SaveStatistics.Save);
            Statistics.NowStatistics = SaveStatistics.Load();
        }

        public static bool IsCs()
        {
            if (ServerManager.Instance == null) return false;
            var sn = ServerManager.Instance.CurrentRegion.TranslateName;
            if (sn is StringNames.ServerNA or StringNames.ServerEU or StringNames.ServerAS or StringNames.ServerSA)
                return false;
            else return true;
        }

        public static bool IsPublicRoomAllowed(bool AllowCS = true)
        {
            if (!VersionChecker.IsSupported)
                return false;
            if (ModUpdater.version != null && ModUpdater.AllowPublicRoom != null)
            {
                return ModUpdater.AllowPublicRoom.Value;
            }
            return (AllowCS && IsCs()) || (!IsCs() && !ModUpdater.hasUpdate && !ModUpdater.isBroken && AllowPublicRoom && IsPublicAvailableOnThisVersion);
        }
        public static bool IsroleAssigned
            => !SetRoleOverride/* && Options.CurrentGameMode == CustomGameMode.Standard*/ || SelectRolesPatch.roleAssigned;
    }
    public enum CustomDeathReason
    {
        Kill,
        Vote,
        Suicide,
        Spell,
        FollowingSuicide,
        Bite,
        Bombed,
        Misfire,
        Torched,
        Sniped,
        Revenge,
        Revenge1,
        Revenge2,
        Execution,
        Infected,
        Grim,
        Disconnected,
        Fall,
        Magic,
        Guess,
        TeleportKill,
        Trap,
        etc = -1
    }
    //WinData
    public enum CustomWinner
    {
        Draw = -1,
        Default = -2,
        None = -3,
        Impostor = CustomRoles.Impostor,
        Crewmate = CustomRoles.Crewmate,
        Jester = CustomRoles.Jester,
        PlagueDoctor = CustomRoles.PlagueDoctor,
        Terrorist = CustomRoles.Terrorist,
        Lovers = CustomRoles.Lovers,
        RedLovers = CustomRoles.RedLovers,
        YellowLovers = CustomRoles.YellowLovers,
        BlueLovers = CustomRoles.BlueLovers,
        GreenLovers = CustomRoles.GreenLovers,
        WhiteLovers = CustomRoles.WhiteLovers,
        PurpleLovers = CustomRoles.PurpleLovers,
        MadonnaLovers = CustomRoles.MadonnaLovers,
        OneLove = CustomRoles.OneLove,
        Executioner = CustomRoles.Executioner,
        Arsonist = CustomRoles.Arsonist,
        Egoist = CustomRoles.Egoist,
        Jackal = CustomRoles.Jackal,
        Remotekiller = CustomRoles.Remotekiller,
        Chef = CustomRoles.Chef,
        Monochromer = CustomRoles.Monochromer,
        GrimReaper = CustomRoles.GrimReaper,
        CountKiller = CustomRoles.CountKiller,
        Workaholic = CustomRoles.Workaholic,
        MassMedia = CustomRoles.MassMedia,
        SantaClaus = CustomRoles.SantaClaus,
        DoppelGanger = CustomRoles.DoppelGanger,
        Vulture = CustomRoles.Vulture,
        CurseMaker = CustomRoles.CurseMaker,
        Fox = CustomRoles.Fox,
        PhantomThief = CustomRoles.PhantomThief,
        MilkyWay = CustomRoles.Vega,

        HASTroll = CustomRoles.HASTroll,
        TaskPlayerB = CustomRoles.TaskPlayerB,
        SuddenDeathRed = 1000, SuddenDeathBlue = 1001, SuddenDeathYellow = 1002, SuddenDeathGreen = 1003, SuddenDeathPurple = 1004
    }
    /*public enum CustomRoles : byte
    {
        Default = 0,
        HASTroll = 1,
        HASHox = 2
    }*/
    public enum SuffixModes
    {
        None = 0,
        TOH,
        Streaming,
        Recording,
        RoomHost,
        OriginalName,
        Timer
    }
    public enum VoteMode
    {
        Default,
        Suicide,
        SelfVote,
        Skip
    }

    public enum TieMode
    {
        Default,
        All,
        Random
    }

    public enum CombinationRoles
    {
        None,
        AssassinandMerlin,
        DriverandBraid,
        VegaandAltair
    }
}
