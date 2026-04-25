using System;

using System.Collections.Generic;

using System.IO;

using System.Linq;

using HarmonyLib;

using TownOfHost.Attributes;

using TownOfHost.Roles.Core;

using UnityEngine;

using static TownOfHost.Translator;

using static TownOfHost.Utils;



namespace TownOfHost.Modules;



public static class Moderator

{

    private static readonly string ModeratorListPath = Path.Combine(Main.BaseDirectory, "moderator.txt");

    private static readonly List<ModeratorEntry> Entries = new();

    private static readonly HashSet<string> FriendCodes = new(StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> Puids = new(StringComparer.OrdinalIgnoreCase);

    private const string ModeratorNamePrefix = "<#ADE0EE>モデレーター</color> - ";

    private const string ChatSendingModeratorNamePrefix = ModeratorNamePrefix;

    private const string LegacyModeratorBreakPrefix = "<#65BBE9><size=80%>モデレーター</size></color><br>";

    private const string LegacyModeratorPlainBreakPrefix = "モデレーター<br>";

    private static readonly Dictionary<byte, string> LastAppliedModeratorNames = new();

    private static readonly Dictionary<byte, string> ChatSendingOriginalNames = new();

    private static float NextModeratorNameRefreshTime;



    [PluginModuleInitializer]

    public static void Init()

    {

        Directory.CreateDirectory(Main.BaseDirectory);

        if (!File.Exists(ModeratorListPath)) File.Create(ModeratorListPath).Close();

        LastAppliedModeratorNames.Clear();

        ChatSendingOriginalNames.Clear();

        NextModeratorNameRefreshTime = 0f;

        Load();

    }



    public static bool TryHandleCommand(PlayerControl player, string[] args, out bool canceled)

    {

        canceled = false;

        if (!AmongUsClient.Instance.AmHost) return false;

        if (player == null || args == null || args.Length == 0) return false;



        var command = NormalizeCommand(args[0]);

        if (string.IsNullOrEmpty(command)) return false;



        if (command == "/mod")

        {

            canceled = true;

            HandleModCommand(player, args);

            return true;

        }



        if (!IsManagedCommand(command)) return false;



        canceled = true;

        if (!CanUseModeratorCommand(player))

        {

            SendMessage("モデレーター権限が必要です。", player.PlayerId);

            return true;

        }



        switch (command)

        {

            case "/kick":

                HandleKickOrBan(player, args, false);

                break;

            case "/ban":

                HandleKickOrBan(player, args, true);

                break;

            case "/say":

                HandleSay(player, args);

                break;

            case "/fe":

            case "/forceend":

                HandleForceEnd();

                break;

            case "/sw":

                HandleSetWinner(player, args);

                break;

            case "/start":

                HandleStart(player, args);

                break;

            case "/kf":

                if (GameStates.InGame) AllPlayerKillFlash();

                break;

            case "/mf":

                HandleMeetingFinish(player);

                break;

        }



        return true;

    }



    public static bool IsModerator(PlayerControl player)

    {

        if (player == null) return false;

        var client = player.GetClient();

        if (client == null) return false;



        var friendCode = NormalizeFriendCode(client.FriendCode);

        var puid = NormalizePuid(client.ProductUserId);



        return (!string.IsNullOrEmpty(friendCode) && FriendCodes.Contains(friendCode))

            || (!string.IsNullOrEmpty(puid) && Puids.Contains(puid));

    }



    public static bool TryGetLobbyChatDisplayName(PlayerControl player, out string displayName)

    {

        displayName = string.Empty;

        if (player == null) return false;

        if (!GameStates.IsLobby) return false;

        if (!IsModerator(player)) return false;



        var rawName = player.Data?.PlayerName ?? player.name;

        var baseName = RemoveModeratorPrefix(rawName).RemoveHtmlTags().Trim();

        if (string.IsNullOrWhiteSpace(baseName)) return false;



        displayName = $"{ChatSendingModeratorNamePrefix}{baseName}";

        return true;

    }



    private static bool IsManagedCommand(string command)

        => command is "/kick" or "/ban" or "/say" or "/fe" or "/forceend" or "/sw" or "/start" or "/kf" or "/mf";



    private static bool CanUseModeratorCommand(PlayerControl player)

        => IsHostPlayer(player) || IsModerator(player);



    private static bool IsHostPlayer(PlayerControl player)

    {

        if (player == null) return false;



        var client = player.GetClient();

        if (client != null && client.Id == AmongUsClient.Instance.HostId)

            return true;



        // fallback (host local player)

        return player.PlayerId == PlayerControl.LocalPlayer.PlayerId && AmongUsClient.Instance.AmHost;

    }



    private static void HandleModCommand(PlayerControl sender, string[] args)

    {

        if (!IsHostPlayer(sender))

        {

            SendMessage("/mod はホストのみ使用できます。", sender.PlayerId);

            return;

        }



        if (args.Length < 2)

        {

            SendMessage("使用方法: /cmd mod <名前|色|FC> | /cmd mod delete <名前|色|FC>", sender.PlayerId);

            return;

        }



        var action = args[1].ToLowerInvariant();

        var isDelete = action is "delete" or "del" or "remove";

        var explicitMode = action is "name" or "color" or "friendcode" ? action : "";

        var keyStartIndex = explicitMode != "" ? 2 : (isDelete ? 2 : 1);

        var key = args.Length > keyStartIndex ? string.Join(" ", args.Skip(keyStartIndex)).Trim() : "";

        if (string.IsNullOrWhiteSpace(key))

        {

            SendMessage("対象が空です。", sender.PlayerId);

            return;

        }



        if (isDelete)

        {

            HandleModDelete(sender, explicitMode, key);

            return;

        }



        var target = explicitMode == "" ? FindTargetAuto(key) : FindTarget(explicitMode, key);

        if (target == null)

        {

            SendMessage($"対象が見つかりません: {key}", sender.PlayerId);

            return;

        }



        var client = target.GetClient();

        if (client == null)

        {

            SendMessage("対象のクライアント情報を取得できませんでした。", sender.PlayerId);

            return;

        }



        var friendCode = NormalizeFriendCode(client.FriendCode);

        var puid = NormalizePuid(client.ProductUserId);



        if (string.IsNullOrEmpty(friendCode) && string.IsNullOrEmpty(puid))

        {

            SendMessage("FC/PUID が取得できないため追加できません。", sender.PlayerId);

            return;

        }



        var changed = UpsertModerator(friendCode, puid, target.Data?.PlayerName.RemoveHtmlTags() ?? target.name);

        if (changed)

        {

            Save();

            SendMessage($"モデレーターに追加: {target.Data?.PlayerName} ({friendCode})", sender.PlayerId);

        }

        else

        {

            SendMessage($"既にモデレーターです: {target.Data?.PlayerName}", sender.PlayerId);

        }



        RefreshModeratorDisplayNames(force: true);

    }



    private static void HandleModDelete(PlayerControl sender, string mode, string key)

    {

        var target = mode == "" ? FindTargetAuto(key) : FindTarget(mode, key);

        var removed = 0;

        string removedLabel = key;



        if (target != null)

        {

            var client = target.GetClient();

            var fc = NormalizeFriendCode(client?.FriendCode);

            var puid = NormalizePuid(client?.ProductUserId);

            removedLabel = target.Data?.PlayerName.RemoveHtmlTags() ?? target.name;

            RemoveModeratorByIdentity(fc, puid, ref removed);

        }

        else

        {

            if (mode is "" or "friendcode")

                RemoveModeratorByFriendCode(key, ref removed);

            if (mode is "" or "name")

                RemoveModeratorByName(key, ref removed);

        }



        if (removed > 0)

        {

            RebuildLookup();

            Save();

            SendMessage($"モデレーター解除: {removedLabel} ({removed}件)", sender.PlayerId);

            RefreshModeratorDisplayNames(force: true);

        }

        else

        {

            SendMessage($"モデレーターが見つかりません: {key}", sender.PlayerId);

        }

    }



    private static void HandleKickOrBan(PlayerControl sender, string[] args, bool ban)

    {

        if (args.Length < 2)

        {

            SendMessage($"使用方法: /cmd {(ban ? "ban" : "kick")} <PlayerId>", sender.PlayerId);

            return;

        }



        if (!TryFindTargetByArg(args[1], out var target))

        {

            SendMessage("対象プレイヤーが見つかりません。", sender.PlayerId);

            return;

        }



        if (target.PlayerId == sender.PlayerId)

        {

            SendMessage("自分自身には実行できません。", sender.PlayerId);

            return;

        }



        if (IsHostPlayer(target))

        {

            SendMessage("ホストは kick / ban できません。", sender.PlayerId);

            return;

        }



        if (IsModerator(target))

        {

            SendMessage("モデレーターは kick / ban できません。", sender.PlayerId);

            return;

        }



        var clientId = target.GetClientId();

        if (clientId == -1)

        {

            SendMessage("対象クライアントIDが取得できません。", sender.PlayerId);

            return;

        }



        if (ban)

        {

            BanManager.AddBanPlayer(target.GetClient());

        }



        AmongUsClient.Instance.KickPlayer(clientId, ban);

    }



    private static void HandleSay(PlayerControl sender, string[] args)

    {

        if (args.Length <= 1)

        {

            SendMessage("使用方法: /cmd say <message>", sender.PlayerId);

            return;

        }



        var message = string.Join(" ", args.Skip(1));

        var name = sender?.Data?.PlayerName?.RemoveHtmlTags() ?? "Unknown";

        SendMessage(message, title: $"<#F58220>モデからの伝言 ({name})</color>");

    }



    private static void HandleForceEnd()

    {

        if (GameStates.InGame)

            SendMessage(GetString("ForceEndText"));



        GameManager.Instance.enabled = false;

        CustomWinnerHolder.WinnerTeam = CustomWinner.Draw;

        GameManager.Instance.RpcEndGame(GameOverReason.ImpostorDisconnect, false);

    }



    private static void HandleSetWinner(PlayerControl sender, string[] args)

    {

        if (!GameStates.IsInGame)

        {

            SendMessage("ゲーム中のみ使用できます。", sender.PlayerId);

            return;

        }



        var subArgs = args.Length < 2 ? "" : args[1].ToLowerInvariant();

        switch (subArgs)

        {

            case "crewmate":

            case "crew":

            case "クルー":

            case "クルーメイト":

                GameManager.Instance.enabled = false;

                CustomWinnerHolder.WinnerTeam = CustomWinner.Crewmate;

                foreach (var p in PlayerCatch.AllPlayerControls.Where(pc => pc.Is(CustomRoleTypes.Crewmate)))

                    CustomWinnerHolder.WinnerIds.Add(p.PlayerId);

                GameManager.Instance.RpcEndGame(GameOverReason.CrewmatesByTask, false);

                break;



            case "impostor":

            case "imp":

            case "インポ":

            case "インポスター":

            case "インポス":

                GameManager.Instance.enabled = false;

                CustomWinnerHolder.WinnerTeam = CustomWinner.Impostor;

                foreach (var p in PlayerCatch.AllPlayerControls.Where(pc => pc.Is(CustomRoleTypes.Impostor) || pc.Is(CustomRoleTypes.Madmate)))

                    CustomWinnerHolder.WinnerIds.Add(p.PlayerId);

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

                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.JackalHadouHo);

                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Tama);

                GameManager.Instance.RpcEndGame(GameOverReason.ImpostorsByKill, false);

                break;



            case "draw":

            case "廃村":

                GameManager.Instance.enabled = false;

                CustomWinnerHolder.WinnerTeam = CustomWinner.Draw;

                GameManager.Instance.RpcEndGame(GameOverReason.ImpostorsByKill, false);

                break;



            default:

                if (UtilsRoleInfo.GetRoleByInputName(subArgs, out var role, true))

                {

                    CustomWinnerHolder.WinnerTeam = (CustomWinner)role;

                    CustomWinnerHolder.WinnerRoles.Add(role);

                    GameManager.Instance.RpcEndGame(GameOverReason.ImpostorsByKill, false);

                }

                else

                {

                    SendMessage("/cmd sw <crewmate|impostor|jackal|none|draw|role>", sender.PlayerId);

                }

                break;

        }



        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Admin, 0);

    }



    private static void HandleStart(PlayerControl sender, string[] args)

    {

        if (!GameStates.IsLobby)

        {

            SendMessage("/start はロビーでのみ使用できます。", sender.PlayerId);

            return;

        }



        var sec = 5;

        if (args.Length >= 2)

        {

            if (!int.TryParse(args[1], out sec))

            {

                SendMessage("使用方法: /cmd start <0-600>", sender.PlayerId);

                return;

            }

        }



        sec = Mathf.Clamp(sec, 0, 600);



        var gsm = GameStartManager.Instance;

        if (gsm == null)

        {

            SendMessage("GameStartManager が取得できません。", sender.PlayerId);

            return;

        }



        gsm.countDownTimer = sec <= 0 ? 0.1f : sec;

        gsm.startState = GameStartManager.StartingStates.Countdown;

    }



    private static void HandleMeetingFinish(PlayerControl sender)

    {

        if (!GameStates.IsMeeting)

        {

            SendMessage("/mf は会議中のみ使用できます。", sender.PlayerId);

            return;

        }



        try

        {

            MeetingVoteManager.Instance?.EndMeeting(true);

        }

        catch

        {

            try

            {

                MeetingHud.Instance?.CheckForEndVoting();

            }

            catch

            {

                SendMessage("会議終了処理に失敗しました。", sender.PlayerId);

            }

        }

    }



    private static PlayerControl FindTarget(string mode, string key)

    {

        return mode switch

        {

            "name" => FindTargetByName(key),

            "color" => TryGetColorId(key, out var colorId)

                ? FindTargetByColor(colorId)

                : null,

            "friendcode" => FindTargetByFriendCode(key),

            _ => null

        };

    }



    private static PlayerControl FindTargetAuto(string key)

    {

        if (byte.TryParse(key, out var id))

        {

            var byId = PlayerCatch.GetPlayerById(id);

            if (byId != null) return byId;

        }



        var byFc = FindTargetByFriendCode(key);

        if (byFc != null) return byFc;



        if (TryGetColorId(key, out var colorId))

        {

            var byColor = FindTargetByColor(colorId);

            if (byColor != null) return byColor;

        }



        return FindTargetByName(key);

    }



    private static PlayerControl FindTargetByName(string key)

        => PlayerCatch.AllPlayerControls.FirstOrDefault(pc =>

            string.Equals(GetDisplayNameForLookup(pc), key, StringComparison.OrdinalIgnoreCase)

            || string.Equals((pc?.Data?.PlayerName ?? "").RemoveHtmlTags(), key, StringComparison.OrdinalIgnoreCase));



    private static PlayerControl FindTargetByColor(int colorId)

        => PlayerCatch.AllPlayerControls.FirstOrDefault(pc => pc?.Data?.DefaultOutfit.ColorId == colorId);



    private static PlayerControl FindTargetByFriendCode(string key)

    {

        var normalizedFc = NormalizeFriendCode(key);

        return PlayerCatch.AllPlayerControls.FirstOrDefault(pc =>

        {

            var client = pc.GetClient();

            return client != null && string.Equals(NormalizeFriendCode(client.FriendCode), normalizedFc, StringComparison.OrdinalIgnoreCase);

        });

    }



    private static bool TryFindTargetByArg(string arg, out PlayerControl target)

    {

        target = FindTargetAuto(arg);

        return target != null;

    }



    private static bool HasModeratorPrefix(string name)

        => !string.IsNullOrWhiteSpace(name)

            && (name.StartsWith(ModeratorNamePrefix, StringComparison.Ordinal)

                || name.StartsWith(ChatSendingModeratorNamePrefix, StringComparison.Ordinal)

                || name.StartsWith(LegacyModeratorBreakPrefix, StringComparison.Ordinal)

                || name.StartsWith(LegacyModeratorPlainBreakPrefix, StringComparison.Ordinal)

                || name.StartsWith("モデレーター\n", StringComparison.Ordinal)

                || name.StartsWith("モデレーター\r\n", StringComparison.Ordinal));



    private static string RemoveModeratorPrefix(string name)

    {

        if (string.IsNullOrWhiteSpace(name)) return string.Empty;



        var result = name;

        if (result.StartsWith(ModeratorNamePrefix, StringComparison.Ordinal))

            return result.Substring(ModeratorNamePrefix.Length);



        if (result.StartsWith(ChatSendingModeratorNamePrefix, StringComparison.Ordinal))

            return result.Substring(ChatSendingModeratorNamePrefix.Length);



        if (result.StartsWith(LegacyModeratorBreakPrefix, StringComparison.Ordinal))

            return result.Substring(LegacyModeratorBreakPrefix.Length);



        if (result.StartsWith(LegacyModeratorPlainBreakPrefix, StringComparison.Ordinal))

            return result.Substring(LegacyModeratorPlainBreakPrefix.Length);



        if (result.StartsWith("モデレーター\r\n", StringComparison.Ordinal))

            return result.Substring("モデレーター\r\n".Length);



        if (result.StartsWith("モデレーター\n", StringComparison.Ordinal))

            return result.Substring("モデレーター\n".Length);



        return result;

    }



    private static string GetDisplayNameForLookup(PlayerControl player)

    {

        var rawName = player?.Data?.PlayerName ?? player?.name ?? string.Empty;

        return RemoveModeratorPrefix(rawName).RemoveHtmlTags().Trim();

    }



    private static void RefreshModeratorDisplayNames(bool force = false)

    {

        if (!AmongUsClient.Instance.AmHost || !GameStates.IsLobby) return;

        if (!force && Time.time < NextModeratorNameRefreshTime) return;



        NextModeratorNameRefreshTime = Time.time + 0.5f;



        foreach (var pc in PlayerCatch.AllPlayerControls)

        {

            if (pc == null || pc.Data == null || pc.name == "Player(Clone)") continue;

            if (ChatSendingOriginalNames.ContainsKey(pc.PlayerId)) continue;



            var currentName = pc.Data.PlayerName ?? pc.name;

            var hasPrefix = HasModeratorPrefix(currentName);

            var baseName = RemoveModeratorPrefix(currentName);



            var targetName = IsModerator(pc)

                ? $"{ModeratorNamePrefix}{baseName}"

                : (hasPrefix ? baseName : currentName);



            if (string.Equals(currentName, targetName, StringComparison.Ordinal))

            {

                LastAppliedModeratorNames[pc.PlayerId] = targetName;

                continue;

            }



            if (!force

                && LastAppliedModeratorNames.TryGetValue(pc.PlayerId, out var lastName)

                && string.Equals(lastName, targetName, StringComparison.Ordinal))

                continue;



            pc.SetName(targetName);

            pc.RpcSetName(targetName);

            LastAppliedModeratorNames[pc.PlayerId] = targetName;

        }

    }



    private static void StripModeratorDisplayNamesForGame()

    {

        if (!AmongUsClient.Instance.AmHost) return;



        ChatSendingOriginalNames.Clear();



        foreach (var pc in PlayerCatch.AllPlayerControls)

        {

            if (pc == null || pc.Data == null || pc.name == "Player(Clone)") continue;



            var currentName = pc.Data.PlayerName ?? pc.name;

            if (!HasModeratorPrefix(currentName)) continue;



            var baseName = RemoveModeratorPrefix(currentName);

            if (string.IsNullOrWhiteSpace(baseName)) continue;



            pc.SetName(baseName);

            pc.RpcSetName(baseName);

            Main.AllPlayerNames[pc.PlayerId] = baseName;

            LastAppliedModeratorNames[pc.PlayerId] = baseName;

        }

    }



    private static void SanitizeNameCachesForResult()

    {

        if (!AmongUsClient.Instance.AmHost) return;



        var keys = Main.AllPlayerNames.Keys.ToArray();

        foreach (var id in keys)

        {

            if (!Main.AllPlayerNames.TryGetValue(id, out var name)) continue;

            var sanitized = RemoveModeratorPrefix(name ?? string.Empty).RemoveHtmlTags().Trim();

            if (string.IsNullOrWhiteSpace(sanitized)) continue;

            Main.AllPlayerNames[id] = sanitized;

        }

    }



    public static void OnBeforeChatSend(PlayerControl player)

        => BeginTemporaryChatName(player);



    public static void OnAfterChatSend(PlayerControl player)

        => EndTemporaryChatName(player);



    private static void BeginTemporaryChatName(PlayerControl player)

    {

        if (player == null || !GameStates.IsLobby) return;

        if (ChatSendingOriginalNames.ContainsKey(player.PlayerId)) return;



        var currentName = player.Data?.PlayerName ?? player.name;

        if (!HasModeratorPrefix(currentName)) return;



        var baseName = RemoveModeratorPrefix(currentName);

        if (string.IsNullOrWhiteSpace(baseName)) return;



        ChatSendingOriginalNames[player.PlayerId] = currentName;

        var sendingName = $"{ChatSendingModeratorNamePrefix}{baseName}";

        player.SetName(sendingName);

        player.RpcSetName(sendingName);

    }



    private static void EndTemporaryChatName(PlayerControl player)

    {

        if (player == null || !GameStates.IsLobby) return;



        if (!ChatSendingOriginalNames.TryGetValue(player.PlayerId, out var originalName)) return;

        if (string.IsNullOrWhiteSpace(originalName)) return;



        var playerId = player.PlayerId;

        _ = new LateTask(() =>

        {

            if (!GameStates.IsLobby) return;

            if (!ChatSendingOriginalNames.TryGetValue(playerId, out var cachedOriginalName)) return;

            ChatSendingOriginalNames.Remove(playerId);



            var targetPlayer = PlayerCatch.GetPlayerById(playerId);

            if (targetPlayer == null || targetPlayer.Data == null) return;

            if (string.IsNullOrWhiteSpace(cachedOriginalName)) return;



            var baseName = RemoveModeratorPrefix(cachedOriginalName);

            var restoreName = IsModerator(targetPlayer)

                ? $"{ModeratorNamePrefix}{baseName}"

                : baseName;

            targetPlayer.SetName(restoreName);

            targetPlayer.RpcSetName(restoreName);

        }, 0.25f, "Moderator.RestoreNameAfterChat", true);

    }



    private static bool TryGetColorId(string value, out int colorId)

    {

        colorId = value.ToLowerInvariant() switch

        {

            "red" or "赤" or "レッド" => 0,

            "blue" or "青" or "ブルー" => 1,

            "green" or "緑" or "グリーン" => 2,

            "pink" or "ピンク" => 3,

            "orange" or "オレンジ" => 4,

            "yellow" or "黄" or "イエロー" => 5,

            "black" or "黒" or "ブラック" => 6,

            "white" or "白" or "ホワイト" => 7,

            "purple" or "紫" or "パープル" => 8,

            "brown" or "ブラウン" => 9,

            "cyan" or "シアン" => 10,

            "lime" or "ライム" => 11,

            "maroon" or "マルーン" => 12,

            "rose" or "ローズ" => 13,

            "banana" or "バナナ" => 14,

            "gray" or "grey" or "グレー" => 15,

            "tan" or "タン" => 16,

            "coral" or "コーラル" => 17,

            _ => -1

        };



        return colorId >= 0;

    }



    private static bool UpsertModerator(string friendCode, string puid, string name)

    {

        friendCode = NormalizeFriendCode(friendCode);

        puid = NormalizePuid(puid);

        name = (name ?? string.Empty).Replace(',', ' ').Trim();



        if (string.IsNullOrEmpty(friendCode) && string.IsNullOrEmpty(puid)) return false;



        var existing = Entries.FirstOrDefault(e =>

            (!string.IsNullOrEmpty(friendCode) && string.Equals(e.FriendCode, friendCode, StringComparison.OrdinalIgnoreCase))

            || (!string.IsNullOrEmpty(puid) && string.Equals(e.Puid, puid, StringComparison.OrdinalIgnoreCase)));



        if (existing != null)

        {

            existing.FriendCode = string.IsNullOrEmpty(existing.FriendCode) ? friendCode : existing.FriendCode;

            existing.Puid = string.IsNullOrEmpty(existing.Puid) ? puid : existing.Puid;

            if (!string.IsNullOrEmpty(name)) existing.Name = name;

            RebuildLookup();

            return false;

        }



        Entries.Add(new ModeratorEntry

        {

            FriendCode = friendCode,

            Puid = puid,

            Name = name

        });



        RebuildLookup();

        return true;

    }



    private static void RemoveModeratorByIdentity(string friendCode, string puid, ref int removed)

    {

        friendCode = NormalizeFriendCode(friendCode);

        puid = NormalizePuid(puid);



        if (string.IsNullOrEmpty(friendCode) && string.IsNullOrEmpty(puid)) return;



        removed += Entries.RemoveAll(e =>

            (!string.IsNullOrEmpty(friendCode) && string.Equals(e.FriendCode, friendCode, StringComparison.OrdinalIgnoreCase))

            || (!string.IsNullOrEmpty(puid) && string.Equals(e.Puid, puid, StringComparison.OrdinalIgnoreCase)));

    }



    private static void RemoveModeratorByName(string name, ref int removed)

    {

        if (string.IsNullOrWhiteSpace(name)) return;

        removed += Entries.RemoveAll(e =>

            !string.IsNullOrWhiteSpace(e.Name)

            && string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));

    }



    private static void RemoveModeratorByFriendCode(string friendCode, ref int removed)

    {

        var normalizedFc = NormalizeFriendCode(friendCode);

        if (string.IsNullOrWhiteSpace(normalizedFc)) return;

        removed += Entries.RemoveAll(e => string.Equals(e.FriendCode, normalizedFc, StringComparison.OrdinalIgnoreCase));

    }



    private static void Load()

    {

        Entries.Clear();



        foreach (var rawLine in File.ReadAllLines(ModeratorListPath))

        {

            var line = rawLine?.Trim();

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;



            var parts = line.Split(',', 3);

            var friendCode = parts.Length > 0 ? NormalizeFriendCode(parts[0]) : string.Empty;

            var puid = parts.Length > 1 ? NormalizePuid(parts[1]) : string.Empty;

            var name = parts.Length > 2 ? parts[2].Trim() : string.Empty;



            if (string.IsNullOrEmpty(friendCode) && string.IsNullOrEmpty(puid)) continue;



            Entries.Add(new ModeratorEntry

            {

                FriendCode = friendCode,

                Puid = puid,

                Name = name

            });

        }



        RebuildLookup();

    }



    private static void Save()

    {

        Directory.CreateDirectory(Main.BaseDirectory);



        var lines = Entries

            .Where(e => !string.IsNullOrEmpty(e.FriendCode) || !string.IsNullOrEmpty(e.Puid))

            .Select(e => $"{e.FriendCode},{e.Puid},{e.Name}")

            .ToArray();



        File.WriteAllLines(ModeratorListPath, lines);

    }



    private static void RebuildLookup()

    {

        FriendCodes.Clear();

        Puids.Clear();



        foreach (var entry in Entries)

        {

            if (!string.IsNullOrEmpty(entry.FriendCode)) FriendCodes.Add(entry.FriendCode);

            if (!string.IsNullOrEmpty(entry.Puid)) Puids.Add(entry.Puid);

        }

    }



    private static string NormalizeCommand(string value)

    {

        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var cmd = value.Trim();

        if (!cmd.StartsWith('/')) cmd = "/" + cmd;

        return cmd.ToLowerInvariant();

    }



    private static string NormalizeFriendCode(string value)

    {

        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        return value.Trim().Replace(':', '#').ToUpperInvariant();

    }



    private static string NormalizePuid(string value)

    {

        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        return value.Trim();

    }



    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSendChat))]

    private static class RpcSendChatPatch

    {

        [HarmonyPriority(Priority.First)]

        public static void Prefix(PlayerControl __instance)

        {

            BeginTemporaryChatName(__instance);

        }



        [HarmonyPriority(Priority.Last)]

        public static void Postfix(PlayerControl __instance)

        {

            EndTemporaryChatName(__instance);

        }

    }



    [HarmonyPatch(typeof(LobbyBehaviour), nameof(LobbyBehaviour.Update))]

    private static class LobbyBehaviourUpdatePatch

    {

        public static void Postfix()

        {

            RefreshModeratorDisplayNames();

        }

    }



    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]

    private static class ShipStatusStartPatch

    {

        public static void Postfix()

        {

            StripModeratorDisplayNamesForGame();

        }

    }



    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]

    private static class AmongUsClientOnGameEndPatch

    {

        public static void Prefix()

        {

            SanitizeNameCachesForResult();

        }

    }



    private sealed class ModeratorEntry

    {

        public string FriendCode;

        public string Puid;

        public string Name;

    }

}









