/*
タスクコンプのRpcが上手く非クライアントに届かなく、
正常にタスク勝利などが行われないバグが続いてるので一旦封印。

using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using UnityEngine;

namespace TownOfHost.Roles.Crewmate;

public sealed class Efficient : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Efficient),
            player => new Efficient(player),
            CustomRoles.Efficient,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            11700,
            (7, 2),
            SetupOptionItem,
            "ef",
            "#a68b96"
        );
    public Efficient(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Task.Clear();
        Cooldown = 0f;
    }
    enum Option { EfficientCollectRect }
    static OptionItem CollectRect;
    public List<uint> Task = new();
    public override void StartGameTasks()
    {
        foreach (var task in Player.myTasks)
        {
            if (!task.IsComplete && !task.WasCollected) Task.Add(task.Id);
        }
    }
    float Cooldown;
    private static void SetupOptionItem()
    {
        CollectRect = FloatOptionItem.Create(RoleInfo, 10, Option.EfficientCollectRect, new(0, 100, 1), 15, false).SetValueFormat(OptionFormat.Percent);
        OverrideTasksData.Create(RoleInfo, 11);
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        Cooldown -= Time.fixedDeltaTime;
    }
    public override bool OnCompleteTask(uint taskid)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        if (Task.Contains(taskid)) Task.Remove(taskid);
        if (Cooldown > 0f) return true;

        int chance = IRandom.Instance.Next(1, 101);

        if (CollectRect.GetFloat() > chance)
        {
            if (Task.Where(id => id != 0).ToArray().Count() == 0) return true;
            var rand = IRandom.Instance;
            var FinTask = Task.Where(id => id != 0).ToArray()[rand.Next(0, Task.Count)];

            if (Cooldown > 0f) return true;

            Cooldown = 3;
            _ = new LateTask(() => Player.RpcCompleteTask(FinTask), 0.25f, "Efficient", true);
            Player.RpcProtectedMurderPlayer();
            Logger.Info($"{Player.name} => 効率化成功!タスクを一個減らすぞ!", "Efficient");
        }
        return true;
    }
}*/