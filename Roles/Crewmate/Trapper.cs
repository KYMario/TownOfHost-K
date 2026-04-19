using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using UnityEngine;

namespace TownOfHost.Roles.Crewmate;

public sealed class Trapper : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Trapper),
            player => new Trapper(player),
            CustomRoles.Trapper,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            21300,
            SetupOptionItem,
            "tra",
            "#5a8fd0",
            (5, 5),
            from: From.TownOfHost
        );
    public Trapper(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        BlockMoveTime = OptionBlockMoveTime.GetFloat();
        Awakened = !Awakening.GetBool() || AwakeningTask.GetInt() < 1;
        IsTraping = false;
    }
    static OptionItem Awakening;
    static OptionItem AwakeningTask;
    bool Awakened;
    private static OptionItem OptionBlockMoveTime;
    enum OptionName
    {
        TrapperBlockMoveTime
    }

    private static float BlockMoveTime;
    bool IsTraping;

    private static void SetupOptionItem()
    {
        OptionBlockMoveTime = FloatOptionItem.Create(RoleInfo, 10, OptionName.TrapperBlockMoveTime, new(1f, 180f, 1f), 5f, false)
            .SetValueFormat(OptionFormat.Seconds);
        Awakening = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.TaskAwakening, false, false);
        AwakeningTask = IntegerOptionItem.Create(RoleInfo, 12, GeneralOption.AwakeningTaskcount, new(1, 255, 1), 5, false, Awakening);
    }
    public override void OnMurderPlayerAsTarget(MurderInfo info)
    {
        if (!Awakened) return;
        if (info.IsSuicide) return;

        var killer = info.AttemptKiller;
        var tmpSpeed = Main.AllPlayerSpeed[killer.PlayerId];
        Main.AllPlayerSpeed[killer.PlayerId] = Main.MinSpeed;
        ReportDeadBodyPatch.CanReport[killer.PlayerId] = false;
        killer.MarkDirtySettings();
        IsTraping = true;
        Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[0]);
        _ = new LateTask(() =>
        {
            IsTraping = false;
            Main.AllPlayerSpeed[killer.PlayerId] = tmpSpeed;
            ReportDeadBodyPatch.CanReport[killer.PlayerId] = true;
            killer.MarkDirtySettings();
            RPC.PlaySoundRPC(killer.PlayerId, Sounds.TaskComplete);
        }, BlockMoveTime, "Trapper BlockMove");
    }
    public override CustomRoles Misidentify() => Awakened ? CustomRoles.NotAssigned : CustomRoles.Crewmate;
    public override bool OnCompleteTask(uint taskid)
    {
        if (MyTaskState.HasCompletedEnoughCountOfTasks(AwakeningTask.GetInt()))
        {
            if (Awakened == false)
                if (!Utils.RoleSendList.Contains(Player.PlayerId))
                    Utils.RoleSendList.Add(Player.PlayerId);
            Awakened = true;
        }
        return true;
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (IsTraping && (target?.PlayerId ?? byte.MaxValue) == Player.PlayerId)
            Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[1]);
    }
    public static System.Collections.Generic.Dictionary<int, Achievement> achievements = new();
    [Attributes.PluginModuleInitializer]
    public static void Load()
    {
        var n1 = new Achievement(RoleInfo, 0, 1, 0, 0);
        var l1 = new Achievement(RoleInfo, 1, 1, 0, 1);
        achievements.Add(0, n1);
        achievements.Add(1, l1);
    }
}