using AmongUs.GameOptions;
using HarmonyLib;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.AddOns.Crewmate;
using TownOfHost.Roles.AddOns.Common;

namespace TownOfHost
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CompleteTask))]
    class PlayerControlCompleteTaskPatch
    {
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] uint taskid)
        {
            var pc = __instance;

            Logger.Info($"TaskComplete:{pc.GetNameWithRole().RemoveHtmlTags()} {taskid}", "CompleteTask");
            var taskState = pc.GetPlayerTaskState();
            taskState.Update(pc);

            var roleClass = pc.GetRoleClass();
            var roleinfo = pc.GetCustomRole().GetRoleInfo();
            var ret = true;

            //タスクバトルの処理　タスバトならこれ以外の処理いらない。
            if (Options.CurrentGameMode == CustomGameMode.TaskBattle)
            {
                ret &= TaskBattle.TaskBattleCompleteTask(pc, taskState);
                return ret;
            }

            if (roleClass != null)
            {
                if (Amnesia.CheckAbility(pc))
                    ret = roleClass.OnCompleteTask(taskid);
            }

            CustomRoleManager.OnTaskCompleteOthers(__instance, ret);

            if (pc.Is(CustomRoles.Amnesia))
                if (Amnesia.OptionCanRealizeTask.GetBool() && taskState.HasCompletedEnoughCountOfTasks(Amnesia.OptionRealizeTaskCount.GetInt()))
                {
                    if (!Utils.RoleSendList.Contains(pc.PlayerId)) Utils.RoleSendList.Add(pc.PlayerId);
                    Amnesia.RemoveAmnesia(pc.PlayerId);

                    taskState.hasTasks = UtilsTask.HasTasks(pc.Data, false);

                    if (AmongUsClient.Instance.AmHost)
                    {
                        if (pc.PlayerId != PlayerControl.LocalPlayer.PlayerId)
                            pc.RpcSetRoleDesync(roleinfo.BaseRoleType.Invoke(), pc.GetClientId());
                        else
                            if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                            if (roleinfo?.IsDesyncImpostor == true && roleinfo?.BaseRoleType.Invoke() is not null and not RoleTypes.Impostor)
                                RoleManager.Instance.SetRole(PlayerControl.LocalPlayer, roleinfo.BaseRoleType.Invoke());

                        pc.SyncSettings();
                        _ = new LateTask(() =>
                        {
                            pc.SetKillCooldown(Main.AllPlayerKillCooldown[pc.PlayerId], force: true, delay: true);
                            pc.RpcResetAbilityCooldown(Sync: true);
                        }, 0.2f, "ResetAbility");
                    }
                }

            //属性クラスの扱いを決定するまで仮置き
            ret &= Workhorse.OnCompleteTask(pc);
            UtilsNotifyRoles.NotifyRoles();

            if (ret)
            {
                if (taskState.CompletedTasksCount < taskState.AllTasksCount) return ret;
                if (!UtilsTask.HasTasks(pc.Data)) return ret;
                UtilsGameLog.AddGameLog("Task", string.Format(Translator.GetString("Taskfin"), UtilsName.GetPlayerColor(pc, true)));
            }
            return ret;
        }
        public static void Postfix()
        {
            //人外のタスクを排除して再計算
            GameData.Instance.RecomputeTaskCounts();
            Logger.Info($"TotalTaskCounts = {GameData.Instance.CompletedTasks}/{GameData.Instance.TotalTasks}", "TaskState.Update");
        }
    }
}