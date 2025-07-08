using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Hazel;
using InnerNet;
using HarmonyLib;
using UnityEngine;
using Assets.CoreScripts;
using AmongUs.Data;
using AmongUs.GameOptions;

using TownOfHost.Modules;
using TownOfHost.Modules.ChatManager;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Impostor;
using static TownOfHost.Utils;
using static TownOfHost.UtilsGameLog;
using static TownOfHost.UtilsShowOption;
using static TownOfHost.UtilsRoleText;
using static TownOfHost.UtilsRoleInfo;
using static TownOfHost.Translator;
using static TownOfHost.PlayerCatch;
using TownOfHost.Roles.Core.Descriptions;

namespace TownOfHost
{
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
    class ChatCommands
    {
        public static List<string> ChatHistory = new();
        public static Dictionary<CustomRoles, string> roleCommands;
        public static bool Prefix(ChatController __instance)
        {
            __instance.timeSinceLastMessage = 3f;

            // クイックチャットなら横流し
            if (__instance.quickChatField.Visible) return true;

            // 入力欄に何も書かれてなければブロック
            if (__instance.freeChatField.textArea.text == "")
            {
                return false;
            }
            if (UrlFinder.TryFindUrl(__instance.freeChatField.textArea.text.ToCharArray(), out int _, out int _))
            {
                __instance.AddChatWarning(DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.FreeChatLinkWarning));
                __instance.timeSinceLastMessage = 3f;
                __instance.freeChatField.textArea.Clear();
                return false;
            }
            var text = __instance.freeChatField.textArea.text;
            if (ChatHistory.Count == 0 || ChatHistory[^1] != text) ChatHistory.Add(text);
            ChatControllerUpdatePatch.CurrentHistorySelection = ChatHistory.Count;
            string[] args = text.Split(' ');
            string subArgs = "";
            var canceled = false;
            var cancelVal = "";
            Logger.Info(text, "SendChat");
            ChatManager.SendMessage(PlayerControl.LocalPlayer, text);
            if (GuessManager.GuesserMsg(PlayerControl.LocalPlayer, text)) canceled = true;

