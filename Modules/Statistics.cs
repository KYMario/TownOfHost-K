using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using TownOfHost.Roles.Core;
using static TownOfHost.Translator;
using static TownOfHost.PlayerCatch;
using static TownOfHost.UtilsRoleText;

namespace TownOfHost
{
    public class SaveStatistics
    {
        private static readonly string PATH = new($"{Application.persistentDataPath}/TownOfHost_K/Statistics.txt");
        public static void SetLogFolder()
        {
            try
            {
                if (!Directory.Exists($"{Application.persistentDataPath}/TownOfHost_K"))
                    Directory.CreateDirectory($"{Application.persistentDataPath}/TownOfHost_K");
            }
            catch { }
        }

        public static void Save()
        {
            try
            {
                SetLogFolder();
                if (File.Exists($"{Application.persistentDataPath}/TownOfHost_K/Statistics.txt"))
                {
                    File.Move($"{Application.persistentDataPath}/TownOfHost_K/Statistics.txt", PATH);
                }

                if (Statistics.NowStatistics == null || Statistics.riset)
                {
                    var t = "";
                    t += $"0!";

                    foreach (var role in EnumHelper.GetAllValues<CustomRoles>())
                    {
                        var id = role.GetRoleInfo()?.ConfigId;
                        t += $"{id}$0$0&";
                    }
                    t += "!";
                    foreach (var kill in EnumHelper.GetAllValues<CustomDeathReason>())
                    {
                        t += $"{(int)kill}$0&";
                    }
                    t += "!";
                    foreach (var dei in EnumHelper.GetAllValues<CustomDeathReason>())
                    {
                        t += $"{(int)dei}$0&";
                    }
                    t += "!";
                    t += $"414";
                    t += $"!0!0";
                    Main.SKey.Value = $"414";
                    Statistics.riset = false;
                    Logger.Info($"や<color>っ<size>ほ<line=ad>～！".RemoveHtmlTags(), "Statistics");
                    File.WriteAllText(PATH, t);

                    Statistics.NowStatistics = Load();
                    return;
                }

                {
                    var t = "";
                    t += $"{Statistics.NowStatistics.gamecount}!";

                    foreach (var role in Statistics.NowStatistics.Rolecount)
                    {
                        var id = role.Key.GetRoleInfo().ConfigId;
                        t += $"{id}${role.Value.Item1}${role.Value.Item2}&";
                    }
                    t += "!";
                    foreach (var kill in Statistics.NowStatistics.Killcount)
                    {
                        t += $"{(int)kill.Key}${kill.Value}&";
                    }
                    t += "!";
                    foreach (var dei in Statistics.NowStatistics.diecount)
                    {
                        t += $"{(int)dei.Key}${dei.Value}&";
                    }
                    t += "!";
                    var a = IRandom.Instance.Next(0, 5000);
                    t += $"{a}";
                    t += $"!{Statistics.NowStatistics.task.Item1}!{Statistics.NowStatistics.task.Item2}";

                    File.WriteAllText(PATH, t);
                    Main.SKey.Value = $"{a}";
                }
            }
            catch
            {
                Logger.Error("Saveでエラー！", "Statistics");
            }
        }
        public static Statistics Load()
        {
            try
            {
                SetLogFolder();
                if (File.Exists($"{Application.persistentDataPath}/TownOfHost_K/Statistics.txt"))
                {
                    File.Move($"{Application.persistentDataPath}/TownOfHost_K/Statistics.txt", PATH);
                }
                else
                {
                    File.WriteAllText(PATH, "");
                }

                string Text = File.ReadAllText(PATH);

                if (Text == "")
                {
                    Logger.Info($"からぽ！", "Statistics-Load");
                    Save();
                    return null;
                }
                var age = Text.Split("!");
                if (age.Count() is not 7) return null;
                int.TryParse(age[0], out int gamecount);

                Dictionary<CustomRoles, (int win, int loss)> RoleCount = new();
                Dictionary<CustomDeathReason, int> Killcount = new();
                Dictionary<CustomDeathReason, int> diecount = new();

                var rolea = age[1];
                var roleage = rolea.Split("&");
                foreach (var text in roleage)
                {
                    var subage = text.Split("$");

                    if (subage.Count() != 3) { Logger.Error($"roleのsubageが3以外({subage.Count()})", "Statistics"); continue; }
                    if (!int.TryParse(subage[0], out int roleid)) continue;
                    if (!int.TryParse(subage[1], out int win)) continue;
                    if (!int.TryParse(subage[2], out int loss)) continue;
                    if (!CustomRoleManager.CustomRoleIds.TryGetValue(roleid, out var role)) continue;

                    RoleCount.TryAdd(role, (win, loss));
                }
                var killa = age[2];
                var kage = killa.Split("&");
                foreach (var text in kage)
                {
                    var subage = text.Split("$");
                    if (subage.Count() != 2) { Logger.Error($"killのsubageが2以外({subage.Count()})", "Statistics"); continue; }
                    if (!int.TryParse(subage[0], out int dr)) continue;
                    if (!int.TryParse(subage[1], out int count)) continue;

                    CustomDeathReason deathReason = (CustomDeathReason)dr;
                    Killcount.TryAdd(deathReason, count);
                }

                var diea = age[3];
                var dieage = diea.Split("&");
                if ((age[4] ?? "-") != Main.SKey.Value && (Main.SKey.Value is not "141c2e1c"))
                {
                    Statistics.riset = true;
                    return null;
                }
                foreach (var text in dieage)
                {
                    var subage = text.Split("$");
                    if (subage.Count() != 2) { Logger.Error($"dieのsubageが2以外({subage.Count()})", "Statistics"); continue; }
                    if (!int.TryParse(subage[0], out int dr)) continue;
                    if (!int.TryParse(subage[1], out int count)) continue;

                    CustomDeathReason deathReason = (CustomDeathReason)dr;
                    diecount.TryAdd(deathReason, count);
                }
                if (!int.TryParse(age[5], out int task)) task = 0;
                if (!int.TryParse(age[6], out int all)) all = 0;

                return new Statistics(gamecount, RoleCount, Killcount, diecount, (task, all));
            }
            catch
            {
                Logger.Error("Loadでエラーを吐いたのでリセット", "Statistics");
                Statistics.riset = true;
                return null;
            }
        }
        public static string ShowText()
        {
            var text = "";

            if (Statistics.NowStatistics == null)
            {
                return GetString("StatisticsError.Null");
            }

            text += $"<size=60%>{GetString("Statistics.GameCount")}：{Statistics.NowStatistics.gamecount}";

            var role = "";
            var wincount = 0;
            foreach (var roledata in Statistics.NowStatistics.Rolecount.OrderBy(x => x.Key.GetRoleInfo().ConfigId))
            {
                if (roledata.Value.Item1 == roledata.Value.Item2 && roledata.Value.Item2 == 0) continue;
                if (roledata.Key.IsE()) continue;
                if (!Event.CheckRole(roledata.Key)) continue;

                role += $"\n{GetRoleColorAndtext(roledata.Key)}：{roledata.Value.Item2}/{roledata.Value.Item1}";
                wincount += roledata.Value.Item2;
            }
            text += $"\n{GetString("Statistics.WinCount")}{wincount}";
            if (role != "") text += $"\n★{GetString("Statistics.RoleWinCount")}<size=50%>{role}</size>";

            var kill = "";
            foreach (var killdata in Statistics.NowStatistics.Killcount)
            {
                if (killdata.Value == 0) continue;
                kill += $"\n{GetString($"DeathReason.{killdata.Key}")} : {killdata.Value}";
            }
            if (kill != "") text += $"\n★{GetString("Statistics.Killcount")}<size=50%>{kill}</size>";

            var die = "";
            foreach (var diedata in Statistics.NowStatistics.diecount)
            {
                if (diedata.Value == 0) continue;
                die += $"\n{GetString($"DeathReason.{diedata.Key}")} : {diedata.Value}";
            }
            if (die != "") text += $"\n★{GetString("Statistics.deadcount")}<size=50%>{die}</size>";

            var task = "";
            task = $"\n・{GetString("Statistics.taskcount")}：{Statistics.NowStatistics.task.Item1}"
            + $"\n・{GetString("Statistics.completetaskcount")}：{Statistics.NowStatistics.task.Item2}";

            return text + task;
        }
    }
    public class Statistics
    {
        public static Statistics NowStatistics = null;
        public static bool riset = false;
        public int gamecount;
        public Dictionary<CustomRoles, (int, int)> Rolecount;
        public Dictionary<CustomDeathReason, int> Killcount;
        public Dictionary<CustomDeathReason, int> diecount;
        public (int, int) task;

