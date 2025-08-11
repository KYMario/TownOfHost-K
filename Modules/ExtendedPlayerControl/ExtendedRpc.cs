using System.Linq;
using System.Collections.Generic;
using Hazel;
using InnerNet;
using HarmonyLib;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.ExtendedPlayerControl;

using static TownOfHost.Translator;

namespace TownOfHost
{
    static class ExtendedRpc
    {

        /// <summary>
        /// 役職変える奴。
        /// </summary>
        /// <param name="player">対象者</param>
        /// <param name="role">変更する役職</param>
        /// <param name="setRole">基本trueにしよう()</param>
        /// <param name="log">true→役職上書き null→役職変更表示 false→なんもしない</param>
        public static void RpcSetCustomRole(this PlayerControl player, CustomRoles role, bool setRole = true, bool? log = false)
        {
            if (player.GetCustomRole() == role) return;
            var roleClass = player.GetRoleClass();
            var roleInfo = role.GetRoleInfo();
            if (role < CustomRoles.NotAssigned)
            {
                if (player.PlayerId == PlayerControl.LocalPlayer.PlayerId && GameStates.AfterIntro) Main.showkillbutton = false;

                if (roleClass != null)
                {
                    roleClass.Dispose();
                    CustomRoleManager.CreateInstance(role, player);
                }
                PlayerState.GetByPlayerId(player.PlayerId).SetMainRole(role);

                if (log == true) UtilsGameLog.LastLogRole[player.PlayerId] = "<b> " + Utils.ColorString(UtilsRoleText.GetRoleColor(role), GetString($"{role}")) + "</b>";
                else if (log == null) UtilsGameLog.LastLogRole[player.PlayerId] = $"<size=40%>{UtilsGameLog.LastLogRole[player.PlayerId].RemoveSizeTags()}</size><b>=> " + Utils.ColorString(UtilsRoleText.GetRoleColor(role), GetString($"{role}")) + "</b>";

                if (!SuddenDeathMode.NowSuddenDeathMode) NameColorManager.RemoveAll(player.PlayerId);

                //マッドメイトの最初からの内通
                if (role.IsMadmate() && Options.MadCanSeeImpostor.GetBool())
                {
                    if (PlayerCatch.AllPlayerFirstTypes.Where(x => x.Value is CustomRoleTypes.Impostor).Any())
                        foreach (var imp in PlayerCatch.AllPlayerFirstTypes.Where(x => x.Value is CustomRoleTypes.Impostor))
                        {
                            var iste = PlayerState.GetByPlayerId(imp.Key);
                            if (iste.TargetColorData.ContainsKey(player.PlayerId)) NameColorManager.Remove(player.PlayerId, imp.Key);
                            NameColorManager.Add(player.PlayerId, imp.Key, "ff1919");
                        }
                }
            }
            else if (role >= CustomRoles.NotAssigned)   //500:NoSubRole 501~:SubRole
            {
                if (role.IsGhostRole()) PlayerState.GetByPlayerId(player.PlayerId).SetGhostRole(role);
                else PlayerState.GetByPlayerId(player.PlayerId).SetSubRole(role);
            }
            if (AmongUsClient.Instance.AmHost)
            {
                if (role < CustomRoles.NotAssigned)
                {
                    if (setRole)
                    {
                        //タスクターン or ミーティング終了前ならその時点で役職変更処理を共有
                        if (GameStates.task || GameStates.CalledMeeting)
                        {
                            SetRole();
                        }
                        else
                        {
                            // 違うなら暗転対策が動作している状態なのであれば役職変更したら不味い。
                            new LateTask(() =>
                            {
                                SetRole();
                            }, 10, "ExSetRole");
                        }
                    }
                }

                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCustomRole, SendOption.Reliable, -1);
                writer.Write(player.PlayerId);
                writer.WritePacked((int)role);
                AmongUsClient.Instance.FinishRpcImmediately(writer);

                if (role.IsGhostRole() || role < CustomRoles.NotAssigned)
                {
                    player.SetKillCooldown(delay: true, force: true);
                    player.RpcResetAbilityCooldown();
                    UtilsNotifyRoles.NotifyRoles(ForceLoop: true);
                    (roleClass as IUsePhantomButton)?.Init(player);
                    foreach (var roleclass in CustomRoleManager.AllActiveRoles.Values)
                    {
                        roleclass.ChangeColor();
                    }
                }
                else
                {
                    player.SyncSettings();
                }
            }
            if (player.PlayerId == PlayerControl.LocalPlayer.PlayerId && GameStates.AfterIntro && role < CustomRoles.NotAssigned)
            {
                CustomButtonHud.BottonHud(true);
                _ = new LateTask(() => Main.showkillbutton = true, 0.02f, "", true);
            }

