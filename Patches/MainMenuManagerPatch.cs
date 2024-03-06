using System;
using HarmonyLib;
using TownOfHost.Templates;
using UnityEngine;
using Object = UnityEngine.Object;
using AmongUs.Data;
using Assets.InnerNet;
using AmongUs.Data.Player;
using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System.Linq;
using System.Text.RegularExpressions;

namespace TownOfHost
{
    [HarmonyPatch(typeof(MainMenuManager))]
    public class MainMenuManagerPatch
    {
        private static SimpleButton discordButton;
        public static SimpleButton UpdateButton { get; private set; }
        public static SimpleButton UpdateButton2;
        private static SimpleButton gitHubButton;
        private static SimpleButton TwitterXButton;
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
                    new(-2f, -1f, 1f),
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
                    new(2f, -1f, 1f),
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
                    new(0f, -1f, 1f),
                    new(0, 202, 255, byte.MaxValue),
                    new(60, 255, 255, byte.MaxValue),
                    () => Application.OpenURL("https://twitter.com/Tohkserver_k"),
                    "Twitter(X)");
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
                        UpdateButton.Button.gameObject.SetActive(false);
                        ModUpdater.StartUpdate(ModUpdater.downloadUrl);
                    },
                    $"{Translator.GetString("updateButton")}\n{ModUpdater.latestTitle}",
                    new(2.5f, 1f),
                    isActive: false);
            }
            // アップデート(詳細)ボタンを生成
            if (UpdateButton2 == null)
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

#if RELEASE
            // フリープレイの無効化
            var howToPlayButton = __instance.howToPlayButton;
            var freeplayButton = howToPlayButton.transform.parent.Find("FreePlayButton");
            if (freeplayButton != null)
            {
                freeplayButton.gameObject.SetActive(false);
            }
            // フリープレイが消えるのでHowToPlayをセンタリング
            howToPlayButton.transform.SetLocalX(0);
