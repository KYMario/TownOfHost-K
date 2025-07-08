using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AmongUs.GameOptions;
using TownOfHost.Roles.Core;

namespace TownOfHost;

class TaskBattle
{
    public static bool IsTaskBattleTeamMode;
    /// <summary>key:チームid,value:List<プレイヤーid> </summary>
    public static Dictionary<byte, List<byte>> TaskBattleTeams = new();
    /// <summary>チャットで設定されたチーム情報 </summary>
    public static Dictionary<byte, List<byte>> SelectedTeams = new();
    /// <summary>追加でタスクを付与した数</summary>
    public static Dictionary<byte, int> TaskAddCount = new();
    public static bool IsAdding;
    [Attributes.GameModuleInitializer]
    public static void Init()
    {
        IsAdding = false;
        TaskAddCount = new();
        IsTaskBattleTeamMode = TaskBattleTeamMode.GetBool();

        if (TaskAddMode.GetBool())
        {
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                //GMはさいなら
                if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId && Options.EnableGM.GetBool()) continue;

                TaskAddCount.TryAdd(pc.PlayerId, 0);
            }
        }
    }
    public static bool TaskBattleCompleteTask(PlayerControl pc, TaskState taskState)
    {
        if (pc.Is(CustomRoles.TaskPlayerB) && taskState.IsTaskFinished)
        {
            if (TaskAddMode.GetBool())
            {
                if (TaskAddCount.TryGetValue(pc.PlayerId, out var count))
                {
                    //タスクは続くよどこまでも
                    if (count < MaxAddCount.GetInt())
                    {
                        IsAdding = true;
                        TaskAddCount[pc.PlayerId]++;

                        taskState.AllTasksCount += NumCommonTasks.GetInt() + NumLongTasks.GetInt() + NumShortTasks.GetInt();

                        if (AmongUsClient.Instance.AmHost)
                        {
                            pc.Data.RpcSetTasks(Array.Empty<byte>()); //タスクを再配布
                            pc.SyncSettings();
                        }
                        UtilsNotifyRoles.NotifyRoles();
                        UtilsGameLog.AddGameLog("TaskBattle", string.Format(Translator.GetString("TB"), Utils.GetPlayerColor(pc, true), taskState.CompletedTasksCount + "/" + taskState.AllTasksCount));
                        return false;
                    }
                }
            }

            if (IsTaskBattleTeamMode)
            {
                foreach (var team in TaskBattleTeams.Values)
                {
                    if (team.Contains(pc.PlayerId)) continue;
                    team.Do(playerId =>
                    {
                        PlayerCatch.GetPlayerById(playerId).RpcExileV2();
                        var playerState = PlayerState.GetByPlayerId(playerId);
                        playerState.SetDead();
                    });
                }
            }
            else
            {
                foreach (var otherPlayer in PlayerCatch.AllAlivePlayerControls)
                {
                    if (otherPlayer == pc || otherPlayer.AllTasksCompleted()) continue;
                    otherPlayer.RpcExileV2();
                    var playerState = PlayerState.GetByPlayerId(otherPlayer.PlayerId);
                    playerState.SetDead();
                }
            }
        }

        UtilsNotifyRoles.NotifyRoles();
        UtilsGameLog.AddGameLog("TaskBattle", string.Format(Translator.GetString("TB"), Utils.GetPlayerColor(pc, true), taskState.CompletedTasksCount + "/" + taskState.AllTasksCount));
        return true;
    }

    public static void GetMark(PlayerControl target, PlayerControl seer, ref StringBuilder Mark)
    {
        //seerがnullの場合targetに
        seer ??= target;

        if (seer.PlayerId == target.PlayerId)
        {
            if (!Options.EnableGM.GetBool())
            {
                if (TaskBattelShowAllTask.GetBool())
                {
                    var t1 = 0f;
                    var t2 = 0;
                    if (!TaskBattleTeamMode.GetBool() && !TaskBattleTeamWinType.GetBool())
                    {
                        foreach (var pc in PlayerCatch.AllPlayerControls)
                        {
                            t1 += pc.GetPlayerTaskState().AllTasksCount;
                            t2 += pc.GetPlayerTaskState().CompletedTasksCount;
                        }
                    }
                    else
                    {
                        foreach (var t in TaskBattleTeams.Values)
                        {
                            if (!t.Contains(seer.PlayerId)) continue;
                            t1 = TaskBattleTeamWinTaskc.GetFloat();
                            foreach (var id in t.Where(id => PlayerCatch.GetPlayerById(id).IsAlive()))
                                t2 += PlayerCatch.GetPlayerById(id).GetPlayerTaskState().CompletedTasksCount;
                        }
                    }
                    Mark.Append($"<color=yellow>({t2}/{t1})</color>");
                }
                if (TaskBattleShowFastestPlayer.GetBool())
                {
                    var to = 0;
                    if (!TaskBattleTeamMode.GetBool() && !TaskBattleTeamWinType.GetBool())
                    {
                        foreach (var pc in PlayerCatch.AllPlayerControls)
                            if (pc.GetPlayerTaskState().CompletedTasksCount > to) to = pc.GetPlayerTaskState().CompletedTasksCount;
                    }
                    else
                        foreach (var t in TaskBattleTeams.Values)
                        {
                            var to2 = 0;
                            foreach (var id in t.Where(id => PlayerCatch.GetPlayerById(id).IsAlive()))
                                to2 += PlayerCatch.GetPlayerById(id).GetPlayerTaskState().CompletedTasksCount;
                            if (to2 > to) to = to2;
                        }
                    Mark.Append($"<#00f7ff>({to})</color>");
                }
            }
        }
        else
        {
            if (TaskBattelCanSeeOtherPlayer.GetBool())
                Mark.Append($"<color=yellow>({target.GetPlayerTaskState().CompletedTasksCount}/{target.GetPlayerTaskState().AllTasksCount})</color>");
        }
    }

    public static OptionItem TaskBattleSet;
    public static OptionItem TaskBattleCanVent;
    public static OptionItem TaskBattleVentCooldown;
    public static OptionItem TaskBattelCanSeeOtherPlayer;
    public static OptionItem TaskBattelShowAllTask;
    public static OptionItem TaskBattleShowFastestPlayer;
    public static OptionItem TaskBattleTeamMode;
    public static OptionItem TaskBattleTeamCount;
    public static OptionItem TaskBattleTeamWinType;
    public static OptionItem TaskBattleTeamWinTaskc;
    public static OptionItem TaskSoroeru;

    public static OptionItem TaskAddMode;
    public static OptionItem NumCommonTasks;
    public static OptionItem NumLongTasks;
    public static OptionItem NumShortTasks;
    public static OptionItem MaxAddCount;
    public static void SetupOptionItem()
    {
        TaskBattleSet = BooleanOptionItem.Create(200317, "TaskBattleSet", false, TabGroup.MainSettings, false).SetGameMode(CustomGameMode.TaskBattle)
            .SetHeader(true)
            .SetColorcode("#87cffa");
        TaskBattleCanVent = BooleanOptionItem.Create(200307, "TaskBattleCanVent", false, TabGroup.MainSettings, false).SetParent(TaskBattleSet)
            .SetGameMode(CustomGameMode.TaskBattle);
        TaskBattleVentCooldown = FloatOptionItem.Create(200308, "TaskBattleVentCooldown", new(0f, 99f, 1f), 5f, TabGroup.MainSettings, false).SetParent(TaskBattleCanVent)
            .SetValueFormat(OptionFormat.Seconds)
            .SetGameMode(CustomGameMode.TaskBattle);
        TaskBattelShowAllTask = BooleanOptionItem.Create(200309, "TaskBattelShowAllTask", false, TabGroup.MainSettings, false).SetGameMode(CustomGameMode.TaskBattle).SetParent(TaskBattleSet);
        TaskBattelCanSeeOtherPlayer = BooleanOptionItem.Create(200311, "TaskBattelCanSeeOtherPlayer", false, TabGroup.MainSettings, false).SetGameMode(CustomGameMode.TaskBattle).SetParent(TaskBattleSet);
        TaskBattleShowFastestPlayer = BooleanOptionItem.Create(200312, "TaskBattleShowFastestPlayer", false, TabGroup.MainSettings, false).SetGameMode(CustomGameMode.TaskBattle).SetParent(TaskBattleSet);
        TaskBattleTeamMode = BooleanOptionItem.Create(200313, "TaskBattleTeamMode", false, TabGroup.MainSettings, false).SetParent(TaskBattleSet)
            .SetGameMode(CustomGameMode.TaskBattle);
        TaskBattleTeamCount = FloatOptionItem.Create(200314, "TaskBattleTeamCount", new(1f, 15f, 1f), 2f, TabGroup.MainSettings, false).SetParent(TaskBattleTeamMode)
            .SetGameMode(CustomGameMode.TaskBattle);
        TaskBattleTeamWinType = BooleanOptionItem.Create(200315, "TaskBattleTeamGameTaskComp", false, TabGroup.MainSettings, false).SetParent(TaskBattleTeamMode)
            .SetGameMode(CustomGameMode.TaskBattle);
        TaskBattleTeamWinTaskc = FloatOptionItem.Create(200316, "TaskBattleTeamWinTaskc", new(1f, 999f, 1f), 20f, TabGroup.MainSettings, false).SetParent(TaskBattleTeamWinType)
            .SetGameMode(CustomGameMode.TaskBattle);
        TaskSoroeru = BooleanOptionItem.Create(200318, "TaskSoroeru", false, TabGroup.MainSettings, false).SetParent(TaskBattleSet)
            .SetGameMode(CustomGameMode.TaskBattle);

        TaskAddMode = BooleanOptionItem.Create(200319, "TaskAddMode", false, TabGroup.MainSettings, false).SetParent(TaskBattleSet)
            .SetGameMode(CustomGameMode.TaskBattle);
        NumCommonTasks = IntegerOptionItem.Create(200320, "WorkhorseNumCommonTasks", new(0, 99, 1), 1, TabGroup.MainSettings, false).SetParent(TaskAddMode)
            .SetGameMode(CustomGameMode.TaskBattle).SetValueFormat(OptionFormat.Pieces);
        NumLongTasks = IntegerOptionItem.Create(200321, "WorkhorseNumLongTasks", new(0, 99, 1), 1, TabGroup.MainSettings, false).SetParent(TaskAddMode)
            .SetGameMode(CustomGameMode.TaskBattle).SetValueFormat(OptionFormat.Pieces);
        NumShortTasks = IntegerOptionItem.Create(200322, "WorkhorseNumShortTasks", new(0, 99, 1), 1, TabGroup.MainSettings, false).SetParent(TaskAddMode)
            .SetGameMode(CustomGameMode.TaskBattle).SetValueFormat(OptionFormat.Pieces);
        MaxAddCount = IntegerOptionItem.Create(200323, "MaxAddCount", new(1, 99, 1), 1, TabGroup.MainSettings, false)
            .SetGameMode(CustomGameMode.TaskBattle).SetParent(TaskAddMode);
    }
    public static void ResetAndSetTeam()
    {
        TaskBattleTeams.Clear();
        if (TaskBattleTeamMode.GetBool())
        {
            var rand = new Random();

            List<PlayerControl> ap = new();
            foreach (var pc in PlayerCatch.AllPlayerControls)
                ap.Add(pc);
            if (Options.EnableGM.GetBool())
                ap.RemoveAll(x => x == PlayerControl.LocalPlayer);
            //チームを指定されている人は処理せず、後から追加する。
            ap.RemoveAll(x => SelectedTeams.Values.Any(list => list.Contains(x.PlayerId)));

            var AllPlayerCount = ap.Count;
            var teamc = Math.Min(TaskBattleTeamCount.GetFloat(), AllPlayerCount);
            var c = AllPlayerCount / teamc;//1チームのプレイヤー数 ↑チーム数
            List<byte> playerlist = new();
            Logger.Info($"{teamc},{c}", "TBTeamandpc");

            for (var i = 0; i < teamc; i++)
            {
                Logger.Info($"team{i}", "TBSetTeam");
                playerlist.Clear();
                for (var i2 = 0; i2 < c; i2++)
                {
                    if (ap.Count == 0) continue;
                    var player = ap[rand.Next(0, ap.Count)];
                    playerlist.Add(player.PlayerId);
                    Logger.Info($"{player.PlayerId}", "TBSetplayer");
                    ap.Remove(player);
                }
                TaskBattleTeams[(byte)(i + 1)] = new List<byte>(playerlist);
            }

            foreach (var (teamId, player) in SelectedTeams)
            {
                List<byte> players;
                players = TaskBattleTeams.TryGetValue(teamId, out players) ? players : new();
                player.Do(x => players.Add(x));
                TaskBattleTeams[teamId] = players;
            }
        }
        foreach (var pc in PlayerCatch.AllPlayerControls)
        {
            if (Options.EnableGM.GetBool() && pc.PlayerId == PlayerControl.LocalPlayer.PlayerId)
            {
                PlayerControl.LocalPlayer.RpcSetCustomRole(CustomRoles.GM);
                PlayerControl.LocalPlayer.RpcSetRole(RoleTypes.Crewmate, false);
                PlayerControl.LocalPlayer.Data.IsDead = true;
            }
            else
            {
                pc.RpcSetCustomRole(CustomRoles.TaskPlayerB);
                pc.RpcSetRole(TaskBattle.TaskBattleCanVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate, false);
            }
        }
    }
    // ﾀｽﾊﾞﾄ用
    public class TaskBattleGameEndPredicate : GameEndPredicate
    {
        public override bool CheckForEndGame(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorsByKill;
            if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) return false;

            if (CheckGameEndByLivingPlayers(out reason)) return true;

            return false;
        }

        public bool CheckGameEndByLivingPlayers(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorsByKill;

            if (Main.RTAMode)
            {
                var player = PlayerCatch.GetPlayerById(Main.RTAPlayer);
                if (player.GetPlayerTaskState().IsTaskFinished)
                {
                    reason = GameOverReason.CrewmatesByTask;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.TaskPlayerB);
                    CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                    Main.RTAPlayer = byte.MaxValue;
                }
            }
            else
            if (!TaskBattle.TaskBattleTeamWinType.GetBool())
            {
                int TaskPlayerB = PlayerCatch.AlivePlayersCount(CountTypes.TaskPlayer);
                bool win = TaskPlayerB <= 1;
                if (TaskBattle.IsTaskBattleTeamMode)
                {
                    foreach (var pc in PlayerCatch.AllPlayerControls)
                    {
                        if (pc == null) continue;
                        if (pc.AllTasksCompleted())
                            win = true;
                    }
                }
                if (win)
                {
                    reason = GameOverReason.CrewmatesByTask;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.TaskPlayerB);
                    foreach (var pc in PlayerCatch.AllAlivePlayerControls)
                        CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                }
                else return false; //勝利条件未達成
            }
            else
            {
                foreach (var t in TaskBattle.TaskBattleTeams.Values)
                {
                    if (t == null) continue;
                    var task = 0;
                    foreach (var id in t)
                        task += PlayerState.GetByPlayerId(id).taskState.CompletedTasksCount;
                    if (TaskBattle.TaskBattleTeamWinTaskc.GetFloat() <= task)
                    {
                        reason = GameOverReason.CrewmatesByTask;
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.TaskPlayerB);
                        foreach (var id in t)
                            CustomWinnerHolder.WinnerIds.Add(id);
                    }
                }
            }

            return true;
        }
    }
}