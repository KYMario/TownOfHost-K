using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Ghost;
using static TownOfHost.DisableDevice;

namespace TownOfHost
{
    //参考元 : https://github.com/ykundesu/SuperNewRoles/blob/master/SuperNewRoles/Mode/SuperHostRoles/BlockTool.cs
    class DisableDevice
    {
        public static bool DoDisable => Options.DisableDevices.GetBool() || Options.IsStandardHAS || optTimeLimitDevices || optTurnTimeLimitDevice;
        public static List<byte> DesyncComms = new();
        private static int frame = 0;

        //検知
        private static Dictionary<byte, Vector2> AdminPoss = new();
        private static Dictionary<byte, Vector2> LogPoss = new();
        private static Dictionary<byte, Vector2> VitalPoss = new();
        //タイマー
        public static float GameAdminTimer;
        public static float GameLogAndCamTimer;
        public static float GameVitalTimer;
        //ターンでのタイマー
        public static float TurnAdminTimer;
        public static float TurnLogAndCamTimer;
        public static float TurnVitalTimer;
        //カメラ検知用
        public static int UseCount;

        //設定
        public static bool optTimeLimitDevices;
        public static bool optTurnTimeLimitDevice;
        public static float optTimeLimitCamAndLog;
        public static float optTurnTimeLimitCamAndLog;
        public static float optTurnTimeLimitAdmin;
        public static float optTurnTimeLimitVital;
        public static float optTimeLimitAdmin;
        public static float optTimeLimitVital;
        public static bool DisableDevicesIgnoreImpostors;
        public static bool DisableDevicesIgnoreMadmates;
        public static bool DisableDevicesIgnoreNeutrals;
        public static bool DisableDevicesIgnoreCrewmates;
        public static bool DisableDevicesIgnoreAfterAnyoneDied;
        [Attributes.GameModuleInitializer]
        public static void Reset()
        {
            DesyncComms.Clear();
            optTimeLimitCamAndLog = Options.TimeLimitCamAndLog.GetFloat();
            optTimeLimitDevices = Options.TimeLimitDevices.GetBool();
            optTurnTimeLimitDevice = Options.TurnTimeLimitDevice.GetBool();
            optTimeLimitAdmin = Options.TimeLimitAdmin.GetFloat();
            optTimeLimitVital = Options.TimeLimitVital.GetFloat();
            optTurnTimeLimitCamAndLog = Options.TurnTimeLimitCamAndLog.GetFloat();
            optTurnTimeLimitAdmin = Options.TurnTimeLimitAdmin.GetFloat();
            optTurnTimeLimitVital = Options.TurnTimeLimitVital.GetFloat();
            DisableDevicesIgnoreImpostors = Options.DisableDevicesIgnoreImpostors.GetBool();
            DisableDevicesIgnoreMadmates = Options.DisableDevicesIgnoreMadmates.GetBool();
            DisableDevicesIgnoreNeutrals = Options.DisableDevicesIgnoreNeutrals.GetBool();
            DisableDevicesIgnoreCrewmates = Options.DisableDevicesIgnoreCrewmates.GetBool();
            DisableDevicesIgnoreAfterAnyoneDied = Options.DisableDevicesIgnoreAfterAnyoneDied.GetBool();
            CloseVitals.Ability = false;
            AdminPoss.Clear();
            LogPoss.Clear();
            VitalPoss.Clear();
            GameAdminTimer = 0;
            GameLogAndCamTimer = 0;
            GameVitalTimer = 0;
            TurnAdminTimer = 0;
            TurnLogAndCamTimer = 0;
            TurnVitalTimer = 0;
            UseCount = 0;
        }
        public static void StartMeeting()
        {
            CloseVitals.Ability = false;
            AdminPoss.Clear();
            LogPoss.Clear();
            VitalPoss.Clear();
            UseCount = 0;

            if (optTimeLimitDevices)
            {
                Logger.Info($"アドミンの残り時間：{Math.Round(optTimeLimitAdmin - GameAdminTimer)}s", "TimeLimitDevices");
                Logger.Info($"カメラ/ログの残り時間：{Math.Round(optTimeLimitCamAndLog - GameLogAndCamTimer)}s", "TimeLimitDevices");
                Logger.Info($"バイタルの残り時間：{Math.Round(optTimeLimitVital - GameVitalTimer)}s", "TimeLimitDevices");
            }
            if (optTurnTimeLimitDevice)
            {
                Logger.Info($"このターンのアドミンの残り時間：{Math.Round(optTurnTimeLimitAdmin - TurnAdminTimer)}s", "TimeLimitDevices");
                Logger.Info($"このターンのカメラ/ログの残り時間：{Math.Round(optTurnTimeLimitCamAndLog - TurnLogAndCamTimer)}s", "TimeLimitDevices");
                Logger.Info($"このターンのバイタルの残り時間：{Math.Round(optTurnTimeLimitVital - TurnVitalTimer)}s", "TimeLimitDevices");
            }
            TurnAdminTimer = 0;
            TurnLogAndCamTimer = 0;
            TurnVitalTimer = 0;
        }
        public static string GetAddminTimer(bool mark = true)
        {
            if (optTimeLimitAdmin == 0
            || (MapNames)Main.NormalOptions.MapId is MapNames.Fungle) return "";
            var a = "<#00ff99>Ⓐ";
            if (!mark) a = "";
            if (optTimeLimitAdmin <= GameAdminTimer) return a + "×";
            else return a + ":" + Math.Round(optTimeLimitAdmin - GameAdminTimer) + "s";
        }
        public static string GetCamTimr(bool mark = true)
        {
            if (optTimeLimitCamAndLog == 0
            || (MapNames)Main.NormalOptions.MapId is MapNames.Fungle) return "";
            var a = (MapNames)Main.NormalOptions.MapId is MapNames.MiraHQ ? "<#cccccc>Ⓛ" : "<#cccccc>Ⓒ";
            if (!mark) a = "";
            if (optTimeLimitCamAndLog <= GameLogAndCamTimer) return a + "×";
            else return a + ":" + Math.Round(optTimeLimitCamAndLog - GameLogAndCamTimer) + "s";
        }
        public static string GetVitalTimer(bool mark = true)
        {
            if (optTimeLimitVital == 0
            || (MapNames)Main.NormalOptions.MapId is MapNames.Skeld or MapNames.MiraHQ) return "";
            var a = "<#33ccff>Ⓥ";
            if (!mark) a = "";
            if (optTimeLimitVital <= GameVitalTimer) return a + "×";
            else return a + ":" + Math.Round(optTimeLimitVital - GameVitalTimer) + "s";
        }
        public static readonly Dictionary<string, Vector2> DevicePos = new()
        {
            ["SkeldAdmin"] = new(3.48f, -8.62f),
            ["SkeldCamera"] = new(-13.06f, -2.45f),
            ["MiraHQAdmin"] = new(21.02f, 19.09f),
            ["MiraHQDoorLog"] = new(16.22f, 5.82f),
            ["PolusLeftAdmin"] = new(22.80f, -21.52f),
            ["PolusRightAdmin"] = new(24.66f, -21.52f),
            ["PolusCamera"] = new(2.96f, -12.74f),
            ["PolusVital"] = new(26.70f, -15.94f),
            ["AirshipCockpitAdmin"] = new(-22.32f, 0.91f),
            ["AirshipRecordsAdmin"] = new(19.89f, 12.60f),
            ["AirshipCamera"] = new(8.10f, -9.63f),
            ["AirshipVital"] = new(25.24f, -7.94f),
            ["FungleVital"] = new(-2.765f, -9.819f)
        };
        public static float UsableDistance()
        {
            var Map = (MapNames)Main.NormalOptions.MapId;
            return Map switch
            {
                MapNames.Skeld => 1.8f,
                MapNames.MiraHQ => 2.4f,
                MapNames.Polus => 1.8f,
                //MapNames.Dleks => 1.5f,
                MapNames.Airship => 1.8f,
                MapNames.Fungle => 1.8f,
                _ => 0.0f
            };
        }
        //役職によって使えるか異なるならこっちつかーう
        //意味わからない処理すぎて笑えてくる。
        public static bool AdminUsecheck(PlayerControl player, bool? i = null)
        {
            if (player == null) return false;
            if (!player.IsAlive() && player.PlayerId == 0) return true;
            else if (!player.IsAlive()) return false;

            if (DemonicCrusher.DemUseAbility) return i == null;

            if (optTimeLimitAdmin > 0 && GameAdminTimer > optTimeLimitAdmin) return i == null;

            if (optTurnTimeLimitAdmin > 0 && TurnAdminTimer > optTurnTimeLimitAdmin) return i == null;

            if (player.Is(CustomRoles.InfoPoor) ||
                (RoleAddAddons.GetRoleAddon(player.GetCustomRole(), out var data, player, subrole: CustomRoles.InfoPoor) &&
                data.GiveInfoPoor.GetBool()))
                return i == null;

            if (player.Is(CustomRoles.MassMedia)) return i == null;

            return i ?? false;
        }
        public static bool VitealUsecheck(PlayerControl player, bool? i = null)
        {
            if (player == null) return false;
            if (!player.IsAlive() && player.PlayerId == 0) return true;
            else if (!player.IsAlive()) return false;

            if (DemonicCrusher.DemUseAbility) return i == null;

            if (optTimeLimitVital > 0 && GameVitalTimer > optTimeLimitVital) return i == null;

            if (optTurnTimeLimitVital > 0 && TurnVitalTimer > optTurnTimeLimitVital) return i == null;

            if (player.Is(CustomRoles.InfoPoor) ||
                            (RoleAddAddons.GetRoleAddon(player.GetCustomRole(), out var data, player, subrole: CustomRoles.InfoPoor) &&
                            data.GiveInfoPoor.GetBool()))
                return i == null;

            if (player.Is(CustomRoles.MassMedia)) return i == null;

            return i ?? false;
        }