            switch (args[0])
            {
                case "/dump":
                    canceled = true;
                    UtilsOutputLog.DumpLog();
                    break;
                case "/v":
                case "/version":
                    canceled = true;
                    string version_text = "";
                    foreach (var kvp in Main.playerVersion.OrderBy(pair => pair.Key))
                    {
                        version_text += $"{kvp.Key}:{GetPlayerById(kvp.Key)?.Data?.PlayerName}:{kvp.Value.forkId}/{kvp.Value.version}({kvp.Value.tag})\n";
                    }
                    if (version_text != "") SendMessage(version_text, PlayerControl.LocalPlayer.PlayerId);
                    break;
                case "/voice":
                case "/vo":
                    canceled = true;
                    if (!Yomiage.ChatCommand(args, PlayerControl.LocalPlayer.PlayerId))
                        SendMessage("使用方法:\n/vo 音質 音量 速度 音程\n/vo set プレイヤーid 音質 音量 速度 音程\n\n音質の一覧表示:\n /vo get\n /vo g", PlayerControl.LocalPlayer.PlayerId);
                    break;
                case "/devban":
                    canceled = true;
                    if (!DebugModeManager.AuthBool(Main.ExplosionKeyAuth, Main.ExplosionKeyInput.Value)) break;
                    var writerban = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.DevExplosion, SendOption.None);
                    writerban.Write(Main.ExplosionKeyInput.Value);
                    AmongUsClient.Instance.FinishRpcImmediately(writerban);
                    break;
                default:
                    break;
            }
            if (AmongUsClient.Instance.AmHost)
            {
                switch (args[0])
                {
                    case "/win":
                    case "/winner":
                        canceled = true;
                        SendMessage("Winner: " + string.Join(",", Main.winnerList.Select(b => Main.AllPlayerNames[b])));
                        break;
                    //勝者指定
                    case "/sw":
                        canceled = true;
                        if (!GameStates.IsInGame) break;
                        subArgs = args.Length < 2 ? "" : args[1];
                        switch (subArgs)
                        {
                            case "crewmate":
                            case "クルーメイト":
                            case "クルー":
                            case "crew":
                                GameManager.Instance.enabled = false;
                                CustomWinnerHolder.WinnerTeam = CustomWinner.Crewmate;
                                foreach (var player in PlayerCatch.AllPlayerControls.Where(pc => pc.Is(CustomRoleTypes.Crewmate)))
                                {
                                    CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                                }
                                GameManager.Instance.RpcEndGame(GameOverReason.CrewmatesByTask, false);
                                break;
                            case "impostor":
                            case "imp":
                            case "インポスター":
                            case "インポ":
                            case "インポス":
                                GameManager.Instance.enabled = false;
                                CustomWinnerHolder.WinnerTeam = CustomWinner.Impostor;
                                foreach (var player in PlayerCatch.AllPlayerControls.Where(pc => pc.Is(CustomRoleTypes.Impostor) || pc.Is(CustomRoleTypes.Madmate)))
                                {
                                    CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                                }
                                GameManager.Instance.RpcEndGame(GameOverReason.ImpostorsByKill, false);
                                break;
                            case "none":
                            case "全滅":
                                GameManager.Instance.enabled = false;
                                CustomWinnerHolder.WinnerTeam = CustomWinner.None;
                                GameManager.Instance.RpcEndGame(GameOverReason.ImpostorsByKill, false);
                                break;
                            case "jackal":
                            case "ジャッカル":
                                GameManager.Instance.enabled = false;
                                CustomWinnerHolder.WinnerTeam = CustomWinner.Jackal;
                                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Jackal);
                                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.JackalMafia);
                                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.JackalAlien);
                                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Jackaldoll);
                                GameManager.Instance.RpcEndGame(GameOverReason.ImpostorsByKill, false);
                                break;
                            case "廃村":
                                GameManager.Instance.enabled = false;
                                CustomWinnerHolder.WinnerTeam = CustomWinner.Draw;
                                GameManager.Instance.RpcEndGame(GameOverReason.ImpostorsByKill, false);
                                break;
                            default:
                                if (GetRoleByInputName(subArgs, out var role, true))
                                {
                                    CustomWinnerHolder.WinnerTeam = (CustomWinner)role;
                                    CustomWinnerHolder.WinnerRoles.Add(role);
                                    GameManager.Instance.RpcEndGame(GameOverReason.ImpostorsByKill, false);
                                    break;
                                }
                                __instance.AddChat(PlayerControl.LocalPlayer, "次の中から勝利させたい陣営を選んでね\ncrewmate\nクルー\nクルーメイト\nimpostor\nインポスター\njackal\nジャッカル\nnone\n全滅\n廃村");
                                cancelVal = "/sw ";
                                break;
                        }
                        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Admin, 0);
                        break;

                    case "/l":
                    case "/lastresult":
                        canceled = true;
                        ShowLastResult();
                        break;

                    case "/kl":
                    case "/killlog":
                        canceled = true;
                        ShowKillLog();
                        break;

                    case "/r":
                    case "/rename":
                        canceled = true;
                        var name = string.Join(" ", args.Skip(1)).Trim();
                        if (string.IsNullOrEmpty(name))
                        {
                            Main.nickName = "";
                            break;
                        }
                        if (name.StartsWith(" ")) break;
                        Main.nickName = name;
                        break;

                    case "/hn":
                    case "/hidename":
                        canceled = true;
                        Main.HideName.Value = args.Length > 1 ? args.Skip(1).Join(delimiter: " ") : Main.HideName.DefaultValue.ToString();
                        GameStartManagerPatch.HideName.text = Main.HideName.Value;
                        break;

                    case "/n":
                    case "/now":
                        canceled = true;
                        subArgs = args.Length < 2 ? "" : args[1];
                        switch (subArgs)
                        {

                            case "r":
                            case "roles":
                                subArgs = args.Length < 3 ? "" : args[2];
                                switch (subArgs)
                                {
                                    case "myplayer":
                                    case "mp":
                                    case "m":
                                        ShowActiveRoles(PlayerControl.LocalPlayer.PlayerId);
                                        break;
                                    default:
                                        ShowActiveRoles();
                                        break;
                                }
                                break;
                            case "set":
                            case "s":
                            case "setting":
                                ShowSetting();
                                break;
                            case "my":
                            case "m":
                                ShowActiveSettings(PlayerControl.LocalPlayer.PlayerId);
                                break;
                            case "w":
                                ShowWinSetting();
                                break;
                            default:
                                ShowActiveSettings();
                                break;
                        }
                        break;

                    case "/dis":
                        canceled = true;
                        if (!GameStates.InGame) break;
                        subArgs = args.Length < 2 ? "" : args[1];
                        switch (subArgs)
                        {
                            case "crewmate":
                                GameManager.Instance.enabled = false;
                                GameManager.Instance.RpcEndGame(GameOverReason.CrewmateDisconnect, false);
                                break;

                            case "impostor":
                                GameManager.Instance.enabled = false;
                                GameManager.Instance.RpcEndGame(GameOverReason.ImpostorDisconnect, false);
                                break;

                            default:
                                __instance.AddChat(PlayerControl.LocalPlayer, "crewmate | impostor");
                                cancelVal = "/dis";
                                break;
                        }
                        break;

                    case "/h":
                    case "/help":
                        canceled = true;
                        var suba1 = 0;
                        byte playerh = 255;
                        subArgs = args.Length < 2 + suba1 ? "" : args[1 + suba1];
                        if (subArgs is "m" or "my")
                        {
                            suba1++;
                            playerh = PlayerControl.LocalPlayer.PlayerId;
                            subArgs = args.Length < 2 + suba1 ? "" : args[1 + suba1];
                        }
                        switch (subArgs)
                        {
                            case "r":
                            case "roles":
                                subArgs = args.Length < 3 + suba1 ? "" : args[2 + suba1];
                                GetRolesInfo(subArgs, playerh);
                                break;

                            case "a":
                            case "addons":
                                subArgs = args.Length < 3 + suba1 ? "" : args[2 + suba1];
                                switch (subArgs)
                                {
                                    case "lastimpostor":
                                    case "limp":
                                        SendMessage(GetRoleName(CustomRoles.LastImpostor) + GetString("LastImpostorInfoLong"), playerh);
                                        break;

                                    default:
                                        SendMessage($"{GetString("Command.h_args")}:\n lastimpostor(limp)", playerh);
                                        break;
                                }
                                break;

                            case "m":
                            case "modes":
                                subArgs = args.Length < 3 + suba1 ? "" : args[2 + suba1];
                                switch (subArgs)
                                {
                                    case "hideandseek":
                                    case "has":
                                        SendMessage(GetString("HideAndSeekInfo"), playerh);
                                        break;

                                    case "タスクバトル":
                                    case "taskbattle":
                                    case "tbm":
                                        SendMessage(GetString("TaskBattleInfo"), playerh);
                                        break;

                                    case "nogameend":
                                    case "nge":
                                        SendMessage(GetString("NoGameEndInfo"), playerh);
                                        break;

                                    case "syncbuttonmode":
                                    case "sbm":
                                        SendMessage(GetString("SyncButtonModeInfo"), playerh);
                                        break;

                                    case "インサイダーモード":
                                    case "insiderMode":
                                    case "im":
                                        SendMessage(GetString("InsiderModeInfo"));
                                        break;

                                    case "ランダムマップモード":
                                    case "randommapsmode":
                                    case "rmm":
                                        SendMessage(GetString("RandomMapsModeInfo"), playerh);
                                        break;
                                    case "サドンデスモード":
                                    case "SuddenDeath":
                                    case "Sd":
                                        SendMessage(GetString("SuddenDeathInfo"), playerh);
                                        break;
                                    default:
                                        SendMessage($"{GetString("Command.h_args")}:\n hideandseek(has), nogameend(nge), syncbuttonmode(sbm), randommapsmode(rmm), taskbattle(tbm), InsiderMode(im),SuddenDeath(sd)", playerh);
                                        break;
                                }
                                break;

                            case "n":
                            case "now":
                                ShowActiveSettingsHelp(playerh);
                                break;

                            default:
                                foreach (var pc in PlayerCatch.AllPlayerControls)
                                {
                                    ShowHelp(pc.PlayerId);
                                }
                                break;
                        }
                        break;
                    case "/hr":
                        canceled = true;
                        subArgs = args.Length < 2 ? "" : args[1];
                        GetRolesInfo(subArgs, byte.MaxValue);
                        break;

                    case "/m":
                    case "/myrole":
                        canceled = true;
                        if (GameStates.IsInGame)
                        {
                            var role = PlayerControl.LocalPlayer.GetCustomRole();
                            var roleClass = PlayerControl.LocalPlayer.GetRoleClass();
                            if (PlayerControl.LocalPlayer.Is(CustomRoles.Amnesia))
                                role = PlayerControl.LocalPlayer.Is(CustomRoleTypes.Crewmate) ? CustomRoles.Crewmate : CustomRoles.Impostor;
                            if (roleClass?.Jikaku() != CustomRoles.NotAssigned && roleClass != null) role = roleClass.Jikaku();

                            if (role is CustomRoles.Amnesiac)
                            {
                                if (roleClass is Amnesiac amnesiac && !amnesiac.omoidasita)
                                    role = Amnesiac.iamwolf ? CustomRoles.WolfBoy : CustomRoles.Sheriff;
                            }
                            var hRoleTextData = GetRoleColorCode(role);
                            string hRoleInfoTitleString = $"{GetString("RoleInfoTitle")}";
                            string hRoleInfoTitle = $"<{hRoleTextData}>{hRoleInfoTitleString}";
                            if (role is CustomRoles.Crewmate or CustomRoles.Impostor)//バーニラならこっちで
                            {
                                SendMessage($"<b><line-height=2.0pic><size=150%>{GetString(role.ToString()).Color(PlayerControl.LocalPlayer.GetRoleColor())}</b>\n<size=60%><line-height=1.8pic>{PlayerControl.LocalPlayer.GetRoleInfo(true)}", PlayerControl.LocalPlayer.PlayerId, hRoleInfoTitle);
                            }
                            else
                                SendMessage(role.GetRoleInfo()?.Description?.FullFormatHelp ?? $"<b><line-height=2.0pic><size=150%>{GetString(role.ToString()).Color(PlayerControl.LocalPlayer.GetRoleColor())}</b>\n<size=60%><line-height=1.8pic>{PlayerControl.LocalPlayer.GetRoleInfo(true)}", PlayerControl.LocalPlayer.PlayerId, hRoleInfoTitle);
                            GetAddonsHelp(PlayerControl.LocalPlayer);

                            subArgs = args.Length < 2 ? "" : args[1];
                            switch (subArgs)
                            {
                                case "a":
                                case "all":
                                case "allplayer":
                                case "ap":
                                    foreach (var player in PlayerCatch.AllPlayerControls.Where(p => p.PlayerId != PlayerControl.LocalPlayer.PlayerId))
                                    {
                                        role = player.GetCustomRole();
                                        roleClass = player.GetRoleClass();
                                        if (player.Is(CustomRoles.Amnesia)) role = player.Is(CustomRoleTypes.Crewmate) ? CustomRoles.Crewmate : CustomRoles.Impostor;
                                        if (roleClass?.Jikaku() != CustomRoles.NotAssigned && roleClass != null) role = roleClass.Jikaku();

                                        if (role is CustomRoles.Amnesiac)
                                        {
                                            if (roleClass is Amnesiac amnesiac && !amnesiac.omoidasita)
                                                role = Amnesiac.iamwolf ? CustomRoles.WolfBoy : CustomRoles.Sheriff;
                                        }

                                        var RoleTextData = GetRoleColorCode(role);
                                        string RoleInfoTitleString = $"{GetString("RoleInfoTitle")}";
                                        string RoleInfoTitle = $"<{RoleTextData}>{RoleInfoTitleString}";

                                        if (role is CustomRoles.Crewmate or CustomRoles.Impostor)
                                        {
                                            SendMessage("<b><line-height=2.0pic><size=150%>" + GetString(role.ToString()).Color(player.GetRoleColor()) + "\n</b><size=90%><line-height=1.8pic>" + player.GetRoleInfo(true), player.PlayerId, RoleInfoTitle);
                                        }
                                        else
                                        if (role.GetRoleInfo()?.Description is { } description)
                                        {

                                            SendMessage(description.FullFormatHelp, player.PlayerId, RoleInfoTitle);
                                        }
                                        // roleInfoがない役職
                                        else
                                        {
                                            SendMessage($"<b><line-height=2.0pic><size=150%>{GetString(role.ToString()).Color(player.GetRoleColor())}</b>\n<size=60%><line-height=1.8pic>{player.GetRoleInfo(true)}", player.PlayerId, RoleInfoTitle);
                                        }
                                        GetAddonsHelp(player);

                                        if (player.IsGhostRole())
                                            SendMessage(GetAddonsHelp(PlayerState.GetByPlayerId(player.PlayerId).GhostRole), player.PlayerId);
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                        break;

                    case "/impstorchat":
                    case "/impct":
                    case "/ic":
                        canceled = true;
                        if (GameStates.InGame && Options.ImpostorHideChat.GetBool() && PlayerControl.LocalPlayer.IsAlive() && (PlayerControl.LocalPlayer.GetCustomRole().IsImpostor() || PlayerControl.LocalPlayer.GetCustomRole() is CustomRoles.Egoist))
                        {
                            if ((PlayerControl.LocalPlayer.GetRoleClass() as Amnesiac)?.omoidasita == false) break;
                            var send = "";
                            foreach (var ag in args)
                            {
                                if (ag.StartsWith("/")) continue;
                                send += ag;
                            }

                            Logger.Info($"{PlayerControl.LocalPlayer.Data.GetLogPlayerName()} : {send}", "impostorsChat");
                            foreach (var imp in PlayerCatch.AllAlivePlayerControls)
                            {
                                if ((imp.GetRoleClass() as Amnesiac)?.omoidasita == false) continue;
                                if (imp && ((imp?.GetCustomRole().IsImpostor() ?? false) || imp?.GetCustomRole() is CustomRoles.Egoist) || !imp.IsAlive())
                                {
                                    var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
                                    writer.StartMessage(imp.GetClientId());
                                    writer.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.SetName)
                                    .Write(PlayerControl.LocalPlayer.Data.NetId)
                                    .Write($"<line-height=-18%>\n<#ff1919>☆{GetPlayerColor(PlayerControl.LocalPlayer)}☆</color></line-height>")
                                    .EndRpc();
                                    writer.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.SendChat)
                                    .Write(send.Mark(Palette.ImpostorRed))
                                    .EndRpc();
                                    writer.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.SetName)
                                    .Write(PlayerControl.LocalPlayer.Data.NetId)
                                    .Write(PlayerControl.LocalPlayer.Data.GetLogPlayerName())
                                    .EndRpc();
                                    writer.EndMessage();
                                    writer.SendMessage();
                                }
                            }
                        }
                        break;

                    case "/jackalchat":
                    case "/jacct":
                    case "/jc":
                        if (Assassin.NowUse) break;
                        canceled = true;
                        if (GameStates.InGame && Options.ImpostorHideChat.GetBool() && PlayerControl.LocalPlayer.IsAlive() && PlayerControl.LocalPlayer.GetCustomRole() is CustomRoles.Jackal or CustomRoles.Jackaldoll or CustomRoles.JackalMafia or CustomRoles.JackalAlien)
                        {
                            var send = "";
                            foreach (var ag in args)
                            {
                                if (ag.StartsWith("/")) continue;
                                send += ag;
                            }

                            Logger.Info($"{PlayerControl.LocalPlayer.Data.GetLogPlayerName()} : {send}", "jackalChat");
                            foreach (var jac in PlayerCatch.AllAlivePlayerControls)
                            {
                                if (jac && ((jac?.GetCustomRole() is CustomRoles.Jackal or CustomRoles.Jackaldoll or CustomRoles.JackalMafia or CustomRoles.JackalAlien) || !jac.IsAlive()))
                                {
                                    var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
                                    writer.StartMessage(jac.GetClientId());
                                    writer.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.SetName)
                                    .Write(PlayerControl.LocalPlayer.Data.NetId)
                                    .Write($"<line-height=-18%>\n<#00b4eb>Φ{GetPlayerColor(PlayerControl.LocalPlayer)}Φ</color></line-height>")
                                    .EndRpc();
                                    writer.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.SendChat)
                                    .Write(send.Mark(ModColors.JackalColor))
                                    .EndRpc();
                                    writer.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.SetName)
                                    .Write(PlayerControl.LocalPlayer.Data.NetId)
                                    .Write(PlayerControl.LocalPlayer.Data.GetLogPlayerName())
                                    .EndRpc();
                                    writer.EndMessage();
                                    writer.SendMessage();
                                }
                            }
                        }
                        break;

                    case "/loverschat":
                    case "/loverchat":
                    case "/lc":
                        if (Assassin.NowUse) break;
                        canceled = true;
                        if (GameStates.InGame && Options.LoversHideChat.GetBool() && PlayerControl.LocalPlayer.IsAlive() && PlayerControl.LocalPlayer.IsRiaju())
                        {
                            var l = PlayerControl.LocalPlayer.GetRiaju();

                            if (l is CustomRoles.NotAssigned or CustomRoles.OneLove || !l.IsRiaju()) break;

                            var send = "";
                            foreach (var ag in args)
                            {
                                if (ag.StartsWith("/")) continue;
                                send += ag;
                            }

                            Logger.Info($"{PlayerControl.LocalPlayer.Data.GetLogPlayerName()} : {send}", "loversChat");
                            foreach (var lover in AllAlivePlayerControls)
                            {
                                if (lover && (lover.GetRiaju() == l || !lover.IsAlive()))
                                {
                                    var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
                                    writer.StartMessage(lover.GetClientId());
                                    writer.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.SetName)
                                    .Write(PlayerControl.LocalPlayer.Data.NetId)
                                    .Write(ColorString(GetRoleColor(l), $"<line-height=-18%>\n♥{GetPlayerColor(PlayerControl.LocalPlayer)}♥</line-height>"))
                                    .EndRpc();
                                    writer.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.SendChat)
                                    .Write(send.Mark(GetRoleColor(l)))
                                    .EndRpc();
                                    writer.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.SetName)
                                    .Write(PlayerControl.LocalPlayer.Data.NetId)
                                    .Write(PlayerControl.LocalPlayer.Data.GetLogPlayerName())
                                    .EndRpc();
                                    writer.EndMessage();
                                    writer.SendMessage();
                                }
                            }
                        }
                        break;
                    case "/Twinschat":
                    case "/twinschet":
                    case "/tc":
                        if (Assassin.NowUse) break;
                        if (GameStates.InGame && Options.TwinsHideChat.GetBool() && PlayerControl.LocalPlayer.IsAlive() && Twins.TwinsList.TryGetValue(PlayerControl.LocalPlayer.PlayerId, out var twinsid))
                        {
                            if (GameStates.Tuihou)
                            {
                                canceled = true;
                                break;
                            }

                            var send = "";
                            foreach (var ag in args)
                            {
                                if (ag.StartsWith("/")) continue;
                                send += ag;
                            }

                            Logger.Info($"{PlayerControl.LocalPlayer.Data.GetLogPlayerName()} : {send}", "TwinsChat");
                            foreach (var twins in AllPlayerControls)
                            {
                                if (twins && (twins.PlayerId == twinsid || twins.PlayerId == PlayerControl.LocalPlayer.PlayerId || !twins.IsAlive()))
                                {
                                    if (AmongUsClient.Instance.AmHost)
                                    {
                                        var clientid = twins.GetClientId();
                                        if (clientid == -1) continue;
                                        var writer = CustomRpcSender.Create("TwinsChat", SendOption.None);
                                        writer.StartMessage(clientid);
                                        writer.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.SetName)
                                        .Write(PlayerControl.LocalPlayer.Data.NetId)
                                        .Write(ColorString(GetRoleColor(CustomRoles.Twins), $"<align=\"left\"><line-height=-18%>\n∈{GetPlayerColor(PlayerControl.LocalPlayer)}∋</line-height>"))
                                        .EndRpc();
                                        writer.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.SendChat)
                                        .Write($"<align=\"left\">{send.Mark(GetRoleColor(CustomRoles.Twins))}")
                                        .EndRpc();
                                        writer.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.SetName)
                                        .Write(PlayerControl.LocalPlayer.Data.NetId)
                                        .Write(PlayerControl.LocalPlayer.Data.GetLogPlayerName())
                                        .EndRpc();
                                        writer.EndMessage();
                                        writer.SendMessage();
                                    }
                                }
                            }
                        }
                        canceled = true;
                        break;
                    case "/Connectingchat":
                    case "/cc":
                        if (Assassin.NowUse) break;
                        if (GameStates.InGame && Options.ConnectingHideChat.GetBool() && PlayerControl.LocalPlayer.IsAlive() && PlayerControl.LocalPlayer.Is(CustomRoles.Connecting))
                        {
                            if (GameStates.Tuihou)
                            {
                                canceled = true;
                                break;
                            }

                            var send = "";
                            foreach (var ag in args)
                            {
                                if (ag.StartsWith("/")) continue;
                                send += ag;
                            }

                            Logger.Info($"{PlayerControl.LocalPlayer.Data.GetLogPlayerName()} : {send}", "Connectingchat");
                            foreach (var connect in AllPlayerControls)
                            {
                                if (connect && (connect.Is(CustomRoles.Connecting) || !connect.IsAlive()))
                                {
                                    if (AmongUsClient.Instance.AmHost)
                                    {
                                        var clientid = connect.GetClientId();
                                        if (clientid == -1) continue;
                                        var writer = CustomRpcSender.Create("Connectingchat", SendOption.None);
                                        writer.StartMessage(clientid);
                                        writer.StartRpc(connect.NetId, (byte)RpcCalls.SetName)
                                        .Write(connect.Data.NetId)
                                        .Write(ColorString(GetRoleColor(CustomRoles.Connecting), $"<align=\"left\"><line-height=-18%>\nΨ{GetPlayerColor(PlayerControl.LocalPlayer)}Ψ</line-height>"))
                                        .EndRpc();
                                        writer.StartRpc(connect.NetId, (byte)RpcCalls.SendChat)
                                        .Write($"<align=\"left\">{send.Mark(GetRoleColor(CustomRoles.Connecting))}")
                                        .EndRpc();
                                        writer.StartRpc(connect.NetId, (byte)RpcCalls.SetName)
                                        .Write(connect.Data.NetId)
                                        .Write(connect.Data.GetLogPlayerName())
                                        .EndRpc();
                                        writer.EndMessage();
                                        writer.SendMessage();
                                    }
                                }
                            }
                        }
                        canceled = true;
                        break;

                    case "/t":
                    case "/template":
                        canceled = true;
                        if (args.Length > 1) TemplateManager.SendTemplate(args[1]);
                        else SendMessage($"{GetString("ForExample")}:\n{args[0]} test", PlayerControl.LocalPlayer.PlayerId);
                        break;
                    case "/mw":
                    case "/messagewait":
                        canceled = true;
                        if (args.Length > 1 && float.TryParse(args[1], out float sec))
                        {
                            Main.MessageWait.Value = sec;
                            SendMessage(string.Format(GetString("Message.SetToSeconds"), sec), 0);
                        }
                        else SendMessage($"{GetString("Message.MessageWaitHelp")}\n{GetString("ForExample")}:\n{args[0]} 3", 0);
                        break;

                    case "/say":
                        canceled = true;
                        if (args.Length > 1)
                            SendMessage(args.Skip(1).Join(delimiter: " "), title: $"<#ff0000>{GetString("MessageFromTheHost")}</color>");
                        break;

                    case "/settask":
                    case "/stt":
                        canceled = true;
                        var chc = "";
                        if (!GameStates.IsLobby) break;
                        if (args.Length > 1 && int.TryParse(args[1], out var cot))
                            if (ch(cot))
                            {
                                Main.NormalOptions.TryCast<NormalGameOptionsV09>().SetInt(Int32OptionNames.NumCommonTasks, cot);
                                chc += $"通常タスクを{cot}にしました!\n";
                            }
                        if (args.Length > 2 && int.TryParse(args[2], out var lot))
                            if (ch(lot))
                            {
                                Main.NormalOptions.TryCast<NormalGameOptionsV09>().SetInt(Int32OptionNames.NumLongTasks, lot);
                                chc += $"ロングタスクを{lot}にしました!\n";
                            }
                        if (args.Length > 3 && int.TryParse(args[3], out var sht))
                            if (ch(sht))
                            {
                                Main.NormalOptions.TryCast<NormalGameOptionsV09>().SetInt(Int32OptionNames.NumShortTasks, sht);
                                chc += $"ショートタスクを{sht}にしました!\n";
                            }
                        if (chc == "")
                        {
                            chc = "/settask(/stt) Common Long Short";
                            SendMessage(chc, PlayerControl.LocalPlayer.PlayerId);
                            break;
                        }
                        GameOptionsSender.RpcSendOptions();
                        SendMessage($"<size=70%>{chc}</size>");

                        static bool ch(int n)
                        {
                            if (n > 99) return false;
                            if (0 > n) return false;
                            return true;
                        }
                        break;
                    case "/kc":
                        canceled = true;
                        if (!GameStates.IsLobby) break;
                        if (args.Length > 1 && float.TryParse(args[1], out var fl))
                        {
                            if (fl <= 0) fl = 0.00000000000000001f;
                            Main.NormalOptions.TryCast<NormalGameOptionsV09>().SetFloat(FloatOptionNames.KillCooldown, fl);
                        }
                        GameOptionsSender.RpcSendOptions();
                        break;
                    case "/exile":
                        canceled = true;
                        if (GameStates.IsLobby) break;
                        if (args.Length < 2 || !int.TryParse(args[1], out int id)) break;
                        GetPlayerById(id)?.RpcExileV2();
                        break;

                    case "/kill":
                        canceled = true;
                        if (GameStates.IsLobby) break;
                        if (args.Length < 2 || !int.TryParse(args[1], out int id2)) break;
                        GetPlayerById(id2)?.RpcMurderPlayer(GetPlayerById(id2), true);
                        break;

                    case "/allplayertp":
                    case "/apt":
                        canceled = true;
                        if (!GameStates.IsLobby) break;
                        foreach (var tp in PlayerCatch.AllPlayerControls)
                        {
                            Vector2 position = new(0.0f, 0.0f);
                            tp.RpcSnapToForced(position);
                        }
                        break;

                    case "/revive":
                    case "/rev":
                        if (!DebugModeManager.EnableDebugMode.GetBool()) break;
                        //まぁ・・・期待してるような動作はしない。
                        canceled = true;
                        var revplayer = PlayerControl.LocalPlayer;
                        if (args.Length < 2 || !int.TryParse(args[1], out int revid)) { }
                        else
                        {
                            revplayer = GetPlayerById(revid);
                            if (revplayer == null) revplayer = PlayerControl.LocalPlayer;
                        }
                        revplayer.Revive();
                        revplayer.RpcSetRole(RoleTypes.Crewmate, true);
                        revplayer.Data.IsDead = false;
                        if (GameStates.InGame)
                        {
                            var state = PlayerState.GetByPlayerId(revplayer.PlayerId);
                            state.IsDead = false;
                            state.DeathReason = CustomDeathReason.etc;

                            revplayer.RpcSetRole(state.MainRole.GetRoleTypes(), true);
                        }
                        RPC.RpcSyncAllNetworkedPlayer();
                        break;

                    case "/id":
                        canceled = true;
                        var sendchatid = "";
                        foreach (var pc in PlayerCatch.AllPlayerControls)
                        {
                            sendchatid = $"{sendchatid}{pc.PlayerId}:{pc.name}\n";
                        }
                        __instance.AddChat(PlayerControl.LocalPlayer, sendchatid);
                        break;

                    case "/forceend":
                    case "/fe":
                        canceled = true;
                        if (GameStates.InGame)
                            SendMessage(GetString("ForceEndText"));
                        GameManager.Instance.enabled = false;
                        CustomWinnerHolder.WinnerTeam = CustomWinner.Draw;
                        GameManager.Instance.RpcEndGame(GameOverReason.ImpostorDisconnect, false);
                        break;

                    case "/w":
                        canceled = true;
                        ShowLastWins();
                        break;

                    case "/timer":
                    case "/tr":
                        canceled = true;
                        if (!GameStates.IsInGame)
                            ShowTimer();
                        break;
                    case "/kf":
                        canceled = true;
                        if (GameStates.InGame)
                            AllPlayerKillFlash();
                        break;
                    case "/MeeginInfo":
                    case "/mi":
                    case "/day":
                        canceled = true;
                        if (GameStates.InGame)
                        {
                            SendMessage(MeetingHudPatch.Send, title: MeetingHudPatch.Title);
                        }
                        break;

                    case "/yaminabe":
                    case "/yami":
                    case "/ym":
                        if (GameStates.IsLobby && !GameStates.IsCountDown)
                        {
                            canceled = true;
                            foreach (var option in Options.CustomRoleSpawnChances.Where(option => option.Key is not CustomRoles.NotAssigned and not CustomRoles.Assassin && Event.CheckRole(option.Key) && (!Event.IsE(option.Key) || Event.Special)))
                            {
                                var r = option.Key;
                                if (r.IsImpostor() || r.IsCrewmate() || r.IsMadmate() || r.IsNeutral()) option.Value.SetValue(10);
                            }
                        }
                        break;
                    case "/superyaminabe":
                    case "/superyami":
                    case "/sym":
                        if (GameStates.IsLobby && !GameStates.IsCountDown)
                        {
                            canceled = true;
                            foreach (var option in Options.CustomRoleSpawnChances.Where(option => option.Key is not CustomRoles.NotAssigned and not CustomRoles.Assassin && Event.CheckRole(option.Key) && (!Event.IsE(option.Key) || Event.Special)))
                            {
                                option.Value.SetValue(10);
                            }
                        }
                        break;
                    case "/rolereset":
                    case "/resetrole":
                        if (GameStates.IsLobby && !GameStates.IsCountDown)
                        {
                            canceled = true;
                            foreach (var option in Options.CustomRoleSpawnChances.Where(option => option.Key is not CustomRoles.NotAssigned))
                            {
                                option.Value.SetValue(0);
                            }
                        }
                        break;
                    case "/addwhite":
                    case "/aw":
                        canceled = true;
                        if (args.Length < 2)
                        {
                            Logger.seeingame("ロビーにいる全てのプレイヤーをホワイトリストに登録するぞ！");
                            //指定がない場合
                            foreach (var pc in AllPlayerControls)
                            {
                                if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
                                BanManager.AddWhitePlayer(pc.GetClient());
                            }
                        }
                        else
                        {
                            var targetname = args[1];
                            var added = false;
                            //指定がない場合
                            foreach (var pc in AllPlayerControls.Where(pc => (pc?.Data?.GetLogPlayerName() ?? "('ω')").RemoveDeltext(" ") == targetname))
                            {
                                BanManager.AddWhitePlayer(pc.GetClient());
                                added = true;
                            }
                            if (!added)
                                SendMessage($"{targetname}って名前のプレイヤーがいないよっ...", 0);
                        }
                        break;

                    case "/st":
                    case "/setteam":

                        canceled = true;

                        //モードがタスバトじゃない時はメッセージ表示
                        if (Options.CurrentGameMode != CustomGameMode.TaskBattle)
                        {
                            __instance.AddChat(PlayerControl.LocalPlayer, "選択されているモードが<color=cyan>タスクバトル</color>のみ実行可能です。\nロビーにある設定から変えてみてね><");
                            break;
                        }

                        if (GameStates.IsLobby && !GameStates.IsCountDown)
                        {
                            if (args.Length < 3)//引数がない場合
                            {

                                if (args.Length > 1 && args[1] == "None")
                                {
                                    TaskBattle.SelectedTeams.Clear();
                                    SendMessage("チームをリセットしました。", PlayerControl.LocalPlayer.PlayerId);
                                    break;
                                }

                                StringBuilder tbSb = new();
                                foreach (var (tbTeamId, tbPlayers) in TaskBattle.SelectedTeams)
                                {
                                    tbSb.Append($"・チーム{tbTeamId}\n");
                                    foreach (var tbId in tbPlayers)
                                        tbSb.Append(GetPlayerInfoById(tbId).PlayerName).Append('\n');
                                    tbSb.Append('\n');
                                }
                                SendMessage($"現在のチーム:\n{tbSb}\n\n使用方法: 設定: /st プレイヤーid チーム番号\nリセット: /st None\nプレイヤーid確認方法: /id", PlayerControl.LocalPlayer.PlayerId);
                                break;
                            }

                            if (byte.TryParse(args[1], out var stPlayerId) && byte.TryParse(args[2], out var stTeamId))
                            {
                                List<byte> stData;
                                TaskBattle.SelectedTeams.Values.Do(players => players.Remove(stPlayerId));
                                TaskBattle.SelectedTeams.DoIf(teamData => teamData.Value.Count < 1, teamData => TaskBattle.SelectedTeams.Remove(teamData.Key));
                                stData = TaskBattle.SelectedTeams.TryGetValue(stTeamId, out stData) ? stData : new();
                                stData.Add(stPlayerId);
                                TaskBattle.SelectedTeams[stTeamId] = stData;
                                SendMessage($"{GetPlayerById(stPlayerId)?.name ?? stPlayerId.ToString()}をチーム{stTeamId}に設定しました！", PlayerControl.LocalPlayer.PlayerId);
                                break;
                            }
                            SendMessage("引数の値が正しくありません。", PlayerControl.LocalPlayer.PlayerId);
                        }
                        break;

                    case "/cr":
                        if (DebugModeManager.EnableTOHkDebugMode.GetBool())
                        {
                            canceled = true;
                            subArgs = args.Length < 2 ? "" : args[1];
                            var pc = PlayerControl.LocalPlayer;
                            if (args.Length > 2 && int.TryParse(args[2], out var taisho))
                            {
                                pc = GetPlayerById(taisho);
                                if (pc == null) pc = PlayerControl.LocalPlayer;
                            }
                            if (GetRoleByInputName(subArgs, out var role, true))
                            {
                                if (GameStates.InGame)
                                {
                                    NameColorManager.RemoveAll(pc.PlayerId);
                                    pc.RpcSetCustomRole(role, true, true);
                                }
                                else
                                {
                                    if (role.IsAddOn() || role.IsGhostRole() || role.IsRiaju()) break;
                                    Main.HostRole = role;
                                    var r = ColorString(GetRoleColor(role), GetString($"{role}"));
                                    SendMessage($"ホストの役職を{r}にするよっ!!");
                                }
                            }
                            else
                            {
                                if (Main.HostRole == CustomRoles.NotAssigned) SendMessage("役職変更に失敗したよ(´・ω・｀)", PlayerControl.LocalPlayer.PlayerId);
                                else
                                {
                                    Main.HostRole = CustomRoles.NotAssigned;
                                    SendMessage("役職固定をリセットしたよっ!", PlayerControl.LocalPlayer.PlayerId);
                                }
                            }
                        }
                        break;
                    case "/wi":
                        if (DebugModeManager.EnableTOHkDebugMode.GetBool())
                        {
                            canceled = true;
                            subArgs = args.Length < 2 ? "" : args[1];
                            if (GetRoleByInputName(subArgs, out var role, true))
                            {
                                if (role.GetRoleInfo()?.Description?.WikiText is not null and not "")
                                {
                                    ClipboardHelper.PutClipboardString(role.GetRoleInfo().Description.WikiText);
                                    SendMessage($"{role}のwikiコピーしたよっ", PlayerControl.LocalPlayer.PlayerId);
                                    GetRolesInfo(subArgs, PlayerControl.LocalPlayer.PlayerId);
                                }
                                else
                                {
                                    string str = GetWikitext(role);
                                    ClipboardHelper.PutClipboardString(str);
                                    SendMessage($"{role}のwikiコピーしたよっ", PlayerControl.LocalPlayer.PlayerId);
                                    GetRolesInfo(subArgs, PlayerControl.LocalPlayer.PlayerId);
                                }
                            }
                        }
                        break;
                    case "/wiop":
                        if (DebugModeManager.EnableTOHkDebugMode.GetBool())
                        {
                            canceled = true;
                            subArgs = args.Length < 2 ? "" : args[1];
                            if (GetRoleByInputName(subArgs, out var role, true))
                            {
                                if (role.GetRoleInfo()?.Description?.WikiOpt is not null and not "")
                                {
                                    ClipboardHelper.PutClipboardString(role.GetRoleInfo().Description.WikiOpt);
                                    SendMessage($"{role}の設定コピーしたよっ", PlayerControl.LocalPlayer.PlayerId);
                                    GetRolesInfo(subArgs, PlayerControl.LocalPlayer.PlayerId);
                                }
                                else
                                {
                                    var builder = new StringBuilder(256);
                                    var sb = new StringBuilder();
                                    if (Options.CustomRoleSpawnChances.TryGetValue(role, out var op))
                                        RoleDescription.wikiOption(op, ref sb);

                                    if (sb.ToString().RemoveHtmlTags() is not null and not "")
                                    {
                                        builder.Append($"\n## 設定\n").Append("|設定名|(設定値 / デフォルト値)|説明|\n").Append("|-----|----------------------|----|\n");
                                        builder.Append($"{sb.ToString().RemoveHtmlTags()}\n");
                                    }

                                    ClipboardHelper.PutClipboardString(builder.ToString());
                                    SendMessage($"{role}の設定コピーしたよっ", PlayerControl.LocalPlayer.PlayerId);
                                    GetRolesInfo(subArgs, PlayerControl.LocalPlayer.PlayerId);
                                }
                            }
                        }
                        break;

                    case "/dgm":
                        if (DebugModeManager.EnableTOHkDebugMode.GetBool())
                        {
                            canceled = true;
                            if (!GameStates.InGame)
                            {
                                SendMessage($"ロビーでは変更出来ないよっ");
                                break;
                            }
                            Main.DontGameSet = !Main.DontGameSet;
                            SendMessage($"ゲームを終了しない設定を{Main.DontGameSet}にしたよっ!!");
                        }
                        break;

                    case "/debug":
                        canceled = true;
                        if (DebugModeManager.EnableTOHkDebugMode.GetBool())
                        {
                            subArgs = args.Length < 2 ? "" : args[1];
                            switch (subArgs)
                            {
                                case "noimp":
                                    Main.NormalOptions.NumImpostors = 0;
                                    break;
                                case "setimp":
                                    int d = 0;
                                    subArgs = subArgs.Length < 2 ? "0" : args[2];
                                    if (int.TryParse(subArgs, out d))
                                    {
                                        Logger.Info($"変換に成功-{d}", "setimp");
                                    }
                                    Main.NormalOptions.NumImpostors = d;
                                    break;
                                case "abo":
                                    if (Main.DebugAntiblackout)
                                        Main.DebugAntiblackout = false;
                                    else
                                        Main.DebugAntiblackout = true;
                                    Logger.seeingame($"AntiBlockOut:{Main.DebugAntiblackout}");
                                    break;
                                case "winset":
                                    byte wid;
                                    subArgs = subArgs.Length < 2 ? "0" : args[2];
                                    if (byte.TryParse(subArgs, out wid))
                                    {
                                        Logger.Info($"変換に成功-{wid}", "winset");
                                    }
                                    CustomWinnerHolder.WinnerIds.Add(wid);
                                    break;
                                case "win":
                                    GameManager.Instance.LogicFlow.CheckEndCriteria();
                                    GameManager.Instance.RpcEndGame(GameOverReason.ImpostorsByKill, false);
                                    break;
                                case "nc":
                                    Main.nickName = "<size=0>";
                                    break;
                                case "getrole":
                                    StringBuilder sb = new();
                                    foreach (var pc in PlayerCatch.AllPlayerControls)
                                        sb.Append(pc.PlayerId + ": " + pc.name + " => " + pc.GetCustomRole() + "\n");
                                    SendMessage(sb.ToString(), PlayerControl.LocalPlayer.PlayerId);
                                    break;
                                case "rr":
                                    var name2 = string.Join(" ", args.Skip(2)).Trim();
                                    if (string.IsNullOrEmpty(name2))
                                    {
                                        Main.nickName = "";
                                        break;
                                    }
                                    if (name2.StartsWith(" ")) break;
                                    name2 = Regex.Replace(name2, @"size=(\d+)", "<size=$1>");
                                    name2 = Regex.Replace(name2, @"pos=(\d+)", "<pos=$1em>");
                                    name2 = Regex.Replace(name2, @"space=(\d+)", "<space=$1em>");
                                    name2 = Regex.Replace(name2, @"line-height=(\d+)", "<line-height=$1%>");
                                    name2 = Regex.Replace(name2, @"space=(\d+)", "<space=$1em>");
                                    name2 = Regex.Replace(name2, @"color=(\w+)", "<color=$1>");

                                    name2 = name2.Replace("\\n", "\n").Replace("しかくうう", "■").Replace("/l-h", "</line-height>");
                                    Main.nickName = name2; //これは何かって..? 気にしちゃﾏｹだ！
                                    break;
                                case "kill":
                                    byte pcid;
                                    byte seerid;
                                    if (byte.TryParse(args[2], out pcid) && byte.TryParse(args[3], out seerid))
                                    {
                                        var pc = GetPlayerById(pcid);
                                        var seer = GetPlayerById(seerid);
                                        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(pc.NetId, (byte)RpcCalls.MurderPlayer, SendOption.Reliable, seer.GetClientId());
                                        writer.WriteNetObject(pc);
                                        writer.Write((int)ExtendedPlayerControl.SuccessFlags);
                                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                                    }
                                    break;
                                case "resetcam":
                                    if (args.Length < 2 || !int.TryParse(args[2], out int id3)) break;
                                    GetPlayerById(id3)?.ResetPlayerCam(1f);
                                    break;
                                case "resetdoorE":
                                    AirShipElectricalDoors.Initialize();
                                    break;
                                case "GetVoice":
                                    foreach (var r in Yomiage.GetvoiceListAsync().Result)
                                        Logger.Info(r.Value, "VoiceList");
                                    break;
                                case "rev":
                                    if (!byte.TryParse(args[2], out byte idr)) break;
                                    var revpc = GetPlayerById(idr);
                                    revpc.Data.IsDead = false;
                                    PlayerControl.LocalPlayer.SetDirtyBit(0b_1u << idr);
                                    AmongUsClient.Instance.SendAllStreamedObjects();
                                    break;
                            }
                            break;
                        }
                        break;

                    default:
                        if (args[0].StartsWith("/"))
                        {
                            canceled = true;
                            break;
                        }
                        break;
                }
            }
            canceled |= AntiBlackout.IsCached && GameStates.InGame;
            if (canceled)
            {
                Logger.Info("Command Canceled", "ChatCommand");
                __instance.freeChatField.textArea.Clear();
                __instance.freeChatField.textArea.SetText(cancelVal);
            }
            if (AmongUsClient.Instance.AmHost && GameStates.IsLobby && !canceled)
            {
                SendMessage(text, title: Main.nickName == "" ? DataManager.player.Customization.Name : Main.nickName, rob: true);
                __instance.freeChatField.textArea.Clear();
                return false;
            }
            return !canceled;
        }

        public static void OnReceiveChat(PlayerControl player, string text, out bool canceled)
        {
            if (player != null)
            {
                var tag = !player.Data.IsDead ? "SendChatAlive" : "SendChatDead";
            }

            canceled = false;

            if (!AmongUsClient.Instance.AmHost) return;
            string[] args = text.Split(' ');
            string subArgs = "";
            if (text.RemoveHtmlTags() != text) return;//システムメッセージなら処理しない
            if (player.PlayerId != 0)
            {
                ChatManager.SendMessage(player, text);
            }
            if (GuessManager.GuesserMsg(player, text)) { canceled = true; return; }

            switch (args[0])
            {
                case "/l":
                case "/lastresult":
                    canceled = true;
                    ShowLastResult(player.PlayerId);
                    break;

                case "/kl":
                case "/killlog":
                    canceled = true;
                    ShowKillLog(player.PlayerId);
                    break;

                case "/n":
                case "/now":
                    canceled = true;
                    subArgs = args.Length < 2 ? "" : args[1];
                    switch (subArgs)
                    {
                        case "r":
                        case "roles":
                            ShowActiveRoles(player.PlayerId);
                            break;
                        case "set":
                        case "s":
                        case "setting":
                            ShowSetting(player.PlayerId);
                            break;
                        case "w":
                            ShowWinSetting(player.PlayerId);
                            break;
                        default:
                            ShowActiveSettings(player.PlayerId);
                            break;
                    }
                    break;

                case "/h":
                case "/help":
                    canceled = true;
                    subArgs = args.Length < 2 ? "" : args[1];
                    switch (subArgs)
                    {
                        case "n":
                        case "now":
                            ShowActiveSettingsHelp(player.PlayerId);
                            break;

                        case "r":
                        case "roles":
                            subArgs = args.Length < 3 ? "" : args[2];
                            GetRolesInfo(subArgs, player.PlayerId);
                            break;
                        default:
                            ShowHelp(player.PlayerId);
                            break;
                    }
                    break;
                case "/hr":
                    canceled = true;
                    subArgs = args.Length < 2 ? "" : args[1];
                    GetRolesInfo(subArgs, player.PlayerId);
                    break;

                case "/m":
                case "/myrole":
                    if (GameStates.IsInGame)
                    {
                        canceled = true;
                        var role = player.GetCustomRole();
                        var roleclass = player.GetRoleClass();
                        if (player.Is(CustomRoles.Amnesia)) role = player.Is(CustomRoleTypes.Crewmate) ? CustomRoles.Crewmate : CustomRoles.Impostor;
                        if (roleclass?.Jikaku() != CustomRoles.NotAssigned && roleclass != null) role = roleclass.Jikaku();

                        if (role is CustomRoles.Amnesiac)
                        {
                            if (roleclass is Amnesiac amnesiac && !amnesiac.omoidasita)
                                role = Amnesiac.iamwolf ? CustomRoles.WolfBoy : CustomRoles.Sheriff;
                        }

                        var RoleTextData = GetRoleColorCode(role);
                        string RoleInfoTitleString = $"{GetString("RoleInfoTitle")}";
                        string RoleInfoTitle = $"<{RoleTextData}>{RoleInfoTitleString}";

                        if (role is CustomRoles.Crewmate or CustomRoles.Impostor)
                        {
                            SendMessage($"<b><line-height=2.0pic><size=150%>{GetString(role.ToString()).Color(player.GetRoleColor())}</b>\n<size=60%><line-height=1.8pic>{player.GetRoleInfo(true)}" + player.GetRoleInfo(true), player.PlayerId, RoleInfoTitle);
                        }
                        else
                        if (role.GetRoleInfo()?.Description is { } description)
                        {

                            SendMessage(description.FullFormatHelp, player.PlayerId, RoleInfoTitle);
                        }
                        // roleInfoがない役職
                        else
                        {
                            SendMessage($"<b><line-height=2.0pic><size=150%>{GetString(role.ToString()).Color(player.GetRoleColor())}</b>\n<size=60%><line-height=1.8pic>{player.GetRoleInfo(true)}", player.PlayerId, RoleInfoTitle);
                        }
                        GetAddonsHelp(player);
                    }
                    break;

                case "/t":
                case "/template":
                    canceled = true;
                    if (args.Length > 1) TemplateManager.SendTemplate(args[1], player.PlayerId);
                    else SendMessage($"{GetString("ForExample")}:\n{args[0]} test", player.PlayerId);
                    break;

                case "/timer":
                case "/tr":
                    canceled = true;
                    if (!GameStates.IsInGame)
                        ShowTimer(player.PlayerId);
                    break;


                case "/tp":
                    if (!GameStates.IsLobby || !Options.sotodererukomando.GetBool() || args.Length < 1) break;
                    canceled = true;
                    subArgs = args[1];
                    switch (subArgs)
                    {
                        case "o":
                            Vector2 position = new(3.0f, 0.0f);
                            player.RpcSnapToForced(position);
                            break;
                        case "i":
                            Vector2 position2 = new(0.0f, 0.0f);
                            player.RpcSnapToForced(position2);
                            break;

                    }
                    break;

                case "/kf":
                    canceled = true;
                    if (GameStates.InGame)
                        player.KillFlash(kiai: true);
                    break;

                case "/MeeginInfo":
                case "/mi":
                    canceled = true;
                    if (GameStates.InGame)
                    {
                        SendMessage(MeetingHudPatch.Send, player.PlayerId, title: MeetingHudPatch.Title);
                    }
                    break;

                case "/voice":
                case "/vo":
                    if (!Yomiage.ChatCommand(args, player.PlayerId))
                        SendMessage("使用方法:\n/vo 音質(id) 音量 速度 音程\n\n音質の一覧表示:\n /vo get\n /vo g", player.PlayerId);
                    break;
                case "/impstorchat":
                case "/impct":
                case "/ic":
                    if (GameStates.InGame && Options.ImpostorHideChat.GetBool() && player.IsAlive() && (player.GetCustomRole().IsImpostor() || player.GetCustomRole() is CustomRoles.Egoist))
                    {
                        if ((player.GetRoleClass() as Amnesiac)?.omoidasita == false)
                        {
                            canceled = true;
                            break;
                        }
                        string send = "";
                        if (GetHideSendText(ref canceled, ref send) is false) return;

                        Logger.Info($"{player.Data.GetLogPlayerName()} : {send}", "ImpostorChat");
                        foreach (var imp in AllPlayerControls)
                        {
                            if ((imp.GetRoleClass() as Amnesiac)?.omoidasita == false && imp.IsAlive()) continue;
                            if (imp && ((imp.GetCustomRole().IsImpostor() || imp.GetCustomRole() is CustomRoles.Egoist) || (!imp.IsAlive() && PlayerControl.LocalPlayer.PlayerId == imp?.PlayerId)) && imp.PlayerId != player.PlayerId)
                            {
                                if (AmongUsClient.Instance.AmHost)
                                {
                                    var clientid = imp.GetClientId();
                                    if (clientid == -1) continue;
                                    string title = $"<line-height=-18%>\n<#ff1919>☆{GetPlayerColor(player)}☆</line-height>";
                                    string sendtext = send.Mark(Palette.ImpostorRed);
                                    SendMessage(sendtext, imp.PlayerId, title);
                                }
                            }
                        }
                        player.RpcProtectedMurderPlayer();
                    }
                    canceled = true;
                    break;

                case "/jackalchat":
                case "/jacct":
                case "/jc":
                    if (Assassin.NowUse) break;
                    if (GameStates.InGame && Options.JackalHideChat.GetBool() && player.IsAlive() && player.GetCustomRole() is CustomRoles.Jackal or CustomRoles.Jackaldoll or CustomRoles.JackalMafia or CustomRoles.JackalAlien)
                    {
                        string send = "";
                        if (GetHideSendText(ref canceled, ref send) is false) return;

                        Logger.Info($"{player.Data.GetLogPlayerName()} : {send}", "JackalChat");
                        foreach (var jac in AllPlayerControls)
                        {
                            if (jac && ((jac.GetCustomRole() is CustomRoles.Jackal or CustomRoles.Jackaldoll or CustomRoles.JackalMafia or CustomRoles.JackalAlien) || (PlayerControl.LocalPlayer.PlayerId == jac?.PlayerId && !jac.IsAlive())) && jac.PlayerId != player.PlayerId)
                            {
                                if (AmongUsClient.Instance.AmHost)
                                {
                                    var clientid = jac.GetClientId();
                                    if (clientid == -1) continue;
                                    string title = $"<line-height=-18%>\n<#00b4eb>Φ{GetPlayerColor(player)}Φ</line-height>";
                                    string sendtext = send.Mark(ModColors.JackalColor);
                                    SendMessage(sendtext, jac.PlayerId, title);
                                }
                            }
                        }
                        player.RpcProtectedMurderPlayer();
                    }
                    canceled = true;
                    break;
                case "/loverschat":
                case "/loverchat":
                case "/lc":
                    if (Assassin.NowUse) break;
                    if (GameStates.InGame && Options.LoversHideChat.GetBool() && player.IsAlive() && player.IsRiaju())
                    {
                        var l = player.GetRiaju();

                        if (GameStates.Tuihou)
                        {
                            canceled = true;
                            break;
                        }
                        if (l is CustomRoles.NotAssigned or CustomRoles.OneLove || !l.IsRiaju()) break;

                        var send = "";
                        foreach (var ag in args)
                        {
                            if (ag.StartsWith("/")) continue;
                            send += ag;
                        }

                        Logger.Info($"{player.Data.GetLogPlayerName()} : {send}", "LoversChat");
                        foreach (var lover in AllPlayerControls)
                        {
                            if (lover && (lover.GetRiaju() == l || (PlayerControl.LocalPlayer.PlayerId == lover?.PlayerId && !lover.IsAlive())) && lover.PlayerId != player.PlayerId)
                            {
                                if (AmongUsClient.Instance.AmHost)
                                {
                                    var clientid = lover.GetClientId();
                                    if (clientid == -1) continue;
                                    string title = ColorString(GetRoleColor(l), $"<line-height=-18%>\n♥{GetPlayerColor(player)}♥</line-height>");
                                    string sendtext = send.Mark(GetRoleColor(l));
                                    SendMessage(sendtext, lover.PlayerId, title);
                                }
                            }
                        }
                        player.RpcProtectedMurderPlayer();
                    }
                    canceled = true;
                    break;
                case "/Twinschat":
                case "/twinschet":
                case "/tc":
                    if (Assassin.NowUse) break;
                    if (GameStates.InGame && Options.TwinsHideChat.GetBool() && player.IsAlive() && Twins.TwinsList.TryGetValue(player.PlayerId, out var twinsid))
                    {
                        string send = "";
                        if (GetHideSendText(ref canceled, ref send) is false) return;
                        Logger.Info($"{player.Data.GetLogPlayerName()} : {send}", "TwinsChat");

                        foreach (var twins in AllPlayerControls)
                        {
                            if (twins && (twins.PlayerId == twinsid || (PlayerControl.LocalPlayer.PlayerId == twins?.PlayerId && !twins.IsAlive())) && twins.PlayerId != player.PlayerId)
                            {
                                if (AmongUsClient.Instance.AmHost)
                                {
                                    var clientid = twins.GetClientId();
                                    if (clientid == -1) continue;
                                    string title = ColorString(GetRoleColor(CustomRoles.Twins), $"<line-height=-18%>\n∈{GetPlayerColor(player)}∋</line-height>");
                                    string sendtext = send.Mark(GetRoleColor(CustomRoles.Twins));
                                    SendMessage(sendtext, twins.PlayerId, title);
                                }
                            }
                        }
                        player.RpcProtectedMurderPlayer();
                    }
                    canceled = true;
                    break;
                case "/Connectingchat":
                case "/cc":
                    if (Assassin.NowUse) break;
                    if (GameStates.InGame && Options.ConnectingHideChat.GetBool() && player.IsAlive() && player.Is(CustomRoles.Connecting))
                    {
                        string send = "";
                        if (GetHideSendText(ref canceled, ref send) is false) return;
                        Logger.Info($"{player.Data.GetLogPlayerName()} : {send}", "Connectingchat");
                        foreach (var connect in AllPlayerControls)
                        {
                            if (connect && (connect.Is(CustomRoles.Connecting) || (PlayerControl.LocalPlayer.PlayerId == connect?.PlayerId && !connect.IsAlive())) && connect.PlayerId != player.PlayerId)
                            {
                                if (AmongUsClient.Instance.AmHost)
                                {
                                    var clientid = connect.GetClientId();
                                    if (clientid == -1) continue;
                                    string title = ColorString(GetRoleColor(CustomRoles.Connecting), $"<line-height=-18%>\nΨ{GetPlayerColor(player)}Ψ</line-height>");
                                    string sendtext = send.Mark(GetRoleColor(CustomRoles.Connecting));
                                    SendMessage(sendtext, connect.PlayerId, title);
                                }
                            }
                        }
                        player.RpcProtectedMurderPlayer();
                    }
                    canceled = true;
                    break;

                default:
                    if (args[0].StartsWith("/"))
                    {
                        canceled = Options.ExHideChatCommand.GetBool() && GameStates.Meeting;
                        break;
                    }
                    canceled = false;
                    /*
                    if (!player.IsAlive() && GameStates.Tuihou && AntiBlackout.IsCached)
                    {
                        ChatManager.SendPreviousMessagesToAll();
                        break;
                    }*/

                    if (!Options.ExHideChatCommand.GetBool()) break;

                    if (GameStates.Meeting && GameStates.IsMeeting && !AntiBlackout.IsSet && !AntiBlackout.IsCached && !canceled)
                    {
                        if (GameStates.Tuihou) break;

                        if (!player.IsAlive()) break;
                        _ = new LateTask(() =>
                        {
                            if (AmongUsClient.Instance.AmHost)
                            {
                                List<PlayerControl> sendplayers = new();
                                foreach (var pc in PlayerCatch.AllAlivePlayerControls)
                                {
                                    if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId || pc.IsModClient() ||
                                    player.PlayerId == PlayerControl.LocalPlayer.PlayerId || player.IsModClient() ||
                                    pc.PlayerId == player.PlayerId) continue;
                                    player.Data.IsDead = false;
                                    string playername = player.GetRealName(isMeeting: true);
                                    playername = playername.ApplyNameColorData(pc, player, true);

                                    var sender = CustomRpcSender.Create("MessagesToSend", SendOption.Reliable);
                                    sender.StartMessage(pc.GetClientId());

                                    MeetingHudPatch.StartPatch.Serialize = true;

                                    sender.Write((wit) =>
                                    {
                                        wit.StartMessage(1); //0x01 Data
                                        {
                                            wit.WritePacked(player.Data.NetId);
                                            player.Data.Serialize(wit, false);
                                        }
                                        wit.EndMessage();
                                    }, true);
                                    MeetingHudPatch.StartPatch.Serialize = false;
                                    sender.StartRpc(player.NetId, (byte)RpcCalls.SetName)
                                    .Write(player.NetId)
                                    .Write(playername)
                                    .EndRpc();
                                    sender.StartRpc(player.NetId, (byte)RpcCalls.SendChat)
                                            .Write(text)
                                            .EndRpc();
                                    player.Data.IsDead = true;
                                    MeetingHudPatch.StartPatch.Serialize = true;

                                    sender.Write((wit) =>
                                    {
                                        wit.StartMessage(1); //0x01 Data
                                        {
                                            wit.WritePacked(player.Data.NetId);
                                            player.Data.Serialize(wit, false);
                                        }
                                        wit.EndMessage();
                                    }, true);
                                    sender.EndMessage();
                                    sender.SendMessage();
                                    MeetingHudPatch.StartPatch.Serialize = false;
                                }
                                player.Data.IsDead = false;
                            }
                        }, Main.LagTime, "", true);
                    }
                    break;
            }
            if (AntiBlackout.IsCached && !player.IsAlive() && GameStates.InGame)
            {
                ChatManager.SendPreviousMessagesToAll();
            }
            canceled &= Options.ExHideChatCommand.GetBool();

            bool GetHideSendText(ref bool canceled, ref string text)
            {
                if (GameStates.Tuihou)
                {
                    canceled = true;
                    return false;
                }

                var send = "";
                foreach (var ag in args)
                {
                    if (ag.StartsWith("/")) continue;
                    send += ag;
                }
                text = send;
                return true;
            }
        }
    }
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
    class ChatUpdatePatch
    {
        public static bool DoBlockChat = false;
        public static bool BlockSendName = false;
        public static void Postfix(ChatController __instance)
        {
            if (!AmongUsClient.Instance.AmHost || Main.MessagesToSend.Count < 1 || (Main.MessagesToSend[0].Item2 == byte.MaxValue && Main.MessageWait.Value > __instance.timeSinceLastMessage)) return;
            if (DoBlockChat) return;

            if (GameStates.IsLobby) ChatManager.SendmessageInLobby();
            else ChatManager.SendMessageInGame();
        }
    }
    /*
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
    class AddChatPatch
    {
        public static void Postfix(string chatText)
        {
            switch (chatText)
            {
                default:
                    break;
            }
            if (!AmongUsClient.Instance.AmHost) return;
        }
    }*/
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSendChat))]
    class RpcSendChatPatch
    {
        public static bool Prefix(PlayerControl __instance, string chatText, ref bool __result)
        {
            if (string.IsNullOrWhiteSpace(chatText))
            {
                __result = false;
                return false;
            }
            int return_count = PlayerControl.LocalPlayer.name.Count(x => x == '\n');
            chatText = new StringBuilder(chatText).Insert(0, "\n", return_count).ToString();
            if (AmongUsClient.Instance.AmClient && DestroyableSingleton<HudManager>.Instance)
                DestroyableSingleton<HudManager>.Instance.Chat.AddChat(__instance, chatText);
            if (chatText.Contains("who", StringComparison.OrdinalIgnoreCase))
                DestroyableSingleton<UnityTelemetry>.Instance.SendWho();
            MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.SendChat, SendOption.None);
            messageWriter.Write(chatText);
            AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
            __result = true;
            return false;
        }
    }
}
