using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

using TownOfHost.Modules;
using TownOfHost.Roles;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.AddOns.Neutral;

namespace TownOfHost
{
    [Flags]
    public enum CustomGameMode
    {
        Standard, //= 0x01,
        HideAndSeek, //= 0x02,
        TaskBattle, //= 0x03,
        All = int.MaxValue
    }

    [HarmonyPatch]
    public static class Options
    {
        //static Task taskOptionsLoad;
        [HarmonyPatch(typeof(TranslationController), nameof(TranslationController.Initialize)), HarmonyPostfix]
        public static void OptionsLoadStart(TranslationController __instance)
        {
            Logger.Info("Options.Load Start", "Options");
            Main.UseYomiage.Value = false;
#if RELEASE
            Main.ViewPingDetails.Value = false;
            Main.DebugSendAmout.Value = false;
            Main.DebugTours.Value = false;
            Main.ShowDistance.Value = false;
            Main.DebugChatopen.Value  =false;
#endif
            //taskOptionsLoad = Task.Run(Load);
            Load();
        }
        [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPostfix]
        public static void WaitOptionsLoad()
        {
            if (IsLoaded) return;
            //taskOptionsLoad.Wait();
            Logger.Info("Options.Load End", "Options");
        }

        // プリセット
        private static readonly string[] presets =
        {
            Main.Preset1.Value, Main.Preset2.Value, Main.Preset3.Value,
            Main.Preset4.Value, Main.Preset5.Value,Main.Preset6.Value,
            Main.Preset7.Value
        };

        // ゲームモード
        public static OptionItem GameMode;
        public static CustomGameMode CurrentGameMode => (CustomGameMode)GameMode.GetValue();

        public static readonly string[] gameModes =
        {
            "Standard", "HideAndSeek","TaskBattle",
        };

        // MapActive
        public static bool IsActiveSkeld => AddedTheSkeld.GetBool() || Main.NormalOptions.MapId == 0;
        public static bool IsActiveMiraHQ => AddedMiraHQ.GetBool() || Main.NormalOptions.MapId == 1;
        public static bool IsActivePolus => AddedPolus.GetBool() || Main.NormalOptions.MapId == 2;
        public static bool IsActiveAirship => AddedTheAirShip.GetBool() || Main.NormalOptions.MapId == 4;
        public static bool IsActiveFungle => AddedTheFungle.GetBool() || Main.NormalOptions.MapId == 5;

        // 役職数・確率
        public static Dictionary<CustomRoles, OptionItem> CustomRoleCounts;
        public static Dictionary<CustomRoles, IntegerOptionItem> CustomRoleSpawnChances;
        public static readonly string[] rates =
        {
            "Rate0",  "Rate5",  "Rate10", "Rate20", "Rate30", "Rate40",
            "Rate50", "Rate60", "Rate70", "Rate80", "Rate90", "Rate100",
        };

        // 各役職の詳細設定
        public static OptionItem EnableGM;
        public static float DefaultKillCooldown = Main.NormalOptions?.KillCooldown ?? 20;
        public static OptionItem DoubleTriggerThreshold;
        public static OptionItem DefaultShapeshiftCooldown;
        public static OptionItem DefaultShapeshiftDuration;
        public static OptionItem DefaultEngineerCooldown;
        public static OptionItem DefaultEngineerInVentMaxTime;
        public static OptionItem CanMakeMadmateCount;
        public static OptionItem SkMadCanUseVent;
        public static OptionItem MadMateOption;
        public static OptionItem MadmateCanSeeKillFlash;
        public static OptionItem MadmateCanSeeDeathReason;
        public static OptionItem MadmateRevengePlayer;
        public static OptionItem MadmateRevengeCanImpostor;
        public static OptionItem MadmateRevengeNeutral;
        public static OptionItem MadmateRevengeMadmate;
        public static OptionItem MadmateRevengeCrewmate;
        public static OptionItem MadCanSeeImpostor;
        public static OptionItem MadmateCanFixLightsOut;
        public static OptionItem MadmateCanFixComms;
        public static OptionItem MadmateHasLighting;
        public static OptionItem MadmateHasMoon;
        public static OptionItem MadmateCanSeeOtherVotes;
        public static OptionItem MadmateTell;
        static string[] Tellopt =
        {"NoProcessing","Crewmate","Madmate","Impostor"};
        public static CustomRoles MadTellOpt()
        {
            switch (Tellopt[MadmateTell.GetValue()])
            {
                case "NoProcessing": return CustomRoles.NotAssigned;
                case "Crewmate": return CustomRoles.Crewmate;
                case "Madmate": return CustomRoles.Madmate;
                case "Impostor": return CustomRoles.Impostor;
            }
            return CustomRoles.NotAssigned;
        }
        public static OptionItem MadmateVentCooldown;
        public static OptionItem MadmateVentMaxTime;
        public static OptionItem MadmateCanMovedByVent;

        //試験的機能
        public static OptionItem ExperimentalMode;
        public static OptionItem ExAftermeetingflash;
        public static OptionItem ExHideChatCommand;
        public static OptionItem FixSpawnPacketSize;
        public static OptionItem ExIntroWeight;
        public static OptionItem ExRpcWeightR;
        public static OptionItem ExCallMeetingBlackout;

        //幽霊役職
        public static OptionItem GhostRoleOption;
        public static OptionItem GhostRoleCanSeeOtherRoles;
        public static OptionItem GhostRoleCanSeeOtherTasks;
        public static OptionItem GhostRoleCanSeeOtherVotes;
        public static OptionItem GhostRoleCanSeeDeathReason;
        public static OptionItem GhostRoleCanSeeKillerColor;
        public static OptionItem GhostRoleCanSeeAllTasks;
        public static OptionItem GhostRoleCanSeeKillflash;
        public static OptionItem GhostRoleCanSeeNumberOfButtonsOnOthers;

        public static OptionItem KillFlashDuration;

        // HideAndSeek
        public static OptionItem AllowCloseDoors;
        public static OptionItem KillDelay;
        public static OptionItem IgnoreVent;
        public static float HideAndSeekKillDelayTimer = 0f;
        //特殊モード
        public static OptionItem ONspecialMode;
        public static OptionItem ColorNameMode;
        public static OptionItem InsiderMode;
        public static OptionItem InsiderModeCanSeeTask;
        public static OptionItem CanSeeImpostorRole;
        public static OptionItem AllPlayerSkinShuffle;

        public static OptionItem UploadDataIsLongTask;
        // タスク無効化
        public static OptionItem DisableTasks;
        public static OptionItem DisableSwipeCard;
        public static OptionItem DisableSubmitScan;
        public static OptionItem DisableUnlockSafe;
        public static OptionItem DisableUploadData;
        public static OptionItem DisableStartReactor;
        public static OptionItem DisableResetBreaker;
        public static OptionItem DisableCatchFish;
        public static OptionItem DisableDivertPower;
        public static OptionItem DisableFuelEngins;
        public static OptionItem DisableInspectSample;
        public static OptionItem DisableRebootWifi;
        public static OptionItem DisableFixWeatherNode;
        //
        public static OptionItem DisableInseki;
        public static OptionItem DisableCalibrateDistributor;
        public static OptionItem DisableVentCleaning;
        public static OptionItem DisableHelpCritter;
        public static OptionItem Disablefixwiring;
        //デバイスブロック
        public static OptionItem DevicesOption;
        public static OptionItem DisableDevices;
        public static OptionItem DisableSkeldDevices;
        public static OptionItem DisableSkeldAdmin;
        public static OptionItem DisableSkeldCamera;
        public static OptionItem DisableMiraHQDevices;
        public static OptionItem DisableMiraHQAdmin;
        public static OptionItem DisableMiraHQDoorLog;
        public static OptionItem DisablePolusDevices;
        public static OptionItem DisablePolusAdmin;
        public static OptionItem DisablePolusCamera;
        public static OptionItem DisablePolusVital;
        public static OptionItem DisableAirshipDevices;
        public static OptionItem DisableAirshipCockpitAdmin;
        public static OptionItem DisableAirshipRecordsAdmin;
        public static OptionItem DisableAirshipCamera;
        public static OptionItem DisableAirshipVital;
        public static OptionItem DisableFungleDevices;
        public static OptionItem DisableFungleVital;
        public static OptionItem DisableDevicesIgnoreConditions;
        public static OptionItem DisableDevicesIgnoreImpostors;
        public static OptionItem DisableDevicesIgnoreMadmates;
        public static OptionItem DisableDevicesIgnoreNeutrals;
        public static OptionItem DisableDevicesIgnoreCrewmates;
        public static OptionItem DisableDevicesIgnoreAfterAnyoneDied;

        public static OptionItem TimeLimitDevices;
        public static OptionItem TimeLimitAdmin;
        public static OptionItem TimeLimitCamAndLog;
        public static OptionItem TimeLimitVital;
        public static OptionItem CanSeeTimeLimit;
        public static OptionItem CanseeImpTimeLimit;
        public static OptionItem CanseeMadTimeLimit;
        public static OptionItem CanseeCrewTimeLimit;
        public static OptionItem CanseeNeuTimeLimit;

        public static OptionItem TurnTimeLimitDevice;
        public static OptionItem TurnTimeLimitAdmin;
        public static OptionItem TurnTimeLimitCamAndLog;
        public static OptionItem TurnTimeLimitVital;

        // ランダムマップ
        public static OptionItem RandomMapsMode;
        public static OptionItem AddedTheSkeld;
        public static OptionItem AddedMiraHQ;
        public static OptionItem AddedPolus;
        public static OptionItem AddedTheAirShip;
        public static OptionItem AddedTheFungle;
        // public static OptionItem AddedDleks;

