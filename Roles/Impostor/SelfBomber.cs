using System.Linq;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;

public sealed class SelfBomber : RoleBase, IImpostor, IUsePhantomButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(SelfBomber),
            player => new SelfBomber(player),
            CustomRoles.SelfBomber,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Impostor,
            3814142,
            SetupOptionItem,
            "sb",
            OptionSort: (3, 2),
            from: From.SuperNewRoles
        );

    public SelfBomber(PlayerControl player)
    : base(RoleInfo, player)
    {
    }

    private static OptionItem OptionCooldown;
    private static OptionItem OptionExplosionRadius;

    private enum OptionName
    {
        SelfBomberCooldown,
        SelfBomberExplosionRadius
    }

    private static void SetupOptionItem()
    {
        OptionCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.SelfBomberCooldown, new(0f, 60f, 0.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionExplosionRadius = FloatOptionItem.Create(RoleInfo, 11, OptionName.SelfBomberExplosionRadius, new(0.5f, 10f, 0.5f), 3f, false)
            .SetValueFormat(OptionFormat.Multiplier);
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.PhantomCooldown = OptionCooldown.GetFloat();
    }

    void IUsePhantomButton.OnClick(ref bool AdjustKillCooldown, ref bool? ResetCooldown)
    {
        if (!Player.IsAlive()) return;

        AdjustKillCooldown = false;
        ResetCooldown = true;

        var explosionRadius = OptionExplosionRadius.GetFloat();
        var playerPos = Player.GetTruePosition();
        var targets = PlayerCatch.AllAlivePlayerControls.ToArray();

        foreach (var target in targets)
        {
            if (target.PlayerId == Player.PlayerId) continue;
            if (Vector2.Distance(playerPos, target.GetTruePosition()) > explosionRadius) continue;

            CustomRoleManager.OnCheckMurder(Player, target, target, target, true, true, 2, CustomDeathReason.Bombed);
        }

        MyState.DeathReason = CustomDeathReason.Suicide;
        Player.SetRealKiller(Player);
        Player.RpcMurderPlayer(Player);

        if (!PlayerCatch.AllAlivePlayerControls.Any())
        {
            CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Impostor, byte.MaxValue);
        }
    }

    bool IUsePhantomButton.IsPhantomRole => Player.IsAlive();

    public override string GetAbilityButtonText() => GetString("SelfBomberAbilityText");
    public override bool OverrideAbilityButton(out string text)
    {
        text = "Bomber_Ability";
        return true;
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (seen.PlayerId != seer.PlayerId || isForMeeting || !Player.IsAlive()) return "";

        if (isForHud) return GetString("SelfBomberLowerText");
        return $"<size=50%>{GetString("SelfBomberLowerText")}</size>";
    }
}