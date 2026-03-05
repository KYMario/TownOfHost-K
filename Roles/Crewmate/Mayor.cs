using System.Linq;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;

public sealed class Mayor : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Mayor),
            player => new Mayor(player),
            CustomRoles.Mayor,
            () => OptionHasPortableButton.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            9500,
            SetupOptionItem,
            "my",
            "#204d42",
            (3, 3),
            introSound: () => GetIntroSound(RoleTypes.Crewmate),
            from: From.TownOfUs,
            Desc: () =>
            {
                var info = "";
                var portable = "";
                if (OptionAwakening.GetBool()) info = string.Format(GetString("MayorDescInfo"), OptionAwakeningCount.GetInt(), OptionKadditionaVote.GetInt() + OptionAdditionalVote.GetInt() + 1);
                if (OptionHasPortableButton.GetBool()) portable = GetString("MayorPortable");

                return string.Format(GetString("MayorDesc"), OptionAdditionalVote.GetInt() + 1, info, portable);
            }
        );
    public Mayor(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        AdditionalVote = OptionAdditionalVote.GetInt();
        HasPortableButton = OptionHasPortableButton.GetBool();
        NumOfUseButton = OptionNumOfUseButton.GetInt();
        Awakening = OptionAwakening.GetBool();
        AwakeningCount = OptionAwakeningCount.GetInt();
        KadditionaVote = OptionKadditionaVote.GetInt();

        votefor = byte.MaxValue;
        sp1flug = 0;
        LeftButtonCount = NumOfUseButton;
    }

    private static OptionItem OptionAdditionalVote;
    private static OptionItem OptionAwakening;
    private static OptionItem OptionAwakeningCount;
    private static OptionItem OptionKadditionaVote;
    private static OptionItem OptionHasPortableButton;
    private static OptionItem OptionNumOfUseButton;
    enum OptionName
    {
        MayorAdditionalVote,
        MayorHasPortableButton,
        MayorAwakening,
        MayorAwakeningPlayerCount,
        MayorNumOfUseButton,
    }
    public static int AdditionalVote;
    public static bool HasPortableButton;
    public static bool Awakening;
    public static int AwakeningCount;
    public static int KadditionaVote;
    public static int NumOfUseButton;

    public int LeftButtonCount;
    byte votefor;
    int sp1flug;
    private static void SetupOptionItem()
    {
        OptionAdditionalVote = IntegerOptionItem.Create(RoleInfo, 10, OptionName.MayorAdditionalVote, new(0, 99, 1), 1, false)
            .SetValueFormat(OptionFormat.Votes);
        OptionAwakening = BooleanOptionItem.Create(RoleInfo, 11, OptionName.MayorAwakening, false, false);
        OptionAwakeningCount = IntegerOptionItem.Create(RoleInfo, 12, OptionName.MayorAwakeningPlayerCount, new(1, 15, 1), 6, false, OptionAwakening)
            .SetValueFormat(OptionFormat.Players);
        OptionKadditionaVote = IntegerOptionItem.Create(RoleInfo, 13, OptionName.MayorAdditionalVote, new(1, 99, 1), 1, false, OptionAwakening)
            .SetValueFormat(OptionFormat.Votes);
        OptionHasPortableButton = BooleanOptionItem.Create(RoleInfo, 14, OptionName.MayorHasPortableButton, false, false);
        OptionNumOfUseButton = IntegerOptionItem.Create(RoleInfo, 15, OptionName.MayorNumOfUseButton, new(1, 99, 1), 1, false, OptionHasPortableButton)
            .SetValueFormat(OptionFormat.Times);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown =
            LeftButtonCount <= 0
            ? 255f
            : opt.GetInt(Int32OptionNames.EmergencyCooldown);
        AURoleOptions.EngineerInVentMaxTime = 1;
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;
        if (Is(reporter) && target == null) //ボタン
            LeftButtonCount--;
    }
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (LeftButtonCount > 0)
        {
            var user = physics.myPlayer;
            //ホスト視点、vent処理中に会議を呼ぶとベントの矢印が残るので遅延させる
            _ = new LateTask(() => ReportDeadBodyPatch.ExReportDeadBody(Player, null), 0.1f, "MayerPortableButton");
            //ポータブルボタン時はベントから追い出す必要はない
            return true;
        }
        return false;
    }
    public override (byte? votedForId, int? numVotes, bool doVote) ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional)
    {
        // 既定値
        var (votedForId, numVotes, doVote) = base.ModifyVote(voterId, sourceVotedForId, isIntentional);

        if (Options.firstturnmeeting && Options.FirstTurnMeetingCantability.GetBool() && MeetingStates.FirstMeeting) return (votedForId, numVotes, doVote);
        if (voterId == Player.PlayerId && PlayerCatch.AllAlivePlayersCount <= AwakeningCount && Awakening)
        {
            numVotes = AdditionalVote + KadditionaVote + 1;
            votefor = votedForId.HasValue ? votedForId.Value : byte.MaxValue;
            if (sp1flug == 0 && PlayerCatch.AllAlivePlayerControls.Count(pc => !pc.GetCustomRole().IsCrewmate()) > PlayerCatch.AllAlivePlayerControls.Count(pc => pc.GetCustomRole().IsCrewmate()))
            {
                sp1flug = 1;
            }
        }
        else if (voterId == Player.PlayerId)
        {
            numVotes = AdditionalVote + 1;
            votefor = votedForId.HasValue ? votedForId.Value : byte.MaxValue;
            if (sp1flug == 0 && PlayerCatch.AllAlivePlayerControls.Count(pc => !pc.GetCustomRole().IsCrewmate()) > PlayerCatch.AllAlivePlayerControls.Count(pc => pc.GetCustomRole().IsCrewmate()))
            {
                sp1flug = 1;
            }
        }
        return (votedForId, numVotes, doVote);
    }
    public override void OnExileWrapUp(NetworkedPlayerInfo exiled, ref bool DecidedWinner)
    {
        if (exiled is null || exiled?.PlayerId == byte.MaxValue)
        {
            sp1flug = 0;
            return;
        }
        if (votefor == exiled.PlayerId)
        {
            Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[0]);
            if (sp1flug == 1 && exiled.Object.GetCustomRole().IsCrewmate() is false)
            {
                sp1flug = 2;
            }
        }
    }
    public override void CheckWinner(GameOverReason reason)
    {
        if (sp1flug == 2 && CustomWinnerHolder.winners.Contains(CustomWinner.Crewmate))
        {
            Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[2]);
        }
    }
    public override string GetAbilityButtonText()
    {
        if (!HasPortableButton) return base.GetAbilityButtonText();
        return GetString("MayorAbilitytext");
    }
    public override bool OverrideAbilityButton(out string text)
    {
        text = "Mayor_Ability";
        return HasPortableButton;
    }
    public static System.Collections.Generic.Dictionary<int, Achievement> achievements = new();
    [Attributes.PluginModuleInitializer]
    public static void Load()
    {
        var n1 = new Achievement(RoleInfo, 0, 1, 0, 0);
        var sp1 = new Achievement(RoleInfo, 1, 1, 0, 2, true);
        achievements.Add(0, n1);
        achievements.Add(1, sp1);
    }
}