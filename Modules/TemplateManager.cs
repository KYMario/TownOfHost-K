using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AmongUs.Data;
using HarmonyLib;
using TownOfHost.Attributes;
using static TownOfHost.Translator;

namespace TownOfHost
{
    public static class TemplateManager
    {
        private static readonly string TEMPLATE_FILE_PATH = Main.BaseDirectory + "/template.txt";
        private static Dictionary<string, Func<string>> _replaceDictionary = new()
        {
            ["RoomCode"] = () => InnerNet.GameCode.IntToGameName(AmongUsClient.Instance.GameId),
            ["PlayerName"] = () => DataManager.Player.Customization.Name,
            ["AmongUsVersion"] = () => UnityEngine.Application.version,
            ["ModVersion"] = () => Main.PluginShowVersion + (Main.DebugVersion ? $"☆{GetString("Debug")}☆" : ""),
            ["Map"] = () => Constants.MapNames[Main.NormalOptions.MapId],
            ["NumEmergencyMeetings"] = () => Main.NormalOptions.NumEmergencyMeetings.ToString(),
            ["EmergencyCooldown"] = () => Main.NormalOptions.EmergencyCooldown.ToString(),
            ["DiscussionTime"] = () => Main.NormalOptions.DiscussionTime.ToString(),
            ["VotingTime"] = () => Main.NormalOptions.VotingTime.ToString(),
            ["PlayerSpeedMod"] = () => Main.NormalOptions.PlayerSpeedMod.ToString(),
            ["CrewLightMod"] = () => Main.NormalOptions.CrewLightMod.ToString(),
            ["ImpostorLightMod"] = () => Main.NormalOptions.ImpostorLightMod.ToString(),
            ["KillCooldown"] = () => Main.NormalOptions.KillCooldown.ToString(),
            ["NumCommonTasks"] = () => Main.NormalOptions.NumCommonTasks.ToString(),
            ["NumLongTasks"] = () => Main.NormalOptions.NumLongTasks.ToString(),
            ["NumShortTasks"] = () => Main.NormalOptions.NumShortTasks.ToString(),
            ["NumImpostors"] = () => Main.NormalOptions.NumImpostors.ToString(),
            ["Date"] = () => DateTime.Now.ToShortDateString(),
            ["Roles"] = () => UtilsShowOption.GetActiveRoleText(byte.MaxValue),
            ["Timer"] = () => Utils.GetTimer(),
            ["ModColor"] = () => Main.ModColor,
            ["NumImpostorRoles"] = () => UtilsShowOption.GetRoleTypesCountInt(true).imp.ToString(),
            ["NumCrewmateRoles"] = () => UtilsShowOption.GetRoleTypesCountInt().crew.ToString(),
            ["NumMadmateRoles"] = () => UtilsShowOption.GetRoleTypesCountInt().mad.ToString(),
            ["NumNeutralRoles"] = () => UtilsShowOption.GetRoleTypesCountInt().neutral.ToString(),
            ["NumAddonRoles"] = () => UtilsShowOption.GetRoleTypesCountInt().addon.ToString(),
            ["NumLoverRoles"] = () => UtilsShowOption.GetRoleTypesCountInt().lovers.ToString(),
            ["NumGhostRoles"] = () => UtilsShowOption.GetRoleTypesCountInt().ghost.ToString(),
        };

        [PluginModuleInitializer]
        public static void Init()
        {
            CreateIfNotExists();
        }

        public static void CreateIfNotExists()
        {
            if (!File.Exists(TEMPLATE_FILE_PATH))
            {
                try
                {
                    if (!Directory.Exists(Main.BaseDirectory)) Directory.CreateDirectory(Main.BaseDirectory);
                    if (File.Exists(@"./template.txt"))
                    {
                        File.Move(@"./template.txt", TEMPLATE_FILE_PATH);
                    }
                    else
                    {
                        Logger.Info("Among Us.exeと同じフォルダにtemplate.txtが見つかりませんでした。新規作成します。", "TemplateManager");
                        File.WriteAllText(TEMPLATE_FILE_PATH, "test:This is template text.\\nLine breaks are also possible.\ntest:これは定型文です。\\n改行も可能です。");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex, "TemplateManager");
                }
            }
        }

        public static void SendTemplate(string str = "", byte playerId = 0xff, bool noErr = false)
        {
            CreateIfNotExists();
            using StreamReader sr = new(TEMPLATE_FILE_PATH, Encoding.GetEncoding("UTF-8"));
            string text;
            string[] tmp = Array.Empty<string>();
            List<string> sendList = new();
            HashSet<string> tags = new();
            while ((text = sr.ReadLine()) != null)
            {
                tmp = text.Split(":");
                if (tmp.Length > 1 && tmp[1] != "")
                {
                    tags.Add(tmp[0]);
                    if (tmp[0].ToLower() == str.ToLower()) sendList.Add(tmp.Skip(1).Join(delimiter: ":").Replace("\\n", "\n"));
                }
            }
            if (sendList.Count == 0 && !noErr)
            {
                if (playerId == 0xff)
                    Utils.SendMessage(string.Format(GetString("Message.TemplateNotFoundHost"), str, tags.Join(delimiter: ", ")), PlayerControl.LocalPlayer.PlayerId);
                else Utils.SendMessage(string.Format(GetString("Message.TemplateNotFoundClient"), str), playerId);
            }
            else for (int i = 0; i < sendList.Count; i++) Utils.SendMessage(ApplyReplaceDictionary(sendList[i]), playerId, str == "welcome" ? $"<{Main.ModColor}>【This Room Use \"Town Of Host-K\"】" : "");
        }
        public static string GetTemplate(string str = "")
        {
            CreateIfNotExists();
            using StreamReader sr = new(TEMPLATE_FILE_PATH, Encoding.GetEncoding("UTF-8"));
            string text;
            string[] tmp = Array.Empty<string>();
            List<string> sendList = new();
            HashSet<string> tags = new();
            while ((text = sr.ReadLine()) != null)
            {
                tmp = text.Split(":");
                if (tmp.Length > 1 && tmp[1] != "")
                {
                    tags.Add(tmp[0]);
                    if (tmp[0].ToLower() == str.ToLower()) sendList.Add(tmp.Skip(1).Join(delimiter: ":").Replace("\\n", "\n"));
                }
            }
            if (sendList.Count == 0)
            {
                return "";
            }
            else
            {
                var rtext = "";
                for (int i = 0; i < sendList.Count; i++)
                {
                    rtext += ApplyReplaceDictionary(sendList[i]);
                }
                return rtext;
            }
        }

        private static string ApplyReplaceDictionary(string text)
        {
            foreach (var kvp in _replaceDictionary)
            {
                text = Regex.Replace(text, "{{" + kvp.Key + "}}", kvp.Value.Invoke() ?? "", RegexOptions.IgnoreCase);
            }
            return text;
        }
    }
}