        public Statistics(int gamecount, Dictionary<CustomRoles, (int, int)> Rolecount, Dictionary<CustomDeathReason, int> Killcount, Dictionary<CustomDeathReason, int> diecount, (int, int) task)
        {
            this.gamecount = gamecount;
            this.Rolecount = Rolecount;
            this.Killcount = Killcount;
            this.diecount = diecount;
            this.task = task;
        }

        public static string CheckAdd(bool InLoby)
        {
            if (CustomWinnerHolder.WinnerTeam == CustomWinner.Default && !InLoby) return GetString("StatisticsError.forceend");
#if DEBUG
            if (DebugModeManager.EnableDebugMode.GetBool() || DebugModeManager.EnableTOHkDebugMode.GetBool()) return GetString("StatisticsError.Debug");
#endif
            if (GameStates.IsLocalGame) return GetString("StatisticsError.Local");
            if (UtilsGameLog.LastLogRole.Count <= 4 && !InLoby) return GetString("StatisticsError.insufficient");
            if (InLoby && AllPlayerControls.Count() <= 4) return GetString("StatisticsError.insufficient") + GetString("LobbyError.insufficient");

            return "";
        }

        public static void Update()
        {
            var check = CheckAdd(false);
            if (check is not "")
            {
                Logger.Info(check, "Statistics");
                return;
            }

            var pc = PlayerControl.LocalPlayer;
            var role = pc.GetCustomRole();
            var mystate = PlayerState.GetByPlayerId(0);
            var rc = NowStatistics.Rolecount;
            var kc = NowStatistics.Killcount;
            var dc = NowStatistics.diecount;

            {
                if (!rc.TryGetValue(role, out var data)) goto pyoon;
                var i1 = data.Item1;
                var i2 = data.Item2;
                if (Main.winnerList.Contains(pc.PlayerId)) i2 += 1;

                rc[role] = (i1 + 1, i2);
            }
            goto pyoon;

        pyoon:
            if (!pc.IsAlive())
            {
                if (dc.TryGetValue(mystate.DeathReason, out var count))
                {
                    dc[mystate.DeathReason] = count + 1;
                }
            }
            foreach (var diedata in Main.HostKill)
            {
                kc[diedata.Value] = kc[diedata.Value] + 1;
            }

            var task = NowStatistics.task;
            if (pc.Is(CustomRoleTypes.Crewmate) && role.GetRoleInfo()?.IsDesyncImpostor == false)
            {
                var state = pc.GetPlayerTaskState();

                task = (task.Item1 + state.CompletedTasksCount, task.Item2 + (state.IsTaskFinished ? 1 : 0));
            }

            NowStatistics = new Statistics(NowStatistics.gamecount + 1, rc, kc, dc, task);

            SaveStatistics.Save();
        }
    }
}