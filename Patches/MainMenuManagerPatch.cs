using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using TMPro;
using HarmonyLib;
using UnityEngine;
using AmongUs.Data;
using Assets.InnerNet;

using TownOfHost.Templates;
using static TownOfHost.GameSettingMenuStartPatch;
using Object = UnityEngine.Object;

namespace TownOfHost
{
    [HarmonyPatch(typeof(MainMenuManager))]
    public class MainMenuManagerPatch
    {
        private static SimpleButton discordButton;
        private static SimpleButton StatisticsButton;
        private static SimpleButton BackButton;
        private static SimpleButton NextButton;
        static Dictionary<byte, string> Pages;
        static byte now;
        public static SimpleButton UpdateButton { get; private set; }
        public static SimpleButton UpdateButton2;
        private static SimpleButton gitHubButton;
        private static SimpleButton TwitterXButton;
        private static SimpleButton TOHkBOTButton;
        private static SimpleButton VersionChangeButton;
        private static SimpleButton betaversionchange;
        public static TextMeshPro Statistisc;
        public static GameObject VersionMenu;
        public static GameObject betaVersionMenu;
        public static AnnouncementPopUp updatea;

        [HarmonyPatch(nameof(MainMenuManager.Start)), HarmonyPostfix, HarmonyPriority(Priority.Normal)]
        public static void StartPostfix(MainMenuManager __instance)
        {
            SimpleButton.SetBase(__instance.quitButton);
            Pages = new();
            now = byte.MaxValue;
            //Discordボタンを生成
            if (SimpleButton.IsNullOrDestroyed(discordButton))
            {
                discordButton = CreateButton(
                    "DiscordButton",
                    new(-2.5f * Widthratio, -1f, 1f),
                    new(88, 101, 242, byte.MaxValue),
                    new(148, 161, byte.MaxValue, byte.MaxValue),
                    () => Application.OpenURL(Main.DiscordInviteUrl),
                    "Discord",
                    isActive: Main.ShowDiscordButton);
            }

            // GitHubボタンを生成
            if (SimpleButton.IsNullOrDestroyed(gitHubButton))
            {
                gitHubButton = CreateButton(
                    "GitHubButton",
                    new(-0.8f * Widthratio, -1f, 1f),//-1f
                    new(153, 153, 153, byte.MaxValue),
                    new(209, 209, 209, byte.MaxValue),
                    () => Application.OpenURL("https://github.com/KYMario/TownOfHost-K"),
                    "GitHub");
            }

            // TwitterXボタンを生成
            if (SimpleButton.IsNullOrDestroyed(TwitterXButton))
            {
                TwitterXButton = CreateButton(
                    "TwitterXButton",
                    new(0.9f * Widthratio, -1f, 1f),
                    new(0, 202, 255, byte.MaxValue),
                    new(60, 255, 255, byte.MaxValue),
                    () => Application.OpenURL("https://twitter.com/Tohkserver_k"),
                    "Twitter(X)");
            }
            // TOHkBOTボタンを生成
            if (SimpleButton.IsNullOrDestroyed(TOHkBOTButton))
            {
                TOHkBOTButton = CreateButton(
                    "TOHkBOTButton",
                    new(2.6f * Widthratio, -1f, 1f),
                    new(0, 201, 87, byte.MaxValue),
                    new(60, 201, 87, byte.MaxValue),
                    () => Application.OpenURL("https://discord.com/api/oauth2/authorize?client_id=1198276538563567716&permissions=8&scope=bot"),
                    "TOHkBOT");
            }
            if (SimpleButton.IsNullOrDestroyed(StatisticsButton))
            {
                StatisticsButton = CreateButton(
                    "StatisticsButton",
                    new Vector3(0, -2.6963f, -5f),
                    new(255, 242, 104, byte.MaxValue),
                    new(255, 248, 173, byte.MaxValue),
                    () =>
                    {
                        var ages = SaveStatistics.ShowText().RemoveSizeTags().Split("\n");
                        var Page = "<size=60%>";
                        var index = 0;
                        byte count = 0;
                        foreach (var text in ages)
                        {
                            index++;
                            Page += $"{text}\n";
                            if (16 <= index)
                            {
                                Pages.TryAdd(count, Page);
                                count++;
                                index = 0;
                                Page = "<size=60%>";
                            }
                        }
                        if (Page.RemoveHtmlTags() != "")
                            Pages.TryAdd(count, Page);

                        CredentialsPatch.TohkLogo.gameObject.SetActive(false);
                        __instance.screenTint.enabled = true;
                        Statistisc.gameObject.SetActive(true);
                        now = 0;
                        Statistisc.text = Pages.TryGetValue(now, out var t) ? t : "???";
                        NextButton.Button.gameObject.SetActive(true);
                        BackButton.Button.gameObject.SetActive(true);
                    },
                    Translator.GetString("Statistics")
                    );
            }

            if (SimpleButton.IsNullOrDestroyed(NextButton))
            {
                NextButton = CreateButton(
                    "NextButton",
                    new(3.1f * Widthratio, -2.2f, -6f),
                    new(255, 242, 104, byte.MaxValue),
                    new(255, 248, 173, byte.MaxValue),
                    () =>
                    {
                        now++;
                        if (Pages.TryGetValue(now, out var t))
                        {
                            Statistisc.text = t;
                        }
                        else now--;
                    },
                    "▷",
                    new Vector2(0.5f, 0.5f),
                    false,
                    __instance.screenTint.transform
                    );
            }

            if (SimpleButton.IsNullOrDestroyed(BackButton))
            {
                BackButton = CreateButton(
                    "BackButton",
                    new(2.5f * Widthratio, -2.2f, -6),
                    new(255, 242, 104, byte.MaxValue),
                    new(255, 248, 173, byte.MaxValue),
                    () =>
                    {
                        now--;
                        if (Pages.TryGetValue(now, out var t))
                        {
                            Statistisc.text = t;
                        }
                        else now++;
                    },
                    "◁",
                    new Vector2(0.5f, 0.5f),
                    false,
                    __instance.screenTint.transform
                    );
            }

            //Updateボタンを生成
            if (SimpleButton.IsNullOrDestroyed(UpdateButton))
            {
                UpdateButton = CreateButton(
                    "UpdateButton",
                    new(0f, -1.7f, 1f),
                    new(0, 202, 255, byte.MaxValue),
                    new(60, 255, 255, byte.MaxValue),
                    () =>
                    {
                        //if (!Main.AllowPublicRoom)
                        //{
                        UpdateButton.Button.gameObject.SetActive(false);
                        ModUpdater.StartUpdate(ModUpdater.downloadUrl);
                        //}
                        /*else
                        {
                            UpdateButton.Button.gameObject.SetActive(false);
                            ModUpdater.GoGithub();
                        }*/
                    },
                    $"{Translator.GetString("updateButton")}\n{ModUpdater.latestTitle}",
                    new(2.5f, 1f),
                    isActive: false);
            }
            // アップデート(詳細)ボタンを生成
            if (SimpleButton.IsNullOrDestroyed(UpdateButton2))
            {
                UpdateButton2 = CreateButton(
                    "UpdateButton2",
                    new(1.3f, -1.9f, 1f),
                    new(153, 153, 153, byte.MaxValue),
                    new(209, 209, 209, byte.MaxValue),
                    () =>
                    {
                        if (updatea == null)
                        {
                            updatea = Object.Instantiate(__instance.announcementPopUp);
                        }
                        updatea.name = "Update Detail";
                        updatea.gameObject.SetActive(true);
                        updatea.AnnouncementListSlider.SetActive(false);
                        updatea.Title.text = "TOH-K " + ModUpdater.latestTitle;
                        updatea.AnnouncementBodyText.text = Regex.Replace(ModUpdater.body.Replace("#", "").Replace("**", ""), @"\[(.*?)\]\(.*?\)", "$1");
                        updatea.DateString.text = "Latest Release";
                        updatea.SubTitle.text = "";
                        updatea.ListScroller.gameObject.SetActive(false);
                    },
                    "▽",
                    new(0.5f, 0.5f),
                    isActive: false);
            }
            //同じバージョンの 安定ver,デバッグバージョンの切り替えの奴
            if (SimpleButton.IsNullOrDestroyed(betaversionchange))
            {
                betaversionchange = CreateButton(
                    "betaversionchange",
                    new(-2.3f * Widthratio, -2.6963f, 1f),
                    new(0, 255, 183, byte.MaxValue),
                    new(60, 255, 183, byte.MaxValue),
                    () =>
                    {
                        CredentialsPatch.TohkLogo.gameObject.SetActive(false);
                        __instance.screenTint.enabled = true;
                        if (betaVersionMenu != null)
                        {
                            betaVersionMenu.SetActive(true);
                            return;
                        }
                        betaVersionMenu = new GameObject("verPanel");
                        betaVersionMenu.transform.parent = __instance.gameModeButtons.transform.parent;
                        betaVersionMenu.transform.localPosition = new(-0.0964f, 0.1378f, 1f);
                        betaVersionMenu.SetActive(true);
                        ModUpdater.CheckRelease(all: true, snap: true).GetAwaiter().GetResult();
                        int i = 0;
                        if (ModUpdater.snapshots.Count == 0) return;

                        foreach (var release in ModUpdater.snapshots)
                        {
                            int column = i % 4;
                            int row = i / 4;
                            // X 座標と Y 座標を計算
                            float x = -1.6891f + (1.6891f * column);
                            float y = 0.8709f - (0.3927f * row);
                            var button2 = new SimpleButton(
                            betaVersionMenu.transform,
                            release.TagName,
                            new(x, y, 1f),
                            release.TagName.Contains("S") ? new(0, 255, 183, byte.MaxValue) : new(0, 202, 255, byte.MaxValue),
                            release.TagName.Contains("S") ? new(60, 255, 183, byte.MaxValue) : new(60, 255, 255, byte.MaxValue),
                            () =>
                            {
                                if (release.DownloadUrl != null)
                                    ModUpdater.StartUpdate(release.DownloadUrl, release.OpenURL);
                            },
                            "v" + release.TagName.TrimStart('v').Trim('S').Trim('s') + (release.DownloadUrl == null ? "(ERROR)" : ""));
                            i++;
                        }
                    },
                    Translator.GetString("versionchangebutton"));
                betaversionchange.FontSize = 2;
            }
            CreateStreameMenu.CreateMenu(__instance);

            // フリープレイの無効化
            var howToPlayButton = __instance.howToPlayButton;
            var freeplayButton = howToPlayButton.transform.parent.Find("FreePlayButton");
#if RELEASE
            if (freeplayButton != null)
            {
                var textm = freeplayButton.transform.FindChild("Text_TMP").GetComponent<TextMeshPro>();
                textm.DestroyTranslator();
                textm.text = Translator.GetString("EditCSp");

                freeplayButton.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() => Main.EditMode = true));
            }
            // フリープレイが消えるのでHowToPlayをセンタリング | 消えないのでしません☆
            //howToPlayButton.transform.SetLocalX(0);
#endif
#if DEBUG
            if (freeplayButton != null)
            {
                var csbutton = GameObject.Instantiate(freeplayButton, freeplayButton.parent);
                var textm = csbutton.transform.FindChild("Text_TMP").GetComponent<TextMeshPro>();
                textm.DestroyTranslator();
                textm.text = Translator.GetString("EditCSp");

                csbutton.transform.localPosition = new Vector3(2.8704f, -1.9916f);
                csbutton.transform.localScale = new Vector3(0.6f, 0.6f);
                var pb = csbutton.GetComponent<PassiveButton>();
                pb.inactiveSprites.GetComponent<SpriteRenderer>().color = new(88, 101, 242, byte.MaxValue);
                pb.activeSprites.GetComponent<SpriteRenderer>().color = new(148, 161, byte.MaxValue, byte.MaxValue);
                pb.OnClick.AddListener((Action)(() => Main.EditMode = true));
                freeplayButton.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() => Main.EditMode = false));//ボタンを生成
                if (SimpleButton.IsNullOrDestroyed(VersionChangeButton))
                {
                    VersionChangeButton = CreateButton(
                        "VersionChangeButton",
                        new(2.4036f * Widthratio, -1.6963f, 1f),
                        new(0, 202, 255, byte.MaxValue),
                        new(60, 255, 255, byte.MaxValue),
                        () =>
                        {
                            CredentialsPatch.TohkLogo.gameObject.SetActive(false);
                            __instance.screenTint.enabled = true;
                            if (VersionMenu != null)
                            {
                                VersionMenu.SetActive(true);
                                return;
                            }
                            VersionMenu = new GameObject("verPanel");
                            VersionMenu.transform.parent = __instance.gameModeButtons.transform.parent;
                            VersionMenu.transform.localPosition = new(-0.0964f, 0.1378f, 1f);
                            VersionMenu.SetActive(true);
                            ModUpdater.CheckRelease(all: true).GetAwaiter().GetResult();
                            int i = 0;
                            foreach (var release in ModUpdater.releases)
                            {
                                int column = i % 4;
                                int row = i / 4;
                                // X 座標と Y 座標を計算
                                float x = -1.6891f + (1.6891f * column);
                                float y = 0.8709f - (0.3927f * row);
                                var button2 = new SimpleButton(
                                VersionMenu.transform,
                                release.TagName,
                                new(x, y, 1f),
                                new(0, 202, 255, byte.MaxValue),
                                new(60, 255, 255, byte.MaxValue),
                                () =>
                                {
                                    if (release.DownloadUrl != null)
                                        ModUpdater.StartUpdate(release.DownloadUrl, release.OpenURL);
                                },
                                "v" + release.TagName.TrimStart('v').Trim('S').Trim('s') + (release.DownloadUrl == null ? "(ERROR)" : ""));
                                i++;
                            }
                        },
                        $"{Translator.GetString("verChangeButton")}");
                    VersionChangeButton.FontSize = 2;
                }
            }
