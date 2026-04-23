using System.Collections.Generic;
using AmongUs.GameOptions;
using Hazel;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using static TownOfHost.PlayerCatch;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Crewmate;

public sealed class Nimrod : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Nimrod),
            player => new Nimrod(player),
            CustomRoles.Nimrod,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            63000,
            SetUpOptionItem,
            "nm",
            "#9fcc5b",
            from: From.SuperNewRoles
        );

    public Nimrod(PlayerControl player)
        : base(RoleInfo, player)
    {
        ExecutionMeetingPlayerId = byte.MaxValue;
    }

    static byte ExecutionMeetingPlayerId = byte.MaxValue;

    static OptionItem OptionKillImpostor;
    static bool KillImpostor;

    enum OptionName
    {
        NimrodKillImpostor,
    }

    static void SetUpOptionItem()
    {
        OptionKillImpostor = BooleanOptionItem.Create(RoleInfo, 10, OptionName.NimrodKillImpostor, false, false);
    }

    public static bool IsExecutionMeeting() => ExecutionMeetingPlayerId != byte.MaxValue;

    public override bool VotingResults(ref NetworkedPlayerInfo Exiled, ref bool IsTie, Dictionary<byte, int> vote, byte[] mostVotedPlayers, bool ClearAndExile)
    {
        if (Exiled == null) return false;
        if (Exiled.PlayerId != Player.PlayerId) return false;
        if (!Player.IsAlive()) return false;

        var exiledId = Exiled.PlayerId;

        _ = new LateTask(() =>
        {
            var exiledPlayer = GetPlayerById(exiledId);
            if (exiledPlayer == null || !exiledPlayer.IsAlive()) return;

            ExecutionMeetingPlayerId = exiledId;
            Logger.Info($"{exiledPlayer.GetNameWithRole()} : ニムロッド会議開始", "Nimrod");
            UtilsNotifyRoles.NotifyRoles(ForceLoop: true);
            exiledPlayer.ReportDeadBody(exiledPlayer.Data);
            SendRPC();
        }, 14.5f, "NimrodExiled", true);

        // ★ 追放をキャンセル
        Exiled = null;
        return true;
    }

    public override (byte? votedForId, int? numVotes, bool doVote) ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional)
    {
        var baseVote = base.ModifyVote(voterId, sourceVotedForId, isIntentional);
        if (ExecutionMeetingPlayerId != Player.PlayerId || voterId != Player.PlayerId)
            return baseVote;

        if (sourceVotedForId < 15)
        {
            var target = GetPlayerById(sourceVotedForId);
            if (target != null)
            {
                if (!KillImpostor && target.GetCustomRole().IsImpostor() && !SuddenDeathMode.NowSuddenDeathMode)
                    return baseVote;

                target.SetRealKiller(Player);
                PlayerState.GetByPlayerId(sourceVotedForId).DeathReason = CustomDeathReason.Execution;
                Logger.Info($"{Player.GetNameWithRole()} : ニムロッド追放→{target.GetNameWithRole()}", "Nimrod");
            }
        }

        MeetingVoteManager.Instance.ClearAndExile(Player.PlayerId, sourceVotedForId);
        return (baseVote.votedForId, baseVote.numVotes, false);
    }

    public override void OnStartMeeting()
    {
        if (!IsExecutionMeeting()) return;
        Utils.SendMessage(
            GetString("IsNimrodMeetingText"),
            title: $"<color={RoleInfo.RoleColorCode}>{GetString("IsNimrodMeetingTitle")}</color>"
        );
    }

    public override void AfterMeetingTasks()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!IsExecutionMeeting()) return;

        MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.Vote, ExecutionMeetingPlayerId);
        ExecutionMeetingPlayerId = byte.MaxValue;
        SendRPC();
    }

    void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(ExecutionMeetingPlayerId);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        ExecutionMeetingPlayerId = reader.ReadByte();
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null,
        bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (!Is(seer) || seer.PlayerId != seen.PlayerId || !Player.IsAlive()) return "";
        if (IsExecutionMeeting() && ExecutionMeetingPlayerId == Player.PlayerId)
            return $"{(isForHud ? "" : "<size=60%>")}<color={RoleInfo.RoleColorCode}>道連れ会議中！誰かに投票して道連れにしよう</color>";
        return "";
    }
}