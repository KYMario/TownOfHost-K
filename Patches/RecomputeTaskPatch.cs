using HarmonyLib;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Crewmate;

namespace TownOfHost
{
    [HarmonyPatch(typeof(GameData), nameof(GameData.RecomputeTaskCounts))]
    class CustomTaskCountsPatch
    {
        public static bool Prefix(GameData __instance)
        {
            __instance.TotalTasks = 0;
            __instance.CompletedTasks = 0;
            foreach (var p in __instance.AllPlayers)
            {
                if (p == null) continue;
                var hasTasks = UtilsTask.HasTasks(p) && PlayerState.GetByPlayerId(p.PlayerId).GetTaskState().AllTasksCount > 0;
                if (hasTasks)
                {
                    if (p.Tasks == null)
                    {
                        Logger.Warn("警告:" + p.PlayerName + "のタスクがnullです", "RecompteTaskPatch");
                        continue;//これより下を実行しない
                    }
                    foreach (var task in p.Tasks)
                    {
                        __instance.TotalTasks++;
                        if (task.Complete) __instance.CompletedTasks++;
                    }

                    if (p._object is null) continue;
                    var roleclass = p.Object.GetRoleClass();
                    if (roleclass is Walker walker)
                    {
                        __instance.TotalTasks += Walker.WalkTaskCount.GetInt();
                        __instance.CompletedTasks += walker.completeroom;
                    }
                }
            }

            return false;
        }
    }
}