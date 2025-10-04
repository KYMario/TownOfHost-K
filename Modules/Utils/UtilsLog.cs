using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Data;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using static TownOfHost.Utils;
using static TownOfHost.Translator;
using static TownOfHost.PlayerCatch;
using static TownOfHost.UtilsRoleText;
using TownOfHost.Attributes;

namespace TownOfHost
{
    #region  OutputLog
    public static class UtilsOutputLog
    {
        public static DirectoryInfo GetLogFolder(bool auto = false)
        {
            var folder = Directory.CreateDirectory($"{Application.persistentDataPath}/TownOfHost_K/Logs");
            if (auto)
            {
                folder = Directory.CreateDirectory($"{folder.FullName}/AutoLogs");
            }
            return folder;
        }
        public static void DumpLog()
        {
            var logs = GetLogFolder();
            var filename = CopyLog(logs.FullName);
            OpenDirectory(filename);
            if (PlayerControl.LocalPlayer != null)
                SendMessage(GetString("Message.LogsSavedInLogsFolder"));
        }
        public static void SaveNowLog()
        {
            var logs = GetLogFolder(true);
            // 3日以上前のログを削除 /* 元は7だけど、7も保存しててもなので...*/
            logs.EnumerateFiles().Where(f => f.CreationTime < DateTime.Now.AddDays(-3)).ToList().ForEach(f => f.Delete());
            CopyLog(logs.FullName);
        }
        public static string CopyLog(string path)
        {
            string t = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");
            string subver = CredentialsPatch.Subver.RemoveHtmlTags();
            if (subver != "") subver = $"({subver})";
            string fileName = $"{path}/TownOfHost_K-v{Main.PluginVersion}{subver}-{t}.log";
            FileInfo file = new(@$"{Environment.CurrentDirectory}/BepInEx/LogOutput.log");
            var logFile = file.CopyTo(fileName);
            return logFile.FullName;
        }
        public static void OpenLogFolder()
        {
            var logs = GetLogFolder(true);
            OpenDirectory(logs.FullName);
        }
        public static void OpenDirectory(string path)
        {
            Process.Start("Explorer.exe", $"/select,{path}");
        }
    }
    #endregion
    #region  GameLog
    public static class UtilsGameLog
    {
        public static int day;
        public static Dictionary<byte, string> LastLog = new();
        public static Dictionary<byte, string> LastLogRole = new();
        public static Dictionary<byte, string> LastLogPro = new();
        public static Dictionary<byte, string> LastLogSubRole = new();
        public static Dictionary<byte, string> LastLogLoveRole = new();
        public static string GetLogtext(byte pc)
        {
            var longestNameByteCount = Main.AllPlayerNames?.Values?.Select(name => name.GetByteCount())?.OrderByDescending(byteCount => byteCount)?.FirstOrDefault() ?? 10;
            var pos = Math.Min(((float)longestNameByteCount / 2) + 1.5f, 11.5f);
            var pos1 = pos + 4f;
            var pos2 = pos + 4f + (DestroyableSingleton<TranslationController>.Instance.currentLanguage.languageID == SupportedLangs.English ? 8f : 4.5f);

            var name = LastLog.TryGetValue(pc, out var log) ? log : "???";
            var pro = LastLogPro.TryGetValue(pc, out var prog) ? prog : "(????)";
            var role = LastLogRole.TryGetValue(pc, out var rolelog) ? rolelog : "??????";
            var addon = "??";
            addon = LastLogLoveRole.TryGetValue(pc, out var m) ? m : "";// : GetSubRolesText(pc, mark: true);

            return name + pro + " : " + GetVitalText(pc, true) + " " + role + addon;
        }
        public static string SummaryTexts(byte id)
        {
            var builder = new StringBuilder();
            // 全プレイヤー中最長の名前の長さからプレイヤー名の後の水平位置を計算する
            // 1em ≒ 半角2文字
            // 空白は0.5emとする
            // SJISではアルファベットは1バイト，日本語は基本的に2バイト
            var longestNameByteCount = Main.AllPlayerNames.Values.Select(name => name.GetByteCount()).OrderByDescending(byteCount => byteCount).FirstOrDefault();
            //最大11.5emとする(★+日本語10文字分+半角空白)
            var pos = Math.Min(((float)longestNameByteCount / 2) + 1.5f /* ★+末尾の半角空白 */ , 11.5f);
            builder.Append(ColorString(Main.PlayerColors[id], Main.AllPlayerNames[id]));
            builder.AppendFormat("<pos={0}em>", pos).Append(GetProgressText(id, ShowManegementText: false, gamelog: true)).Append("</pos>");
            // "(00/00) " = 4em
            pos += 6f;
            builder.AppendFormat("<pos={0}em>", pos).Append(GetVitalText(id, true)).Append("</pos>");
            // "Lover's Suicide " = 8em
            // "回線切断 " = 4.5em
            pos += DestroyableSingleton<TranslationController>.Instance.currentLanguage.languageID == SupportedLangs.English ? 8.5f : 5f;
            builder.AppendFormat("<pos={0}em>", pos);
            var role = GetTrueRoleName(id);
            if (LastLogRole.ContainsKey(id))
                role = LastLogRole[id];
            role = Regex.Replace(role, "<b>", "");
            role = Regex.Replace(role, "</b>", "");
            builder.Append(role);
            builder.Append(LastLogSubRole.TryGetValue(id, out var m) ? m : GetSubRolesText(id, mark: true));
            builder.Append("</pos>");
            return builder.ToString();
        }
        /// <summary> WinnerTextを生成します </summary>
        /// <returns>CustomWinnerText,CustomWinnerColor,WinText,barColor,winColorの順に返します</returns>
        public static (string, string, string, Color, Color) GetWinnerText(string WinText = "", Color barColor = new Color(), Color winColor = new Color(), List<byte> winnerList = null)
        {
            string CustomWinnerText = "";
            StringBuilder AdditionalWinnerText = new(32);
            var CustomWinnerColor = GetRoleColorCode(CustomRoles.Crewmate);

            winnerList ??= Main.winnerList;

            if ((int)CustomWinnerHolder.WinnerTeam < 1000)
            {
                var winnerRole = (CustomRoles)CustomWinnerHolder.WinnerTeam;
                if (winnerRole >= 0)
                {
                    CustomWinnerText = GetRoleName(winnerRole);
                    CustomWinnerColor = GetRoleColorCode(winnerRole);
                    if (winnerRole.IsNeutral())
                    {
                        barColor = GetRoleColor(winnerRole);
                    }
                }
            }
            if (AmongUsClient.Instance.AmHost && PlayerState.GetByPlayerId(0).MainRole == CustomRoles.GM)
            {
                WinText = "Game Over";
                winColor =
                barColor = GetRoleColor(CustomRoles.GM);
            }
            switch (CustomWinnerHolder.WinnerTeam)
            {
                //通常勝利
                case CustomWinner.Crewmate:
                    barColor = Palette.CrewmateBlue;
                    CustomWinnerColor = GetRoleColorCode(CustomRoles.Crewmate);
                    break;
                //特殊勝利
                case CustomWinner.Terrorist: barColor = Color.red; break;
                case CustomWinner.Lovers: barColor = GetRoleColor(CustomRoles.Lovers); break;
                case CustomWinner.RedLovers: barColor = GetRoleColor(CustomRoles.RedLovers); break;
                case CustomWinner.YellowLovers: barColor = GetRoleColor(CustomRoles.YellowLovers); break;
                case CustomWinner.BlueLovers: barColor = GetRoleColor(CustomRoles.BlueLovers); break;
                case CustomWinner.GreenLovers: barColor = GetRoleColor(CustomRoles.GreenLovers); break;
                case CustomWinner.WhiteLovers: barColor = GetRoleColor(CustomRoles.WhiteLovers); break;
                case CustomWinner.PurpleLovers: barColor = GetRoleColor(CustomRoles.PurpleLovers); break;
                case CustomWinner.MadonnaLovers: barColor = GetRoleColor(CustomRoles.MadonnaLovers); break;
                case CustomWinner.OneLove: CustomWinnerText = ColorString(GetRoleColor(CustomRoles.OneLove), GetString("OneLoveWin")); barColor = GetRoleColor(CustomRoles.OneLove); break;
                case CustomWinner.MilkyWay: var MilkyWayColor = StringHelper.CodeColor(Roles.Neutral.Vega.TeamColor); CustomWinnerText = ColorString(MilkyWayColor, GetString("TeamMilkyWay")); barColor = MilkyWayColor; break;
                case CustomWinner.TaskPlayerB:
                    if (winnerList.Count is 0) break;
                    if (winnerList.Count == 1)
                        if (Main.RTAMode)
                        {
                            WinText = "Game Over";
                            CustomWinnerText = $"タイム: {HudManagerPatch.GetTaskBattleTimer().Replace(" : ", "：")}<size=0>";
                        }
                        else
                            CustomWinnerText = Main.AllPlayerNames[winnerList[0]];
                    else
                    {
                        foreach (var (team, players) in TaskBattle.TaskBattleTeams)
                        {
                            if (players.Contains(winnerList[0]))
                            {
                                CustomWinnerText = string.Format(GetString("Team2"), team);
                                break;
                            }
                        }
                    }
                    break;
                case CustomWinner.SuddenDeathRed:
                    WinText = "Game Over";
                    winColor = ModColors.Red;
                    barColor = ModColors.Red;
                    CustomWinnerText = GetString("SuddenDeathRed");
                    CustomWinnerColor = ModColors.codered;
                    break;

                case CustomWinner.SuddenDeathBlue:
                    WinText = "Game Over";
                    winColor = ModColors.Blue;
                    barColor = ModColors.Blue;
                    CustomWinnerText = GetString("SuddenDeathBlue");
                    CustomWinnerColor = ModColors.codeblue;
                    break;

                case CustomWinner.SuddenDeathYellow:
                    WinText = "Game Over";
                    winColor = ModColors.Yellow;
                    barColor = ModColors.Yellow;
                    CustomWinnerText = GetString("SuddenDeathYellow");
                    CustomWinnerColor = ModColors.codeyellow;
                    break;

                case CustomWinner.SuddenDeathGreen:
                    WinText = "Game Over";
                    winColor = ModColors.Green;
                    barColor = ModColors.Green;
                    CustomWinnerText = GetString("SuddenDeathGreen");
                    CustomWinnerColor = ModColors.codegreen;
                    break;

                case CustomWinner.SuddenDeathPurple:
                    WinText = "Game Over";
                    winColor = ModColors.Purple;
                    barColor = ModColors.Purple;
                    CustomWinnerText = GetString("SuddenDeathPurple");
                    CustomWinnerColor = ModColors.codepurple;
                    break;
                //廃村処理
                case CustomWinner.Draw:
                    WinText = GetString("ForceEnd");
                    winColor = Color.white;
                    barColor = Color.gray;
                    CustomWinnerText = GetString("ForceEndText");
                    CustomWinnerColor = StringHelper.ColorCode(Color.gray);
                    break;
                //全滅
                case CustomWinner.None:
                    WinText = "";
                    winColor = Color.black;
                    barColor = Color.gray;
                    CustomWinnerText = GetString("EveryoneDied");
                    CustomWinnerColor = StringHelper.ColorCode(Color.gray);
                    break;
            }
            if (CustomWinnerHolder.WinnerTeam is not CustomWinner.None and not CustomWinner.Draw)
                if (SuddenDeathMode.NowSuddenDeathMode && !SuddenDeathMode.NowSuddenDeathTemeMode)
                {
                    var winnerRole = (CustomRoles)CustomWinnerHolder.WinnerTeam;
                    var winner = CustomWinnerHolder.WinnerIds.FirstOrDefault();
                    var color = Color.white;
                    if (Main.PlayerColors.TryGetValue(winner, out var co)) color = co;
                    WinText = "Game Over";
                    winColor = color;
                    barColor = color;
                    var name = "";
                    if (Main.AllPlayerNames.ContainsKey(winner)) name = ColorString(Main.PlayerColors[winner], Main.AllPlayerNames[winner]);
                    CustomWinnerText = "<size=60%>" + name + ColorString(GetRoleColor(winnerRole), $"({GetRoleName(winnerRole)})") + $"{GetString("Win")}</size>";
                }

            foreach (var role in CustomWinnerHolder.AdditionalWinnerRoles)
            {
                AdditionalWinnerText.Append('＆').Append(ColorString(GetRoleColor(role), GetRoleName(role)));
            }
            if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Draw and not CustomWinner.None and not CustomWinner.OneLove && !SuddenDeathMode.NowSuddenDeathMode)
            {
                CustomWinnerText = $"<{CustomWinnerColor}>{CustomWinnerText}{AdditionalWinnerText}{GetString("Win")}</color>";
            }
            else
            {
                CustomWinnerText = $"<{CustomWinnerColor}>{CustomWinnerText}";
            }