#endif
        }

        /// <summary>TOHロゴの子としてボタンを生成</summary>
        /// <param name="name">オブジェクト名</param>
        /// <param name="normalColor">普段のボタンの色</param>
        /// <param name="hoverColor">マウスが乗っているときのボタンの色</param>
        /// <param name="action">押したときに発火するアクション</param>
        /// <param name="label">ボタンのテキスト</param>
        /// <param name="scale">ボタンのサイズ 変更しないなら不要</param>
        public static SimpleButton CreateButton(
            string name,
            Vector3 localPosition,
            Color32 normalColor,
            Color32 hoverColor,
            Action action,
            string label,
            Vector2? scale = null,
            bool isActive = true,
            Transform transform = null)
        {
            var button = new SimpleButton(transform == null ? CredentialsPatch.TohkLogo.transform : transform, name, localPosition, normalColor, hoverColor, action, label, isActive);
            if (scale.HasValue)
            {
                button.Scale = scale.Value;
            }
            return button;
        }

        [HarmonyPatch(nameof(MainMenuManager.OpenFindGame))]
        [HarmonyPatch(nameof(MainMenuManager.OpenEnterCodeMenu))]
        [HarmonyPrefix]
        public static bool ClickFindGame()
        {
            return false;
        }
        [HarmonyPatch(nameof(MainMenuManager.ClickBackOnline))]
        [HarmonyPostfix]
        public static void ClickBackOnline(MainMenuManager __instance)
        {
            __instance.ResetScreen();
        }
        // プレイメニュー，アカウントメニュー，クレジット画面が開かれたらロゴとボタンを消す
        [HarmonyPatch(nameof(MainMenuManager.OpenGameModeMenu))]
        [HarmonyPatch(nameof(MainMenuManager.OpenAccountMenu))]
        [HarmonyPatch(nameof(MainMenuManager.OpenCredits))]
        [HarmonyPatch(nameof(MainMenuManager.OpenOnlineMenu))]
        [HarmonyPatch(nameof(MainMenuManager.GoBackCreateGame))]
        [HarmonyPatch(nameof(MainMenuManager.OpenEnterCodeMenu))]
        [HarmonyPostfix]
        public static void OpenMenuPostfix(MainMenuManager __instance)
        {
            CreateStreameMenu.CloseMenu();
            var Findbuttongo = GameObject.Find("MainMenuManager/MainUI/AspectScaler/RightPanel/MaskedBlackScreen/OnlineButtons/AspectSize/Scaler/Find Game Button");

            var codebuttongo = GameObject.Find("MainMenuManager/MainUI/AspectScaler/RightPanel/MaskedBlackScreen/OnlineButtons/AspectSize/Scaler/Enter Code Button");
            var Codebutton = codebuttongo.transform.GetComponent<PassiveButton>();

            var createbuttongameobject = GameObject.Find("MainMenuManager/MainUI/AspectScaler/RightPanel/MaskedBlackScreen/OnlineButtons/AspectSize/Scaler/Create Lobby Button");
            var createbutton = createbuttongameobject.transform.GetComponent<PassiveButton>();

            if (Findbuttongo)
            {
                Findbuttongo.SetActive(false);
            }
            if (Codebutton)
            {
                var buttonCollider = Codebutton.GetComponent<BoxCollider2D>();
                buttonCollider.offset = new(100f, 100f);
            }
            if (Main.IsAndroid())
            {
                //    createbutton.GetComponent<BoxCollider2D>().offset = new(100f, 100);
            }

            if (CredentialsPatch.TohkLogo != null)
            {
                CredentialsPatch.TohkLogo.gameObject.SetActive(false);
            }
            if (VersionMenu != null)
                VersionMenu.SetActive(false);
            if (betaVersionMenu != null)
                betaVersionMenu.SetActive(false);

            {
                var warning = GameObject.Find("MainMenuManager/MainUI/AspectScaler/RightPanel/MaskedBlackScreen/OnlineButtons/AspectSize/CrossplayWarning");
                warning.SetActive(true);
                var TMPobjct = GameObject.Find("MainMenuManager/MainUI/AspectScaler/RightPanel/MaskedBlackScreen/OnlineButtons/AspectSize/CrossplayWarning/CrossPlayText/Text_TMP");

                var TMP = TMPobjct.transform.GetComponent<TextMeshPro>();
                TMP.SetText(Main.IsAndroid() ? Translator.GetString("CantAndroidCreateGame") : Translator.GetString("CantPublickAndJoin"));
                _ = new LateTask(() => TMP.SetText(Main.IsAndroid() ? Translator.GetString("CantAndroidCreateGame") : Translator.GetString("CantPublickAndJoin")), 0.05f, "Set", true);
            }
        }
        [HarmonyPatch(nameof(MainMenuManager.ResetScreen)), HarmonyPostfix]
        public static void ResetScreenPostfix()
        {
            if (CredentialsPatch.TohkLogo != null)
            {
                CredentialsPatch.TohkLogo?.gameObject?.SetActive(true);
            }
            if (VersionMenu != null)
                VersionMenu.SetActive(false);
            if (betaVersionMenu != null)
                betaVersionMenu.SetActive(false);
            CreateStreameMenu.CloseMenu();
        }
        public static void DestroyButton()
        {
            VersionMenu = null;
            betaVersionMenu = null;
        }
    }
    public class ModNews
    {
        public int Number;
        public int BeforeNumber;
        public string Title;
        public string SubTitle;
        public string ShortTitle;
        public string Text;
        public string Date;

        public Announcement ToAnnouncement()
        {
            var result = new Announcement
            {
                Number = Number,
                Title = Title,
                SubTitle = SubTitle,
                ShortTitle = ShortTitle,
                Text = Text,
                Language = (uint)DataManager.Settings.Language.CurrentLanguage,
                Date = Date,
                Id = "ModNews"
            };

            return result;
        }
    }
    public class JsonModNews
    {
        public JsonModNews(int Number, string Title, string SubTitle, string ShortTitle,
            string Text, string Date)
        {
            var news = new ModNews
            {
                Number = Number,
                Title = Title,
                SubTitle = SubTitle,
                ShortTitle = ShortTitle,
                Text = Text,
                Date = Date
            };
            ModNewsHistory.JsonAndAllModNews.Add(news);
        }
    }
}