            void SetRole()
            {
                //会議中なら処理しない
                if (GameStates.CalledMeeting) return;

                if (roleInfo?.IsDesyncImpostor ?? false || SuddenDeathMode.NowSuddenDeathMode)
                {
                    var subSenders = new List<CustomRpcSender>();
                    var sender = CustomRpcSender.Create("SetCustomRole", SendOption.Reliable);
                    sender.StartMessage(player.GetClientId());
                    foreach (var pc in PlayerCatch.AllPlayerControls)
                    {
                        if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                        {
                            player.StartCoroutine(player.CoSetRole(RoleTypes.Crewmate, Main.SetRoleOverride));
                            if (player != pc)
                            {
                                sender.AutoStartRpc(pc.NetId, RpcCalls.SetRole, player.GetClientId())
                                .Write((ushort)(pc.IsAlive() ? RoleTypes.Scientist : RoleTypes.CrewmateGhost))
                                .Write(true)
                                .EndRpc();
                            }
                        }
                        else
                        {
                            var subSender = CustomRpcSender.Create("SetCustomRole Sub", SendOption.Reliable);
                            subSender.StartMessage(pc.GetClientId())
                            .AutoStartRpc(player.NetId, RpcCalls.SetRole, pc.GetClientId())
                            .Write((ushort)(pc.PlayerId == player.PlayerId ? role.GetRoleTypes() : RoleTypes.Crewmate))
                            .Write(true)
                            .EndRpc()
                            .EndMessage();
                            subSenders.Add(subSender);
                            if (player != pc)
                            {
                                sender.AutoStartRpc(pc.NetId, RpcCalls.SetRole, player.GetClientId())
                                .Write((ushort)(pc.IsAlive() ? RoleTypes.Scientist : RoleTypes.CrewmateGhost))
                                .Write(true)
                                .EndRpc();
                            }
                        }
                    }
                    sender.EndMessage();
                    sender.SendMessage();
                    subSenders.Do(x => x.SendMessage());
                    if (player.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                    {
                        if ((roleInfo?.IsDesyncImpostor ?? false || SuddenDeathMode.NowSuddenDeathMode) && roleInfo?.BaseRoleType.Invoke() != RoleTypes.Impostor)
                            RoleManager.Instance.SetRole(PlayerControl.LocalPlayer, roleInfo?.BaseRoleType.Invoke() ?? RoleTypes.Crewmate);
                    }
                }
                else
                {
                    player.RpcSetRole(role.GetRoleTypes(), Main.SetRoleOverride);
                }
                if ((roleInfo?.IsCantSeeTeammates == true || player.Is(CustomRoles.OneWolf)) && role.IsImpostor() && !SuddenDeathMode.NowSuddenDeathMode)
                {
                    var clientId = player.GetClientId();
                    foreach (var killer in PlayerCatch.AllPlayerControls)
                    {
                        if (!killer.GetCustomRole().IsImpostor()) continue;
                        //Amnesiac視点インポスターをクルーにする
                        killer.RpcSetRoleDesync(RoleTypes.Scientist, clientId, Hazel.SendOption.None);
                    }
                }
                foreach (var pc in PlayerCatch.AllPlayerControls)
                {
                    if (pc == null) continue;
                    if (pc.IsAlive()) continue;

                    pc.RpcExileV2();
                }
            }
        }
        public static void RpcSetCustomRole(byte PlayerId, CustomRoles role)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCustomRole, SendOption.Reliable, -1);
                writer.Write(PlayerId);
                writer.WritePacked((int)role);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
        }

        public static void RpcExile(this PlayerControl player)
        {
            RPC.ExileAsync(player);
        }
        public static void RpcSetNameEx(this PlayerControl player, string name)
        {
            foreach (var seer in PlayerCatch.AllPlayerControls)
            {
                Main.LastNotifyNames[(player.PlayerId, seer.PlayerId)] = name;
            }
            HudManagerPatch.LastSetNameDesyncCount++;

            Logger.Info($"Set:{player?.Data?.GetLogPlayerName()}:{name} for All", "RpcSetNameEx");
            player.RpcSetName(name);
        }
        public static void RpcSetNamePrivate(this PlayerControl player, string name, bool DontShowOnModdedClient = false, PlayerControl seer = null, bool force = false)
        {
            //player: 名前の変更対象
            //seer: 上の変更を確認することができるプレイヤー
            if (player == null || name == null || !AmongUsClient.Instance.AmHost) return;
            if (seer == null) seer = player;

            if (!player.SetNameCheck(name, seer, force)) return;

            var clientId = seer.GetClientId();
            if (clientId == -1) return;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.SetName, Hazel.SendOption.None, clientId);
            writer.Write(player.Data.NetId);
            writer.Write(name);
            writer.Write(DontShowOnModdedClient);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void RpcSetRoleDesync(this PlayerControl player, RoleTypes role, int clientId, SendOption sendoption = SendOption.Reliable)
        {
            if (clientId == -1)
            {
                Logger.Error($"clientIdが-1!", "RpcSetRoleDesync");
                return;
            }
            //player: ロールの変更対象

            if (player == null) return;

            var pc = AmongUsClient.Instance.allClients.ToArray().Where(x => x.Id == clientId).FirstOrDefault();

            //Logger.Info($"({pc?.PlayerName ?? "???"}){player?.Data?.GetLogPlayerName() ?? "( ᐛ )"} =>  {role}", "RpcSetRoleDesync");

            if (AmongUsClient.Instance.ClientId == clientId)
            {
                player.StartCoroutine(player.CoSetRole(role, Main.SetRoleOverride && Options.CurrentGameMode == CustomGameMode.Standard));
                return;
            }
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.SetRole, sendoption, clientId);
            writer.Write((ushort)role);
            writer.Write(Main.SetRoleOverride && Options.CurrentGameMode == CustomGameMode.Standard);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void RpcMeetingKill(this PlayerControl killer, PlayerControl target = null)
        {
            if (!GameStates.InGame) return;
            if (target == null) target = killer;

            MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.MurderPlayer, SendOption.None, target.GetClientId());
            messageWriter.WriteNetObject(target);
            messageWriter.Write((int)MurderResultFlags.Succeeded);
            AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        }
        public static void RpcSpecificMurderPlayer(this PlayerControl killer, PlayerControl target = null)
        {
            if (!GameStates.InGame) return;
            if (target == null) target = killer;
            if (killer.AmOwner)
            {
                killer.MurderPlayer(target, MurderResultFlags.Succeeded);
            }
            else
            {
                MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.MurderPlayer, SendOption.None, killer.GetClientId());
                messageWriter.WriteNetObject(target);
                messageWriter.Write((int)MurderResultFlags.Succeeded);
                AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
            }
        }
        public static void RpcResetAbilityCooldownAllPlayer(bool Sync = true)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Default) return;
            if (Sync) UtilsOption.SyncAllSettings();
            _ = new LateTask(() =>
            {
                foreach (var target in PlayerCatch.AllPlayerControls)
                {
                    target.RpcResetAbilityCooldown();
                }
                Main.CanUseAbility = true;
            }
            , 0.2f, "AllPlayerResetAbilityCoolDown", null);
        }
        public static void RpcResetAbilityCooldown(this PlayerControl target, bool log = true, bool Sync = false)
        {
            if (target == null || !AmongUsClient.Instance.AmHost)
            {
                return;
            }
            if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Default) return;
            if (Sync) target.SyncSettings();
            if (log) Logger.Info($"アビリティクールダウンのリセット:{target?.name ?? "ﾇﾙﾎﾟｯ"}({target?.PlayerId ?? 334})", "RpcResetAbilityCooldown");

            if (Sync)
            {
                _ = new LateTask(() =>
                {
                    if (PlayerControl.LocalPlayer == target)
                    {
                        //targetがホストだった場合
                        PlayerControl.LocalPlayer?.Data?.Role?.SetCooldown();
                    }
                    else
                    {
                        //targetがホスト以外だった場合
                        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(target.NetId, (byte)RpcCalls.ProtectPlayer, SendOption.None, target.GetClientId());
                        writer.WriteNetObject(target);
                        writer.Write(0);
                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                    }
                }, Main.LagTime, "abilityrset", null);
            }
            else
            {
                if (PlayerControl.LocalPlayer == target)
                {
                    //targetがホストだった場合
                    PlayerControl.LocalPlayer?.Data?.Role?.SetCooldown();
                }
                else
                {
                    //targetがホスト以外だった場合
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(target.NetId, (byte)RpcCalls.ProtectPlayer, SendOption.None, target.GetClientId());
                    writer.WriteNetObject(target);
                    writer.Write(0);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                }
            }
            /*
                プレイヤーがバリアを張ったとき、そのプレイヤーの役職に関わらずアビリティーのクールダウンがリセットされます。
                ログの追加により無にバリアを張ることができなくなったため、代わりに自身に0秒バリアを張るように変更しました。
                この変更により、役職としての守護天使が無効化されます。
                ホストのクールダウンは直接リセットします。
            */
        }
        public static void RpcSpecificShapeshift(this PlayerControl player, PlayerControl target, bool shouldAnimate)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (player.PlayerId == 0)
            {
                player.Shapeshift(target, shouldAnimate);
                return;
            }
            MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.Shapeshift, SendOption.None, player.GetClientId());
            messageWriter.WriteNetObject(target);
            messageWriter.Write(shouldAnimate);
            AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        }
        public static void RpcSpecificRejectShapeshift(this PlayerControl player, PlayerControl target, bool shouldAnimate)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            foreach (var seer in PlayerCatch.AllPlayerControls)
            {
                if (seer != player)
                {
                    MessageWriter msg = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.RejectShapeshift, SendOption.None, seer.GetClientId());
                    AmongUsClient.Instance.FinishRpcImmediately(msg);
                }
                else
                {
                    player.RpcSpecificShapeshift(target, shouldAnimate);
                }
            }
        }
        public static void RpcDesyncUpdateSystem(this PlayerControl target, SystemTypes systemType, int amount)
        {
            if (target == null) return;
            MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.UpdateSystem, SendOption.None, target.GetClientId());
            messageWriter.Write((byte)systemType);
            messageWriter.WriteNetObject(target);
            messageWriter.Write((byte)amount);
            AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        }

        public static void RpcDesyncUpdateSystem(this PlayerControl target, SystemTypes systemType, MessageWriter msgWriter)
        {
            MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.UpdateSystem, SendOption.None, target.GetClientId());
            messageWriter.Write((byte)systemType);
            messageWriter.WriteNetObject(PlayerControl.LocalPlayer);
            messageWriter.Write(msgWriter, false);
            AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        }
        public static void RpcExileV2(this PlayerControl player)
        {
            if (player == null) return;
            player.Exiled();
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.Exiled, SendOption.None, -1);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void MurderPlayer(this PlayerControl killer, PlayerControl target)
        {
            killer.MurderPlayer(target, SucceededFlags);
        }
        public static void RpcMurderPlayer(this PlayerControl killer, PlayerControl target)
        {
            killer.RpcMurderPlayer(target, true);
        }
        public static void RpcMurderPlayerV2(this PlayerControl killer, PlayerControl target)
        {
            if (target == null) target = killer;
            if (AmongUsClient.Instance.AmClient)
            {
                killer.MurderPlayer(target, SuccessFlags);
            }
            MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.MurderPlayer, SendOption.None, -1);
            messageWriter.WriteNetObject(target);
            messageWriter.Write((int)SuccessFlags);
            AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
            UtilsNotifyRoles.NotifyRoles();
        }
        public static void RpcProtectedMurderPlayer(this PlayerControl killer, PlayerControl target = null)
        {
            //killerが死んでいる場合は実行しない
            if (!killer.IsAlive()) return;

            if (target == null) target = killer;
            // Host
            if (killer.AmOwner)
            {
                killer.MurderPlayer(target, MurderResultFlags.FailedProtected);
            }
            // Other Clients
            if (killer.PlayerId != 0)
            {
                var writer = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.MurderPlayer, SendOption.None, killer.GetClientId());
                writer.WriteNetObject(target);
                writer.Write((int)MurderResultFlags.FailedProtected);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
        }
        public static void RpcProtectedMurderPlayer(this PlayerControl killer, PlayerControl seer, PlayerControl target = null)
        {
            if (seer == null) return;
            //killerが死んでいる場合は実行しない
            if (!killer.IsAlive()) return;

            if (target == null) target = killer;
            // Host
            if (killer.AmOwner)
            {
                killer.MurderPlayer(target, MurderResultFlags.FailedProtected);
            }
            // Other Clients
            if (killer.PlayerId != 0)
            {
                var writer = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.MurderPlayer, SendOption.None, seer.GetClientId());
                writer.WriteNetObject(target);
                writer.Write((int)MurderResultFlags.FailedProtected);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
        }
        public static void RpcSnapToForced(this PlayerControl pc, Vector2 position, SendOption sendOption = SendOption.None)
        {
            var netTransform = pc.NetTransform;
            if (AmongUsClient.Instance.AmClient)
            {
                netTransform.SnapTo(position, (ushort)(netTransform.lastSequenceId + 128));
            }
            ushort newSid = (ushort)(netTransform.lastSequenceId + 2);
            MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(netTransform.NetId, (byte)RpcCalls.SnapTo, sendOption);
            NetHelpers.WriteVector2(position, messageWriter);
            messageWriter.Write(newSid);
            AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        }
        public static void RpcSnapToDesync(this PlayerControl pc, PlayerControl seer, Vector2 position)
        {
            var net = pc.NetTransform;
            var num = (ushort)(net.lastSequenceId + 2);
            MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(net.NetId, (byte)RpcCalls.SnapTo, SendOption.None, seer.GetClientId());
            NetHelpers.WriteVector2(position, messageWriter);
            messageWriter.Write(num);
            AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        }
        /// <summary>
        /// 自身の色を特定の人視点のみ変更する。<br/>
        /// 前→変更対象者<br/>
        /// target→視認対象者<br/>
        /// </summary>
        /// <param name="seer">見せる相手</param>
        /// <param name="Color">色</param>
        public static void RpcChColor(this PlayerControl pc, PlayerControl seer, byte Color, bool NonSkin = false)
        {
            if (Options.firstturnmeeting && MeetingStates.FirstMeeting) return;
            if (GameStates.IsLobby) return;

            if (seer.PlayerId == PlayerControl.LocalPlayer.PlayerId)
            {
                pc.SetColor(Color);
                if (NonSkin)
                {
                    pc.SetSkin("", Color);
                    pc.SetHat("", Color);
                    pc.SetVisor("", Color);
                    return;
                }
            }
            var sender = CustomRpcSender.Create("DChengeColor", SendOption.Reliable);
            sender.StartMessage(seer.GetClientId());

            sender.StartRpc(pc.NetId, RpcCalls.SetColor)
            .Write(pc.NetId)
            .Write(Color)
            .EndRpc();

            if (NonSkin)
            {
                sender.StartRpc(pc.NetId, RpcCalls.SetHatStr)
                .Write("")
                .Write(pc.GetNextRpcSequenceId(RpcCalls.SetHatStr))
                .EndRpc();
                sender.StartRpc(pc.NetId, RpcCalls.SetSkinStr)
                .Write("")
                .Write(pc.GetNextRpcSequenceId(RpcCalls.SetSkinStr))
                .EndRpc();
                sender.StartRpc(pc.NetId, RpcCalls.SetVisorStr)
                .Write("")
                .Write(pc.GetNextRpcSequenceId(RpcCalls.SetVisorStr))
                .EndRpc();
            }
            sender.EndMessage();
            sender.SendMessage();
        }
        public static void OnlySeeMyPet(this PlayerControl pc, string petid = null)
        {
            petid ??= Camouflage.PlayerSkins.TryGetValue(pc.PlayerId, out var outfit) ? outfit.PetId : "";
            if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Default) return;
            //pc.RpcSetPet("");

            if (Options.ExRpcWeightR.GetBool()) return;

            foreach (var ap in PlayerCatch.AllPlayerControls)
            {
                var petId = "";
                if (ap.GetClient() == null) continue;
                if (pc.PlayerId == ap.PlayerId && pc.IsAlive()) petId = petid;

                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(pc.NetId, (byte)RpcCalls.SetPetStr, SendOption.None, ap.GetClientId());
                writer.Write(petId);
                writer.Write(pc.GetNextRpcSequenceId(RpcCalls.SetPetStr));
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
            /*
            if (pc.IsAlive() && pc.GetClient() != null)
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(pc.NetId, (byte)RpcCalls.SetPetStr, SendOption.None, pc.GetClientId());
                writer.Write(petid);
                writer.Write(pc.GetNextRpcSequenceId(RpcCalls.SetPetStr));
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }*/

            pc.RawSetPet(pc.PlayerId == PlayerControl.LocalPlayer.PlayerId ? petid : "pet_EmptyPet", pc.Data.DefaultOutfit.ColorId);
        }
        public static void AllPlayerOnlySeeMePet() => PlayerCatch.AllPlayerControls.Do(pc => pc.OnlySeeMyPet(Camouflage.PlayerSkins.TryGetValue(pc.PlayerId, out var outfit) ? outfit.PetId : ""));
    }
}