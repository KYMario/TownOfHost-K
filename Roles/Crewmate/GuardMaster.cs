using AmongUs.GameOptions;
using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;

public sealed class GuardMaster : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(GuardMaster),
            player => new GuardMaster(player),
            CustomRoles.GuardMaster,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            10800,
            SetupOptionItem,
            "gms",
            "#8FBC8B",
            (5, 4)
        );
    public GuardMaster(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CanSeeProtect = OptionCanSeeProtect.GetBool();
        AddGuardCount = OptionAddGuardCount.GetInt();
        Guard = 0;
        Awakened = !OptAwakening.GetBool() || OptAwakeningTaskcount.GetInt() < 1;
    }
    private static OptionItem OptionAddGuardCount;
    private static OptionItem OptionCanSeeProtect;
    private static int AddGuardCount;
    private static bool CanSeeProtect;
    static OptionItem OptAwakening;
    static OptionItem OptAwakeningTaskcount;
    bool Awakened;
    int Guard = 0;
    enum OptionName
    {
        AddGuardCount,
        MadGuardianCanSeeWhoTriedToKill
    }
    private static void SetupOptionItem()
    {
        OptionCanSeeProtect = BooleanOptionItem.Create(RoleInfo, 10, OptionName.MadGuardianCanSeeWhoTriedToKill, true, false);
        OptionAddGuardCount = IntegerOptionItem.Create(RoleInfo, 11, OptionName.AddGuardCount, new(1, 99, 1), 1, false)
            .SetValueFormat(OptionFormat.Times);
        OptAwakening = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.TaskAwakening, false, false);
        OptAwakeningTaskcount = IntegerOptionItem.Create(RoleInfo, 13, GeneralOption.AwakeningTaskcount, new(1, 255, 1), 5, false, OptAwakening);
        OverrideTasksData.Create(RoleInfo, 20);
    }
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;
        // 直接キル出来る役職チェック
        if (Guard <= 0) return true; // ガードなしで普通にキル

        if (!NameColorManager.TryGetData(killer, target, out var value) || value != RoleInfo.RoleColorCode)
        {
            NameColorManager.Add(killer.PlayerId, target.PlayerId);
            if (CanSeeProtect)
                NameColorManager.Add(target.PlayerId, killer.PlayerId, RoleInfo.RoleColorCode);
        }
        killer.RpcProtectedMurderPlayer(target);
        if (CanSeeProtect && Awakened) target.RpcProtectedMurderPlayer(target);
        info.GuardPower = 1;
        Guard--;
        UtilsGameLog.AddGameLog($"GuardMaster", UtilsName.GetPlayerColor(Player) + ":  " + string.Format(GetString("GuardMaster.Guard"), UtilsName.GetPlayerColor(killer, true)));
        Logger.Info($"{target.GetNameWithRole().RemoveHtmlTags()} : ガード残り{Guard}回", "GuardMaster");
        UtilsNotifyRoles.NotifyRoles();
        return true;
    }
    public override bool OnCompleteTask(uint taskid)
    {
        if (MyTaskState.HasCompletedEnoughCountOfTasks(OptAwakeningTaskcount.GetInt()))
        {
            if (Awakened == false)
                if (!Utils.RoleSendList.Contains(Player.PlayerId))
                    Utils.RoleSendList.Add(Player.PlayerId);
            Awakened = true;
        }
        if (IsTaskFinished && Player.IsAlive())
            Guard += AddGuardCount;
        return true;
    }
    public override string GetProgressText(bool comms = false, bool gamelog = false) => CanSeeProtect ? Utils.ColorString(Guard == 0 ? UnityEngine.Color.gray : RoleInfo.RoleColor, $"({Guard})") : "";
    public override CustomRoles Misidentify() => Awakened ? CustomRoles.NotAssigned : CustomRoles.Crewmate;
}