using System;
using System.IO;
using System.Linq;
using System.Text;
using HarmonyLib;

using TownOfHost.Attributes;

namespace TownOfHost
{
    public static class ClientOptionsManager
    {
        private static readonly string OPTIONS_FILE_PATH = "./TOHK_DATA/options.txt";
        private static readonly string DEFAULT = "//default:none\nWebHookUrl:none\n//default:50080\nYomiagePort:50080\n\n// Don't Change The Value. / この値を変更しないでください。\nverison:1";
        private static readonly int Version = 1;
        public static string WebhookUrl = "none";
        public static string YomiagePort = "50080";
        [PluginModuleInitializer]
        public static void Init()
        {
            CreateIfNotExists();
        }

        public static void CreateIfNotExists()
        {
            if (Main.IsAndroid()) return;
            if (!File.Exists(OPTIONS_FILE_PATH))
            {
                try
                {
                    if (!Directory.Exists(@"TOHK_DATA")) Directory.CreateDirectory(@"TOHK_DATA");
                    if (File.Exists(@"./options.txt"))
                    {
                        File.Move(@"./options.txt", OPTIONS_FILE_PATH);
                    }
                    else
                    {
                        Logger.Info("Among Us.exeと同じフォルダにoptions.txtが見つかりませんでした。新規作成します。", "OptionsManager");
                        File.WriteAllText(OPTIONS_FILE_PATH, DEFAULT);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex, "OptionsManager");
                }
            }
        }

        public static void CheckOptions()
        {
            //CreateIfNotExists();
            CheckVersion();
            using StreamReader sr = new(OPTIONS_FILE_PATH, Encoding.GetEncoding("UTF-8"));
            string text;
            string[] tmp = Array.Empty<string>();

            while ((text = sr.ReadLine()) != null)
            {
                tmp = text.Split(":");
                if (tmp.Length > 1 && tmp[1] != "")
                {
                    if (tmp[0].ToLower() == "webhookurl")
                    {
                        var none = tmp.Skip(1).Join(delimiter: ":").ToLower() == "none";
                        WebhookUrl = none ? "none" : tmp.Skip(1).Join(delimiter: ":");
                    }
                    if (tmp[0].ToLower() == "yomiageport") YomiagePort = tmp.Skip(1).Join(delimiter: ":");
                }
            }
        }
        public static void CheckVersion(StreamReader sr = null)
        {
            if (sr == null)
            {
                if (!File.Exists(OPTIONS_FILE_PATH))
                {
                    CreateIfNotExists();
                    return; //options.textがなかったらチェックしない
                }
                sr = new(OPTIONS_FILE_PATH, Encoding.GetEncoding("UTF-8"));
            }
            string text;
            string[] tmp = Array.Empty<string>();

            while ((text = sr.ReadLine()) != null)
            {
                tmp = text.Split(":");
                if (tmp.Length > 1 && tmp[1] != "")
                {
                    if (tmp[0].ToLower() == "verison")
                    {
                        if (tmp.Skip(1).Join(delimiter: ":") == $"{Version}") return;
                        sr.Close();
                        Logger.Info("バージョンが違うからデフォ値で上書きするのだ！", "OptionsManager");
                        try
                        {
                            File.WriteAllText(OPTIONS_FILE_PATH, DEFAULT);
                        }
                        catch (Exception ex)
                        {
                            Logger.Exception(ex, "OptionsManager");
                        }
                        return;

                    }
                }
            }
            sr.Close();
            Logger.Info("バージョン情報ないなら帰れにゃ ﾊﾞｲﾊﾞｲ\\(^^)", "OptionsManager");
            try
            {
                File.WriteAllText(OPTIONS_FILE_PATH, DEFAULT);
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "OptionsManager");
            }
        }
    }
}