        public static bool LogAndCamUsecheck(PlayerControl player, bool? i = null)
        {
            if (player == null) return false;
            if (!player.IsAlive() && player.PlayerId == 0) return true;
            else if (!player.IsAlive()) return false;

            if (DemonicCrusher.DemUseAbility) return i == null;

            if (optTimeLimitCamAndLog > 0 && GameLogAndCamTimer > optTimeLimitCamAndLog) return i == null;

            if (optTurnTimeLimitCamAndLog > 0 && TurnLogAndCamTimer > optTurnTimeLimitCamAndLog) return i == null;

            if (player.Is(CustomRoles.InfoPoor) ||
                            (RoleAddAddons.GetRoleAddon(player.GetCustomRole(), out var data, player, subrole: CustomRoles.InfoPoor) &&
                            data.GiveInfoPoor.GetBool()))
                return i == null;

            //ここから
            if (player.Is(CustomRoles.MassMedia)) return i == null;

            return (bool)(i != null ? i : false);
        }
        public static void AdminTimer(PlayerControl pc, Vector2 pos)
        {
            if (AdminPoss.TryGetValue(pc.PlayerId, out var p))
            {
                if (p == pos)
                {
                    if (optTimeLimitDevices) GameAdminTimer += Time.fixedDeltaTime;
                    if (optTurnTimeLimitDevice) TurnAdminTimer += Time.fixedDeltaTime;
                }
                else AdminPoss[pc.PlayerId] = pos;
            }
            else AdminPoss.TryAdd(pc.PlayerId, pos);
        }
        public static void LogTimer(PlayerControl pc, Vector2 pos)
        {
            if (LogPoss.TryGetValue(pc.PlayerId, out var p))
            {
                if (p == pos)
                {
                    if (optTimeLimitDevices) GameLogAndCamTimer += Time.fixedDeltaTime;
                    if (optTurnTimeLimitDevice) TurnLogAndCamTimer += Time.fixedDeltaTime;
                }
                else LogPoss[pc.PlayerId] = pos;
            }
            else LogPoss.TryAdd(pc.PlayerId, pos);
        }
        public static void VitalTimer(PlayerControl pc, Vector2 pos)
        {
            if (VitalPoss.TryGetValue(pc.PlayerId, out var p))
            {
                if (p == pos)
                {
                    if (optTimeLimitDevices) GameVitalTimer += Time.fixedDeltaTime;
                    if (optTurnTimeLimitDevice) TurnVitalTimer += Time.fixedDeltaTime;
                }
                else VitalPoss[pc.PlayerId] = pos;
            }
            else VitalPoss.TryAdd(pc.PlayerId, pos);
        }
        public static void FixedUpdate()
        {
            frame = frame == 3 ? 0 : ++frame;
            //if (frame > 0) return;

            //if (!DoDisable) return;
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                try
                {
                    if (pc.IsModClient()) continue;

                    bool doComms = false;
                    bool RoleDisable = false;
                    bool IsComms = Utils.IsActive(SystemTypes.Comms);
                    Vector2 PlayerPos = pc.GetTruePosition();
                    bool check = false;

                    if (DisableDevicesIgnoreImpostors && pc.Is(CustomRoleTypes.Impostor)) check |= true;
                    if (DisableDevicesIgnoreMadmates && pc.Is(CustomRoleTypes.Madmate)) check |= true;
                    if (DisableDevicesIgnoreNeutrals && pc.Is(CustomRoleTypes.Neutral)) check |= true;
                    if (DisableDevicesIgnoreCrewmates && pc.Is(CustomRoleTypes.Crewmate)) check |= true;
                    if (DisableDevicesIgnoreAfterAnyoneDied && GameStates.AlreadyDied) check |= true;

                    if (pc.IsAlive() && !IsComms)
                    {
                        var usableDistance = UsableDistance();
                        switch (Main.NormalOptions.MapId)
                        {
                            case 0:
                                if (Vector2.Distance(PlayerPos, DevicePos["SkeldAdmin"]) <= usableDistance)
                                {
                                    doComms |= Options.DisableSkeldAdmin.GetBool();
                                    RoleDisable |= AdminUsecheck(pc);
                                    if (!pc.inVent && !doComms && !RoleDisable) AdminTimer(pc, PlayerPos);
                                }
                                if (Vector2.Distance(PlayerPos, DevicePos["SkeldCamera"]) <= usableDistance)
                                {
                                    doComms |= Options.DisableSkeldCamera.GetBool();
                                    RoleDisable |= LogAndCamUsecheck(pc);
                                }
                                break;
                            case 1:
                                if (Vector2.Distance(PlayerPos, DevicePos["MiraHQAdmin"]) <= usableDistance)
                                {
                                    doComms |= Options.DisableMiraHQAdmin.GetBool();
                                    RoleDisable |= AdminUsecheck(pc);
                                    if (!pc.inVent && !doComms && !RoleDisable) AdminTimer(pc, PlayerPos);
                                }
                                if (Vector2.Distance(PlayerPos, DevicePos["MiraHQDoorLog"]) <= usableDistance)
                                {
                                    doComms |= Options.DisableMiraHQDoorLog.GetBool();
                                    RoleDisable |= LogAndCamUsecheck(pc);
                                    if (!pc.inVent && !doComms && !RoleDisable) LogTimer(pc, PlayerPos);
                                }
                                break;
                            case 2:
                                if ((Vector2.Distance(PlayerPos, DevicePos["PolusLeftAdmin"]) <= usableDistance && (PlayerPos.y < -19.8f)) || (Vector2.Distance(PlayerPos, DevicePos["PolusRightAdmin"]) <= usableDistance && (PlayerPos.y < -19.8f)))
                                {
                                    doComms |= Options.DisablePolusAdmin.GetBool();
                                    RoleDisable |= AdminUsecheck(pc);
                                    if (!pc.inVent && !doComms && !RoleDisable) AdminTimer(pc, PlayerPos);
                                }
                                if (Vector2.Distance(PlayerPos, DevicePos["PolusCamera"]) <= usableDistance)
                                {
                                    doComms |= Options.DisablePolusCamera.GetBool();
                                    RoleDisable |= LogAndCamUsecheck(pc);
                                }
                                if (Vector2.Distance(PlayerPos, DevicePos["PolusVital"]) <= usableDistance && (PlayerPos.y < -15.8f))
                                {
                                    doComms |= Options.DisablePolusVital.GetBool();
                                    RoleDisable |= VitealUsecheck(pc);
                                    if (!pc.inVent && !doComms && !RoleDisable) VitalTimer(pc, PlayerPos);
                                }
                                break;
                            case 4:
                                if (Vector2.Distance(PlayerPos, DevicePos["AirshipCockpitAdmin"]) <= usableDistance)
                                {
                                    doComms |= Options.DisableAirshipCockpitAdmin.GetBool();
                                    RoleDisable |= AdminUsecheck(pc);
                                    if (!pc.inVent && !doComms && !RoleDisable) AdminTimer(pc, PlayerPos);
                                }
                                if (Vector2.Distance(PlayerPos, DevicePos["AirshipRecordsAdmin"]) <= usableDistance)
                                {
                                    doComms |= Options.DisableAirshipRecordsAdmin.GetBool();
                                    RoleDisable |= AdminUsecheck(pc);
                                    if (!pc.inVent && !doComms && !RoleDisable) AdminTimer(pc, PlayerPos);
                                }
                                if (Vector2.Distance(PlayerPos, DevicePos["AirshipCamera"]) <= usableDistance)
                                {
                                    doComms |= Options.DisableAirshipCamera.GetBool();
                                    RoleDisable |= LogAndCamUsecheck(pc);
                                }
                                if (Vector2.Distance(PlayerPos, DevicePos["AirshipVital"]) <= usableDistance)
                                {
                                    doComms |= Options.DisableAirshipVital.GetBool();
                                    RoleDisable |= VitealUsecheck(pc);
                                    if (!pc.inVent && !doComms && !RoleDisable) VitalTimer(pc, PlayerPos);
                                }
                                break;
                            case 5:
                                if (Vector2.Distance(PlayerPos, DevicePos["FungleVital"]) <= usableDistance)
                                {
                                    doComms |= Options.DisableFungleVital.GetBool();
                                    RoleDisable |= VitealUsecheck(pc);
                                    if (!pc.inVent && !doComms && !RoleDisable) VitalTimer(pc, PlayerPos);
                                }
                                break;
                        }
                    }
                    if (check) doComms = false;
                    if ((RoleDisable || doComms) && !pc.inVent && GameStates.IsInTask)
                    {
                        if (!DesyncComms.Contains(pc.PlayerId))
                            DesyncComms.Add(pc.PlayerId);
                        else return;

                        pc.RpcDesyncUpdateSystem(SystemTypes.Comms, 128);
                    }
                    else if (!IsComms && DesyncComms.Contains(pc.PlayerId))
                    {
                        var sender = CustomRpcSender.Create("Reset", Hazel.SendOption.None);
                        DesyncComms.Remove(pc.PlayerId);
                        sender.AutoStartRpc(ShipStatus.Instance.NetId, RpcCalls.UpdateSystem, pc.GetClientId())
                                .Write((byte)SystemTypes.Comms)
                                .WriteNetObject(pc)
                                .Write((byte)16)
                                .EndRpc();

                        if (Main.NormalOptions.MapId is 1 or 5)
                        {
                            sender.AutoStartRpc(ShipStatus.Instance.NetId, RpcCalls.UpdateSystem, pc.GetClientId())
                                    .Write((byte)SystemTypes.Comms)
                                    .WriteNetObject(pc)
                                    .Write((byte)17)
                                    .EndRpc();
                        }

                        sender.EndMessage();
                        sender.SendMessage();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex, "DisableDevice");
                }
            }
        }
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
    public class RemoveDisableDevicesPatch
    {
        public static void Postfix()
        {
            if (GameStates.IsFreePlay && Main.EditMode)
                GameObject.FindObjectsOfType<SystemConsole>(true).DoIf(x => x.name == "TaskAddConsole", x => x.gameObject.SetActive(false));

            UpdateDisableDevices();
        }

