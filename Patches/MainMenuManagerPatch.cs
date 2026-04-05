using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using TMPro;
using HarmonyLib;
using UnityEngine;
using AmongUs.Data;
using Assets.InnerNet;

using TownOfHost.Templates;
using Object = UnityEngine.Object;
using TownOfHost.Modules;
using System.Linq;

namespace TownOfHost
{
    [HarmonyPatch(typeof(MainMenuManager))]
    public class MainMenuManagerPatch
    {
        private static SimpleButton discordButton;
        private static SimpleButton StatisticsButton;
        private static GameObject Statistics_ScrollStuff;
        public static SimpleButton UpdateButton { get; private set; }
        public static SimpleButton UpdateButton2;
        private static SimpleButton gitHubButton;
        private static SimpleButton TwitterXButton;
        private static SimpleButton TOHPBOTButton;
        //private static SimpleButton VersionChangeButton;
        private static SimpleButton betaversionchange;
        public static TextMeshPro Statistics_TMP;
        public static GameObject VersionMenu;
        public static GameObject betaVersionMenu;
        public static AnnouncementPopUp updatea;

        [HarmonyPatch(nameof(MainMenuManager.Start)), HarmonyPostfix, HarmonyPriority(Priority.Normal)]
        public static void StartPostfix(MainMenuManager __instance)
        {
            SimpleButton.SetBase(__instance.quitButton);
            //Discordボタンを生成
            if (SimpleButton.IsNullOrDestroyed(discordButton))
            {
                discordButton = CreateButton(
                    "DiscordButton",
                    new(-2.5f, -1f, 1f),
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
                    new(-0.8f, -1f, 1f),//-1f
                    new(153, 153, 153, byte.MaxValue),
                    new(209, 209, 209, byte.MaxValue),
                    () => Application.OpenURL("https://github.com/KYMario/TownOfHost-Pko"),
                    "GitHub");
            }

            // TwitterXボタンを生成
            if (SimpleButton.IsNullOrDestroyed(TwitterXButton))
            {
                TwitterXButton = CreateButton(
                    "TwitterXButton",
                    new(0.9f, -1f, 1f),
                    new(0, 202, 255, byte.MaxValue),
                    new(60, 255, 255, byte.MaxValue),
                    () => Application.OpenURL("https://twitter.com/TOHPserver_k"),
                    "Twitter(X)");
            }
            // TOHPBOTボタンを生成
            if (SimpleButton.IsNullOrDestroyed(TOHPBOTButton))
            {
                TOHPBOTButton = CreateButton(
                    "TOHPBOTButton",
                    new(2.6f, -1f, 1f),
                    new(0, 201, 87, byte.MaxValue),
                    new(60, 201, 87, byte.MaxValue),
                    () => Application.OpenURL("https://discord.com/api/oauth2/authorize?client_id=1198276538563567716&permissions=8&scope=bot"),
                    "TOHPBOT");
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
                        CredentialsPatch.TOHPLogo.gameObject.SetActive(false);
                        __instance.screenTint.enabled = true;
                        Statistics_TMP.gameObject.SetActive(true);
                        Statistics_TMP.text = $"<size=60%>{SaveStatistics.ShowText()}";
                        Statistics_ScrollStuff.gameObject.SetActive(true);
                        var St_Scroller = Statistics_ScrollStuff.transform.GetChild(0);
                        Statistics_TMP.transform.parent = St_Scroller.GetChild(3).transform;
                        St_Scroller.GetChild(1).gameObject.SetActive(false);
                        St_Scroller.GetChild(2).gameObject.SetActive(false);
                        St_Scroller.localPosition = new(3.18f, -5.45f, 0.1473f);
                        St_Scroller.GetChild(0).localPosition = new(2.1f, 2.6f, 2f);
                        St_Scroller.GetChild(0).localScale = new Vector3(0.7f, 1, 0);
                        St_Scroller.GetChild(3).SetLocalY(0);
                        var ages = Statistics_TMP.text.Split("\n").Count();
                        St_Scroller.GetComponentInParent<Scroller>().ContentYBounds.max = ages > 16 ? (ages - 16) * 0.25f : 0;
                    },
                    Translator.GetString("Statistics")
                    );
            }

            if (Statistics_ScrollStuff == null || Statistics_ScrollStuff.gameObject == null)
            {
                var sc = GameObject.Find("StoreMenu/Background/Scroll Stuff");
                Statistics_ScrollStuff = Object.Instantiate(sc, __instance.transform);
                Statistics_ScrollStuff.gameObject.name = "stscroll";
                var Scroller = Statistics_ScrollStuff.transform.GetChild(0);
                Scroller.GetChild(3).DestroyChildren();//inner全削除
                Statistics_ScrollStuff.gameObject.SetActive(false);
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
                        updatea.Title.text = "TOH-P " + ModUpdater.latestTitle;
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
                    new(-2.3f, -2.6963f, 1f),
                    new(0, 255, 183, byte.MaxValue),
                    new(60, 255, 183, byte.MaxValue),
                    () =>
                    {
                        CredentialsPatch.TOHPLogo.gameObject.SetActive(false);
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
                        ModUpdater.CheckRelease(all: true).GetAwaiter().GetResult();
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

                freeplayButton.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() => CustomSpawnEditor.ActiveEditMode = true));
            }
            // フリープレイが消えるのでHowToPlayをセンタリング | 消えないのでしません☆
            //howToPlayButton.transform.SetLocalX(0);
