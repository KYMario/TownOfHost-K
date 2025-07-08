using System.Collections.Generic;
using System.Linq;
using Hazel;
using System;
using TownOfHost.Roles.Core;

namespace TownOfHost.Modules.ChatManager
{
    //参考→https://github.com/0xDrMoe/TownofHost-Enhanced/releases/tag/v1.0.1
    public class ChatManager
    {
        public static bool cancel = false;
        private static List<string> chatHistory = new();
        private const int maxHistorySize = 20;
        [Attributes.GameModuleInitializer]
        public static void ResetChat()
        {
            chatHistory.Clear();
        }
        public static bool CheckCommond(ref string msg, string command, bool exact = true)
        {
            var comList = command.Split('|');
            for (int i = 0; i < comList.Length; i++)
            {
                if (exact)
                {
                    if (msg == "/" + comList[i]) return true;
                }
                else
                {
                    if (msg.StartsWith("/" + comList[i]))
                    {
                        msg = msg.Replace("/" + comList[i], string.Empty);
                        return true;
                    }
                }
            }
            return false;
        }
        public static bool CommandCheck(string meg)
        {
            var m = meg;
            if (m.StartsWith("/")) return true;
            return false;
        }
        public static void SendMessage(PlayerControl player, string message)
        {
            int operate = 0; // 1:ID 2:猜测
            string msg = message;
            string playername = player.GetNameWithRole();
            message = message.ToLower().TrimStart().TrimEnd();
            var isalive = player.IsAlive();
            if (!isalive || !AmongUsClient.Instance.AmHost) return;
            operate = GameStates.IsInGame ? 3 : 6;
            if (msg.StartsWith("<size=0>.</size>")) operate = 3;//投票の記録
            else if (CheckCommond(ref msg, "bt", false)) operate = 2;
            else if (CommandCheck(message)) operate = 1;
            else if (message.RemoveHtmlTags() != message) operate = 5;//tagが含まれてるならシステムメッセ

            //ワード検知はできるようにはなった。
            /*if (message.Contains("ｱ") && operate == 3)
            {
                operate = 4;
                var pos = player.transform.position;
                player.RpcSnapToForced(new UnityEngine.Vector2(999f, 999f));

                _ = new LateTask(() => player.RpcMurderPlayer(player), 0.2f, "");
                _ = new LateTask(() =>
                {
                    Utils.SendMessage(Utils.GetPlayerColor(player.PlayerId, true) + "は余計なことを言ったから消えちゃった...");
                    player.RpcSnapToForced(pos);

                    var meetingHud = MeetingHud.Instance;

                    PlayerVoteArea voteArea = MeetingHud.Instance.playerStates.First(x => x.TargetPlayerId == player.PlayerId);
                    if (voteArea == null) return;
                    if (voteArea.DidVote) voteArea.UnsetVote();
                    foreach (var playerVoteArea in meetingHud.playerStates)
                    {
                        if (playerVoteArea.VotedFor != player.PlayerId) continue;
                        playerVoteArea.UnsetVote();
                        meetingHud.RpcClearVote(playerVoteArea.TargetPlayerId);
                        meetingHud.ClearVote();
                        MeetingHudPatch.CastVotePatch.Prefix(meetingHud, playerVoteArea.TargetPlayerId, player.PlayerId);
                        var voteAreaPlayer = PlayerCatch.GetPlayerById(playerVoteArea.TargetPlayerId);
                        if (!voteAreaPlayer.AmOwner) continue;
                        MeetingHudPatch.CastVotePatch.Prefix(meetingHud, playerVoteArea.TargetPlayerId, player.PlayerId);
                        meetingHud.RpcClearVote(voteAreaPlayer.GetClientId());
                        meetingHud.ClearVote();
                        playerVoteArea.UnsetVote();
                    }
                    _ = new LateTask(() => meetingHud.CheckForEndVoting(), 5f, "VoteChack");
                }, 1f, "");
            }*/

            switch (operate)
            {
                case 1://その他コマンド
                    message = msg;
                    cancel = true;
                    break;
                case 2://ゲッサーコマンド
                    message = msg;
                    cancel = false;
                    break;
                case 3: //投票の記録、通常のチャット
                    if (Main.UseYomiage.Value && isalive) Yomiage.Send(player.Data.DefaultOutfit.ColorId, message).Wait();
                    message = msg;
                    string chatEntry = $"{player.PlayerId}: {message}";
                    chatHistory.Add(chatEntry);
                    if (chatHistory.Count > maxHistorySize)
                    {
                        chatHistory.RemoveAt(0);
                    }
                    cancel = false;
                    break;
                case 4: //特定の人物が喋ったら消す等
                    message = msg;
                    cancel = false;
                    SendPreviousMessagesToAll();
                    break;
                case 5://システムメッセージ
                    message = msg;
                    cancel = false;
                    break;
                case 6://ロビーの処理
                    if (Main.UseYomiage.Value && isalive) Yomiage.Send(player.Data.DefaultOutfit.ColorId, message).Wait();
                    break;
            }
        }

