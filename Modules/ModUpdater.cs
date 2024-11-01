using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using TownOfHost.Templates;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch]
    public class ModUpdater
    {
        private static readonly string URL = "https://api.github.com/repos/KYMario/TownOfHost-K";
        public static bool hasUpdate = false;
        public static bool isBroken = false;
        public static bool isChecked = false;
        public static bool isSubUpdata = false;
        public static Version latestVersion = null;
        public static string latestTitle = null;
        public static string downloadUrl = null;
        public static GenericPopup InfoPopup;
        public static bool? AllowPublicRoom = null;
        public static bool matchmaking = false;
        public static bool nothostbug = false;
        public static string body = "詳細のチェックに失敗しました";
        public static List<Release> releases = new();
        private static List<SimpleButton> buttons = new();
        public static Versions version;

        [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPostfix, HarmonyPriority(Priority.LowerThanNormal)]
        public static void StartPostfix()
        {
            DeleteOldDLL();
            InfoPopup = UnityEngine.Object.Instantiate(Twitch.TwitchManager.Instance.TwitchPopup);
            InfoPopup.name = "InfoPopup";
            InfoPopup.TextAreaTMP.GetComponent<RectTransform>().sizeDelta = new(2.5f, 2f);
            if (!isChecked)
            {
                //CheckVersionsJson().GetAwaiter().GetResult();
                CheckRelease(Main.BetaBuildURL.Value != "").GetAwaiter().GetResult();
            }
            /*
            //オンライン無効化
            if (version.NotAvailableOnline)
            {
                DestroyableSingleton<MainMenuManager>.Instance.PlayOnlineButton.gameObject.SetActive(false);
                DestroyableSingleton<MainMenuManager>.Instance.playLocalButton.transform.SetLocalX(0);

                TMPTemplate.SetBase(DestroyableSingleton<VersionShower>.Instance.text);
            }
            if (version.NotAvailableOnline || !(version?.Info?.IsNullOrWhiteSpace() ?? true))
            {
                var text = TMPTemplate.Create("Info", (version?.Info?.IsNullOrWhiteSpace() ?? true) ? "このバージョンではオンラインプレイをすることができません。" : version.Info, Color.red);
                text.transform.localPosition = new(0.68f, 1.7198f, -5f);
                text.alignment = TMPro.TextAlignmentOptions.Left;
                text.gameObject.SetActive(true);
            }
            AllowPublicRoom = version.AllowPublicRoom;
            if (hasUpdate && (version?.Update?.Forced ?? false))
                StartUpdate(downloadUrl);*/
            MainMenuManagerPatch.UpdateButton.Button.gameObject.SetActive(hasUpdate);
            MainMenuManagerPatch.UpdateButton.Button.transform.Find("FontPlacer/Text_TMP").GetComponent<TMPro.TMP_Text>().SetText($"{GetString("updateButton")}\n{latestTitle}");
            MainMenuManagerPatch.UpdateButton2.Button.gameObject.SetActive(hasUpdate);
        }
        public static async Task<bool> CheckRelease(bool beta = false, bool all = false)
        {
            bool updateCheck = version != null && version.Update.Version != null;
            //string url = beta ? Main.BetaBuildURL.Value : URL + "/releases" + (updateCheck ? "/tags/" + version.Update.Version : (all ? "" : "/latest"));
            string url = beta ? Main.BetaBuildURL.Value : URL + "/releases" + (all ? "" : "/latest");
            try
            {
                string result;
                using (HttpClient client = new())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "TownOfHost-K Updater");
                    using var response = await client.GetAsync(new Uri(url), HttpCompletionOption.ResponseContentRead);
                    if (!response.IsSuccessStatusCode || response.Content == null)
                    {
                        Logger.Error($"ステータスコード: {response.StatusCode}", "CheckRelease");
                        return false;
                    }
                    result = await response.Content.ReadAsStringAsync();
                }
                JObject data = all ? null : JObject.Parse(result);
                if (beta)
                {
                    latestTitle = data["name"].ToString();
                    downloadUrl = data["url"].ToString();
                    hasUpdate = latestTitle != ThisAssembly.Git.Commit;
                }
                else if (all)
                {
                    releases = JsonSerializer.Deserialize<List<Release>>(result);
                    foreach (var release in releases)
                    {
                        var assets = release.Assets;
                        foreach (var asset in assets)
                        {
                            if (asset.Name == "TownOfHost-K_Steam.dll" && Constants.GetPlatformType() == Platforms.StandaloneSteamPC)
                            {
                                release.DownloadUrl = asset.DownloadUrl;
                                break;
                            }
                            if (asset.Name == "TownOfHost-K_Epic.dll" && Constants.GetPlatformType() == Platforms.StandaloneEpicPC)
                            {
                                release.DownloadUrl = asset.DownloadUrl;
                                break;
                            }
                            if (asset.Name == "TownOfHost-K.dll")
                                release.DownloadUrl = asset.DownloadUrl;
                        }
                    }
                }
                else
                {
                    latestVersion = new(data["tag_name"]?.ToString().TrimStart('v'));
                    latestTitle = $"Ver. {latestVersion}";
                    JArray assets = data["assets"].Cast<JArray>();
                    for (int i = 0; i < assets.Count; i++)
                    {
                        if (assets[i]["name"].ToString() == "TownOfHost-K_Steam.dll" && Constants.GetPlatformType() == Platforms.StandaloneSteamPC)
                        {
                            downloadUrl = assets[i]["browser_download_url"].ToString();
                            break;
                        }
                        if (assets[i]["name"].ToString() == "TownOfHost-K_Epic.dll" && Constants.GetPlatformType() == Platforms.StandaloneEpicPC)
                        {
                            downloadUrl = assets[i]["browser_download_url"].ToString();
                            break;
                        }
                        if (assets[i]["name"].ToString() == "TownOfHost-K.dll")
                            downloadUrl = assets[i]["browser_download_url"].ToString();
                    }
                    hasUpdate = latestVersion.CompareTo(Main.version) > 0 /*|| version.Update.ShowUpdateButton || version.Update.Forced*/;
                }
                if (all) return true;
                if (downloadUrl == null)
                {
                    Logger.Error("ダウンロードURLを取得できませんでした。", "CheckRelease");
                    return false;
                }
                isChecked = true;
                isBroken = false;
                body = data["body"].ToString();
                // Subverのチェック。
                // バージョンが同一で、　　　　　　　　　　　　　　　　現在のサブバージョンがGitHubに記載されてなくて、　　　　　　　　サブバージョンの記載がある時～
                if (latestVersion.CompareTo(Main.version) == 0 && !body.Contains($"PluginSubVersion:{Main.PluginSubVersion}") && body.Contains($"PluginSubVersion:"))
                {
                    hasUpdate = true;
                    isSubUpdata = true;
                    var entryParts = body.Split("luginSubVersion:");
                    var subvarsion = 1 <= entryParts.Length ? entryParts[1].Trim() : "?";
                    latestTitle += $"<sub>{subvarsion.RemoveText(true)}</sub>";
                }
                else isSubUpdata = false;
                //if (body.Contains("📢公開ルーム○")) publicok = true;
                //else if (body.Contains("📢公開ルーム×")) publicok = false;
                //nothostbug = body.Contains("非ホストmodクライアントにバグあり");
            }
            catch (Exception ex)
            {
                isBroken = true;
                Logger.Error($"リリースのチェックに失敗しました。\n{ex}", "CheckRelease", false);
                return false;
            }
            return true;
        }
        public static void StartUpdate(string url)
        {
            ShowPopup(GetString("updatePleaseWait"));
            if (!BackupDLL())
            {
                ShowPopup(GetString("updateManually"), true);
                return;
            }
            _ = DownloadDLL(url);
            return;
        }
        public static void GoGithub()
        {
            ShowPopup(GetString("gogithub"), true);
            return;
        }
        public static bool BackupDLL()
        {
            try
            {
                File.Move(Assembly.GetExecutingAssembly().Location, Assembly.GetExecutingAssembly().Location + ".bak");
            }
            catch
            {
                Logger.Error("バックアップに失敗しました", "BackupDLL");
                return false;
            }
            return true;
        }
        public static void DeleteOldDLL()
        {
            try
            {
                foreach (var path in Directory.EnumerateFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "*.bak"))
                {
                    Logger.Info($"{Path.GetFileName(path)}を削除", "DeleteOldDLL");
                    File.Delete(path);
                }
            }
            catch
            {
                Logger.Error("削除に失敗しました", "DeleteOldDLL");
            }
            return;
        }
        public static async Task<bool> DownloadDLL(string url)
        {
            try
            {
                using HttpClient client = new();
                using var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    using var content = response.Content;
                    using var stream = content.ReadAsStream();
                    using var file = new FileStream("BepInEx/plugins/TownOfHost-K.dll", FileMode.Create, FileAccess.Write);
                    stream.CopyTo(file);
                    ShowPopup(GetString("updateRestart"), true);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"ダウンロードに失敗しました。\n{ex}", "DownloadDLL", false);
            }
            ShowPopup(GetString("updateManually"), true);
            return false;
        }
        private static void DownloadCallBack(object sender, DownloadProgressChangedEventArgs e)
        {
            ShowPopup($"{GetString("updateInProgress")}\n{e.BytesReceived}/{e.TotalBytesToReceive}({e.ProgressPercentage}%)");
        }
        private static void ShowPopup(string message, bool showButton = false)
        {
            if (InfoPopup != null)
            {
                InfoPopup.Show(message);
                var button = InfoPopup.transform.FindChild("ExitGame");
                if (button != null)
                {
                    button.gameObject.SetActive(showButton);
                    button.GetComponentInChildren<TextTranslatorTMP>().TargetText = StringNames.QuitLabel;
                    button.GetComponent<PassiveButton>().OnClick = new();
                    button.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() =>
                    {
                        Application.OpenURL("https://github.com/KYMario/TownOfHost-K/releases/latest");
                        Application.Quit();
                    }));
                }
            }
        }
        /*
        public static async Task<bool> CheckVersionsJson()
        {
            using HttpClient client = new();
            var url = "https://raw.githubusercontent.com/KYMario/TOHk-Test/main/versions.json";
            client.DefaultRequestHeaders.Add("User-Agent", "TownOfHost-K Updater");
            using var response = await client.GetAsync(new Uri(url), HttpCompletionOption.ResponseContentRead);
            if (!response.IsSuccessStatusCode || response.Content == null)
            {
                Logger.Error($"ステータスコード: {response.StatusCode}", "CheckJson");
                return false;
            }
            var result = await response.Content.ReadAsStringAsync();
            version = JsonSerializer.Deserialize<List<Versions>>(result).Where(ver => ver.Version == Main.version).First();
            return true;
        }*/
        public class Release
        {
            [JsonPropertyName("tag_name")]
            public string TagName { get; set; }
            [JsonPropertyName("assets")]
            public List<Asset> Assets { get; set; }

            public string DownloadUrl { get; set; }

            public class Asset
            {
                [JsonPropertyName("name")]
                public string Name { get; set; }
                [JsonPropertyName("browser_download_url")]
                public string DownloadUrl { get; set; }
            }
        }
        public class Versions
        {
            public Version Version { get; set; }
            public bool? AllowPublicRoom { get; set; }
            public bool Unavailable { get; set; }
            public bool NotAvailableOnline { get; set; }
            public string Info { get; set; }

            public Updates Update { get; set; }
            public class Updates
            {
                public Version Version { get; set; }
                public bool Forced { get; set; }
                public bool ShowUpdateButton { get; set; }
            }
        }
    }
}
