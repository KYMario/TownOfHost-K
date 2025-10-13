using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using TMPro;
using UnityEngine;

using TownOfHost.Roles.Core;
using UnityEngine.Networking;

namespace TownOfHost.Modules;

[HarmonyPatch]
class VersionInfoManager
{
    public static readonly string URL = "https://raw.githubusercontent.com/KYMario/TownOfHost-K/main/versions.json";
    //public static readonly string URL = "https://raw.githubusercontent.com/KYMario/TOHk-Test/main/versions.json";
    public static Dictionary<string, VersionInfo> Versions { get; protected set; }
    public static VersionInfo version { get; protected set; }
    public static bool isChecked = false;
    private static bool IsSupported = true;
    private static int totalSeconds = 0;
    private static TextMeshPro ModInfoText;
    private static ulong CustomFlags = 0;

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPostfix, HarmonyPriority(Priority.Last)]
    public static void StartPostfix(MainMenuManager __instance)
    {
        if (__instance == null) return;
        var result = CheckVersionsJson().GetAwaiter().GetResult();
        if (result) isChecked = true;

        if (version == null) return;

        string infoText = "";

        if (!version.HighestSupportedVersion.IsNullOrWhiteSpace())
        {
            if (Version.TryParse(version.HighestSupportedVersion, out var highestSupported))
            {
                IsSupported = Version.Parse(Application.version) <= highestSupported;
                Logger.Info($"app: {Version.Parse(Application.version)} support: {highestSupported}", "ver");
            }
        }

        //オンライン無効化
        if (version.NotAvailableOnline == true)
        {
            infoText = "現在MODを導入してオンラインでプレイすることができません";

            __instance.PlayOnlineButton.gameObject.SetActive(false);
            __instance.playLocalButton.transform.SetLocalX(0);
        }
        if (version.Unavailable == true || !IsSupported || Options.LoadError)
        {
            infoText = "現在MODを導入してプレイすることができません";

            __instance.PlayOnlineButton.gameObject.SetActive(false);
            __instance.playLocalButton.gameObject.SetActive(false);
            __instance.freePlayButton.gameObject.SetActive(false);
            __instance.howToPlayButton.transform.localPosition += new Vector3(-1, 0, 0);
        }
        if (version.BlockPublicRoom.HasValue)
        {
            ModUpdater.BlockPublicRoom = version.BlockPublicRoom;
        }

        if (!version.AnnounceText.IsNullOrWhiteSpace())
        {
            var announceText = new GameObject("ModAnnounceText").AddComponent<TextMeshPro>();

            announceText.transform.SetParent(CredentialsPatch.TohkLogo.transform);
            announceText.transform.localPosition = new(0f, 1f, 0f);
            announceText.fontSize =
            announceText.fontSizeMax =
            announceText.fontSizeMin = 3;
            announceText.color = Color.white;
            announceText.alignment = TextAlignmentOptions.Center;
            announceText.gameObject.SetActive(true);

            announceText.text = version.AnnounceText;
        }

        var text = new GameObject("ModInfoText").AddComponent<TextMeshPro>();

        text.transform.SetParent(__instance.gameModeButtons.transform);
        text.transform.localPosition = new(6.8f, 0, 0);
        text.fontSize =
        text.fontSizeMax =
        text.fontSizeMin = 2;
        text.color = Color.red;
        text.text = infoText;
        text.gameObject.SetActive(true);
        ModInfoText = text;

        if (isChecked && version.Update?.Version != null)
        {
            if (ModUpdater.hasUpdate || version.Update?.ShowUpdateButton == true)
            {
                ModUpdater.CheckRelease(all: true).GetAwaiter().GetResult();
                var release = ModUpdater.releases.FirstOrDefault(x => x.TagName == version.Update?.Version, null);
                if (release != null)
                {
                    ModUpdater.downloadUrl = release.DownloadUrl;
                    ModUpdater.latestVersion = new(release.TagName.TrimStart('v')?.Trim('S')?.Trim('s'));
                    ModUpdater.latestTitle = $"Ver. {ModUpdater.latestVersion}";
                    MainMenuManagerPatch.UpdateButton2.Button.gameObject.SetActive(false);
                }
            }

            MainMenuManagerPatch.UpdateButton.Button.gameObject.SetActive(ModUpdater.hasUpdate || version.Update?.ShowUpdateButton == true);
            MainMenuManagerPatch.UpdateButton.Label.text = $"{Translator.GetString("updateButton")}\n{ModUpdater.latestTitle}";
        }

        if (isChecked && !IsSupported)
        {
            __instance.StartCoroutine(UpdateText(text).WrapToIl2Cpp());
            try
            {
                var button = GameObject.Instantiate(__instance.howToPlayButton, __instance.howToPlayButton.transform.parent);
                button.buttonText.DestroyTranslator();
                button.OnClick = null;
                if (button) InitButton(__instance, button);
            }
            catch (Exception ex) { Logger.Exception(ex, "VersionInfo"); }
        }

        if (version.CustomFlags != null)
        {
            ulong bits = 0;
            foreach (var kv in version.CustomFlags)
            {
                if (kv.Value && kv.Key >= 0 && kv.Key < 64)
                    bits |= 1UL << kv.Key;
            }
            CustomFlags = bits;
        }
    }

    private static IEnumerator UpdateText(TextMeshPro infoText)
    {
        var baseText = "\n\n\nこのAmongUsのバージョンには対応してないよ!!\nアップデートしてね!!";
        baseText += "\n                                                                                    <size=4><color=white>";
        string[] omake =
        [
        "（´・ω・｀）",
        "  ?(･ω･`｡)",
        "  (´-ω- `)",
        " zzz(-_- )",
        ];

        foreach (var text in omake)
        {
            infoText.text = $"{baseText}{text}";
            yield return new WaitForSeconds(300f);
        }
    }
    private static void InitButton(MainMenuManager __instance, PassiveButton button)
    {
        __instance.StartCoroutine(UpdateButton(button, false).WrapToIl2Cpp());
        button.transform.localPosition += new Vector3(2.5f, 0);

        if (button.OnClick == null)
        {
            button.OnClick = new();
            button.OnClick.AddListener((Action)(() =>
            {
                button.enabled = false;
                if (ModUpdater.hasUpdate)
                {
                    button.gameObject.SetActive(false);
                    ModUpdater.StartUpdate(ModUpdater.downloadUrl);
                    return;
                }
                ModUpdater.CheckRelease().GetAwaiter().GetResult();
                __instance.StartCoroutine(UpdateButton(button).WrapToIl2Cpp());
            }));
        }
    }
    private static IEnumerator UpdateButton(PassiveButton button, bool resetTime = true)
    {
        if (resetTime) totalSeconds = 300;

        SetButtonState(button, false);

        while (totalSeconds > 0 && !ModUpdater.hasUpdate)
        {
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;

            button.buttonText.text = $"再度チェック可能まで {minutes:D2}:{seconds:D2}";
            yield return new WaitForSeconds(1f);
            totalSeconds--;
        }

        SetButtonState(button, true);
        UpdateText(button);

    }
    private static void SetButtonState(PassiveButton button, bool active)
    {
        button.enabled = active;
        button.activeSprites.GetComponent<SpriteRenderer>().color = active ? new(1f, 1f, 1f) : new(0.3f, 0.3f, 0.3f);
        button.activeSprites.SetActive(!active);
        button.inactiveSprites.SetActive(active);
    }
    private static void UpdateText(PassiveButton button)
    {
        if (ModUpdater.hasUpdate)
            button.buttonText.text = "アップデート";
        else
            button.buttonText.text = "最新の情報を取得";
    }

    private static void SendBugList(byte sendTo = byte.MaxValue)
    {
        if (version?.BugInfos == null) return;
        var bugTexts = new Dictionary<BugCategory, StringBuilder>();
        for (var i = 0; version.BugInfos.Count > i; i++)
        {
            var bugInfo = version.BugInfos[i];
            if (!bugTexts.TryGetValue(bugInfo.Category, out var sb))
            {
                sb = new StringBuilder();
                bugTexts[bugInfo.Category] = sb;
            }

            sb.AppendLine($"[{i}]{bugInfo.Title}");
        }
        foreach (var (category, bugText) in bugTexts)
        {
            Utils.SendMessage(bugText.ToString(), sendTo, Translator.GetString(category.ToString()));
        }
    }
    public static void SendBugInfo(int? index = null, byte sendTo = byte.MaxValue)
    {
        //バグ一覧がまだない
        if (version?.BugInfos == null || !version.BugInfos.Any())
        {
            Utils.SendMessage("まだバグは報告されていません。", sendTo);
            return;
        }
        //idが入力されていない or idが無効
        if (!index.HasValue || version.BugInfos.Count <= index.Value)
        {
            SendBugList(sendTo);
            return;
        }

        var bugInfo = version.BugInfos[index.Value];
        Utils.SendMessage(bugInfo.Description, sendTo, bugInfo.Title);
    }

    public static async Task<bool> CheckVersionsJson(bool force = false)
    {
        if (Versions != null && !force) return true;

        try
        {
            UnityWebRequest request = UnityWebRequest.Get(URL);
            request.SetRequestHeader("User-Agent", "TownOfHost-K VersionChecker");

            var operation = request.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield();

            if (request.isNetworkError || request.isHttpError)
            {
                Logger.Error($"ステータスコード: {request.responseCode.ToString()}", "CheckVersionJson");
                return false;
            }

            var result = request.downloadHandler.text;
            var versions = JsonSerializer.Deserialize<Dictionary<string, VersionInfo>>(result);
            if (versions == null) return false;
            Versions = versions;
            if (Versions.ContainsKey(Main.PluginVersion)) version = Versions[Main.PluginVersion];
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"バージョン情報の取得に失敗！\n{ex}", "CheckVersionJson", false);
            return false;
        }
    }

    /// <summary>カスタムなフラグを取得 </summary> <param name="id">0から63まで</param>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static bool GetCustomFlag(int id) => ((CustomFlags >> id) & 1UL) != 0;

    public class VersionInfo
    {
        public bool? BlockPublicRoom { get; set; }
        public bool? Unavailable { get; set; }
        public bool? NotAvailableOnline { get; set; }
        public string AnnounceText { get; set; }
        public string HighestSupportedVersion { get; set; }

        public UpdateInfo Update { get; set; }
        public List<BugInfo> BugInfos { get; set; }
        public List<EventData> Events { get; set; }
        public Dictionary<int, bool> CustomFlags { get; set; } //intは0から63まで

        public class UpdateInfo
        {
            public string Version { get; set; }
            public bool? Forced { get; set; } //強制アップデートはあんま好きじゃないので機能はまだ考え中
            public bool? ShowUpdateButton { get; set; }
        }
        public class BugInfo
        {
            public string Title { get; set; }
            public BugCategory Category { get; set; }
            public string Description { get; set; }
        }
        public class EventData
        {
            public int RoleId { get; set; }
            public EventPeriod Period { get; set; }
            public class EventPeriod
            {
                public int Month { get; set; }
                public int StartDay { get; set; }
                public int EndDay { get; set; }
                public bool IsActive => DateTime.Now.Month == this.Month
                                && DateTime.Now.Day >= this.StartDay
                                && DateTime.Now.Day <= this.EndDay;
            }
        }
    }
    public enum BugCategory
    {
        MainSystemBug,
        ImpostorBug,
        MadmateBug,
        CrewmateBug,
        NeutralBug
    }
}

static class BugInfoShower
{
}