        public static void SendPreviousMessagesToAll()
        {
            var rd = IRandom.Instance;
            string msg;
            List<CustomRoles> roles = Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>().Where(role => (role.IsRiaju() || role.IsCrewmate() || role.IsImpostorTeam() || role.IsNeutral()) && !role.IsE() && Event.CheckRole(role) && role is not CustomRoles.Assassin and CustomRoles.Merlin).ToList();
            string[] specialTexts = new string[] { "bt" };

            for (int i = chatHistory.Count; i < 30; i++)
            {
                msg = "/";
                msg += specialTexts[rd.Next(0, specialTexts.Length - 1)] + " ";
                msg += rd.Next(0, 15).ToString() + " ";
                CustomRoles role = roles[rd.Next(0, roles.Count)];
                msg += UtilsRoleText.GetRoleName(role) + " ";

                if (PlayerCatch.AllAlivePlayersCount == 0) break;
                var player = PlayerCatch.AllAlivePlayerControls.ToArray()[rd.Next(0, PlayerCatch.AllAlivePlayersCount)];
                DestroyableSingleton<HudManager>.Instance.Chat.AddChat(player, msg);
                var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
                writer.StartMessage(-1);
                writer.StartRpc(player.NetId, (byte)RpcCalls.SendChat)
                    .Write(msg)
                    .EndRpc();
                writer.EndMessage();
                writer.SendMessage();
            }

            foreach (var entry in chatHistory)
            {
                var entryParts = entry.Split(':');
                var senderId = entryParts[0].Trim();
                var senderMessage = entryParts[1].Trim();
                var isvote = senderMessage.StartsWith("<size=0>.</size>");

                foreach (var senderPlayer in PlayerCatch.AllPlayerControls)
                {
                    if (senderPlayer.PlayerId.ToString() == senderId)
                    {
                        if (!senderPlayer.IsAlive())
                        {
                            //var deathReason = (PlayerState.DeathReason)senderPlayer.PlayerId;
                            senderPlayer.Revive();
                            if (isvote)
                            {
                                DestroyableSingleton<HudManager>.Instance.Chat.AddChatNote(senderPlayer.Data, ChatNoteTypes.DidVote);

                                var wt = CustomRpcSender.Create("MessagesToSend", SendOption.None);
                                wt.StartMessage(-1);
                                wt.StartRpc(senderPlayer.NetId, (byte)RpcCalls.SendChatNote)
                                .Write(senderPlayer.PlayerId)
                                .Write((int)ChatNoteTypes.DidVote)
                                    .EndRpc();
                                wt.EndMessage();
                                wt.SendMessage();
                                senderPlayer.Die(DeathReason.Kill, true);
                                continue;
                            }
                            DestroyableSingleton<HudManager>.Instance.Chat.AddChat(senderPlayer, senderMessage);

                            var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
                            writer.StartMessage(-1);
                            writer.StartRpc(senderPlayer.NetId, (byte)RpcCalls.SendChat)
                                .Write(senderMessage)
                                .EndRpc();
                            writer.EndMessage();
                            writer.SendMessage();
                            senderPlayer.Die(DeathReason.Kill, true);
                            //Main.PlayerStates[senderPlayer.PlayerId].deathReason = deathReason;
                        }
                        else
                        {
                            if (isvote)
                            {
                                DestroyableSingleton<HudManager>.Instance.Chat.AddChatNote(senderPlayer.Data, ChatNoteTypes.DidVote);

                                var wt = CustomRpcSender.Create("MessagesToSend", SendOption.None);
                                wt.StartMessage(-1);
                                wt.StartRpc(senderPlayer.NetId, (byte)RpcCalls.SendChatNote)
                                .Write(senderPlayer.PlayerId)
                                .Write((int)ChatNoteTypes.DidVote)
                                    .EndRpc();
                                wt.EndMessage();
                                wt.SendMessage();
                                continue;
                            }
                            DestroyableSingleton<HudManager>.Instance.Chat.AddChat(senderPlayer, senderMessage);
                            var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
                            writer.StartMessage(-1);
                            writer.StartRpc(senderPlayer.NetId, (byte)RpcCalls.SendChat)
                                .Write(senderMessage)
                                .EndRpc();
                            writer.EndMessage();
                            writer.SendMessage();
                        }
                    }
                }
            }
        }
        public static void IntaskCheckSendMessage(PlayerControl player)
        {
            if (!GameStates.Meeting && PlayerControl.LocalPlayer.IsAlive() && !ChatUpdatePatch.DoBlockChat)
            {
                if (Main.MessagesToSend.Where(x => x.Item2 is not byte.MaxValue).Count() > 0)
                {
                    (string msg, byte sendTo, string title) = Main.MessagesToSend.Where(x => x.Item2 is not byte.MaxValue).FirstOrDefault();
                    if (sendTo is not byte.MaxValue && Main.MegCount < 50)
                    {
                        Main.MessagesToSend.Remove((msg, sendTo, title));
                        var sendpc = PlayerCatch.GetPlayerById(sendTo);
                        int clientId = sendpc.GetClientId();
                        if (sendpc != null)
                        {
                            var name = sendpc.Data.GetLogPlayerName();
                            if (clientId == -1)
                            {
                                sendpc.SetName(title);
                                DestroyableSingleton<HudManager>.Instance.Chat.AddChat(sendpc, msg);
                                sendpc.SetName(name);
                            }
                            var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
                            writer.StartMessage(clientId);
                            writer.StartRpc(sendpc.NetId, (byte)RpcCalls.SetName)
                                .Write(player.Data.NetId)
                                .Write(title)
                                .EndRpc();
                            writer.StartRpc(sendpc.NetId, (byte)RpcCalls.SendChat)
                                .Write(msg)
                                .EndRpc();
                            writer.EndMessage();
                            writer.SendMessage();
                            UtilsNotifyRoles.NotifyRoles(true, false, true, [sendpc]);
                            if (!Main.IsCs() && Options.ExRpcWeightR.GetBool()) Main.MegCount++;
                        }
                    }
                }
            }
        }
        public static void SendMessageInGame()
        {
            PlayerControl senderplayer = PlayerCatch.AllAlivePlayerControls.OrderBy(x => x.PlayerId).FirstOrDefault();
            if (senderplayer == null) return;

            (string msg, byte sendTo, string title) = Main.MessagesToSend[0];
            var name = senderplayer.Data.GetLogPlayerName();
            var SendToPlayerControl = PlayerCatch.GetPlayerById(sendTo);
            int clientId = sendTo == byte.MaxValue ? -1 : SendToPlayerControl.GetClientId();

            if (clientId is not -1 && SendToPlayerControl is null)
            {
                Main.MessagesToSend.RemoveAt(0);
                Logger.Error($"{sendTo}がnullの為弾きます。", "SendMassage");
                return;
            }

            // タスク中で送信者が生きてて全員に表示 => 個別送信に切り替え、名前をその人視点の者に戻す
            if (GameStates.Meeting is false && senderplayer.IsAlive() && sendTo == byte.MaxValue)
            {
                Main.MessagesToSend.RemoveAt(0);

                foreach (var seer in PlayerCatch.AllPlayerControls)
                {
                    int seerclientid = seer.GetClientId();
                    string playername = seer.GetRealName(isMeeting: true);
                    playername = playername.ApplyNameColorData(seer, seer, true);
                    if (Main.LastNotifyNames.TryGetValue((seer.PlayerId, seer.PlayerId), out var lastname))
                    {
                        playername = lastname;
                    }

                    var Nwriter = CustomRpcSender.Create("MessagesToSend", SendOption.None);
                    Nwriter.StartMessage(seerclientid);
                    Nwriter.StartRpc(seer.NetId, (byte)RpcCalls.SetName)
                    .Write(seer.Data.NetId)
                    .Write(title)
                    .EndRpc();
                    Nwriter.StartRpc(seer.NetId, (byte)RpcCalls.SendChat)
                    .Write(msg)
                    .EndRpc();
                    Nwriter.StartRpc(seer.NetId, (byte)RpcCalls.SetName)
                    .Write(seer.Data.NetId)
                    .Write(playername)
                    .EndRpc();
                    Nwriter.EndMessage();
                    Nwriter.SendMessage();
                    UtilsNotifyRoles.NotifyRoles();
                }
                return;
            }

            if (Options.ExHideChatCommand.GetBool() is false ||//秘匿Off
                    (senderplayer.PlayerId == 0 && sendTo == byte.MaxValue))//秘匿Onだけど、Snedplayrがホストかつ全員に送信
            {
                Main.MessagesToSend.RemoveAt(0);
                // ホスト視点でのチャット送信
                if (sendTo == byte.MaxValue)
                {
                    senderplayer.SetName(title);
                    DestroyableSingleton<HudManager>.Instance.Chat.AddChat(senderplayer, msg);
                    senderplayer.SetName(name);
                }
                var Nwriter = CustomRpcSender.Create("MessagesToSend", SendOption.None);
                Nwriter.StartMessage(clientId);
                Nwriter.StartRpc(senderplayer.NetId, (byte)RpcCalls.SetName)
                .Write(senderplayer.Data.NetId)
                .Write(title)
                .EndRpc();
                Nwriter.StartRpc(senderplayer.NetId, (byte)RpcCalls.SendChat)
                .Write(msg)
                .EndRpc();
                Nwriter.StartRpc(senderplayer.NetId, (byte)RpcCalls.SetName)
                .Write(senderplayer.Data.NetId)
                .Write(senderplayer.Data.GetLogPlayerName())
                .EndRpc();
                Nwriter.EndMessage();
                Nwriter.SendMessage();
                if (GameStates.Meeting && Main.MessagesToSend.Count < 1)
                {
                    _ = new LateTask(() =>
                    {
                        NameColorManager.RpcMeetingColorName(senderplayer);
                        ChatUpdatePatch.DoBlockChat = false;
                    }, Main.LagTime, "Setname", true);
                }
                return;
            }
            if (Options.ExHideChatCommand.GetBool())
            {
                Main.MessagesToSend.RemoveAt(0);
                List<PlayerControl> Seers = new();
                Seers = sendTo == byte.MaxValue ? PlayerCatch.AllPlayerControls.ToList() : [SendToPlayerControl];

                foreach (var seer in Seers)
                {
                    int seerclientid = seer.GetClientId();
                    string playername = seer.GetRealName(isMeeting: true);
                    playername = playername.ApplyNameColorData(seer, seer, true);
                    if (!GameStates.IsMeeting && Main.LastNotifyNames.TryGetValue((seer.PlayerId, seer.PlayerId), out var lastname))
                    {
                        playername = lastname;
                    }

                    var Nwriter = CustomRpcSender.Create("MessagesToSend", SendOption.None);
                    Nwriter.StartMessage(seerclientid);
                    Nwriter.StartRpc(seer.NetId, (byte)RpcCalls.SetName)
                    .Write(seer.Data.NetId)
                    .Write(title)
                    .EndRpc();
                    Nwriter.StartRpc(seer.NetId, (byte)RpcCalls.SendChat)
                    .Write(msg)
                    .EndRpc();
                    Nwriter.StartRpc(seer.NetId, (byte)RpcCalls.SetName)
                    .Write(seer.Data.NetId)
                    .Write(playername)
                    .EndRpc();
                    Nwriter.EndMessage();
                    Nwriter.SendMessage();
                }
            }
        }
        public static void SendmessageInLobby()
        {
            PlayerControl senderplayer = PlayerCatch.AllAlivePlayerControls.Where(x => x.PlayerId is not 0).OrderBy(x => x.PlayerId).FirstOrDefault();
            if (senderplayer == null)
            {
                if (PlayerControl.LocalPlayer is not null)
                {
                    senderplayer = PlayerControl.LocalPlayer;
                }
                else
                { return; }
            }
            (string msg, byte sendTo, string title) = Main.MessagesToSend[0];
            var name = senderplayer.Data.GetLogPlayerName();
            var SendToPlayerControl = PlayerCatch.GetPlayerById(sendTo);
            int clientId = sendTo == byte.MaxValue ? -1 : SendToPlayerControl.GetClientId();

            if (title.RemoveHtmlTags() == title) // ホストが送信した場合、
            {
                senderplayer = PlayerControl.LocalPlayer;
                if (senderplayer == null) senderplayer = PlayerCatch.AllAlivePlayerControls.OrderBy(x => x.PlayerId).FirstOrDefault();
            }
            if (senderplayer == PlayerControl.LocalPlayer) _ = new LateTask(() => Utils.ApplySuffix(null, true), 0.24f, "", true);

            Main.MessagesToSend.RemoveAt(0);

            // ホスト視点でのチャット送信
            if (clientId is -1)
            {
                senderplayer.SetName(title);
                DestroyableSingleton<HudManager>.Instance.Chat.AddChat(senderplayer, msg);
                senderplayer.SetName(name);
            }
            var Nwriter = CustomRpcSender.Create("MessagesToSend", SendOption.None);
            Nwriter.StartMessage(clientId);
            Nwriter.StartRpc(senderplayer.NetId, (byte)RpcCalls.SetName)
            .Write(senderplayer.Data.NetId)
            .Write(title)
            .EndRpc();
            Nwriter.StartRpc(senderplayer.NetId, (byte)RpcCalls.SendChat)
            .Write(msg)
            .EndRpc();
            Nwriter.StartRpc(senderplayer.NetId, (byte)RpcCalls.SetName)
            .Write(senderplayer.Data.NetId)
            .Write(name)
            .EndRpc();
            Nwriter.EndMessage();
            Nwriter.SendMessage();
        }
    }
}