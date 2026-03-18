using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Modules.MeetingVoteManager;

namespace TownOfHost.Roles.Madmate;

public sealed class MadReduced : RoleBase, IKillFlashSeeable, IDeathReasonSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(MadReduced),
            player => new MadReduced(player),
            CustomRoles.MadReduced,
            () => OptionCanVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
            CustomRoleTypes.Madmate,
            20900,
            SetupOptionItem,
            "mre",
            OptionSort: (3, 1),
            introSound: () => GetIntroSound(RoleTypes.Impostor)
        );
    public MadReduced(PlayerControl player)
    : base(
        RoleInfo,
        player
        )
    {
        canSeeKillFlash = Options.MadmateCanSeeKillFlash.GetBool();
        canSeeDeathReason = Options.MadmateCanSeeDeathReason.GetBool();
        Vote = OptionVote.GetInt();
        forvote = byte.MaxValue;
    }
    private static OptionItem OptionCanVent;
    private static OptionItem OptionVote;
    private static bool canSeeKillFlash;
    private static bool canSeeDeathReason;
    private static int Vote;
    bool Skill;
    byte forvote;
    enum OptionName
    {
        MadReduecdVote
    }

    public bool? CheckKillFlash(MurderInfo info) => canSeeKillFlash;
    public bool? CheckSeeDeathReason(PlayerControl seen) => canSeeDeathReason;
    public override CustomRoles TellResults(PlayerControl player) => Options.MadTellOpt();

    public static void SetupOptionItem()
    {
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 10, GeneralOption.CanVent, false, false);
        OptionVote = IntegerOptionItem.Create(RoleInfo, 11, OptionName.MadReduecdVote, new(1, 99, 1), 1, false)
            .SetValueFormat(OptionFormat.Votes);
    }
    public override (byte? votedForId, int? numVotes, bool doVote) ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional)
    {
        // 既定値
        var (votedForId, numVotes, doVote) = base.ModifyVote(voterId, sourceVotedForId, isIntentional);

        if (Options.firstturnmeeting && Options.FirstTurnMeetingCantability.GetBool() && MeetingStates.FirstMeeting) return (votedForId, numVotes, doVote);
        if (voterId == Player.PlayerId)
        {
            if (sourceVotedForId == NoVote)
            {
                Skill = false;
                numVotes = 1;
                forvote = 255;
            }
            else if (sourceVotedForId == Skip)
            {
                Skill = false;
                numVotes = 1;
                forvote = 255;
            }
            else if (sourceVotedForId == Player.PlayerId)
            {
                Skill = false;
                numVotes = 1;
                forvote = 255;
            }
            else
            {
                Skill = true;
                numVotes = Vote * -1;
                forvote = sourceVotedForId;
            }
        }
        return (votedForId, numVotes, doVote);
    }
    public override void OnStartMeeting()
    {
        if (!Player.IsAlive())
        {
            forvote = 255;
            Skill = false;
            return;
        }
        if (Skill)
        {
            var T = PlayerCatch.GetPlayerById(forvote);
            Voteresult += string.Format(GetString("Skill.MadReduced"), UtilsName.GetPlayerColor(T, true), $"<b> {Vote}</b>");
        }
    }
}