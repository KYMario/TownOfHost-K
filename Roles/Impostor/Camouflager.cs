using System.Collections.Generic;
using AmongUs.GameOptions;
using HarmonyLib;
using UnityEngine;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;

public sealed class Camouflager : RoleBase, IImpostor, IUsePhantomButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
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
    public Camouflager(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        NowUse = false;
        Limit = -50;
        VentPlayers.Clear();
    }
    static OptionItem OptionKillCoolDown;
    static OptionItem OptionCooldown;
    static OptionItem OptionAblitytime;
    public static bool NowUse;
    float Limit;
    List<byte> VentPlayers = new();
    enum OptionName { GhostNoiseSenderTime/* 効果時間って翻訳一緒なので・・・ */}
    static void SetupOptionItem()
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
        if (!AmongUsClient.Instance.AmHost || !NowUse || GameStates.CalledMeeting || Limit <= -50) return;

        Limit -= Time.fixedDeltaTime;

        if (VentPlayers.Count > 0)
        {
            var remove = new List<byte>();
            foreach (var id in VentPlayers)
            {
                var target = PlayerCatch.GetPlayerById(id);
                if (target.inVent) continue;
                if (Camouflage.IsCamouflage)
                {
                    var sender = CustomRpcSender.Create(name: $"Camouflage.RpcSetSkin({target.Data.GetLogPlayerName()})");
                    byte color = (byte)ModColors.PlayerColor.Gray;

                    target.SetColor(color);
                    sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetColor)
                        .Write(target.Data.NetId)
                        .Write(color)
                        .EndRpc();

                    target.SetHat("", color);
                    sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetHatStr)
                        .Write("")
                        .Write(target.GetNextRpcSequenceId(RpcCalls.SetHatStr))
                        .EndRpc();

                    target.SetSkin("", color);
                    sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetSkinStr)
                        .Write("")
                        .Write(target.GetNextRpcSequenceId(RpcCalls.SetSkinStr))
                        .EndRpc();

                    target.SetVisor("", color);
                    sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetVisorStr)
                        .Write("")
                        .Write(target.GetNextRpcSequenceId(RpcCalls.SetVisorStr))
                        .EndRpc();
                    sender.SendMessage();
                }
                else Camouflage.RpcSetSkin(target);
                remove.Add(id);
            }

            if (remove.Count != 0)
            {
                remove.Do(id => VentPlayers.Remove(id));
                foreach (var pl in PlayerCatch.AllPlayerControls)
                {
                    pl?.GetRoleClass()?.ChangeColor();
                }
            }
        }
        if (Limit <= 0)
        {
            Limit = -100;
            NowUse = false;
            PlayerCatch.AllPlayerControls.Do(pc => Camouflage.RpcSetSkin(pc, force: null));
            _ = new LateTask(() =>
            {
                if (GameStates.CalledMeeting) return;
                foreach (var pl in PlayerCatch.AllPlayerControls)
                {
                    pl?.GetRoleClass()?.ChangeColor();
                }
                UtilsNotifyRoles.NotifyRoles(ForceLoop: true);
                Player.RpcResetAbilityCooldown(log: false, Sync: true);
            }, 0.4f, "", true);
        }
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        NowUse = false;
        Limit = -50;
        VentPlayers.Clear();
        PlayerCatch.AllPlayerControls.Do(pc => Camouflage.RpcSetSkin(pc, ForceRevert: true, force: null));
    }
    public override bool NotifyRolesCheckOtherName => true;
    public void OnClick(ref bool AdjustKillCooldown, ref bool? ResetCooldown)
    {
        AdjustKillCooldown = true;
        ResetCooldown = false;
        if (NowUse) return;

        foreach (var target in PlayerCatch.AllAlivePlayerControls)
        {
            if (target.inVent)
            {
                VentPlayers.Add(target.PlayerId);
                continue;
            }
            var sender = CustomRpcSender.Create(name: $"Camouflage.RpcSetSkin({target.Data.GetLogPlayerName()})");
            byte color = (byte)ModColors.PlayerColor.Gray;

            target.SetColor(color);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetColor)
                .Write(target.Data.NetId)
                .Write(color)
                .EndRpc();

            target.SetHat("", color);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetHatStr)
                .Write("")
                .Write(target.GetNextRpcSequenceId(RpcCalls.SetHatStr))
                .EndRpc();

            target.SetSkin("", color);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetSkinStr)
                .Write("")
                .Write(target.GetNextRpcSequenceId(RpcCalls.SetSkinStr))
                .EndRpc();

            target.SetVisor("", color);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetVisorStr)
                .Write("")
                .Write(target.GetNextRpcSequenceId(RpcCalls.SetVisorStr))
                .EndRpc();
            sender.SendMessage();
        }

        Limit = OptionAblitytime.GetFloat();
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
