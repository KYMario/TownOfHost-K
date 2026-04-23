using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using AmongUs.GameOptions;
using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;

public sealed class Rabbit : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Rabbit),
            player => new Rabbit(player),
            CustomRoles.Rabbit,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            62000,
            SetupOptionItem,
            "rb",
            "#8fd0ff",
            (6, 0),
            from: From.TownOfHost_Y
        );

    public Rabbit(PlayerControl player)
    : base(RoleInfo, player, () => IsFinish(player) ? HasTask.ForRecompute : HasTask.True)
    {
        TaskTrigger = OptionTaskTrigger.GetInt();
        NumLongTasks = OptionNumLongTasks.GetInt();
        NumShortTasks = OptionNumShortTasks.GetInt();

        if (Main.NormalOptions.NumLongTasks < NumLongTasks)
            NumLongTasks = Main.NormalOptions.NumLongTasks;
        if (Main.NormalOptions.NumShortTasks < NumShortTasks)
            NumShortTasks = Main.NormalOptions.NumShortTasks;

        taskFinish = new();
        arrowPos = Vector2.zero;
        hasArrow = false;
    }

    static OptionItem OptionTaskTrigger;
    static OptionItem OptionNumLongTasks;
    static OptionItem OptionNumShortTasks;

    enum OptionName
    {
        RabbitRedistributionLongTasks,
        RabbitRedistributionShortTasks,
    }

    static int TaskTrigger;
    static int NumLongTasks;
    static int NumShortTasks;
    static List<PlayerControl> taskFinish = new();

    Vector2 arrowPos;
    bool hasArrow;

    public static bool IsFinish(PlayerControl pc) => taskFinish.Contains(pc);

    private static void SetupOptionItem()
    {
        OptionTaskTrigger = IntegerOptionItem.Create(RoleInfo, 10, GeneralOption.TaskTrigger, new(0, 20, 1), 10, false)
            .SetValueFormat(OptionFormat.Pieces);
        OptionNumLongTasks = IntegerOptionItem.Create(RoleInfo, 11, OptionName.RabbitRedistributionLongTasks, new(0, 15, 1), 1, false)
            .SetValueFormat(OptionFormat.Pieces);
        OptionNumShortTasks = IntegerOptionItem.Create(RoleInfo, 12, OptionName.RabbitRedistributionShortTasks, new(0, 15, 1), 1, false)
            .SetValueFormat(OptionFormat.Pieces);
    }

    public override void Add()
    {
        arrowPos = Vector2.zero;
        hasArrow = false;
    }

    public override bool OnCompleteTask(uint taskid)
    {
        if (!Player.IsAlive()) return true;

        if (!IsFinish(Player))
        {
            if (!(MyTaskState.CompletedTasksCount >= TaskTrigger || IsTaskFinished))
                return true;
            if (IsTaskFinished) taskFinish.Add(Player);
        }

        var impostors = PlayerCatch.AllAlivePlayerControls
            .Where(pc => pc.Is(CustomRoleTypes.Impostor)).ToArray();

        if (impostors.Length == 0) return true;

        var target = impostors[IRandom.Instance.Next(impostors.Length)];
        var pos = target.GetTruePosition();

        if (hasArrow)
            GetArrow.Remove(Player.PlayerId, arrowPos);

        arrowPos = pos;
        hasArrow = true;
        GetArrow.Add(Player.PlayerId, arrowPos);

        Logger.Info($"{Player.GetNameWithRole()} target:{target.GetNameWithRole()}", "Rabbit");

        _ = new LateTask(() =>
        {
            if (hasArrow)
            {
                GetArrow.Remove(Player.PlayerId, arrowPos);
                hasArrow = false;
            }
            UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player);
        }, 5f, "Rabbit Arrow Empty", true);

        if (IsTaskFinished)
        {
            MyTaskState.AllTasksCount += NumLongTasks + NumShortTasks;
            Player.Data.RpcSetTasks(Array.Empty<byte>());
            Player.SyncSettings();
        }

        UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player);
        return true;
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (hasArrow)
        {
            GetArrow.Remove(Player.PlayerId, arrowPos);
            hasArrow = false;
        }
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (!Is(seer) || !Is(seen) || isForMeeting) return "";
        if (!Player.IsAlive() || !hasArrow) return "";

        var arrow = GetArrow.GetArrows(seer, arrowPos);
        return arrow == "" ? "" : $"<color={RoleInfo.RoleColorCode}>{arrow}</color>";
    }
}