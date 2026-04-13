using AmongUs.GameOptions;
using Hazel;
using UnityEngine;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.PlayerCatch;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Neutral;

public sealed class Tama : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Tama),
            player => new Tama(player),
            CustomRoles.Tama,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            24500,
            SetupOptionItem,
            "tm",
            "#00b4eb",
            from: From.SuperNewRoles,
            isDesyncImpostor: true,
            countType: CountTypes.Crew
        );

    public Tama(PlayerControl player)
        : base(RoleInfo, player, () => HasTask.False)
    {
        OwnerId = byte.MaxValue;
        hasLoaded = false;
        isLoading = false;
        LoadCooldown = OptLoadCooldown.GetFloat();
        CanLoad = OptCanLoad.GetBool();
        CanVentMove = OptCanVentMove.GetBool();
    }

    public byte OwnerId;
    public bool hasLoaded;
    bool isLoading;

    static OptionItem OptLoadCooldown;
    static float LoadCooldown;

    // ★ 追加：装填できるかどうか
    static OptionItem OptCanLoad;
    static bool CanLoad;

    static OptionItem OptVentCooldown;
    static OptionItem OptVentMaxTime;
    static OptionItem OptCanVentMove;
    static bool CanVentMove;

    private static void SetupOptionItem()
    {
        OptLoadCooldown = FloatOptionItem.Create(RoleInfo, 10, "TamaLoadCooldown", new(0f, 60f, 0.5f), 10f, false)
            .SetValueFormat(OptionFormat.Seconds);

        // ★ 装填できるかどうかのオプション
        OptCanLoad = BooleanOptionItem.Create(RoleInfo, 11, "TamaCanLoad", true, false);

        OptVentCooldown = FloatOptionItem.Create(RoleInfo, 12, GeneralOption.Cooldown, new(0f, 180f, 0.5f), 0f, false)
        .SetValueFormat(OptionFormat.Seconds);
        OptVentMaxTime = FloatOptionItem.Create(RoleInfo, 13, GeneralOption.EngineerInVentCooldown, new(0f, 180f, 0.5f), 0f, false)
            .SetZeroNotation(OptionZeroNotation.Infinity)
            .SetValueFormat(OptionFormat.Seconds);
        OptCanVentMove = BooleanOptionItem.Create(RoleInfo, 14, "MadmateCanMovedByVent", false, false);
    }

    public void SetOwner(byte ownerId)
    {
        OwnerId = ownerId;
        SendRPC();
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(true);

        // ★ 装填不可ならエンジニア判定に偽装
        if (!CanLoad)
            Player.RpcSetRoleDesync(RoleTypes.Engineer, Player.GetClientId());

        AURoleOptions.EngineerCooldown = OptVentCooldown.GetFloat();
        AURoleOptions.EngineerInVentMaxTime = OptVentMaxTime.GetFloat();
    }

    public float CalculateKillCooldown() => LoadCooldown;

    // ★ 装填不可ならキルボタンを出さない
    public bool CanUseKillButton()
    {
        if (!CanLoad) return false;
        return Player.IsAlive() && !hasLoaded && !isLoading && IsOwnerAlive();
    }

    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => true;

    private bool IsOwnerAlive()
    {
        if (OwnerId == byte.MaxValue) return false;
        var owner = GetPlayerById(OwnerId);
        return owner != null && owner.IsAlive();
    }

    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        info.DoKill = false;

        // ★ 装填不可なら何も起きない
        if (!CanLoad) return;

        var (killer, target) = info.AttemptTuple;

        if (hasLoaded || isLoading) return;

        // ★ キルボタンのターゲットがオーナー（波動砲ジャッカル）でないと装填不可
        if (target.PlayerId != OwnerId) return;

        isLoading = true;
        hasLoaded = true;

        // ★ 波動砲ジャッカルに装填を通知
        var owner = GetPlayerById(OwnerId);
        if (owner?.GetRoleClass() is JackalHadouHo jhh)
            jhh.SetLoaded(true);

        owner.RpcResetAbilityCooldown();

        SendRPC();
        UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player);
        Utils.SendMessage(GetString("TamaLoaded"), Player.PlayerId);
    }

    public override string GetProgressText(bool comms = false, bool GameLog = false)
    {
        if (!CanLoad) return "<color=#5e5e5e>【装填不可】</color>";
        if (hasLoaded) return $"<color=#00b4eb>【装填済】</color>";
        return $"<color=#5e5e5e>【未装填】</color>";
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (!Is(seer) || seer.PlayerId != seen.PlayerId || isForMeeting || !Player.IsAlive()) return "";

        if (!CanLoad)
            return $"{(isForHud ? "" : "<size=60%>")}<color=#5e5e5e>装填機能は無効化されています</color>";

        if (hasLoaded)
            return $"{(isForHud ? "" : "<size=60%>")}<color=#00b4eb>装填済み！波動砲ジャッカルが超波動砲を撃てる</color>";

        if (!IsOwnerAlive())
            return $"{(isForHud ? "" : "<size=60%>")}<color=#5e5e5e>波動砲ジャッカルが死亡しています</color>";

        return $"{(isForHud ? "" : "<size=60%>")}<color=#00b4eb>波動砲ジャッカルにキルボタンで装填</color>";
    }

    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!GameStates.IsInTask) return;
        if (OwnerId == byte.MaxValue) return;

        var owner = GetPlayerById(OwnerId);

        // ★ 装填中に弾が死んだらジャッカルの超波動砲状態を解除
        if (!player.IsAlive() && hasLoaded)
        {
            hasLoaded = false;
            isLoading = false;

            if (owner?.GetRoleClass() is JackalHadouHo jhh)
                jhh.SetLoaded(false);

            SendRPC();
            return;
        }

        // ★ オーナーが死亡したら昇格
        if (player.IsAlive() && (owner == null || !owner.IsAlive()))
        {
            OwnerId = byte.MaxValue;
            MyState.SetCountType(CountTypes.Jackal);
            if (!Utils.RoleSendList.Contains(Player.PlayerId))
                Utils.RoleSendList.Add(Player.PlayerId);
            JackalHadouHo.NextNoSideKick = true;
            Player.RpcSetCustomRole(CustomRoles.JackalHadouHo, true);
        }

        if (!hasLoaded) return;
        if (owner == null || !owner.IsAlive() || !player.IsAlive()) return;

        // ★ 装填後はオーナーの位置にワープ
        var position = owner.transform.position;
        player.RpcSnapToForced(position, SendOption.None);
    }

    public override void OnStartMeeting()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        // ★ 会議開始時に装填キャンセル
        if (hasLoaded || isLoading)
        {
            hasLoaded = false;
            isLoading = false;

            // ★ 波動砲ジャッカルの装填状態もリセット
            var owner = GetPlayerById(OwnerId);
            if (owner?.GetRoleClass() is JackalHadouHo jhh)
                jhh.SetLoaded(false);

            SendRPC();
            UtilsNotifyRoles.NotifyRoles();
        }
    }

    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, ref bool enabled, ref Color roleColor, ref string roleText, ref bool addon)
    {
        addon = false;
        if (seen.Is(CountTypes.Jackal))
            enabled = true;
    }

    public override void OverrideDisplayRoleNameAsSeen(PlayerControl seen, ref bool enabled, ref Color roleColor, ref string roleText, ref bool addon)
    {
        addon = false;
        if (seen.Is(CountTypes.Jackal))
            enabled = true;
    }

    public void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(OwnerId);
        sender.Writer.Write(hasLoaded);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        OwnerId = reader.ReadByte();
        hasLoaded = reader.ReadBoolean();
    }

    public bool OverrideKillButtonText(out string text)
    {
        text = GetString("TamaLoadButtonText");
        return true;
    }

    public bool OverrideKillButton(out string text)
    {
        text = "Tama_Load";
        return true;
    }
}
