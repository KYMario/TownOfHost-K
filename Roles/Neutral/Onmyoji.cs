using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Hazel;
using UnityEngine;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.PlayerCatch;
using static TownOfHost.Utils;
using static TownOfHost.Translator;
using static TownOfHost.Modules.SelfVoteManager;

namespace TownOfHost.Roles.Neutral;

public sealed class Onmyoji : RoleBase, ISelfVoter
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Onmyoji),
            player => new Onmyoji(player),
            CustomRoles.Onmyoji,
            () => (OptionCanUseVent?.GetBool() ?? true) ? RoleTypes.Engineer : RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            80200,
            SetupOptionItem,
            "oy",
            "#9b59b6",
            (6, 1),
            true,
            countType: CountTypes.None,
            from: From.SuperNewRoles
        );

    static OptionItem OptionWinTaskCount;
    static OptionItem OptionCanUseVent;
    static OptionItem OptionVentCooldown;
    static OptionItem OptionVentDuration;
    static OptionItem OptionImpostorVision;
    static OptionItem OptionCanCreateShikigami;
    static OptionItem OptionCreateShikigamiCooldown;
    static OptionItem OptionNeedTaskToWin;
    static OptionItem OptionCanHijackCrewWin;
    static OptionItem OptionDisableReport;
    static OptionItem OptionDisableEmergencyMeeting;
    static OptionItem OptionShikigamiShiftCooldown;
    static OptionItem OptionShikigamiSuicideCooldown;

    public List<byte> ShikigamiIds;
    bool hasCompletedTaskRequirement;
    float nearTimer;
    float createCooldownTimer;
    float spawnWaitTimer;
    public byte NextShikigamiCandidate;

    bool SkCanApproach => spawnWaitTimer >= 3f;

    enum OptionName
    {
        OnmyojiWinTaskCount,
        OnmyojiCanCreateShikigami,
        OnmyojiCreateShikigamiCooldown,
        OnmyojiNeedTaskToWin,
        OnmyojiCanHijackCrewWin,
        OnmyojiDisableReport,
        OnmyojiDisableEmergencyMeeting,
        OnmyojiShikigamiShiftCooldown,
        OnmyojiShikigamiSuicideCooldown,
    }

    public Onmyoji(PlayerControl player)
        : base(RoleInfo, player, () => HasTask.True)
    {
        ShikigamiIds = new();
        hasCompletedTaskRequirement = !(OptionNeedTaskToWin?.GetBool() ?? false);
        nearTimer = 0f;
        createCooldownTimer = 0f;
        spawnWaitTimer = -1f;
        NextShikigamiCandidate = byte.MaxValue;

        MyTaskState.NeedTaskCount = OptionWinTaskCount.GetInt();
    }

    private static void SetupOptionItem()
    {
        SoloWinOption.Create(RoleInfo, 8, defo: 15);

        OptionWinTaskCount = IntegerOptionItem.Create(RoleInfo, 9, OptionName.OnmyojiWinTaskCount, new(1, 99, 1), 5, false)
            .SetValueFormat(OptionFormat.Times);

        OptionCanUseVent = BooleanOptionItem.Create(RoleInfo, 10, GeneralOption.CanVent, true, false);
        OptionVentCooldown = FloatOptionItem.Create(RoleInfo, 11, "OnmyojiVentCooldown", new(0f, 60f, 2.5f), 15f, false, OptionCanUseVent)
            .SetValueFormat(OptionFormat.Seconds);
        OptionVentDuration = FloatOptionItem.Create(RoleInfo, 12, "OnmyojiVentDuration", new(0f, 60f, 2.5f), 10f, false, OptionCanUseVent)
            .SetValueFormat(OptionFormat.Seconds);
        OptionImpostorVision = BooleanOptionItem.Create(RoleInfo, 13, GeneralOption.ImpostorVision, true, false);

        OptionCanCreateShikigami = BooleanOptionItem.Create(RoleInfo, 14, OptionName.OnmyojiCanCreateShikigami, true, false);
        OptionCreateShikigamiCooldown = FloatOptionItem.Create(RoleInfo, 15, OptionName.OnmyojiCreateShikigamiCooldown, new(0f, 60f, 2.5f), 20f, false, OptionCanCreateShikigami)
            .SetValueFormat(OptionFormat.Seconds);

        OptionNeedTaskToWin = BooleanOptionItem.Create(RoleInfo, 16, OptionName.OnmyojiNeedTaskToWin, true, false);
        OptionCanHijackCrewWin = BooleanOptionItem.Create(RoleInfo, 17, OptionName.OnmyojiCanHijackCrewWin, true, false);
        OptionDisableReport = BooleanOptionItem.Create(RoleInfo, 18, OptionName.OnmyojiDisableReport, false, false);
        OptionDisableEmergencyMeeting = BooleanOptionItem.Create(RoleInfo, 19, OptionName.OnmyojiDisableEmergencyMeeting, false, false);

        OptionShikigamiShiftCooldown = FloatOptionItem.Create(RoleInfo, 20, OptionName.OnmyojiShikigamiShiftCooldown, new(0f, 60f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionShikigamiSuicideCooldown = FloatOptionItem.Create(RoleInfo, 21, OptionName.OnmyojiShikigamiSuicideCooldown, new(0f, 60f, 2.5f), 10f, false)
            .SetValueFormat(OptionFormat.Seconds);

        OverrideTasksData.Create(RoleInfo, 30);

        PavlovDog.HideRoleOptions(CustomRoles.Shikigami);
    }

    public static float GetShikigamiShiftCooldown() => OptionShikigamiShiftCooldown?.GetFloat() ?? 20f;
    public static float GetShikigamiSuicideCooldown() => OptionShikigamiSuicideCooldown?.GetFloat() ?? 10f;

    public override void OnSpawn(bool initialState)
    {
        if (initialState)
        {
            ShikigamiIds.Clear();
            hasCompletedTaskRequirement = !(OptionNeedTaskToWin?.GetBool() ?? false);
            nearTimer = 0f;
            createCooldownTimer = OptionCreateShikigamiCooldown.GetFloat();
            spawnWaitTimer = -1f;
            NextShikigamiCandidate = byte.MaxValue;
            NameColorManager.RemoveAll(Player.PlayerId);
        }
        RefreshStarReadingTargets();
    }

    public override void OnDestroy()
    {
        NameColorManager.RemoveAll(Player.PlayerId);

        foreach (var id in ShikigamiIds)
            TargetArrow.Remove(Player.PlayerId, id);
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(OptionImpostorVision.GetBool());

        if (OptionCanUseVent.GetBool())
        {
            AURoleOptions.EngineerCooldown = OptionVentCooldown.GetFloat();
            AURoleOptions.EngineerInVentMaxTime = OptionVentDuration.GetFloat();
        }

        if (OptionDisableEmergencyMeeting.GetBool())
            opt.SetInt(Int32OptionNames.NumEmergencyMeetings, 0);
    }

    bool ISelfVoter.CanUseVoted()
        => Player.IsAlive()
           && OptionCanCreateShikigami.GetBool()
           && ShikigamiIds.Count < 1;

    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (!Is(voter)) return true;
        if (!Player.IsAlive() || !OptionCanCreateShikigami.GetBool() || ShikigamiIds.Count >= 1) return true;

        if (CheckSelfVoteMode(Player, votedForId, out var status))
        {
            if (status is VoteStatus.Self)
            {
                NextShikigamiCandidate = byte.MaxValue;
                Utils.SendMessage("<color=#9b59b6>【式神任命モード】</color>\n候補に投票 → 次ターン3秒近づいて作成\nスキップ → キャンセル", Player.PlayerId);
                SetMode(Player, true);
                SendRPC();
                return false;
            }
            if (status is VoteStatus.Skip)
            {
                NextShikigamiCandidate = byte.MaxValue;
                Utils.SendMessage(GetString("VoteSkillFin"), Player.PlayerId);
                SetMode(Player, false);
                SendRPC();
                return false;
            }
            if (status is VoteStatus.Vote)
            {
                var target = GetPlayerById(votedForId);
                if (target == null || !target.IsAlive() || votedForId == Player.PlayerId)
                {
                    Utils.SendMessage("<color=#9b59b6>その相手は式神にできません。</color>", Player.PlayerId);
                    SetMode(Player, false);
                    return false;
                }
                if (!IsValidShikigamiTarget(target))
                {
                    Utils.SendMessage("<color=#9b59b6>キル能力を持たない相手のみ式神にできます。</color>", Player.PlayerId);
                    SetMode(Player, false);
                    return false;
                }

                NextShikigamiCandidate = votedForId;
                nearTimer = 0f;
                Utils.SendMessage($"<color=#9b59b6>【式神候補設定】</color>\n{UtilsName.GetPlayerColor(target, true)} を候補に設定しました。\n次ターン、3秒近づいて式神作成！", Player.PlayerId);
                SetMode(Player, false);
                SendRPC();
                return false;
            }
        }
        return true;
    }

    public override void OnStartMeeting()
    {
        NextShikigamiCandidate = byte.MaxValue;
        nearTimer = 0f;
        spawnWaitTimer = -1f;
        RefreshStarReadingTargets();
    }

    public override void AfterMeetingTasks()
    {
        spawnWaitTimer = 0f;
        createCooldownTimer = OptionCreateShikigamiCooldown.GetFloat();
        nearTimer = 0f;

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Player.IsAlive()) return;

        RefreshStarReadingTargets();

        foreach (var id in ShikigamiIds)
        {
            var sk = GetPlayerById(id);
            if (sk == null || !sk.IsAlive()) continue;
            TargetArrow.Add(Player.PlayerId, id);
        }
    }

    public override void OnFixedUpdate(PlayerControl player)
    {
        bool needsUiUpdate = false;

        if (createCooldownTimer > 0f)
        {
            createCooldownTimer = Mathf.Max(0f, createCooldownTimer - Time.fixedDeltaTime);
            needsUiUpdate = true;
        }

        if (GameStates.IsInTask && Player.IsAlive() && OptionCanCreateShikigami.GetBool() && ShikigamiIds.Count < 1)
        {
            if (spawnWaitTimer >= 0f && spawnWaitTimer < 3f)
            {
                spawnWaitTimer += Time.fixedDeltaTime;
                if (spawnWaitTimer > 3f) spawnWaitTimer = 3f;
                needsUiUpdate = true;
            }

            if (NextShikigamiCandidate != byte.MaxValue)
            {
                needsUiUpdate = true;
                if (SkCanApproach && createCooldownTimer <= 0f)
                {
                    var target = GetPlayerById(NextShikigamiCandidate);
                    if (target == null || !target.IsAlive() || !IsValidShikigamiTarget(target))
                    {
                        if (AmongUsClient.Instance.AmHost)
                        {
                            NextShikigamiCandidate = byte.MaxValue;
                            nearTimer = 0f;
                            SendRPC();
                        }
                    }
                    else
                    {
                        float dist = Vector2.Distance(Player.GetTruePosition(), target.GetTruePosition());
                        if (dist <= 1.0f)
                        {
                            nearTimer += Time.fixedDeltaTime;

                            if (AmongUsClient.Instance.AmHost && nearTimer >= 3f)
                            {
                                NextShikigamiCandidate = byte.MaxValue;
                                nearTimer = 0f;
                                AddShikigami(target);
                            }
                        }
                        else
                        {
                            nearTimer = 0f;
                        }
                    }
                }
            }
        }

        if (needsUiUpdate && player.AmOwner && Is(player))
        {
            UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);
        }

        if (!AmongUsClient.Instance.AmHost) return;
        if (!GameStates.IsInTask) return;
        if (!Player.IsAlive()) return;
        if (!OptionCanCreateShikigami.GetBool()) return;
    }

    bool IsValidShikigamiTarget(PlayerControl target)
    {
        if (target == null) return false;
        if (!target.IsAlive()) return false;
        if (target.PlayerId == Player.PlayerId) return false;
        if (target.Is(CustomRoles.PavlovOwner)) return true;

        if (IsOnmyojiKillerTarget(target)) return false;

        return true;
    }

    void AddShikigami(PlayerControl target)
    {
        if (ShikigamiIds.Count >= 1) return;
        if (!IsValidShikigamiTarget(target)) return;

        ShikigamiIds.Add(target.PlayerId);
        TargetArrow.Add(Player.PlayerId, target.PlayerId);

        if (!RoleSendList.Contains(target.PlayerId))
            RoleSendList.Add(target.PlayerId);

        target.RpcSetCustomRole(CustomRoles.Shikigami, log: null);
        _ = new LateTask(() =>
        {
            if (target.GetRoleClass() is Shikigami sk)
                sk.SetOwner(Player.PlayerId);
        }, 0.1f, "Onmyoji.SetOwner", true);

        NameColorManager.Add(Player.PlayerId, target.PlayerId, "#9b59b6");

        createCooldownTimer = OptionCreateShikigamiCooldown.GetFloat();

        SendRPC();
        _ = new LateTask(() => UtilsNotifyRoles.NotifyRoles(), 0.2f, "Onmyoji.Shikigami", true);
    }

    void RefreshStarReadingTargets()
    {
        NameColorManager.RemoveAll(Player.PlayerId);

        if (!Player.IsAlive()) return;

        foreach (var pc in AllPlayerControls)
        {
            if (pc == null || pc.PlayerId == Player.PlayerId) continue;
            if (!pc.IsAlive()) continue;
            if (!IsStarReadingTarget(pc)) continue;

            NameColorManager.Add(Player.PlayerId, pc.PlayerId, UtilsRoleText.GetRoleColorCode(pc.GetCustomRole()));
        }

        foreach (var id in ShikigamiIds)
        {
            NameColorManager.Add(Player.PlayerId, id, "#9b59b6");
        }
    }

    public override bool OnCompleteTask(uint taskid)
    {
        if (MyTaskState.HasCompletedEnoughCountOfTasks(OptionWinTaskCount.GetInt()))
            hasCompletedTaskRequirement = true;

        return true;
    }

    public override bool CancelReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target, ref DontReportreson reason)
    {
        if (reporter == null || reporter.PlayerId != Player.PlayerId) return false;
        if (!OptionDisableReport.GetBool()) return false;

        reason = DontReportreson.CantUseButton;
        return true;
    }

    public override void OnLeftPlayer(PlayerControl player)
    {
        if (player == null) return;

        if (player.PlayerId == NextShikigamiCandidate)
        {
            NextShikigamiCandidate = byte.MaxValue;
            nearTimer = 0f;
            if (AmongUsClient.Instance.AmHost) SendRPC();
        }

        ShikigamiIds.Remove(player.PlayerId);
        TargetArrow.Remove(Player.PlayerId, player.PlayerId);
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (!Is(seer) || !Player.IsAlive()) return "";

        if (seer.PlayerId != seen.PlayerId)
        {
            if (!IsStarReadingTarget(seen)) return "";
            var colorCode = UtilsRoleText.GetRoleColorCode(seen.GetCustomRole());
            return $" <color={colorCode}>★</color>";
        }

        if (isForMeeting) return "";

        var result = "";
        if (ShikigamiIds.Count == 0) return result;

        var arrows = "";
        foreach (var id in ShikigamiIds)
        {
            var sk = GetPlayerById(id);
            if (sk == null || !sk.IsAlive()) continue;
            arrows += TargetArrow.GetArrows(seer, id);
        }

        if (arrows != "")
            result += $"<color=#9b59b6>{arrows}</color>";

        return result;
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (seen.PlayerId != seer.PlayerId || !Player.IsAlive()) return "";
        string size = isForHud ? "" : "<size=60%>";

        if (OptionCanCreateShikigami.GetBool() && ShikigamiIds.Count < 1)
        {
            string cdText = createCooldownTimer > 0f ? $"作成 CD: {Mathf.CeilToInt(createCooldownTimer)}s" : "作成準備完了";

            if (NextShikigamiCandidate != byte.MaxValue)
            {
                var target = GetPlayerById(NextShikigamiCandidate);
                string name = target != null ? target.Data.PlayerName : "???";

                if (isForMeeting) return $"{size}<color=#9b59b6>候補: {name} (次ターン接近で作成)</color>";

                if (createCooldownTimer > 0f)
                    return $"{size}<color=#9b59b6>{cdText} | 候補: {name}</color>";
                if (!SkCanApproach)
                    return $"{size}<color=#9b59b6>待機中... | 候補: {name}</color>";
                float progress = System.Math.Min(nearTimer, 3f);
                return $"{size}<color=#9b59b6>{name}に近づき中 {progress:F1}/3.0s</color>";
            }

            if (isForMeeting) return $"{size}<color=#9b59b6>自投票→式神候補を投票で指定</color>";
            return $"{size}<color=#9b59b6>{cdText} | 【会議で自投票→候補指定】</color>";
        }

        return "";
    }

    bool IsStarReadingTarget(PlayerControl target)
    {
        if (target == null) return false;
        if (!target.IsAlive()) return false;
        if (target.Is(CustomRoles.PavlovOwner)) return false;
        return IsOnmyojiKillerTarget(target);
    }

    bool IsOnmyojiKillerTarget(PlayerControl target)
    {
        if (target == null) return false;

        var role = target.GetCustomRole();
        if (target.Is(CustomRoleTypes.Impostor)) return true;

        return role is
            CustomRoles.CountKiller or
            CustomRoles.Strawdoll or
            CustomRoles.Jackal or
            CustomRoles.JackalHadouHo or
            CustomRoles.JackalMafia or
            CustomRoles.JackalAlien or
            CustomRoles.DoppelGanger or
            CustomRoles.GrimReaper or
            CustomRoles.Remotekiller or
            CustomRoles.Egoist or
            CustomRoles.Eater or
            CustomRoles.PavlovDog or
            CustomRoles.Sheriff or
            CustomRoles.SwitchSheriff or
            CustomRoles.MeetingSheriff or
            CustomRoles.WolfBoy;
    }

    bool CanWinNow()
    {
        if (!(OptionNeedTaskToWin?.GetBool() ?? false)) return true;

        var needCount = OptionWinTaskCount?.GetInt() ?? 0;
        return hasCompletedTaskRequirement || MyTaskState.HasCompletedEnoughCountOfTasks(needCount);
    }

    public static bool TryTakeOverCrewWin(ref GameOverReason reason)
    {
        var currentWinner = CustomWinnerHolder.WinnerTeam;
        if (currentWinner is CustomWinner.Default or CustomWinner.Onmyoji) return false;
        if (currentWinner is CustomWinner.Crewmate && !(OptionCanHijackCrewWin?.GetBool() ?? true)) return false;

        foreach (var pc in AllPlayerControls)
        {
            if (pc == null || !pc.IsAlive()) continue;
            if (pc.GetRoleClass() is not Onmyoji onmyoji) continue;
            if (!onmyoji.CanWinNow()) continue;

            if (!CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Onmyoji, pc.PlayerId, true))
                continue;

            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Onmyoji);
            CustomWinnerHolder.NeutralWinnerIds.Add(pc.PlayerId);
            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);

            foreach (var id in onmyoji.ShikigamiIds)
            {
                var sk = GetPlayerById(id);
                if (sk == null) continue;
                if (sk.Data?.Disconnected == true) continue;
                if (!sk.Is(CustomRoles.Shikigami)) continue;

                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Shikigami);
                CustomWinnerHolder.NeutralWinnerIds.Add(id);
                CustomWinnerHolder.WinnerIds.Add(id);
            }

            reason = GameOverReason.CrewmatesByVote;
            return true;
        }

        return false;
    }

    public override string GetProgressText(bool comms = false, bool gameLog = false)
    {
        var count = ShikigamiIds.Count;
        var ready = CanWinNow() ? "#9b59b6" : "#5e5e5e";
        var cd = Mathf.CeilToInt(Mathf.Max(0f, createCooldownTimer));
        var cdText = cd > 0 ? $" ({cd}s)" : "";
        return $"<color={ready}>式:{count}/1{cdText}</color>";
    }

    public void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(ShikigamiIds.Count);
        foreach (var id in ShikigamiIds)
            sender.Writer.Write(id);
        sender.Writer.Write(hasCompletedTaskRequirement);
        sender.Writer.Write(createCooldownTimer);
        sender.Writer.Write(NextShikigamiCandidate);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        int count = reader.ReadInt32();
        ShikigamiIds = new();
        for (int i = 0; i < count; i++)
            ShikigamiIds.Add(reader.ReadByte());

        hasCompletedTaskRequirement = reader.ReadBoolean();
        createCooldownTimer = reader.ReadSingle();
        if (reader.BytesRemaining > 0)
        {
            NextShikigamiCandidate = reader.ReadByte();
        }
    }

    public override string GetAbilityButtonText() => GetString("OnmyojiAbilityButtonText");
}