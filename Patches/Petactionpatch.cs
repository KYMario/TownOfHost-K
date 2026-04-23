using System;
using System.Collections.Generic;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using TownOfHost.Roles.Core;

namespace TownOfHost.Patches;

/// <summary>
/// ペットを撫でたときにRPCを送信し、役職のOnPetを呼ぶPatch。
/// EHRのLocalPetPatch + ExternalRpcPetPatchを参考に実装。
/// </summary>

// ★ ホスト自身がペットを撫でたとき
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.TryPet))]
internal static class LocalPetPatch
{
    private static readonly Dictionary<byte, long> LastProcess = new();

    public static bool Prefix(PlayerControl __instance)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        if (GameStates.IsLobby || !__instance.IsAlive()) return true;
        if (__instance.petting) return true;

        __instance.petting = true;

        if (!LastProcess.ContainsKey(__instance.PlayerId))
            LastProcess[__instance.PlayerId] = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 2;
        if (LastProcess[__instance.PlayerId] + 1 >= DateTimeOffset.UtcNow.ToUnixTimeSeconds()) return true;

        // ★ 他クライアントにPet RPCを送信
        ExternalRpcPetPatch.Prefix(__instance.MyPhysics, (byte)RpcCalls.Pet);

        LastProcess[__instance.PlayerId] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return true;
    }

    public static void Postfix(PlayerControl __instance)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        __instance.petting = false;
    }
}

// ★ 誰かがペットを撫でたRPCを受信したとき（ホストのみ処理）
[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleRpc))]
internal static class ExternalRpcPetPatch
{
    private static readonly Dictionary<byte, long> LastProcess = new();

    public static void Prefix(PlayerPhysics __instance, [HarmonyArgument(0)] byte callID)
    {
        if ((RpcCalls)callID != RpcCalls.Pet) return;
        if (!AmongUsClient.Instance.AmHost) return;
        if (GameStates.IsLobby) return;

        var pc = __instance.myPlayer;
        if (pc == null || !pc.IsAlive()) return;

        if (!LastProcess.ContainsKey(pc.PlayerId))
            LastProcess[pc.PlayerId] = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 2;
        if (LastProcess[pc.PlayerId] + 1 >= DateTimeOffset.UtcNow.ToUnixTimeSeconds()) return;

        LastProcess[pc.PlayerId] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        Logger.Info($"{pc.Data?.GetLogPlayerName()} がペットを撫でた", "PetActionPatch");

        // ★ 役職のOnPetを呼ぶ
        OnPetUse(pc);
    }

    private static void OnPetUse(PlayerControl pc)
    {
        if (pc == null || !pc.IsAlive()) return;
        if (!AmongUsClient.Instance.AmHost) return;
        if (GameStates.IsLobby || GameStates.IsMeeting) return;

        // ★ ベント中・梯子中などはスキップ
        if (pc.inVent || pc.inMovingPlat || pc.onLadder || pc.walkingToVent) return;
        if (pc.MyPhysics.Animations.IsPlayingEnterVentAnimation()) return;
        if (pc.MyPhysics.Animations.IsPlayingAnyLadderAnimation()) return;

        // ★ 登録されたPetActionハンドラを呼ぶ
        if (PetActionManager.Handlers.TryGetValue(pc.PlayerId, out var handler))
        {
            handler.Invoke();
            Logger.Info($"{pc.Data?.GetLogPlayerName()} のOnPet実行", "PetActionPatch");
        }
    }
}

/// <summary>
/// ペットIDをRPCで変更するヘルパー
/// EHRのPetsHelper.SetPetを参考に実装
/// </summary>
public static class PetsHelper
{
    // ★ プレイヤーのペットをRPCで変更
    public static void SetPet(PlayerControl pc, string petId)
    {
        if (pc == null) return;

        try { pc.SetPet(petId); }
        catch { }

        try { pc.Data.DefaultOutfit.PetSequenceId += 10; }
        catch { }

        var sender = CustomRpcSender.Create("PetsHelper.SetPet", SendOption.Reliable);
        sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetPetStr)
            .Write(petId)
            .Write(pc.GetNextRpcSequenceId(RpcCalls.SetPetStr))
            .EndRpc();
        sender.SendMessage();
    }

    // ★ 死亡したプレイヤーのペットを外す
    public static void RemovePet(PlayerControl pc)
    {
        if (pc == null || !pc.Data.IsDead || pc.IsAlive()) return;
        if (pc.CurrentOutfit.PetId == "") return;
        SetPet(pc, "");
    }
}

/// <summary>
/// 役職ごとのペット撫でハンドラを管理するクラス。
/// 役職のコンストラクタでハンドラを登録し、OnDestroyで解除する。
/// </summary>
public static class PetActionManager
{
    // PlayerId → ペット撫でアクション
    public static readonly Dictionary<byte, System.Action> Handlers = new();

    // ★ ハンドラを登録（役職のコンストラクタで呼ぶ）
    public static void Register(byte playerId, System.Action action)
    {
        Handlers[playerId] = action;
    }

    // ★ ハンドラを解除（役職のOnDestroyで呼ぶ）
    public static void Unregister(byte playerId)
    {
        Handlers.Remove(playerId);
    }

    // ★ 全ハンドラをクリア（ゲーム終了時）
    public static void Reset()
    {
        Handlers.Clear();
    }
}