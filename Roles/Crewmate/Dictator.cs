using AmongUs.GameOptions;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Modules.SelfVoteManager;

namespace TownOfHost.Roles.Crewmate;

public sealed class Dictator : RoleBase, ISelfVoter
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Dictator),
            player => new Dictator(player),
            CustomRoles.Dictator,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            9800,
            SetupOptionItem,
            "dic",
            "#df9b00",
            (3, 6),
            from: From.TownOfHost
        );
    public Dictator(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
    enum OptionName
    {
        DictatorSelfVote
    }
    static void SetupOptionItem()
    {
        OptionSelfVote = BooleanOptionItem.Create(RoleInfo, 10, OptionName.DictatorSelfVote, false, false);
    }
    static OptionItem OptionSelfVote;
    bool ISelfVoter.CanUseVoted() => false;
    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (!Canuseability()) return true;

        if (Is(voter))
        {
            if (!OptionSelfVote.GetBool()) return true;
            if (CheckSelfVoteMode(Player, votedForId, out var status))
            {
                if (status is VoteStatus.Self)
                    Utils.SendMessage(string.Format(GetString("SkillMode"), GetString("Mode.Dictator"), GetString("Vote.Dictator")) + GetString("VoteSkillMode"), Player.PlayerId);
                if (status is VoteStatus.Skip)
                    Utils.SendMessage(GetString("VoteSkillFin"), Player.PlayerId);
                if (status is VoteStatus.Vote)
                {
                    MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.Suicide, Player.PlayerId);
                    PlayerCatch.GetPlayerById(votedForId).SetRealKiller(Player);
                    MeetingVoteManager.Instance.ClearAndExile(Player.PlayerId, votedForId);
                    UtilsGameLog.AddGameLog($"Dictator", string.Format(GetString("Dictator.log"), UtilsName.GetPlayerColor(Player)));
                }
                SetMode(Player, status is VoteStatus.Self);
                return status is VoteStatus.Vote;
            }
        }

        return true;
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (isForMeeting && Player.IsAlive() && seer.PlayerId == seen.PlayerId && Canuseability())
        {
            var mes = $"<color={RoleInfo.RoleColorCode}>{(OptionSelfVote.GetBool() ? GetString("SelfVoteRoleInfoMeg") : GetString("NomalVoteRoleInfoMeg"))}</color>";
            return isForHud ? mes : $"<size=40%>{mes}</size>";
        }
        return "";
    }
    public override (byte? votedForId, int? numVotes, bool doVote) ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional)
    {
        var (votedForId, numVotes, doVote) = base.ModifyVote(voterId, sourceVotedForId, isIntentional);
        var baseVote = (votedForId, numVotes, doVote);
        if (!isIntentional || !Canuseability() || OptionSelfVote.GetBool() || voterId != Player.PlayerId || sourceVotedForId == Player.PlayerId || sourceVotedForId >= 253 || !Player.IsAlive())
        {
            return baseVote;
        }
        if (!OptionSelfVote.GetBool())
        {
            MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.Suicide, Player.PlayerId);
            PlayerCatch.GetPlayerById(sourceVotedForId).SetRealKiller(Player);
            MeetingVoteManager.Instance.ClearAndExile(Player.PlayerId, sourceVotedForId);
            UtilsGameLog.AddGameLog($"Dictator", string.Format(GetString("Dictator.log"), UtilsName.GetPlayerColor(Player)));
        }
        return (votedForId, numVotes, false);
    }
}
