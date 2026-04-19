using System.Linq;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Madmate;

public sealed class MadJester : RoleBase, IKillFlashSeeable, IDeathReasonSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(MadJester),
            player => new MadJester(player),
            CustomRoles.MadJester,
            () => OptionCanVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
            CustomRoleTypes.Madmate,
            8300,
            SetupOptionItem,
            "mje",
            OptionSort: (4, 1),
            introSound: () => GetIntroSound(RoleTypes.Impostor),
            from: From.au_libhalt_net
        );
    public MadJester(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.ForRecompute
    )
    {
        canSeeKillFlash = Options.MadmateCanSeeKillFlash.GetBool();
        canSeeDeathReason = Options.MadmateCanSeeDeathReason.GetBool();
    }
    private static OptionItem OptionCanVent;
    private static bool canSeeKillFlash;
    private static bool canSeeDeathReason;

    public bool? CheckKillFlash(MurderInfo info) => canSeeKillFlash;
    public bool? CheckSeeDeathReason(PlayerControl seen) => canSeeDeathReason;
    public override CustomRoles TellResults(PlayerControl player) => Options.MadTellOpt();

    public static void SetupOptionItem()
    {
        SoloWinOption.Create(RoleInfo, 9);
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 10, GeneralOption.CanVent, false, false);
        OverrideTasksData.Create(RoleInfo, 11);
    }

    public override void OnExileWrapUp(NetworkedPlayerInfo exiled, ref bool DecidedWinner)
    {
        if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;
        if (!AmongUsClient.Instance.AmHost || Player.PlayerId != exiled.PlayerId) return;
        if (!IsTaskFinished) return;
        Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[0]);
        if (PlayerCatch.AllAlivePlayerControls.Any(pc => pc.Is(CustomRoles.Impostor)) is false)
            Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[1]);

        CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Impostor, Player.PlayerId, hantrole: CustomRoles.MadJester);
        DecidedWinner = true;
    }
    public override bool OnCompleteTask(uint taskid)
    {
        if (IsTaskFinished)
        {
            Player.MarkDirtySettings();
        }

        return true;
    }
    public static System.Collections.Generic.Dictionary<int, Achievement> achievements = new();
    [Attributes.PluginModuleInitializer]
    public static void Load()
    {
        var l1 = new Achievement(RoleInfo, 0, 1, 0, 1);
        var sp1 = new Achievement(RoleInfo, 1, 1, 0, 2);
        achievements.Add(0, l1);
        achievements.Add(1, sp1);
    }
}