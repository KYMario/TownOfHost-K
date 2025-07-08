using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using AmongUs.GameOptions;
using Hazel;

using TownOfHost.Attributes;
using TownOfHost.Modules;
using TownOfHost.Roles.AddOns.Common;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Modules.MeetingVoteManager;

namespace TownOfHost
{
    public static class AntiBlackout
    {
        public static bool IsCached { get; private set; } = false;
        public static bool IsSet { get; private set; } = false;
        public static bool Iswaitsend { get; private set; } = false;
        public static Dictionary<byte, (bool isDead, bool Disconnected)> isDeadCache = new();
        public static List<byte> isRoleCache = new();
        public static VoteResult? voteresult;
        //private static Dictionary<(byte, byte), RoleTypes> RoleTypeCache = new();
        private readonly static LogHandler logger = Logger.Handler("AntiBlackout");

        private static bool GetA()
        {
            foreach (var (role, info) in CustomRoleManager.AllRolesInfo)
                if (info.IsEnable && info.CountType is not CountTypes.Crew and not CountTypes.Impostor)
                    return true;
            return false;
        }

        ///<summary>
        ///追放処理を上書きするかどうか
        ///</summary>
        public static bool OverrideExiledPlayer()
        {
            if (4 <= PlayerCatch.AllPlayerControls.Count()) return false;
            if (ModClientOnly is true) return false;
            return (Options.NoGameEnd.GetBool() || GetA()) && (Main.DebugAntiblackout || !DebugModeManager.EnableDebugMode.GetBool());
        }

        private static bool ModClientOnly//全員ModClient ↓これじゃダメなの?()
            => PlayerCatch.AllPlayerControls.All(pc => pc.IsModClient());

        public static void SetIsDead(bool doSend = true, [CallerMemberName] string callerMethodName = "")
        {
            logger.Info($"SetIsDead is called from {callerMethodName}");
            if (IsCached)
            {
                logger.Info("再度SetIsDeadを実行する前に、RestoreIsDeadを実行してください。");
                return;
            }
            isDeadCache.Clear();
            var nowcount = PlayerCatch.AllPlayerControls.Count();
            foreach (var info in GameData.Instance.AllPlayers)
            {
                isDeadCache[info.PlayerId] = (info.IsDead, info.Disconnected);
                //情報が無い　　　   4人以上正常者がいる場合は役職変えるので回線切断者を生存擬装する必要が多分ない。
                if (info == null || ((info?.Disconnected == true) && 4 <= nowcount)) continue;
                info.IsDead = false;
                info.Disconnected = false;
            }
            IsCached = true;
            if (doSend) SendGameData();
        }
        public static void RestoreIsDead(bool doSend = true, [CallerMemberName] string callerMethodName = "")
        {
            logger.Info($"RestoreIsDead is called from {callerMethodName}");
            foreach (var info in GameData.Instance.AllPlayers)
            {
                if (info == null) continue;
                if (isDeadCache.TryGetValue(info.PlayerId, out var val))
                {
                    info.IsDead = val.isDead;
                    info.Disconnected = val.Disconnected;
                }
            }
            isDeadCache.Clear();
            IsCached = false;
            IsSet = false;
            if (doSend) SendGameData();
        }