#endif
        }

        /// <summary>TOHロゴの子としてボタンを生成</summary>
        /// <param name="name">オブジェクト名</param>
        /// <param name="normalColor">普段のボタンの色</param>
        /// <param name="hoverColor">マウスが乗っているときのボタンの色</param>
        /// <param name="action">押したときに発火するアクション</param>
        /// <param name="label">ボタンのテキスト</param>
        /// <param name="scale">ボタンのサイズ 変更しないなら不要</param>
        private static SimpleButton CreateButton(
            string name,
            Vector3 localPosition,
            Color32 normalColor,
            Color32 hoverColor,
            Action action,
            string label,
            Vector2? scale = null,
            bool isActive = true)
        {
            var button = new SimpleButton(CredentialsPatch.TohkLogo.transform, name, localPosition, normalColor, hoverColor, action, label, isActive);
            if (scale.HasValue)
            {
                button.Scale = scale.Value;
            }
            return button;
        }

        // プレイメニュー，アカウントメニュー，クレジット画面が開かれたらロゴとボタンを消す
        [HarmonyPatch(nameof(MainMenuManager.OpenGameModeMenu))]
        [HarmonyPatch(nameof(MainMenuManager.OpenAccountMenu))]
        [HarmonyPatch(nameof(MainMenuManager.OpenCredits))]
        [HarmonyPostfix]
        public static void OpenMenuPostfix()
        {
            if (CredentialsPatch.TohkLogo != null)
            {
                CredentialsPatch.TohkLogo.gameObject.SetActive(false);
            }
        }
        [HarmonyPatch(nameof(MainMenuManager.ResetScreen)), HarmonyPostfix]
        public static void ResetScreenPostfix()
        {
            if (CredentialsPatch.TohkLogo != null)
            {
                CredentialsPatch.TohkLogo.gameObject.SetActive(true);
            }
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

    //TOH_Yを参考にさせて貰いました ありがとうございます
    [HarmonyPatch]
    public class ModNewsHistory
    {
        public static List<ModNews> AllModNews = new();
        public static void Init()
        {
            {
                {
                    var news = new ModNews
                    {
                        Number = 100002,
                        //BeforeNumber = 0,
                        Title = "ハッピーハロウィンついにTOH-Kリリース！",
                        SubTitle = "やっとリリースしたよ！",
                        ShortTitle = "◆TOH-K v5.1.14",
                        Text = "ハロウィンにリリースしたのだぁー\n\rってことで(?)TOH-Kを使ってくれてありがとおおお!\n\r\n\rあ、詳しくは<nobr><link=\"https://github.com/KYMario/TownOfHost-K\">README</nobr></link> 見てね～\n\r\n\rマジでここなに書いたらいいんやろな なにも思いつかないぜ(これただ独り言めっちゃ書いてるやばいやつだ)\n\rまぁTOH-Kのこと話します 初リリースってことで元々24役職(ﾈﾀ役職含め)あったのを12役職まで減らしたんだぜ！ 多分いつかアプデで一部は追加すると思う\n\rそ～し～て～実は隠し要素あります！ 1つはコマンド、もう一つは隠しコマンド(key)で使えるようになるよ！探してみてね\n\rあとYouTubeとかTwitter(X)でTOHkの動画とかじゃんじゃん投稿しちゃって！ あ、でもちゃんとMODで本家じゃなくてTOHkってことわかるようにしてね それだけ守ってくれれば.. 配信とか動画で使ってくれるとめちゃ喜びます！\n\rそれじゃあこのぐらいでいいかな、じゃあkを楽しんできてね～\n\r\n\rTOH-K開発者: けーわい,タイガー,夜藍/中の人,ねむa\nサポーター:りぃりぃ",
                        Date = "2023-10-31T00:00:00Z"
                    };
                    AllModNews.Add(news);
                }
                {
                    var news = new ModNews
                    {
                        Number = 100003,
                        Title = "もう12月！？ メリクリ～",
                        SubTitle = "Town Of Host-K v5.1.31",
                        ShortTitle = "◆TOH-K v5.1.31",
                        Text = "TOH v5.1.3への対応\nテレポートキラーに自爆設定が追加されたよ!\n (ターゲットが)ベントやぬーん、梯子を使っている時自爆する設定を追加したのだ！\n次はジャッカルマフィア!\n ジャッカルがｼﾞｬｯｶﾙﾏﾌｨｱを視認できるか(その逆も)\n ジャッカルがｼﾞｬｯｶﾙﾏﾌｨｱをキルできるかも設定できるようになったよぉ\nあと参加者がタイマーコマンド使うと霊界送りにされるﾔﾊﾞｲﾔﾂも直したのだ",
                        Date = "2023-12-23T00:00:00Z"
                    };
                    AllModNews.Add(news);
                }
                {
                    var news = new ModNews
                    {
                        Number = 100004,
                        Title = "ハッピーニューイヤー",
                        SubTitle = "Town Of Host-K v5.1.45",
                        ShortTitle = "◆TOH-K v5.1.45",
                        Text = "TOH v5.1.4への対応\nメインメニューにX(Twitter)のボタンを追加したよ！\n是非フォローよろしくね^^(乞食)\nそして新役職！大狼の追加!!\nシェリフが大狼をキルとシェリフが誤爆する。\nまた、占い師が大狼を占ってもクルーメイトと表示される!\n新ゲームモード タスクバトル解放！\nタスクを誰よりも早く終わらせよう\nボタンが押せなく、インポスターがいないモードです\nタスクを全て終わらせると自動でゲームが終了します\n (実はv5.1.14から隠しコマンドとしてあったり..)\n\nそしてこれがほぼGithubのと同じになってる..\nってことでいつも通りここになにか書いときます((\n今回のアプデ、kyは..\n\n\n\n\n\nタスクバトルの解放以外なにもしていない!!(((殴\nﾀﾞｯﾃ!ﾀﾞﾚﾓkﾔｯﾃｸﾝﾅｲｼﾞｬﾝ!!(((\n\nてかﾊｯﾋﾟｰﾆｭｰｲﾔｰってもう遅かったりする..?",
                        Date = "2024-01-19T00:00:00Z"
                    };
                    AllModNews.Add(news);
                }
                {
                    var news = new ModNews
                    {
                        Number = 100005,
                        Title = "鬼退治だ！！！",
                        SubTitle = "Town Of Host-K v5.1.46",
                        ShortTitle = "◆TOH-K v5.1.46",
                        Text = "新役職大量に追加したよ(ﾀｲﾘｮｳﾅﾉｶｼﾗﾝｹﾄﾞ)\n各役職紹介は<nobr><link=\"https://github.com/KYMario/TownOfHost-K\">README</nobr></link>をご覧ください！\n\n大狼とマッドジェスター、占い師に設定を追加！\n\n大狼はシェリフ誤爆時の死因を変えるかの設定追加\nマッドジェスターはベント使えるかの有無を追加\n占い師に能力を発揮するタスク数を追加!\n\n機能面の追加もあるよ！\nキルークール0sでやってみたいよね？そんな時は設定で出来るようになったよ！\n/sw 勝利させたい陣営 のコマンド追加！(ｱﾝﾏﾂｶﾜﾝｶﾓ)\n\n\n最後にK開発者2人増えた事をお知らせするね！\nみんなが沢山遊んでくれるMODにしていくのでみんなよろしくね^^",
                        Date = "2024-02-03T00:00:00Z"
                    };
                    AllModNews.Add(news);
                }
                {
                    var news = new ModNews
                    {
                        Number = 100006,
                        Title = "ハッピーバレンタイン！",
                        SubTitle = "Town Of Host-K v5.1.47",
                        ShortTitle = "◆TOH-K v5.1.47",
                        Text = "今回はバグ修正や設定追加が基本だ！ﾀﾀﾞｼ新役職もあるぞい！\n\n新役職\n\nマジシャン！\n天秤！なんかこの役職聞いたことあるって？ｿﾝﾅﾉｷﾆｼﾀﾗﾏｹﾀﾞ!\n\nバグ修正\n\nウルトラスターが霊界でキルしてしまう問題\n非ホストModクライアントがいると発生する問題(一部)\n\n変更箇所\n\nぽんこつ占い師:\n占い失敗時今まではクルーメイト固定だったのが死亡者、自身、投票相手含むゲームにある役職が結果として表示されるように(更にぽんこつ度アップに)\n\n死神:\n死体通報出来ないように変更\n\nウルトラスター:\nキル範囲をめっちゃ狭くしたぜ\n\n設定追加\n\n設定場所の移動、まとめたり、カラフルにしました。\n\nマッドメイト系役職の設定にレポート出来ない設定追加\n\nウォッチャーをマッドメイトにアサインするように設定追加、それに伴いマッドメイト系役職の設定から他の人の投票先が見れる設定削除\n\nぽんこつ占い師、マッドテラーに陣営占い設定追加\n\n死神に緊急会議ボタンが使用可能かの設定追加\n\n自投票で能力発動する役職に1会議に使う能力を制限する設定追加\n\nクライアント設定に、「推奨設定を隠す」追加\n\nこの他にも変更あるから<nobr><link=\"https://github.com/KYMario/TownOfHost-K\">README</nobr></link>を見てね\n\nボイスコマンドにset追加。使い方は/voって打とう！",
                        Date = "2024-02-14T00:00:00Z"
                    };
                    AllModNews.Add(news);
                }
                {
                    var news = new ModNews
                    {
                        Number = 100007,
                        Title = "ひな祭り",
                        SubTitle = "Town Of Host-K v5.1.48",
                        ShortTitle = "◆TOH-K v5.1.48",
                        Text = "バグ修正\n\n天秤の会議にて会議が終わらなくなる問題\n天秤会議にて、同数投票でホストが追放された時「どちらも追放された。」と表示されない問題\nGMが入ってる場合会議が終わらない問題\nマジシャンのマジックでキルした時のキルクールが元の半分になる問題\nマジシャンのハットの色がホストの色になってしまう問題\nホスト以外のmod導入済みプレイヤーがシャイボーイになったとき、mod導入者がBANされる問題\nメイヤーの覚醒OFF設定でも覚醒してしまう問題\nワークホースが配役されない問題",
                        Date = "2024-03-03T00:00:00Z"
                    };
                    AllModNews.Add(news);
                }
                {
                    var news = new ModNews
                    {
                        Number = 100008,
                        Title = "v2024.3.5sに対応したのだ～",
                        SubTitle = "Town Of Host-K v5.1.48.2",
                        ShortTitle = "◆TOH-K v5.1.48.2",
                        Text = "朝起きてk鯖見たらなんか#downloadにあったから\nこっちも対抗だぁ!()\n\nk5.1.48の入れ忘れ:\nホスト以外のmod導入済みプレイヤーがシャイボーイになったとき、mod導入者がBANされる問題の修正\n\n他:\nv2024.3.5sでゲームを開始することができなかったのを修正\n\n※このバージョンはv2023.11.28sでは動きません。\nk5.1.48.5でなにか問題があったときに使ってください。",
                        Date = "2024-03-06T00:00:00Z"
                    };
                    AllModNews.Add(news);
                }
                AnnouncementPopUp.UpdateState = AnnouncementPopUp.AnnounceState.NotStarted;
            }
        }

        [HarmonyPatch(typeof(PlayerAnnouncementData), nameof(PlayerAnnouncementData.SetAnnouncements)), HarmonyPrefix]
        public static bool SetModAnnouncements(PlayerAnnouncementData __instance, [HarmonyArgument(0)] ref Il2CppReferenceArray<Announcement> aRange)
        {
            if (AllModNews.Count < 1)
            {
                Init();
                AllModNews.Sort((a1, a2) => { return DateTime.Compare(DateTime.Parse(a2.Date), DateTime.Parse(a1.Date)); });
            }

            List<Announcement> FinalAllNews = new();
            AllModNews.Do(n => FinalAllNews.Add(n.ToAnnouncement()));
            foreach (var news in aRange)
            {
                if (!AllModNews.Any(x => x.Number == news.Number))
                    FinalAllNews.Add(news);
            }
            FinalAllNews.Sort((a1, a2) => { return DateTime.Compare(DateTime.Parse(a2.Date), DateTime.Parse(a1.Date)); });

            aRange = new(FinalAllNews.Count);
            for (int i = 0; i < FinalAllNews.Count; i++)
                aRange[i] = FinalAllNews[i];

            return true;
        }
    }
}