        // ランダムスポーン
        public static OptionItem EnableRandomSpawn;
        public static OptionItem CanSeeNextRandomSpawn;
        //Skeld
        public static OptionItem RandomSpawnSkeld;
        public static OptionItem RandomSpawnSkeldCafeteria;
        public static OptionItem RandomSpawnSkeldWeapons;
        public static OptionItem RandomSpawnSkeldLifeSupp;
        public static OptionItem RandomSpawnSkeldNav;
        public static OptionItem RandomSpawnSkeldShields;
        public static OptionItem RandomSpawnSkeldComms;
        public static OptionItem RandomSpawnSkeldStorage;
        public static OptionItem RandomSpawnSkeldAdmin;
        public static OptionItem RandomSpawnSkeldElectrical;
        public static OptionItem RandomSpawnSkeldLowerEngine;
        public static OptionItem RandomSpawnSkeldUpperEngine;
        public static OptionItem RandomSpawnSkeldSecurity;
        public static OptionItem RandomSpawnSkeldReactor;
        public static OptionItem RandomSpawnSkeldMedBay;
        //Mira
        public static OptionItem RandomSpawnMira;
        public static OptionItem RandomSpawnMiraCafeteria;
        public static OptionItem RandomSpawnMiraBalcony;
        public static OptionItem RandomSpawnMiraStorage;
        public static OptionItem RandomSpawnMiraJunction;
        public static OptionItem RandomSpawnMiraComms;
        public static OptionItem RandomSpawnMiraMedBay;
        public static OptionItem RandomSpawnMiraLockerRoom;
        public static OptionItem RandomSpawnMiraDecontamination;
        public static OptionItem RandomSpawnMiraLaboratory;
        public static OptionItem RandomSpawnMiraReactor;
        public static OptionItem RandomSpawnMiraLaunchpad;
        public static OptionItem RandomSpawnMiraAdmin;
        public static OptionItem RandomSpawnMiraOffice;
        public static OptionItem RandomSpawnMiraGreenhouse;
        //Polus
        public static OptionItem RandomSpawnPolus;
        public static OptionItem RandomSpawnPolusOfficeLeft;
        public static OptionItem RandomSpawnPolusOfficeRight;
        public static OptionItem RandomSpawnPolusAdmin;
        public static OptionItem RandomSpawnPolusComms;
        public static OptionItem RandomSpawnPolusWeapons;
        public static OptionItem RandomSpawnPolusBoilerRoom;
        public static OptionItem RandomSpawnPolusLifeSupp;
        public static OptionItem RandomSpawnPolusElectrical;
        public static OptionItem RandomSpawnPolusSecurity;
        public static OptionItem RandomSpawnPolusDropship;
        public static OptionItem RandomSpawnPolusStorage;
        public static OptionItem RandomSpawnPolusRocket;
        public static OptionItem RandomSpawnPolusLaboratory;
        public static OptionItem RandomSpawnPolusToilet;
        public static OptionItem RandomSpawnPolusSpecimens;
        //AIrShip
        public static OptionItem RandomSpawnAirship;
        public static OptionItem RandomSpawnAirshipBrig;
        public static OptionItem RandomSpawnAirshipEngine;
        public static OptionItem RandomSpawnAirshipKitchen;
        public static OptionItem RandomSpawnAirshipCargoBay;
        public static OptionItem RandomSpawnAirshipRecords;
        public static OptionItem RandomSpawnAirshipMainHall;
        public static OptionItem RandomSpawnAirshipNapRoom;
        public static OptionItem RandomSpawnAirshipMeetingRoom;
        public static OptionItem RandomSpawnAirshipGapRoom;
        public static OptionItem RandomSpawnAirshipVaultRoom;
        public static OptionItem RandomSpawnAirshipComms;
        public static OptionItem RandomSpawnAirshipCockpit;
        public static OptionItem RandomSpawnAirshipArmory;
        public static OptionItem RandomSpawnAirshipViewingDeck;
        public static OptionItem RandomSpawnAirshipSecurity;
        public static OptionItem RandomSpawnAirshipElectrical;
        public static OptionItem RandomSpawnAirshipMedical;
        public static OptionItem RandomSpawnAirshipToilet;
        public static OptionItem RandomSpawnAirshipShowers;
        //Fungle
        public static OptionItem RandomSpawnFungle;
        public static OptionItem RandomSpawnFungleKitchen;
        public static OptionItem RandomSpawnFungleBeach;
        public static OptionItem RandomSpawnFungleCafeteria;
        public static OptionItem RandomSpawnFungleRecRoom;
        public static OptionItem RandomSpawnFungleBonfire;
        public static OptionItem RandomSpawnFungleDropship;
        public static OptionItem RandomSpawnFungleStorage;
        public static OptionItem RandomSpawnFungleMeetingRoom;
        public static OptionItem RandomSpawnFungleSleepingQuarters;
        public static OptionItem RandomSpawnFungleLaboratory;
        public static OptionItem RandomSpawnFungleGreenhouse;
        public static OptionItem RandomSpawnFungleReactor;
        public static OptionItem RandomSpawnFungleJungleTop;
        public static OptionItem RandomSpawnFungleJungleBottom;
        public static OptionItem RandomSpawnFungleLookout;
        public static OptionItem RandomSpawnFungleMiningPit;
        public static OptionItem RandomSpawnFungleHighlands;
        public static OptionItem RandomSpawnFungleUpperEngine;
        public static OptionItem RandomSpawnFunglePrecipice;
        public static OptionItem RandomSpawnFungleComms;

        // CustomSpawn
        public static OptionItem CustomSpawn;
        public static OptionItem RandomSpawnCustom1;
        public static OptionItem RandomSpawnCustom2;
        public static OptionItem RandomSpawnCustom3;
        public static OptionItem RandomSpawnCustom4;
        public static OptionItem RandomSpawnCustom5;
        public static OptionItem RandomSpawnCustom6;
        public static OptionItem RandomSpawnCustom7;
        public static OptionItem RandomSpawnCustom8;
        public static OptionItem MeetingAndVoteOpt;

        public static OptionItem ShowVoteResult;
        public static OptionItem ShowVoteJudgment;
        public static readonly string[] ShowVoteJudgments =
        {
            "Impostor","Neutral", "CrewMate(Mad)", "Crewmate","Role","ShowTeam"
        };
        // 投票モード
        public static OptionItem VoteMode;
        public static OptionItem WhenSkipVote;
        public static OptionItem WhenSkipVoteIgnoreFirstMeeting;
        public static OptionItem WhenSkipVoteIgnoreNoDeadBody;
        public static OptionItem WhenSkipVoteIgnoreEmergency;
        public static OptionItem WhenNonVote;
        public static OptionItem WhenTie;
        public static readonly string[] voteModes =
        {
            "Default", "Suicide", "SelfVote", "Skip"
        };
        public static readonly string[] tieModes =
        {
            "TieMode.Default", "TieMode.All", "TieMode.Random"
        };
        public static VoteMode GetWhenSkipVote() => (VoteMode)WhenSkipVote.GetValue();
        public static VoteMode GetWhenNonVote() => (VoteMode)WhenNonVote.GetValue();

        // ボタン回数
        public static OptionItem SyncButtonMode;
        public static OptionItem SyncedButtonCount;
        public static int UsedButtonCount = 0;

        // 全員生存時の会議時間
        public static OptionItem AllAliveMeeting;
        public static OptionItem AllAliveMeetingTime;

        // 追加の緊急ボタンクールダウン
        public static OptionItem AdditionalEmergencyCooldown;
        public static OptionItem AdditionalEmergencyCooldownThreshold;
        public static OptionItem AdditionalEmergencyCooldownTime;

        //会議時間
        public static OptionItem LowerLimitVotingTime;
        public static OptionItem MeetingTimeLimit;

        //転落死
        public static OptionItem LadderDeath;
        public static OptionItem LadderDeathChance;
        public static OptionItem LadderDeathNuuun;
        public static OptionItem LadderDeathZipline;

        // 通常モードでかくれんぼ
        public static bool IsStandardHAS => StandardHAS.GetBool() && CurrentGameMode == CustomGameMode.Standard;
        public static OptionItem StandardHAS;
        public static OptionItem StandardHASWaitingTime;

        // リアクターの時間制御
        public static OptionItem SabotageActivetimerControl;
        public static OptionItem SkeldReactorTimeLimit;
        public static OptionItem SkeldO2TimeLimit;
        public static OptionItem MiraReactorTimeLimit;
        public static OptionItem MiraO2TimeLimit;
        public static OptionItem PolusReactorTimeLimit;
        public static OptionItem AirshipReactorTimeLimit;
        public static OptionItem FungleReactorTimeLimit;
        public static OptionItem FungleMushroomMixupDuration;

        // サボタージュのクールダウン変更
        public static OptionItem ModifySabotageCooldown;
        public static OptionItem SabotageCooldown;

