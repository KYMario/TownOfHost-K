using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.Madmate;

namespace TownOfHost.Roles.Impostor;

public sealed class Driver : RoleBase, IImpostor, IKillFlashSeeable, IDeathReasonSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Driver),
            player => new Driver(player),
            CustomRoles.Driver,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            16000,
            SetupOptionItems,
            "dr",
            OptionSort: (0, 0),
            tab: TabGroup.Combinations,
            assignInfo: new RoleAssignInfo(CustomRoles.Driver, CustomRoleTypes.Impostor)
            {
                AssignCountRule = new(1, 1, 1),
                AssignUnitRoles = new CustomRoles[2] { CustomRoles.Driver, CustomRoles.Braid }
            },
            combination: CombinationRoles.DriverandBraid
        );
    public Driver(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCooldown = OptionKillCooldown.GetFloat();
    }
    public static OptionItem OptionKillCooldown;
    public static OptionItem OptionBraidKillCooldown;
    public static OptionItem OptionGiveKillFlash;
    public static OptionItem OptionGiveKillFlashtaskTrigger;
    public static OptionItem OptionGiveKnowDeathreason;
    public static OptionItem OptionGiveKnowDeathreasontaskTrigger;
    public static OptionItem OptionGiveGuard;
    public static OptionItem OptionGiveGuardtaskTrigger;
    public static OptionItem OptionGiveWatchVotes;
    public static OptionItem OptionGiveWatchVotestaskTrigger;
    public static OptionItem OptionBraidCanSeeDriver;
    public static OptionItem OptionDriverCanSeeBraid;
    public static OptionItem OptionBraidCanUseVent;
    enum OptionName
    {
        KillCooldown,
        BraidKillCooldown,
        GiveKillFlash,
        GiveKnowDeathreason,
        GiveGuard,
        GiveWatchVotes,
        BraidCanSeeDriver, DriverTaskTrigger,
        BraidCanVent,
        DriverCanSeeBraid
    }
    public static float BraidKillCooldown;
    public static float KillCooldown;
    public static bool HasGuard;
    public static void SetupOptionItems()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 9, OptionName.KillCooldown, new(0f, 180f, 0.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionBraidKillCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.BraidKillCooldown, new(0f, 180f, 0.5f), 15f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionGiveKillFlash = BooleanOptionItem.Create(RoleInfo, 11, OptionName.GiveKillFlash, false, false);
        OptionGiveKillFlashtaskTrigger = IntegerOptionItem.Create(RoleInfo, 12, OptionName.DriverTaskTrigger, new(1, 297, 1), 5, false, OptionGiveKillFlash);
        OptionGiveKnowDeathreason = BooleanOptionItem.Create(RoleInfo, 13, OptionName.GiveKnowDeathreason, false, false);
        OptionGiveKnowDeathreasontaskTrigger = IntegerOptionItem.Create(RoleInfo, 14, OptionName.DriverTaskTrigger, new(1, 297, 1), 5, false, OptionGiveKnowDeathreason);
        OptionGiveWatchVotes = BooleanOptionItem.Create(RoleInfo, 15, OptionName.GiveWatchVotes, false, false);
        OptionGiveWatchVotestaskTrigger = IntegerOptionItem.Create(RoleInfo, 16, OptionName.DriverTaskTrigger, new(1, 297, 1), 5, false, OptionGiveWatchVotes);
        OptionGiveGuard = BooleanOptionItem.Create(RoleInfo, 17, OptionName.GiveGuard, false, false);
        OptionGiveGuardtaskTrigger = IntegerOptionItem.Create(RoleInfo, 18, OptionName.DriverTaskTrigger, new(1, 297, 1), 5, false, OptionGiveGuard);
        OptionBraidCanSeeDriver = BooleanOptionItem.Create(RoleInfo, 19, OptionName.BraidCanSeeDriver, false, false);
        OptionDriverCanSeeBraid = BooleanOptionItem.Create(RoleInfo, 21, OptionName.DriverCanSeeBraid, false, false);
        OptionBraidCanUseVent = BooleanOptionItem.Create(RoleInfo, 22, OptionName.BraidCanVent, true, false);
        Braid.Tasks = OverrideTasksData.Create(RoleInfo, 50, CustomRoles.Braid);
    }

    public bool? CheckKillFlash(MurderInfo info) => Braid.DriverseeKillFlash;
    public bool? CheckSeeDeathReason(PlayerControl seen) => Braid.Driverseedeathreason;
    public override void ApplyGameOptions(IGameOptions opt)
    {
        //匿名投票
        opt.SetBool(BoolOptionNames.AnonymousVotes, !Braid.DriverseeVote);
    }
    public float CalculateKillCooldown() => Braid.TaskFin ? BraidKillCooldown : KillCooldown;
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        if (Braid.GivedGuard && HasGuard)
        {
            HasGuard = false;
            (var killer, var target) = info.AttemptTuple;
            // 直接キル出来る役職チェック

            if (Player.IsAlive())
                if (!NameColorManager.TryGetData(killer, target, out var value) || value != RoleInfo.RoleColorCode)
                {
                    NameColorManager.Add(killer.PlayerId, target.PlayerId);
                }
            killer.RpcProtectedMurderPlayer(target);
            target.RpcProtectedMurderPlayer(target);
            info.GuardPower = 1;
            UtilsGameLog.AddGameLog($"Driver", UtilsName.GetPlayerColor(Player) + ":  " + string.Format(GetString("GuardMaster.Guard"), UtilsName.GetPlayerColor(killer, true)));
            UtilsNotifyRoles.NotifyRoles();
        }
        return true;
    }
}