        public static void SendGameData([CallerMemberName] string callerMethodName = "")
        {
            logger.Info($"SendGameData is called from {callerMethodName}");
            MeetingHudPatch.StartPatch.Serialize = true;
            foreach (var playerinfo in GameData.Instance.AllPlayers)
            {
                MessageWriter writer = MessageWriter.Get(SendOption.Reliable);
                // 書き込み {}は読みやすさのためです。
                writer.StartMessage(5); //0x05 GameData
                {
                    writer.Write(AmongUsClient.Instance.GameId);
                    writer.StartMessage(1); //0x01 Data
                    {
                        writer.WritePacked(playerinfo.NetId);
                        playerinfo.Serialize(writer, true);
                    }
                    writer.EndMessage();
                }
                writer.EndMessage();

                AmongUsClient.Instance.SendOrDisconnect(writer);
                writer.Recycle();
            }
            MeetingHudPatch.StartPatch.Serialize = false;
        }
        public static void OnDisconnect(NetworkedPlayerInfo player)
        {
            // 実行条件: クライアントがホストである, IsDeadが上書きされている, playerが切断済み
            if (!AmongUsClient.Instance.AmHost || !IsCached || !player.Disconnected) return;
            isDeadCache[player.PlayerId] = (true, true);
            player.IsDead = player.Disconnected = false;
            SendGameData();
        }
        public static void SetRole(VoteResult? result = null)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                RoleTypes? HostRole = PlayerControl.LocalPlayer.Data.RoleType;
                isRoleCache.Clear();
                IsSet = true;
                Iswaitsend = true;
                var ImpostorId = PlayerControl.LocalPlayer.PlayerId;
                if (result?.Exiled?.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                {
                    byte[] DontImpostrTargetIds = [PlayerControl.LocalPlayer.PlayerId, (result?.Exiled?.PlayerId ?? byte.MaxValue)];
                    var impostortarget = PlayerCatch.AllAlivePlayerControls.Where(pc => !DontImpostrTargetIds.Contains(pc.PlayerId)).FirstOrDefault();
                    ImpostorId = impostortarget == null ?
                                PlayerCatch.AllPlayerControls?.FirstOrDefault()?.PlayerId ?? PlayerControl.LocalPlayer.PlayerId : impostortarget.PlayerId;
                    HostRole = null;
                }
                bool check = false;
                foreach (var player in PlayerCatch.AllPlayerControls)
                {
                    AntiBlackout.isRoleCache.Add(player.PlayerId);
                }
                MeetingHudPatch.StartPatch.Serialize = true;
                var sender = CustomRpcSender.Create("AntiBlackoutSetRole", SendOption.Reliable);
                sender.StartMessage();
                foreach (var target in GameData.Instance.AllPlayers)
                {
                    if (target == null) continue;
                    uint? netid = PlayerCatch.AllPlayerNetId.TryGetValue(target.PlayerId, out var id) ? id : (target?._object?.NetId ?? null);

                    if (netid.HasValue is false) continue;
                    RoleTypes setrole = target.PlayerId == ImpostorId ? RoleTypes.Impostor : RoleTypes.Crewmate;

                    if (sender.stream.Length > 750 && Options.ExRpcWeightR.GetBool())
                    {
                        sender.EndMessage();
                        sender.SendMessage();
                        sender = CustomRpcSender.Create("AntiBlackoutSetRole+2", SendOption.Reliable);
                        sender.StartMessage();
                    }
                    if (setrole is RoleTypes.Impostor)
                    {
                        target.IsDead = false;
                        sender.Write((wit) =>
                        {
                            wit.StartMessage(1); //0x01 Data
                            {
                                wit.WritePacked(target.NetId);
                                target.Serialize(wit, false);
                            }
                            wit.EndMessage();
                        }, true);
                    }
                    sender.StartRpc(netid.Value, RpcCalls.SetRole)
                    .Write((ushort)setrole)
                    .Write(true)
                    .EndRpc();

                    if (check is false)
                    {
                        Logger.Info($"{target.GetLogPlayerName()} => {setrole}", "AntiBlackout");
                    }
                }
                check = true;
                sender.EndMessage();
                sender.SendMessage();
                MeetingHudPatch.StartPatch.Serialize = false;
                //}
            }
        }