        // 停電の特殊設定
        public static OptionItem LightsOutSpecialSettings;
        public static OptionItem LightOutDonttouch;
        public static OptionItem LightOutDonttouchTime;
        public static OptionItem DisableAirshipViewingDeckLightsPanel;
        public static OptionItem DisableAirshipGapRoomLightsPanel;
        public static OptionItem DisableAirshipCargoLightsPanel;
        public static OptionItem BlockDisturbancesToSwitches;
        public static OptionItem CommsSpecialSettings;
        public static OptionItem CommsCamouflage;
        public static OptionItem CommsDonttouch;
        public static OptionItem CommsDonttouchTime;
        // 他サボ
        public static OptionItem ChangeSabotageWinRole;
        // マップ改造
        public static OptionItem Sabotage;
        public static OptionItem MapModification;
        public static OptionItem AirShipVariableElectrical;
        public static OptionItem AirShipPlatform;
        public static OptionItem DisableAirshipMovingPlatform;
        public static OptionItem CantUseVentMode;
        public static OptionItem CantUseVentTrueCount;
        public static OptionItem MaxInVentMode;
        public static OptionItem MaxInVentTime;
        public static OptionItem ResetDoorsEveryTurns;
        public static OptionItem DoorsResetMode;
        public static OptionItem DisableFungleSporeTrigger;
        public static OptionItem CantUseZipLineTotop;
        public static OptionItem CantUseZipLineTodown;
        public static string[] PlatformOption =
        {
            "ColoredOff" , "AssignAlgorithm.Random" , "PlatfromLeft" , "PlatfromRight"
        };
        // その他
        public static OptionItem ConvenientOptions;
        public static OptionItem FirstTurnMeeting;
        public static bool firstturnmeeting;
        public static OptionItem FirstTurnMeetingCantability;
        public static OptionItem FixFirstKillCooldown;
        public static OptionItem CanseeVoteresult;
        public static OptionItem CommnTaskResetAssing;
        public static OptionItem OutroCrewWinreasonchenge;
        public static OptionItem TeamHideChat;
        public static OptionItem ImpostorHideChat;
        public static OptionItem LoversHideChat;
        public static OptionItem JackalHideChat;
        public static OptionItem TwinsHideChat;
        public static OptionItem ConnectingHideChat;

        public static OptionItem DisableTaskWin;

        public static OptionItem GhostOptions;
        public static OptionItem GhostCanSeeOtherRoles;
        public static OptionItem GhostCanSeeOtherTasks;
        public static OptionItem GhostCanSeeOtherVotes;
        public static OptionItem GhostCanSeeDeathReason;
        public static OptionItem GhostCanSeeKillerColor;
        public static OptionItem GhostIgnoreTasks;
        public static OptionItem GhostCanSeeAllTasks;
        public static OptionItem GhostCanSeeKillflash;
        public static OptionItem GhostCanSeeNumberOfButtonsOnOthers;

        // プリセット対象外
        public static OptionItem NoGameEnd;
        public static OptionItem AutoDisplayLastResult;
        public static OptionItem AutoDisplayKillLog;
        public static OptionItem SuffixMode;
        public static OptionItem HideGameSettings;
        public static OptionItem HideSettingsDuringGame;
        public static OptionItem ChangeNameToRoleInfo;
        public static OptionItem RoleAssigningAlgorithm;
        public static OptionItem UseZoom;

        public static OptionItem ApplyDenyNameList;
        public static OptionItem KickPlayerFriendCodeNotExist;
        public static OptionItem ApplyBanList;
        public static OptionItem KiclHotNotFriend;

        public static readonly string[] suffixModes =
        {
            "SuffixMode.None",
            "SuffixMode.Version",
            "SuffixMode.Streaming",
            "SuffixMode.Recording",
            "SuffixMode.RoomHost",
            "SuffixMode.OriginalName",
            "SuffixMode.Timer"
        };
        public static readonly string[] RoleAssigningAlgorithms =
        {
            "RoleAssigningAlgorithm.Default",
            "RoleAssigningAlgorithm.NetRandom",
            "RoleAssigningAlgorithm.HashRandom",
            "RoleAssigningAlgorithm.Xorshift",
            "RoleAssigningAlgorithm.MersenneTwister",
        };
        public static SuffixModes GetSuffixMode()
        {
            return (SuffixModes)SuffixMode.GetValue();
        }

        public static int SnitchExposeTaskLeft = 1;

        public static bool IsLoaded = false;
        public static int GetRoleCount(CustomRoles role)
        {
            return GetRoleChance(role) == 0 ? 0 : CustomRoleCounts.TryGetValue(role, out var option) ? option.GetInt() : 0;
        }

