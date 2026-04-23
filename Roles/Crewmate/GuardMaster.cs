using AmongUs.GameOptions;
using Hazel;
using TownOfHost.Roles.Core;
using UnityEngine;

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
            (5, 4),
            from: From.TownOfHost_K
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
        timer = 0;
    }
    private static OptionItem OptionAddGuardCount;
    private static OptionItem OptionCanSeeProtect;
    private static int AddGuardCount;
    private static bool CanSeeProtect;
    static OptionItem OptAwakening;
    static OptionItem OptAwakeningTaskcount;
    bool Awakened;
    float timer = 0;
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
        SendRPC();
        UtilsGameLog.AddGameLog($"GuardMaster", UtilsName.GetPlayerColor(Player) + ":  " + string.Format(GetString("GuardMaster.Guard"), UtilsName.GetPlayerColor(killer, true)));
        Logger.Info($"{target.GetNameWithRole().RemoveHtmlTags()} : ガード残り{Guard}回", "GuardMaster");
        UtilsNotifyRoles.NotifyRoles();
        if (timer < 5)
        {
            _ = new LateTask(() =>
            {
                if (Player.IsAlive()) Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[2]);
            }, 0.1f, "checkalive", true);
        }
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
            timer = 0;
        }
        if (IsTaskFinished && Player.IsAlive())
            Guard += AddGuardCount;
        return true;
    }
    public override string GetProgressText(bool comms = false, bool gamelog = false) => CanSeeProtect ? Utils.ColorString(Guard == 0 ? UnityEngine.Color.gray : RoleInfo.RoleColor, $"({Guard})") : "";
    public override CustomRoles Misidentify() => Awakened ? CustomRoles.NotAssigned : CustomRoles.Crewmate;
    public override void OnFixedUpdate(PlayerControl player) => timer += Time.fixedDeltaTime;

    public override void CheckWinner(GameOverReason reason)
    {
        if (Guard < OptionAddGuardCount.GetInt() && Player.IsAlive())
            Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[0]);
        if (Guard < (OptionAddGuardCount.GetInt() - 3) && Player.IsAlive())
        {
            Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[1]);
        }
    }
    public void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(Guard);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        Guard = reader.ReadInt32();
    }
    public static System.Collections.Generic.Dictionary<int, Achievement> achievements = new();
    [Attributes.PluginModuleInitializer]
    public static void Load()
    {
        var n1 = new Achievement(RoleInfo, 0, 1, 0, 0);
        var l1 = new Achievement(RoleInfo, 1, 1, 0, 1);
        var sp1 = new Achievement(RoleInfo, 2, 1, 0, 2, true);
        achievements.Add(0, n1);
        achievements.Add(1, l1);
        achievements.Add(2, sp1);
    }
}