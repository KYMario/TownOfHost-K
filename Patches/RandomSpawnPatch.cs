using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Hazel;
using UnityEngine;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Impostor;
using TownOfHost.Roles.Neutral;
using TownOfHost.Roles.AddOns.Common;
using TownOfHost.Modules;

namespace TownOfHost
{
    public enum SpawnPoint
    {
        Cafeteria,
        Weapons,
        LifeSupp,
        Nav,
        Shields,
        Comms,
        Storage,
        Admin,
        Electrical,
        LowerEngine,
        UpperEngine,
        Security,
        Reactor,
        MedBay,
        Balcony,
        Junction,//StringNamesにない文言 string.csvに追加
        LockerRoom,
        Decontamination,
        Laboratory,
        Launchpad,
        Office,
        OfficeLeft,//StringNamesにない文言 string.csvに追加
        OfficeRight,//StringNamesにない文言 string.csvに追加
        Greenhouse,
        BoilerRoom,
        Dropship,
        Rocket,//StringNamesにない文言 string.csvに追加
        Toilet,//StringNamesにない文言 string.csvに追加
        Specimens,
        Brig,
        Engine,
        Kitchen,
        CargoBay,
        Records,
        MainHall,
        NapRoom,//StringNamesにない文言 string.csvに追加 AirShipメインホール左上の仮眠室
        MeetingRoom,
        GapRoom,
        VaultRoom,
        Cockpit,
        Armory,
        ViewingDeck,
        Medical,
        Showers,
        Beach,
        RecRoom,//SplashZoneのこと
        Bonfire,//StringNamesにない文言 string.csvに追加 Fungleの焚き火
        SleepingQuarters,//TheDorm 宿舎のこと
        JungleTop,//StringNamesにない文言 string.csvに追加
        JungleBottom,//StringNamesにない文言 string.csvに追加
        Lookout,
        MiningPit,
        Highlands,//Fungleの高地
        Precipice,//StringNamesにない文言 string.csvに追加
        Custom, //カスタム
    }
    class RandomSpawn
    {
        private static Dictionary<byte, int> NumOfTP = new();

