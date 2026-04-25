using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AmongUs.GameOptions;
using Hazel;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Neutral;

public sealed class Eater : RoleBase, IKiller, IUsePhantomButton, IKillFlashSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Eater),
            player => new Eater(player),
            CustomRoles.Eater,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Neutral,
            50230,
            SetupOptionItem,
            "Ea",
            "#662B2C",
            (5, 2),
            true,
            countType: CountTypes.Eater,
            from: From.ExtremeRoles
        );

    static OptionItem OptionVisionEnable;
    static OptionItem OptionVisionRange;
    static OptionItem OptionDisableBlackout;
    static OptionItem OptionCanUseVent;
    static OptionItem OptionLinkCooldowns;
    static OptionItem OptionSharedCooldown;
    static OptionItem OptionSwallowCooldown;
    static OptionItem OptionEatCooldown;
    static OptionItem OptionWinCount;
    static OptionItem OptionSwallowTime;
    static OptionItem OptionSwallowRange;
    static OptionItem OptionEatIncreaseRate;
    static OptionItem OptionSwallowCooldownIncreaseRate;
    static OptionItem OptionMeetingReduceRate;
    static OptionItem OptionResetCooldownOnMeeting;
    static OptionItem OptionShowArrow;
    public static bool IsCustomVisionEnabled => OptionVisionEnable?.GetBool() ?? false;

    enum OptionName
    {
        EaterVisionRange,
        EaterLinkCooldowns,
        EaterSharedCooldown,
        EaterSwallowCooldown,
        EaterEatCooldown,
        EaterWinCount,
        EaterSwallowTime,
        EaterEatIncreaseRate,
        EaterSwallowCooldownIncreaseRate,
        EaterMeetingReduceRate
    }

    sealed class PendingSwallowInfo
    {
        public byte TargetId;
        public float Elapsed;
        public float Required;
        public PendingSwallowInfo(byte targetId, float required)
        {
            TargetId = targetId;
            Required = required;
            Elapsed = 0f;
        }
    }

    readonly Dictionary<byte, Vector3> deadBodyPositions = new();
    static readonly HashSet<byte> EatenBodies = new();

    int eatOrSwallowCount;
    bool eatMode;
    float extraSwallowTime;
    float extraCooldownRate;
    float eatCooldownTimer;
    float swallowCooldownTimer;
    bool usedAbilityThisRound;
    PendingSwallowInfo pendingSwallow;

    public Eater(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        eatOrSwallowCount = 0;
        eatMode = true;
        extraSwallowTime = 0f;
        extraCooldownRate = 0f;
        eatCooldownTimer = 0f;
        swallowCooldownTimer = 0f;
        usedAbilityThisRound = false;
        pendingSwallow = null;
    }

    [Attributes.GameModuleInitializer]
    public static void Init()
    {
        EatenBodies.Clear();
    }

    private static void SetupOptionItem()
    {
        SoloWinOption.Create(RoleInfo, 8, defo: 1);

        OptionVisionEnable = BooleanOptionItem.Create(RoleInfo, 9, "EaterVisionEnable", false, false);
        OptionVisionRange = FloatOptionItem.Create(RoleInfo, 10, OptionName.EaterVisionRange, new(0.5f, 3f, 0.1f), 1f, false, OptionVisionEnable)
            .SetValueFormat(OptionFormat.Multiplier);
        OptionDisableBlackout = BooleanOptionItem.Create(RoleInfo, 11, "EaterDisableBlackout", false, false, OptionVisionEnable);
        OptionCanUseVent = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.CanVent, false, false);

        OptionLinkCooldowns = BooleanOptionItem.Create(RoleInfo, 13, OptionName.EaterLinkCooldowns, true, false);
        OptionSharedCooldown = FloatOptionItem.Create(RoleInfo, 14, OptionName.EaterSharedCooldown, new(0f, 60f, 1f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds)
            .SetEnabled(() => !OptionLinkCooldowns.GetBool());
        OptionSwallowCooldown = FloatOptionItem.Create(RoleInfo, 15, OptionName.EaterSwallowCooldown, new(0f, 60f, 1f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds)
            .SetEnabled(() => OptionLinkCooldowns.GetBool());
        OptionEatCooldown = FloatOptionItem.Create(RoleInfo, 16, OptionName.EaterEatCooldown, new(0f, 60f, 1f), 10f, false)
            .SetValueFormat(OptionFormat.Seconds)
            .SetEnabled(() => OptionLinkCooldowns.GetBool());

        OptionWinCount = IntegerOptionItem.Create(RoleInfo, 17, OptionName.EaterWinCount, new(1, 10, 1), 3, false);
        OptionSwallowTime = FloatOptionItem.Create(RoleInfo, 18, OptionName.EaterSwallowTime, new(0.5f, 60f, 0.5f), 5f, false).SetValueFormat(OptionFormat.Seconds);
        OptionSwallowRange = StringOptionItem.Create(RoleInfo, 19, "EaterSwallowRange", new string[] { "Short", "Middle", "Long" }, 1, false);
        OptionEatIncreaseRate = FloatOptionItem.Create(RoleInfo, 20, OptionName.EaterEatIncreaseRate, new(0f, 25f, 0.5f), 5f, false).SetValueFormat(OptionFormat.Percent);
        OptionSwallowCooldownIncreaseRate = FloatOptionItem.Create(RoleInfo, 21, OptionName.EaterSwallowCooldownIncreaseRate, new(0f, 25f, 0.5f), 5f, false).SetValueFormat(OptionFormat.Percent);
        OptionMeetingReduceRate = FloatOptionItem.Create(RoleInfo, 22, OptionName.EaterMeetingReduceRate, new(0f, 50f, 0.5f), 10f, false).SetValueFormat(OptionFormat.Percent);
        OptionResetCooldownOnMeeting = BooleanOptionItem.Create(RoleInfo, 23, "EaterResetCooldownOnMeeting", false, false);
        OptionShowArrow = BooleanOptionItem.Create(RoleInfo, 24, "EaterShowArrow", true, false);
    }

    public override void OnSpawn(bool initialState = false)
    {
        if (initialState)
        {
            eatOrSwallowCount = 0;
            eatMode = true;
            extraSwallowTime = 0f;
            extraCooldownRate = 0f;
            eatCooldownTimer = GetBaseEatCooldown();
            swallowCooldownTimer = GetBaseSwallowCooldown();
            usedAbilityThisRound = false;
            pendingSwallow = null;
            deadBodyPositions.Clear();
            EatenBodies.Clear();
        }

        (this as IUsePhantomButton).Init(Player);
        IUsePhantomButton.IPPlayerKillCooldown[Player.PlayerId] = 0f;
        Player.RpcResetAbilityCooldown();
        RefreshKillCooldown();
    }

    public override void OnDestroy()
    {
        ClearAllBodyArrows();
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        var useCustomVision = OptionVisionEnable.GetBool();
        var ignoreBlackout = useCustomVision && OptionDisableBlackout.GetBool();
        var vision = Mathf.Clamp(useCustomVision ? OptionVisionRange.GetFloat() : Main.DefaultCrewmateVision, 0f, 5f);


        opt.SetFloat(FloatOptionNames.CrewLightMod, vision);
        opt.SetFloat(FloatOptionNames.ImpostorLightMod, vision);


        if (!ignoreBlackout && Utils.IsActive(SystemTypes.Electrical))
        {
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, vision / 5f);
        }
        AURoleOptions.PhantomCooldown = 0.1f;
    }

    bool IsCooldownLinked() => OptionLinkCooldowns.GetBool();
    bool CanStartSwallowNow() => Player.IsAlive() && pendingSwallow == null && GetSwallowCooldownRemaining() <= 0f;

    float GetBaseSwallowCooldown() => IsCooldownLinked() ? OptionSwallowCooldown.GetFloat() : OptionSharedCooldown.GetFloat();
    float GetBaseEatCooldown() => IsCooldownLinked() ? OptionEatCooldown.GetFloat() : OptionSharedCooldown.GetFloat();

    float GetCurrentSwallowCooldownValue() => GetBaseSwallowCooldown() * (1f + extraCooldownRate);
    float GetCurrentEatCooldownValue() => GetBaseEatCooldown() * (1f + extraCooldownRate);
    float GetCurrentSwallowCastTime() => Mathf.Max(0.5f, OptionSwallowTime.GetFloat() + extraSwallowTime);

    float GetEatCooldownRemaining() => Mathf.Max(0f, eatCooldownTimer);
    float GetSwallowCooldownRemaining() => Mathf.Max(0f, swallowCooldownTimer);
    float GetPendingRemaining() => pendingSwallow == null ? 0f : Mathf.Max(0f, pendingSwallow.Required - pendingSwallow.Elapsed);
    float GetCurrentKillCooldownTimer() => pendingSwallow != null ? GetPendingRemaining() : GetSwallowCooldownRemaining();

    float GetConfiguredSwallowRange()
    {
        var rangeIndex = Mathf.Clamp(OptionSwallowRange.GetValue(), 0, NormalGameOptionsV10.KillDistances.Length - 1);
        return NormalGameOptionsV10.KillDistances[rangeIndex];
    }

    void TickCooldown(float dt)
    {
        if (eatCooldownTimer > 0f) eatCooldownTimer = Mathf.Max(0f, eatCooldownTimer - dt);
        if (swallowCooldownTimer > 0f) swallowCooldownTimer = Mathf.Max(0f, swallowCooldownTimer - dt);
    }

    void RefreshKillCooldown(bool sync = true)
    {
        Player.ResetKillCooldown();
        if (sync) Player.SyncSettings();
    }

    void StartEatCooldown()
    {
        var cooldown = GetCurrentEatCooldownValue();
        eatCooldownTimer = cooldown;
        if (!IsCooldownLinked())
        {
            swallowCooldownTimer = cooldown;
            Player.SetKillCooldown(cooldown, force: true, delay: true);
        }
    }

    void StartSwallowCooldown()
    {
        var cooldown = GetCurrentSwallowCooldownValue();
        swallowCooldownTimer = cooldown;
        if (!IsCooldownLinked())
        {
            eatCooldownTimer = cooldown;
        }
        Player.SetKillCooldown(cooldown, force: true, delay: true);
    }

    void AddEatPenalty()
    {
        extraSwallowTime += OptionSwallowTime.GetFloat() * (OptionEatIncreaseRate.GetFloat() / 100f);
    }

    void AddSwallowPenalty()
    {
        extraCooldownRate += OptionSwallowCooldownIncreaseRate.GetFloat() / 100f;
    }

    void ReduceSwallowTimeAfterMeeting()
    {
        var reduce = OptionSwallowTime.GetFloat() * (OptionMeetingReduceRate.GetFloat() / 100f);
        extraSwallowTime = Mathf.Max(0f, extraSwallowTime - reduce);
    }

    void AddDeadBodyArrow(byte playerId, Vector2 pos)
        => AddDeadBodyArrow(playerId, pos, sync: true);

    void AddDeadBodyArrow(byte playerId, Vector2 pos, bool sync)
    {
        if (!OptionShowArrow.GetBool()) return;

        if (deadBodyPositions.TryGetValue(playerId, out var oldPos))
        {
            GetArrow.Remove(Player.PlayerId, oldPos);
        }

        var position = new Vector3(pos.x, pos.y, 0f);
        deadBodyPositions[playerId] = position;
        GetArrow.Add(Player.PlayerId, position);

        if (sync) RpcAddDeadBodyArrow(playerId, pos);
        UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player);
    }

    void RemoveDeadBodyArrow(byte playerId)
        => RemoveDeadBodyArrow(playerId, sync: true);

    void RemoveDeadBodyArrow(byte playerId, bool sync)
    {
        if (!deadBodyPositions.TryGetValue(playerId, out var pos)) return;
        GetArrow.Remove(Player.PlayerId, pos);
        deadBodyPositions.Remove(playerId);

        if (sync) RpcRemoveDeadBodyArrow(playerId);
        UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player);
    }

    void ClearAllBodyArrows()
        => ClearAllBodyArrows(sync: true);

    void ClearAllBodyArrows(bool sync)
    {
        foreach (var pos in deadBodyPositions.Values)
        {
            GetArrow.Remove(Player.PlayerId, pos);
        }
        deadBodyPositions.Clear();

        if (sync) RpcClearAllBodyArrows();
        UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player);
    }

    void RpcAddDeadBodyArrow(byte playerId, Vector2 pos)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        using var sender = CreateSender();
        sender.Writer.WritePacked((int)RPCType.AddDeadBodyArrow);
        sender.Writer.Write(playerId);
        NetHelpers.WriteVector2(pos, sender.Writer);
    }

    void RpcRemoveDeadBodyArrow(byte playerId)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        using var sender = CreateSender();
        sender.Writer.WritePacked((int)RPCType.RemoveDeadBodyArrow);
        sender.Writer.Write(playerId);
    }

    void RpcClearAllBodyArrows()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        using var sender = CreateSender();
        sender.Writer.WritePacked((int)RPCType.ClearDeadBodyArrows);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        switch ((RPCType)reader.ReadPackedInt32())
        {
            case RPCType.AddDeadBodyArrow:
                var playerId = reader.ReadByte();
                var pos = NetHelpers.ReadVector2(reader);
                AddDeadBodyArrow(playerId, pos, sync: false);
                break;
            case RPCType.RemoveDeadBodyArrow:
                RemoveDeadBodyArrow(reader.ReadByte(), sync: false);
                break;
            case RPCType.ClearDeadBodyArrows:
                ClearAllBodyArrows(sync: false);
                break;
        }
    }

    enum RPCType
    {
        AddDeadBodyArrow,
        RemoveDeadBodyArrow,
        ClearDeadBodyArrows
    }

    void BeginSwallow(PlayerControl target)
    {
        var required = GetCurrentSwallowCastTime();
        pendingSwallow = new PendingSwallowInfo(target.PlayerId, required);
        Player.SetKillCooldown(required, target: target, force: true, delay: true);
        UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player);
    }

    bool IsWithinSwallowRange(PlayerControl target)
    {
        if (target == null) return false;
        var distance = Vector2.Distance(Player.GetTruePosition(), target.GetTruePosition());
        return distance <= GetConfiguredSwallowRange();
    }

    void CancelPendingSwallow()
    {
        if (pendingSwallow == null) return;
        pendingSwallow = null;
        RefreshKillCooldown();
        UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player);
    }

    void CancelPendingSwallowByDistance()
    {
        if (pendingSwallow == null) return;
        pendingSwallow = null;
        Player.SetKillCooldown(0.1f, force: true, delay: true);
        UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player);
    }

    PlayerControl GetNearestPlayerTo(Vector2 origin)
        => PlayerCatch.AllAlivePlayerControls
            .Where(pc => pc.PlayerId != Player.PlayerId)
            .OrderBy(pc => Vector2.Distance(pc.GetTruePosition(), origin))
            .FirstOrDefault();

    void CompleteSwallow()
    {
        var target = PlayerCatch.GetPlayerById(pendingSwallow.TargetId);
        var origin = target != null ? target.GetTruePosition() : Player.GetTruePosition();
        var exileTarget = GetNearestPlayerTo(origin);

        if (exileTarget != null)
        {
            var state = PlayerState.GetByPlayerId(exileTarget.PlayerId);
            if (state != null) state.DeathReason = CustomDeathReason.Swallowed;
            exileTarget.SetRealKiller(Player);
            exileTarget.RpcExileV3();
            UtilsGameLog.AddGameLog("Eater", $"{UtilsName.GetPlayerColor(Player)} swallowed {UtilsName.GetPlayerColor(exileTarget)}");
            RemoveDeadBodyArrow(exileTarget.PlayerId);
        }

        eatOrSwallowCount++;
        usedAbilityThisRound = true;
        AddSwallowPenalty();
        pendingSwallow = null;
        StartSwallowCooldown();
        TryWinByCount();

        Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[0]);
        UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player);
    }

    void EatBody(NetworkedPlayerInfo target)
    {
        EatenBodies.Add(target.PlayerId);
        RemoveDeadBodyArrow(target.PlayerId);

        eatOrSwallowCount++;
        usedAbilityThisRound = true;
        AddEatPenalty();
        StartEatCooldown();
        TryWinByCount();

        UtilsGameLog.AddGameLog("Eater", $"{UtilsName.GetPlayerColor(Player)} ate {UtilsName.GetPlayerColor(target)}");
        Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[0]);
        UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player);
    }

    void TryWinByCount()
    {
        if (!Player.IsAlive()) return;
        if (eatOrSwallowCount < OptionWinCount.GetInt()) return;

        if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Eater, Player.PlayerId, true))
        {
            CustomWinnerHolder.NeutralWinnerIds.Add(Player.PlayerId);
            CustomWinnerHolder.WinnerIds.Add(Player.PlayerId);
        }
    }

    bool ShouldWinByLastSurvivorRule()
    {
        if (CustomWinnerHolder.WinnerTeam is not (CustomWinner.Crewmate or CustomWinner.Remotekiller or CustomWinner.None)) return false;

        var alivePlayers = PlayerCatch.AllAlivePlayerControls.ToList();
        if (alivePlayers.Count != 2) return false;

        var other = alivePlayers.FirstOrDefault(pc => pc.PlayerId != Player.PlayerId);
        if (other == null) return false;

        return IsValidLastOpponent(other);
    }

    bool IsValidLastOpponent(PlayerControl other)
    {
        if (other.Is(CustomRoleTypes.Crewmate)) return true;
        if (!other.Is(CustomRoleTypes.Neutral)) return false;

        if (other.GetRoleClass() is IKiller killer)
            return !killer.CanUseKillButton();

        return true;
    }

    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => OptionCanUseVent.GetBool();

    public bool CanUseKillButton() => Player.IsAlive();
    public bool CanKill => Player.IsAlive();
    public bool IsKiller => true;
    public float CalculateKillCooldown() => Mathf.Max(GetCurrentKillCooldownTimer(), 0.01f);

    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        if (!Is(info.AttemptKiller)) return;
        info.DoKill = false;

        if (!CanStartSwallowNow()) return;

        var target = info.AttemptTarget;
        if (target == null || !target.IsAlive() || target.PlayerId == Player.PlayerId) return;
        if (!IsWithinSwallowRange(target)) return;

        BeginSwallow(target);
    }

    public override bool CancelReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target, ref DontReportreson reason)
    {
        if (target == null) return false;

        if (EatenBodies.Contains(target.PlayerId))
        {
            reason = DontReportreson.Eat;
            return true;
        }

        if (reporter.PlayerId != Player.PlayerId) return false;
        if (!Player.IsAlive()) return false;
        if (!eatMode) return false;

        if (GetEatCooldownRemaining() > 0f)
        {
            reason = DontReportreson.Other;
            return true;
        }

        EatBody(target);
        reason = DontReportreson.Eat;
        return true;
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        CancelPendingSwallow();
        ClearAllBodyArrows();
    }

    public override void OnStartMeeting()
    {
        CancelPendingSwallow();
        ClearAllBodyArrows();
        if (OptionResetCooldownOnMeeting.GetBool())
        {
            eatCooldownTimer = 0f;
            swallowCooldownTimer = 0f;
            RefreshKillCooldown();
        }
    }

    public override void AfterMeetingTasks()
    {
        if (!usedAbilityThisRound)
        {
            ReduceSwallowTimeAfterMeeting();
        }

        usedAbilityThisRound = false;
        pendingSwallow = null;
        EatenBodies.Clear();
        ClearAllBodyArrows();

        if (OptionResetCooldownOnMeeting.GetBool())
        {
            eatCooldownTimer = 0f;
            swallowCooldownTimer = 0f;
            RefreshKillCooldown();
        }
    }

    public override void OnLeftPlayer(PlayerControl player)
    {
        if (pendingSwallow != null && pendingSwallow.TargetId == player.PlayerId)
        {
            CancelPendingSwallow();
        }
        RemoveDeadBodyArrow(player.PlayerId);
        EatenBodies.Remove(player.PlayerId);
    }

    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!Player.IsAlive())
        {
            pendingSwallow = null;
            return;
        }

        TickCooldown(Time.fixedDeltaTime);

        if (pendingSwallow == null) return;

        var target = PlayerCatch.GetPlayerById(pendingSwallow.TargetId);
        if (target == null || !target.IsAlive())
        {
            CancelPendingSwallow();
            return;
        }
        if (!IsWithinSwallowRange(target))
        {
            CancelPendingSwallowByDistance();
            return;
        }

        pendingSwallow.Elapsed += Time.fixedDeltaTime;
        if (pendingSwallow.Elapsed >= pendingSwallow.Required)
        {
            CompleteSwallow();
        }
    }

    bool? IKillFlashSeeable.CheckKillFlash(MurderInfo info)
    {
        if (!OptionShowArrow.GetBool()) return false;
        if (!Player.IsAlive()) return false;

        var dead = info.AppearanceTarget;
        if (dead == null) return false;

        AddDeadBodyArrow(dead.PlayerId, dead.GetTruePosition());
        return false;
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (isForMeeting) return "";
        if (!OptionShowArrow.GetBool()) return "";
        if (!Player.IsAlive()) return "";
        if (!Is(seer) || !Is(seen)) return "";
        if (deadBodyPositions.Count == 0) return "";

        var arrows = "";
        foreach (var pos in deadBodyPositions.Values)
        {
            arrows += GetArrow.GetArrows(seer, pos);
        }
        return arrows == "" ? "" : $"<{RoleInfo.RoleColorCode}>{arrows}</color>";
    }

    public override string GetProgressText(bool comms = false, bool gameLog = false)
        => $" <color={RoleInfo.RoleColorCode}>({eatOrSwallowCount}/{OptionWinCount.GetInt()})</color>";

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (isForMeeting) return "";
        if (!Is(seer) || !Is(seen) || !Player.IsAlive()) return "";

        var modeText = eatMode ? "食らう" : "レポート";
        var eatCd = Mathf.CeilToInt(GetEatCooldownRemaining());
        var swCd = Mathf.CeilToInt(GetCurrentKillCooldownTimer());

        if (isForHud) return $"[{modeText}]";
        return $"<size=60%>[{modeText}] E:{eatCd}s S:{swCd}s</size>";
    }

    void IUsePhantomButton.OnClick(ref bool AdjustKillCooldown, ref bool? ResetCooldown)
    {
        AdjustKillCooldown = false;
        ResetCooldown = true;

        if (!Player.IsAlive()) return;
        if (pendingSwallow != null) return;

        eatMode = !eatMode;
        Player.RpcResetAbilityCooldown();
        UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player);
    }

    public bool UseOneclickButton => true;
    public bool IsPhantomRole => true;
    public bool IsresetAfterKill => false;

    public override void CheckWinner(GameOverReason reason)
    {
        if (!Player.IsAlive()) return;
        if (CustomWinnerHolder.WinnerTeam is CustomWinner.Eater) return;
        if (!ShouldWinByLastSurvivorRule()) return;

        if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Eater, Player.PlayerId, true))
        {
            CustomWinnerHolder.NeutralWinnerIds.Add(Player.PlayerId);
            CustomWinnerHolder.WinnerIds.Add(Player.PlayerId);
        }
    }

    public static Dictionary<int, Achievement> achievements = new();
    [Attributes.PluginModuleInitializer]
    public static void Load()
    {
        var n1 = new Achievement(RoleInfo, 0, 1, 0, 0);
        achievements.Add(0, n1);
    }
}