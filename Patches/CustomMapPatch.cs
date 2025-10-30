using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using TownOfHost.Attributes;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using UnityEngine;

namespace TownOfHost;

class SubmergedPatch
{
    private const string SubmergedGuid = "Submerged";
    private const byte MapId = 6;
    private const byte SpawnDoneId = 2;
    private const float FLOOR_CUTOFF = -6.19f;
    private const string STRMISS = "STRMISS";

    private static byte CurrentMapId => GameStates.IsFreePlay ? (byte)AmongUsClient.Instance.TutorialMapId : Main.NormalOptions.MapId;
    public static bool IsSubmergedId => CurrentMapId == MapId;
    public static bool IsPluginLoaded => IL2CPPChainloader.Instance.Plugins.ContainsKey(SubmergedGuid);
    public static bool GetPluginInfo(out PluginInfo info) => IL2CPPChainloader.Instance.Plugins.TryGetValue(SubmergedGuid, out info);

    public static Type GetType(Assembly assembly, string typeName)
        => assembly.GetTypes().FirstOrDefault(x => x.Name == typeName);

    public static MethodInfo GetMethod(Type type, string methodName)
        => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).FirstOrDefault(x => x.Name == methodName);

    public static MethodInfo GetMethod(Assembly assembly, string typeName, string methodName)
    {
        var type = GetType(assembly, typeName);
        if (type == null) return null;

        return GetMethod(type, methodName);
    }

    public static bool CheckStackTrace()
    {
        var trace = new StackTrace(1, false);
        var frames = trace.GetFrames();

        if (frames == null)
            return false;

        for (int i = 1; i < frames.Length && i < 6; i++)
        {
            var asm = frames[i].GetMethod()?.DeclaringType?.Assembly;
            if (asm == Assembly.GetAssembly(typeof(SubmergedPatch)))
            {
                return true;
            }
        }
        return false;
    }

    public static object GetFloorHandler(CustomNetworkTransform networkTransform)
        => _getFloorHandler.Invoke(null, [networkTransform]);
    public static void RpcRequestChangeFloor(object floorHandler, bool toUpper)
        => _rpcRequestChangeFloor.Invoke(floorHandler, [toUpper]);

    public static bool CheckInElevator(Vector2 position, out object elevator)
    {
        elevator = null;
        var submarineStatus = _submarineStatusField.GetValue(null);
        var elevators = (IList)_elevatorsField.GetValue(submarineStatus);
        if (elevators.Count == 0) return false;

        for (var i = 0; i < elevators.Count; i++)
        {
            elevator = elevators[i];
            if ((bool)_checkInElevatorMethod.Invoke(elevator, [position]))
            {
                return true;
            }
        }
        elevator = null;
        return false;
    }

    public static bool Patched = false;
    public static bool spawnComplete = false;
    public static MethodInfo _getFloorHandler;
    public static MethodInfo _rpcRequestChangeFloor;
    public static FieldInfo _timerField;
    public static FieldInfo _stateField;
    public static FieldInfo _submarineStatusField;
    public static FieldInfo _elevatorsField;
    public static FieldInfo _upperDeckIsTargetFloorField;
    public static FieldInfo _elevatorSystemField;
    public static MethodInfo _checkInElevatorMethod;
    public static FieldInfo _enableRpcSnapToVentPatch;
    public static MethodInfo _rpcSnapToVentPatch;
    public static Dictionary<string, string> StringMap;

    [GameModuleInitializer]
    public static void Init()
    {
        if (Patched) return;
        if (!IsSubmergedId) return;
        if (!GetPluginInfo(out var info)) return;

        StringMap = new();
        spawnComplete = false;
        Patched = true;

        try
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            var submergedAssembly = info.Instance.GetType().Assembly;
            var myAssembly = Main.Instance.GetType().Assembly;

            //会議後の処理
            var wrapUpAndSpawn = GetMethod(submergedAssembly, "SubmergedExileController", "WrapUpAndSpawn");
            var wrapUpPatch = GetMethod(myAssembly, "BaseExileControllerPatch", "Postfix");
            Main.Instance.Harmony.Patch(wrapUpAndSpawn, postfix: new(wrapUpPatch));

            //別の階層にテレポートできるように
            var snapToMethod = typeof(CustomNetworkTransform).GetMethod("SnapTo", flags, null, [typeof(Vector2)], null);
            // var snapToMethod2 = typeof(CustomNetworkTransform).GetMethod("SnapTo", flags, null, [typeof(Vector2), typeof(ushort)], null);
            var snapToMethod2 = GetMethod(myAssembly, "ExtendedRpc", "RpcSnapToForced");
            var snapToMethod3 = GetMethod(myAssembly, "ExtendedRpc", "RpcSnapToDesync");
            var snapToPatch = new HarmonyMethod(GetMethod(myAssembly, nameof(SubmergedPatch), "SnapToPatch"));
            var snapToPatch2 = new HarmonyMethod(GetMethod(myAssembly, nameof(SubmergedPatch), "ModSnapToPatch"));
            Main.Instance.Harmony.Patch(snapToMethod, prefix: snapToPatch);
            Main.Instance.Harmony.Patch(snapToMethod2, prefix: snapToPatch2);
            Main.Instance.Harmony.Patch(snapToMethod3, prefix: snapToPatch2);

            //別の階層へのベント移動をできるように
            var movementVent = GetType(submergedAssembly, "MovementPatches");
            _enableRpcSnapToVentPatch = movementVent.GetField("_enableRpcSnapToPatch", flags);
            _rpcSnapToVentPatch = movementVent.GetMethod("RpcSnapToPatch", flags);

            //スポーン検知方法を変更
            var afterMeeting = GetMethod(myAssembly, "ExileControllerWrapUpPatch", "AfterMeetingTasks");
            var afterMeetingPatch = GetMethod(myAssembly, nameof(SubmergedPatch), "AfterMeetingTasksPatch");
            Main.Instance.Harmony.Patch(afterMeeting, prefix: new(afterMeetingPatch));

            //スポーン検知
            var spawnInSystemType = GetType(submergedAssembly, "SubmarineSpawnInSystem");
            var deteriorate = GetMethod(spawnInSystemType, "Deteriorate");
            var deterioratePatch = GetMethod(myAssembly, nameof(SubmergedPatch), "DeterioratePatch");
            Main.Instance.Harmony.Patch(deteriorate, postfix: new(deterioratePatch));

            var selectSpawnStart = GetMethod(submergedAssembly, "SubmarineSelectSpawn", "Start");
            var selectSpawnStartPatch = GetMethod(myAssembly, nameof(SubmergedPatch), "SelectSpawnStartPatch");
            Main.Instance.Harmony.Patch(selectSpawnStart, postfix: new(selectSpawnStartPatch));

            _timerField = spawnInSystemType.GetField("timer", BindingFlags.Public | BindingFlags.Instance);
            _stateField = spawnInSystemType.GetField("currentState", BindingFlags.Public | BindingFlags.Instance);

            //階層を取得&変更できるように
            var floorHandler = GetType(submergedAssembly, "FloorHandler");
            _getFloorHandler = floorHandler.GetMethod("GetFloorHandler", flags, null, [typeof(CustomNetworkTransform)], null);
            _rpcRequestChangeFloor = floorHandler.GetMethod("RpcRequestChangeFloor", flags, null, [typeof(bool)], null);

            //エレベーター取得
            var submarineStatus = GetType(submergedAssembly, "SubmarineStatus");
            _submarineStatusField = submarineStatus.GetField("instance", flags);
            _elevatorsField = submarineStatus.GetField("elevators", flags);

            var submarineElevator = GetType(submergedAssembly, "SubmarineElevator");
            _checkInElevatorMethod = GetMethod(submarineElevator, "CheckInElevator");
            _elevatorSystemField = submarineElevator.GetField("system", flags);

            var submarineElevatorSystem = GetType(submergedAssembly, "SubmarineElevatorSystem");
            _upperDeckIsTargetFloorField = submarineElevatorSystem.GetField("upperDeckIsTargetFloor", flags);

            //MOD翻訳を適応
            var getString = GetType(myAssembly, "Translator").GetMethod("GetString", flags, null, [typeof(string), typeof(SupportedLangs)], null);
            var getStringPatch = GetMethod(myAssembly, nameof(SubmergedPatch), "GetStringPatch");
            Main.Instance.Harmony.Patch(getString, postfix: new(getStringPatch));
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, nameof(SubmergedPatch));
            Logger.seeingame("Submergedパッチの適用中にエラーが発生しました");
        }
    }

    public static bool ModSnapToPatch(PlayerControl pc, Vector2 position)
    {
        if (!IsSubmergedId) return true;

        var handler = GetFloorHandler(pc.NetTransform);
        var onUpperField = handler.GetType().GetField("onUpper", BindingFlags.Public | BindingFlags.Instance);
        var onUpper = (bool)onUpperField.GetValue(handler);
        bool newUpper = onUpper;

        newUpper = position.y > FLOOR_CUTOFF;

        if (CheckInElevator(position, out var elevator))
        {
            var elevatorSystem = _elevatorSystemField.GetValue(elevator);
            var upper = (bool)_upperDeckIsTargetFloorField.GetValue(elevatorSystem);
            if (upper != newUpper)
            {
                newUpper = upper;
            }
        }

        if (onUpper != newUpper)
        {
            RpcRequestChangeFloor(handler, newUpper);
            if (pc.inVent)
            {
                pc.RpcSnapToForced(position, Hazel.SendOption.Reliable);
                return true;
            }
        }
        return true;
    }

    public static void SnapToPatch(CustomNetworkTransform __instance, [HarmonyArgument(0)] Vector2 pos)
    {
        if (!IsSubmergedId) return;
        if (!CheckStackTrace()) return; //k以外のSnapToは処理しない
        if (__instance.myPlayer.inVent) return;

        ModSnapToPatch(__instance.myPlayer, pos);
    }

    public static bool AfterMeetingTasksPatch()
    {
        if (!IsSubmergedId) return true;

        if (AmongUsClient.Instance.AmHost)
        {
            //まだスポーンしていない
            foreach (var state in PlayerState.AllPlayerStates.Values)
            {
                state.HasSpawned = false;
            }

            if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Default) return false;

            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                pc.ResetKillCooldown();
            }
        }

        FallFromLadder.Reset();
        PlayerCatch.CountAlivePlayers(true);
        Utils.AfterMeetingTasks();

        spawnComplete = false;

        return false;
    }

    public static void SelectSpawnStartPatch()
    {
        if (!IsSubmergedId) return;
        if (!AmongUsClient.Instance.AmHost) return;

        //スポーン画面がでてきたって知らせなきゃ！
        spawnComplete = false;
    }

    public static void DeterioratePatch(object __instance)
    {
        if (!IsSubmergedId) return;
        if (!AmongUsClient.Instance.AmHost) return;

        //スポーン時間が既に終わっている場合は処理しない
        if (spawnComplete || !GameStates.introDestroyed) return;

        var state = (byte)_stateField.GetValue(__instance);

        //ランダムスポーンが有効な場合は待たない
        if (RandomSpawn.IsRandomSpawn())
        {
            _timerField.SetValue(__instance, 0f);
        }

        //全員選択するまで待つ
        if (state != SpawnDoneId) return;

        //スポーンさせる
        spawnComplete = true;
        foreach (var pc in PlayerCatch.AllPlayerControls)
            SpawnPlayer(pc);
    }

    public static void GetStringPatch([HarmonyArgument(0)] string str, ref string __result)
    {
        if (!IsSubmergedId) return;
        //バニラandMODに翻訳が存在しない時のみ処理
        if (__result != $"<INVALID:{str}>") return;

        if (StringMap.TryGetValue(str, out string result))
        {
            __result = result;
            return;
        }

        if (!byte.TryParse(str, out var value)) return;

        result ??= DestroyableSingleton<TranslationController>.Instance.GetString((SystemTypes)value);
        if (result == STRMISS) result = null;

        result ??= DestroyableSingleton<TranslationController>.Instance.GetString((TaskTypes)value);
        if (result == STRMISS) result = null;

        StringMap[__result] = result;
        __result = result ?? __result;
    }

    public static void SpawnPlayer(PlayerControl player)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            //最初のスポーンと判定
            var roleClass = player.GetRoleClass();
            roleClass?.OnSpawn(MeetingStates.FirstMeeting);

            if (SuddenDeathMode.SuddenKillcooltime.GetBool() && SuddenDeathMode.NowSuddenDeathMode)
            {
                PlayerCatch.AllPlayerControls.Do(pc => pc.SetKillCooldown(SuddenDeathMode.SuddenKillcooltime.GetFloat(), delay: true));
            }
            else
            {
                if (Options.FixFirstKillCooldown.GetBool() && !MeetingStates.MeetingCalled && Options.CurrentGameMode != CustomGameMode.TaskBattle)
                    player.SetKillCooldown(Main.AllPlayerKillCooldown[player.PlayerId], delay: true);
                else if (Options.CurrentGameMode != CustomGameMode.TaskBattle && MeetingStates.FirstMeeting)
                    player.SetKillCooldown(10f, delay: true);
            }

            if (MeetingStates.FirstMeeting) player.RpcResetAbilityCooldown();

            GameStates.Intro = false;
            GameStates.AfterIntro = true;

            if (RandomSpawn.IsRandomSpawn())
            {
                new SubmergedSpawnMap().RandomTeleport(player);
            }
        }
        PlayerState.GetByPlayerId(player.PlayerId).HasSpawned = true;
    }

    class SubmergedSpawnMap : RandomSpawn.SpawnMap
    {
        public override Dictionary<OptionItem, Vector2> Positions { get; } = [];
    }
}