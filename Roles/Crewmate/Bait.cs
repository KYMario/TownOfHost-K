using System.Collections.Generic;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;

public sealed class Bait : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Bait),
            player => new Bait(player),
            CustomRoles.Bait,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            10400,
            SetupOptionItem,
            "ba",
            "#00f7ff",
            (5, 1),
            from: From.TheOtherRoles
        );
    public Bait(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Awakened = !OptAwakening.GetBool();
        killerid = byte.MaxValue;
    }
    enum OptionName
    {
        BaitReportDelay, BaitMaxDelay
    }
    static OptionItem OptAwakening;
    static OptionItem OptAwakeningTaskcount;
    public static OptionItem OptCanUseActiveComms;
    static OptionItem OptReportDelay;
    static OptionItem OptMaxDelay;
    bool Awakened;
    byte killerid;
    private static void SetupOptionItem()
    {
        OptCanUseActiveComms = BooleanOptionItem.Create(RoleInfo, 9, GeneralOption.CanUseActiveComms, true, false);
        OptReportDelay = FloatOptionItem.Create(RoleInfo, 12, OptionName.BaitReportDelay, new(0f, 180f, 0.5f), 3f, false).SetValueFormat(OptionFormat.Seconds);
        OptMaxDelay = FloatOptionItem.Create(RoleInfo, 13, OptionName.BaitMaxDelay, new(0f, 180f, 0.5f), 3f, false).SetValueFormat(OptionFormat.Seconds);
        OptAwakening = BooleanOptionItem.Create(RoleInfo, 10, GeneralOption.TaskAwakening, false, false);
        OptAwakeningTaskcount = IntegerOptionItem.Create(RoleInfo, 11, GeneralOption.AwakeningTaskcount, new(1, 255, 1), 5, false, OptAwakening);
    }
    public override void OnMurderPlayerAsTarget(MurderInfo info)
    {
        if (!Awakened) return;
        var tien = 0f;
        //小数対応
        if (OptMaxDelay.GetFloat() > 0)
        {
            int ti = IRandom.Instance.Next(0, (int)OptMaxDelay.GetFloat() * 10);
            tien = ti * 0.1f;
            Logger.Info($"{tien}sの追加遅延発生!!", "Bait");
        }
        var (killer, target) = info.AttemptTuple;
        killerid = killer.PlayerId;
        if (target.Is(CustomRoles.Bait) && !info.IsSuicide && !info.IsFakeSuicide && (OptCanUseActiveComms.GetBool() || !Utils.IsActive(SystemTypes.Comms)))
            _ = new LateTask(() => ReportDeadBodyPatch.ExReportDeadBody(killer, target.Data), 0.15f + OptReportDelay.GetFloat() + tien, "Bait Self Report");
    }
    public override CustomRoles Misidentify() => Awakened ? CustomRoles.NotAssigned : CustomRoles.Crewmate;
    public override bool OnCompleteTask(uint taskid)
    {
        if (MyTaskState.HasCompletedEnoughCountOfTasks(OptAwakeningTaskcount.GetInt()))
        {
            if (Awakened == false)
                if (!Utils.RoleSendList.Contains(Player.PlayerId))
                    Utils.RoleSendList.Add(Player.PlayerId);
            Awakened = true;
        }
        return true;
    }
    public override void OnExileWrapUp(NetworkedPlayerInfo exiled, ref bool DecidedWinner)
    {
        if ((exiled?.PlayerId ?? byte.MaxValue) == killerid && killerid is not byte.MaxValue) Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[0]);
        killerid = byte.MaxValue;
    }
    public static Dictionary<int, Achievement> achievements = new();
    [Attributes.PluginModuleInitializer]
    public static void Load()
    {
        var l1 = new Achievement(RoleInfo, 0, 1, 0, 1);
        achievements.Add(0, l1);
    }
}