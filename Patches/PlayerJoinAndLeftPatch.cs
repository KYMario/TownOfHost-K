using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.Data;
using AmongUs.GameOptions;
using HarmonyLib;
using InnerNet;

using TownOfHost.Modules;
using TownOfHost.Roles;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Neutral;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
    class OnGameJoinedPatch
    {
        public static void Postfix(AmongUsClient __instance)
        {
            while (!Options.IsLoaded) System.Threading.Tasks.Task.Delay(1);
            if (Main.NormalOptions.NumImpostors == 0 && GameStates.IsOnlineGame)
                Main.NormalOptions.TryCast<NormalGameOptionsV08>().SetInt(Int32OptionNames.NumImpostors, 1);

            ResolutionManager.SetResolution(Screen.width, Screen.height, Screen.fullScreen);
            Logger.Info($"{__instance.GameId}に参加", "OnGameJoined");
            GameStates.IsOutro = false;
            CheckPingPatch.Check = false;
            Main.playerVersion = new Dictionary<byte, PlayerVersion>();
            RPC.RpcVersionCheck();
            SoundManager.Instance.ChangeAmbienceVolume(DataManager.Settings.Audio.AmbienceVolume);
            ChatUpdatePatch.DoBlockChat = false;
            GameStates.InGame = false;
            Main.FeColl = 0;
            GameStates.canmusic = true;
            ErrorText.Instance.Clear();
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                if (pc == null) continue;
                Logger.Info($"FriendCore:{pc.FriendCode},Puid:{pc.GetClient()?.ProductUserId}", "Session");
            }
            if (AmongUsClient.Instance.AmHost) //以下、ホストのみ実行
            {
                if (Main.NormalOptions.KillCooldown == 0f)
                    Main.NormalOptions.KillCooldown = Main.LastKillCooldown.Value;

                AURoleOptions.SetOpt(Main.NormalOptions.Cast<IGameOptions>());
                if (AURoleOptions.ShapeshifterCooldown == 0f)
                    AURoleOptions.ShapeshifterCooldown = Main.LastShapeshifterCooldown.Value;

                Main.NormalOptions.TryCast<NormalGameOptionsV08>().RoleOptions.SetRoleRate(RoleTypes.Scientist, 0, 0);
                Main.NormalOptions.TryCast<NormalGameOptionsV08>().RoleOptions.SetRoleRate(RoleTypes.Engineer, 0, 0);
                Main.NormalOptions.TryCast<NormalGameOptionsV08>().RoleOptions.SetRoleRate(RoleTypes.Tracker, 0, 0);
                Main.NormalOptions.TryCast<NormalGameOptionsV08>().RoleOptions.SetRoleRate(RoleTypes.Noisemaker, 0, 0);
                Main.NormalOptions.TryCast<NormalGameOptionsV08>().RoleOptions.SetRoleRate(RoleTypes.Shapeshifter, 0, 0);
                Main.NormalOptions.TryCast<NormalGameOptionsV08>().RoleOptions.SetRoleRate(RoleTypes.Phantom, 0, 0);
                Main.NormalOptions.roleOptions.TryGetRoleOptions(RoleTypes.GuardianAngel, out GuardianAngelRoleOptionsV08 roleData);
                Main.NormalOptions.TryCast<NormalGameOptionsV08>().SetBool(BoolOptionNames.ConfirmImpostor, false);
                Main.NormalOptions.TryCast<NormalGameOptionsV08>().SetInt(Int32OptionNames.TaskBarMode, 2);
                roleData.ProtectionDurationSeconds = 9999999999;
                foreach (var option in OptionItem.AllOptions)
                {
                    if (Event.OptionLoad.Contains(option.Name) && !Event.Special) option.SetValue(0);
                }
            }
        }
    }
    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.DisconnectInternal))]
    class DisconnectInternalPatch
    {
        public static void Prefix(InnerNetClient __instance, DisconnectReasons reason, string stringReason)
        {
            Logger.Info($"切断(理由:{reason}:{stringReason}, ping:{__instance.Ping},FriendCode:{__instance?.GetClient(__instance.ClientId)?.FriendCode}PUID{__instance?.GetClient(__instance.ClientId)?.ProductUserId})", "Session");

            if (GameStates.IsFreePlay && Main.EditMode)
            {
                CustomSpawnEditor.Save();
                Main.EditMode = false;
            }

            if (AmongUsClient.Instance.AmHost && GameStates.InGame && reason is not DisconnectReasons.Destroy)
            {
                GameManager.Instance.RpcEndGame(GameOverReason.ImpostorDisconnect, false);
                LastGameSave.CreateIfNotExists(oti: true);//落ちでも保存
            }
            Main.AssignSameRoles = false;
            CustomRoleManager.Dispose();
        }
    }
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
    class OnPlayerJoinedPatch
    {
        public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData client)
        {
            Logger.Info($"{client.PlayerName}(ClientID:{client.Id})(FriendCode:{client?.FriendCode ?? "???"})(PuId:{client?.ProductUserId ?? "???"})が参加", "Session");
            if (AmongUsClient.Instance.AmHost && client.FriendCode == "" && Options.KickPlayerFriendCodeNotExist.GetBool() && !GameStates.IsLocalGame && !Main.IsCs() && !BanManager.CheckWhiteList(client?.FriendCode, client?.ProductUserId))
            {
                AmongUsClient.Instance.KickPlayer(client.Id, false);
                Logger.seeingame(string.Format(GetString("Message.KickedByNoFriendCode"), client.PlayerName));
                Logger.Info($"フレンドコードがないプレイヤーを{client?.PlayerName}をキックしました。", "Kick");
                return;
            }
            if (DestroyableSingleton<FriendsListManager>.Instance.IsPlayerBlockedUsername(client.FriendCode) && AmongUsClient.Instance.AmHost)
            {
                AmongUsClient.Instance.KickPlayer(client.Id, true);
                Logger.seeingame($"{client?.PlayerName}({client.FriendCode})はブロック済みのため、BANしました");
                Logger.Info($"ブロック済みのプレイヤー{client?.PlayerName}({client.FriendCode})をBANしました。", "BAN");
                return;
            }
            if (Options.KiclHotNotFriend.GetBool() && !GameStates.IsLocalGame && !Main.IsCs() && !(DestroyableSingleton<FriendsListManager>.Instance.IsPlayerFriend(client.ProductUserId) || client.FriendCode == PlayerControl.LocalPlayer.GetClient().FriendCode || Main.IsCs()) && AmongUsClient.Instance.AmHost && !BanManager.CheckWhiteList(client?.FriendCode, client?.ProductUserId))
            {
                AmongUsClient.Instance.KickPlayer(client.Id, false);
                Logger.seeingame($"{client?.PlayerName}({client.FriendCode})はフレンドではないため、kickしました");
                Logger.Info($"プレイヤー{client?.PlayerName}({client.FriendCode})をKickしました。", "Kick");
                return;
            }
            BanManager.CheckBanPlayer(client);
            if (!BanManager.CheckWhiteList(client?.FriendCode, client?.ProductUserId))
            {
                BanManager.CheckDenyNamePlayer(client);
            }
            RPC.RpcVersionCheck();
            if (AmongUsClient.Instance.AmHost)
            {
                RPC.RpcSyncRoomTimer();
                RPC.SyncYomiage();
            }
        }
    }
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerLeft))]
    class OnPlayerLeftPatch
    {
        static void Prefix([HarmonyArgument(0)] ClientData data)
        {
            if (CustomRoles.Executioner.IsPresent())
                Executioner.ChangeRoleByTarget(data.Character.PlayerId);
        }
        public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData data, [HarmonyArgument(1)] DisconnectReasons reason)
        {
            var isFailure = false;
            if (GameStates.IsLobby) SuddenDeathMode.TeamReset();

            try
            {
                if (data == null)
                {
                    isFailure = true;
                    Logger.Warn("退出者のClientDataがnull", nameof(OnPlayerLeftPatch));
                }
                else if (data.Character == null)
                {
                    isFailure = true;
                    Logger.Warn("退出者のPlayerControlがnull", nameof(OnPlayerLeftPatch));
                }
                else if (data.Character.Data == null)
                {
                    isFailure = true;
                    Logger.Warn("退出者のPlayerInfoがnull", nameof(OnPlayerLeftPatch));
                }
                else
                {
                    if (GameStates.IsInGame)
                    {
                        if (!SelectRolesPatch.Disconnected.Contains(data.Character.PlayerId))
                            SelectRolesPatch.Disconnected.Add(data.Character.PlayerId);

                        Lovers.LoverDisconnected(data.Character);
                        var state = PlayerState.GetByPlayerId(data.Character.PlayerId);
                        if (state.DeathReason == CustomDeathReason.etc) //死因が設定されていなかったら
                        {
                            state.DeathReason = CustomDeathReason.Disconnected;
                            UtilsGameLog.AddGameLog("Disconnected", data.PlayerName + GetString("DeathReason.Disconnected"));
                        }
                        foreach (var role in CustomRoleManager.AllActiveRoles.Values)
                        {
                            role?.OnLeftPlayer(data.Character);
                        }
                        state.SetDead();
                        AntiBlackout.OnDisconnect(data.Character.Data);
                        PlayerGameOptionsSender.RemoveSender(data.Character);
                        PlayerCatch.AllPlayerControls.Do(pc => Camouflage.RpcSetSkin(pc, RevertToDefault: true, kyousei: true));
                        UtilsNotifyRoles.NotifyRoles(NoCache: true);
                    }
                    Croissant.diaries.Remove($"{data.Character.PlayerId}");
                    var diary = Croissant.diaries.Where(x => x.Value.day == data.Character.PlayerId).FirstOrDefault().Value;
                    if (diary != null) diary.day = byte.MaxValue;
                    Main.playerVersion.Remove(data.Character.PlayerId);
                    Logger.Info($"{data?.PlayerName ?? "('ω')"}(ClientID:{data.Id})が切断(理由:{reason}, ping:{AmongUsClient.Instance.Ping}), Platform:{data?.PlatformData?.Platform} , friendcode:{data?.FriendCode ?? "???"} , PuId:{data?.ProductUserId ?? "???"}", "Session");
                }
            }
            catch (Exception e)
            {
                Logger.Warn("切断処理中に例外が発生", nameof(OnPlayerLeftPatch));
                Logger.Exception(e, nameof(OnPlayerLeftPatch));
                isFailure = true;
            }

            if (isFailure)
            {
                Logger.Warn($"正常に完了しなかった切断 - 名前:{(data == null || data.PlayerName == null ? "(不明)" : data.PlayerName)}, 理由:{reason}, ping:{AmongUsClient.Instance.Ping}, Platform:{data?.PlatformData?.Platform ?? Platforms.Unknown} , friendcode:{data?.FriendCode ?? "???"} , PuId:{data?.ProductUserId ?? "???"}", "Session");
                ErrorText.Instance.AddError(AmongUsClient.Instance.GameState is InnerNetClient.GameStates.Started ? ErrorCode.OnPlayerLeftPostfixFailedInGame : ErrorCode.OnPlayerLeftPostfixFailedInLobby);
            }
        }
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CreatePlayer))]
    class CreatePlayerPatch
    {
        public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData client)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                if (client?.Character?.PlayerId == 0)
                    _ = new LateTask(() => CheckPingPatch.Check = true, 10f, "Start Ping Check", true);
                OptionItem.SyncAllOptions();

                _ = new LateTask(() =>
                {
                    if (client.Character == null) return;
                    TemplateManager.SendTemplate("welcome", client.Character.PlayerId, true);

                    var varsion = Main.PluginShowVersion;
                    var text = $"<size=80%>この部屋では\n<color={Main.ModColor}><size=180%><u><b>{Main.ModName}</color></b></size> v.{varsion}</u>\nを導入しております。<size=40%>\n\n</size>現在AmongUsでは、<color=#fc8803>公開ルームでのMod利用はできません</color><size=40%>\n\n";
                    var text2 = "</size><color=red>公開ルームからMod部屋へ勧誘/誘導をするのは<b>禁止</b>です</color>。<size=40%>\n<color=red>勧誘/誘導行為</color>にあった場合はスクリーンショット等と一緒に開発者にお知らせください。<color=red>Mod使えなくします</color>。";
                    var text3 = "</size>\n<size=60%>\n☆参加型配信を行ったり、オープンチャットやTwitterで募集するのは?\n<size=50%>→<color=#352ac9>全然大丈夫です!!やっちゃってください!!</color>\n　<color=#fc8803>バニラAmongUsの公開ルーム</color>での<color=red>宣伝/勧誘/誘導</color>がダメなのです!!</size>";
                    var text4 = "\n☆開発者から許可貰ってるって言ってる?　　\n<size=50%>→<color=#c9145a>個々で許可を出しておりません</color>!!大噓つきですよ!!</size>\n☆公開ルームに参加し、コード宣伝して「来てね～」って言うのは?\n<size=50%>→<color=red>勧誘/誘導</color>に当たるのでダメです。迷惑考えてくださいよ!!</size>";
                    Utils.SendMessage($"{text}{text2}{text3}{text4}", client.Character.PlayerId, $"<color={Main.ModColor}>【This Room Use \"Town Of Host-K\"】");

                    if (Options.AutoDisplayLastResult.GetBool() && PlayerState.AllPlayerStates.Count != 0 && Main.clientIdList.Contains(client.Id))
                    {
                        if (!AmongUsClient.Instance.IsGameStarted && client.Character != null && !Main.AssignSameRoles)
                        {
                            UtilsGameLog.ShowLastResult(client.Character.PlayerId);
                        }
                    }
                    if (Options.AutoDisplayKillLog.GetBool() && PlayerState.AllPlayerStates.Count != 0 && Main.clientIdList.Contains(client.Id))
                    {
                        if (!GameStates.IsInGame && client.Character != null)
                        {
                            UtilsGameLog.ShowKillLog(client.Character.PlayerId);
                        }
                    }
                    if (Main.DebugVersion)
                    {
                        var kigen = "";
                        /* Debugversion
                        if (!Main.NotKigenDebug)
                            kigen = $"\n\n・このデバッグ版の有効期限⇒{Main.DebugvalidityYear}年{Main.DebugvalidityMonth}月{Main.DebugvalidityDay}日";
                        */
                        Utils.SendMessage($"<size=120%>☆これはデバッグ版です☆</size>\n<line-height=80%><size=80%>\n・正式リリース版ではありません。\n・バグが発生する場合があります。\nバグが発生した場合はDiscordで報告すること!{kigen}", client.Character.PlayerId, "<color=red>【=====　これはデバッグ版です　=====】</color>");
                    }
                }, 3.0f, "Welcome Meg");
            }
        }
    }
    //[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.Update))]
    class CheckPingPatch
    {
        public static bool Check;
        /*public static void Postfix(InnerNetClient __instance)
        {
            if (Check)
                if (__instance.Ping >= 750)
                {
                    Logger.Warn($"接続が不安定", "Ping");
                    ErrorText.Instance.AddError(ErrorCode.CommunicationisUnstable);
                }
        }*/
    }
}
