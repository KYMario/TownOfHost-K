using System.IO;
using UnityEngine;

namespace TownOfHost;

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