using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using Newtonsoft.Json.Linq;
using TownOfHost.Templates;
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
        public static bool? BlockPublicRoom = null;
        public static string body = "詳細のチェックに失敗しました\nFailed to verify the details.";
        public static List<Release> releases = new();
        public static List<Release> snapshots = new();

        [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPostfix, HarmonyPriority(Priority.LowerThanNormal)]
        public static void StartPostfix()
        {
            DeleteOldDLL();
            InfoPopup = UnityEngine.Object.Instantiate(Twitch.TwitchManager.Instance.TwitchPopup);
            InfoPopup.name = "InfoPopup";
            InfoPopup.TextAreaTMP.GetComponent<RectTransform>().sizeDelta = new(2.5f, 2f);
            if (!isChecked)
            {
                CheckRelease(Main.BetaBuildURL.Value != "").GetAwaiter().GetResult();
            }
            MainMenuManagerPatch.UpdateButton.Button.gameObject.SetActive(hasUpdate);
            MainMenuManagerPatch.UpdateButton.Button.transform.Find("FontPlacer/Text_TMP").GetComponent<TMPro.TMP_Text>().SetText($"{GetString("updateButton")}\n{latestTitle}");
            MainMenuManagerPatch.UpdateButton2.Button.gameObject.SetActive(hasUpdate);
        }
        /// <param name="all">1ページ分のリリースをすべて取得し、releasesとsnapshotsを更新します</param>
        /// <param name="forced">allパラメータと同時に使用します / キャッシュを使用せずもう一度取得します</param>
        /// <returns></returns>
        public static async Task<bool> CheckRelease(bool beta = false, bool all = false, bool forced = false)
        {
            //bool updateCheck = version != null && version.Update.Version != null;
            string url = beta ? Main.BetaBuildURL.Value : URL + "/releases" + (all ? "" : "/latest");
            if (all) url = url + "?page=1";

            //強制オプションが使用されていない & allオプションが使用されている & 既に取得済み
            if (!forced && all && releases.Any()) return true;
            if (Main.IsAndroid()) return true;

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
                    snapshots = new();
                    releases = JsonSerializer.Deserialize<List<Release>>(result);
                    foreach (var release in releases)
                    {
                        var tag = release.TagName;
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
                        release.OpenURL = $"https://github.com/KYMario/TownOfHost-K/releases/tag/{tag}";

                        if (tag == null) continue;
                        //動かないバージョンに切り替えれないようにするための応急手当。.31になる頃には消す。
                        if (!tag.Contains($"{Main.ModVersion}") || tag.Contains(".30.1") || tag.Contains(".30.21") || tag.Contains(".30.22") || tag is "51.13.30") continue;//そのバージョンの奴じゃないなら除外
                        if (tag.StartsWith("5.") || tag.StartsWith("S5.") || tag.StartsWith("s5.") || tag.Contains("519.") || tag.Contains("S519.")) continue;//今の表記は519とかなので5.1.x表示ならもう表示しない

                        snapshots.Add(release);
                    }
                }
                else
                {
                    latestVersion = new(data["tag_name"]?.ToString().TrimStart('v')?.Trim('S')?.Trim('s'));
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
                    var body = data["body"].ToString();
                    bool? check = body?.Contains("IsforceUpdate") ?? null;
                    hasUpdate = latestVersion.CompareTo(Main.version) > 0 ||
                    //最後のアプデのcheckが有効で～最終バージョンと現バージョンが一緒じゃない
                    (check is true && latestVersion.CompareTo(Main.version) is not 0);
                }
                if (all) return true;
                if (downloadUrl == null)
                {
                    Logger.Error("ダウンロードURLを取得できませんでした。", "CheckRelease");
                    return false;
                }
                isChecked = true;
                isBroken = false;
                var ages = data["body"].ToString().Split("## ");
                for (var i = 0; i < ages.Length - 1; i++)
                {
                    if (i == 0)
                    {
                        body = ages[0] + "<size=80%>";
                        continue;
                    }
                    if (i == 1) continue;
                    var ages2 = ages[i].Split("\n");
                    for (var i2 = 0; i2 < ages2.Length; i2++)
                    {
                        if (i2 == 0)
                        {
                            body += $"<b><size=120%>{ages2[i2]}";
                            body += "</b></size>\n";
                            continue;
                        }
                        body += ages2[i2] + "\n";
                    }
                }
            }
            catch (Exception ex)
            {
                isBroken = true;
                Logger.Error($"リリースのチェックに失敗しました。\n{ex}", "CheckRelease", false);
                return false;
            }
            return true;
        }
        public static void StartUpdate(string url, string openurl = "")
        {
            ShowPopup(GetString("updatePleaseWait"));
            if (!BackupDLL())
            {
                ShowPopup(GetString("updateManually"), true, openurl);
                return;
            }
            _ = DownloadDLL(url, openurl);
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
        public static async Task<bool> DownloadDLL(string url, string openurl)
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
                    ShowPopup(GetString("updateRestart"), true, openurl);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"ダウンロードに失敗しました。\n{ex}", "DownloadDLL", false);
            }
            ShowPopup(GetString("updateManually"), true, openurl);
            return false;
        }
        private static void DownloadCallBack(object sender, DownloadProgressChangedEventArgs e)
        {
            ShowPopup($"{GetString("updateInProgress")}\n{e.BytesReceived}/{e.TotalBytesToReceive}({e.ProgressPercentage}%)");
        }
        private static void ShowPopup(string message, bool showButton = false, string OpenURL = "")
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
                        Application.OpenURL(OpenURL == "" ? "https://github.com/KYMario/TownOfHost-K/releases/latest" : OpenURL);
                        Application.Quit();
                    }));
                }
            }
        }
        public class Release
        {
            [JsonPropertyName("tag_name")]
            public string TagName { get; set; }
            [JsonPropertyName("assets")]
            public List<Asset> Assets { get; set; }

            public string DownloadUrl { get; set; }
            public string OpenURL { get; set; }

            public class Asset
            {
                [JsonPropertyName("name")]
                public string Name { get; set; }
                [JsonPropertyName("browser_download_url")]
                public string DownloadUrl { get; set; }
            }
        }
    }
}
