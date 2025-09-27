using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using TownOfHost.Modules.ChatManager;

namespace TownOfHost;

class Yomiage
// Memo
// 棒読みちゃんを起動していない時に
// 読み上げをONにしてチャットを送信すると
// クラッシュする。
{
    public static Dictionary<int, string> YomiageS = new();
    public static bool ChatCommand(string[] args, byte playerId = 0)
    {
        if (!Main.UseYomiage.Value || !AmongUsClient.Instance.AmHost) return true;
        var indx = 0;
        byte vo0id = PlayerControl.LocalPlayer.PlayerId;
        if ((args.Length < 2 ? "" : args[1]) == "set" && (args.Length < 3 ? "" : args[2]) != "")
        {
            if (playerId != PlayerControl.LocalPlayer.PlayerId) return false;
            if (byte.TryParse(args[2], out vo0id))
            {
                indx += 2;
            }
        }

        string subArgs = args.Length < 2 + indx ? "" : args[indx + 1];
        string subArgs2 = args.Length < 3 + indx ? "" : args[indx + 2];
        string subArgs3 = args.Length < 4 + indx ? "" : args[indx + 3];
        string subArgs4 = args.Length < 5 + indx ? "" : args[indx + 4];

        if (subArgs is "get" or "g" && Main.UseYomiage.Value)
        {
            StringBuilder sb = new();
            foreach (var r in GetvoiceListAsync(true).Result)
                sb.Append($"{r.Key}: {r.Value}\n");
            Utils.SendMessage(sb.ToString(), playerId);
        }
        else if (subArgs != "" && subArgs2 != "" && subArgs3 != "" && subArgs4 != "")
        {
            if (VoiceList is null) GetvoiceListAsync().Wait();
            if (int.TryParse(subArgs, out int vid) && VoiceList.Count > vid)
            {
                var vopc = PlayerCatch.GetPlayerById(vo0id);
                YomiageS[vopc.Data.DefaultOutfit.ColorId] = $"{subArgs} {subArgs2} {subArgs3} {subArgs4}";
                if (AmongUsClient.Instance.AmHost) RPC.SyncYomiage();
                if (vo0id != PlayerControl.LocalPlayer.PlayerId)
                    Utils.SendMessage($"{vopc.name}の声設定を変更しました。", playerId);
            }
            else
            {
                StringBuilder sb = new();
                foreach (var r in GetvoiceListAsync().Result)
                    sb.Append($"{r.Key}: {r.Value}\n");
                Utils.SendMessage(sb.ToString(), playerId);
            }
        }
        return true;
    }

    public static async Task Send(int color, string text = "")
    {
        if (Main.IsAndroid()) return;
        var te = text;
        text = text.RemoveHtmlTags();//Html消す
        if (ChatManager.CommandCheck(text)) return;// /から始まってるならスルー
        if (text != te) return;//htmlタグが入ってる場合は発言じゃないのでスルー
                               // HttpClientを作成
        using var httpClient = new HttpClient();
        try
        {
            ClientOptionsManager.CheckOptions();
            string url = $"http://localhost:{ClientOptionsManager.YomiagePort}/";
            if (YomiageS.ContainsKey(color))
            {
                string[] args = YomiageS[color].Split(' ');
                string y1 = args[0];
                string y2 = args[1];
                string y3 = args[2];
                string y4 = args[3];
                HttpResponseMessage response = await httpClient.GetAsync(url + "talk?text=" + text + "&voice=" + y1 + "&volume=" + y2 + "&speed=" + y3 + "&tone=" + y4);
            }
            else
            {
                HttpResponseMessage response = await httpClient.GetAsync(url + "talk?text=" + text);
            }
        }
        catch (HttpRequestException e)
        {
            // エラーが発生した場合はエラーメッセージを表示
            Logger.Info($"Error: {e.Message}", "yomiage");
            Logger.seeingame("エラーが発生したため、読み上げが無効になりました");
            Main.UseYomiage.Value = false;
        }
    }
    public static Dictionary<int, string> VoiceList;
    public static async Task<Dictionary<int, string>> GetvoiceListAsync(bool forced = false)
    {
        if (Main.IsAndroid()) return null;
        if (VoiceList is null || VoiceList.Count is 0 || forced)
        {
            try
            {
                string result;
                ClientOptionsManager.CheckOptions();
                using (HttpClient client = new())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "TownOfHost-K Updater");
                    using var response = await client.GetAsync(new Uri($"http://localhost:{ClientOptionsManager.YomiagePort}/getvoicelist"), HttpCompletionOption.ResponseContentRead);
                    if (!response.IsSuccessStatusCode || response.Content == null)
                    {
                        Logger.Error($"ステータスコード: {response.StatusCode}", "GetVoiceList");
                        return null;
                    }
                    result = await response.Content.ReadAsStringAsync();
                }
                var voice = JsonSerializer.Deserialize<Voice>(result)?.VoiceList;

                VoiceList = new();
                for (var i = 0; i < voice.Count; i++)
                    VoiceList.Add(i, voice[i].Name);
                return VoiceList;

            }
            catch (HttpRequestException e)
            {
                // エラーが発生した場合はエラーメッセージを表示
                Logger.Info($"Error: {e.Message}", "yomiage");
                Logger.seeingame("エラーが発生したため、読み上げが無効になりました");
                Main.UseYomiage.Value = false;
            }
            return null;
        }
        return VoiceList;
    }
    public class Voice
    {
        public List<VoiceName> VoiceList { get; set; }

        public class VoiceName
        {
            public string Name { get; set; }
        }
    }
}