        public static void UpdateDisableDevices(bool kyouseikousin = false)
        {
            var player = PlayerControl.LocalPlayer;
            bool ignore = player.Is(CustomRoles.GM) ||
                !player.IsAlive() ||
                !Options.DisableDevices.GetBool() ||
                (DisableDevicesIgnoreImpostors && player.Is(CustomRoleTypes.Impostor)) ||
                (DisableDevicesIgnoreMadmates && player.Is(CustomRoleTypes.Madmate)) ||
                (DisableDevicesIgnoreNeutrals && player.Is(CustomRoleTypes.Neutral)) ||
                (DisableDevicesIgnoreCrewmates && player.Is(CustomRoleTypes.Crewmate)) ||
                (DisableDevicesIgnoreAfterAnyoneDied && GameStates.AlreadyDied);
            var admins = GameObject.FindObjectsOfType<MapConsole>(true);
            var consoles = GameObject.FindObjectsOfType<SystemConsole>(true);
            if (admins == null || consoles == null) return;
            switch (Main.NormalOptions.MapId)
            {
                case 0:
                    if (Options.DisableSkeldAdmin.GetBool() || AdminUsecheck(player) || kyouseikousin)
                        admins[0].gameObject.GetComponent<CircleCollider2D>().enabled = AdminUsecheck(player, ignore);
                    if (Options.DisableSkeldCamera.GetBool() || LogAndCamUsecheck(player) || kyouseikousin)
                        consoles.DoIf(x => x.name == "SurvConsole", x => x.gameObject.GetComponent<PolygonCollider2D>().enabled = LogAndCamUsecheck(player, ignore));
                    break;
                case 1:
                    if (Options.DisableMiraHQAdmin.GetBool() || AdminUsecheck(player) || kyouseikousin)
                        admins[0].gameObject.GetComponent<CircleCollider2D>().enabled = AdminUsecheck(player, ignore);
                    if (Options.DisableMiraHQDoorLog.GetBool() || LogAndCamUsecheck(player) || kyouseikousin)
                        consoles.DoIf(x => x.name == "SurvLogConsole", x => x.gameObject.GetComponent<BoxCollider2D>().enabled = LogAndCamUsecheck(player, ignore));
                    break;
                case 2:
                    if (Options.DisablePolusAdmin.GetBool() || AdminUsecheck(player) || kyouseikousin)
                        admins.Do(x => x.gameObject.GetComponent<BoxCollider2D>().enabled = AdminUsecheck(player, ignore));
                    if (Options.DisablePolusCamera.GetBool() || LogAndCamUsecheck(player) || kyouseikousin)
                        consoles.DoIf(x => x.name == "Surv_Panel", x => x.gameObject.GetComponent<BoxCollider2D>().enabled = LogAndCamUsecheck(player, ignore));
                    if (Options.DisablePolusVital.GetBool() || VitealUsecheck(player) || kyouseikousin)
                        consoles.DoIf(x => x.name == "panel_vitals", x => x.gameObject.GetComponent<BoxCollider2D>().enabled = VitealUsecheck(player, ignore));
                    break;
                case 4:
                    admins.Do(x =>
                    {
                        if (((Options.DisableAirshipCockpitAdmin.GetBool() || AdminUsecheck(player) || kyouseikousin) && x.name == "panel_cockpit_map") ||
                            ((Options.DisableAirshipRecordsAdmin.GetBool() || AdminUsecheck(player) || kyouseikousin) && x.name == "records_admin_map"))
                            x.gameObject.GetComponent<BoxCollider2D>().enabled = AdminUsecheck(player, ignore);
                    });
                    if (Options.DisableAirshipCamera.GetBool() || LogAndCamUsecheck(player) || kyouseikousin)
                        consoles.DoIf(x => x.name == "task_cams", x => x.gameObject.GetComponent<BoxCollider2D>().enabled = LogAndCamUsecheck(player, ignore));
                    if (Options.DisableAirshipVital.GetBool() || VitealUsecheck(player) || kyouseikousin)
                        consoles.DoIf(x => x.name == "panel_vitals", x => x.gameObject.GetComponent<CircleCollider2D>().enabled = VitealUsecheck(player, ignore));
                    break;
                case 5:
                    if (Options.DisableFungleVital.GetBool() || VitealUsecheck(player) || kyouseikousin)
                    {
                        consoles.DoIf(x => x.name == "VitalsConsole", x => x.GetComponent<Collider2D>().enabled = VitealUsecheck(player, ignore));
                    }
                    break;
            }
        }
    }
    [HarmonyPatch(typeof(Minigame), nameof(Minigame.Close), new Type[] { })]
    class VitalClosePatch
    {
        public static void Postfix(Minigame __instance)
        {
            CloseVitals.Ability = false;
        }
    }
    [HarmonyPatch(typeof(VitalsMinigame), nameof(VitalsMinigame.Update))]
    class CloseVitals
    {
        public static bool Ability;
        public static void Postfix(VitalsMinigame __instance)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                if (Ability && PlayerControl.LocalPlayer.IsAlive()) return;
                if (PlayerControl.LocalPlayer.IsAlive() && DemonicCrusher.DemUseAbility) __instance.Close();

