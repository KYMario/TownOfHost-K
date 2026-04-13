using AmongUs.GameOptions;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using UnityEngine;

namespace TownOfHost.Roles.Impostor;

public sealed class DoubleKiller : RoleBase, IImpostor, IUsePhantomButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(DoubleKiller),
            player => new DoubleKiller(player),
            CustomRoles.DoubleKiller,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Impostor,
            24300,
            SetUpOptionItem,
            "dk",
            OptionSort: (3, 13),
            from: From.SuperNewRoles
        );

    public DoubleKiller(PlayerControl player)
        : base(RoleInfo, player)
    {
        PhantomCooldown = OptionPhantomCooldown.GetFloat();
        KillCooldown = OptionKillCooldown.GetFloat();
        CanVent = !OptionCanVent.GetBool();
        CanSabotage = !OptionCanSabotage.GetBool();
        hasUsedPhantom = false;
    }

    static OptionItem OptionPhantomCooldown;
    static float PhantomCooldown;
    static OptionItem OptionKillCooldown;
    static float KillCooldown;
    static OptionItem OptionCanVent;
    static bool CanVent;
    static OptionItem OptionCanSabotage;
    static bool CanSabotage;
    bool hasUsedPhantom;

    enum OptionName
    {
        DoubleKillerPhantomCooldown,
        DoubleKillerKillCooldown,
        DoubleKillerCanVent,
        DoubleKillerCanSabotage,
    }

    static void SetUpOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.DoubleKillerKillCooldown, new(0.5f, 60f, 0.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionPhantomCooldown = FloatOptionItem.Create(RoleInfo, 11, OptionName.DoubleKillerPhantomCooldown, new(0.5f, 60f, 0.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 12, OptionName.DoubleKillerCanVent, true, false);
        OptionCanSabotage = BooleanOptionItem.Create(RoleInfo, 13, OptionName.DoubleKillerCanSabotage, true, false);
    }

    public float CalculateKillCooldown() => KillCooldown;
    public bool CanUseSabotageButton() => CanSabotage;
    public bool CanUseImpostorVentButton() => CanVent;

    public override bool CanClickUseVentButton => CanVent;
    public override bool OnEnterVent(PlayerPhysics physics, int ventId) => CanVent;
    public override bool CanVentMoving(PlayerPhysics physics, int ventId) => CanVent;

    public override void ApplyGameOptions(IGameOptions opt)
    {
        if (!hasUsedPhantom)
            AURoleOptions.PhantomCooldown = PhantomCooldown;
    }

    bool IUsePhantomButton.IsPhantomRole => !hasUsedPhantom;

    bool IUsePhantomButton.IsresetAfterKill => false;

    void IUsePhantomButton.OnClick(ref bool AdjustKillCooldown, ref bool? ResetCooldown)
    {
        AdjustKillCooldown = false;
        ResetCooldown = false;

        if (hasUsedPhantom || !Player.IsAlive()) return;

        PlayerControl nearest = null;
        float minDist = Main.NormalOptions.KillDistance switch
        {
            0 => 1f,
            1 => 1.8f,
            _ => 2.5f
        };

        foreach (var target in PlayerCatch.AllAlivePlayerControls)
        {
            if (target.PlayerId == Player.PlayerId) continue;
            if (target.GetCustomRole().IsImpostor() && !SuddenDeathMode.NowSuddenDeathMode) continue;

            float dist = Vector2.Distance(Player.GetTruePosition(), target.GetTruePosition());
            if (dist < minDist)
            {
                minDist = dist;
                nearest = target;
            }
        }

        if (nearest == null) return;

        // ★ ファントムボタン押した瞬間の残りキルCDを取得
        float savedKillCooldown = Player.killTimer; // ★ 残り時間はkillTimerに入っている

        hasUsedPhantom = true;
        Player.RpcSetRole(RoleTypes.Impostor);
        Player.RpcMurderPlayer(nearest);

        _ = new LateTask(() =>
        {
            if (!Player.IsAlive()) return;
            Main.AllPlayerKillCooldown[Player.PlayerId] = savedKillCooldown;
            Player.SetKillCooldown(savedKillCooldown);
            Player.SyncSettings();
            Player.MarkDirtySettings();
        }, 0.1f, "DoubleKillerRestoreCD", true);
    }

    public override string GetProgressText(bool comms = false, bool GameLog = false)
        => hasUsedPhantom ? "" : $"<#ff0000>(1)</color>";
}