using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.InnerNet.GameDataMessages;
using Hazel;
using InnerNet;
using UnityEngine;

// EHRのCustomNetObjectをTOH-P向けに移植
// Credit: https://github.com/Rabek009/MoreGamemodes

namespace TownOfHost.Modules;

public class CustomNetObject
{
    public static readonly List<CustomNetObject> AllObjects = new();
    private static int MaxId = -1;

    // ★ スポーンキュー（複数同時スポーン時の競合を防ぐ）
    private static readonly Queue<Action> SpawnQueue = new();
    private static bool IsSpawning = false;

    protected int Id;
    public PlayerControl PlayerControl;
    public Vector2 Position;

    protected virtual bool IsDynamic => false;

    public void Despawn()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        try
        {
            if (PlayerControl != null)
            {
                MessageWriter writer = MessageWriter.Get(SendOption.Reliable);
                writer.StartMessage(5);
                writer.Write(AmongUsClient.Instance.GameId);
                writer.StartMessage(5);
                writer.WritePacked(PlayerControl.NetId);
                writer.EndMessage();
                writer.EndMessage();
                AmongUsClient.Instance.SendOrDisconnect(writer);
                writer.Recycle();

                AmongUsClient.Instance.RemoveNetObject(PlayerControl);
                UnityEngine.Object.Destroy(PlayerControl.gameObject);
            }
            AllObjects.Remove(this);
        }
        catch (Exception e) { Logger.Error(e.ToString(), "CNO.Despawn"); }
    }

    protected void Hide(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        if (player.AmOwner)
        {
            _ = new LateTask(() =>
            {
                try { PlayerControl.transform.FindChild("Names").FindChild("NameText_TMP").gameObject.SetActive(false); }
                catch { }
                PlayerControl.Visible = false;
            }, 0.1f, "CNO.Hide.Local", true);
            return;
        }

        MessageWriter writer = MessageWriter.Get(SendOption.Reliable);
        writer.StartMessage(6);
        writer.Write(AmongUsClient.Instance.GameId);
        writer.WritePacked(player.OwnerId);
        writer.StartMessage(5);
        writer.WritePacked(PlayerControl.NetId);
        writer.EndMessage();
        writer.EndMessage();
        AmongUsClient.Instance.SendOrDisconnect(writer);
        writer.Recycle();
    }

    protected virtual void OnFixedUpdate() { }

    // ★ 外見を設定（Data.Serializeで全員に送る）
    protected void SetAppearance(int colorId, string skinId = "", string hatId = "", string petId = "", string visorId = "")
    {
        if (PlayerControl == null) return;

        var outfit = PlayerControl.Data.Outfits[PlayerOutfitType.Default];
        outfit.ColorId = colorId;
        outfit.SkinId = skinId ?? "";
        outfit.HatId = hatId ?? "";
        outfit.PetId = petId ?? "";
        outfit.VisorId = visorId ?? "";

        PlayerControl.RpcSetColor((byte)colorId);

        // Data同期
        MessageWriter writer = MessageWriter.Get(SendOption.Reliable);
        writer.StartMessage(5);
        writer.Write(AmongUsClient.Instance.GameId);
        writer.StartMessage(1);
        writer.WritePacked(PlayerControl.Data.NetId);
        PlayerControl.Data.Serialize(writer, false);
        writer.EndMessage();
        writer.EndMessage();
        AmongUsClient.Instance.SendOrDisconnect(writer);
        writer.Recycle();
    }

    // ★ 名前を設定
    protected void SetName(string name)
    {
        if (PlayerControl == null) return;

        // ローカル
        if (PlayerControl.cosmetics?.nameText != null)
            PlayerControl.cosmetics.nameText.text = name;

        // RPC（SetNameの正しい引数: playerName のみ）
        var writer = AmongUsClient.Instance.StartRpcImmediately(
            PlayerControl.NetId, (byte)RpcCalls.SetName, SendOption.Reliable);
        writer.Write(PlayerControl.Data.NetId);
        writer.Write(name);
        writer.Write(false);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    // ★ 位置固定
    protected void SnapToPosition(Vector2 position)
    {
        if (PlayerControl == null) return;
        Position = position;

        try { PlayerControl.NetTransform.SnapTo(position); }
        catch { }

        ushort sid = (ushort)(PlayerControl.NetTransform.lastSequenceId + 100U);
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(
            PlayerControl.NetTransform.NetId, (byte)RpcCalls.SnapTo, SendOption.Reliable);
        NetHelpers.WriteVector2(position, writer);
        writer.Write(sid);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }

    // ★ キューに追加してスポーン（競合防止）
    protected void CreateNetObject(Vector2 position)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (GameStates.IsLobby || GameStates.IsEnded) return;

        SpawnQueue.Enqueue(() => DoCreate(position));
        ProcessQueue();
    }

    private static void ProcessQueue()
    {
        if (IsSpawning || SpawnQueue.Count == 0) return;
        IsSpawning = true;
        var action = SpawnQueue.Dequeue();
        action();
    }

    private void DoCreate(Vector2 position)
    {
        PlayerControl = UnityEngine.Object.Instantiate(
            AmongUsClient.Instance.PlayerPrefab, Vector2.zero, Quaternion.identity);
        PlayerControl.PlayerId = 254;
        PlayerControl.isNew = false;
        PlayerControl.notRealPlayer = true;

        try { PlayerControl.NetTransform.SnapTo(new Vector2(50f, 50f)); }
        catch { }

        AmongUsClient.Instance.NetIdCnt += 1U;

        // スポーンメッセージ
        MessageWriter msg = MessageWriter.Get(SendOption.Reliable);
        msg.StartMessage(5);
        msg.Write(AmongUsClient.Instance.GameId);
        msg.StartMessage(4);
        SpawnGameDataMessage item = AmongUsClient.Instance.CreateSpawnMessage(PlayerControl, -2, SpawnFlags.None);
        item.SerializeValues(msg);
        msg.EndMessage();

        // バニラ向け追加コンポーネント
        for (uint i = 1; i <= 3; ++i)
        {
            msg.StartMessage(4);
            msg.WritePacked(2U);
            msg.WritePacked(-2);
            msg.Write((byte)SpawnFlags.None);
            msg.WritePacked(1);
            msg.WritePacked(AmongUsClient.Instance.NetIdCnt - i);
            msg.StartMessage(1);
            msg.EndMessage();
            msg.EndMessage();
        }

        msg.EndMessage();
        AmongUsClient.Instance.SendOrDisconnect(msg);
        msg.Recycle();

        if (PlayerControl.AllPlayerControls.Contains(PlayerControl))
            PlayerControl.AllPlayerControls.Remove(PlayerControl);

        PlayerControl.cosmetics.colorBlindText.color = Color.clear;

        Position = position;
        ++MaxId;
        Id = MaxId;
        if (MaxId == int.MaxValue) MaxId = -1;

        AllObjects.Add(this);

        // PlayerId割り当て（0.1秒後）
        _ = new LateTask(() =>
        {
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                if (pc.AmOwner) continue;

                var sender = CustomRpcSender.Create("CNO.AssignId", SendOption.Reliable);
                MessageWriter writer = sender.stream;
                sender.StartMessage(pc.OwnerId);

                writer.StartMessage(1);
                writer.WritePacked(PlayerControl.NetId);
                writer.Write(pc.PlayerId);
                writer.EndMessage();

                sender.StartRpc(PlayerControl.NetId, RpcCalls.MurderPlayer)
                    .WriteNetObject(PlayerControl)
                    .Write((int)MurderResultFlags.FailedError)
                    .EndRpc();

                writer.StartMessage(1);
                writer.WritePacked(PlayerControl.NetId);
                writer.Write((byte)254);
                writer.EndMessage();

                sender.EndMessage();
                sender.SendMessage();
            }

            PlayerControl.CachedPlayerData = PlayerControl.LocalPlayer.Data;
        }, 0.1f, "CNO.AssignId", true);

        // ★ 外見・名前・位置設定（0.4秒後）
        // ★ 終わったら次のキューを処理
        _ = new LateTask(() =>
        {
            OnCreated();
            IsSpawning = false;
            ProcessQueue(); // 次のダミーをスポーン
        }, 0.4f, "CNO.OnCreated", true);
    }

    // ★ サブクラスで外見・名前・位置を設定
    protected virtual void OnCreated() { }

    public virtual void OnMeeting()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        Despawn();
    }

    public static void FixedUpdate()
    {
        foreach (var cno in AllObjects.ToArray())
            cno?.OnFixedUpdate();
    }

    public static CustomNetObject Get(int id)
        => AllObjects.FirstOrDefault(x => x.Id == id);

    public static void Reset()
    {
        try
        {
            SpawnQueue.Clear();
            IsSpawning = false;
            foreach (var obj in AllObjects.ToArray())
                obj?.Despawn();
            AllObjects.Clear();
        }
        catch (Exception e) { Logger.Error(e.ToString(), "CNO.Reset"); }
    }

    // ★ キル可能ダミーかチェック（キラーとの距離判定）
    public static CustomNetObject GetKillableTarget(PlayerControl killer, float range = 1.0f)
    {
        if (killer == null) return null;
        var pos = killer.GetTruePosition();
        return AllObjects
            .Where(o => o is IKillableDummy)
            .OrderBy(o => Vector2.Distance(pos, o.Position))
            .FirstOrDefault(o => Vector2.Distance(pos, o.Position) <= range);
    }
}

// ★ キル可能ダミーのマーカーインターフェース
public interface IKillableDummy
{
    void OnKilled(PlayerControl killer);
}

public sealed class MarkerObject : CustomNetObject
{
    public MarkerObject(Vector2 position, List<byte> visibleTo = null)
    {
        CreateNetObject(position);
        if (visibleTo != null)
        {
            _ = new LateTask(() =>
            {
                foreach (var pc in PlayerCatch.AllPlayerControls)
                    if (!visibleTo.Contains(pc.PlayerId)) Hide(pc);
            }, 0.5f, "MarkerObject.Hide", true);
        }
    }

    protected override void OnCreated()
    {
        SetName("<color=#ff0000>★</color>");
        SnapToPosition(Position);
    }
}