        public static void ResetSetRole(PlayerControl Player)
        {
            if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Default && !Main.DontGameSet) return;
            if (Player) isRoleCache.Remove(Player.PlayerId);
            if (Player.GetClient() is null)
            {
                Logger.Error($"{Player?.Data?.PlayerName ?? "???"}のclientがnull", "ExiledSetRole");
                return;
            }
            if (Iswaitsend)
            {
                Iswaitsend = false;
                Main.CanUseAbility = true;
                //個々視点のみになってるっぽい。会議時とかそういう場で相互性が取れなくなる。
                //1000msとか行ったら暗転するけどそこまで考えるのは...
                //_ = new LateTask(() => SendGameData(), 1f, "SetAllPlayerData", true);
            }
            if (Player != PlayerControl.LocalPlayer)
            {
                var sender = CustomRpcSender.Create("ExiledSetRole", Hazel.SendOption.Reliable);
                sender.StartMessage(Player.GetClientId());
                foreach (var pc in PlayerCatch.AllPlayerControls)
                {
                    if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId && Options.EnableGM.GetBool())
                    {
                        sender.StartRpc(pc.NetId, RpcCalls.SetRole)
                        .Write((ushort)RoleTypes.Crewmate)
                        .Write(true)
                        .EndRpc();
                        continue;
                    }
                    var customrole = pc.GetCustomRole();
                    var roleinfo = customrole.GetRoleInfo();
                    var role = roleinfo?.BaseRoleType.Invoke() ?? RoleTypes.Scientist;
                    var isalive = pc.IsAlive();
                    if (customrole is CustomRoles.SKMadmate)
                    {
                        role = Options.SkMadCanUseVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate;
                    }
                    if (!isalive)
                    {
                        role = customrole.IsImpostor() || ((pc.GetRoleClass() as IKiller)?.CanUseSabotageButton() ?? false) ?
                                RoleTypes.ImpostorGhost : RoleTypes.CrewmateGhost;
                    }

                    if (Player != pc && (roleinfo?.IsDesyncImpostor ?? false))
                        role = !isalive ? RoleTypes.CrewmateGhost : RoleTypes.Crewmate;

                    var IDesycImpostor = Player.GetCustomRole().GetRoleInfo()?.IsDesyncImpostor ?? false;
                    IDesycImpostor |= SuddenDeathMode.NowSuddenDeathMode;

                    if (pc.Is(CustomRoles.Amnesia))
                    {
                        if (roleinfo?.IsDesyncImpostor == true && !pc.Is(CustomRoleTypes.Impostor))
                            role = RoleTypes.Crewmate;
                        if (Amnesia.dontcanUseability)
                        {
                            role = pc.Is(CustomRoleTypes.Impostor) ? RoleTypes.Impostor : RoleTypes.Crewmate;
                        }
                    }
                    var setrole = (IDesycImpostor && Player != pc) ? (!isalive ? RoleTypes.CrewmateGhost : RoleTypes.Crewmate) : role;

                    if (pc.PlayerId == Player.PlayerId && pc.GetRoleClass()?.AfterMeetingRole is not null && pc.IsAlive())
                    {
                        setrole = pc.GetRoleClass().AfterMeetingRole.Value;
                    }
                    if ((pc.GetRoleClass() as IUsePhantomButton)?.IsPhantomRole is false && setrole is RoleTypes.Phantom && pc.IsAlive())
                    {
                        //使えないならインポスターに戻す
                        setrole = RoleTypes.Impostor;
                    }

                    sender.StartRpc(pc.NetId, RpcCalls.SetRole)
                    .Write((ushort)setrole)
                    .Write(true)
                    .EndRpc();
                }
                sender.EndMessage();
                sender.SendMessage();
                Player.Revive();
            }

            Player.ResetKillCooldown();
            Player.PlayerId.GetPlayerState().IsBlackOut = false;
            Player.SyncSettings();
            _ = new LateTask(() =>
                {
                    Player.SetKillCooldown(kyousei: true, delay: true);
                    if (Player.IsAlive() && !(Player.PlayerId == PlayerControl.LocalPlayer.PlayerId && Options.EnableGM.GetBool()))
                    {
                        var roleclass = Player.GetRoleClass();
                        (roleclass as IUsePhantomButton)?.Init(Player);
                    }
                    else
                    {
                        Player.RpcExileV2();
                        if (Player.IsGhostRole()) Player.RpcSetRole(RoleTypes.GuardianAngel, true);
                    }
                }, Main.LagTime, "Re-SetRole", true);

            {
                Twins.TwinsSuicide(true);
                if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Default) return;
                Player.RpcResetAbilityCooldown();
                UtilsNotifyRoles.NotifyRoles(true, true, SpecifySeer: Player);
            }
        }

        ///<summary>
        ///一時的にIsDeadを本来のものに戻した状態でコードを実行します
        ///<param name="action">実行内容</param>
        ///</summary>
        public static void TempRestore(Action action)
        {
            logger.Info("==Temp Restore==");
            //IsDeadが上書きされた状態でTempRestoreが実行されたかどうか
            bool before_IsCached = IsCached;
            try
            {
                if (before_IsCached) RestoreIsDead(doSend: false);
                action();
            }
            catch (Exception ex)
            {
                logger.Warn("AntiBlackout.TempRestore内で例外が発生しました");
                logger.Exception(ex);
            }
            finally
            {
                if (before_IsCached) SetIsDead(doSend: false);
                logger.Info("==/Temp Restore==");
            }
        }

        [GameModuleInitializer]
        public static void Reset()
        {
            logger.Info("==Reset==");
            if (isDeadCache == null) isDeadCache = new();
            if (isRoleCache == null) isRoleCache = new();
            isRoleCache.Clear();
            isDeadCache.Clear();
            //RoleTypeCache.Clear();
            voteresult = null;
            IsCached = false;
            Iswaitsend = false;
            IsSet = false;
        }
    }
}