#endif
#if DEBUG
            var csbutton = GameObject.Instantiate(freeplayButton, freeplayButton.parent);
            var textm = csbutton.transform.FindChild("Text_TMP").GetComponent<TextMeshPro>();
            textm.DestroyTranslator();
            textm.text = Translator.GetString("EditCSp");

            csbutton.transform.localPosition = new Vector3(2.8704f, -1.9916f);
            csbutton.transform.localScale = new Vector3(0.6f, 0.6f);
            var pb = csbutton.GetComponent<PassiveButton>();
            pb.inactiveSprites.GetComponent<SpriteRenderer>().color = new(88, 101, 242, byte.MaxValue);
            pb.activeSprites.GetComponent<SpriteRenderer>().color = new(148, 161, byte.MaxValue, byte.MaxValue);
            pb.OnClick.AddListener((Action)(() => CustomSpawnEditor.ActiveEditMode = true));
            freeplayButton.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() => CustomSpawnEditor.ActiveEditMode = false));//ボタンを生成
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
            var button = new SimpleButton(transform == null ? CredentialsPatch.TOHPLogo.transform : transform, name, localPosition, normalColor, hoverColor, action, label, isActive);
            if (scale.HasValue)
            {
                button.Scale = scale.Value;
            }
            return button;
        }

        [HarmonyPatch(nameof(MainMenuManager.OpenFindGame))]
        [HarmonyPrefix]
        public static bool ClickFindGame()
        {
            return !(VersionInfoManager.version == null || VersionInfoManager.allversion.DisableMM
            || (VersionInfoManager.allversion != null && VersionInfoManager.allversion.DisableMM));
        }
        [HarmonyPatch(nameof(MainMenuManager.OpenEnterCodeMenu))]
        [HarmonyPrefix]
        public static bool ClickOpenEnterCodeMenu()
        {
            return !(VersionInfoManager.version != null && VersionInfoManager.version.DisableRoomJoin == true
            && VersionInfoManager.allversion != null && VersionInfoManager.allversion.DisableRoomJoin == true);
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

            var version = VersionInfoManager.version;
            var allVersion = VersionInfoManager.allversion;

            if (Findbuttongo)
            {
                // var disable = version == null || version.DisableMM || (allVersion != null && allVersion.DisableMM);
                //Findbuttongo.SetActive(!disable);
                Findbuttongo.SetActive(false);
            }
            if (Codebutton && ((VersionInfoManager.version != null && VersionInfoManager.version.DisableRoomJoin == true) ||
            VersionInfoManager.allversion != null && VersionInfoManager.allversion.DisableRoomJoin == true))
            {
                var buttonCollider = Codebutton.GetComponent<BoxCollider2D>();
                buttonCollider.offset = new(100f, 100f);
            }
            if (Main.IsAndroid())
            {
                //    createbutton.GetComponent<BoxCollider2D>().offset = new(100f, 100);
            }

            if (CredentialsPatch.TOHPLogo != null)
            {
                CredentialsPatch.TOHPLogo.gameObject.SetActive(false);
            }
            if (VersionMenu != null)
                VersionMenu.SetActive(false);
            if (betaVersionMenu != null)
                betaVersionMenu.SetActive(false);
            if (Statistics_TMP.gameObject != null)
                Statistics_TMP.gameObject.SetActive(false);
            if (Statistics_ScrollStuff?.gameObject != null)
                Statistics_ScrollStuff?.gameObject.SetActive(false);

            {
                var warning = GameObject.Find("MainMenuManager/MainUI/AspectScaler/RightPanel/MaskedBlackScreen/OnlineButtons/AspectSize/CrossplayWarning");
                warning.SetActive(true);
                var TMPobjct = GameObject.Find("MainMenuManager/MainUI/AspectScaler/RightPanel/MaskedBlackScreen/OnlineButtons/AspectSize/CrossplayWarning/CrossPlayText/Text_TMP");

                var TMP = TMPobjct.transform.GetComponent<TextMeshPro>();
                var cantJoin = VersionInfoManager.version != null && VersionInfoManager.version.DisableRoomJoin == true;
                cantJoin |= VersionInfoManager.allversion != null && VersionInfoManager.allversion.DisableRoomJoin == true;
                var text = Main.IsAndroid() ? Translator.GetString("CantAndroidCreateGame") : cantJoin ? Translator.GetString("CantPublickAndJoin") : "";
                TMP.SetText(text);
                _ = new LateTask(() => TMP.SetText(text), 0.05f, "Set", true);
                if (text == "") warning.SetActive(false);
            }
        }
        [HarmonyPatch(nameof(MainMenuManager.ResetScreen)), HarmonyPostfix]
        public static void ResetScreenPostfix()
        {
            if (CredentialsPatch.TOHPLogo != null)
            {
                CredentialsPatch.TOHPLogo?.gameObject?.SetActive(true);
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