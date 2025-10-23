using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Csv;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TownOfHost.Attributes;
using TownOfHost.Roles.Core;

namespace TownOfHost
{
    public static class Translator
    {
        public static Dictionary<string, Dictionary<int, string>> translateMaps;
        public const string LANGUAGE_FOLDER_NAME = "Language";

        [PluginModuleInitializer]
        public static void Init()
        {
            Logger.Info("Language Dictionary Initialize...", "Translator");
            LoadLangs();
            Logger.Info("Language Dictionary Initialize Finished", "Translator");
        }
        public static void LoadLangs()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream("TownOfHost.Resources.string.csv");
            translateMaps = new Dictionary<string, Dictionary<int, string>>();

            var options = new CsvOptions()
            {
                HeaderMode = HeaderMode.HeaderPresent,
                AllowNewLineInEnclosedFieldValues = false,
            };
            foreach (var line in CsvReader.ReadFromStream(stream, options))
            {
                if (line.Values[0][0] == '#') continue;
                try
                {
                    Dictionary<int, string> dic = new();
                    for (int i = 1; i < line.ColumnCount; i++)
                    {
                        int id = int.Parse(line.Headers[i]);
                        dic[id] = line.Values[i].Replace("\\n", "\n").Replace("\\r", "\r");
                    }
                    if (!translateMaps.TryAdd(line.Values[0], dic))
                        Logger.Warn($"翻訳用CSVに重複があります。{line.Index}行目: \"{line.Values[0]}\"", "Translator");
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex.ToString(), "Translator");
                }
            }

            if (Main.IsAndroid()) return;
            // カスタム翻訳ファイルの読み込み
            if (!Directory.Exists(LANGUAGE_FOLDER_NAME)) Directory.CreateDirectory(LANGUAGE_FOLDER_NAME);

