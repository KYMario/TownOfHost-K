using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace TownOfHost.Modules
{
    public static class Aiserver
    {
        private const string Url = "http://localhost:5005/ai";

        public static void Send(string prompt, byte senderId)
        {
            Logger.Info("[AI] Send called: " + prompt, "AI");
            Task.Run(async () =>
            {
                Logger.Info("[AI] Task.Run start", "AI");
                try
                {
                    using var client = new HttpClient();
                    client.Timeout = System.TimeSpan.FromSeconds(30);

                    string json = "{\"message\":\"" + EscapeJson(prompt) + "\"}";
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    Logger.Info("[AI] Sending request...", "AI");
                    var res = await client.PostAsync(Url, content);
                    var body = await res.Content.ReadAsStringAsync();
                    Logger.Info("[AI] Response: " + body, "AI");

                    var data = JObject.Parse(body);
                    string reply = data["reply"]?.ToString() ?? "AIエラー";

                    var sender = PlayerCatch.GetPlayerById(senderId);
                    string playerName = sender?.Data?.PlayerName ?? "Unknown";

                    // ★ RPCを使わず直接MessagesToSendに追加
                    Main.MessagesToSend.Add(($"{playerName}: {prompt}", byte.MaxValue, playerName));
                    Main.MessagesToSend.Add(($"<color=#FFA500>ぴけおAI</color>: {reply}", byte.MaxValue, $"<color=#FFA500>ぴけおAI</color>"));
                }
                catch (System.Exception e)
                {
                    Logger.Info("[AI] Error: " + e.Message, "AI");
                    Main.MessagesToSend.Add(($"<color=#FFA500>ぴけおAI</color>: エラーが発生しました", byte.MaxValue, $"<color=#FFA500>ぴけおAI</color>"));
                }
            });
        }

        private static string EscapeJson(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}