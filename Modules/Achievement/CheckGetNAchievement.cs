using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TownOfHost.Attributes;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Impostor;

namespace TownOfHost;

class CheckGetNomalAchievement
{
    private static Dictionary<int, Achievement> achievements => NomalAchievement.achievements;
    private static Statistics statistics => Statistics.NowStatistics;
    private static byte playerid => PlayerControl.LocalPlayer?.PlayerId ?? byte.MaxValue;

    private static Dictionary<byte, int> turnkill;

    [GameModuleInitializer]
    public static void OnStartGame()
    {
        turnkill = new();
    }
    public static void OnGameEnd()
    {
        if (!achievements[100_001].IsCompleted)
            Achievements.RpcCompleteAchievement(playerid, 2, achievements[100_000]);

        var st = statistics.gamemodecount.TryGetValue(CustomGameMode.Standard, out var _s) ? _s : (0, 0);
        if (3 <= st.Item1) Achievements.RpcCompleteAchievement(playerid, 2, achievements[100_001]);
        if (10 <= st.Item1) Achievements.RpcCompleteAchievement(playerid, 2, achievements[100_002]);
        if (30 <= st.Item1) Achievements.RpcCompleteAchievement(playerid, 2, achievements[100_003]);
        if (50 <= st.Item1) Achievements.RpcCompleteAchievement(playerid, 2, achievements[100_004]);
        if (100 <= st.Item1) Achievements.RpcCompleteAchievement(playerid, 2, achievements[100_005]);
        if (200 <= st.Item1) Achievements.RpcCompleteAchievement(playerid, 2, achievements[100_006]);
        if (300 <= st.Item1) Achievements.RpcCompleteAchievement(playerid, 2, achievements[100_007]);
        if (400 <= st.Item1) Achievements.RpcCompleteAchievement(playerid, 2, achievements[100_008]);
        if (500 <= st.Item1) Achievements.RpcCompleteAchievement(playerid, 2, achievements[100_009]);
        if (1000 <= st.Item1) Achievements.RpcCompleteAchievement(playerid, 2, achievements[100_010]);

        if (1 <= st.Item2) Achievements.RpcCompleteAchievement(playerid, 2, achievements[100_011]);
        if (10 <= st.Item2) Achievements.RpcCompleteAchievement(playerid, 2, achievements[100_012]);
        if (30 <= st.Item2) Achievements.RpcCompleteAchievement(playerid, 2, achievements[100_013]);
        if (50 <= st.Item2) Achievements.RpcCompleteAchievement(playerid, 2, achievements[100_014]);
        if (100 <= st.Item2) Achievements.RpcCompleteAchievement(playerid, 2, achievements[100_015]);

        var allkill = 0;
        statistics.Killcount.Values.Do(x => allkill += x);
        if (3 <= allkill) Achievements.RpcCompleteAchievement(playerid, 2, achievements[600_000]);
        if (10 <= allkill) Achievements.RpcCompleteAchievement(playerid, 2, achievements[600_001]);
        if (50 <= allkill) Achievements.RpcCompleteAchievement(playerid, 2, achievements[600_002]);
        if (100 <= allkill) Achievements.RpcCompleteAchievement(playerid, 2, achievements[600_003]);
        if (300 <= allkill) Achievements.RpcCompleteAchievement(playerid, 2, achievements[600_004]);
        if (1000 <= allkill) Achievements.RpcCompleteAchievement(playerid, 2, achievements[600_005]);

        if (3 <= (statistics.Killcount.TryGetValue(CustomDeathReason.Bombed, out var bomkill) ? bomkill : 0)) Achievements.RpcCompleteAchievement(playerid, 2, achievements[600_006]);

        var nonkill = 0;
        statistics.Killcount.DoIf(x => x.Key is not CustomDeathReason.Kill, x => nonkill += x.Value);
        if (3 <= nonkill) Achievements.RpcCompleteAchievement(playerid, 2, achievements[600_007]);
        if (10 <= nonkill) Achievements.RpcCompleteAchievement(playerid, 2, achievements[600_008]);
        if (50 <= nonkill) Achievements.RpcCompleteAchievement(playerid, 2, achievements[600_009]);

        if (playerid.GetPlayerState().GetKillCount() <= 0 && playerid.GetPlayerControl().IsAlive() && PlayerControl.LocalPlayer.IsWinner() && !PlayerControl.LocalPlayer.IsLovers())
            Achievements.RpcCompleteAchievement(playerid, 2, achievements[600_019]);

        var HnSGame = statistics.gamemodecount.TryGetValue(CustomGameMode.HideAndSeek, out var _Hns) ? _Hns : (0, 0);
        if (statistics.gamemodecount.TryGetValue(CustomGameMode.StandardHAS, out var _shns))
        {
            HnSGame.Item1 += _shns.Item1;
            HnSGame.Item2 += _shns.Item2;
        }
        if (1 <= HnSGame.Item1) Achievements.RpcCompleteAchievement(playerid, 2, achievements[200_000]);
        if (10 <= HnSGame.Item1) Achievements.RpcCompleteAchievement(playerid, 2, achievements[200_001]);
        if (50 <= HnSGame.Item1) Achievements.RpcCompleteAchievement(playerid, 2, achievements[200_002]);
        if (100 <= HnSGame.Item1) Achievements.RpcCompleteAchievement(playerid, 2, achievements[200_003]);
        if (200 <= HnSGame.Item1) Achievements.RpcCompleteAchievement(playerid, 2, achievements[200_004]);

        if (Options.CurrentGameMode is CustomGameMode.HideAndSeek or CustomGameMode.StandardHAS)
        {
            if (CustomWinnerHolder.WinnerTeam is CustomWinner.Impostor && PlayerControl.LocalPlayer.Is(CustomRoleTypes.Impostor))
            {
                Achievements.RpcCompleteAchievement(playerid, 2, achievements[200_005]);
                Achievements.RpcCompleteAchievement(playerid, 1, achievements[200_006]);
                Achievements.RpcCompleteAchievement(playerid, 1, achievements[200_007]);
                Achievements.RpcCompleteAchievement(playerid, 1, achievements[200_008]);
                Achievements.RpcCompleteAchievement(playerid, 1, achievements[200_009]);
            }
            if (CustomWinnerHolder.WinnerTeam is CustomWinner.Crewmate && PlayerControl.LocalPlayer.Is(CustomRoleTypes.Crewmate))
            {
                Achievements.RpcCompleteAchievement(playerid, 2, achievements[200_010]);
                Achievements.RpcCompleteAchievement(playerid, 1, achievements[200_011]);
                Achievements.RpcCompleteAchievement(playerid, 1, achievements[200_012]);
                Achievements.RpcCompleteAchievement(playerid, 1, achievements[200_013]);
                Achievements.RpcCompleteAchievement(playerid, 1, achievements[200_014]);
            }
        }
        var sudden = statistics.gamemodecount.TryGetValue(CustomGameMode.SuddenDeath, out var _sud) ? _sud : (0, 0);
        if (1 <= sudden.Item1) Achievements.RpcCompleteAchievement(playerid, 2, achievements[300_000]);
        if (3 <= sudden.Item1) Achievements.RpcCompleteAchievement(playerid, 2, achievements[300_001]);
        if (10 <= sudden.Item1) Achievements.RpcCompleteAchievement(playerid, 2, achievements[300_002]);
        if (30 <= sudden.Item1) Achievements.RpcCompleteAchievement(playerid, 2, achievements[300_003]);
        if (100 <= sudden.Item1) Achievements.RpcCompleteAchievement(playerid, 2, achievements[300_004]);

        if (10 <= sudden.Item2) Achievements.RpcCompleteAchievement(playerid, 2, achievements[300_005]);
        if (50 <= sudden.Item2) Achievements.RpcCompleteAchievement(playerid, 2, achievements[300_006]);
        if (100 <= sudden.Item2) Achievements.RpcCompleteAchievement(playerid, 2, achievements[300_007]);

        if (Options.CurrentGameMode is CustomGameMode.SuddenDeath)
        {
            if (CustomWinnerHolder.WinnerTeam is CustomWinner.SuddenDeathRed && SuddenDeathMode.TeamRed.Contains(playerid))
                Achievements.RpcCompleteAchievement(playerid, 2, achievements[300_008]);
            if (CustomWinnerHolder.WinnerTeam is CustomWinner.SuddenDeathBlue && SuddenDeathMode.TeamBlue.Contains(playerid))
                Achievements.RpcCompleteAchievement(playerid, 2, achievements[300_009]);
            if (CustomWinnerHolder.WinnerTeam is CustomWinner.SuddenDeathYellow && SuddenDeathMode.TeamYellow.Contains(playerid))
                Achievements.RpcCompleteAchievement(playerid, 2, achievements[300_010]);
            if (CustomWinnerHolder.WinnerTeam is CustomWinner.SuddenDeathGreen && SuddenDeathMode.TeamGreen.Contains(playerid))
                Achievements.RpcCompleteAchievement(playerid, 2, achievements[300_011]);
            if (CustomWinnerHolder.WinnerTeam is CustomWinner.SuddenDeathPurple && SuddenDeathMode.TeamPurple.Contains(playerid))
                Achievements.RpcCompleteAchievement(playerid, 2, achievements[300_012]);
        }

        if (statistics.gamemodecount.TryGetValue(CustomGameMode.TaskBattle, out var taskbattle))
        {
            if (10 <= taskbattle.count) Achievements.RpcCompleteAchievement(playerid, 2, achievements[400_000]);
            if (50 <= taskbattle.count) Achievements.RpcCompleteAchievement(playerid, 2, achievements[400_001]);
            if (100 <= taskbattle.count) Achievements.RpcCompleteAchievement(playerid, 2, achievements[400_002]);
            if (1 <= taskbattle.win) Achievements.RpcCompleteAchievement(playerid, 2, achievements[400_003]);
            if (5 <= taskbattle.win) Achievements.RpcCompleteAchievement(playerid, 2, achievements[400_004]);
            if (50 <= taskbattle.win) Achievements.RpcCompleteAchievement(playerid, 2, achievements[400_005]);
        }
        if (Options.CurrentGameMode is CustomGameMode.TaskBattle)
        {
            var alltask = Main.NormalOptions.NumCommonTasks + Main.NormalOptions.NumShortTasks + Main.NormalOptions.NumLongTasks;
            Achievements.RpcCompleteAchievement(playerid, 1, achievements[400_006], PlayerControl.LocalPlayer.GetPlayerTaskState().CompletedTasksCount);
            Achievements.RpcCompleteAchievement(playerid, 1, achievements[400_007], PlayerControl.LocalPlayer.GetPlayerTaskState().CompletedTasksCount);
            Achievements.RpcCompleteAchievement(playerid, 1, achievements[400_008], PlayerControl.LocalPlayer.GetPlayerTaskState().CompletedTasksCount);
            Achievements.RpcCompleteAchievement(playerid, 1, achievements[400_009], PlayerControl.LocalPlayer.GetPlayerTaskState().CompletedTasksCount);
            Achievements.RpcCompleteAchievement(playerid, 1, achievements[400_010], PlayerControl.LocalPlayer.GetPlayerTaskState().CompletedTasksCount);

            if (250 <= allkill)
                Achievements.RpcCompleteAchievement(playerid, 2, achievements[400_011]);
        }
        if (Options.CurrentGameMode is CustomGameMode.MurderMystery)
        {
            if (statistics.gamemodecount.TryGetValue(CustomGameMode.MurderMystery, out var mm))
            {
                if (1 <= mm.count) Achievements.RpcCompleteAchievement(playerid, 2, achievements[500_000]);
                if (10 <= mm.count) Achievements.RpcCompleteAchievement(playerid, 2, achievements[500_001]);
                if (50 <= mm.count) Achievements.RpcCompleteAchievement(playerid, 2, achievements[500_002]);
            }
            if (PlayerControl.LocalPlayer.IsWinner(CustomWinner.Crewmate))
            {
                Achievements.RpcCompleteAchievement(playerid, 2, achievements[500_003]);
                Achievements.RpcCompleteAchievement(playerid, 2, achievements[500_004]);
                Achievements.RpcCompleteAchievement(playerid, 2, achievements[500_005]);
            }
            if (PlayerControl.LocalPlayer.IsWinner(CustomWinner.Impostor))
            {
                Achievements.RpcCompleteAchievement(playerid, 2, achievements[500_006]);
                Achievements.RpcCompleteAchievement(playerid, 2, achievements[500_007]);
                Achievements.RpcCompleteAchievement(playerid, 2, achievements[500_008]);
            }
        }
        if (Options.CurrentGameMode is CustomGameMode.Standard)
        {
            var (imp, crew, mad, neu) = (0, 0, 0, 0);
            foreach (var roledata in statistics.Rolecount)
            {
                switch (roledata.Key.GetCustomRoleTypes())
                {
                    case CustomRoleTypes.Impostor: imp += roledata.Value.Item2; break;
                    case CustomRoleTypes.Crewmate: crew += roledata.Value.Item2; break;
                    case CustomRoleTypes.Madmate: mad += roledata.Value.Item2; break;
                    case CustomRoleTypes.Neutral: neu += roledata.Value.Item2; break;
                }
            }
            if (3 <= imp) Achievements.RpcCompleteAchievement(playerid, 2, achievements[700_000]);
            if (10 <= imp) Achievements.RpcCompleteAchievement(playerid, 2, achievements[700_001]);
            if (30 <= imp) Achievements.RpcCompleteAchievement(playerid, 2, achievements[700_002]);
            if (50 <= imp) Achievements.RpcCompleteAchievement(playerid, 2, achievements[700_003]);
            if (100 <= imp) Achievements.RpcCompleteAchievement(playerid, 2, achievements[700_004]);

            if (3 <= mad) Achievements.RpcCompleteAchievement(playerid, 2, achievements[800_000]);
            if (25 <= mad) Achievements.RpcCompleteAchievement(playerid, 2, achievements[800_001]);
            if (50 <= mad) Achievements.RpcCompleteAchievement(playerid, 2, achievements[800_002]);

            if (10 <= crew) Achievements.RpcCompleteAchievement(playerid, 2, achievements[900_000]);
            if (30 <= crew) Achievements.RpcCompleteAchievement(playerid, 2, achievements[900_001]);
            if (50 <= crew) Achievements.RpcCompleteAchievement(playerid, 2, achievements[900_002]);
            if (100 <= crew) Achievements.RpcCompleteAchievement(playerid, 2, achievements[900_003]);
            if (200 <= crew) Achievements.RpcCompleteAchievement(playerid, 2, achievements[900_004]);

            if (1 <= neu) Achievements.RpcCompleteAchievement(playerid, 2, achievements[1000_000]);
            if (5 <= neu) Achievements.RpcCompleteAchievement(playerid, 2, achievements[1000_001]);
            if (30 <= neu) Achievements.RpcCompleteAchievement(playerid, 2, achievements[1000_002]);

            if (100 <= statistics.task.Item1) Achievements.RpcCompleteAchievement(playerid, 2, achievements[900_005]);
            if (200 <= statistics.task.Item1) Achievements.RpcCompleteAchievement(playerid, 2, achievements[900_006]);
            if (300 <= statistics.task.Item1) Achievements.RpcCompleteAchievement(playerid, 2, achievements[900_007]);
            if (10 <= statistics.task.Item2) Achievements.RpcCompleteAchievement(playerid, 2, achievements[900_008]);
            if (100 <= statistics.task.Item2) Achievements.RpcCompleteAchievement(playerid, 2, achievements[900_009]);
        }
    }

