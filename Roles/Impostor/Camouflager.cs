using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using UnityEngine;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;

public sealed class Camouflager : RoleBase, IImpostor, IUsePhantomButton
{
    public static readonly SimpleRoleInfo RoleInfo = SimpleRoleInfo.Create(
        typeof(Camouflager),
        player => new Camouflager(player),
        CustomRoles.Camouflager,
        () => RoleTypes.Phantom,
        CustomRoleTypes.Impostor,
        5600,
        SetupOptionItem,
        "Mo",
        OptionSort: (6, 0),
        from: From.TheOtherRoles
    );

    private static OptionItem OptionKillCoolDown;
    private static OptionItem OptionCooldown;
    private static OptionItem OptionAblitytime;

    public static bool NowUse { get; set; }

    private float _limit;
    private readonly List<byte> _ventPlayers = new();

    private enum OptionName
    {
        GhostNoiseSenderTime // 効果時間って翻訳一緒なので・・・
    }

    public Camouflager(PlayerControl player) : base(RoleInfo, player)
    {
        NowUse = false;
        _limit = -50;
        _ventPlayers.Clear();
    }

    private static void SetupOptionItem()
    {
        OptionKillCoolDown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 0.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionCooldown = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.Cooldown, new(0f, 180f, 0.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionAblitytime = FloatOptionItem.Create(RoleInfo, 12, OptionName.GhostNoiseSenderTime, new(0f, 100f, 0.5f), 10f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.PhantomCooldown = NowUse ? (OptionAblitytime.GetFloat() + 1f) : OptionCooldown.GetFloat();
    }

    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost || !NowUse || GameStates.CalledMeeting || _limit <= -50) return;

        _limit -= Time.fixedDeltaTime;
        if (_limit <= 0)
        {
            _limit = -100;
            NowUse = false;
            PlayerCatch.AllPlayerControls.Do(pc => Camouflage.RpcSetSkin(pc, force: null));

            _ = new LateTask(() =>
            {
                if (GameStates.CalledMeeting) return;

                foreach (var pl in PlayerCatch.AllPlayerControls)
                {
                    pl.RpcShapeshift(pl, false);
                    var sender = CustomRpcSender.Create("CamouflagerShape");
                    sender.AutoStartRpc(pl.NetId, RpcCalls.Shapeshift)
                        .Write(pl)
                        .Write(false)
                        .EndRpc();
                    sender.EndMessage();
                    sender.SendMessage();
                }

                UtilsNotifyRoles.NotifyRoles(ForceLoop: true);
                Player.RpcResetAbilityCooldown(log: false, Sync: true);
            }, 0.4f, "", true);
        }
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        NowUse = false;
        _limit = -50;
        _ventPlayers.Clear();
    }

    public override bool NotifyRolesCheckOtherName => true;

    public void OnClick(ref bool AdjustKillCooldown, ref bool? ResetCooldown)
    {
        AdjustKillCooldown = true;
        ResetCooldown = false;
        if (NowUse) return;

        var dummy = PlayerCatch.AllPlayerControls.FirstOrDefault(pc => pc != null) ?? PlayerCatch.GetPlayerById(0);

        foreach (var pl in PlayerCatch.AllPlayerControls)
        {
            pl.RpcShapeshift(dummy, false);
            var sender = CustomRpcSender.Create("CamouflagerShape");
            sender.AutoStartRpc(pl.NetId, RpcCalls.Shapeshift)
                .Write(dummy)
                .Write(false)
                .EndRpc();
            sender.EndMessage();
            sender.SendMessage();
        }

        _limit = OptionAblitytime.GetFloat();
        NowUse = true;

        _ = new LateTask(() =>
        {
            UtilsNotifyRoles.NotifyRoles(ForceLoop: true);
            Player.RpcResetAbilityCooldown(log: false, Sync: true);
        }, 0.2f, "", true);
    }

    public float CalculateKillCooldown() => OptionKillCoolDown.GetFloat();

    public override bool OverrideAbilityButton(out string text)
    {
        text = "Camouflager_Ability";
        return true;
    }

    public override string GetAbilityButtonText() => GetString("CamouflagerText");

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (seen.PlayerId != seer.PlayerId || isForMeeting || !Player.IsAlive()) return "";

        if (isForHud) return GetString("PhantomButtonLowertext");
        return $"<size=50%>{GetString("PhantomButtonLowertext")}</size>";
    }

    bool IUsePhantomButton.IsresetAfterKill => false;
}
