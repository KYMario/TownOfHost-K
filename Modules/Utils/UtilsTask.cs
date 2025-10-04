using System;
using AmongUs.GameOptions;
using TownOfHost.Roles.Core;

namespace TownOfHost
{
    public static class UtilsTask
    {
        public static bool HasTasks(NetworkedPlayerInfo p, bool ForRecompute = true)
        {
            try
            {
                if (GameStates.IsLobby) return false;
                //Tasksがnullの場合があるのでその場合タスク無しとする
                if (p?.Tasks == null
                || p?.Role == null
                || (p?.Disconnected ?? true)) return false;

                var hasTasks = true;
                var States = PlayerState.GetByPlayerId(p.PlayerId);
                if (States is null) return false;
                if (p.Role.IsImpostor)
                    hasTasks = false; //タスクはCustomRoleを元に判定する
                if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
                {
                    if (p.IsDead) hasTasks = false;
                    if (States.MainRole is CustomRoles.HASFox or CustomRoles.HASTroll) hasTasks = false;
                }
                else
                {
                    // 死んでいて，死人のタスク免除が有効なら確定でfalse
                    if (p.IsDead && Options.GhostIgnoreTasks.GetBool())
                    {
                        return false;
                    }
                    var role = States.MainRole;
                    var roleClass = CustomRoleManager.GetByPlayerId(p.PlayerId);
                    if (roleClass != null)
                    {
                        switch (roleClass.HasTasks)
                        {
                            case HasTask.True:
                                hasTasks = true;
                                break;
                            case HasTask.False:
                                hasTasks = false;
                                break;
                            case HasTask.ForRecompute:
                                hasTasks = !ForRecompute;
                                break;
                        }
                    }
                    switch (role)
                    {
                        case CustomRoles.GM:
                        case CustomRoles.SKMadmate:
                            hasTasks = false;
                            break;
                        default:
                            if (role.IsImpostor()) hasTasks = false;
                            break;
                    }

                    foreach (var subRole in States.SubRoles)
                        switch (subRole)
                        {
                            case CustomRoles.Amanojaku: hasTasks &= !ForRecompute; break;
                            case CustomRoles.Amnesia:
                                {
                                    var ch = false;
                                    switch (role.GetRoleInfo()?.BaseRoleType.Invoke())
                                    {
                                        case RoleTypes.Crewmate:
                                        case RoleTypes.Engineer:
                                        case RoleTypes.Scientist:
                                        case RoleTypes.Noisemaker:
                                        case RoleTypes.Tracker:
                                        case RoleTypes.Detective:
                                            ch = true;
                                            break;
                                    }
                                    hasTasks = role.IsCrewmate() && ch ? hasTasks : (!ForRecompute && !States.MainRole.IsImpostor());
                                }
                                break;
                        }

                    if (States.GhostRole is CustomRoles.AsistingAngel) hasTasks = false;

                    //ラバーズはタスクを勝利用にカウントしない
                    //回線落ちになってもタスクは復活しない
                    if (Lovers.HaveLoverDontTaskPlayers.Contains(p.PlayerId))
                        hasTasks &= !ForRecompute;
                }
                return hasTasks;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString(), "HasTask");
                return false;
            }
        }
        public static string AllTaskstext(bool percentage, bool RoughPercentage, bool OnlyMeeting, bool Activecomms, bool CanSeeComms)
        {
            float t1 = 0;
            float t2 = 0;
            float pa = 0;
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                var task = PlayerState.GetByPlayerId(pc.PlayerId).taskState;
                if (task.hasTasks && HasTasks(pc.Data))
                {
                    t1 += pc.GetPlayerTaskState().AllTasksCount;
                    t2 += pc.GetPlayerTaskState().CompletedTasksCount;
                    pa = t2 / t1;//intならぶっこわれる!
                }
            }
            float pas = pa * 100;//小数点考えない四捨五入
            double ret1 = Math.Round(pas);//小数点以下の四捨五入
            double ret = ret1 * 0.1f;//ぽんこつ用に0.1倍して
            double ret2 = Math.Round(ret);//四捨五入
            double ret3 = ret2 * 10;//10倍してぽんこつに。

            if ((!GameStates.CalledMeeting && OnlyMeeting) || (Activecomms && !CanSeeComms)) return $"<#cee4ae>[??]</color>";
            else if (!percentage) return $"<#cee4ae>[{t2}/{t1}]</color>";
            else if (RoughPercentage) return $"<#cee4ae>[{ret3}%]</color>";
            else return $"<#cee4ae>[{ret1}%]</color>";
        }
    }
}