    public static void OnMurderPlayer(MurderInfo info)
    {
        var (killer, target) = info.AppearanceTuple;

        if (info.KillPower is 2) Achievements.RpcCompleteAchievement(killer.PlayerId, 1, achievements[600_010]);
        if (info.KillPower is 3) Achievements.RpcCompleteAchievement(killer.PlayerId, 1, achievements[600_011]);
        if (info.KillPower is 10) Achievements.RpcCompleteAchievement(killer.PlayerId, 1, achievements[600_012]);
        if (turnkill.TryAdd(killer.PlayerId, 1) is false) turnkill[killer.PlayerId] = turnkill[killer.PlayerId] + 1;
        if (turnkill.TryGetValue(killer.PlayerId, out var nowturnkill))
        {
            if (nowturnkill is 3) Achievements.RpcCompleteAchievement(killer.PlayerId, 0, achievements[600_013]);
            if (nowturnkill is 5) Achievements.RpcCompleteAchievement(killer.PlayerId, 0, achievements[600_014]);
        }
        if (target.GetCustomRole().IsImpostor()) Achievements.RpcCompleteAchievement(killer.PlayerId, 0, achievements[600_15]);

        if (Options.CurrentGameMode is CustomGameMode.HideAndSeek or CustomGameMode.StandardHAS)
        {
            Achievements.RpcCompleteAchievement(killer.PlayerId, 1, achievements[200_015]);
            Achievements.RpcCompleteAchievement(killer.PlayerId, 1, achievements[200_016]);
            Achievements.RpcCompleteAchievement(killer.PlayerId, 1, achievements[200_017]);
        }

        if (Camouflage.IsCamouflage || Camouflager.NowUse)
            Achievements.RpcCompleteAchievement(killer.PlayerId, 0, achievements[700_006]);

        if (killer.Is(CustomRoleTypes.Impostor) && target.GetCustomRole().IsMadmate())
        {
            Achievements.RpcCompleteAchievement(target.PlayerId, 0, achievements[800_003]);
        }

        if (PlayerCatch.AllPlayersCount - PlayerCatch.AllAlivePlayersCount is 0 or 1)
            Achievements.RpcCompleteAchievement(target.PlayerId, 2, achievements[1100_003]);
    }
    public static void OnMeeting()
    {
        turnkill = new();
    }
    public static void CallEndGame(GameOverReason reason)
    {
        if (Options.CurrentGameMode is CustomGameMode.Standard)
        {
            if (CustomWinnerHolder.WinnerTeam is CustomWinner.Impostor)
            {
                if (reason is GameOverReason.ImpostorsBySabotage)
                {
                    PlayerCatch.AllPlayerControls.Where(pc => pc.Is(CustomRoleTypes.Impostor)).Do(pc =>
                    {
                        Achievements.RpcCompleteAchievement(pc.PlayerId, 0, achievements[700_005]);
                    });
                }
                if (PlayerCatch.AllPlayerControls.Any(pc => pc.GetPlayerState().DeathReason is CustomDeathReason.Vote) is false)
                {
                    PlayerCatch.AllPlayerControls.Where(pc => pc.Is(CustomRoleTypes.Impostor)).Do(pc =>
                    {
                        Achievements.RpcCompleteAchievement(pc.PlayerId, 0, achievements[700_007]);
                    });
                }
                if (PlayerCatch.AllPlayerControls.All(pc => !pc.Is(CustomRoleTypes.Impostor) || (pc.Is(CustomRoleTypes.Impostor) && pc.IsAlive())))
                {
                    PlayerCatch.AllPlayerControls.Where(pc => pc.Is(CustomRoleTypes.Impostor)).Do(pc =>
                    {
                        Achievements.RpcCompleteAchievement(pc.PlayerId, 0, achievements[700_008]);
                    });
                }
            }

            if (PlayerCatch.AllPlayerControls.Any(pc => pc.GetCustomRole().IsE() || pc.GetCustomRole() is CustomRoles.Cakeshop))
            {
                PlayerCatch.AllPlayerControls.Do(pc =>
                {
                    Achievements.RpcCompleteAchievement(pc.PlayerId, 0, achievements[1100_001]);
                });
            }
            if (Main.CustomName.Value && Event.IsEventDay)
            {
                PlayerCatch.AllPlayerControls.Do(pc =>
                {
                    Achievements.RpcCompleteAchievement(pc.PlayerId, 0, achievements[1100_000]);
                });
            }
            if (Main.GameCount is 30)
            {
                PlayerCatch.AllPlayerControls.Do(pc =>
                {
                    Achievements.RpcCompleteAchievement(pc.PlayerId, 0, achievements[1100_002]);
                });
            }
        }
    }
}