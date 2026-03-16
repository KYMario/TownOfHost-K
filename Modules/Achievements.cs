using System.Collections.Generic;
using System.IO;
using Hazel;
using TownOfHost.Attributes;
using TownOfHost.Roles.Core;
using UnityEngine;

namespace TownOfHost;

class Achievements
{
    public static List<Achievement> GameCompleteAchievement = new();
    public static Dictionary<Achievement, int> UpdateStatesAchievement = new();
    [GameModuleInitializer]
    public static void init()
    {
        UpdateStatesAchievement.Clear();
        GameCompleteAchievement.Clear();
        AllPlayerAchievements = new();
    }
    public static Dictionary<string, List<Achievement>> AllPlayerAchievements = new();
    public static void RpcCompleteAchievement(byte playerid, int flug, int id)
    {
        var acchievement = Achievement.AllAchievements.TryGetValue(id, out var ac) ? ac : null;
        if (acchievement is not null)
            RpcCompleteAchievement(playerid, flug, acchievement);
    }
    public static void RpcCompleteAchievement(byte playerid, int flug, Achievement achievement, int addstate = 1)
    {
        if (flug == 0)
        {
            var key = playerid.GetPlayerControl().GetClient()?.ProductUserId ?? $"{PlayerCatch.GetPlayerInfoById(playerid).GetLogPlayerName()}";
            if (AllPlayerAchievements.TryGetValue(key, out var list))
            {
                if (list.Contains(achievement) is false)
                {
                    list.Add(achievement);
                    AllPlayerAchievements[key] = list;
                }
                else return;//もう送信済みなら以下の処理を行わない
            }
            else
            {
                List<Achievement> achilist = [achievement];
                AllPlayerAchievements.Add(key, achilist);
            }
        }
        if (playerid == PlayerControl.LocalPlayer.PlayerId)
        {
            switch (flug)
            {
                case 0:
                    GameCompleteAchievement.Add(achievement);
                    break;
                case 1:
                    UpdateStatesAchievement.Add(achievement, addstate);
                    break;

            }
            return;
        }
        else if (playerid.GetPlayerControl().IsModClient() && AmongUsClient.Instance.AmHost)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.GetAchievement, SendOption.None, -1);
            writer.Write(playerid);
            writer.Write(flug);
            writer.Write(achievement.id);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
    }

    public static string ShowCompleteAchievement()
    {
        var text = "";
        foreach (var ach in GameCompleteAchievement)
        {
            text += GetAchievementNames(ach, "Title");
        }
        return text;
    }

    public static string GetAchievementNames(Achievement achievement, string key, int changetextflug = 0)
    {
        var name = $"{achievement.id}_{key}";
        if (Translator.achievementMaps.TryGetValue(name, out var n))
        {
            var text = n[Main.UseingJapanese ? 11 : 0];
            switch (changetextflug)
            {
                case 1: text = text.RemoveDeltext("<[^>]*?>", "???"); break;
                case 2: text = text.RemoveDeltext("<").RemoveDeltext(">"); break;
            }
            if (key == "Constraint" && text.StartsWith("-")) text = $"<i>{text}</i>";
            return text;
        }
        return "";
    }
    public static void UpdateAchievement()
    {
        if (Statistics.CheckAdd(false) is not "") return;
        foreach (var upac in UpdateStatesAchievement)
        {
            upac.Key.Updatestates(upac.Value);
            if (upac.Key.step <= upac.Key.states && upac.Key.IsCompleted is false)
                GameCompleteAchievement.Add(upac.Key);
        }
        foreach (var ac in GameCompleteAchievement)
        {
            ac.IsCompleted = true;
        }
    }
    public static string GetAllAchievement()
    {
        var text = "★Achievement\n";
        foreach (var achi in GameCompleteAchievement)
        {
            var mark = "";
            var color = "";
            switch (achi.Difficulty)
            {
                case 0: mark += "◎"; color = "<#674020>"; break;
                case 1: mark += "◆"; color = "<#aacbf7>"; break;
                case 2: mark += "★"; color = "<#ffea4e>"; break;
                case 3: mark += "ф"; color = "<#262750>"; break;
            }
            text += $"{color}{mark}" + "  ";
            text += $"～{GetAchievementNames(achi, "Title")}～</color>" + $"<size=60%>({UtilsRoleText.GetRoleColorAndtext(achi.role)})</size>";
            text += $"<size=60%>{GetAchievementNames(achi, "Info")}</size>";
            text += "\n";
        }
        return text;
    }
}

public class Achievement
{
    public static Dictionary<int, Achievement> AllAchievements = new();
    public CustomRoles role;
    public int id;
    public int step;
    public int states;
    public int Difficulty;
    public bool IsHidden;
    public bool IsCompleted;

    public Achievement(SimpleRoleInfo roleinfo, int id, int step, int states, int difficulty, bool IsHidden = false)
    {
        this.role = roleinfo.RoleName;
        this.id = roleinfo.ConfigId + id;
        this.step = step;
        this.states = states;
        this.IsHidden = IsHidden;
        Difficulty = difficulty;
        IsCompleted = false;
        if (!AllAchievements.TryAdd(this.id, this))
        {
            Logger.Error($"{this.id}({role}-{id})が重複してます", "Achievement");
        }
    }
    public void Updatestates(int addvalue = 1)
    {
        states += addvalue;
    }
    public void SetStates(int states, bool IsCompleted)
    {
        this.states = states;
        this.IsCompleted = IsCompleted;
    }
}

public class AchievementSaver
{
    private static readonly string PATH = new($"{Application.persistentDataPath}/TownOfHost_K/Achievement.txt");
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
            if (File.Exists($"{Application.persistentDataPath}/TownOfHost_K/Achievement.txt"))
            {
                File.Move($"{Application.persistentDataPath}/TownOfHost_K/Achievement.txt", PATH);
            }
            if (SaveStatistics.IsOldVersion) return;

            var text = "";
            foreach (var data in Achievement.AllAchievements)
            {
                if (text != "") text += "%";
                text += $"{data.Key}!{data.Value.states}!{(data.Value.IsCompleted is true ? 1 : 0)}";
            }
            File.WriteAllText(PATH, text);
        }
        catch
        {
            Logger.Error("Saveでエラー！", "Achievement");
        }
    }
    public static void Load()
    {
        try
        {
            SetLogFolder();
            if (File.Exists($"{Application.persistentDataPath}/TownOfHost_K/Achievement.txt"))
            {
                File.Move($"{Application.persistentDataPath}/TownOfHost_K/Achievement.txt", PATH);
            }
            else
            {
                File.WriteAllText(PATH, "");
            }

            string Text = File.ReadAllText(PATH);

            if (Text == "")
            {
                Logger.Info($"からぽ！", "AchievementSaver-Load");
                Save();
                return;
            }
            var ages = Text.Split("%");
            foreach (var age in ages)
            {
                try
                {
                    var achitext = age.Split("!");
                    if (Achievement.AllAchievements.TryGetValue(int.TryParse(achitext[0], out var a) ? a : -1, out var achievement))
                    {
                        var states = int.TryParse(achitext[1], out var s) ? s : 0;
                        var iscomp = int.TryParse(achitext[2], out var ic) ? ic : 0;
                        achievement.SetStates(s, iscomp is 1);
                    }
                }
                catch { }
            }
        }
        catch { }
    }
}