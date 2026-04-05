using System;
using AmongUs.GameOptions;
using Hazel;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using UnityEngine;
using static TownOfHost.PlayerCatch;
using static TownOfHost.Utils;

namespace TownOfHost.Roles.Crewmate;

public sealed class VillageChief : RoleBase, IUsePhantomButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(VillageChief),
            player => new VillageChief(player),
            CustomRoles.VillageChief,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            60000,
            SetupOptionItem,
            "vc",
            "#f5a623",
            from: From.SuperNewRoles,
            isDesyncImpostor: true
        );

    public VillageChief(PlayerControl player)
        : base(RoleInfo, player, () => HasTask.True)
    {
        hasUsedAbility = false;
        // ★ mod入れてる人（ホスト）は最初から任命モード(TaskMode = false)
        // ★ バニラプレイヤーはタスクモード(TaskMode = true)
        TaskMode = !AmongUsClient.Instance.AmHost;
    }

    private bool hasUsedAbility;
    private bool TaskMode;

    private static OptionItem AbilityCooldown;
    private static OptionItem NotifyTarget;
    private static readonly string[] NotifyTargetOptions =
        ["送信しない", "全員", "村長のみ", "シェリフのみ", "村長とシェリフ"];

    public PlayerControl appointedSheriff = null;

    private static void SetupOptionItem()
    {
        AbilityCooldown = FloatOptionItem.Create(
                RoleInfo, 11, "VillageChiefAbilityCooldown",
                new(0f, 60f, 2.5f), 20f, false
            )
            .SetValueFormat(OptionFormat.Seconds);

        NotifyTarget = StringOptionItem.Create(
            RoleInfo, 12, "VillageChiefNotifyTarget",
            NotifyTargetOptions, 0, false
        );
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(false);
    }

    // ============================
    //     Phantom Button（B方式）
    // ============================

    public override bool OverrideAbilityButton(out string text)
    {
        text = "VillageChief_Ability";
        return true;
    }

    public override string GetAbilityButtonText()
        => hasUsedAbility ? "任命済み" : "任命";

    public void OnClick(ref bool AdjustKillCooldown, ref bool? ResetCooldown)
    {
        if (hasUsedAbility) return;
        // ★ ホスト以外（バニラ）は任命できない
        if (!AmongUsClient.Instance.AmHost) return;
        if (TaskMode) return;

        // ★ 最も近い生存プレイヤーを探す（GetClosestPlayer は存在しないので自前で）
        PlayerControl nearest = null;
        float minDist = 2f;

        foreach (var pc in AllPlayerControls)
        {
            if (pc.PlayerId == Player.PlayerId) continue;
            if (!pc.IsAlive()) continue;

            float d = Vector2.Distance(Player.GetTruePosition(), pc.GetTruePosition());
            if (d < minDist)
            {
                minDist = d;
                nearest = pc;
            }
        }

        if (nearest == null)
        {
            ResetCooldown = false;
            return;
        }

        hasUsedAbility = true;
        ResetCooldown = false;

        // 任命モード終了 → タスクモードへ戻す
        TaskMode = true;
        SendModeRPC();

        // 内部ロールは Engineer のまま
        Player.RpcSetRole(RoleTypes.Engineer);

        // 自分に見えるロールも Crewmate に戻す
        Player.RpcSetRoleDesync(RoleTypes.Crewmate, Player.GetClientId());

        UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);

        // インポスター任命禁止 → 自爆
        if (nearest.GetCustomRole().IsImpostor())
        {
            PlayerState.GetByPlayerId(Player.PlayerId).DeathReason = CustomDeathReason.Suicide;
            Player.RpcMurderPlayer(Player);
            return;
        }

        var previousRole = nearest.GetCustomRole();
        appointedSheriff = nearest;

        if (!Utils.RoleSendList.Contains(nearest.PlayerId))
            Utils.RoleSendList.Add(nearest.PlayerId);

        nearest.RpcSetCustomRole(CustomRoles.Sheriff, log: null);

        NameColorManager.Add(Player.PlayerId, nearest.PlayerId, "#f5a623");

        UtilsGameLog.AddGameLog(
            "VillageChief",
            $"{UtilsName.GetPlayerColor(Player)}({UtilsRoleText.GetRoleName(CustomRoles.VillageChief)})が" +
            $"{UtilsName.GetPlayerColor(nearest)}({UtilsRoleText.GetRoleName(previousRole)})をシェリフに任命した"
        );

        UtilsNotifyRoles.NotifyRoles();

        string msg = NotifyTarget.GetValue() >= 1
            ? $"{Player.Data.PlayerName}(村長)が{nearest.Data.PlayerName}をシェリフに任命しました！"
            : "";

        switch (NotifyTarget.GetValue())
        {
            case 0: break;
            case 1: SendMessage(msg); break;
            case 2: SendMessage(msg, Player.PlayerId); break;
            case 3: SendMessage(msg, nearest.PlayerId); break;
            case 4:
                SendMessage(msg, Player.PlayerId);
                SendMessage(msg, nearest.PlayerId);
                break;
        }
    }

    // ============================
    //     ベントでモード切替
    // ============================

    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (!Player.IsAlive()) return false;
        if (hasUsedAbility) return false;

        TaskMode = !TaskMode;
        SendModeRPC();

        Player.RpcSetRoleDesync(TaskMode ? RoleTypes.Engineer : RoleTypes.Phantom, Player.GetClientId());

        if (TaskMode)
            Player.RpcSetRole(RoleTypes.Engineer);

        UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);
        return false;
    }

    // ============================
    //     RPC 同期
    // ============================

    private void SendModeRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(TaskMode);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        TaskMode = reader.ReadBoolean();
    }

    public override bool CanTask() => true;
}