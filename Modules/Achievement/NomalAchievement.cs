using System.Collections.Generic;

namespace TownOfHost;

static class NomalAchievement
{
    public static Dictionary<int, Achievement> achievements = new();
    public static Dictionary<NomalAchievementType, List<Achievement>> typeachievement = new();
    [Attributes.PluginModuleInitializer]
    public static void Load()
    {
        // Mod 01
        new Achievement(NomalAchievementType.Standard1, 0, 1, 0, 3);
        new Achievement(NomalAchievementType.Standard1, 1, 1, 0, 0);
        new Achievement(NomalAchievementType.Standard1, 2, 1, 0, 0);
        new Achievement(NomalAchievementType.Standard1, 3, 1, 0, 1);
        new Achievement(NomalAchievementType.Standard1, 4, 1, 0, 1);
        new Achievement(NomalAchievementType.Standard1, 5, 1, 0, 2, true);
        new Achievement(NomalAchievementType.Standard1, 6, 1, 0, 2, true);
        new Achievement(NomalAchievementType.Standard1, 7, 1, 0, 2, true);
        new Achievement(NomalAchievementType.Standard1, 8, 1, 0, 2, true);
        new Achievement(NomalAchievementType.Standard1, 9, 1, 0, 3, true);
        new Achievement(NomalAchievementType.Standard1, 10, 1, 0, 3, true);

        new Achievement(NomalAchievementType.Standard1, 11, 1, 0, 0);
        new Achievement(NomalAchievementType.Standard1, 12, 1, 0, 0);
        new Achievement(NomalAchievementType.Standard1, 13, 1, 0, 1);
        new Achievement(NomalAchievementType.Standard1, 14, 1, 0, 2);
        new Achievement(NomalAchievementType.Standard1, 15, 1, 0, 3, true);

        // HnS
        new Achievement(NomalAchievementType.HideAndSeek, 0, 1, 0, 0);
        new Achievement(NomalAchievementType.HideAndSeek, 1, 1, 0, 0);
        new Achievement(NomalAchievementType.HideAndSeek, 2, 1, 0, 1);
        new Achievement(NomalAchievementType.HideAndSeek, 3, 1, 0, 2);
        new Achievement(NomalAchievementType.HideAndSeek, 4, 1, 0, 3, true);

        new Achievement(NomalAchievementType.HideAndSeek, 5, 1, 0, 0);
        new Achievement(NomalAchievementType.HideAndSeek, 6, 3, 0, 0);
        new Achievement(NomalAchievementType.HideAndSeek, 7, 10, 0, 1);
        new Achievement(NomalAchievementType.HideAndSeek, 8, 30, 0, 1);
        new Achievement(NomalAchievementType.HideAndSeek, 9, 50, 0, 2);

        new Achievement(NomalAchievementType.HideAndSeek, 10, 1, 0, 0);
        new Achievement(NomalAchievementType.HideAndSeek, 11, 3, 0, 0);
        new Achievement(NomalAchievementType.HideAndSeek, 12, 5, 0, 0);
        new Achievement(NomalAchievementType.HideAndSeek, 13, 10, 0, 1);
        new Achievement(NomalAchievementType.HideAndSeek, 14, 30, 0, 2);

        new Achievement(NomalAchievementType.HideAndSeek, 15, 5, 0, 0);
        new Achievement(NomalAchievementType.HideAndSeek, 16, 50, 0, 1);
        new Achievement(NomalAchievementType.HideAndSeek, 17, 100, 0, 2);

        // Sudden
        new Achievement(NomalAchievementType.SuddenDeath, 0, 1, 0, 0);
        new Achievement(NomalAchievementType.SuddenDeath, 1, 1, 0, 0);
        new Achievement(NomalAchievementType.SuddenDeath, 2, 1, 0, 1);
        new Achievement(NomalAchievementType.SuddenDeath, 3, 1, 0, 2);
        new Achievement(NomalAchievementType.SuddenDeath, 4, 1, 0, 3, true);

        new Achievement(NomalAchievementType.SuddenDeath, 5, 1, 0, 0);
        new Achievement(NomalAchievementType.SuddenDeath, 6, 1, 0, 1);
        new Achievement(NomalAchievementType.SuddenDeath, 7, 1, 0, 2);

        new Achievement(NomalAchievementType.SuddenDeath, 8, 1, 0, 0);
        new Achievement(NomalAchievementType.SuddenDeath, 9, 1, 0, 0);
        new Achievement(NomalAchievementType.SuddenDeath, 10, 1, 0, 0);
        new Achievement(NomalAchievementType.SuddenDeath, 11, 1, 0, 0);
        new Achievement(NomalAchievementType.SuddenDeath, 12, 1, 0, 0);

        //Task
        new Achievement(NomalAchievementType.TaskBattle, 0, 1, 0, 0);
        new Achievement(NomalAchievementType.TaskBattle, 1, 1, 0, 0);
        new Achievement(NomalAchievementType.TaskBattle, 2, 1, 0, 1);

        new Achievement(NomalAchievementType.TaskBattle, 3, 1, 0, 0);
        new Achievement(NomalAchievementType.TaskBattle, 4, 1, 0, 1);
        new Achievement(NomalAchievementType.TaskBattle, 5, 1, 0, 2);

        new Achievement(NomalAchievementType.TaskBattle, 6, 100, 0, 0);
        new Achievement(NomalAchievementType.TaskBattle, 7, 300, 0, 0);
        new Achievement(NomalAchievementType.TaskBattle, 8, 500, 0, 1);
        new Achievement(NomalAchievementType.TaskBattle, 9, 1000, 0, 2);
        new Achievement(NomalAchievementType.TaskBattle, 10, 2200, 0, 3, true);

        new Achievement(NomalAchievementType.TaskBattle, 11, 1, 0, 3, true);

        // MM
        new Achievement(NomalAchievementType.MurderMystery, 0, 1, 0, 0);
        new Achievement(NomalAchievementType.MurderMystery, 1, 1, 0, 0);
        new Achievement(NomalAchievementType.MurderMystery, 2, 1, 0, 1);

        new Achievement(NomalAchievementType.MurderMystery, 3, 1, 0, 0);
        new Achievement(NomalAchievementType.MurderMystery, 4, 5, 0, 0);
        new Achievement(NomalAchievementType.MurderMystery, 5, 50, 0, 1);

        new Achievement(NomalAchievementType.MurderMystery, 6, 1, 0, 0);
        new Achievement(NomalAchievementType.MurderMystery, 7, 3, 0, 1);
        new Achievement(NomalAchievementType.MurderMystery, 8, 30, 0, 2);

        //Killer
        new Achievement(NomalAchievementType.KillerRole1, 0, 1, 0, 0);
        new Achievement(NomalAchievementType.KillerRole1, 1, 1, 0, 0);
        new Achievement(NomalAchievementType.KillerRole1, 2, 1, 0, 0);
        new Achievement(NomalAchievementType.KillerRole1, 3, 1, 0, 1);
        new Achievement(NomalAchievementType.KillerRole1, 4, 1, 0, 2);
        new Achievement(NomalAchievementType.KillerRole1, 5, 1, 0, 3, true);

        new Achievement(NomalAchievementType.KillerRole1, 6, 1, 0, 0);

        new Achievement(NomalAchievementType.KillerRole1, 7, 1, 0, 0);
        new Achievement(NomalAchievementType.KillerRole1, 8, 1, 0, 1);
        new Achievement(NomalAchievementType.KillerRole1, 9, 1, 0, 2);

        new Achievement(NomalAchievementType.KillerRole1, 10, 3, 0, 0);
        new Achievement(NomalAchievementType.KillerRole1, 11, 3, 0, 0);
        new Achievement(NomalAchievementType.KillerRole1, 12, 3, 0, 0);

        new Achievement(NomalAchievementType.KillerRole1, 13, 1, 0, 1);
        new Achievement(NomalAchievementType.KillerRole1, 14, 1, 0, 2);

        new Achievement(NomalAchievementType.KillerRole1, 15, 1, 0, 0);

        new Achievement(NomalAchievementType.KillerRole1, 16, 1, 0, 0);
        new Achievement(NomalAchievementType.KillerRole1, 17, 1, 0, 1);
        new Achievement(NomalAchievementType.KillerRole1, 18, 1, 0, 2, true);

        new Achievement(NomalAchievementType.KillerRole1, 19, 1, 0, 2, true);

        //Imp
        new Achievement(NomalAchievementType.ImpostorRole1, 0, 1, 0, 0);
        new Achievement(NomalAchievementType.ImpostorRole1, 1, 1, 0, 0);
        new Achievement(NomalAchievementType.ImpostorRole1, 2, 1, 0, 0);
        new Achievement(NomalAchievementType.ImpostorRole1, 3, 1, 0, 1);
        new Achievement(NomalAchievementType.ImpostorRole1, 4, 1, 0, 2);

        new Achievement(NomalAchievementType.ImpostorRole1, 5, 1, 0, 1, true);

        new Achievement(NomalAchievementType.ImpostorRole1, 6, 1, 0, 0);
        new Achievement(NomalAchievementType.ImpostorRole1, 7, 1, 0, 1);
        new Achievement(NomalAchievementType.ImpostorRole1, 8, 1, 0, 0);

        //mad
        new Achievement(NomalAchievementType.MadmateRole1, 0, 1, 0, 0);
        new Achievement(NomalAchievementType.MadmateRole1, 1, 1, 0, 1);
        new Achievement(NomalAchievementType.MadmateRole1, 2, 1, 0, 2);

        new Achievement(NomalAchievementType.MadmateRole1, 3, 1, 0, 1);

        //crew
        new Achievement(NomalAchievementType.CrewmateRole1, 0, 1, 0, 0);
        new Achievement(NomalAchievementType.CrewmateRole1, 1, 1, 0, 0);
        new Achievement(NomalAchievementType.CrewmateRole1, 2, 1, 0, 0);
        new Achievement(NomalAchievementType.CrewmateRole1, 3, 1, 0, 1, true);
        new Achievement(NomalAchievementType.CrewmateRole1, 4, 1, 0, 2, true);

        new Achievement(NomalAchievementType.CrewmateRole1, 5, 1, 0, 0);
        new Achievement(NomalAchievementType.CrewmateRole1, 6, 1, 0, 0);
        new Achievement(NomalAchievementType.CrewmateRole1, 7, 1, 0, 1);
        new Achievement(NomalAchievementType.CrewmateRole1, 8, 1, 0, 0);
        new Achievement(NomalAchievementType.CrewmateRole1, 9, 1, 0, 2);

        //Neu
        new Achievement(NomalAchievementType.NeutralRole1, 0, 1, 0, 0);
        new Achievement(NomalAchievementType.NeutralRole1, 1, 1, 0, 0);
        new Achievement(NomalAchievementType.NeutralRole1, 2, 1, 0, 1);

        //other
        new Achievement(NomalAchievementType.Other, 0, 1, 0, 1);
        new Achievement(NomalAchievementType.Other, 1, 1, 0, 1);
        new Achievement(NomalAchievementType.Other, 2, 1, 0, 2);
        new Achievement(NomalAchievementType.Other, 3, 1, 0, 0);
    }

