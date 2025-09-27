using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using LogLevel = BepInEx.Logging.LogLevel;
using TownOfHost.Modules;

namespace TownOfHost
{
    class Webhook
    {
        public static void Send(string text)
        {
            if (Main.IsAndroid()) return;
            ClientOptionsManager.CheckOptions();
            if (ClientOptionsManager.WebhookUrl == "none" || !Main.UseWebHook.Value) return;
            HttpClient httpClient = new();
            Dictionary<string, string> strs = new()
            {
                { "content", text },
            };
            try
            {
                TaskAwaiter<HttpResponseMessage> awaiter = httpClient.PostAsync(
                    ClientOptionsManager.WebhookUrl, new FormUrlEncodedContent(strs)).GetAwaiter();
                awaiter.GetResult();
            }
            catch
            {
                Logger.Warn("WebHookの送信に失敗", nameof(Webhook));
            }
        }
        //参考元→https://github.com/Dolly1016/Nebula-Public/
        public static void SendResult(byte[] pngImage)
        {
            if (Main.IsAndroid()) return;
            ClientOptionsManager.CheckOptions();
            if (ClientOptionsManager.WebhookUrl == "none" || !Main.UseWebHook.Value) return;
            try
            {
                HttpClient httpClient = new();
                using MultipartFormDataContent content = new();
                content.Add(new ByteArrayContent(pngImage), "file", "image.png");
                var awaiter = httpClient.PostAsync(ClientOptionsManager.WebhookUrl, content).GetAwaiter();
                awaiter.GetResult();
                return;
            }
            catch (Exception e)
            {
                Logger.Info($"{e}", "SendResult");
            }
        }
    }

    class Alert
    {
        /*
        public static void Send(string text, string name = "TownOfHost-K", string avatar = "https://cdn.discordapp.com/attachments/1219855613752774657/1254725875535183933/TabIcon_MainSettings.png?ex=667a8a08&is=66793888&hm=dc20a50c7cadab0a15a215c19abcde6006fbef9911299ab82e452b7cf5242f57&")
        {
            ClientOptionsManager.CheckOptions();
            HttpClient httpClient = new();
            Dictionary<string, string> strs = new()
            {
                { "content", text },
                { "username", name },
                { "avatar_url", avatar }
            };
            TaskAwaiter<HttpResponseMessage> awaiter = httpClient.PostAsync(
                Main.DebugwebURL, new FormUrlEncodedContent(strs)).GetAwaiter();
            awaiter.GetResult();
        }*/
    }

    class Logger
    {
        public static bool isEnable;
        public static List<string> disableList = new();
        public static List<string> sendToGameList = new();
        public static bool isDetail = false;
        public static bool isAlsoInGame = false;
        public static void Enable() => isEnable = true;
        public static void Disable() => isEnable = false;
        public static void Enable(string tag, bool toGame = false)
        {
            disableList.Remove(tag);
            if (toGame && !sendToGameList.Contains(tag)) sendToGameList.Add(tag);
            else sendToGameList.Remove(tag);
        }
        public static void Disable(string tag) { if (!disableList.Contains(tag)) disableList.Add(tag); }
        public static void seeingame(string text, bool isAlways = false)
        {
            if (!isEnable) return;
            if (DestroyableSingleton<HudManager>._instance) DestroyableSingleton<HudManager>.Instance.Notifier.AddDisconnectMessage(text);
        }
        private static void SendToFile(string text, LogLevel level = LogLevel.Info, string tag = "", bool escapeCRLF = true, int lineNumber = 0, string fileName = "")
        {
            if (!isEnable || disableList.Contains(tag)) return;
            var logger = Main.Logger;
            string t = DateTime.Now.ToString("HH:mm:ss:ff");
            if (sendToGameList.Contains(tag) || isAlsoInGame) seeingame($"[{tag}]{text}");
            if (escapeCRLF)
                text = text.Replace("\r", "\\r").Replace("\n", "\\n");
            string log_text = $"[{t}][{tag}]{text}";
            if (isDetail && DebugModeManager.AmDebugger)
            {
                StackFrame stack = new(2);
                string className = stack.GetMethod().ReflectedType.Name;
                string memberName = stack.GetMethod().Name;
                log_text = $"[{t}][{className}.{memberName}({Path.GetFileName(fileName)}:{lineNumber})][{tag}]{text}";
            }
            switch (level)
            {
                case LogLevel.Info:
                    logger.LogInfo(log_text);
                    break;
                case LogLevel.Warning:
                    logger.LogWarning(log_text);
                    break;
                case LogLevel.Error:
                    logger.LogError(log_text);
                    break;
                case LogLevel.Fatal:
                    logger.LogFatal(log_text);
                    break;
                case LogLevel.Message:
                    logger.LogMessage(log_text);
                    break;
                default:
                    logger.LogWarning("Error:Invalid LogLevel");
                    logger.LogInfo(log_text);
                    break;
            }
        }
        public static void Info(string text, string tag, bool escapeCRLF = true, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "") =>
            SendToFile(text, LogLevel.Info, tag, escapeCRLF, lineNumber, fileName);
        public static void Warn(string text, string tag, bool escapeCRLF = true, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "") =>
            SendToFile(text, LogLevel.Warning, tag, escapeCRLF, lineNumber, fileName);
        public static void Error(string text, string tag, bool escapeCRLF = false, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "") =>
            SendToFile(text, LogLevel.Error, tag, escapeCRLF, lineNumber, fileName);
        public static void Fatal(string text, string tag, bool escapeCRLF = true, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "") =>
            SendToFile(text, LogLevel.Fatal, tag, escapeCRLF, lineNumber, fileName);
        public static void Msg(string text, string tag, bool escapeCRLF = true, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "") =>
            SendToFile(text, LogLevel.Message, tag, escapeCRLF, lineNumber, fileName);
        public static void Exception(Exception ex, string tag, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "") =>
            SendToFile(ex.ToString(), LogLevel.Error, tag, false, lineNumber, fileName);
        static float OldDate = -1;
        public static void CheckElapsed(string tag)
        {
            var Nowdate = DateTime.Now;
            var NowTime = Nowdate.Millisecond + Nowdate.Second * 1000 + Nowdate.Minute * 100000 + Nowdate.Hour * 10000000;
            var elapsed = OldDate < 0 ? "null" : $"{NowTime - OldDate}";

            SendToFile($"{DateTime.Now:HH.mm.ss.fff} ({elapsed})", LogLevel.Info, tag);
            OldDate = Nowdate.Millisecond + Nowdate.Second * 1000 + Nowdate.Minute * 100000 + Nowdate.Hour * 10000000;
        }
        public static void CurrentMethod([CallerLineNumber] int lineNumber = 0, [CallerFilePath] string fileName = "")
        {
            StackFrame stack = new(1);
            Logger.Msg($"\"{stack.GetMethod().ReflectedType.Name}.{stack.GetMethod().Name}\" Called in \"{Path.GetFileName(fileName)}({lineNumber})\"", "Method");
        }

        public static LogHandler Handler(string tag, bool escapeCRLF = true)
            => new(tag, escapeCRLF);
    }
}