        [HarmonyPatch(typeof(CustomNetworkTransform), nameof(CustomNetworkTransform.HandleRpc))]
        public class CustomNetworkTransformHandleRpcPatch
        {
            public static bool Prefix(CustomNetworkTransform __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
            {
                if (!AmongUsClient.Instance.AmHost)
                {
                    return true;
                }
                if (AntiBlackout.isRoleCache.Contains(__instance?.myPlayer?.PlayerId ?? byte.MaxValue) && (MapNames)Main.NormalOptions.MapId is MapNames.Airship && (RpcCalls)callId == RpcCalls.SnapTo)
                {
                    AntiBlackout.ResetSetRole(__instance.myPlayer);
                }
                if (!__instance.isActiveAndEnabled)
                {
                    return false;
                }
                if (TaskBattle.IsRTAMode && (RpcCalls)callId == RpcCalls.SnapTo)
                {
                    HudManagerPatch.TaskBattleTimer = 0.0f;
                }
                if ((RpcCalls)callId == RpcCalls.SnapTo && (MapNames)Main.NormalOptions.MapId == MapNames.Airship)
                {
                    var player = __instance.myPlayer;
                    var state = PlayerState.GetByPlayerId(player.PlayerId);
                    // プレイヤーがまだ湧いていない
                    if (!state.HasSpawned)
                    {
                        // SnapTo先の座標を読み取る
                        Vector2 position;
                        {
                            var newReader = MessageReader.Get(reader);
                            position = NetHelpers.ReadVector2(newReader);
                            newReader.Recycle();
                        }
                        Logger.Info($"SnapTo: {player.GetRealName()}, ({position.x}, {position.y})", "RandomSpawn");
                        // SnapTo先が湧き位置だったら湧き処理に進む
                        if (IsAirshipVanillaSpawnPosition(position))
                        {
                            AirshipSpawn(player);
                            return !IsRandomSpawn();
                        }
                        else
                        {
                            Logger.Info("ポジションは湧き位置ではありません", "RandomSpawn");
                        }
                    }
                }
                return true;
            }
            public static void TP(CustomNetworkTransform nt, Vector2 location)
            {
                if (AmongUsClient.Instance.AmHost) nt.SnapTo(location);
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(nt.NetId, (byte)RpcCalls.SnapTo, SendOption.None);
                NetHelpers.WriteVector2(location, writer);
                writer.Write(nt.lastSequenceId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }

            private static bool IsAirshipVanillaSpawnPosition(Vector2 position)
            {
                // 湧き位置の座標が0.1刻みであることを利用し，float型の誤差やReadVector2の実装による誤差の拡大の対策として座標を10倍したint型で比較する
                var decupleXFloat = position.x * 10f;
                var decupleYFloat = position.y * 10f;
                var decupleXInt = Mathf.RoundToInt(decupleXFloat);
                // 10倍した値の差が0.1近く以上あったら，元の座標が0.1刻みではないので湧き位置ではない
                if (Mathf.Abs(((float)decupleXInt) - decupleXFloat) >= 0.09f)
                {
                    return false;
                }
                var decupleYInt = Mathf.RoundToInt(decupleYFloat);
                if (Mathf.Abs(((float)decupleYInt) - decupleYFloat) >= 0.09f)
                {
                    return false;
                }
                var decuplePosition = (decupleXInt, decupleYInt);
                return decupleVanillaSpawnPositions.Contains(decuplePosition);
            }
            /// <summary>比較用 エアシップのバニラ湧き位置の10倍</summary>
            public static readonly HashSet<(int x, int y)> decupleVanillaSpawnPositions = new()
            {
                (-7, 85),  // 宿舎前通路
                (-7, -10),  // エンジン
                (-70, -115),  // キッチン
                (335, -15),  // 貨物
                (200, 105),  // アーカイブ
                (155, 0),  // メインホール
            };
        }
        [HarmonyPatch(typeof(SpawnInMinigame), nameof(SpawnInMinigame.SpawnAt))]
        public static class SpawnInMinigameSpawnAtPatch
        {
            public static bool Prefix(SpawnInMinigame __instance, [HarmonyArgument(0)] SpawnInMinigame.SpawnLocation spawnPoint)
            {
                if (!AmongUsClient.Instance.AmHost)
                {
                    return true;
                }

                if (__instance.amClosing != Minigame.CloseState.None)
                {
                    return false;
                }
                // ランダムスポーンが有効ならバニラの湧きをキャンセル
                if (IsRandomSpawn())
                {
                    // バニラ処理のRpcSnapToForcedをAirshipSpawnに置き換えたもの
                    __instance.gotButton = true;
                    PlayerControl.LocalPlayer.SetKinematic(true);
                    PlayerControl.LocalPlayer.NetTransform.SetPaused(true);
                    var state = PlayerState.GetByPlayerId(PlayerControl.LocalPlayer.PlayerId);
                    AirshipSpawn(PlayerControl.LocalPlayer);
                    DestroyableSingleton<HudManager>.Instance.PlayerCam.SnapToTarget();
                    __instance.StopAllCoroutines();
                    __instance.StartCoroutine(__instance.CoSpawnAt(PlayerControl.LocalPlayer, spawnPoint));
                    return false;
                }
                else
                {
                    AirshipSpawn(PlayerControl.LocalPlayer);
                    return true;
                }
            }
        }
        public static void AirshipSpawn(PlayerControl player)
        {
            Logger.Info($"Spawn: {player.GetRealName()}", "RandomSpawn");
            if (AmongUsClient.Instance.AmHost)
            {
                if (player.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                {
                    if (AntiBlackout.isRoleCache.Contains(player.PlayerId))
                    {
                        AntiBlackout.ResetSetRole(player);
                    }
                }

                //最初のスポーンと判定
                var roleClass = player.GetRoleClass();
                roleClass?.OnSpawn(MeetingStates.FirstMeeting);
                if (SuddenDeathMode.SuddenKillcooltime.GetBool() && Modules.SuddenDeathMode.NowSuddenDeathMode)
                {
                    PlayerCatch.AllPlayerControls.Do(pc => pc.SetKillCooldown(SuddenDeathMode.SuddenKillcooltime.GetFloat(), delay: true));
                }
                else
                {
                    if (Options.FixFirstKillCooldown.GetBool() && !MeetingStates.MeetingCalled &&
                        Options.CurrentGameMode != CustomGameMode.TaskBattle
                    ) player.SetKillCooldown(Main.AllPlayerKillCooldown[player.PlayerId], delay: true);
                    else if (Options.CurrentGameMode != CustomGameMode.TaskBattle && MeetingStates.FirstMeeting) player.SetKillCooldown(10f, delay: true);
                }
                if (MeetingStates.FirstMeeting) player.RpcResetAbilityCooldown();
                GameStates.Intro = false;
                GameStates.AfterIntro = true;
                if (IsRandomSpawn())
                {
                    new AirshipSpawnMap().RandomTeleport(player);
                }
                else if (player.Is(CustomRoles.GM))
                {
                    new AirshipSpawnMap().FirstTeleport(player);
                }
                //else// if (!MeetingStates.FirstMeeting && Options.BlackOutwokesitobasu.GetBool())
                //{
                //    AirshipSpawnMap.VpRandomTeleport(player);
                //}
            }
            PlayerState.GetByPlayerId(player.PlayerId).HasSpawned = true;
        }
        public static bool CheckCustomSpawn()
        {
            if (!Options.EnableRandomSpawn.GetBool() || !Options.EnableCustomSpawn.GetBool()) return false;

            var spawnMaps = CustomSpawnManager.Data.CurrentPreset.SpawnMaps;
            var hasData = spawnMaps.TryGetValue((MapNames)Main.NormalOptions.MapId, out var mapData);

            return hasData && mapData.Points.Count > 0;

        }
        public static bool IsRandomSpawn(bool checkCustomSpawn = true)
        {
            if (!Options.EnableRandomSpawn.GetBool()) return false;

            //カスタムスポーンがあるなら
            if (checkCustomSpawn && CheckCustomSpawn()) return true;

            switch (Main.NormalOptions.MapId)
            {
                case 0:
                    return Options.RandomSpawnSkeld.GetBool();
                case 1:
                    return Options.RandomSpawnMira.GetBool();
                case 2:
                    return Options.RandomSpawnPolus.GetBool();
                case 4:
                    return Options.RandomSpawnAirship.GetBool();
                case 5:
                    return Options.RandomSpawnFungle.GetBool();
                default:
                    Logger.Error($"MapIdFailed ID:{Main.NormalOptions.MapId}", "IsRandomSpawn");
                    return false;
            }
        }

        public static void SetupCustomOption()
        {
            // Skeld
            Options.RandomSpawnSkeld = BooleanOptionItem.Create(103000, StringNames.MapNameSkeld, false, TabGroup.MainSettings, false).SetColorcode("#666666").SetParent(Options.EnableRandomSpawn).SetTag(CustomOptionTags.All);
            Options.RandomSpawnSkeldCafeteria = BooleanOptionItem.Create(103001, StringNames.Cafeteria, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetTag(CustomOptionTags.All);
            Options.RandomSpawnSkeldWeapons = BooleanOptionItem.Create(103002, StringNames.Weapons, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetTag(CustomOptionTags.All);
            Options.RandomSpawnSkeldShields = BooleanOptionItem.Create(103003, StringNames.Shields, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetTag(CustomOptionTags.All);
            Options.RandomSpawnSkeldStorage = BooleanOptionItem.Create(103004, StringNames.Storage, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetTag(CustomOptionTags.All);
            Options.RandomSpawnSkeldLowerEngine = BooleanOptionItem.Create(103005, StringNames.LowerEngine, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetTag(CustomOptionTags.All);
            Options.RandomSpawnSkeldUpperEngine = BooleanOptionItem.Create(103006, StringNames.UpperEngine, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetTag(CustomOptionTags.All);
            Options.RandomSpawnSkeldLifeSupp = BooleanOptionItem.Create(103007, StringNames.LifeSupp, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetTag(CustomOptionTags.All);
            Options.RandomSpawnSkeldNav = BooleanOptionItem.Create(103008, StringNames.Nav, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetTag(CustomOptionTags.All);
            Options.RandomSpawnSkeldComms = BooleanOptionItem.Create(103009, StringNames.Comms, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetTag(CustomOptionTags.All);
            Options.RandomSpawnSkeldAdmin = BooleanOptionItem.Create(103010, StringNames.Admin, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetTag(CustomOptionTags.All);
            Options.RandomSpawnSkeldElectrical = BooleanOptionItem.Create(103011, StringNames.Electrical, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetTag(CustomOptionTags.All);
            Options.RandomSpawnSkeldSecurity = BooleanOptionItem.Create(103012, StringNames.Security, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetTag(CustomOptionTags.All);
            Options.RandomSpawnSkeldReactor = BooleanOptionItem.Create(103013, StringNames.Reactor, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetTag(CustomOptionTags.All);
            Options.RandomSpawnSkeldMedBay = BooleanOptionItem.Create(103014, StringNames.MedBay, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetTag(CustomOptionTags.All);
            // Mira
            Options.RandomSpawnMira = BooleanOptionItem.Create(103100, StringNames.MapNameMira, false, TabGroup.MainSettings, false).SetColorcode("#ff6633").SetParent(Options.EnableRandomSpawn).SetTag(CustomOptionTags.All);
            Options.RandomSpawnMiraCafeteria = BooleanOptionItem.Create(103101, StringNames.Cafeteria, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetTag(CustomOptionTags.All);
            Options.RandomSpawnMiraComms = BooleanOptionItem.Create(103102, StringNames.Comms, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetTag(CustomOptionTags.All);
            Options.RandomSpawnMiraDecontamination = BooleanOptionItem.Create(103103, StringNames.Decontamination, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetTag(CustomOptionTags.All);
            Options.RandomSpawnMiraReactor = BooleanOptionItem.Create(103104, StringNames.Reactor, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetTag(CustomOptionTags.All);
            Options.RandomSpawnMiraLaunchpad = BooleanOptionItem.Create(103105, StringNames.Launchpad, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetTag(CustomOptionTags.All);
            Options.RandomSpawnMiraAdmin = BooleanOptionItem.Create(103106, StringNames.Admin, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetTag(CustomOptionTags.All);
            Options.RandomSpawnMiraBalcony = BooleanOptionItem.Create(103107, StringNames.Balcony, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetTag(CustomOptionTags.All);
            Options.RandomSpawnMiraStorage = BooleanOptionItem.Create(103108, StringNames.Storage, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetTag(CustomOptionTags.All);
            Options.RandomSpawnMiraJunction = BooleanOptionItem.Create(103109, SpawnPoint.Junction, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetTag(CustomOptionTags.All);
            Options.RandomSpawnMiraMedBay = BooleanOptionItem.Create(103110, StringNames.MedBay, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetTag(CustomOptionTags.All);
            Options.RandomSpawnMiraLockerRoom = BooleanOptionItem.Create(103111, StringNames.LockerRoom, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetTag(CustomOptionTags.All);
            Options.RandomSpawnMiraLaboratory = BooleanOptionItem.Create(103112, StringNames.Laboratory, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetTag(CustomOptionTags.All);
            Options.RandomSpawnMiraOffice = BooleanOptionItem.Create(103113, StringNames.Office, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetTag(CustomOptionTags.All);
            Options.RandomSpawnMiraGreenhouse = BooleanOptionItem.Create(103114, StringNames.Greenhouse, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetTag(CustomOptionTags.All);
            // Polus
            Options.RandomSpawnPolus = BooleanOptionItem.Create(103200, StringNames.MapNamePolus, false, TabGroup.MainSettings, false).SetColorcode("#980098").SetParent(Options.EnableRandomSpawn).SetTag(CustomOptionTags.All);
            Options.RandomSpawnPolusOfficeLeft = BooleanOptionItem.Create(103201, SpawnPoint.OfficeLeft, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetTag(CustomOptionTags.All);
            Options.RandomSpawnPolusBoilerRoom = BooleanOptionItem.Create(103202, StringNames.BoilerRoom, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetTag(CustomOptionTags.All);
            Options.RandomSpawnPolusSecurity = BooleanOptionItem.Create(103203, StringNames.Security, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetTag(CustomOptionTags.All);
            Options.RandomSpawnPolusDropship = BooleanOptionItem.Create(103204, StringNames.Dropship, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetTag(CustomOptionTags.All);
            Options.RandomSpawnPolusLaboratory = BooleanOptionItem.Create(103205, StringNames.Laboratory, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetTag(CustomOptionTags.All);
            Options.RandomSpawnPolusSpecimens = BooleanOptionItem.Create(103206, StringNames.Specimens, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetTag(CustomOptionTags.All);
            Options.RandomSpawnPolusOfficeRight = BooleanOptionItem.Create(103207, SpawnPoint.OfficeRight, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetTag(CustomOptionTags.All);
            Options.RandomSpawnPolusAdmin = BooleanOptionItem.Create(103208, StringNames.Admin, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetTag(CustomOptionTags.All);
            Options.RandomSpawnPolusComms = BooleanOptionItem.Create(103209, StringNames.Comms, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetTag(CustomOptionTags.All);
            Options.RandomSpawnPolusWeapons = BooleanOptionItem.Create(103210, StringNames.Weapons, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetTag(CustomOptionTags.All);
            Options.RandomSpawnPolusLifeSupp = BooleanOptionItem.Create(103211, StringNames.LifeSupp, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetTag(CustomOptionTags.All);
            Options.RandomSpawnPolusElectrical = BooleanOptionItem.Create(103212, StringNames.Electrical, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetTag(CustomOptionTags.All);
            Options.RandomSpawnPolusStorage = BooleanOptionItem.Create(103213, StringNames.Storage, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetTag(CustomOptionTags.All);
            Options.RandomSpawnPolusRocket = BooleanOptionItem.Create(103214, SpawnPoint.Rocket, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetTag(CustomOptionTags.All);
            Options.RandomSpawnPolusToilet = BooleanOptionItem.Create(103215, SpawnPoint.Toilet, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetTag(CustomOptionTags.All);
            // Airship
            Options.RandomSpawnAirship = BooleanOptionItem.Create(103400, StringNames.MapNameAirship, false, TabGroup.MainSettings, false).SetColorcode("#ff3300").SetParent(Options.EnableRandomSpawn).SetTag(CustomOptionTags.All);
            Options.RandomSpawnAirshipBrig = BooleanOptionItem.Create(103401, StringNames.Brig, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetTag(CustomOptionTags.All);
            Options.RandomSpawnAirshipEngine = BooleanOptionItem.Create(103402, StringNames.Engine, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetTag(CustomOptionTags.All);
            Options.RandomSpawnAirshipKitchen = BooleanOptionItem.Create(103403, StringNames.Kitchen, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetTag(CustomOptionTags.All);
            Options.RandomSpawnAirshipCargoBay = BooleanOptionItem.Create(103404, StringNames.CargoBay, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetTag(CustomOptionTags.All);
            Options.RandomSpawnAirshipRecords = BooleanOptionItem.Create(103405, StringNames.Records, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetTag(CustomOptionTags.All);
            Options.RandomSpawnAirshipMainHall = BooleanOptionItem.Create(103406, StringNames.MainHall, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetTag(CustomOptionTags.All);
            Options.RandomSpawnAirshipNapRoom = BooleanOptionItem.Create(103407, SpawnPoint.NapRoom, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetTag(CustomOptionTags.All);
            Options.RandomSpawnAirshipMeetingRoom = BooleanOptionItem.Create(103408, StringNames.MeetingRoom, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetTag(CustomOptionTags.All);
            Options.RandomSpawnAirshipGapRoom = BooleanOptionItem.Create(103409, StringNames.GapRoom, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetTag(CustomOptionTags.All);
            Options.RandomSpawnAirshipVaultRoom = BooleanOptionItem.Create(103410, StringNames.VaultRoom, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetTag(CustomOptionTags.All);
            Options.RandomSpawnAirshipComms = BooleanOptionItem.Create(103411, StringNames.Comms, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetTag(CustomOptionTags.All);
            Options.RandomSpawnAirshipCockpit = BooleanOptionItem.Create(103412, StringNames.Cockpit, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetTag(CustomOptionTags.All);
            Options.RandomSpawnAirshipArmory = BooleanOptionItem.Create(103413, StringNames.Armory, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetTag(CustomOptionTags.All);
            Options.RandomSpawnAirshipViewingDeck = BooleanOptionItem.Create(103414, StringNames.ViewingDeck, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetTag(CustomOptionTags.All);
            Options.RandomSpawnAirshipSecurity = BooleanOptionItem.Create(103415, StringNames.Security, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetTag(CustomOptionTags.All);
            Options.RandomSpawnAirshipElectrical = BooleanOptionItem.Create(103416, StringNames.Electrical, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetTag(CustomOptionTags.All);
            Options.RandomSpawnAirshipMedical = BooleanOptionItem.Create(103417, StringNames.Medical, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetTag(CustomOptionTags.All);
            Options.RandomSpawnAirshipToilet = BooleanOptionItem.Create(103418, SpawnPoint.Toilet, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetTag(CustomOptionTags.All);
            Options.RandomSpawnAirshipShowers = BooleanOptionItem.Create(103419, StringNames.Showers, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetTag(CustomOptionTags.All);
            // Fungle
            Options.RandomSpawnFungle = BooleanOptionItem.Create(103500, StringNames.MapNameFungle, false, TabGroup.MainSettings, false).SetColorcode("#ff9900").SetParent(Options.EnableRandomSpawn).SetTag(CustomOptionTags.All);
            Options.RandomSpawnFungleKitchen = BooleanOptionItem.Create(103501, StringNames.Kitchen, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetTag(CustomOptionTags.All);
            Options.RandomSpawnFungleBeach = BooleanOptionItem.Create(103502, StringNames.Beach, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetTag(CustomOptionTags.All);
            Options.RandomSpawnFungleBonfire = BooleanOptionItem.Create(103503, SpawnPoint.Bonfire, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetTag(CustomOptionTags.All);
            Options.RandomSpawnFungleGreenhouse = BooleanOptionItem.Create(103504, StringNames.Greenhouse, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetTag(CustomOptionTags.All);
            Options.RandomSpawnFungleComms = BooleanOptionItem.Create(103505, StringNames.Comms, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetTag(CustomOptionTags.All);
            Options.RandomSpawnFungleHighlands = BooleanOptionItem.Create(103506, StringNames.Highlands, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetTag(CustomOptionTags.All);
            Options.RandomSpawnFungleCafeteria = BooleanOptionItem.Create(103507, StringNames.Cafeteria, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetTag(CustomOptionTags.All);
            Options.RandomSpawnFungleRecRoom = BooleanOptionItem.Create(103508, StringNames.RecRoom, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetTag(CustomOptionTags.All);
            Options.RandomSpawnFungleDropship = BooleanOptionItem.Create(103509, StringNames.Dropship, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetTag(CustomOptionTags.All);
            Options.RandomSpawnFungleStorage = BooleanOptionItem.Create(103510, StringNames.Storage, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetTag(CustomOptionTags.All);
            Options.RandomSpawnFungleMeetingRoom = BooleanOptionItem.Create(103511, StringNames.MeetingRoom, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetTag(CustomOptionTags.All);
            Options.RandomSpawnFungleSleepingQuarters = BooleanOptionItem.Create(103512, StringNames.SleepingQuarters, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetTag(CustomOptionTags.All);
            Options.RandomSpawnFungleLaboratory = BooleanOptionItem.Create(103513, StringNames.Laboratory, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetTag(CustomOptionTags.All);
            Options.RandomSpawnFungleReactor = BooleanOptionItem.Create(103514, StringNames.Reactor, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetTag(CustomOptionTags.All);
            Options.RandomSpawnFungleJungleTop = BooleanOptionItem.Create(103515, SpawnPoint.JungleTop, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetTag(CustomOptionTags.All);
            Options.RandomSpawnFungleJungleBottom = BooleanOptionItem.Create(103516, SpawnPoint.JungleBottom, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetTag(CustomOptionTags.All);
            Options.RandomSpawnFungleLookout = BooleanOptionItem.Create(103517, StringNames.Lookout, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetTag(CustomOptionTags.All);
            Options.RandomSpawnFungleMiningPit = BooleanOptionItem.Create(103518, StringNames.MiningPit, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetTag(CustomOptionTags.All);
            Options.RandomSpawnFungleUpperEngine = BooleanOptionItem.Create(103519, StringNames.UpperEngine, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetTag(CustomOptionTags.All);
            Options.RandomSpawnFunglePrecipice = BooleanOptionItem.Create(103520, SpawnPoint.Precipice, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetTag(CustomOptionTags.All);

            // CustomSpawn
            Options.EnableCustomSpawn = BooleanOptionItem.Create(105900, "CustomSpawn", false, TabGroup.MainSettings, false).SetColor(Color.yellow).SetParent(Options.EnableRandomSpawn).SetTag(CustomOptionTags.All);
            Options.RandomSpawnCustom1 = BooleanOptionItem.Create(105901, SpawnPoint.Custom, false, TabGroup.MainSettings, false).SetParent(Options.EnableCustomSpawn).SetTag(CustomOptionTags.All).SetEnabled(() => CustomSpawnManager.CheckActiveSpawns(0));
            Options.RandomSpawnCustom2 = BooleanOptionItem.Create(105902, SpawnPoint.Custom, false, TabGroup.MainSettings, false).SetParent(Options.EnableCustomSpawn).SetTag(CustomOptionTags.All).SetEnabled(() => CustomSpawnManager.CheckActiveSpawns(1));
            Options.RandomSpawnCustom3 = BooleanOptionItem.Create(105903, SpawnPoint.Custom, false, TabGroup.MainSettings, false).SetParent(Options.EnableCustomSpawn).SetTag(CustomOptionTags.All).SetEnabled(() => CustomSpawnManager.CheckActiveSpawns(2));
            Options.RandomSpawnCustom4 = BooleanOptionItem.Create(105904, SpawnPoint.Custom, false, TabGroup.MainSettings, false).SetParent(Options.EnableCustomSpawn).SetTag(CustomOptionTags.All).SetEnabled(() => CustomSpawnManager.CheckActiveSpawns(3));
            Options.RandomSpawnCustom5 = BooleanOptionItem.Create(105905, SpawnPoint.Custom, false, TabGroup.MainSettings, false).SetParent(Options.EnableCustomSpawn).SetTag(CustomOptionTags.All).SetEnabled(() => CustomSpawnManager.CheckActiveSpawns(4));
            Options.RandomSpawnCustom6 = BooleanOptionItem.Create(105906, SpawnPoint.Custom, false, TabGroup.MainSettings, false).SetParent(Options.EnableCustomSpawn).SetTag(CustomOptionTags.All).SetEnabled(() => CustomSpawnManager.CheckActiveSpawns(5));
            Options.RandomSpawnCustom7 = BooleanOptionItem.Create(105907, SpawnPoint.Custom, false, TabGroup.MainSettings, false).SetParent(Options.EnableCustomSpawn).SetTag(CustomOptionTags.All).SetEnabled(() => CustomSpawnManager.CheckActiveSpawns(6));
            Options.RandomSpawnCustom8 = BooleanOptionItem.Create(105908, SpawnPoint.Custom, false, TabGroup.MainSettings, false).SetParent(Options.EnableCustomSpawn).SetTag(CustomOptionTags.All).SetEnabled(() => CustomSpawnManager.CheckActiveSpawns(7));
        }

        public abstract class SpawnMap
        {
            public static Dictionary<byte, string> NextSpornName = new();
            public static Dictionary<byte, Vector2> NextSporn = new();
            public abstract Dictionary<OptionItem, Vector2> Positions { get; }
            public virtual void RandomTeleport(PlayerControl player)
            {
                var roomtext = "";
                Teleport(player, true);
                var pos = GetLocation(ref roomtext, false);
                if (!NextSporn.ContainsKey(player.PlayerId))
                    NextSporn.Add(player.PlayerId, pos);
                else NextSporn[player.PlayerId] = pos;

                if (!NextSpornName.ContainsKey(player.PlayerId))
                    NextSpornName.Add(player.PlayerId, roomtext);
                else NextSpornName[player.PlayerId] = roomtext;
            }
            public virtual void FirstTeleport(PlayerControl player)
            {
                Teleport(player, false);
            }
            private void Teleport(PlayerControl player, bool isRadndom)
            {
                var roomtext = "";
                var location = GetLocation(ref roomtext, !isRadndom);
                if (NextSporn.ContainsKey(player.PlayerId))
                {
                    location = NextSporn[player.PlayerId];
                }
                Logger.Info($"{player.Data.GetLogPlayerName()}:{location}", "RandomSpawn");
                if (Main.NormalOptions.MapId is 0 && GameObject.Find("AprilShip(Clone)") is not null)
                {
                    location = new Vector2(location.x * -1, location.y);
                }
                player.RpcSnapToForced(location);
            }

            public Vector2 GetLocation(ref string roomtext, bool first = false)
            {
                List<Vector2> EnableLocations = new();
                foreach (var data in Positions.Where(o => o.Key.GetBool()))
                    EnableLocations.Add(data.Value);

                List<Vector2> AllLocations = Positions.Values.ToList();

                AddCustomSpawnPoint(EnableLocations, AllLocations);

                var locations = EnableLocations.Count > 0 ? EnableLocations : AllLocations;
                if (first) return locations[0];
                var location = locations.OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault();
                {
                    roomtext = "";

                    if (Positions.ContainsValue(location))
                    {
                        var pos = Positions.Where(x => x.Value == location).FirstOrDefault();
                        roomtext = Translator.GetString(pos.Key.Name, pos.Key.ReplacementDictionary);
                    }
                    else
                    {
                        var Rooms = ShipStatus.Instance.AllRooms;
                        Dictionary<PlainShipRoom, float> Distance = new();

                        var pos = location;
                        if (Rooms != null)
                            foreach (var room in Rooms)
                                Distance.Add(room, Vector2.Distance(pos, room.transform.position));

                        var roo = Distance.OrderByDescending(x => x.Value).Last().Key;

                        if (roo)
                        {
                            var add = "";
                            if (roo.RoomId == SystemTypes.Hallway)
                            {
                                Distance.Remove(roo);
                                var rooo = Distance.OrderByDescending(x => x.Value).Last().Key;
                                add = Translator.GetString($"{rooo.RoomId}");
                            }
                            roomtext = $"☆" + add + Translator.GetString($"{roo.RoomId}");
                        }
                        else
                        {
                            roomtext = Translator.GetString("EDCustomSpawn");
                        }
                    }
                }
                return location;
            }

            public static void AddCustomSpawnPoint(List<Vector2> enableLocations, List<Vector2> allLocations)
            {
                //カスランスポがOFFならさっさとreturn
                if (!Options.EnableCustomSpawn.GetBool()) return;

                var customSpawnMaps = CustomSpawnManager.Data.CurrentPreset.SpawnMaps;
                if (!customSpawnMaps.TryGetValue((MapNames)Main.NormalOptions.MapId, out var customSpawns)) return;

                var spawnPoints = customSpawns.Points;

                //カススポが全く登録されていなかった場合は既存のランスポに切り替え
                if (spawnPoints == null || !spawnPoints.Any()) return;

                //既存のランダムスポーンがOFFの場合はカススポだけを候補に
                if (!IsRandomSpawn(false)) allLocations.Clear();
                allLocations.AddRange(spawnPoints.Select(x => x.Position).ToList());

                if (Options.RandomSpawnCustom1.GetBool() && spawnPoints.Count > 0)
                    enableLocations.Add(spawnPoints[0].Position);

                if (Options.RandomSpawnCustom2.GetBool() && spawnPoints.Count > 1)
                    enableLocations.Add(spawnPoints[1].Position);

                if (Options.RandomSpawnCustom3.GetBool() && spawnPoints.Count > 2)
                    enableLocations.Add(spawnPoints[2].Position);

                if (Options.RandomSpawnCustom4.GetBool() && spawnPoints.Count > 3)
                    enableLocations.Add(spawnPoints[3].Position);

                if (Options.RandomSpawnCustom5.GetBool() && spawnPoints.Count > 4)
                    enableLocations.Add(spawnPoints[4].Position);

                if (Options.RandomSpawnCustom6.GetBool() && spawnPoints.Count > 5)
                    enableLocations.Add(spawnPoints[5].Position);

                if (Options.RandomSpawnCustom7.GetBool() && spawnPoints.Count > 6)
                    enableLocations.Add(spawnPoints[6].Position);

                if (Options.RandomSpawnCustom8.GetBool() && spawnPoints.Count > 7)
                    enableLocations.Add(spawnPoints[7].Position);
            }
        }

        public class SkeldSpawnMap : SpawnMap
        {
            public override Dictionary<OptionItem, Vector2> Positions { get; } = new()
            {
                [Options.RandomSpawnSkeldCafeteria] = new(-1.0f, 3.0f),
                [Options.RandomSpawnSkeldWeapons] = new(9.3f, 1.0f),
                [Options.RandomSpawnSkeldLifeSupp] = new(6.5f, -3.8f),
                [Options.RandomSpawnSkeldNav] = new(16.5f, -4.8f),
                [Options.RandomSpawnSkeldShields] = new(9.3f, -12.3f),
                [Options.RandomSpawnSkeldComms] = new(4.0f, -15.5f),
                [Options.RandomSpawnSkeldStorage] = new(-1.5f, -15.5f),
                [Options.RandomSpawnSkeldAdmin] = new(4.5f, -7.9f),
                [Options.RandomSpawnSkeldElectrical] = new(-7.5f, -8.8f),
                [Options.RandomSpawnSkeldLowerEngine] = new(-17.0f, -13.5f),
                [Options.RandomSpawnSkeldUpperEngine] = new(-17.0f, -1.3f),
                [Options.RandomSpawnSkeldSecurity] = new(-13.5f, -5.5f),
                [Options.RandomSpawnSkeldReactor] = new(-20.5f, -5.5f),
                [Options.RandomSpawnSkeldMedBay] = new(-9.0f, -4.0f)
            };
        }
        public class MiraHQSpawnMap : SpawnMap
        {
            public override Dictionary<OptionItem, Vector2> Positions { get; } = new()
            {
                [Options.RandomSpawnMiraCafeteria] = new(25.5f, 2.0f),
                [Options.RandomSpawnMiraBalcony] = new(24.0f, -2.0f),
                [Options.RandomSpawnMiraStorage] = new(19.5f, 4.0f),
                [Options.RandomSpawnMiraJunction] = new(17.8f, 11.5f),
                [Options.RandomSpawnMiraComms] = new(15.3f, 3.8f),
                [Options.RandomSpawnMiraMedBay] = new(15.5f, -0.5f),
                [Options.RandomSpawnMiraLockerRoom] = new(9.0f, 1.0f),
                [Options.RandomSpawnMiraDecontamination] = new(6.1f, 6.0f),
                [Options.RandomSpawnMiraLaboratory] = new(9.5f, 12.0f),
                [Options.RandomSpawnMiraReactor] = new(2.5f, 10.5f),
                [Options.RandomSpawnMiraLaunchpad] = new(-4.5f, 2.0f),
                [Options.RandomSpawnMiraAdmin] = new(21.0f, 17.5f),
                [Options.RandomSpawnMiraOffice] = new(15.0f, 19.0f),
                [Options.RandomSpawnMiraGreenhouse] = new(17.8f, 23.0f)
            };
        }
        public class PolusSpawnMap : SpawnMap
        {
            public override Dictionary<OptionItem, Vector2> Positions { get; } = new()
            {

                [Options.RandomSpawnPolusOfficeLeft] = new(19.5f, -18.0f),
                [Options.RandomSpawnPolusOfficeRight] = new(26.0f, -17.0f),
                [Options.RandomSpawnPolusAdmin] = new(24.0f, -22.5f),
                [Options.RandomSpawnPolusComms] = new(12.5f, -16.0f),
                [Options.RandomSpawnPolusWeapons] = new(12.0f, -23.5f),
                [Options.RandomSpawnPolusBoilerRoom] = new(2.3f, -24.0f),
                [Options.RandomSpawnPolusLifeSupp] = new(2.0f, -17.5f),
                [Options.RandomSpawnPolusElectrical] = new(9.5f, -12.5f),
                [Options.RandomSpawnPolusSecurity] = new(3.0f, -12.0f),
                [Options.RandomSpawnPolusDropship] = new(16.7f, -3.0f),
                [Options.RandomSpawnPolusStorage] = new(20.5f, -12.0f),
                [Options.RandomSpawnPolusRocket] = new(26.7f, -8.5f),
                [Options.RandomSpawnPolusLaboratory] = new(36.5f, -7.5f),
                [Options.RandomSpawnPolusToilet] = new(34.0f, -10.0f),
                [Options.RandomSpawnPolusSpecimens] = new(36.5f, -22.0f)
            };
        }
        public class AirshipSpawnMap : SpawnMap
        {
            public override Dictionary<OptionItem, Vector2> Positions { get; } = new()
            {
                [Options.RandomSpawnAirshipBrig] = new(-0.7f, 8.5f),
                [Options.RandomSpawnAirshipEngine] = new(-0.7f, -1.0f),
                [Options.RandomSpawnAirshipKitchen] = new(-7.0f, -11.5f),
                [Options.RandomSpawnAirshipCargoBay] = new(33.5f, -1.5f),
                [Options.RandomSpawnAirshipRecords] = new(20.0f, 10.5f),
                [Options.RandomSpawnAirshipMainHall] = new(15.5f, 0.0f),
                [Options.RandomSpawnAirshipNapRoom] = new(6.3f, 2.5f),
                [Options.RandomSpawnAirshipMeetingRoom] = new(17.1f, 14.9f),
                [Options.RandomSpawnAirshipGapRoom] = new(12.0f, 8.5f),
                [Options.RandomSpawnAirshipVaultRoom] = new(-8.9f, 12.2f),
                [Options.RandomSpawnAirshipComms] = new(-13.3f, 1.3f),
                [Options.RandomSpawnAirshipCockpit] = new(-23.5f, -1.6f),
                [Options.RandomSpawnAirshipArmory] = new(-10.3f, -5.9f),
                [Options.RandomSpawnAirshipViewingDeck] = new(-13.7f, -12.6f),
                [Options.RandomSpawnAirshipSecurity] = new(5.8f, -10.8f),
                [Options.RandomSpawnAirshipElectrical] = new(16.3f, -8.8f),
                [Options.RandomSpawnAirshipMedical] = new(29.0f, -6.2f),
                [Options.RandomSpawnAirshipToilet] = new(30.9f, 6.8f),
                [Options.RandomSpawnAirshipShowers] = new(21.2f, -0.8f)
            };
            public static void VpRandomTeleport(PlayerControl pc)
            {
                var spawnPoints = CustomNetworkTransformHandleRpcPatch.decupleVanillaSpawnPositions;
                var location = spawnPoints.ToArray().OrderBy(i => Guid.NewGuid()).First();
                Logger.Info($"{pc.Data.GetLogPlayerName()}:{location}", "VpRandomSpawn");
                pc.RpcSnapToForced(new Vector2(location.x / 10, location.y / 10));
            }
        }
        public class FungleSpawnMap : SpawnMap
        {
            public override Dictionary<OptionItem, Vector2> Positions { get; } = new()
            {
                [Options.RandomSpawnFungleKitchen] = new(-17.8f, -7.3f),
                [Options.RandomSpawnFungleBeach] = new(-21.3f, 3.0f),   //海岸
                [Options.RandomSpawnFungleCafeteria] = new(-16.9f, 5.5f),
                [Options.RandomSpawnFungleRecRoom] = new(-17.7f, 0.0f),
                [Options.RandomSpawnFungleBonfire] = new(-9.7f, 2.7f),  //焚き火
                [Options.RandomSpawnFungleDropship] = new(-7.6f, 10.4f),
                [Options.RandomSpawnFungleStorage] = new(2.3f, 4.3f),
                [Options.RandomSpawnFungleMeetingRoom] = new(-4.2f, -2.2f),
                [Options.RandomSpawnFungleSleepingQuarters] = new(1.7f, -1.4f),  //宿舎
                [Options.RandomSpawnFungleLaboratory] = new(-4.2f, -7.9f),
                [Options.RandomSpawnFungleGreenhouse] = new(9.2f, -11.8f),
                [Options.RandomSpawnFungleReactor] = new(21.8f, -7.2f),
                [Options.RandomSpawnFungleJungleTop] = new(4.2f, -5.3f),
                [Options.RandomSpawnFungleJungleBottom] = new(15.9f, -14.8f),
                [Options.RandomSpawnFungleLookout] = new(6.4f, 3.1f),
                [Options.RandomSpawnFungleMiningPit] = new(12.5f, 9.6f),
                [Options.RandomSpawnFungleHighlands] = new(15.5f, 3.9f),    //展望台右の高地
                [Options.RandomSpawnFungleUpperEngine] = new(21.9f, 3.2f),
                [Options.RandomSpawnFunglePrecipice] = new(19.8f, 7.3f),   //通信室下の崖
                [Options.RandomSpawnFungleComms] = new(20.9f, 13.4f),
            };
        }
    }
}
