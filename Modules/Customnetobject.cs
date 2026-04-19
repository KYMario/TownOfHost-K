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

    protected int Id;
    public PlayerControl PlayerControl;
    public Vector2 Position;
    private string Sprite;

    // ★ EHRと全く同じRpcChangeSpriteの実装
    public void RpcChangeSprite(string sprite)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        Sprite = sprite;

        var outfit = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default];
        string origName = outfit.PlayerName;
        int origColor = outfit.ColorId;
        string origHat = outfit.HatId;
        string origSkin = outfit.SkinId;
        string origPet = outfit.PetId;
        string origVisor = outfit.VisorId;

        var sender = CustomRpcSender.Create("CNO.RpcChangeSprite", SendOption.Reliable);
        MessageWriter writer = sender.stream;
        sender.StartMessage();

        outfit.PlayerName = "<size=14><br></size>" + sprite;
        outfit.ColorId = 0;
        outfit.HatId = "";
        outfit.SkinId = "";
        outfit.PetId = "";
        outfit.VisorId = "";

        writer.StartMessage(1);
        writer.WritePacked(PlayerControl.LocalPlayer.Data.NetId);
        PlayerControl.LocalPlayer.Data.Serialize(writer, false);
        writer.EndMessage();

        try { PlayerControl.Shapeshift(PlayerControl.LocalPlayer, false); }
        catch (Exception e) { Logger.Error(e.ToString(), "CNO.RpcChangeSprite"); }

        sender.StartRpc(PlayerControl.NetId, RpcCalls.Shapeshift)
            .WriteNetObject(PlayerControl.LocalPlayer)
            .Write(false)
            .EndRpc();

        outfit.PlayerName = origName;
        outfit.ColorId = origColor;
        outfit.HatId = origHat;
        outfit.SkinId = origSkin;
        outfit.PetId = origPet;
        outfit.VisorId = origVisor;

        writer.StartMessage(1);
        writer.WritePacked(PlayerControl.LocalPlayer.Data.NetId);
        PlayerControl.LocalPlayer.Data.Serialize(writer, false);
        writer.EndMessage();

        sender.EndMessage();
        sender.SendMessage();
    }

    public void TP(Vector2 position)
    {
        Position = position;
    }

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
                try
                {
                    PlayerControl.transform.FindChild("Names")
                        .FindChild("NameText_TMP").gameObject.SetActive(false);
                }
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

    protected virtual void OnFixedUpdate()
    {
        try
        {
            if (!AmongUsClient.Instance.AmHost) return;

            if (AmongUsClient.Instance.AmClient)
            {
                try { PlayerControl.NetTransform.SnapTo(Position, (ushort)(PlayerControl.NetTransform.lastSequenceId + 1U)); }
                catch { }
            }

            ushort num = (ushort)(PlayerControl.NetTransform.lastSequenceId + 2U);
            MessageWriter mw = AmongUsClient.Instance.StartRpcImmediately(
                PlayerControl.NetTransform.NetId, 21, SendOption.None);
            NetHelpers.WriteVector2(Position, mw);
            mw.Write(num);
            AmongUsClient.Instance.FinishRpcImmediately(mw);
        }
        catch { }
    }

    // ★ EHRと全く同じCreateNetObjectの実装
    protected void CreateNetObject(string sprite, Vector2 position)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (GameStates.IsLobby || GameStates.IsEnded) return;

        PlayerControl = UnityEngine.Object.Instantiate(
            AmongUsClient.Instance.PlayerPrefab, Vector2.zero, Quaternion.identity);
        PlayerControl.PlayerId = 254;
        PlayerControl.isNew = false;
        PlayerControl.notRealPlayer = true;

        try { PlayerControl.NetTransform.SnapTo(new Vector2(50f, 50f)); }
        catch (Exception e) { Logger.Error(e.ToString(), "CNO.Create.SnapTo"); }

        AmongUsClient.Instance.NetIdCnt += 1U;

        // ★ EHRと同じSpawnGameDataMessage方式
        MessageWriter msg = MessageWriter.Get(SendOption.Reliable);
        msg.StartMessage(5);
        msg.Write(AmongUsClient.Instance.GameId);
        msg.StartMessage(4);
        SpawnGameDataMessage item = AmongUsClient.Instance.CreateSpawnMessage(PlayerControl, -2, SpawnFlags.None);
        item.SerializeValues(msg);
        msg.EndMessage();
        msg.EndMessage();
        AmongUsClient.Instance.SendOrDisconnect(msg);
        msg.Recycle();

        if (PlayerControl.AllPlayerControls.Contains(PlayerControl))
            PlayerControl.AllPlayerControls.Remove(PlayerControl);

        PlayerControl.cosmetics.currentBodySprite.BodySprite.color = Color.clear;
        PlayerControl.cosmetics.colorBlindText.color = Color.clear;

        Position = position;
        Sprite = sprite;
        ++MaxId;
        Id = MaxId;
        if (MaxId == int.MaxValue) MaxId = -1;

        AllObjects.Add(this);

        // ★ EHRと同じ: LateTask内でSenderを使ってスプライト・位置を送信
        _ = new LateTask(() =>
        {
            var outfit = PlayerControl.LocalPlayer.Data.Outfits[PlayerOutfitType.Default];
            string origName = outfit.PlayerName;
            int origColor = outfit.ColorId;
            string origHat = outfit.HatId;
            string origSkin = outfit.SkinId;
            string origPet = outfit.PetId;
            string origVisor = outfit.VisorId;

            var sender = CustomRpcSender.Create("CNO.CreateNetObject", SendOption.Reliable);
            MessageWriter writer = sender.stream;
            sender.StartMessage();

            outfit.PlayerName = "<size=14><br></size>" + sprite;
            outfit.ColorId = 0;
            outfit.HatId = "";
            outfit.SkinId = "";
            outfit.PetId = "";
            outfit.VisorId = "";

            writer.StartMessage(1);
            writer.WritePacked(PlayerControl.LocalPlayer.Data.NetId);
            PlayerControl.LocalPlayer.Data.Serialize(writer, false);
            writer.EndMessage();

            try { PlayerControl.Shapeshift(PlayerControl.LocalPlayer, false); }
            catch (Exception e) { Logger.Error(e.ToString(), "CNO.Create.Shapeshift"); }

            sender.StartRpc(PlayerControl.NetId, RpcCalls.Shapeshift)
                .WriteNetObject(PlayerControl.LocalPlayer)
                .Write(false)
                .EndRpc();

            outfit.PlayerName = origName;
            outfit.ColorId = origColor;
            outfit.HatId = origHat;
            outfit.SkinId = origSkin;
            outfit.PetId = origPet;
            outfit.VisorId = origVisor;

            writer.StartMessage(1);
            writer.WritePacked(PlayerControl.LocalPlayer.Data.NetId);
            PlayerControl.LocalPlayer.Data.Serialize(writer, false);
            writer.EndMessage();

            try { PlayerControl.NetTransform.SnapTo(position); }
            catch (Exception e) { Logger.Error(e.ToString(), "CNO.Create.SnapTo2"); }

            // ★ SnapToはStartRpcImmediatelyで送信（TOH-PのCustomRpcSenderにSnapTo用メソッドがないため）
            sender.EndMessage();
            sender.SendMessage();

            MessageWriter snapWriter = AmongUsClient.Instance.StartRpcImmediately(
                PlayerControl.NetTransform.NetId, (byte)RpcCalls.SnapTo, SendOption.Reliable);
            NetHelpers.WriteVector2(position, snapWriter);
            snapWriter.Write(PlayerControl.NetTransform.lastSequenceId);
            AmongUsClient.Instance.FinishRpcImmediately(snapWriter);

        }, 0.25f, "CNO.CreateSprite", true);

        // ★ EHRと同じ: 各クライアントにPlayerId書き込みとMurderPlayer送信
        _ = new LateTask(() =>
        {
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                if (pc.AmOwner) continue;

                var sender = CustomRpcSender.Create("CNO.CreateNetObject2", SendOption.Reliable);
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
    }

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
            foreach (var obj in AllObjects.ToArray())
                obj?.Despawn();
            AllObjects.Clear();
        }
        catch (Exception e) { Logger.Error(e.ToString(), "CNO.Reset"); }
    }
}

// ======================================================
// ★ 使用例: マーカーオブジェクト
// ======================================================
public sealed class MarkerObject : CustomNetObject
{
    public MarkerObject(Vector2 position, List<byte> visibleTo = null)
    {
        CreateNetObject("<size=200%><color=#ff0000>★</color></size>", position);

        if (visibleTo != null)
        {
            _ = new LateTask(() =>
            {
                foreach (var pc in PlayerCatch.AllPlayerControls)
                {
                    if (!visibleTo.Contains(pc.PlayerId))
                        Hide(pc);
                }
            }, 0.4f, "MarkerObject.Hide", true);
        }
    }
}