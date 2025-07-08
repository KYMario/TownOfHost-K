using AmongUs.GameOptions;
using Hazel;
using System;
using HarmonyLib;
using System.Linq;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.AddOns.Common;
using static TownOfHost.SelectRolesPatch;

namespace TownOfHost.Modules;

class StandardIntro
{
    public static void CoGameIntroWeight()
    {
        // イントロ通信分割
        if (Options.ExIntroWeight.GetBool() && Options.CurrentGameMode is CustomGameMode.Standard)
        {
            InnerNetClientPatch.DontTouch = true;
            MeetingHudPatch.StartPatch.Serialize = true;
            var stream = MessageWriter.Get(SendOption.Reliable);
            stream.StartMessage(5);
            stream.Write(AmongUsClient.Instance.GameId);
            foreach (var data in GameData.Instance.AllPlayers)//これ1人でstream.Lengthが111
            {
                data.Disconnected = true;
                stream.StartMessage(1);
                stream.WritePacked(data.NetId);
                data.Serialize(stream, false);
                stream.EndMessage();
            }
            stream.EndMessage();
            AmongUsClient.Instance.SendOrDisconnect(stream);
            stream.Recycle();
            InnerNetClientPatch.DontTouch = false;
            MeetingHudPatch.StartPatch.Serialize = false;
            foreach (var data in GameData.Instance.AllPlayers)
            {
                data.Disconnected = false;
            }
        }
    }
    public static void CoResetRoleY()
    {
        //playercount ^2だったのがこれだとplayercount * 3(Serialize,Setrole) + αで済む。重くない←重いよ?
        var host = PlayerControl.LocalPlayer;

        InnerNetClientPatch.DontTouch = true;
        MeetingHudPatch.StartPatch.Serialize = true;
        if (Options.ExIntroWeight.GetBool())
        {
            foreach (var data in GameData.Instance.AllPlayers) data.Disconnected = true;
        }
        var stream = MessageWriter.Get(SendOption.Reliable);
        stream.StartMessage(5);
        stream.Write(AmongUsClient.Instance.GameId);
        if (Options.ExIntroWeight.GetBool() is false)
        {
            foreach (var data in GameData.Instance.AllPlayers)//これ1人でstream.Lengthが111
            {
                data.Disconnected = true;
                stream.StartMessage(1);
                stream.WritePacked(data.NetId);
                data.Serialize(stream, false);
                stream.EndMessage();
            }
        }
        _ = new LateTask(() =>
        {
            stream.StartMessage(2);
            stream.WritePacked(PlayerControl.LocalPlayer.NetId);
            stream.Write((byte)RpcCalls.SetRole);
            stream.Write((ushort)RoleTypes.Crewmate);
            stream.Write(true);
            stream.EndMessage();
            foreach (var data in GameData.Instance.AllPlayers)
            {
                data.Disconnected = false;
                stream.StartMessage(1);
                stream.WritePacked(data.NetId);
                data.Serialize(stream, false);
                stream.EndMessage();
            }
            stream.EndMessage();
            AmongUsClient.Instance.SendOrDisconnect(stream);
            stream.Recycle();
            InnerNetClientPatch.DontTouch = false;

            MeetingHudPatch.StartPatch.Serialize = false;

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
                    foreach (var seen in PlayerCatch.AllPlayerControls)
                    {
                        if (!SuddenDeathMode.NowSuddenDeathMode && (role.GetCustomRoleTypes() is CustomRoleTypes.Impostor || role is CustomRoles.Egoist)
                        && (seen.GetCustomRole().GetCustomRoleTypes() is CustomRoleTypes.Impostor || seen.GetCustomRole() is CustomRoles.Egoist))
                        {
                            _ = new LateTask(() =>
                            seen.RpcSetRoleDesync(RoleTypes.Impostor, pc.GetClientId(), SendOption.Reliable)
                            , Main.LagTime, "SetHostImpostor", true);
                        }
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

            if (!Main.IsroleAssigned)
            {
                roleAssigned = true;
                PlayerCatch.AllPlayerControls.DoIf(x => RpcSetTasksPatch.taskIds.ContainsKey(x.PlayerId), pc => pc.Data.RpcSetTasks(RpcSetTasksPatch.taskIds[pc.PlayerId]));
            }

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
        }, Options.ExIntroWeight.GetBool() ? 1f : 0f, "gamestartsetrole");
    }
    public static void SetRole()
    {
        if (AmongUsClient.Instance.AmHost)
        {
            _ = new LateTask(() =>
            {
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

                            var roleinfo = pc.GetCustomRole().GetRoleInfo();
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
                            if (pc == PlayerControl.LocalPlayer && (roleinfo?.IsDesyncImpostor ?? false) && !(Options.SuddenAllRoleonaji.GetBool() && Options.SuddenTeamRole.GetBool())) continue;
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