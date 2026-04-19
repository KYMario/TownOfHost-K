using System;
using AmongUs.GameOptions;
using Hazel;
using UnityEngine;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.PlayerCatch;
using static TownOfHost.Utils;
using static TownOfHost.Translator;
using static TownOfHost.Modules.SelfVoteManager;

namespace TownOfHost.Roles.Crewmate;

public sealed class VillageChief : RoleBase, ISelfVoter
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(VillageChief),
            player => new VillageChief(player),
            CustomRoles.VillageChief,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            60000,
            SetupOptionItem,
            "vc",
            "#f5a623",
            (2, 0),
            from: From.SuperNewRoles
        );

    public VillageChief(PlayerControl player)
        : base(RoleInfo, player, () => HasTask.True)
    {
        hasUsedAbility = false;
        nearTimer = 0f;
        spawnWaitTimer = -1f;   // ★ -1 = 近接処理無効
        NextAppointCandidate = byte.MaxValue;
        appointedSheriff = null;
    }

    private bool hasUsedAbility;
    private float nearTimer;

    // ★ 会議後スポーン待機タイマー（-1で無効、0以上でカウント中、3f以上で有効）
    private float spawnWaitTimer;

    // ★ 近接処理が有効か
    private bool CanApproach => spawnWaitTimer >= 3f;

    public byte NextAppointCandidate;
    public PlayerControl appointedSheriff = null;

    private static OptionItem NotifyTarget;
    private static readonly string[] NotifyTargetOptions =
        ["送信しない", "全員", "村長のみ", "シェリフのみ", "村長とシェリフ"];

    private static void SetupOptionItem()
    {
        NotifyTarget = StringOptionItem.Create(
            RoleInfo, 12, "VillageChiefNotifyTarget",
            NotifyTargetOptions, 0, false
        );
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(false);
    }

    bool ISelfVoter.CanUseVoted()
        => Player.IsAlive() && !hasUsedAbility;

    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (!Is(voter)) return true;
        if (!Player.IsAlive()) return true;
        if (hasUsedAbility) return true;

        if (CheckSelfVoteMode(Player, votedForId, out var status))
        {
            if (status is VoteStatus.Self)
            {
                SendMessage(
                    "<color=#f5a623>任命モードになりました！</color>\n\n" +
                    "誰かに投票 → <color=#f5a623>任命候補に指定</color>\n" +
                    "投票スキップ → <color=#f5a623>任命をキャンセル</color>",
                    Player.PlayerId
                );
                SetMode(Player, true);
                return false;
            }

            if (status is VoteStatus.Vote)
            {
                if (votedForId == Player.PlayerId || votedForId == SkipId)
                {
                    SendMessage("<color=#f5a623>その相手は任命できません。</color>", Player.PlayerId);
                    SetMode(Player, false);
                    return false;
                }

                NextAppointCandidate = votedForId;

                SendMessage(
                    "<color=#f5a623>任命候補を設定しました！</color>\n" +
                    "次のターン、この相手に3秒近づくと任命します。",
                    Player.PlayerId
                );

                SetMode(Player, false);
                return false;
            }

            if (status is VoteStatus.Skip)
            {
                NextAppointCandidate = byte.MaxValue;
                SendMessage("<color=#f5a623>任命をキャンセルしました。</color>", Player.PlayerId);
                SetMode(Player, false);
                return false;
            }
        }

        return true;
    }

    public override void OnStartMeeting()
    {
        // ★ 会議開始時にタイマーを無効化
        spawnWaitTimer = -1f;
        nearTimer = 0f;
    }

    public override void AfterMeetingTasks()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        // ★ 会議終了後、3秒間は近接処理を無効にするためタイマーをゼロスタート
        spawnWaitTimer = 0f;
    }

    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!GameStates.IsInTask) return;
        if (!Player.IsAlive()) return;
        if (hasUsedAbility) return;
        if (NextAppointCandidate == byte.MaxValue) return;

        // ★ スポーン待機タイマーをカウントアップ（3秒未満は処理しない）
        if (spawnWaitTimer >= 0f && spawnWaitTimer < 3f)
        {
            spawnWaitTimer += Time.fixedDeltaTime;
            return;
        }

        if (!CanApproach) return;

        var target = GetPlayerById(NextAppointCandidate);
        if (target == null || !target.IsAlive())
        {
            nearTimer = 0f;
            NextAppointCandidate = byte.MaxValue;
            SendRPC();
            return;
        }

        float dist = Vector2.Distance(Player.GetTruePosition(), target.GetTruePosition());
        if (dist <= 1.0f)
        {
            nearTimer += Time.fixedDeltaTime;
            if (nearTimer >= 3f)
            {
                DoAppoint(target);
                NextAppointCandidate = byte.MaxValue;
                nearTimer = 0f;
            }
        }
        else
        {
            nearTimer = 0f;
        }
    }

    private void DoAppoint(PlayerControl target)
    {
        hasUsedAbility = true;

        if (target.GetCustomRole().IsImpostor())
        {
            PlayerState.GetByPlayerId(Player.PlayerId).DeathReason = CustomDeathReason.Suicide;
            Player.RpcMurderPlayer(Player);
            SendRPC();
            return;
        }

        appointedSheriff = target;

        if (!Utils.RoleSendList.Contains(target.PlayerId))
            Utils.RoleSendList.Add(target.PlayerId);

        var previousRole = target.GetCustomRole();
        target.RpcSetCustomRole(CustomRoles.Sheriff, log: null);

        Main.AllPlayerKillCooldown[target.PlayerId] = 0f;
        target.ResetKillCooldown();
        target.RpcResetAbilityCooldown();

        NameColorManager.Add(Player.PlayerId, target.PlayerId, "#f5a623");

        UtilsGameLog.AddGameLog(
            "VillageChief",
            $"{UtilsName.GetPlayerColor(Player)}({UtilsRoleText.GetRoleName(CustomRoles.VillageChief)})が" +
            $"{UtilsName.GetPlayerColor(target)}({UtilsRoleText.GetRoleName(previousRole)})をシェリフに任命した"
        );

        string msg = NotifyTarget.GetValue() >= 1
            ? $"{Player.Data.PlayerName}(村長)が{target.Data.PlayerName}をシェリフに任命しました！"
            : "";

        switch (NotifyTarget.GetValue())
        {
            case 0: break;
            case 1: SendMessage(msg); break;
            case 2: SendMessage(msg, Player.PlayerId); break;
            case 3: SendMessage(msg, target.PlayerId); break;
            case 4:
                SendMessage(msg, Player.PlayerId);
                SendMessage(msg, target.PlayerId);
                break;
        }

        SendRPC();
        UtilsNotifyRoles.NotifyRoles();
    }

    public override string GetProgressText(bool comms = false, bool GameLog = false)
    {
        if (hasUsedAbility) return "<color=#f5a623>(任命済)</color>";
        if (NextAppointCandidate != byte.MaxValue) return "<color=#f5a623>(候補選択中)</color>";
        return "<color=#f5a623>(未任命)</color>";
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null,
        bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (!Is(seer) || seer.PlayerId != seen.PlayerId || isForMeeting || !Player.IsAlive()) return "";
        if (hasUsedAbility) return "";
        if (NextAppointCandidate == byte.MaxValue)
            return $"{(isForHud ? "" : "<size=60%>")}<color=#f5a623>会議で自投票→任命候補を選択</color>";

        var candidate = GetPlayerById(NextAppointCandidate);
        string name = candidate != null ? candidate.Data.PlayerName : "???";

        if (!CanApproach)
            return $"{(isForHud ? "" : "<size=60%>")}<color=#f5a623>準備中...</color>";

        return $"{(isForHud ? "" : "<size=60%>")}<color=#f5a623>{name}に3秒近づいて任命！</color>";
    }

    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(hasUsedAbility);
        sender.Writer.Write(NextAppointCandidate);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        hasUsedAbility = reader.ReadBoolean();
        NextAppointCandidate = reader.ReadByte();
    }

    public override bool CanTask() => true;
}