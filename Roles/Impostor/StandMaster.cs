using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Hazel;
using HarmonyLib;
using UnityEngine;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.PlayerCatch;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Impostor;

public sealed class StandMaster : RoleBase, IImpostor, IUsePhantomButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(StandMaster),
            player => new StandMaster(player),
            CustomRoles.StandMaster,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Impostor,
            25100,
            SetUpOptionItem,
            "sm",
            OptionSort: (3, 15)
        );

    public StandMaster(PlayerControl player)
        : base(RoleInfo, player)
    {
        PhantomCooldown = OptionPhantomCooldown.GetFloat();
        KillCooldownReduction = OptionKillCooldownReduction.GetFloat();

        standId = byte.MaxValue;
        standOriginPos = Vector2.zero;
        isStandActive = false;
        standWasAlive = false;
    }

    static OptionItem OptionPhantomCooldown;
    static float PhantomCooldown;
    static OptionItem OptionKillCooldownReduction;
    static float KillCooldownReduction;

    public byte standId;
    public Vector2 standOriginPos;
    public bool isStandActive;
    bool standWasAlive;

    enum OptionName
    {
        StandMasterPhantomCooldown,
        StandMasterKillCooldownReduction,
    }

    static void SetUpOptionItem()
    {
        OptionPhantomCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.StandMasterPhantomCooldown, OptionBaseCoolTime, 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionKillCooldownReduction = FloatOptionItem.Create(RoleInfo, 11, OptionName.StandMasterKillCooldownReduction, new(0f, 60f, 0.5f), 5f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }

    public float CalculateKillCooldown() => 30f;
    public bool CanUseSabotageButton() => true;
    public bool CanUseImpostorVentButton() => true;
    bool IUsePhantomButton.IsresetAfterKill => false;
    bool IUsePhantomButton.IsPhantomRole => true;

    public override void ApplyGameOptions(IGameOptions opt)
    {
        if (Player.AmOwner && Is(Player))
        {
            AURoleOptions.PhantomCooldown = PhantomCooldown;
        }
    }

    void IUsePhantomButton.OnClick(ref bool AdjustKillCooldown, ref bool? ResetCooldown)
    {
        AdjustKillCooldown = false;
        ResetCooldown = false;

        if (!Player.IsAlive()) return;
        if (isStandActive) return;

        var candidates = new List<PlayerControl>();
        foreach (var pc in AllAlivePlayerControls)
        {
            if (pc.PlayerId == Player.PlayerId) continue;
            if (!pc.GetCustomRole().IsImpostor()) continue;
            if (pc.MyPhysics.Animations.IsPlayingAnyLadderAnimation()) continue;
            if ((MapNames)Main.NormalOptions.MapId == MapNames.Airship
                && Vector2.Distance(pc.GetTruePosition(), new Vector2(7.76f, 8.56f)) <= 1.9f) continue;
            candidates.Add(pc);
        }

        if (candidates.Count == 0)
        {
            float myCurrentTimer = Player.killTimer;
            Player.killTimer = Mathf.Max(0f, myCurrentTimer - KillCooldownReduction);

            if (Player.AmOwner && Is(Player))
            {
                AURoleOptions.PhantomCooldown = PhantomCooldown;
            }
            Player.RpcResetAbilityCooldown(Sync: true);

            UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);
            Utils.SendMessage("<color=#cc0000>対象不在のため、自身のキルクールを短縮しました。</color>", Player.PlayerId);
            return;
        }

        var rand = IRandom.Instance;
        var stand = candidates[rand.Next(candidates.Count)];

        standId = stand.PlayerId;
        standOriginPos = stand.GetTruePosition();
        isStandActive = true;
        standWasAlive = true;

        var warpPos = Player.GetTruePosition();
        warpPos.y += 0.47f;
        stand.NetTransform.RpcSnapTo(warpPos);

        float currentTimer = stand.killTimer;
        float newCooldown = Mathf.Max(0f, currentTimer - KillCooldownReduction);
        stand.killTimer = newCooldown;

        if (Player.AmOwner && Is(Player))
        {
            AURoleOptions.PhantomCooldown = PhantomCooldown;
        }
        Player.RpcResetAbilityCooldown(Sync: true);

        SendRpcToggle(standId, standOriginPos, newCooldown);

        UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);
        Utils.SendMessage(GetString("StandMasterActivated"), Player.PlayerId);
    }

    public override void OnFixedUpdate(PlayerControl player)
    {
        if (player.AmOwner && Is(player))
        {
            AURoleOptions.PhantomCooldown = PhantomCooldown;
        }

        if (!AmongUsClient.Instance.AmHost) return;
        if (!isStandActive) return;

        var stand = GetPlayerById(standId);
        if (stand == null)
        {
            ResetStand(returnToOrigin: false);
            return;
        }

        bool nowDead = PlayerState.GetByPlayerId(standId)?.IsDead ?? true;

        if (standWasAlive && nowDead)
        {
            stand.NetTransform.RpcSnapTo(standOriginPos);
            ResetStand(returnToOrigin: false);
            return;
        }

        standWasAlive = !nowDead;
    }

    public void ResetStand(bool returnToOrigin)
    {
        if (!isStandActive) return;

        if (returnToOrigin)
        {
            var stand = GetPlayerById(standId);
            if (stand != null && stand.IsAlive())
                stand.NetTransform.RpcSnapTo(standOriginPos);
        }

        isStandActive = false;
        standWasAlive = false;
        standId = byte.MaxValue;
        standOriginPos = Vector2.zero;

        SendRpcReturn();
        UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (isStandActive)
            ResetStand(returnToOrigin: true);
    }

    public override void OnStartMeeting()
    {
        if (isStandActive)
            ResetStand(returnToOrigin: true);
    }

    public override void AfterMeetingTasks()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!Player.IsAlive()) return;

        if (PlayerControl.LocalPlayer != null && Is(PlayerControl.LocalPlayer))
        {
            AURoleOptions.PhantomCooldown = PhantomCooldown;
            Player.RpcResetAbilityCooldown();
        }
    }

    void SendRpcToggle(byte sId, Vector2 origin, float newTimer)
    {
        using var sender = CreateSender();
        sender.Writer.Write((byte)1);
        sender.Writer.Write(sId);
        sender.Writer.Write(origin.x);
        sender.Writer.Write(origin.y);
        sender.Writer.Write(newTimer);
    }

    void SendRpcReturn()
    {
        using var sender = CreateSender();
        sender.Writer.Write((byte)2);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        byte action = reader.ReadByte();
        if (action == 1)
        {
            standId = reader.ReadByte();
            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            standOriginPos = new Vector2(x, y);
            float newTimer = reader.ReadSingle();

            isStandActive = true;
            standWasAlive = true;

            var s = GetPlayerById(standId);
            if (s != null) s.killTimer = newTimer;
        }
        else if (action == 2)
        {
            isStandActive = false;
            standWasAlive = false;
            standId = byte.MaxValue;
            standOriginPos = Vector2.zero;
        }
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (seen.PlayerId != seer.PlayerId || isForMeeting || !Player.IsAlive()) return "";
        if (isStandActive)
        {
            var stand = GetPlayerById(standId);
            var name = stand != null ? stand.Data.PlayerName : "?";
            return $"{(isForHud ? "" : "<size=60%>")}<color=#cc0000>スタンド発動中: {name}</color>";
        }
        return $"{(isForHud ? "" : "<size=60%>")}<color=#cc0000>ファントムボタン → スタンド召喚</color>";
    }

    public override string GetAbilityButtonText() => GetString("StandMasterAbilityButtonText");
    public override bool OverrideAbilityButton(out string text)
    {
        text = "StandMaster_Ability";
        return true;
    }
}

[HarmonyPatch(typeof(CustomRoleManager), nameof(CustomRoleManager.OnCheckMurder))]
public static class StandMasterKillReturnPatch
{
    public static void Postfix(PlayerControl killer)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        foreach (var pc in AllPlayerControls)
        {
            if (pc == null || !pc.IsAlive()) continue;
            if (pc.GetRoleClass() is not StandMaster sm) continue;

            if (sm.isStandActive && sm.standId == killer.PlayerId)
            {
                sm.ResetStand(returnToOrigin: true);
                break;
            }
        }
    }
}