        public static int GetRoleChance(CustomRoles role)
        {
            return CustomRoleSpawnChances.TryGetValue(role, out var option) ? option.GetInt() : 0;
        }
        public static void Load()
        {
            if (IsLoaded) return;
            OptionSaver.Initialize();
            // プリセット
            PresetOptionItem.Preset = PresetOptionItem.Create(0, TabGroup.MainSettings)
                .SetColor(new Color32(204, 204, 0, 255))
                .SetHeader(true)
                .SetGameMode(CustomGameMode.All);

            // ゲームモード
            GameMode = StringOptionItem.Create(1, "GameMode", gameModes, 0, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.All)
                .SetColor(ModColors.bluegreen);

            #region 役職・詳細設定
            CustomRoleCounts = new();
            CustomRoleSpawnChances = new();

            var sortedRoleInfo = CustomRoleManager.AllRolesInfo.Values.OrderBy(role => role.ConfigId);
            // GM
            EnableGM = BooleanOptionItem.Create(100, "GM", false, TabGroup.MainSettings, false)
                .SetColor(UtilsRoleText.GetRoleColor(CustomRoles.GM))
                .SetHeader(true);

            RoleAssignManager.SetupOptionItem();
            WinOption.SetupCustomOption();

            //タスクバトル
            TaskBattle.SetupOptionItem();

            //特殊モード
            ONspecialMode = BooleanOptionItem.Create(100000, "ONspecialMode", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetColorcode("#00c1ff");
            InsiderMode = BooleanOptionItem.Create(100001, "InsiderMode", false, TabGroup.MainSettings, false).SetParent(ONspecialMode)
                .SetGameMode(CustomGameMode.Standard);
            InsiderModeCanSeeTask = BooleanOptionItem.Create(200002, "InsiderModeCanSeeTask", false, TabGroup.MainSettings, false).SetParent(InsiderMode);
            ColorNameMode = BooleanOptionItem.Create(100003, "ColorNameMode", false, TabGroup.MainSettings, false).SetParent(ONspecialMode)
                .SetGameMode(CustomGameMode.All);
            CanSeeImpostorRole = BooleanOptionItem.Create(100004, "CanSeeImpostorRole", false, TabGroup.MainSettings, false).SetParent(ONspecialMode)
                .SetGameMode(CustomGameMode.Standard);
            AllPlayerSkinShuffle = BooleanOptionItem.Create(100005, "AllPlayerSkinShuffle", false, TabGroup.MainSettings, false).SetParent(ONspecialMode)
                .SetEnabled(() => Event.April || Event.Special).SetInfo(Translator.GetString("AprilfoolOnly"));
            StandardHAS = BooleanOptionItem.Create(100006, "StandardHAS", false, TabGroup.MainSettings, false).SetParent(ONspecialMode)
            .SetGameMode(CustomGameMode.Standard);
            StandardHASWaitingTime = FloatOptionItem.Create(100007, "StandardHASWaitingTime", new(0f, 180f, 2.5f), 10f, TabGroup.MainSettings, false).SetParent(StandardHAS)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);
            //最初のオプションのみここ
            SuddenDeathMode.SuddenDeathModeActive = BooleanOptionItem.Create(101000, "SuddenDeathMode", false, TabGroup.MainSettings, false).SetParent(ONspecialMode).SetGameMode(CustomGameMode.Standard);
            SuddenDeathMode.CreateOption();

            // 試験的機能
            ExperimentalMode = BooleanOptionItem.Create(105000, "ExperimentalMode", false, TabGroup.MainSettings, false).SetColor(Palette.CrewmateSettingChangeText)
                .SetGameMode(CustomGameMode.Standard);
            ExAftermeetingflash = BooleanOptionItem.Create(105001, "ExAftermeetingflash", false, TabGroup.MainSettings, false).SetParent(ExperimentalMode)
                            .SetGameMode(CustomGameMode.Standard);
            ExHideChatCommand = BooleanOptionItem.Create(105002, "ExHideChatCommand", false, TabGroup.MainSettings, false).SetParent(ExperimentalMode)
                            .SetGameMode(CustomGameMode.Standard)
                            .SetInfo(Translator.GetString("ExHideChatCommandInfo"));
            TeamHideChat = BooleanOptionItem.Create(105003, "TeamHideChat", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetParent(ExHideChatCommand);
            ImpostorHideChat = BooleanOptionItem.Create(105004, "ImpostorHideChat", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColor(ModColors.ImpostorRed).SetParent(TeamHideChat);
            JackalHideChat = BooleanOptionItem.Create(105005, "JackalHideChat", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColor(UtilsRoleText.GetRoleColor(CustomRoles.Jackal)).SetParent(TeamHideChat);
            LoversHideChat = BooleanOptionItem.Create(105006, "LoversHideChat", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColor(UtilsRoleText.GetRoleColor(CustomRoles.Lovers)).SetParent(TeamHideChat);
            TwinsHideChat = BooleanOptionItem.Create(105007, "TwinsCanUseHideChet", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColor(UtilsRoleText.GetRoleColor(CustomRoles.Twins)).SetParent(TeamHideChat);
            ConnectingHideChat = BooleanOptionItem.Create(105008, "ConnectingHideChat", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColor(UtilsRoleText.GetRoleColor(CustomRoles.Connecting)).SetParent(TeamHideChat);
            ExRpcWeightR = BooleanOptionItem.Create(105009, "ExRpcWeightR", false, TabGroup.MainSettings, false).SetParent(ExperimentalMode);
            ExCallMeetingBlackout = BooleanOptionItem.Create(105012, "ExCallMeetingBlackout", false, TabGroup.MainSettings, false).SetParent(ExperimentalMode);

            //9人以上部屋で落ちる現象の対策
            FixSpawnPacketSize = BooleanOptionItem.Create(105010, "FixSpawnPacketSize", false, TabGroup.MainSettings, true)
                .SetColor(new Color32(255, 255, 0, 255))
                .SetGameMode(CustomGameMode.All)
                .SetInfo(Translator.GetString("FixSpawnPacketSizeInfo"));
            ExIntroWeight = BooleanOptionItem.Create(105011, "ExIntroWeight", false, TabGroup.MainSettings, false)
                .SetColor(new Color32(byte.MaxValue, byte.MaxValue, 0, byte.MaxValue))
                .SetGameMode(CustomGameMode.All)
                .SetInfo(Translator.GetString("ExIntroWeightInfo"));

            // Impostor
            CreateRoleOption(sortedRoleInfo, CustomRoleTypes.Impostor);

            DoubleTriggerThreshold = FloatOptionItem.Create(102500, "DoubleTriggerThreashould", new(0.3f, 1f, 0.1f), 0.5f, TabGroup.ImpostorRoles, false)
                .SetHeader(true)
                .SetValueFormat(OptionFormat.Seconds);
            DefaultShapeshiftCooldown = FloatOptionItem.Create(102501, "DefaultShapeshiftCooldown", new(1f, 999f, 1f), 15f, TabGroup.ImpostorRoles, false)
                .SetHeader(true)
                .SetValueFormat(OptionFormat.Seconds);
            DefaultShapeshiftDuration = FloatOptionItem.Create(102502, "DefaultShapeshiftDuration", new(1, 300, 1f), 10, TabGroup.ImpostorRoles, false)
                .SetValueFormat(OptionFormat.Seconds);

            // Madmate, Crewmate, Neutral
            CreateRoleOption(sortedRoleInfo, CustomRoleTypes.Madmate);
            CreateRoleOption(sortedRoleInfo, CustomRoleTypes.Crewmate);
            DefaultEngineerCooldown = FloatOptionItem.Create(102503, "DefaultEngineerCooldown", new(0, 180, 1f), 15, TabGroup.CrewmateRoles, false)
                .SetHeader(true).SetValueFormat(OptionFormat.Seconds);
            DefaultEngineerInVentMaxTime = FloatOptionItem.Create(102504, "DefaultEngineerInVentMaxTime", new(0, 180, 1), 5, TabGroup.CrewmateRoles, false)
                .SetValueFormat(OptionFormat.Seconds).SetZeroNotation(OptionZeroNotation.Infinity);

            CreateRoleOption(sortedRoleInfo, CustomRoleTypes.Neutral);

            SetupRoleOptions(102800, TabGroup.MainSettings, CustomRoles.NotAssigned, new(1, 1, 1));
            RoleAddAddons.Create(102810, TabGroup.MainSettings, CustomRoles.NotAssigned);
            // Madmate Common Options
            CanMakeMadmateCount = IntegerOptionItem.Create(102000, "CanMakeMadmateCount", new(0, 15, 1), 0, TabGroup.MadmateRoles, false)
                .SetValueFormat(OptionFormat.Players)
                .SetHeader(true)
                .SetColor(Palette.ImpostorRed);
            SkMadCanUseVent = BooleanOptionItem.Create(102001, "SkMadCanUseVent", false, TabGroup.MadmateRoles, false)
                .SetParent(CanMakeMadmateCount);
            MadMateOption = BooleanOptionItem.Create(102002, "MadmateOption", false, TabGroup.MadmateRoles, false)
                .SetHeader(true)
                .SetColorcode("#ffa3a3");
            MadmateCanFixLightsOut = BooleanOptionItem.Create(102003, "MadmateCanFixLightsOut", false, TabGroup.MadmateRoles, false).SetColorcode("#ffcc66").SetParent(MadMateOption);
            MadmateCanFixComms = BooleanOptionItem.Create(102004, "MadmateCanFixComms", false, TabGroup.MadmateRoles, false).SetColorcode("#999999").SetParent(MadMateOption);
            MadmateHasLighting = BooleanOptionItem.Create(102005, "MadmateHasLighting", false, TabGroup.MadmateRoles, false).SetColorcode("#ec6800").SetParent(MadMateOption);
            MadmateHasMoon = BooleanOptionItem.Create(102006, "MadmateHasMoon", false, TabGroup.MadmateRoles, false).SetColorcode("#ffff33").SetParent(MadMateOption);

            MadmateCanSeeKillFlash = BooleanOptionItem.Create(102007, "MadmateCanSeeKillFlash", false, TabGroup.MadmateRoles, false).SetColorcode("#61b26c").SetParent(MadMateOption);
            MadmateCanSeeOtherVotes = BooleanOptionItem.Create(102008, "MadmateCanSeeOtherVotes", false, TabGroup.MadmateRoles, false).SetColorcode("#800080").SetParent(MadMateOption);
            MadmateCanSeeDeathReason = BooleanOptionItem.Create(102009, "MadmateCanSeeDeathReason", false, TabGroup.MadmateRoles, false).SetColorcode("#80ffdd").SetParent(MadMateOption);
            MadmateRevengePlayer = BooleanOptionItem.Create(102010, "MadmateExileCrewmate", false, TabGroup.MadmateRoles, false).SetColorcode("#00fa9a").SetParent(MadMateOption);
            MadmateRevengeCanImpostor = BooleanOptionItem.Create(102011, "NekoKabochaImpostorsGetRevenged", false, TabGroup.MadmateRoles, false).SetParent(MadmateRevengePlayer);
            MadmateRevengeCrewmate = BooleanOptionItem.Create(102012, "RevengeToCrewmate", true, TabGroup.MadmateRoles, false).SetParent(MadmateRevengePlayer);
            MadmateRevengeMadmate = BooleanOptionItem.Create(102013, "NekoKabochaMadmatesGetRevenged", true, TabGroup.MadmateRoles, false).SetParent(MadmateRevengePlayer);
            MadmateRevengeNeutral = BooleanOptionItem.Create(102014, "RevengeToNeutral", true, TabGroup.MadmateRoles, false).SetParent(MadmateRevengePlayer);
            MadCanSeeImpostor = BooleanOptionItem.Create(102015, "MadmateCanSeeImpostor", false, TabGroup.MadmateRoles, false).SetColor(UtilsRoleText.GetRoleColor(CustomRoles.Snitch)).SetParent(MadMateOption);
            MadmateTell = StringOptionItem.Create(102016, "MadmateTellOption", Tellopt, 0, TabGroup.MadmateRoles, false).SetColor(UtilsRoleText.GetRoleColor(CustomRoles.FortuneTeller)).SetParent(MadMateOption);

            MadmateVentCooldown = FloatOptionItem.Create(102017, "MadmateVentCooldown", new(0f, 180f, 0.5f), 0f, TabGroup.MadmateRoles, false).SetColorcode("#8cffff").SetParent(MadMateOption)
                .SetHeader(true)
                .SetValueFormat(OptionFormat.Seconds);
            MadmateVentMaxTime = FloatOptionItem.Create(102018, "MadmateVentMaxTime", new(0f, 180f, 0.5f), 0f, TabGroup.MadmateRoles, false).SetZeroNotation(OptionZeroNotation.Infinity).SetColorcode("#8cffff").SetParent(MadMateOption)
                .SetValueFormat(OptionFormat.Seconds);
            MadmateCanMovedByVent = BooleanOptionItem.Create(102019, "MadmateCanMovedByVent", true, TabGroup.MadmateRoles, false).SetColorcode("#8cffff").SetParent(MadMateOption);

            //Com
            Faction.SetUpOption();
            Twins.SetUpTwinsOptions();
            Lovers.SetLoversOptions();
            GhostRoleCore.SetupCustomOptionAddonAndIsGhostRole();

            //幽霊役職の設定
            GhostRoleOption = BooleanOptionItem.Create(106000, "GhostRoleOptions", false, TabGroup.GhostRoles, false)
                .SetHeader(true)
                .SetColorcode("#666699")
                .SetGameMode(CustomGameMode.All);
            GhostRoleCanSeeOtherRoles = BooleanOptionItem.Create(106001, "GhostRoleCanSeeOtherRoles", false, TabGroup.GhostRoles, false)
                .SetColorcode("#7474ab")
                .SetGameMode(CustomGameMode.All)
                .SetParent(GhostRoleOption);
            GhostRoleCanSeeOtherTasks = BooleanOptionItem.Create(106002, "GhostRoleCanSeeOtherTasks", false, TabGroup.GhostRoles, false)
                .SetGameMode(CustomGameMode.All)
                .SetColor(Color.yellow)
                .SetParent(GhostRoleOption);
            GhostRoleCanSeeOtherVotes = BooleanOptionItem.Create(106003, "GhostRoleCanSeeOtherVotes", false, TabGroup.GhostRoles, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#800080")
                .SetParent(GhostRoleOption);
            GhostRoleCanSeeDeathReason = BooleanOptionItem.Create(106004, "GhostRoleCanSeeDeathReason", false, TabGroup.GhostRoles, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#80ffdd")
                .SetParent(GhostRoleOption);
            GhostRoleCanSeeKillerColor = BooleanOptionItem.Create(106005, "GhostRoleCanSeeKillerColor", false, TabGroup.GhostRoles, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#80ffdd")
                .SetParent(GhostRoleCanSeeDeathReason);
            GhostRoleCanSeeAllTasks = BooleanOptionItem.Create(106006, "GhostRoleCanSeeAllTasks", false, TabGroup.GhostRoles, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#cee4ae")
                .SetParent(GhostRoleOption);
            GhostRoleCanSeeNumberOfButtonsOnOthers = BooleanOptionItem.Create(106007, "GhostRoleCanSeeNumberOfButtonsOnOthers", false, TabGroup.GhostRoles, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#d7c447")
                .SetParent(GhostRoleOption);
            GhostRoleCanSeeKillflash = BooleanOptionItem.Create(106008, "GhostRoleCanSeeKillflash", false, TabGroup.GhostRoles, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#61b26c")
                .SetParent(GhostRoleOption);
            #endregion

            KillFlashDuration = FloatOptionItem.Create(90000, "KillFlashDuration", new(0.1f, 0.45f, 0.05f), 0.3f, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#bf483f");

            // HideAndSeek
            SetupRoleOptions(112000, TabGroup.MainSettings, CustomRoles.HASFox, customGameMode: CustomGameMode.HideAndSeek);
            SetupRoleOptions(112100, TabGroup.MainSettings, CustomRoles.HASTroll, customGameMode: CustomGameMode.HideAndSeek);
            KillDelay = FloatOptionItem.Create(112200, "HideAndSeekWaitingTime", new(0f, 180f, 5f), 10f, TabGroup.MainSettings, false)
                .SetValueFormat(OptionFormat.Seconds)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.HideAndSeek);
            IgnoreVent = BooleanOptionItem.Create(112002, "IgnoreVent", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.HideAndSeek);

            // マップ改造
            MapModification = BooleanOptionItem.Create(107000, "MapModification", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetColorcode("#ccff66");
            AirShipVariableElectrical = BooleanOptionItem.Create(107001, "AirShipVariableElectrical", false, TabGroup.MainSettings, false).SetParent(MapModification)
                .SetEnabled(() => IsActiveAirship);
            AirShipPlatform = StringOptionItem.Create(107002, "AirShipPlatform", PlatformOption, 0, TabGroup.MainSettings, false).SetParent(MapModification)
                .SetEnabled(() => IsActiveAirship);
            DisableAirshipMovingPlatform = BooleanOptionItem.Create(107003, "DisableAirshipMovingPlatform", false, TabGroup.MainSettings, false).SetParent(MapModification)
                .SetEnabled(() => IsActiveAirship);
            DisableFungleSporeTrigger = BooleanOptionItem.Create(107004, "DisableFungleSporeTrigger", false, TabGroup.MainSettings, false).SetParent(MapModification)
                .SetEnabled(() => IsActiveFungle);
            CantUseZipLineTotop = BooleanOptionItem.Create(107005, "CantUseZipLineTotop", false, TabGroup.MainSettings, false).SetParent(MapModification)
                .SetEnabled(() => IsActiveFungle);
            CantUseZipLineTodown = BooleanOptionItem.Create(107006, "CantUseZipLineTodown", false, TabGroup.MainSettings, false).SetParent(MapModification)
                .SetEnabled(() => IsActiveFungle);
            ResetDoorsEveryTurns = BooleanOptionItem.Create(107007, "ResetDoorsEveryTurns", false, TabGroup.MainSettings, false).SetParent(MapModification)
                .SetEnabled(() => IsActiveAirship || IsActiveFungle || IsActivePolus);
            DoorsResetMode = StringOptionItem.Create(107008, "DoorsResetMode", EnumHelper.GetAllNames<DoorsReset.ResetMode>(), 0, TabGroup.MainSettings, false).SetParent(ResetDoorsEveryTurns);
            CantUseVentMode = BooleanOptionItem.Create(107009, "Can'tUseVent", false, TabGroup.MainSettings, false).SetParent(MapModification);
            CantUseVentTrueCount = IntegerOptionItem.Create(107010, "CantUseVentTrueCount", new(1, 15, 1), 5, TabGroup.MainSettings, false).SetValueFormat(OptionFormat.Players).SetParent(CantUseVentMode);
            MaxInVentMode = BooleanOptionItem.Create(107011, "MaxInVentMode", false, TabGroup.MainSettings, false).SetParent(MapModification);
            MaxInVentTime = FloatOptionItem.Create(107012, "MaxInVentTime", new(3f, 300, 0.5f), 30f, TabGroup.MainSettings, false).SetValueFormat(OptionFormat.Seconds).SetParent(MaxInVentMode);

            // タスク無効化
            UploadDataIsLongTask = BooleanOptionItem.Create(107200, "UploadDataIsLongTask", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetParent(MapModification);
            DisableTasks = BooleanOptionItem.Create(107201, "DisableTasks", false, TabGroup.MainSettings, false).SetParent(MapModification)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#6b6b6b");
            DisableSwipeCard = BooleanOptionItem.Create(107202, "DisableSwipeCardTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableSubmitScan = BooleanOptionItem.Create(107203, "DisableSubmitScanTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableUnlockSafe = BooleanOptionItem.Create(107204, "DisableUnlockSafeTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableUploadData = BooleanOptionItem.Create(107205, "DisableUploadDataTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableStartReactor = BooleanOptionItem.Create(107206, "DisableStartReactorTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableResetBreaker = BooleanOptionItem.Create(107207, "DisableResetBreakerTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableCatchFish = BooleanOptionItem.Create(107208, "DisableCatchFish", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableDivertPower = BooleanOptionItem.Create(107209, "DisableDivertPower", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableFuelEngins = BooleanOptionItem.Create(107210, "DisableFuelEngins", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableInspectSample = BooleanOptionItem.Create(107211, "DisableInspectSample", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableRebootWifi = BooleanOptionItem.Create(107212, "DisableRebootWifi", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableInseki = BooleanOptionItem.Create(107213, "DisableInseki", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableCalibrateDistributor = BooleanOptionItem.Create(107214, "DisableCalibrateDistributor", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableVentCleaning = BooleanOptionItem.Create(107215, "DisableVentCleaning", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableHelpCritter = BooleanOptionItem.Create(107216, "DisableHelpCritter", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            Disablefixwiring = BooleanOptionItem.Create(107217, "Disablefixwiring", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableFixWeatherNode = BooleanOptionItem.Create(107218, "DisableFixWeatherNodeTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                            .SetGameMode(CustomGameMode.All);

            //デバイス設定
            DevicesOption = BooleanOptionItem.Create(104000, "DevicesOption", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetColorcode("#d860e0");
            DisableDevices = BooleanOptionItem.Create(104001, "DisableDevices", false, TabGroup.MainSettings, false).SetParent(DevicesOption)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#00ff99");
            DisableSkeldDevices = BooleanOptionItem.Create(104002, "DisableSkeldDevices", false, TabGroup.MainSettings, false).SetParent(DisableDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetEnabled(() => IsActiveSkeld);
            DisableSkeldAdmin = BooleanOptionItem.Create(104003, "DisableSkeldAdmin", false, TabGroup.MainSettings, false).SetParent(DisableSkeldDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#00ff99");
            DisableSkeldCamera = BooleanOptionItem.Create(104004, "DisableSkeldCamera", false, TabGroup.MainSettings, false).SetParent(DisableSkeldDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#cccccc");
            DisableMiraHQDevices = BooleanOptionItem.Create(104005, "DisableMiraHQDevices", false, TabGroup.MainSettings, false).SetParent(DisableDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetEnabled(() => IsActiveMiraHQ);
            DisableMiraHQAdmin = BooleanOptionItem.Create(104006, "DisableMiraHQAdmin", false, TabGroup.MainSettings, false).SetParent(DisableMiraHQDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#00ff99");
            DisableMiraHQDoorLog = BooleanOptionItem.Create(104007, "DisableMiraHQDoorLog", false, TabGroup.MainSettings, false).SetParent(DisableMiraHQDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#cccccc");
            DisablePolusDevices = BooleanOptionItem.Create(104008, "DisablePolusDevices", false, TabGroup.MainSettings, false).SetParent(DisableDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetEnabled(() => IsActivePolus);
            DisablePolusAdmin = BooleanOptionItem.Create(104009, "DisablePolusAdmin", false, TabGroup.MainSettings, false).SetParent(DisablePolusDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#00ff99");
            DisablePolusCamera = BooleanOptionItem.Create(104010, "DisablePolusCamera", false, TabGroup.MainSettings, false).SetParent(DisablePolusDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#cccccc");
            DisablePolusVital = BooleanOptionItem.Create(104011, "DisablePolusVital", false, TabGroup.MainSettings, false).SetParent(DisablePolusDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#33ccff");
            DisableAirshipDevices = BooleanOptionItem.Create(104012, "DisableAirshipDevices", false, TabGroup.MainSettings, false).SetParent(DisableDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetEnabled(() => IsActiveAirship);
            DisableAirshipCockpitAdmin = BooleanOptionItem.Create(104013, "DisableAirshipCockpitAdmin", false, TabGroup.MainSettings, false).SetParent(DisableAirshipDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#00ff99");
            DisableAirshipRecordsAdmin = BooleanOptionItem.Create(104014, "DisableAirshipRecordsAdmin", false, TabGroup.MainSettings, false).SetParent(DisableAirshipDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#00ff99");
            DisableAirshipCamera = BooleanOptionItem.Create(104015, "DisableAirshipCamera", false, TabGroup.MainSettings, false).SetParent(DisableAirshipDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#cccccc");
            DisableAirshipVital = BooleanOptionItem.Create(104016, "DisableAirshipVital", false, TabGroup.MainSettings, false).SetParent(DisableAirshipDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#33ccff");
            DisableFungleDevices = BooleanOptionItem.Create(104017, "DisableFungleDevices", false, TabGroup.MainSettings, false).SetParent(DisableDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetEnabled(() => IsActiveFungle);
            DisableFungleVital = BooleanOptionItem.Create(104018, "DisableFungleVital", false, TabGroup.MainSettings, false).SetParent(DisableFungleDevices)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#33ccff");

            DisableDevicesIgnoreConditions = BooleanOptionItem.Create(104100, "IgnoreConditions", false, TabGroup.MainSettings, false).SetParent(DisableDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableDevicesIgnoreImpostors = BooleanOptionItem.Create(104101, "IgnoreImpostors", false, TabGroup.MainSettings, false).SetParent(DisableDevicesIgnoreConditions)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#ff1919");
            DisableDevicesIgnoreMadmates = BooleanOptionItem.Create(104102, "IgnoreMadmates", false, TabGroup.MainSettings, false).SetParent(DisableDevicesIgnoreConditions)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#ff1919");
            DisableDevicesIgnoreNeutrals = BooleanOptionItem.Create(104103, "IgnoreNeutrals", false, TabGroup.MainSettings, false).SetParent(DisableDevicesIgnoreConditions)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#808080");
            DisableDevicesIgnoreCrewmates = BooleanOptionItem.Create(104104, "IgnoreCrewmates", false, TabGroup.MainSettings, false).SetParent(DisableDevicesIgnoreConditions)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#8cffff");
            DisableDevicesIgnoreAfterAnyoneDied = BooleanOptionItem.Create(104105, "IgnoreAfterAnyoneDied", false, TabGroup.MainSettings, false).SetParent(DisableDevicesIgnoreConditions)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#666699");

            TimeLimitDevices = BooleanOptionItem.Create(104200, "TimeLimitDevices", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#948e50")
                .SetParent(DevicesOption);
            TimeLimitAdmin = FloatOptionItem.Create(104201, "TimeLimitAdmin", new(0f, 300f, 1), 20f, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.Standard).SetColorcode("#00ff99").SetValueFormat(OptionFormat.Seconds).SetZeroNotation(OptionZeroNotation.Infinity).SetParent(TimeLimitDevices);
            TimeLimitCamAndLog = FloatOptionItem.Create(104202, "TimeLimitCamAndLog", new(0f, 300f, 1), 20f, TabGroup.MainSettings, false)
                            .SetGameMode(CustomGameMode.Standard).SetColorcode("#cccccc").SetValueFormat(OptionFormat.Seconds).SetZeroNotation(OptionZeroNotation.Infinity).SetParent(TimeLimitDevices);
            TimeLimitVital = FloatOptionItem.Create(104203, "TimeLimitVital", new(0f, 300f, 1), 20f, TabGroup.MainSettings, false)
                            .SetGameMode(CustomGameMode.Standard).SetColorcode("#33ccff").SetValueFormat(OptionFormat.Seconds).SetZeroNotation(OptionZeroNotation.Infinity).SetParent(TimeLimitDevices);
            CanSeeTimeLimit = BooleanOptionItem.Create(104204, "CanSeeTimeLimit", false, TabGroup.MainSettings, false)
                            .SetGameMode(CustomGameMode.Standard).SetColorcode("#cc8b60").SetParent(TimeLimitDevices);
            CanseeImpTimeLimit = BooleanOptionItem.Create(104205, "CanseeImpTimeLimit", false, TabGroup.MainSettings, false)
            .SetGameMode(CustomGameMode.Standard).SetColor(ModColors.ImpostorRed).SetParent(CanSeeTimeLimit);
            CanseeMadTimeLimit = BooleanOptionItem.Create(104206, "CanseeMadTimeLimit", false, TabGroup.MainSettings, false)
            .SetGameMode(CustomGameMode.Standard).SetColor(ModColors.MadMateOrenge).SetParent(CanSeeTimeLimit);
            CanseeCrewTimeLimit = BooleanOptionItem.Create(104207, "CanseeCrewTimeLimit", false, TabGroup.MainSettings, false)
            .SetGameMode(CustomGameMode.Standard).SetColor(ModColors.CrewMateBlue).SetParent(CanSeeTimeLimit);
            CanseeNeuTimeLimit = BooleanOptionItem.Create(104208, "CanseeNeuTimeLimit", false, TabGroup.MainSettings, false)
            .SetGameMode(CustomGameMode.Standard).SetColor(Palette.DisabledGrey).SetParent(CanSeeTimeLimit);

            TurnTimeLimitDevice = BooleanOptionItem.Create(104300, "TurnTimeLimitDevice", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#b06927")
                .SetParent(DevicesOption);
            TurnTimeLimitAdmin = FloatOptionItem.Create(104301, "TimeLimitAdmin", new(0f, 300f, 1), 20f, TabGroup.MainSettings, false)
            .SetGameMode(CustomGameMode.Standard).SetColorcode("#00ff99").SetValueFormat(OptionFormat.Seconds).SetZeroNotation(OptionZeroNotation.Infinity).SetParent(TurnTimeLimitDevice);
            TurnTimeLimitCamAndLog = FloatOptionItem.Create(104302, "TimeLimitCamAndLog", new(0f, 300f, 1), 20f, TabGroup.MainSettings, false)
                            .SetGameMode(CustomGameMode.Standard).SetColorcode("#cccccc").SetValueFormat(OptionFormat.Seconds).SetZeroNotation(OptionZeroNotation.Infinity).SetParent(TurnTimeLimitDevice);
            TurnTimeLimitVital = FloatOptionItem.Create(104303, "TimeLimitVital", new(0f, 300f, 1), 20f, TabGroup.MainSettings, false)
                            .SetGameMode(CustomGameMode.Standard).SetColorcode("#33ccff").SetValueFormat(OptionFormat.Seconds).SetZeroNotation(OptionZeroNotation.Infinity).SetParent(TurnTimeLimitDevice);

            //サボ
            Sabotage = BooleanOptionItem.Create(108000, "Sabotage", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetColorcode("#b71e1e")
                .SetGameMode(CustomGameMode.Standard);
            // リアクターの時間制御
            SabotageActivetimerControl = BooleanOptionItem.Create(108001, "SabotageActivetimerControl", false, TabGroup.MainSettings, false).SetParent(Sabotage)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#f22c50");
            SkeldReactorTimeLimit = FloatOptionItem.Create(108002, "SkeldReactorTimeLimit", new(1f, 90f, 1f), 30f, TabGroup.MainSettings, false).SetParent(SabotageActivetimerControl)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard)
                .SetEnabled(() => IsActiveSkeld);
            SkeldO2TimeLimit = FloatOptionItem.Create(108003, "SkeldO2TimeLimit", new(1f, 90f, 1f), 30f, TabGroup.MainSettings, false).SetParent(SabotageActivetimerControl)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard)
                .SetEnabled(() => IsActiveSkeld);
            MiraReactorTimeLimit = FloatOptionItem.Create(108004, "MiraReactorTimeLimit", new(1f, 90f, 1f), 30f, TabGroup.MainSettings, false).SetParent(SabotageActivetimerControl)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard)
                .SetEnabled(() => IsActiveMiraHQ);
            MiraO2TimeLimit = FloatOptionItem.Create(108005, "MiraO2TimeLimit", new(1f, 90f, 1f), 30f, TabGroup.MainSettings, false).SetParent(SabotageActivetimerControl)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard)
                .SetEnabled(() => IsActiveMiraHQ);
            PolusReactorTimeLimit = FloatOptionItem.Create(108006, "PolusReactorTimeLimit", new(1f, 90f, 1f), 30f, TabGroup.MainSettings, false).SetParent(SabotageActivetimerControl)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard)
                .SetEnabled(() => IsActivePolus);
            AirshipReactorTimeLimit = FloatOptionItem.Create(108007, "AirshipReactorTimeLimit", new(1f, 90f, 1f), 60f, TabGroup.MainSettings, false).SetParent(SabotageActivetimerControl)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard)
                .SetEnabled(() => IsActiveAirship);
            FungleReactorTimeLimit = FloatOptionItem.Create(108008, "FungleReactorTimeLimit", new(1f, 90f, 1f), 60f, TabGroup.MainSettings, false).SetParent(SabotageActivetimerControl)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard)
                .SetEnabled(() => IsActiveFungle);
            FungleMushroomMixupDuration = FloatOptionItem.Create(108009, "FungleMushroomMixupDuration", new(1f, 20f, 1f), 10f, TabGroup.MainSettings, false).SetParent(SabotageActivetimerControl)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard)
                .SetEnabled(() => IsActiveFungle);
            // 他
            ChangeSabotageWinRole = BooleanOptionItem.Create(108100, "ChangeSabotageWinRole", false, TabGroup.MainSettings, false).SetParent(Sabotage)
                .SetGameMode(CustomGameMode.Standard);
            // サボタージュのクールダウン変更
            ModifySabotageCooldown = BooleanOptionItem.Create(108101, "ModifySabotageCooldown", false, TabGroup.MainSettings, false).SetParent(Sabotage)
                .SetGameMode(CustomGameMode.Standard);
            SabotageCooldown = FloatOptionItem.Create(108102, "SabotageCooldown", new(1f, 60f, 1f), 30f, TabGroup.MainSettings, false).SetParent(ModifySabotageCooldown)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);

            CommsSpecialSettings = BooleanOptionItem.Create(108103, "CommsSpecialSettings", false, TabGroup.MainSettings, false).SetParent(Sabotage)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#999999");
            CommsDonttouch = BooleanOptionItem.Create(108104, "CommsDonttouch", false, TabGroup.MainSettings, false).SetParent(CommsSpecialSettings)
                .SetGameMode(CustomGameMode.Standard);
            CommsDonttouchTime = FloatOptionItem.Create(108105, "CommsDonttouchTime", new(0f, 180f, 0.5f), 3.0f, TabGroup.MainSettings, false).SetParent(CommsDonttouch)
                .SetGameMode(CustomGameMode.Standard).SetValueFormat(OptionFormat.Seconds);
            CommsCamouflage = BooleanOptionItem.Create(108106, "CommsCamouflage", false, TabGroup.MainSettings, false).SetParent(CommsSpecialSettings)
                .SetGameMode(CustomGameMode.Standard);

            // 停電の特殊設定
            LightsOutSpecialSettings = BooleanOptionItem.Create(108107, "LightsOutSpecialSettings", false, TabGroup.MainSettings, false).SetParent(Sabotage)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#ffcc66");
            LightOutDonttouch = BooleanOptionItem.Create(108108, "LightOutDonttouch", false, TabGroup.MainSettings, false).SetParent(LightsOutSpecialSettings)
                .SetGameMode(CustomGameMode.Standard).SetValueFormat(OptionFormat.Seconds);
            LightOutDonttouchTime = FloatOptionItem.Create(108109, "LightOutDonttouchTime", new(0f, 180f, 0.5f), 3.0f, TabGroup.MainSettings, false).SetParent(LightOutDonttouch)
            .SetGameMode(CustomGameMode.Standard).SetValueFormat(OptionFormat.Seconds);
            DisableAirshipViewingDeckLightsPanel = BooleanOptionItem.Create(108110, "DisableAirshipViewingDeckLightsPanel", false, TabGroup.MainSettings, false).SetParent(LightsOutSpecialSettings)
                .SetGameMode(CustomGameMode.Standard)
                .SetEnabled(() => IsActiveAirship);
            DisableAirshipGapRoomLightsPanel = BooleanOptionItem.Create(108111, "DisableAirshipGapRoomLightsPanel", false, TabGroup.MainSettings, false).SetParent(LightsOutSpecialSettings)
                .SetGameMode(CustomGameMode.Standard)
                .SetEnabled(() => IsActiveAirship);
            DisableAirshipCargoLightsPanel = BooleanOptionItem.Create(108112, "DisableAirshipCargoLightsPanel", false, TabGroup.MainSettings, false).SetParent(LightsOutSpecialSettings)
                .SetGameMode(CustomGameMode.Standard)
                .SetEnabled(() => IsActiveAirship);
            BlockDisturbancesToSwitches = BooleanOptionItem.Create(108113, "BlockDisturbancesToSwitches", false, TabGroup.MainSettings, false).SetParent(LightsOutSpecialSettings)
                .SetGameMode(CustomGameMode.Standard)
                .SetEnabled(() => IsActiveAirship);
            AllowCloseDoors = BooleanOptionItem.Create(108114, "AllowCloseDoors", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All).SetParent(Sabotage);
            // ランダムマップ
            RandomMapsMode = BooleanOptionItem.Create(108700, "RandomMapsMode", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetColorcode("#ffcc66")
                .SetGameMode(CustomGameMode.All);
            AddedTheSkeld = BooleanOptionItem.Create(108701, "AddedTheSkeld", false, TabGroup.MainSettings, false).SetParent(RandomMapsMode)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#666666");
            AddedMiraHQ = BooleanOptionItem.Create(108702, "AddedMIRAHQ", false, TabGroup.MainSettings, false).SetParent(RandomMapsMode)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#ff6633");
            AddedPolus = BooleanOptionItem.Create(108703, "AddedPolus", false, TabGroup.MainSettings, false).SetParent(RandomMapsMode)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#980098");
            AddedTheAirShip = BooleanOptionItem.Create(108704, "AddedTheAirShip", false, TabGroup.MainSettings, false).SetParent(RandomMapsMode)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#ff3300");
            AddedTheFungle = BooleanOptionItem.Create(108705, "AddedTheFungle", false, TabGroup.MainSettings, false).SetParent(RandomMapsMode)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#ff9900");

            // ランダムスポーン
            EnableRandomSpawn = BooleanOptionItem.Create(101300, "RandomSpawn", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetColorcode("#ff99cc")
                .SetGameMode(CustomGameMode.All);
            CanSeeNextRandomSpawn = BooleanOptionItem.Create(101301, "CanSeeNextRandomSpawn", false, TabGroup.MainSettings, false).SetParent(EnableRandomSpawn)
                .SetGameMode(CustomGameMode.All);
            RandomSpawn.SetupCustomOption();

            //会議設定
            MeetingAndVoteOpt = BooleanOptionItem.Create(109000, "MeetingAndVoteOpt", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.Standard)
                .SetColorcode("#64ff0a")
                .SetHeader(true);
            LowerLimitVotingTime = FloatOptionItem.Create(109001, "LowerLimitVotingTime", new(5f, 300f, 1f), 60f, TabGroup.MainSettings, false)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard)
                .SetParent(MeetingAndVoteOpt);
            MeetingTimeLimit = FloatOptionItem.Create(109002, "LimitMeetingTime", new(5f, 300f, 1f), 300f, TabGroup.MainSettings, false)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard)
                .SetParent(MeetingAndVoteOpt);
            // 全員生存時の会議時間
            AllAliveMeeting = BooleanOptionItem.Create(109003, "AllAliveMeeting", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.Standard)
                .SetParent(MeetingAndVoteOpt);
            AllAliveMeetingTime = FloatOptionItem.Create(109004, "AllAliveMeetingTime", new(1f, 300f, 1f), 10f, TabGroup.MainSettings, false).SetParent(AllAliveMeeting)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);
            // 投票モード
            VoteMode = BooleanOptionItem.Create(109005, "VoteMode", false, TabGroup.MainSettings, false)
                .SetColorcode("#33ff99")
                .SetGameMode(CustomGameMode.Standard)
                .SetParent(MeetingAndVoteOpt);
            WhenSkipVote = StringOptionItem.Create(109006, "WhenSkipVote", voteModes[0..3], 0, TabGroup.MainSettings, false).SetParent(VoteMode)
                .SetGameMode(CustomGameMode.Standard);
            WhenSkipVoteIgnoreFirstMeeting = BooleanOptionItem.Create(109007, "WhenSkipVoteIgnoreFirstMeeting", false, TabGroup.MainSettings, false).SetParent(WhenSkipVote)
                .SetGameMode(CustomGameMode.Standard);
            WhenSkipVoteIgnoreNoDeadBody = BooleanOptionItem.Create(109008, "WhenSkipVoteIgnoreNoDeadBody", false, TabGroup.MainSettings, false).SetParent(WhenSkipVote)
                .SetGameMode(CustomGameMode.Standard);
            WhenSkipVoteIgnoreEmergency = BooleanOptionItem.Create(109009, "WhenSkipVoteIgnoreEmergency", false, TabGroup.MainSettings, false).SetParent(WhenSkipVote)
                .SetGameMode(CustomGameMode.Standard);
            WhenNonVote = StringOptionItem.Create(109010, "WhenNonVote", voteModes, 0, TabGroup.MainSettings, false).SetParent(VoteMode)
                .SetGameMode(CustomGameMode.Standard);
            WhenTie = StringOptionItem.Create(109011, "WhenTie", tieModes, 0, TabGroup.MainSettings, false).SetParent(VoteMode)
                .SetGameMode(CustomGameMode.Standard);
            SyncButtonMode = BooleanOptionItem.Create(109012, "SyncButtonMode", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.Standard)
                .SetParent(MeetingAndVoteOpt)
                .SetColorcode("#64ff0a");
            SyncedButtonCount = IntegerOptionItem.Create(109013, "SyncedButtonCount", new(0, 100, 1), 10, TabGroup.MainSettings, false).SetParent(SyncButtonMode)
                .SetValueFormat(OptionFormat.Times)
                .SetGameMode(CustomGameMode.Standard);
            // 生存人数ごとの緊急会議
            AdditionalEmergencyCooldown = BooleanOptionItem.Create(109014, "AdditionalEmergencyCooldown", false, TabGroup.MainSettings, false).SetParent(MeetingAndVoteOpt);
            AdditionalEmergencyCooldownThreshold = IntegerOptionItem.Create(109015, "AdditionalEmergencyCooldownThreshold", new(1, 15, 1), 1, TabGroup.MainSettings, false).SetParent(AdditionalEmergencyCooldown)
                .SetValueFormat(OptionFormat.Players)
                .SetGameMode(CustomGameMode.Standard);
            AdditionalEmergencyCooldownTime = FloatOptionItem.Create(109016, "AdditionalEmergencyCooldownTime", new(1f, 60f, 1f), 1f, TabGroup.MainSettings, false).SetParent(AdditionalEmergencyCooldown)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);
            ShowVoteResult = BooleanOptionItem.Create(109017, "ShowVoteResult", false, TabGroup.MainSettings, false).SetParent(MeetingAndVoteOpt);
            ShowVoteJudgment = StringOptionItem.Create(109018, "ShowVoteJudgment", ShowVoteJudgments, 0, TabGroup.MainSettings, false).SetParent(ShowVoteResult);

            // 転落死
            LadderDeath = BooleanOptionItem.Create(109900, "LadderDeath", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetColorcode("#ffcc00");
            LadderDeathChance = StringOptionItem.Create(109901, "LadderDeathChance", rates[1..], 0, TabGroup.MainSettings, false).SetParent(LadderDeath);
            LadderDeathNuuun = BooleanOptionItem.Create(109902, "LadderDeathNuuun", false, TabGroup.MainSettings, false).SetParent(LadderDeath);
            LadderDeathZipline = BooleanOptionItem.Create(109903, "LadderDeathZipline", false, TabGroup.MainSettings, false).SetParent(LadderDeath);

            //幽霊設定
            GhostOptions = BooleanOptionItem.Create(110000, "GhostOptions", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetColorcode("#6c6ce0")
                .SetGameMode(CustomGameMode.All);
            GhostCanSeeOtherRoles = BooleanOptionItem.Create(110001, "GhostCanSeeOtherRoles", true, TabGroup.MainSettings, false)
                .SetColorcode("#7474ab")
                .SetGameMode(CustomGameMode.All)
                .SetParent(GhostOptions);
            GhostCanSeeOtherTasks = BooleanOptionItem.Create(110002, "GhostCanSeeOtherTasks", true, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColor(Color.yellow)
                .SetParent(GhostOptions);
            GhostCanSeeOtherVotes = BooleanOptionItem.Create(110003, "GhostCanSeeOtherVotes", true, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#800080")
                .SetParent(GhostOptions);
            GhostCanSeeDeathReason = BooleanOptionItem.Create(110004, "GhostCanSeeDeathReason", true, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#80ffdd")
                .SetParent(GhostOptions);
            GhostCanSeeKillerColor = BooleanOptionItem.Create(110005, "GhostCanSeeKillerColor", true, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#80ffdd")
                .SetParent(GhostCanSeeDeathReason);
            GhostCanSeeAllTasks = BooleanOptionItem.Create(110006, "GhostCanSeeAllTasks", true, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#cee4ae")
                .SetParent(GhostOptions);
            GhostCanSeeNumberOfButtonsOnOthers = BooleanOptionItem.Create(110007, "GhostCanSeeNumberOfButtonsOnOthers", true, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#d7c447")
                .SetParent(GhostOptions);
            GhostCanSeeKillflash = BooleanOptionItem.Create(110008, "GhostCanSeeKillflash", true, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#61b26c")
                .SetParent(GhostOptions);
            GhostIgnoreTasks = BooleanOptionItem.Create(110009, "GhostIgnoreTasks", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#bbbbdd")
                .SetParent(GhostOptions);

            // その他
            ConvenientOptions = BooleanOptionItem.Create(111000, "ConvenientOptions", true, TabGroup.MainSettings, false)
                .SetColorcode("#cc3366")
                .SetHeader(true);
            FirstTurnMeeting = BooleanOptionItem.Create(111001, "FirstTurnMeeting", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.Standard)//初手強制会議
                .SetColorcode("#4fd6a7")
                .SetParent(ConvenientOptions);
            FirstTurnMeetingCantability = BooleanOptionItem.Create(111002, "FirstTurnMeetingCantability", false, TabGroup.MainSettings, false).SetGameMode(CustomGameMode.Standard).SetParent(FirstTurnMeeting);
            FixFirstKillCooldown = BooleanOptionItem.Create(111003, "FixFirstKillCooldown", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#fa7373")
                .SetParent(ConvenientOptions);
            CommnTaskResetAssing = BooleanOptionItem.Create(111004, "CommnTaskResetAssing", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColor(Color.yellow)
                .SetParent(ConvenientOptions);
            CanseeVoteresult = BooleanOptionItem.Create(111005, "CanseeVoteresult", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#64ff0a")
                .SetParent(ConvenientOptions);
            OutroCrewWinreasonchenge = BooleanOptionItem.Create(111006, "OutroCrewWinreasonchenge", true, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#66ffff")
                .SetParent(ConvenientOptions);

            DisableTaskWin = BooleanOptionItem.Create(1_000_200, "DisableTaskWin", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetColorcode("#ccff00")
                .SetGameMode(CustomGameMode.All);
            NoGameEnd = BooleanOptionItem.Create(1_000_201, "NoGameEnd", false, TabGroup.MainSettings, false)
                .SetColorcode("#ff1919")
                .SetGameMode(CustomGameMode.All);
            // プリセット対象外
            AutoDisplayLastResult = BooleanOptionItem.Create(1_000_000, "AutoDisplayLastResult", true, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetColorcode("#66ffff")
                .SetGameMode(CustomGameMode.All);
            AutoDisplayKillLog = BooleanOptionItem.Create(1_000_006, "AutoDisplayKillLog", true, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#66ffff");
            HideGameSettings = BooleanOptionItem.Create(1_000_002, "HideGameSettings", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#00c1ff");
            HideSettingsDuringGame = BooleanOptionItem.Create(1_000_003, "HideGameSettingsDuringGame", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#00c1ff");
            SuffixMode = StringOptionItem.Create(1_000_001, "SuffixMode", suffixModes, 0, TabGroup.MainSettings, true)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#00c1ff");
            ChangeNameToRoleInfo = BooleanOptionItem.Create(1_000_004, "ChangeNameToRoleInfo", true, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#00c1ff");
            RoleAssigningAlgorithm = StringOptionItem.Create(1_000_005, "RoleAssigningAlgorithm", RoleAssigningAlgorithms, 0, TabGroup.MainSettings, true)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#00c1ff")
                .RegisterUpdateValueEvent(
                    (object obj, OptionItem.UpdateValueEventArgs args) => IRandom.SetInstanceById(args.CurrentValue)
                );
            UseZoom = BooleanOptionItem.Create(1_000_008, "UseZoom", true, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetColorcode("#9199a1");

            ApplyDenyNameList = BooleanOptionItem.Create(1_000_100, "ApplyDenyNameList", true, TabGroup.MainSettings, true)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.All)
                .SetInfo(Translator.GetString("KickBanOptionWhiteList"))
                .SetColor(Color.red);
            KickPlayerFriendCodeNotExist = BooleanOptionItem.Create(1_000_101, "KickPlayerFriendCodeNotExist", false, TabGroup.MainSettings, true)
                .SetGameMode(CustomGameMode.All)
                .SetInfo(Translator.GetString("KickBanOptionWhiteList"))
                .SetColor(Color.red);
            ApplyBanList = BooleanOptionItem.Create(1_000_110, "ApplyBanList", true, TabGroup.MainSettings, true)
                .SetGameMode(CustomGameMode.All)
                .SetColor(Color.red);
            KiclHotNotFriend = BooleanOptionItem.Create(1_000_111, "KiclHotNotFriend", false, TabGroup.MainSettings, true)
                .SetGameMode(CustomGameMode.All)
                .SetInfo(Translator.GetString("KickBanOptionWhiteList"))
                .SetColor(Color.red);

            DebugModeManager.SetupCustomOption();

            OptionSaver.Load();

            Combinations = null; //使わないから消す

            IsLoaded = true;

            static void CreateRoleOption(IOrderedEnumerable<SimpleRoleInfo> sortedRoleInfo, CustomRoleTypes roleTypes)
            {
                bool Create = true;
                int NowTabNum = 0;
                while (Create)
                {
                    var RoleList = sortedRoleInfo.Where(role => role.CustomRoleType == roleTypes
                    && role.OptionSort.TabNumber == NowTabNum);
                    if (RoleList.Count() <= 0)
                    {
                        Create = false;
                        break;
                    }
                    foreach (var info in RoleList.OrderBy(role => role.OptionSort.SortNumber))
                    {
                        if (info.RoleName is CustomRoles.AlienHijack) continue;
                        SetupRoleOptions(info);
                        info.OptionCreator?.Invoke();
                    }
                    NowTabNum++;
                }
            }
        }
        private static List<CombinationRoles> Combinations = new();
        public static void SetupRoleOptions(SimpleRoleInfo info) => SetupRoleOptions(info.ConfigId, info.Tab, info.RoleName, info.AssignInfo.AssignCountRule, fromtext: UtilsOption.GetFrom(info), combination: info.Combination);
        public static OptionItem SetupRoleOptions(int id, TabGroup tab, CustomRoles role, IntegerValueRule assignCountRule = null, CustomGameMode customGameMode = CustomGameMode.Standard, string fromtext = "", CombinationRoles combination = CombinationRoles.None, int defo = -1)
        {
            if ((role is CustomRoles.Phantom) || (combination != CombinationRoles.None && Combinations.Contains(combination))) return null;
            if (role.IsVanilla())
            {
                switch (role)
                {
                    case CustomRoles.Impostor: id = 10; break;
                    case CustomRoles.Shapeshifter: id = 30; break;
                    case CustomRoles.Phantom: id = 40; break;
                    case CustomRoles.Crewmate: id = 11; break;
                    case CustomRoles.Engineer: id = 200; break;
                    case CustomRoles.Scientist: id = 250; break;
                    case CustomRoles.Tracker: id = 300; break;
                    case CustomRoles.Noisemaker: id = 350; break;
                }
            }
            assignCountRule ??= new(1, 15, 1);
            var from = "<line-height=25%><size=25%>\n</size><size=60%><pos=50%></color> <b>" + fromtext + "</b></size>";

            var spawnOption = IntegerOptionItem.Create(id, combination == CombinationRoles.None ? role.ToString() : combination.ToString(), new(0, 100, 10), 0, tab, false, from)
                .SetColorcode(UtilsRoleText.GetRoleColorCode(role))
                .SetColor(UtilsRoleText.GetRoleColor(role, true))
                .SetCustomRole(role)
                .SetValueFormat(OptionFormat.Percent)
                .SetHeader(true)
                .SetEnabled(() => role is not CustomRoles.Crewmate and not CustomRoles.Impostor || ShowFilter.NowSettingRole is not CustomRoles.NotAssigned)
                .SetHidden(role == CustomRoles.NotAssigned || (!DebugModeManager.AmDebugger && combination is CombinationRoles.AssassinandMerlin))
                .SetGameMode(customGameMode) as IntegerOptionItem;
            var hidevalue = role is CustomRoles.Driver || role.IsLovers() || (assignCountRule.MaxValue == assignCountRule.MinValue);

            if (role is CustomRoles.Crewmate or CustomRoles.Impostor) return spawnOption;

            var countOption = IntegerOptionItem.Create(id + 1, "Maximum", assignCountRule, defo is -1 ? assignCountRule.Step : defo, tab, false, HideValue: hidevalue)
                .SetParent(spawnOption)
                .SetValueFormat(assignCountRule.MaxValue is 7 ? OptionFormat.Set : OptionFormat.Players)
                .SetGameMode(customGameMode)
                .SetHidden(hidevalue)
                .SetParentRole(role);

            if (combination != CombinationRoles.None) Combinations.Add(combination);
            CustomRoleSpawnChances.Add(role, spawnOption);
            CustomRoleCounts.Add(role, countOption);
            return spawnOption;
        }
    }
}
