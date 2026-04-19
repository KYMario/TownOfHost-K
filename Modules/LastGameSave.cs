using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

using TownOfHost.Attributes;
using TownOfHost.Roles.Core;
using static TownOfHost.Translator;

namespace TownOfHost.Modules;

public static class LastGameSave
{
    private static readonly string PATH = new(Main.BaseDirectory + "/LastGameResult.txt");
    private static readonly DirectoryInfo ScreenShotFolder = new(Main.BaseDirectory + "/ScreenShots/");

    [PluginModuleInitializer]
    public static void Init()
    {
        CreateIfNotExists(true);
        if (!ScreenShotFolder.Exists)
        {
            ScreenShotFolder.Create();
        }
    }

    public static void CreateIfNotExists(bool delete = false, bool destroy = false)
    {
        if (!File.Exists(PATH))
        {
            try
            {
                if (!Directory.Exists(Main.BaseDirectory)) Directory.CreateDirectory(Main.BaseDirectory);
                if (File.Exists(@"./LastGameResult.txt"))
                {
                    File.Move(@"./LastGameResult.txt", PATH);
                    if (delete)
                    {
                        File.WriteAllText(PATH, "");
                        return;
                    }
                    File.WriteAllText(PATH, EndGamePatch.outputLog.RemoveHtmlTags() + Log());
                }
                else
                {
                    if (delete)
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
            if (!Directory.Exists(Main.BaseDirectory)) Directory.CreateDirectory(Main.BaseDirectory);
            if (File.Exists(@"./LastGameResult.txt"))
            {
                if (delete)
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
                if (delete)
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
            StringBuilder sb = new();
            if (TaskBattle.IsRTAMode && Options.CurrentGameMode == CustomGameMode.TaskBattle)
            {
                sb.Append(UtilsGameLog.GetRTAText());
                EndGamePatch.KillLog += $"<#D4AF37>~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~</color>{"★".Color(Palette.DisabledGrey)}\n" + sb.ToString().Replace("\n", "\n　") + $"\n{"★".Color(Palette.DisabledGrey)}<#D4AF37>~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~</color>{"★".Color(Palette.DisabledGrey)}";
            }
            else
            {
                sb.Append(GetString("RoleSummaryText"));
                List<byte> cloneRoles = new(PlayerState.AllPlayerStates.Keys);
                foreach (var id in Main.winnerList)
                {
                    sb.Append($"\n★ ").Append(EndGamePatch.SummaryText[id]);
                    cloneRoles.Remove(id);
                }
                foreach (var id in cloneRoles)
                {
                    sb.Append($"\n　 ").Append(EndGamePatch.SummaryText[id]);
                }
            }
            return "\n" + sb.ToString().RemoveHtmlTags();
        }
    }
    public static void SeveImage(bool autosave = false)
    {
        if (autosave && !Main.AutoSaveScreenShot.Value) return;
        var endGameNavigation = GameObject.Find("EndGameNavigation");
        if (!autosave)
        {
            if (endGameNavigation == null) return;
            endGameNavigation.SetActive(false);
        }
        SetEverythingUpPatch.ScreenShotbutton.Button.transform.SetLocalY(-50);
        var now = DateTime.Now;
        var path = $"{ScreenShotFolder.FullName}TOH-Pv{Main.PluginVersion}-{now.Year}-{now.Month}-{now.Day}-{now.Hour}.{now.Minute}.png";

        _ = new LateTask(() => ScreenCapture.CaptureScreenshot(path), 0.5f, "SecreenShot");

        if (!autosave)
            _ = new LateTask(() =>
            {
                endGameNavigation.SetActive(true);
                SetEverythingUpPatch.ScreenShotbutton.Button.transform.SetLocalY(2.6f);
            }, 1f, "", true);
    }
}