    public static string GetButtonName(this NomalAchievementType type)
    {
        return type switch
        {
            NomalAchievementType.Standard1 => $"<{Main.ModColor}>Standard</color>",
            NomalAchievementType.HideAndSeek => $"<#ff1919>Hide<#ffff00>And<#8cffff>Seek",
            NomalAchievementType.SuddenDeath => $"<#ff9966>SuddenDeath</color>",
            NomalAchievementType.TaskBattle => $"<#8cb8ff>TaskBattle</color>",
            NomalAchievementType.MurderMystery => $"<#1a389c>MurderMystery</color>",
            NomalAchievementType.KillerRole1 => $"<#e7959a>Killer</color>",
            NomalAchievementType.ImpostorRole1 => $"<#ff1919>Impostor</color>",
            NomalAchievementType.CrewmateRole1 => $"<#8cffff>Crewmate</color>",
            NomalAchievementType.MadmateRole1 => "<#ff7f50>Madmate</color>",
            NomalAchievementType.NeutralRole1 => "<#cccccc>Neutral</color>",
            NomalAchievementType.Other => $"<#17f7aa>Other</color>",
            _ => "",
        };
    }
}

public enum NomalAchievementType
{
    Standard1,
    HideAndSeek,
    SuddenDeath,
    TaskBattle,
    MurderMystery,
    KillerRole1,
    ImpostorRole1,
    MadmateRole1,
    CrewmateRole1,
    NeutralRole1,
    Other,
}