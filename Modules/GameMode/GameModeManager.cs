

using System.Collections.Generic;

namespace TownOfHost;

class GameModeManager
{
    /// <summary>
    /// 現在のゲームモードに応じたタグを取得します。
    /// </summary>
    /// <param name="gamemode"></param>
    /// <returns></returns>
    public static CustomOptionTags[] GetTags(CustomGameMode gamemode = CustomGameMode.All)
    {
        List<CustomOptionTags> result = [CustomOptionTags.All];

        switch (gamemode)
        {
            case CustomGameMode.Standard:
                result.Add(CustomOptionTags.Standard);
                result.Add(CustomOptionTags.Role);
                result.Add(CustomOptionTags.GameOption);
                result.Add(CustomOptionTags.OtherOption);
                break;
            case CustomGameMode.HideAndSeek:
                result.Add(CustomOptionTags.HideAndSeek);
                result.Add(CustomOptionTags.GameOption);
                result.Add(CustomOptionTags.OtherOption);
                break;
            case CustomGameMode.TaskBattle:
                result.Add(CustomOptionTags.TaskBattle);
                result.Add(CustomOptionTags.GameOption);
                result.Add(CustomOptionTags.OtherOption);
                break;
            case CustomGameMode.SuddenDeath:
                result.Add(CustomOptionTags.Standard);
                result.Add(CustomOptionTags.SuddenDeath);
                result.Add(CustomOptionTags.Role);
                result.Add(CustomOptionTags.GameOption);
                result.Add(CustomOptionTags.OtherOption);
                break;
            case CustomGameMode.StandardHAS:
                result.Add(CustomOptionTags.Standard);
                result.Add(CustomOptionTags.StandardHAS);
                result.Add(CustomOptionTags.Role);
                result.Add(CustomOptionTags.GameOption);
                result.Add(CustomOptionTags.OtherOption);
                break;
            case CustomGameMode.MurderMystery:
                result.Add(CustomOptionTags.MurderMystery);
                result.Add(CustomOptionTags.GameOption);
                result.Add(CustomOptionTags.OtherOption);
                break;
        }

        return result.ToArray();
    }

    public static bool IsStandardClass()
        => Options.CurrentGameMode is CustomGameMode.Standard or CustomGameMode.StandardHAS or CustomGameMode.SuddenDeath or CustomGameMode.MurderMystery;
}
