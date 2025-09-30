using System;
using System.IO;
using System.Text.RegularExpressions;
using HarmonyLib;
using TownOfHost.Attributes;
using static TownOfHost.Translator;

namespace TownOfHost
{
    public static class BanManager
    {
        private static readonly string DENY_NAME_LIST_PATH = Main.BaseDirectory + "/DenyName.txt";
        private static readonly string BAN_LIST_PATH = Main.BaseDirectory + "/BanList.txt";
        private static readonly string WhiteList_LIST_PATH = Main.BaseDirectory + "/WhiteList.txt";

        [PluginModuleInitializer]
        public static void Init()
        {
            Directory.CreateDirectory(Main.BaseDirectory);
            if (!File.Exists(DENY_NAME_LIST_PATH)) File.Create(DENY_NAME_LIST_PATH).Close();
            if (!File.Exists(BAN_LIST_PATH)) File.Create(BAN_LIST_PATH).Close();
            if (!File.Exists(WhiteList_LIST_PATH)) File.Create(WhiteList_LIST_PATH).Close();
        }
        public static void AddBanPlayer(InnerNet.ClientData player)
        {
            if (!AmongUsClient.Instance.AmHost || player == null) return;
            if (!CheckBanList(player?.FriendCode, player?.ProductUserId))
            {
                if (player?.ProductUserId is not "" and not null and not "e3b0cb855")
                {
                    var additionalInfo = "";
                    File.AppendAllText(BAN_LIST_PATH, $"{player?.FriendCode},{player?.ProductUserId},{player.PlayerName.RemoveHtmlTags()}{additionalInfo}\n");
                    Logger.seeingame(string.Format(GetString("Message.AddedPlayerToBanList"), player.PlayerName));
                }
                else Logger.Info($"Failed to add player {player?.PlayerName.RemoveHtmlTags()}/{player?.FriendCode}/{player?.ProductUserId} to ban list!", "AddBanPlayer");
            }
        }
        public static void AddWhitePlayer(InnerNet.ClientData player)
        {
            if (!AmongUsClient.Instance.AmHost || player == null)
            {
                Logger.seeingame($"{player?.PlayerName} → ぬるぽ / ホストじゃないから処理キャンセル");
                return;
            }
            if (CheckWhiteList(player?.FriendCode, player?.ProductUserId))
            {
                Logger.seeingame($"{player?.PlayerName} → ホワイトリストに記載済みだよ！！");
                return;
            }

            if (!CheckBanList(player?.FriendCode, player?.ProductUserId))
            {
                if (player?.ProductUserId is not "" and not null and not "e3b0cb855")
                {
                    var additionalInfo = "";
                    File.AppendAllText(WhiteList_LIST_PATH, $"{player?.FriendCode},{player?.ProductUserId},{player.PlayerName.RemoveHtmlTags()}{additionalInfo}\n");
                    Logger.seeingame(string.Format(GetString("Message.AddedWhiteList"), player.PlayerName));
                }
                else
                {
                    Logger.seeingame($"{player?.PlayerName} → PUIDがぬるぽ！追加できませんでした！");
                    Logger.Info($"Failed to add player {player?.PlayerName.RemoveHtmlTags()}/{player?.FriendCode}/{player?.ProductUserId} to White list!", "AddWhitePlayer");
                }
            }
            else
            {
                Logger.seeingame($"{player?.PlayerName.RemoveHtmlTags()}はBANListに記載済み...");
            }
        }
        public static void CheckDenyNamePlayer(InnerNet.ClientData player)
        {
            if (!AmongUsClient.Instance.AmHost || !Options.ApplyDenyNameList.GetBool()) return;
            try
            {
                Directory.CreateDirectory(Main.BaseDirectory);
                if (!File.Exists(DENY_NAME_LIST_PATH)) File.Create(DENY_NAME_LIST_PATH).Close();
                using StreamReader sr = new(DENY_NAME_LIST_PATH);
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line == "") continue;
                    if (Regex.IsMatch(player.PlayerName, line))
                    {
                        AmongUsClient.Instance.KickPlayer(player.Id, false);
                        Logger.seeingame(string.Format(GetString("Message.KickedByDenyName"), player.PlayerName, line));
                        Logger.Info($"{player.PlayerName}は名前が「{line}」に一致したためキックされました。", "Kick");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "CheckDenyNamePlayer");
            }
        }
        public static void CheckBanPlayer(InnerNet.ClientData player)
        {
            if (!AmongUsClient.Instance.AmHost || !Options.ApplyBanList.GetBool()) return;
            if (CheckBanList(player?.FriendCode, player?.ProductUserId))
            {
                AmongUsClient.Instance.KickPlayer(player.Id, true);
                Logger.seeingame(string.Format(GetString("Message.BanedByBanList"), player.PlayerName));
                Logger.Info($"{player.PlayerName}は過去にBAN済みのためBANされました。", "BAN");
                return;
            }
        }
        public static bool CheckBanList(string code, string hashedpuid = "")
        {
            bool OnlyCheckPuid = false;
            if (code == "" && hashedpuid != "") OnlyCheckPuid = true;
            else if (code == "") return false;
            try
            {
                if (!File.Exists(BAN_LIST_PATH)) File.Create(BAN_LIST_PATH).Close();
                using StreamReader sr = new(BAN_LIST_PATH);
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line == "") continue;
                    if (!OnlyCheckPuid)
                        if (line.Contains(code)) return true;
                    if (line.Contains(hashedpuid)) return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "CheckBanList");
            }
            return false;
        }
        public static bool CheckWhiteList(string code, string hashedpuid = "")
        {
            bool OnlyCheckPuid = false;
            //nullなら空白
            code ??= "";
            hashedpuid ??= "";
            if (code == "" && hashedpuid != "") OnlyCheckPuid = true;
            else if (code == "") return false;
            try
            {
                if (!File.Exists(WhiteList_LIST_PATH)) File.Create(WhiteList_LIST_PATH).Close();
                using StreamReader sr = new(WhiteList_LIST_PATH);
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line == "") continue;
                    if (!OnlyCheckPuid)
                        if (line.Contains(code)) return true;
                    if (line.Contains(hashedpuid)) return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "CheckWhiteList");
            }
            return false;
        }
    }
    [HarmonyPatch(typeof(BanMenu), nameof(BanMenu.Select))]
    class BanMenuSelectPatch
    {
        public static void Postfix(BanMenu __instance, int clientId)
        {
            InnerNet.ClientData recentClient = AmongUsClient.Instance.GetRecentClient(clientId);
            if (recentClient == null) return;
            if (!BanManager.CheckBanList(recentClient?.FriendCode, recentClient?.ProductUserId)) __instance.BanButton.GetComponent<ButtonRolloverHandler>().SetEnabledColors();
        }
    }
}
