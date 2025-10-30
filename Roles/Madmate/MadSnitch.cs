using System.Linq;

using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Madmate;

public sealed class MadSnitch : RoleBase, IKillFlashSeeable, IDeathReasonSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(MadSnitch),
            player => new MadSnitch(player),
            CustomRoles.MadSnitch,
            () => OptionCanVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
            CustomRoleTypes.Madmate,
            7500,
            SetupOptionItem,
            "msn",
            OptionSort: (1, 1),
            introSound: () => GetIntroSound(RoleTypes.Impostor),
            from: From.TownOfHost
        );
    public MadSnitch(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.ForRecompute)
    {
        canSeeKillFlash = Options.MadmateCanSeeKillFlash.GetBool();
        canSeeDeathReason = Options.MadmateCanSeeDeathReason.GetBool();

        canAlsoBeExposedToImpostor = OptionCanAlsoBeExposedToImpostor.GetBool();
        TaskTrigger = OptionTaskTrigger.GetInt();

        MyTaskState.NeedTaskCount = OptionTaskTrigger.GetInt();
        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
    }

    private static OptionItem OptionCanVent;
    private static OptionItem OptionCanAlsoBeExposedToImpostor;
    /// <summary>能力発動タスク数</summary>
    private static OptionItem OptionTaskTrigger;
    private static OverrideTasksData Tasks;
    enum OptionName
    {
        MadSnitchCanAlsoBeExposedToImpostor
    }

    private static bool canSeeKillFlash;
    private static bool canSeeDeathReason;
    private static bool canAlsoBeExposedToImpostor;
    private static int TaskTrigger;

    public static void SetupOptionItem()
    {
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 10, GeneralOption.CanVent, false, false);
        OptionCanAlsoBeExposedToImpostor = BooleanOptionItem.Create(RoleInfo, 11, OptionName.MadSnitchCanAlsoBeExposedToImpostor, false, false);
        OptionTaskTrigger = IntegerOptionItem.Create(RoleInfo, 12, GeneralOption.TaskTrigger, new(0, 99, 1), 1, false).SetValueFormat(OptionFormat.Pieces);
        Tasks = OverrideTasksData.Create(RoleInfo, 20);
        RoleAddAddons.Create(RoleInfo, 25);
    }

    private bool KnowsImpostor()
    {
        return MyTaskState.HasCompletedEnoughCountOfTasks(TaskTrigger);
    }
    private void CheckAndAddNameColorToImpostors()
    {
        if (!KnowsImpostor()) return;

        foreach (var impostor in PlayerCatch.AllPlayerControls.Where(player => player.Is(CustomRoleTypes.Impostor) || player.Is(CustomRoles.WolfBoy)))
        {
            NameColorManager.Add(Player.PlayerId, impostor.PlayerId, Player.GetRoleColorCode());
        }
    }

    public override void Add()
    {
        CheckAndAddNameColorToImpostors();
    }
    public override bool OnCompleteTask(uint taskid)
    {
        CheckAndAddNameColorToImpostors();
        return true;
    }
    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (
            // オプションが無効
            !canAlsoBeExposedToImpostor ||
            // インポスター→MadSnitchではない
            !seer.Is(CustomRoleTypes.Impostor) ||
            //  狼少年の場合は除く
            seer.Is(CustomRoles.WolfBoy) ||

            seen.GetRoleClass() is not MadSnitch madSnitch ||
            // マッドスニッチがまだインポスターを知らない
            !madSnitch.KnowsImpostor())
        {
            return string.Empty;
        }

        return Utils.ColorString(UtilsRoleText.GetRoleColor(CustomRoles.MadSnitch), "★");
    }

    public bool? CheckKillFlash(MurderInfo info) => canSeeKillFlash;
    public bool? CheckSeeDeathReason(PlayerControl seen) => canSeeDeathReason;
    public override CustomRoles TellResults(PlayerControl player) => Options.MadTellOpt();
}