            return (CustomWinnerText, CustomWinnerColor, WinText, barColor, winColor);
        }
        public static StringBuilder GetRTAText(List<byte> winnerList = null)
        {
            var AddCommon = TaskBattle.NumCommonTasks.GetInt() * TaskBattle.MaxAddCount.GetInt();
            var AddLong = TaskBattle.NumLongTasks.GetInt() * TaskBattle.MaxAddCount.GetInt();
            var AddShort = TaskBattle.NumShortTasks.GetInt() * TaskBattle.MaxAddCount.GetInt();
            if (!TaskBattle.TaskAddMode.GetBool())
                (AddCommon, AddLong, AddShort) = (0, 0, 0);
            StringBuilder sb = new();
            winnerList ??= Main.winnerList;

            if (!(CustomWinnerHolder.WinnerTeam is CustomWinner.None or CustomWinner.Draw))
                sb.Append($"{GetString("TaskPlayerB")}:\n　{Main.AllPlayerNames[winnerList[0]] ?? "?"}");

            sb.Append($"\n{GetString("TaskCount")}:")
            .Append($"\n　{GetString(StringNames.GameCommonTasks)}: {Main.NormalOptions.NumCommonTasks + AddCommon}")
            .Append($"\n　{GetString(StringNames.GameShortTasks)}: {Main.NormalOptions.NumShortTasks + AddLong}")
            .Append($"\n　{GetString(StringNames.GameLongTasks)}: {Main.NormalOptions.NumLongTasks + AddShort}")
            .Append($"\nタイム: {HudManagerPatch.GetTaskBattleTimer()}")
            .Append($"\nMap: {(MapNames)Main.NormalOptions.MapId}")
            .Append($"\nVent: " + (TaskBattle.TaskBattleCanVent.GetBool() ? "On" : "Off"));//マップの設定なども記載しなければならない
            if (TaskBattle.TaskBattleCanVent.GetBool())
                sb.Append($"\n　クールダウン:{TaskBattle.TaskBattleVentCooldown.GetFloat()}");
            return sb;
        }
        public static void ShowLastResult(byte PlayerId = byte.MaxValue)
        {
            if (AmongUsClient.Instance.IsGameStarted)
            {
                SendMessage(GetString("CantUse.lastresult"), PlayerId);
                return;
            }
            var sb = new StringBuilder();

            var winnerColor = ((CustomRoles)CustomWinnerHolder.WinnerTeam).GetRoleInfo()?.RoleColor ?? GetRoleColor((CustomRoles)CustomWinnerHolder.WinnerTeam);

            sb.Append("""<align="center">""");
            sb.Append("<size=150%>").Append(GetString("LastResult")).Append("</size>");
            sb.Append('\n').Append(SetEverythingUpPatch.LastWinsText.Mark(winnerColor, false));
            sb.Append("</align>");

            sb.Append("<size=65%>\n");
            List<byte> cloneRoles = new(PlayerState.AllPlayerStates.Keys);

            foreach (var pc in cloneRoles) if (GetPlayerById(pc) == null) continue;

            var count = 0;
            foreach (var id in Main.winnerList)
            {
                sb.Append($"\n★ ".Color(winnerColor)).Append(GetLogtext(id));
                cloneRoles.Remove(id);
                CheckMeg();
                count++;
            }
            foreach (var id in cloneRoles)
            {
                sb.Append($"\n　 ").Append(GetLogtext(id));
                CheckMeg();
                count++;
            }
            sb.Append("</color>   ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~\n");
            sb.Append(string.Format(GetString("Result.Task"), Main.Alltask));
            SendMessage(sb.ToString().RemoveDeltext("<b>").RemoveDeltext("</b>"), PlayerId);

            void CheckMeg()
            {
                if (sb.Length > 700 && PlayerState.AllPlayerStates.Keys.Count() / 1.8f < count)
                {
                    count = 0;
                    SendMessage(sb.ToString().RemoveDeltext("<b>").RemoveDeltext("</b>"), PlayerId);
                    sb.Clear();
                    sb.Append("<size=65%>");
                }
            }
        }
        public static void ShowLastWins(byte PlayerId = byte.MaxValue)
        {
            SendMessage("<size=0>", PlayerId, $"{SetEverythingUpPatch.LastWinsText}");
        }
        public static void ShowKillLog(byte PlayerId = byte.MaxValue)
        {
            if (GameStates.IsInGame)
            {
                SendMessage(GetString("CantUse.killlog"), PlayerId);
                return;
            }
            var mes = new StringBuilder();
            mes.Append($"{GetString("GameLog")}\n{gamelog}");
            var last = GameLog.Values.LastOrDefault();
            foreach (var log in GameLog)
            {
                mes.Append(log.Value);

                if ((last ?? "??") == log.Value)
                {
                    var meg = GetString($"{(CustomRoles)CustomWinnerHolder.WinnerTeam}") + GetString("Team") + GetString("Win");
                    var winnerColor = ((CustomRoles)CustomWinnerHolder.WinnerTeam).GetRoleInfo()?.RoleColor ?? GetRoleColor((CustomRoles)CustomWinnerHolder.WinnerTeam);

                    switch (CustomWinnerHolder.WinnerTeam)
                    {
                        case CustomWinner.Draw: meg = GetString("ForceEnd"); break;
                        case CustomWinner.None: meg = GetString("EveryoneDied"); break;
                        case CustomWinner.SuddenDeathRed: meg = GetString("SuddenDeathRed"); winnerColor = ModColors.Red; break;
                        case CustomWinner.SuddenDeathBlue: meg = GetString("SuddenDeathBlue"); winnerColor = ModColors.Blue; break;
                        case CustomWinner.SuddenDeathYellow: meg = GetString("SuddenDeathYellow"); winnerColor = ModColors.Yellow; break;
                        case CustomWinner.SuddenDeathGreen: meg = GetString("SuddenDeathGreen"); winnerColor = ModColors.Green; break;
                        case CustomWinner.SuddenDeathPurple: meg = GetString("SuddenDeathPurple"); winnerColor = ModColors.Purple; break;
                    }

                    var Star = "★";
                    var send = mes.ToString() + "\n\n" + $"{Star}{meg}{Star}".Color(winnerColor);
                    SendMessage(send.RemoveDeltext("<b>").RemoveDeltext("</b>"), PlayerId);
                    break;
                }

                if (mes.Length > 500 && log.Key < day)
                {
                    SendMessage(mes.ToString().RemoveDeltext("<b>").RemoveDeltext("</b>"), PlayerId);
                    mes = mes.Clear();
                    mes.Append("<size=60%>");
                }
            }
            //SendMessage(/*EndGamePatch.KillLog*/, PlayerId);
        }
        public static void Reset()
        {
            Main.showkillbutton = false;
            day = 1;
            Main.ShowRoleIntro = true;
            Main.IsActiveSabotage = false;
            Main.ForcedGameEndColl = 0;
            Main.GameCount++;
            Main.CanUseAbility = false;
            GameLog = new();
            TodayLog = "";
            var startgametext = string.Format(GetString("log.Start"), Main.GameCount);
            AddGameLogsub($"<size=60%>{DateTime.Now:HH.mm.ss} [Start]{startgametext}\n" + string.Format(GetString("Message.Day").RemoveDeltext("【").RemoveDeltext("】"), day).Color(Palette.Orange));
        }
        public static void WriteGameLog()
        {
            if (!GameLog.TryAdd(day, TodayLog))
            {
                Logger.Info("なんかTryAddが失敗に終わったよ!", "WriteGameLog");
            }
            else if (GameLog.TryGetValue(day, out var Oldlog))
            {
                if (Oldlog != TodayLog)
                {
                    GameLog[day] = Oldlog + TodayLog;
                }
            }
            TodayLog = "";
        }
        public static string gamelog;
        static string TodayLog;
        public static Dictionary<int, string> GameLog = new();
        public static void AddGameLog(string Name, string Meg) => TodayLog += $"\n[{Name}]　" + Meg;
        public static void AddGameLogsub(string Meg) => TodayLog += Meg;
    }
    #endregion
    #region WebHook
    public static class UtilsWebHook
    {
        public static void WH_ShowActiveRoles()
        {
            StringBuilder sb;
            if (Options.CurrentGameMode == CustomGameMode.Standard)
            {

                sb = new StringBuilder("```cs\n").Append(GetString("Roles")).Append(':');
                sb.AppendFormat("\n# {0}:{1}", GetRoleName(CustomRoles.GM), Options.EnableGM.GetString().RemoveHtmlTags());
                CustomRoleTypes? rr = null;//☆インポスターも表示させるため
                foreach (CustomRoles role in EnumHelper.GetAllValues<CustomRoles>())
                {
                    if (role is CustomRoles.GM or CustomRoles.NotAssigned) continue;
                    if (rr == null && rr != role.GetCustomRoleTypes() && role.IsEnable())
                    {
                        rr = role.GetCustomRoleTypes();
                        if (role.IsSubRole())
                            sb.AppendFormat($"\n☆{GetString("Addons")}");
                        else
                        {
                            sb.AppendFormat($"\n☆{GetString($"{rr}")}");
                        }
                    }
                    if (role is CustomRoles.HASFox or CustomRoles.HASTroll) continue;
                    var mark = "〇";
                    switch (role.GetCustomRoleTypes())
                    {
                        case CustomRoleTypes.Impostor: mark = "Ⓘ"; break;
                        case CustomRoleTypes.Crewmate: mark = "Ⓒ"; break;
                        case CustomRoleTypes.Madmate: mark = "Ⓜ"; break;
                        case CustomRoleTypes.Neutral: mark = "Ⓝ"; break;
                    }
                    if (role.IsSubRole())
                    {
                        if (role.IsBuffAddon()) mark = "Ⓐ";
                        else if (role.IsDebuffAddon()) mark = "Ⓓ";
                        else if (role.IsLovers()) mark = "Ⓛ";
                        else if (role.IsGhostRole()) mark = "Ⓖ";
                        else mark = "〇";
                    }
                    if (role.IsEnable()) sb.AppendFormat($"\n {mark}" + "\"{0}\"   {1}×{2}", role.GetCombinationName(false), $"{role.GetChance()}%", role.GetCount());
                }
            }
            else
            {
                sb = new StringBuilder("```cs\n").Append(GetString(Options.CurrentGameMode.ToString()));
                sb.Append("\n\n").Append(GetString("TaskPlayerB") + ":");
                foreach (var pc in PlayerCatch.AllPlayerControls)
                    sb.Append("\n  " + pc.name);
            }
            sb.Append("\n```");
            Webhook.Send(sb.ToString());
        }
        public static void WH_ShowLastResult()
        {
            var sb = new StringBuilder();

            var winnerColor = ((CustomRoles)CustomWinnerHolder.WinnerTeam).GetRoleInfo()?.RoleColor ?? Palette.DisabledGrey;
            var tb = Options.CurrentGameMode == CustomGameMode.TaskBattle;
            sb.Append(GetString("LastResult"));
            if (!tb) sb.Append("\n## ").Append(SetEverythingUpPatch.LastWinsText).Append('\n');
            List<byte> cloneRoles = new(PlayerState.AllPlayerStates.Keys);
            foreach (var id in Main.winnerList)
            {
                var sr = GetSubRolesText(id).RemoveColorTags() != "";
                sb.Append("\n**★ ").Append(Main.AllPlayerNames[id] + "**");
                if (!tb)
                {
                    sb.Append("\n┣  ").Append(GetVitalText(id)).Append('　');
                    sb.Append(GetProgressText(id, ShowManegementText: false, gamelog: true).RemoveColorTags());
                    sb.Append(sr ? "\n┣  " : "\n┗   ").Append(GetTrueRoleName(id, false).RemoveColorTags());
                    if (sr) sb.Append("\n┗  ").Append(GetSubRolesText(id).RemoveColorTags());
                }
                else
                {
                    sb.Append('\n').Append($"{Main.AllPlayerNames[id]}{GetString("Win")}").Append('\n');
                    sb.Append('　').Append(GetProgressText(id, ShowManegementText: false, gamelog: true).RemoveColorTags());
                }
                cloneRoles.Remove(id);
            }
            foreach (var id in cloneRoles)
            {
                var sr = GetSubRolesText(id).RemoveColorTags() != "";
                sb.Append("\n〇 ").Append(Main.AllPlayerNames[id]);
                if (!tb)
                {
                    sb.Append("\n┣  ").Append(GetVitalText(id)).Append('　');
                    sb.Append(GetProgressText(id, ShowManegementText: false, gamelog: true).RemoveColorTags());
                    sb.Append(sr ? "\n┣  " : "\n┗   ").Append(GetTrueRoleName(id, false).RemoveColorTags());
                    if (sr) sb.Append("\n┗  ").Append(GetSubRolesText(id).RemoveColorTags());
                }
                else
                    sb.Append('　').Append(GetProgressText(id, gamelog: true).RemoveColorTags());
            }
            Webhook.Send(sb.ToString());
            Webhook.Send($"```\n{EndGamePatch.KillLog.RemoveHtmlTags()}\n```");
        }
    }
}
#endregion