            // 翻訳テンプレートの作成
            CreateTemplateFile();
            foreach (var lang in EnumHelper.GetAllValues<SupportedLangs>())
            {
                if (File.Exists(@$"./{LANGUAGE_FOLDER_NAME}/{lang}.dat"))
                    LoadCustomTranslation($"{lang}.dat", lang);
            }
        }

        public static string GetString(string s, Dictionary<string, string> replacementDic = null)
        {
            var langId = TranslationController.InstanceExists ? TranslationController.Instance.currentLanguage.languageID : SupportedLangs.English;
            if (Main.ForceJapanese.Value) langId = SupportedLangs.Japanese;
            string str = GetString(s, langId);
            if (replacementDic != null)
                foreach (var rd in replacementDic)
                {
                    str = str.Replace(rd.Key, rd.Value);
                }
            return str;
        }
        public static List<string> NotString = new();
        public static string GetString(string str, SupportedLangs langId)
        {
            var res = $"<INVALID:{str}>";
            var ch = res;
            if ((Event.OptionLoad.Contains(str) || str is "CakeshopInfoLong" or "CakeshopInfo" or "Cakeshop") && !Event.Special && !Event.CheckRole(CustomRoles.Cakeshop)) return res;
            if (str is "VegaInfoLong" or "VegaInfo" or "Vega" && !Event.CheckRole(CustomRoles.Vega)) return res;
            if (str is "AltairInfoLong" or "AltairInfo" or "Altair" && !Event.CheckRole(CustomRoles.Altair)) return res;
            if (str is "Assassin" or "Merlin" && !DebugModeManager.AmDebugger) return res;
            if (translateMaps.TryGetValue(str, out var dic) && (!dic.TryGetValue((int)langId, out res) || res == "")) //strに該当する&無効なlangIdかresが空
            {
                res = $"*{dic[0]}";
            }
            if (str == "Chameleon") res = "カメレオン";
            if (langId == SupportedLangs.Japanese && Main.CustomName.Value)
            {
                //このソースコ―ドを見た人へ。口外しないでもらえると嬉しいです...
                //To anyone who has seen this source code. I would appreciate it if you would keep your mouth shut...
                if (Event.IsChristmas)
                {
                    res = str switch
                    {
                        "Bakery" => "ケーキ作りのパン屋",
                        "BakeryInfo" => "今日はパンじゃなくてケーキだよ！",
                        "ChefInfo" => "全員にチキンを渡そう",
                        "Arsonist" => "ライティングタクロース",
                        "ArsonistInfo" => "全員にプレゼントを渡して逃げろ！",
                        "ArsonistDouseButtonText" => "渡す",
                        "EnterVentToWin" => "ベントに入って逃げろ！",
                        "Message.Bakery1" => "パン屋がケーキを作ったよ！",
                        "SuddenDeathIntro" => "他を落としていい子になれ",
                        "KingInfo" => "ほっほっほ...",
                        "ShyboyInfo" => "雪に埋まりたい",
                        "UltraStarInfo" => "ｼｬｰﾝｼｬｰﾝｼｬｰﾝ",
                        "PonkotuTellerInfo" => "あの子がサンタさん!?",
                        "GrimReaperInfo" => "悪い子にはお仕置きを",
                        "MadonnaInfo" => "ず...ずっと君の事が...!",
                        "WhiteLoversInfo" => "クリスマスが記念日",
                        "JesterInfo" => "あ～した雪ふ～れ",
                        "MadJesterInfo" => "あ～した霰ふ～れ",
                        "EfficientInfo" => "速く終わらせてサンタさんを待たないと..!",
                        "NiceAddoerInfo" => "いっぱいプレゼント貰っちゃったぁ",
                        "JumperInfo" => "サンタは一瞬で過ぎ去るのさ",
                        "BomberInfo" => "僕からもプレゼントがあるよ",
                        "FireWorksInfo" => "聖夜に花火はいかが?",
                        _ => res
                    };
                }
                if (Event.IsHalloween)
                {
                    res = str switch
                    {
                        "MadmateInfo" => "インポスターが有利になるようイタズラしろ",
                        "Bakery" => "お菓子作りのパン屋",
                        "BakeryInfo" => "今日はパンじゃなくてお菓子だよ！",
                        "ChefInfo" => "パン屋とお菓子作りに苦戦中..",
                        "TaskPlayerBInfo" => "タスクを誰よりも早く済ませ人にはお菓子が！？",
                        "Message.Bakery1" => "ハッピーハロウィン！ パン屋がお菓子を作ったよ！",
                        "SuddenDeathIntro" => "お菓子は全て我が頂くのだ",
                        "WolfBoyInfo" => "仮装だよ!!仲間だよっ!!",
                        "ShrineMaidenInfo" => "オバケの声が聞こえる...",
                        "ShyboyInfo" => "ﾓｸﾞﾓｸﾞﾓｸﾞﾓｸﾞﾓｸﾞ...",
                        "PonkotuTellerInfo" => "えっそれ仮装なの!?",
                        "JackalAlienInfo" or "AlienInfo" => "ﾜﾚﾜﾚﾊ..ｳﾁｭｳｼﾞﾝﾀﾞ..",
                        "GrimReaperInfo" => "悪い子には悪戯を",
                        "YellowLoversInfo" => "おそろいのカボチャのコーデ",
                        "EfficientInfo" => "おかしいっぱい食べるぞぉ",
                        "CamouflagerInfo" => "同じ仮装にしちゃうぞ!",
                        "MoleInfo" => "今...君の下にいるよ?",
                        "LimiterInfo" => "せめてお菓子だけは...!",
                        "TairouInfo" => "仮装よくできてるって?ありがとよっ",
                        "VampireInfo" => "噛んじゃうぞぉ～",
                        "ShapeMasterInfo" => "あの子に似てるでしょ!",
                        _ => res
                    };
                }
                if (Event.White)
                {
                    res = str switch
                    {
                        "Bakery" => "ホワイトチョコ作りのパン屋",
                        "BakeryInfo" => "今日はパンじゃなくてホワイトチョコだよ！",
                        "ChefInfo" => "ホワイトチョコを配ろう！",
                        "Message.Bakery1" => "パン屋がホワイトチョコを作ったよ！",
                        "ShyboyInfo" => "ﾁｮ...ﾁｮｺ...!?",
                        "EfficientInfo" => "チョコいっぱい食べないと!",
                        "JumperInfo" => "そこどきなさぁい!!!",
                        "BomberInfo" => "僕からも...",
                        _ => res
                    };
                }
                if (Event.GoldenWeek)
                {
                    res = str switch
                    {
                        "Bakery" => "鯉のぼり作りのパン屋",
                        "BakeryInfo" => "今回はパンではなくこいのぼり制作！",
                        "Message.Bakery1" => "パン屋がこいのぼりを大量に制作したよ...?",
                        "ShyboyInfo" => "人だかりが多いよぉ...",
                        "JesterInfo" => "あ～した天気になぁれ",
                        "MadJesterInfo" => "あ～した雨になぁれ",
                        "EfficientInfo" => "あそこも行って..こっちにも...",
                        "JumperInfo" => "春は過ぎ去った...",
                        "EraserInfo" => "宿題終わらないよぉ...",
                        "QuickKillerInfo" => "3分前のバスに...",
                        "WalkerInfo" => "歩き疲れたよ!",
                        "OneLoveInfo" => "今度...水族館行かない?",
                        _ => res
                    };
                }
            }
            if (!translateMaps.ContainsKey(str)) //translateMapsにない場合、StringNamesにあれば取得する
            {
                var stringNames = EnumHelper.GetAllValues<StringNames>().Where(x => x.ToString() == str);
                if (stringNames != null && stringNames.Any())
                    res = GetString(stringNames.FirstOrDefault());
            }
            if (res == ch)
            {
                if (NotString.Contains(res)) return res;
                NotString.Add(res);
                Logger.Warn($"未翻訳の関数 : {str}", "Translator");
            }
            return res;
        }
        public static string GetString(StringNames stringName)
            => DestroyableSingleton<TranslationController>.Instance.GetString(stringName, new Il2CppReferenceArray<Il2CppSystem.Object>(0));
        public static string GetRoleString(string str)
        {
            var CurrentLanguage = TranslationController.Instance.currentLanguage.languageID;
            var lang = CurrentLanguage;
            if (Main.ForceJapanese.Value && Main.JapaneseRoleName.Value)
                lang = SupportedLangs.Japanese;
            else if (CurrentLanguage == SupportedLangs.Japanese && !Main.JapaneseRoleName.Value)
                lang = SupportedLangs.English;

            return GetString(str, lang);
        }
        public static void LoadCustomTranslation(string filename, SupportedLangs lang)
        {
            if (Main.IsAndroid()) return;
            string path = @$"./{LANGUAGE_FOLDER_NAME}/{filename}";
            if (File.Exists(path))
            {
                Logger.Info($"カスタム翻訳ファイル「{filename}」を読み込み", "LoadCustomTranslation");
                using StreamReader sr = new(path, Encoding.GetEncoding("UTF-8"));
                string text;
                string[] tmp = Array.Empty<string>();
                while ((text = sr.ReadLine()) != null)
                {
                    tmp = text.Split(":");
                    if (tmp.Length > 1 && tmp[1] != "")
                    {
                        try
                        {
                            translateMaps[tmp[0]][(int)lang] = tmp.Skip(1).Join(delimiter: ":").Replace("\\n", "\n").Replace("\\r", "\r");
                        }
                        catch (KeyNotFoundException)
                        {
                            Logger.Warn($"「{tmp[0]}」は有効なキーではありません。", "LoadCustomTranslation");
                        }
                    }
                }
            }
            else
            {
                Logger.Error($"カスタム翻訳ファイル「{filename}」が見つかりませんでした", "LoadCustomTranslation");
            }
        }

        private static void CreateTemplateFile()
        {
            var sb = new StringBuilder();
            foreach (var title in translateMaps) sb.Append($"{title.Key}:\n");
            File.WriteAllText(@$"./{LANGUAGE_FOLDER_NAME}/template.dat", sb.ToString());
            sb.Clear();
            foreach (var title in translateMaps) sb.Append($"{title.Key}:{title.Value[0].Replace("\n", "\\n").Replace("\r", "\\r")}\n");
            File.WriteAllText(@$"./{LANGUAGE_FOLDER_NAME}/template_English.dat", sb.ToString());
        }
        public static void ExportCustomTranslation()
        {
            LoadLangs();
            var sb = new StringBuilder();
            var lang = TranslationController.Instance.currentLanguage.languageID;
            foreach (var title in translateMaps)
            {
                if (!title.Value.TryGetValue((int)lang, out var text)) text = "";
                sb.Append($"{title.Key}:{text.Replace("\n", "\\n").Replace("\r", "\\r")}\n");
            }
            File.WriteAllText(@$"./{LANGUAGE_FOLDER_NAME}/export_{lang}.dat", sb.ToString());
        }
    }
}