using System.Collections.Generic;

namespace TownOfHost;

static class NomalAchievement
{
    public static Dictionary<int, Achievement> achievements = new();
    public static Dictionary<NomalAchievementType, List<Achievement>> typeachievement = new();
    [Attributes.PluginModuleInitializer]
    public static void Load()
    {
        /*
        var n1 = new Achievement(NomalAchievementType.AllRole1, 0, 1, 0, 0);
        var l1 = new Achievement(NomalAchievementType.AllRole1, 1, 1, 0, 1);
        var sp1 = new Achievement(NomalAchievementType.AllRole1, 2, 1, 0, 2);
        */
    }

    public static string GetButtonName(this NomalAchievementType type)
    {
        switch (type)
        {
            case NomalAchievementType.AllRole1:
                return $"<{Main.ModColor}>Mod</color>";
            case NomalAchievementType.KillerRole1:
                return $"<#e7959a>Killer</color>";
            case NomalAchievementType.ImpostorRole1:
                return $"<#ff1919>Impostor</color>";
            case NomalAchievementType.CrewmateRole1:
                return $"<#8cffff>Crewmate</color>";
            case NomalAchievementType.MadmateRole1:
                return "<#ff7f50>Madmate</color>";
            case NomalAchievementType.NeutralRole1:
                return "<#cccccc>Neutral</color>";
            case NomalAchievementType.Other:
                return $"<#17f7aa>Other</color>";
            default: return "";
        }
    }
}

public enum NomalAchievementType
{
    AllRole1,
    KillerRole1,
    ImpostorRole1,
    MadmateRole1,
    CrewmateRole1,
    NeutralRole1,
    Other,
}