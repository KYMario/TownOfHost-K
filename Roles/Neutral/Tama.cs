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
            26500,
            SetupOptionItem,
            "tm",
            "#00b4eb",
            (1, 5),
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
        if (!CanLoad)
            Player.RpcSetRoleDesync(RoleTypes.Engineer, Player.GetClientId());
        AURoleOptions.EngineerCooldown = OptVentCooldown.GetFloat();
        AURoleOptions.EngineerInVentMaxTime = OptVentMaxTime.GetFloat();
    }

    public float CalculateKillCooldown() => LoadCooldown;

    public bool CanUseKillButton()
    {
        if (!CanLoad) return false;
        return Player.IsAlive() && !hasLoaded && !isLoading && IsOwnerAlive();
    }

    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => true;
    public override bool CanVentMoving(PlayerPhysics physics, int ventId) => CanVentMove;

    private bool IsOwnerAlive()
    {
        if (OwnerId == byte.MaxValue) return false;
        var owner = GetPlayerById(OwnerId);
        return owner != null && owner.IsAlive();
    }

    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        info.DoKill = false;
        if (!CanLoad) return;

        var (killer, target) = info.AttemptTuple;
        if (hasLoaded || isLoading) return;
        if (target.PlayerId != OwnerId) return;

        isLoading = true;
        hasLoaded = true;

        var owner = GetPlayerById(OwnerId);
        if (owner?.GetRoleClass() is JackalHadouHo jhh)
            jhh.SetLoaded(true);

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

        if (!player.IsAlive() && hasLoaded)
        {
            hasLoaded = false;
            isLoading = false;
            if (owner?.GetRoleClass() is JackalHadouHo jhh)
                jhh.SetLoaded(false);
            SendRPC();
            return;
        }

        // ★ オーナーが死亡または転職（JackalHadouHoでなくなった）したら昇格
        if (player.IsAlive() && (owner == null || !owner.IsAlive() || owner.GetCustomRole() != CustomRoles.JackalHadouHo))
        {
            OwnerId = byte.MaxValue;
            MyState.SetCountType(CountTypes.Jackal);
            if (!Utils.RoleSendList.Contains(Player.PlayerId))
                Utils.RoleSendList.Add(Player.PlayerId);
            JackalHadouHo.NextNoSideKick = true;
            Player.RpcSetCustomRole(CustomRoles.JackalHadouHo, true);
            SendRPC();
            UtilsNotifyRoles.NotifyRoles();
            return;
        }

        if (!hasLoaded) return;
        if (owner == null || !owner.IsAlive() || !player.IsAlive()) return;

        var position = owner.transform.position;
        player.RpcSnapToForced(position, SendOption.None);
    }

    public override void OnStartMeeting()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (hasLoaded || isLoading)
        {
            hasLoaded = false;
            isLoading = false;
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