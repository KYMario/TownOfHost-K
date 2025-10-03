using AmongUs.GameOptions;
using Hazel;
using System;
using HarmonyLib;
using System.Linq;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.AddOns.Common;
using static TownOfHost.SelectRolesPatch;
using TownOfHost.Patches;

namespace TownOfHost.Modules;

class StandardIntro
{
    public static void CoGameIntroWeight()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        // イントロ通信分割
        if (!Options.ExOldIntroSystem.GetBool() && Options.CurrentGameMode is CustomGameMode.Standard)//役職配布前に通信擬装をしておく。
        {
            _ = new LateTask(() =>
            {
                InnerNetClientPatch.DontTouch = true;
                GameDataSerializePatch.SerializeMessageCount++;
                bool IsSend = false;
                var stream = MessageWriter.Get(SendOption.Reliable);
                stream.StartMessage(5);
                stream.Write(AmongUsClient.Instance.GameId);
                foreach (var data in GameData.Instance.AllPlayers)//これ1人でstream.Lengthが111
                {
                    if (data.PlayerId == 0) continue;
                    if (IsSend)//全員通信擬装するとイントロが複数発生するのでホスト以外
                    {
                        stream = MessageWriter.Get(SendOption.Reliable);
                        stream.StartMessage(5);
                        stream.Write(AmongUsClient.Instance.GameId);
                        IsSend = false;
                    }
                    data.Disconnected = true;
                    stream.StartMessage(1);
                    stream.WritePacked(data.NetId);
                    Logger.Info($"{data.GetLogPlayerName()}", "StandardIntro");
                    data.Serialize(stream, false);
                    stream.EndMessage();
                    if (stream.Length > UtilsNotifyRoles.chengepake)
                    {
                        IsSend = true;
                        stream.EndMessage();
                        AmongUsClient.Instance.SendOrDisconnect(stream);
                        stream.Recycle();
                    }
                }
                if (!IsSend)
                {
                    stream.EndMessage();
                    AmongUsClient.Instance.SendOrDisconnect(stream);
                    stream.Recycle();
                }
                InnerNetClientPatch.DontTouch = false;
                GameDataSerializePatch.SerializeMessageCount--;
                Logger.Info($"SetDisconnected", "StandardIntro");
                GameDataSerializePatch.DontTouch = true;
                foreach (var data in GameData.Instance.AllPlayers)
                {
                    data.Disconnected = false;
                }
            }, 0.5f, "setdisconnected");
        }
    }
    public static void CoResetRoleY()
    {
        Logger.Info($"ShowIntro", "StandardIntro");
        if (!Options.ExOldIntroSystem.GetBool())
        {
            var host = PlayerControl.LocalPlayer;

            GameDataSerializePatch.DontTouch = false;
            InnerNetClientPatch.DontTouch = true;
            GameDataSerializePatch.SerializeMessageCount++;
            var stream = MessageWriter.Get(SendOption.Reliable);
            stream.StartMessage(5);
            stream.Write(AmongUsClient.Instance.GameId);
            {
                var data = host.Data;//ホストの偽装はこっちで。
                data.Disconnected = true;
                stream.StartMessage(1);
                stream.WritePacked(data.NetId);
                data.Serialize(stream, false);
                stream.EndMessage();
            }
            stream.StartMessage(2);
            stream.WritePacked(PlayerControl.LocalPlayer.NetId);
            stream.Write((byte)RpcCalls.SetRole);
            stream.Write((ushort)RoleTypes.Crewmate);
            stream.Write(true);
            stream.EndMessage();
            var i = 0;
            foreach (var data in GameData.Instance.AllPlayers)//これ1人でstream.Lengthが111
            {
                i++;
                data.Disconnected = false;
                //data.PlayerName = Camouflage.PlayerSkins.TryGetValue(data.PlayerId, out var cos) ? cos.PlayerName : data.GetLogPlayerName();
                if (4 < i) continue;//4人以上は後ででよい。
                stream.StartMessage(1);
                stream.WritePacked(data.NetId);
                data.Serialize(stream, false);
                stream.EndMessage();
            }
            stream.EndMessage();
            AmongUsClient.Instance.SendOrDisconnect(stream);
            stream.Recycle();
            _ = new LateTask(() =>
            {
                if (!Main.IsroleAssigned)
                {
                    var sender = MessageWriter.Get(SendOption.Reliable);
                    sender.StartMessage(5);
                    sender.Write(AmongUsClient.Instance.GameId);
                    i = 0;
                    bool issend = false;
                    foreach (var data in GameData.Instance.AllPlayers)//これ1人でstream.Lengthが111
                    {
                        i++;
                        if (4 < i)//イントロが始まるまでの間に戻しておく。
                        {
                            if (issend)
                            {
                                sender = MessageWriter.Get(SendOption.Reliable);
                                sender.StartMessage(5);
                                sender.Write(AmongUsClient.Instance.GameId);
                                issend = false;
                            }
                            data.Disconnected = false;
                            sender.StartMessage(1);
                            sender.WritePacked(data.NetId);
                            data.Serialize(sender, false);
                            sender.EndMessage();
                            if (sender.Length > UtilsNotifyRoles.chengepake)
                            {
                                issend = true;
                                sender.EndMessage();
                                AmongUsClient.Instance.SendOrDisconnect(sender);
                                sender.Recycle();
                            }
                        }
                    }
                    if (!issend)
                    {
                        sender.EndMessage();
                        AmongUsClient.Instance.SendOrDisconnect(sender);
                        sender.Recycle();
                    }
                    _ = new LateTask(() =>
                    {
                        PlayerCatch.AllPlayerControls.Do(pc =>
                        {
                            if (RpcSetTasksPatch.taskIds.TryGetValue(pc.PlayerId, out var taskids))
                                pc.Data.RpcSetTasks(taskids);
                            else
                            {
                                Logger.Error($"{pc.Data.GetLogPlayerName()} => taskIds is null", "AssingTask");
                                pc.Data.RpcSetTasks(Array.Empty<byte>());//再配布と同じ処理を行なっておく。
                            }
                        });
                        PlayerCatch.AllPlayerControls.Do(x => PlayerState.GetByPlayerId(x.PlayerId).InitTask(x));
                        GameData.Instance.RecomputeTaskCounts();
                        TaskState.InitialTotalTasks = GameData.Instance.TotalTasks;
                    }, 3, "SetTask");
                    roleAssigned = true;
                    InnerNetClientPatch.DontTouch = false;

                    GameDataSerializePatch.SerializeMessageCount--;
                }
            }, 0.75f, "SetTaskDelay");
            Intoro();
            return;
        }
        // イントロ緩和設定 ↑
        // イントロ緩和設定OFF↓
        {
            //playercount ^2だったのがこれだとplayercount * 3(Serialize,Setrole) + αで済む。重くない←重いよ?
            var host = PlayerControl.LocalPlayer;

            InnerNetClientPatch.DontTouch = true;
            GameDataSerializePatch.SerializeMessageCount++;
            var stream = MessageWriter.Get(SendOption.Reliable);
            stream.StartMessage(5);
            stream.Write(AmongUsClient.Instance.GameId);
            //_ = new LateTask(() =>
            {
                foreach (var data in GameData.Instance.AllPlayers)//これ1人でstream.Lengthが111
                {
                    data.Disconnected = true;
                    stream.StartMessage(1);
                    stream.WritePacked(data.NetId);
                    data.Serialize(stream, false);
                    stream.EndMessage();
                }
                stream.StartMessage(2);
                stream.WritePacked(PlayerControl.LocalPlayer.NetId);
                stream.Write((byte)RpcCalls.SetRole);
                stream.Write((ushort)RoleTypes.Crewmate);
                stream.Write(true);
                stream.EndMessage();
                var i = 0;
                foreach (var data in GameData.Instance.AllPlayers)//これ1人でstream.Lengthが111
                {
                    i++;
                    data.Disconnected = false;
                    if (4 < i && Options.ExRpcWeightR.GetBool()) continue;
                    stream.StartMessage(1);
                    stream.WritePacked(data.NetId);
                    data.Serialize(stream, false);
                    stream.EndMessage();
                }
                stream.EndMessage();
                AmongUsClient.Instance.SendOrDisconnect(stream);
                stream.Recycle();
                _ = new LateTask(() =>
                {
                    if (!Main.IsroleAssigned)
                    {
                        if (Options.ExRpcWeightR.GetBool())
                        {
                            var sender = MessageWriter.Get(SendOption.Reliable);
                            sender.StartMessage(5);
                            sender.Write(AmongUsClient.Instance.GameId);
                            i = 0;
                            bool issend = false;
                            foreach (var data in GameData.Instance.AllPlayers)//これ1人でstream.Lengthが111
                            {
                                i++;
                                if (4 < i)
                                {
                                    issend = true;
                                    data.Disconnected = false;
                                    sender.StartMessage(1);
                                    sender.WritePacked(data.NetId);
                                    data.Serialize(sender, false);
                                    sender.EndMessage();
                                }
                            }
                            if (issend)
                            {
                                sender.EndMessage();
                                AmongUsClient.Instance.SendOrDisconnect(sender);
                                sender.Recycle();
                            }
                        }
                        _ = new LateTask(() =>
                        {
                            PlayerCatch.AllPlayerControls.Do(pc =>
                            {
                                if (RpcSetTasksPatch.taskIds.TryGetValue(pc.PlayerId, out var taskids))
                                    pc.Data.RpcSetTasks(taskids);
                                else
                                {
                                    Logger.Error($"{pc.Data.GetLogPlayerName()} => taskIds is null", "AssingTask");
                                    pc.Data.RpcSetTasks(Array.Empty<byte>());//再配布と同じ処理を行なっておく。
                                }
                            });
                            PlayerCatch.AllPlayerControls.Do(x => PlayerState.GetByPlayerId(x.PlayerId).InitTask(x));
                            GameData.Instance.RecomputeTaskCounts();
                            TaskState.InitialTotalTasks = GameData.Instance.TotalTasks;
                        }, 3, "SetTask");
                        roleAssigned = true;
                        InnerNetClientPatch.DontTouch = false;

                        GameDataSerializePatch.SerializeMessageCount--;
                    }
                }, 0.3f, "SetTaskDelay");
                Intoro();
            }

        }
        void Intoro()
        {
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
                if (pc.GetClientId() == -1) continue;
                var role = pc.GetCustomRole();
                var roleType = role.GetRoleTypes();

                if (role.GetRoleInfo()?.IsDesyncImpostor == true || role is CustomRoles.Amnesiac || role.IsMadmate() || (role.IsNeutral() && role is not CustomRoles.Egoist) || SuddenDeathMode.NowSuddenDeathMode)
                {
                    roleType = role.IsCrewmate() ? RoleTypes.Crewmate : (role.IsMadmate() ? RoleTypes.Crewmate : ((role.IsNeutral() && role is not CustomRoles.Egoist) ? RoleTypes.Impostor : roleType));
                    if (role is CustomRoles.Amnesiac) roleType = RoleTypes.Crewmate;
                }
                if (role is CustomRoles.BakeCat) roleType = RoleTypes.Crewmate;
                if (pc.Is(CustomRoles.Amnesia) && Amnesia.dontcanUseability)
                {
                    roleType = role.IsImpostor() && !pc.Is(CustomRoles.Amnesiac) ? RoleTypes.Impostor : RoleTypes.Crewmate;
                }

                pc.RpcSetRoleDesync(roleType, pc.GetClientId(), SendOption.None);

                if (pc.Is(CustomRoles.Amnesiac)) continue;
                if (pc.Is(CustomRoles.OneWolf)) continue;
                foreach (var seen in PlayerCatch.AllPlayerControls)
                {
                    if (seen.Is(CustomRoles.OneWolf)) continue;
                    if (!SuddenDeathMode.NowSuddenDeathMode && (role.GetCustomRoleTypes() is CustomRoleTypes.Impostor || role is CustomRoles.Egoist)
                    && (seen.GetCustomRole().GetCustomRoleTypes() is CustomRoleTypes.Impostor || seen.GetCustomRole() is CustomRoles.Egoist))
                    {
                        _ = new LateTask(() =>
                        seen.RpcSetRoleDesync(RoleTypes.Impostor, pc.GetClientId(), SendOption.Reliable)
                        , Main.LagTime, "SetHostImpostor", true);
                    }
                }
            }

            new LateTask(() =>
            {
                PlayerControl.AllPlayerControls.ForEach((Action<PlayerControl>)(pc => PlayerNameColor.Set(pc)));
                PlayerControl.LocalPlayer.StopAllCoroutines();
                HudManagerCoShowIntroPatch.Cancel = false;
                DestroyableSingleton<HudManager>.Instance.StartCoroutine(DestroyableSingleton<HudManager>.Instance.CoShowIntro());
                DestroyableSingleton<HudManager>.Instance.HideGameLoader();
                UtilsNotifyRoles.NotifyRoles(ForceLoop: true);
            }, 0.2f, "", true);

            new LateTask(() =>
            {
                foreach (var pc in PlayerCatch.AllPlayerControls)
                {
                    if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
                    if (pc.GetClientId() == -1) continue;
                    var role = pc.GetCustomRole();
                    var roleType = role.GetRoleTypes();
                    if (role.GetRoleInfo()?.IsDesyncImpostor == true || role is CustomRoles.Amnesiac || role.IsMadmate() || (role.IsNeutral() && role is not CustomRoles.Egoist) || SuddenDeathMode.NowSuddenDeathMode)
                    {
                        roleType = role.IsCrewmate() ? RoleTypes.Crewmate : (role.IsMadmate() ? RoleTypes.Phantom : ((role.IsNeutral() && role is not CustomRoles.Egoist) ? RoleTypes.Crewmate : roleType));
                        if (role is CustomRoles.Amnesiac) roleType = RoleTypes.Crewmate;
                    }

                    if (role is CustomRoles.BakeCat) roleType = RoleTypes.Crewmate;

                    if (pc.Is(CustomRoles.Amnesia) && Amnesia.dontcanUseability)
                    {
                        roleType = role.IsImpostor() && !pc.Is(CustomRoles.Amnesiac) ? RoleTypes.Impostor : RoleTypes.Crewmate;
                    }
                    pc.RpcSetRoleDesync(roleType, pc.GetClientId(), SendOption.None);
                }
                SuddenDeathMode.ColorSetAndRoleset();
                senders2 = null;
            }, 2.2f + (GameStates.IsOnlineGame ? 0.4f : 0), "", false);
            _ = new LateTask(() => SetRole(), 5.5f, "", true);
            //}, Options.ExOldIntroSystem.GetBool() ? 1f : 0f, "gamestartsetrole");
        }
    }
    public static void SetRole()
    {
        if (AmongUsClient.Instance.AmHost)
        {
            _ = new LateTask(() =>
            {
                Logger.Info($"SetRole", "StandardIntro");
                //イントロ中回線落ち用の奴
                // ...ホストが廃村してほしいけド...一応...
                var send = false;
                foreach (var dis in Disconnected)
                {
                    var client = GameData.Instance.AllPlayers.ToArray().Where(data => data.PlayerId == dis).FirstOrDefault();
                    if (client == null) continue;
                    client.Disconnected = true;
                    send = true;
                }
                if (send) RPC.RpcSyncAllNetworkedPlayer();

                PlayerCatch.AllPlayerControls.Do(Player => PlayerOutfitManager.Save(Player));
                if (Options.CurrentGameMode == CustomGameMode.Standard)
                {
                    //初手強制会議あるなら戻さない
                    if (!SuddenDeathMode.NowSuddenDeathMode && Options.FirstTurnMeeting.GetBool()) return;

                    if (GameStates.InGame)
                        foreach (var pc in PlayerCatch.AllPlayerControls)
                        {
                            if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId && Options.EnableGM.GetBool()) continue;

                            var role = pc.GetCustomRole();
                            var roleinfo = role.GetRoleInfo();

                            if (role.GetRoleInfo()?.IsCantSeeTeammates == true && role.IsImpostor() && !SuddenDeathMode.NowSuddenDeathMode)
                            {
                                var clientId = pc.GetClientId();
                                foreach (var killer in PlayerCatch.AllPlayerControls)
                                {
                                    if (!killer.GetCustomRole().IsImpostor()) continue;
                                    //Amnesiac視点インポスターをクルーにする
                                    killer.RpcSetRoleDesync(RoleTypes.Scientist, clientId);
                                }
                            }
                            if (pc.Is(CustomRoles.Amnesiac) && !SuddenDeathMode.NowSuddenDeathMode)
                            {
                                foreach (var killer in PlayerCatch.AllPlayerControls)
                                {
                                    if (killer == null) continue;
                                    if (pc.PlayerId == killer.PlayerId) continue;
                                    if (!killer.GetCustomRole().IsImpostor()) continue;
                                    var clientId = killer.GetClientId();
                                    //他者視点Amnesiacをインポスターにする
                                    pc.RpcSetRoleDesync(RoleTypes.Impostor, clientId);
                                }
                            }
                            if (pc.Is(CustomRoles.OneWolf))
                            {
                                foreach (var seer in PlayerCatch.AllPlayerControls)
                                {
                                    if (seer.PlayerId == pc.PlayerId) continue;
                                    pc.RpcSetRoleDesync(RoleTypes.Crewmate, seer.GetClientId());
                                }
                                foreach (var target in PlayerCatch.AllPlayerControls)
                                {
                                    if (target.PlayerId == pc.PlayerId) continue;
                                    target.RpcSetRoleDesync(RoleTypes.Crewmate, pc.GetClientId());
                                }
                            }
                            if (pc.Is(CustomRoles.Amnesia))//continueでいいかもだけど一応...
                            {
                                if (roleinfo?.IsDesyncImpostor == true && pc.Is(CustomRoleTypes.Crewmate))
                                {
                                    if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                                    {
                                        RoleManager.Instance.SetRole(PlayerControl.LocalPlayer, RoleTypes.Crewmate);
                                        continue;
                                    }
                                    pc.RpcSetRoleDesync(RoleTypes.Crewmate, pc.GetClientId());
                                    continue;
                                }
                                if (Amnesia.dontcanUseability)
                                {
                                    if (pc.Is(CustomRoleTypes.Impostor))
                                    {
                                        if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                                        {
                                            RoleManager.Instance.SetRole(PlayerControl.LocalPlayer, RoleTypes.Impostor);
                                            continue;
                                        }
                                        pc.RpcSetRoleDesync(RoleTypes.Impostor, pc.GetClientId());
                                        continue;
                                    }
                                    else
                                    {
                                        if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                                        {
                                            RoleManager.Instance.SetRole(PlayerControl.LocalPlayer, RoleTypes.Crewmate);
                                            continue;
                                        }
                                        pc.RpcSetRoleDesync(RoleTypes.Crewmate, pc.GetClientId());
                                        continue;
                                    }
                                }
                            }
                            if (pc == PlayerControl.LocalPlayer && (roleinfo?.IsDesyncImpostor ?? false) && !(SuddenDeathMode.SuddenSharingRoles.GetBool() && SuddenDeathMode.SuddenTeamRole.GetBool())) continue;
                            {
                                pc.RpcSetRoleDesync(roleinfo.BaseRoleType.Invoke(), pc.GetClientId());
                            }
                        }
                }

                if (Options.CurrentGameMode == CustomGameMode.Standard)
                    _ = new LateTask(() =>
                    {
                        foreach (var Player in PlayerCatch.AllPlayerControls)
                        {
                            if (Player.PlayerId == PlayerControl.LocalPlayer.PlayerId && Options.EnableGM.GetBool()) continue;
                            {
                                if (!AmongUsClient.Instance.AmHost) return;
                                if (Camouflage.IsCamouflage) return;
                                if (Player.inVent) return;
                                var outfit = PlayerOutfitManager.Load(Player);

                                if (outfit == null) return;

                                if (Player.IsAlive()) Player.RpcSetPet(outfit.pet);
                            }
                        }
                        UtilsNotifyRoles.NotifyRoles(ForceLoop: true);
                    }, 0.2f, "Use On click Shepe", true);
            }, 2.0f, "Roleset", false);
        }
    }
}