                if (PlayerControl.LocalPlayer.IsAlive())
                {
                    if ((optTimeLimitVital > 0 && GameVitalTimer > optTimeLimitVital)
                    || (optTurnTimeLimitVital > 0 && TurnVitalTimer > optTurnTimeLimitVital))
                    {
                        __instance.Close();
                    }
                    else
                    {
                        if (optTimeLimitDevices) GameVitalTimer += Time.deltaTime;
                        if (optTurnTimeLimitDevice) TurnVitalTimer += Time.deltaTime;
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(PlanetSurveillanceMinigame), nameof(PlanetSurveillanceMinigame.Update))]
    class PCloseCam
    {
        public static void Postfix(PlanetSurveillanceMinigame __instance)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                if (PlayerControl.LocalPlayer.IsAlive() && DemonicCrusher.DemUseAbility) __instance.Close();
                if (PlayerControl.LocalPlayer.IsAlive() && __instance)
                {
                    if ((optTimeLimitCamAndLog > 0 && GameLogAndCamTimer > optTimeLimitCamAndLog)
                    || (optTurnTimeLimitCamAndLog > 0 && TurnLogAndCamTimer > optTurnTimeLimitCamAndLog))
                    {
                        __instance.Close();
                    }
                }
            }
        }
    }
    [HarmonyPatch(typeof(SurveillanceMinigame), nameof(SurveillanceMinigame.Update))]
    class SCloseCam
    {
        public static void Postfix(SurveillanceMinigame __instance)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                if (PlayerControl.LocalPlayer.IsAlive() && DemonicCrusher.DemUseAbility) __instance.Close();

                if (PlayerControl.LocalPlayer.IsAlive() && __instance)
                {
                    if ((optTimeLimitCamAndLog > 0 && GameLogAndCamTimer > optTimeLimitCamAndLog)
                    || (optTurnTimeLimitCamAndLog > 0 && TurnLogAndCamTimer > optTurnTimeLimitCamAndLog))
                        __instance.Close();
                }
            }
        }
    }

    [HarmonyPatch(typeof(SecurityLogGame), nameof(SecurityLogGame.Update))]
    class CloseLog
    {
        public static void Postfix(SecurityLogGame __instance)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                if (PlayerControl.LocalPlayer.IsAlive() && DemonicCrusher.DemUseAbility) __instance.Close();

                if (PlayerControl.LocalPlayer.IsAlive() && __instance)
                {
                    if ((optTimeLimitCamAndLog > 0 && GameLogAndCamTimer > optTimeLimitCamAndLog)
                    || (optTurnTimeLimitCamAndLog > 0 && TurnLogAndCamTimer > optTurnTimeLimitCamAndLog))
                    {
                        __instance.Close();
                    }
                    else
                    {
                        if (optTimeLimitDevices) GameLogAndCamTimer += Time.deltaTime;
                        if (optTurnTimeLimitDevice) TurnLogAndCamTimer += Time.deltaTime;
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(MapCountOverlay), nameof(MapCountOverlay.Update))]
    class CloseAddmin
    {
        public static void Prefix(MapCountOverlay __instance)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                if (GameStates.IsFreePlay && Main.EditMode) return;

                if (PlayerControl.LocalPlayer.IsAlive() && DemonicCrusher.DemUseAbility)
                {
                    MapBehaviour.Instance.Close();
                    return;
                }
                if (PlayerControl.LocalPlayer.IsAlive() && MapBehaviour.Instance && __instance)
                {
                    if ((optTimeLimitAdmin > 0 && GameAdminTimer > optTimeLimitAdmin)
                    || (optTurnTimeLimitAdmin > 0 && TurnAdminTimer > optTurnTimeLimitAdmin))
                    {
                        MapBehaviour.Instance.Close();
                    }
                    else
                    {
                        if (optTimeLimitDevices) GameAdminTimer += Time.deltaTime;
                        if (optTurnTimeLimitDevice) TurnAdminTimer += Time.deltaTime;
                    }
                }
            }
        }
    }
}