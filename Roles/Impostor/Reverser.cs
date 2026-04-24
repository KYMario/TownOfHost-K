using System.Collections.Generic;
using AmongUs.GameOptions;
using UnityEngine;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;

public sealed class Reverser : RoleBase, IImpostor, IUsePhantomButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Reverser),
            player => new Reverser(player),
            CustomRoles.Reverser,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Impostor,
            73230,
            SetupOptionItem,
            "esb",
            "#ff1919",
            (0, 1)
        );

    private static OptionItem OptionCooldown;
    private static OptionItem OptionDuration;

    private static int ActiveDebuffCount;
    public static bool AllowNegativeSpeed => ActiveDebuffCount > 0;

    private int effectToken;
    private bool isSpeedDebuffActive;
    private readonly Dictionary<byte, float> speedBeforeEffect;

    private enum OptionName
    {
        ReverserDuration
    }

    public Reverser(PlayerControl player)
        : base(RoleInfo, player)
    {
        effectToken = 0;
        isSpeedDebuffActive = false;
        speedBeforeEffect = new();
    }

    [Attributes.GameModuleInitializer]
    public static void Init()
    {
        ActiveDebuffCount = 0;
    }

    private static void SetupOptionItem()
    {
        OptionCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.Cooldown, new(0f, 60f, 0.5f), 25f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionDuration = FloatOptionItem.Create(RoleInfo, 11, OptionName.ReverserDuration, new(5f, 60f, 1f), 10f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.PhantomCooldown = OptionCooldown.GetFloat();
    }

    void IUsePhantomButton.OnClick(ref bool AdjustKillCooldown, ref bool? ResetCooldown)
    {
        AdjustKillCooldown = false;
        ResetCooldown = false; // ボタンを押した瞬間にはクールダウンを開始させない

        // 反転中はファントムボタンのクリックを無効化
        if (isSpeedDebuffActive) return;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!GameStates.InGame || !Player.IsAlive()) return;

        ActivateSpeedDebuff();
    }

    bool IUsePhantomButton.IsPhantomRole => true;
    bool IUsePhantomButton.IsresetAfterKill => false;

    private static void IncrementActiveDebuff()
    {
        ActiveDebuffCount++;
        if (ActiveDebuffCount < 0) ActiveDebuffCount = 0;
    }

    private static void DecrementActiveDebuff()
    {
        if (ActiveDebuffCount <= 0)
        {
            ActiveDebuffCount = 0;
            return;
        }

        ActiveDebuffCount--;
    }

    private void ActivateSpeedDebuff()
    {
        RestoreSpeedDebuff();

        speedBeforeEffect.Clear();
        foreach (var pc in PlayerCatch.AllPlayerControls)
        {
            if (pc == null) continue;
            if (!Main.AllPlayerSpeed.TryGetValue(pc.PlayerId, out var speed))
            {
                speed = Main.NormalOptions.PlayerSpeedMod;
            }
            speedBeforeEffect[pc.PlayerId] = speed;
        }

        var negativeVanillaSpeed = -Main.NormalOptions.PlayerSpeedMod;
        foreach (var pc in PlayerCatch.AllPlayerControls)
        {
            if (pc == null) continue;
            Main.AllPlayerSpeed[pc.PlayerId] = Mathf.Clamp(negativeVanillaSpeed, -10f, 10f);
        }

        isSpeedDebuffActive = true;
        IncrementActiveDebuff();

        effectToken++;
        var currentToken = effectToken;

        UtilsOption.MarkEveryoneDirtySettings();

        _ = new LateTask(() =>
        {
            if (!GameStates.InGame) return;
            if (currentToken != effectToken) return;
            RestoreSpeedDebuff();
        }, OptionDuration.GetFloat(), "Reverser.RestoreSpeed", true);
    }

    private void RestoreSpeedDebuff()
    {
        if (!isSpeedDebuffActive) return;

        foreach (var data in speedBeforeEffect)
        {
            Main.AllPlayerSpeed[data.Key] = data.Value;
        }

        speedBeforeEffect.Clear();
        isSpeedDebuffActive = false;
        DecrementActiveDebuff();
        UtilsOption.MarkEveryoneDirtySettings();

        // 反転終了時にファントムボタンのクールダウンをリセット（開始）する
        if (Player.IsAlive())
        {
            Player.RpcResetAbilityCooldown();
        }
    }

    public override void OnStartMeeting() => RestoreSpeedDebuff();
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target) => RestoreSpeedDebuff();
    public override void OnDestroy() => RestoreSpeedDebuff();

    public override void OnLeftPlayer(PlayerControl player)
    {
        if (player == null) return;
        speedBeforeEffect.Remove(player.PlayerId);
    }

    // ボタンのテキストを日本語化
    public override string GetAbilityButtonText() => "反転";

    // LowerTextを日本語化し、状態によって表示を切り替え
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (seen.PlayerId != seer.PlayerId || isForMeeting || !Player.IsAlive()) return "";

        string text = isSpeedDebuffActive
            ? "<color=#ff1919>操作反転中...</color>"
            : "<color=#ff1919>ファントムボタンで全員の操作を反転</color>";

        if (isForHud) return text;
        return $"<size=50%>{text}</size>";
    }
}