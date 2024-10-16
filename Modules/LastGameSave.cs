using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TownOfHost.Attributes;
using TownOfHost.Roles.Core;
using static TownOfHost.Translator;
using static TownOfHost.Utils;

namespace TownOfHost.Modules;

public static class LastGameSave
{
    private static readonly string PATH = new("./TOHK_DATA/LastGameResult.txt");

    [PluginModuleInitializer]
    public static void Init()
    {
        CreateIfNotExists(true);
    }

    public static void CreateIfNotExists(bool sakujo = false, bool oti = false)
    {
        if (!File.Exists(PATH))
        {
            try
            {
                if (!Directory.Exists(@"TOHK_DATA")) Directory.CreateDirectory(@"TOHK_DATA");
                if (File.Exists(@"./LastGameResult.txt"))
                {
                    File.Move(@"./LastGameResult.txt", PATH);
                    if (sakujo)
                    {
                        File.WriteAllText(PATH, "");
                        return;
                    }
                    File.WriteAllText(PATH, EndGamePatch.outputLog.RemoveHtmlTags() + Log());
                }
                else
                {
                    if (sakujo)
                    {
                        File.WriteAllText(PATH, "");
                        return;
                    }
                    File.WriteAllText(PATH, EndGamePatch.outputLog.RemoveHtmlTags() + Log());
                }
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "LastGameResult");
            }
        }
        else
        {
            if (!Directory.Exists(@"TOHK_DATA")) Directory.CreateDirectory(@"TOHK_DATA");
            if (File.Exists(@"./LastGameResult.txt"))
            {
                if (sakujo)
                {
                    File.WriteAllText(PATH, "");
                    return;
                }
                if (Main.GameCount <= 1)
                {
                    File.AppendAllText(PATH, EndGamePatch.outputLog.RemoveHtmlTags() + Log());
                    return;
                }
                File.AppendAllText(PATH, "\n" + EndGamePatch.outputLog.RemoveHtmlTags() + Log());
            }
            else
            {
                if (sakujo)
                {
                    File.WriteAllText(PATH, "");
                    return;
                }
                if (Main.GameCount <= 1)
                {
                    File.AppendAllText(PATH, EndGamePatch.outputLog.RemoveHtmlTags() + Log());
                    return;
                }
                File.AppendAllText(PATH, "\n" + EndGamePatch.outputLog.RemoveHtmlTags() + Log());
            }
        }
        string Log()
        {
            var sb = new StringBuilder();

            var winnerColor = ((CustomRoles)CustomWinnerHolder.WinnerTeam).GetRoleInfo()?.RoleColor ?? GetRoleColor((CustomRoles)CustomWinnerHolder.WinnerTeam);
            if (oti)
            {
                sb.Append("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~\n" + Main.gamelog + "\n\n<b>" + "</b>");
            }

            sb.Append("""<align="center">""");
            sb.Append("<size=150%>").Append(GetString("LastResult")).Append("</size>");
            sb.Append('\n').Append(SetEverythingUpPatch.LastWinsText.Mark(winnerColor, false));
            sb.Append("</align>");

            sb.Append("<size=70%>\n");
            List<byte> cloneRoles = new(PlayerState.AllPlayerStates.Keys);

            foreach (var pc in cloneRoles) if (GetPlayerById(pc) == null) continue;

            foreach (var id in Main.winnerList)
            {
                sb.Append($"\n★ ".Color(winnerColor)).Append(GetLogtext(id));
                cloneRoles.Remove(id);
            }
            foreach (var id in cloneRoles)
            {
                sb.Append($"\n　 ").Append(GetLogtext(id));
            }
            sb.Append("\n\n");
            sb.Append(string.Format(GetString("Result.Task"), Main.Alltask));
            return "\n\n" + sb.ToString().RemoveHtmlTags();
        }
    }
}