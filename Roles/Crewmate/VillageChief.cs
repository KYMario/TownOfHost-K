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
            () => RoleTypes.Engineer,
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
        spawnWaitTimer = -1f;
        appointCooldownTimer = 0f;
        NextAppointCandidate = byte.MaxValue;
        appointedSheriff = null;
    }

    private bool hasUsedAbility;
    private float nearTimer;

    private float spawnWaitTimer;
    private bool CanApproach => spawnWaitTimer >= 3f;

    private float appointCooldownTimer;

    public byte NextAppointCandidate;
    public PlayerControl appointedSheriff = null;

    private static OptionItem NotifyTarget;
    private static OptionItem OptionAppointCooldown;

    private static readonly string[] NotifyTargetOptions =
        ["None", "Everyone", "VillageChiefOnly", "SheriffOnly", "VillageChiefAndSheriff"];

    private static void SetupOptionItem()
    {
        NotifyTarget = StringOptionItem.Create(
            RoleInfo, 12, "VillageChiefNotifyTarget",
            NotifyTargetOptions, 0, false
        );

        OptionAppointCooldown = FloatOptionItem.Create(
            RoleInfo, 13, "AppointCooldown",
            new(0f, 120f, 5f), 30f, false
        ).SetValueFormat(OptionFormat.Seconds);
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(false);
        AURoleOptions.EngineerCooldown = OptionAppointCooldown.GetFloat();
    }

    public override bool CanClickUseVentButton => false;
    public override bool OnEnterVent(PlayerPhysics physics, int ventId) => false;

    public override string GetAbilityButtonText()
    {
        if (hasUsedAbility) return "<color=#888888>任命済</color>";
        if (NextAppointCandidate == byte.MaxValue) return "候補未設定";
        return "任命";
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
                    "次のターン、この相手に近づいて任命します。",
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
        spawnWaitTimer = -1f;
        nearTimer = 0f;
    }

    public override void AfterMeetingTasks()
    {
        spawnWaitTimer = 0f;
        appointCooldownTimer = OptionAppointCooldown.GetFloat();

        if (Player.IsAlive())
        {
            Player.RpcResetAbilityCooldown();
        }
    }

    public override void OnFixedUpdate(PlayerControl player)
    {
        if (GameStates.IsInTask && Player.IsAlive() && !hasUsedAbility)
        {
            if (spawnWaitTimer >= 0f && spawnWaitTimer < 3f)
            {
                spawnWaitTimer += Time.fixedDeltaTime;
            }

            if (appointCooldownTimer > 0f)
            {
                appointCooldownTimer -= Time.fixedDeltaTime;
            }
        }

        bool isHost = AmongUsClient.Instance.AmHost;
        bool isMe = Is(PlayerControl.LocalPlayer);

        if ((isHost || isMe) && GameStates.IsInTask && Player.IsAlive() && !hasUsedAbility && NextAppointCandidate != byte.MaxValue && CanApproach)
        {
            var target = GetPlayerById(NextAppointCandidate);
            if (target != null && target.IsAlive())
            {
                float dist = Vector2.Distance(Player.GetTruePosition(), target.GetTruePosition());
                if (dist <= 1.0f)
                {
                    nearTimer += Time.fixedDeltaTime;

                    if (nearTimer >= 3f)
                    {
                        nearTimer = 3f;

                        if (isHost && appointCooldownTimer <= 0f)
                        {
                            DoAppoint(target);
                            NextAppointCandidate = byte.MaxValue;
                            nearTimer = 0f;
                        }
                    }
                }
                else
                {
                    nearTimer = 0f;
                }
            }
            else
            {
                nearTimer = 0f;
                if (isHost)
                {
                    NextAppointCandidate = byte.MaxValue;
                    SyncStateRpc();
                }
            }
        }
    }

    private void DoAppoint(PlayerControl target)
    {
        hasUsedAbility = true;

        if (target.GetCustomRole().IsImpostor())
        {
            PlayerState.GetByPlayerId(Player.PlayerId).DeathReason = CustomDeathReason.Suicide;
            Player.RpcMurderPlayer(Player);
            SyncStateRpc();
            return;
        }

        appointedSheriff = target;

        if (!Utils.RoleSendList.Contains(target.PlayerId))
            Utils.RoleSendList.Add(target.PlayerId);

        var previousRole = target.GetCustomRole();
        target.RpcSetCustomRole(CustomRoles.Sheriff, log: null);

        target.ResetKillCooldown();
        target.SetKillCooldown();
        target.RpcResetAbilityCooldown();

        NameColorManager.Add(Player.PlayerId, target.PlayerId, "#f5a623");

        UtilsGameLog.AddGameLog(
            "VillageChief",
            $"{UtilsName.GetPlayerColor(Player)}({UtilsRoleText.GetRoleName(CustomRoles.VillageChief)})が" +
            $"{UtilsName.GetPlayerColor(target)}({UtilsRoleText.GetRoleName(previousRole)})をシェリフに任命した"
        );

        SyncStateRpc();
        UtilsNotifyRoles.NotifyRoles();
    }

    public override string GetProgressText(bool comms = false, bool GameLog = false)
    {
        if (hasUsedAbility) return "<color=#f5a623>(任命済)</color>";
        if (NextAppointCandidate != byte.MaxValue) return "<color=#f5a623>(候補選択中)</color>";
        return "<color=#808080>(未任命)</color>";
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null,
        bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (!Is(seer) || seer.PlayerId != seen.PlayerId || isForMeeting || !Player.IsAlive()) return "";
        if (hasUsedAbility) return "";

        string prefix = isForHud ? "" : "<size=60%>";

        if (NextAppointCandidate == byte.MaxValue)
            return $"{prefix}<color=#f5a623>会議で自投票→任命候補を選択</color>";

        var candidate = GetPlayerById(NextAppointCandidate);
        string name = candidate != null ? candidate.Data.PlayerName : "???";

        if (appointCooldownTimer > 0f)
        {
            return $"{prefix}<color=#f5a623>クールタイム明け待機中</color>";
        }

        return $"{prefix}<color=#f5a623>{name}に3秒近づいて任命！</color>";
    }

    private void SyncStateRpc()
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