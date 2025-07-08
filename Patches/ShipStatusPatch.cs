using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TownOfHost.Modules;
using UnityEngine;

namespace TownOfHost
{
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.FixedUpdate))]
    class ShipFixedUpdatePatch
    {
        public static void Postfix(ShipStatus __instance)
        {
            //ここより上、全員が実行する
            if (!AmongUsClient.Instance.AmHost) return;
            //ここより下、ホストのみが実行する
            if ((Options.CurrentGameMode == CustomGameMode.HideAndSeek || Options.IsStandardHAS) && GameStates.introDestroyed)
            {
                if (Options.HideAndSeekKillDelayTimer > 0)
                {
                    Options.HideAndSeekKillDelayTimer -= Time.fixedDeltaTime;
                }
                else if (!float.IsNaN(Options.HideAndSeekKillDelayTimer))
                {
                    UtilsOption.MarkEveryoneDirtySettings();
                    Options.HideAndSeekKillDelayTimer = float.NaN;
                    Logger.Info("キル能力解禁", "HideAndSeek");
                }
            }
        }
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.UpdateSystem), typeof(SystemTypes), typeof(PlayerControl), typeof(byte))]
    class ShipStatusUpdateSystemPatch
    {
        public static void Prefix(ShipStatus __instance,
            [HarmonyArgument(0)] SystemTypes systemType,
            [HarmonyArgument(1)] PlayerControl player,
            [HarmonyArgument(2)] byte amount)
        {
            if (systemType != SystemTypes.Sabotage)
            {
                Logger.Info("SystemType: " + systemType.ToString() + ", PlayerName: " + player.GetNameWithRole().RemoveHtmlTags() + ", amount: " + amount, "UpdateSystem");
            }
            else
            {
                DisableDevice.DesyncComms.Clear();
            }
            if (RepairSender.enabled && AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
            {
                Logger.seeingame("SystemType: " + systemType.ToString() + ", PlayerName: " + player.GetNameWithRole().RemoveHtmlTags() + ", amount: " + amount);
            }
        }
        public static void CheckAndOpenDoorsRange(ShipStatus __instance, int amount, int min, int max)
        {
            var Ids = new List<int>();
            for (var i = min; i <= max; i++)
            {
                Ids.Add(i);
            }
            CheckAndOpenDoors(__instance, amount, Ids.ToArray());
        }
        private static void CheckAndOpenDoors(ShipStatus __instance, int amount, params int[] DoorIds)
        {
            if (DoorIds.Contains(amount)) foreach (var id in DoorIds)
                {
                    __instance.RpcUpdateSystem(SystemTypes.Doors, (byte)id);
                }
        }
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CloseDoorsOfType))]
    class CloseDoorsPatch
    {
        public static bool Prefix(ShipStatus __instance)
        {
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek || Options.IsStandardHAS) return false;
            if (Options.CurrentGameMode != CustomGameMode.Standard || SuddenDeathMode.NowSuddenDeathMode) return false;

            return !Options.AllowCloseDoors.GetBool();
        }
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
    class StartPatch
    {
        public static void Postfix()
        {
            Logger.CurrentMethod();
            Logger.Info("-----------ゲーム開始-----------", "Phase");
            if (GameStates.IsFreePlay && Main.EditMode)
            {
                CustomSpawnEditor.CustomSpawnPosition.TryAdd(AmongUsClient.Instance.TutorialMapId, new List<Vector2>());
                _ = new LateTask(() =>
                {
                    try
                    {
                        CustomSpawnEditor.Setup();
                    }
                    finally
                    {
                        Logger.Error("0.2f後でErrorが発生したため5秒後に再度実行", "SetCustomSporn");
                        _ = new LateTask(() => CustomSpawnEditor.Setup(), 5f, "SetCustomSporn");
                    }
                }, 0.2f, "SetCustomSporn");
                return;
            }
            if (GameStates.IsModHost && Main.UseWebHook.Value) UtilsWebHook.WH_ShowActiveRoles();
            PlayerCatch.CountAlivePlayers(true);
            Main.RTAMode = Options.CurrentGameMode == CustomGameMode.TaskBattle && PlayerCatch.AllPlayerControls.Count() == (Options.EnableGM.GetBool() ? 2 : 1);
            //RTAモードじゃないならLateTaskを作らない
            if (!Main.RTAMode) return;
            _ = new LateTask(() =>
            {
                var playerRTA = Options.EnableGM.GetBool() ? PlayerCatch.AllAlivePlayerControls.Where(p => p.PlayerId != PlayerControl.LocalPlayer.PlayerId).First() : PlayerControl.LocalPlayer;
                if (playerRTA == null)
                {
                    Logger.Warn("[TR] プレイヤーがnullです", "TaskBattle RTA");
                    return;
                }
                Main.RTAPlayer = playerRTA.PlayerId;
                HudManagerPatch.TaskBattleTimer = 0f;
            }, 1f, "TaskBattle TimerReset");
        }
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.StartMeeting))]
    class StartMeetingPatch
    {
        public static void Prefix(ShipStatus __instance, PlayerControl reporter, NetworkedPlayerInfo target)
        {
            MeetingStates.ReportTarget = target;
            MeetingStates.DeadBodies = UnityEngine.Object.FindObjectsOfType<DeadBody>();
        }
        public static void Postfix()
        {
            // 全プレイヤーを湧いてない状態にする
            foreach (var state in PlayerState.AllPlayerStates.Values)
            {
                state.HasSpawned = false;
            }
        }
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Begin))]
    class BeginPatch
    {
        public static void Postfix()
        {
            Logger.CurrentMethod();

            //ホストの役職初期設定はここで行うべき？
        }
    }
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.CheckTaskCompletion))]
    class CheckTaskCompletionPatch
    {
        public static bool Prefix(ref bool __result)
        {
            if (Options.DisableTaskWin.GetBool() || Options.NoGameEnd.GetBool() || TaskState.InitialTotalTasks == 0)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}