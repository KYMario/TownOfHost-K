using System.Collections.Generic;
using System.Linq;
using System.Text;

using HarmonyLib;
using UnityEngine;

using TownOfHost.Modules;
using TownOfHost.Patches;
using TownOfHost.Roles;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Crewmate;
using TownOfHost.Roles.Impostor;
using TownOfHost.Roles.Neutral;
using TownOfHost.Roles.Ghost;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.AddOns.Impostor;
using TownOfHost.Roles.AddOns.Neutral;
using TownOfHost.Roles.AddOns.Common;
using static TownOfHost.Translator;
using TownOfHost.Modules.ChatManager;

namespace TownOfHost;

[HarmonyPatch]
public static class MeetingHudPatch
{
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CheckForEndVoting))]
    class CheckForEndVotingPatch
    {
        public static bool Prefix()
        {
            if (!AmongUsClient.Instance.AmHost) return true;
            MeetingVoteManager.Instance?.CheckAndEndMeeting();
            return false;
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CastVote))]
    public static class CastVotePatch
    {
        public static bool Prefix(MeetingHud __instance, [HarmonyArgument(0)] byte srcPlayerId /* 投票した人 */ , [HarmonyArgument(1)] byte suspectPlayerId /* 投票された人 */ )
        {
            if (AmongUsClient.Instance.AmHost is false) return true;
            if (Options.firstturnmeeting && Options.FirstTurnMeetingCantability.GetBool() && MeetingStates.FirstMeeting)
            {
                MeetingVoteManager.Instance?.SetVote(srcPlayerId, 253);
                return true;
            }
            var voter = PlayerCatch.GetPlayerById(srcPlayerId);
            if (voter.isDummy)
            {
                MeetingVoteManager.Instance?.SetVote(srcPlayerId, suspectPlayerId);
                return true;
            }
            var votefor = PlayerCatch.GetPlayerById(suspectPlayerId);

            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                var roleClass = pc.GetRoleClass();
                if (Balancer.Id != 255 && !(suspectPlayerId == srcPlayerId || suspectPlayerId == Balancer.target1 || suspectPlayerId == Balancer.target2) && !pc.Is(CustomRoles.Balancer) && Balancer.OptionCanMeetingAbility.GetBool()) continue;
                if (Amnesia.CheckAbilityreturn(pc)) roleClass = null;

                if (roleClass?.CheckVoteAsVoter(suspectPlayerId, voter) == false || (!votefor.IsAlive() && suspectPlayerId != 253 && suspectPlayerId != 254 && !Assassin.NowUse))
                {
                    __instance.RpcClearVote(voter.GetClientId());
                    Logger.Info($"{voter.GetNameWithRole().RemoveHtmlTags()} は投票しない！ => {suspectPlayerId}", nameof(CastVotePatch));
                    return false;
                }
                else
                if (voter.Is(CustomRoles.Elector) && suspectPlayerId == 253 || (RoleAddAddons.GetRoleAddon(voter.GetCustomRole(), out var da, voter, subrole: CustomRoles.Elector) && da.GiveElector.GetBool() && suspectPlayerId == 253))
                {
                    Utils.SendMessage(GetString("ElectorCancelMessage"), voter.PlayerId);
                    __instance.RpcClearVote(voter.GetClientId());
                    Logger.Info($"{voter.GetNameWithRole().RemoveHtmlTags()} イレクター発動 => {suspectPlayerId}", nameof(CastVotePatch));
                    return false;
                }
            }
            if (voter.GetRoleClass() is ISelfVoter selfVoter)
            {
                if (selfVoter.CanUseVoted())
                {
                    MeetingVoteManager.Instance?.SetVote(srcPlayerId, suspectPlayerId, isoverride: false);
                    __instance.RpcClearVote(voter.GetClientId());
                    return true;
                }
            }
            MeetingVoteManager.Instance?.SetVote(srcPlayerId, suspectPlayerId);
            return true;
        }
    }
    public static string Oniku = "";
    public static string Send = "";
    public static string Title = "";
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    public class StartPatch
    {
        public static void Prefix()
        {
            Logger.Info($"------------会議開始　day:{UtilsGameLog.day}------------", "Phase");
            ChatUpdatePatch.DoBlockChat = true;
            ChatUpdatePatch.BlockSendName = true;
            MeetingStates.Sending = true;
            GameStates.task = false;
            InnerNetClientPatch.DontTouch = false;
            GameStates.AlreadyDied |= !PlayerCatch.IsAllAlive;
            PlayerCatch.OldAlivePlayerControles.Clear();
            var Sender = CustomRpcSender.Create("MeetingSet", Hazel.SendOption.Reliable);
            Sender.StartMessage();
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                pc.GetPlayerState().IsBlackOut = false;
                pc.Data.Role.NameColor = Palette.White;
                ReportDeadBodyPatch.WaitReport[pc.PlayerId].Clear();

                if (Main.CheckShapeshift.TryGetValue(pc.PlayerId, out var shapeshifting) && shapeshifting && AmongUsClient.Instance.AmHost)
                {
                    pc.RpcShapeshift(pc, false);
                }
                if (!pc.IsAlive() && !Assassin.NowUse)
                {
                    if (AmongUsClient.Instance.AmHost) pc.RpcExileV2();
                }//  会議時に生きてたぜリスト追加
                else
                {
                    PlayerCatch.OldAlivePlayerControles.Add(pc);
                }
            }
            UtilsOption.MarkEveryoneDirtySettings();
            ReportDeadBodyPatch.DontReport.Clear();
            MeetingStates.MeetingCalled = true;
            GameStates.ExiledAnimate = false;

            if (Options.ExHideChatCommand.GetBool() && !Assassin.NowUse)
            {
                ChatManager.StratMeetingSetDead();
            }
        }
        public static void Postfix(MeetingHud __instance)
        {
            MeetingVoteManager.Start();

            SoundManager.Instance.ChangeAmbienceVolume(0f);
            if (!GameStates.IsModHost) return;

            var myRole = PlayerControl.LocalPlayer.GetRoleClass();
            var aliveplayer = PlayerCatch.AllAlivePlayerControls.OrderBy(x => x.PlayerId);
            var deadplayer = PlayerCatch.AllPlayerControls.Where(x => !x.IsAlive()).OrderBy(x => x.PlayerId);
            var list = aliveplayer.ToArray().AddRangeToArray(deadplayer.ToArray());
            var meetinginfoplayer = list[0] is null || Assassin.NowUse ? PlayerCatch.GetPlayerById(0) : list[0];

            HudManagerPatch.LowerInfoText.text = myRole?.GetLowerText(PlayerControl.LocalPlayer, isForMeeting: true, isForHud: true) ?? "";
            if (PlayerControl.LocalPlayer.Is(CustomRoles.Amnesia)) HudManagerPatch.LowerInfoText.text = "";
            if (PlayerControl.LocalPlayer.GetMisidentify(out _)) HudManagerPatch.LowerInfoText.text = "";

            HudManagerPatch.LowerInfoText.enabled = HudManagerPatch.LowerInfoText.text != "";

            foreach (var pva in __instance.playerStates)
            {
                var pc = PlayerCatch.GetPlayerById(pva.TargetPlayerId);
                if (pc == null) continue;

                var roleTextMeeting = UnityEngine.Object.Instantiate(pva.NameText);
                roleTextMeeting.transform.SetParent(pva.PlayerIcon.transform);
                roleTextMeeting.transform.localPosition = new Vector3(3.25f, 1.02f, -5f);
                roleTextMeeting.fontSize = 1.5f;
                (roleTextMeeting.enabled, roleTextMeeting.text)
                    = UtilsRoleText.GetRoleNameAndProgressTextData(PlayerControl.LocalPlayer, pc, PlayerControl.LocalPlayer == pc);
                roleTextMeeting.gameObject.name = "RoleTextMeeting";
                roleTextMeeting.enableWordWrapping = false;
                //見る側が双子で相方が双子の場合
                if (Twins.TwinsList.TryGetValue(PlayerControl.LocalPlayer.PlayerId, out var targetid))
                {
                    if (targetid == pc.PlayerId) roleTextMeeting.text = UtilsRoleText.GetRoleColorAndtext(CustomRoles.Twins) + roleTextMeeting.text;
                }

                var suffixTextMeeting = UnityEngine.Object.Instantiate(pva.NameText);
                suffixTextMeeting.transform.SetParent(pva.PlayerIcon.transform);
                suffixTextMeeting.transform.localPosition = new Vector3(3.25f, 0.02f, 0f);
                suffixTextMeeting.fontSize = 1.5f;
                suffixTextMeeting.gameObject.name = "suffixTextMeeting";
                suffixTextMeeting.enableWordWrapping = false;
                suffixTextMeeting.enabled = false;

                // NameTextにSetParentすると後に作ったのにも付いてきちゃうからこっちに
                var MeetingInfo = UnityEngine.Object.Instantiate(pva.NameText);
                MeetingInfo.transform.SetParent(pva.PlayerIcon.transform);
                MeetingInfo.transform.localPosition = new Vector3(3.13f, 1.71f, 0f);
                MeetingInfo.fontSize = 1.8f;
                MeetingInfo.gameObject.name = "MeetingInfo";
                MeetingInfo.enableWordWrapping = false;
                MeetingInfo.enabled = false;

                var suffixBuilder = new StringBuilder(32);
                if (myRole != null)
                {
                    if (Amnesia.CheckAbility(PlayerControl.LocalPlayer))
                        suffixBuilder.Append(myRole.GetSuffix(PlayerControl.LocalPlayer, pc, isForMeeting: true));
                }
                suffixBuilder.Append(CustomRoleManager.GetSuffixOthers(PlayerControl.LocalPlayer, pc, isForMeeting: true));
                // suffixが0文字じゃなくて　　　　タグ、空白をきったら空にならない時は
                if (suffixBuilder.Length > 0 && suffixBuilder.ToString().RemoveHtmlTags().Trim(' ').Trim('　') != "")
                {
                    //下にSuffixを表示
                    suffixTextMeeting.text = suffixBuilder.ToString();
                    suffixTextMeeting.enabled = true;
                }
                else
                {
                    //そうじゃない時、上側ロールはなんか好まないので下に
                    roleTextMeeting.enabled = false;
                    suffixTextMeeting.text = roleTextMeeting.text;
                    suffixTextMeeting.enabled = true;
                }
                if (meetinginfoplayer.PlayerId == pc.PlayerId)
                {
                    MeetingInfo.enabled = true;
                    MeetingInfo.text = $"<#ffffff><line-height=95%>" + $"Day.{UtilsGameLog.day}".Color(Palette.Orange) + Bakery.BakeryMark() + $"\n{UtilsNotifyRoles.ExtendedMeetingText}";
                    if (CustomRolesHelper.CheckGuesser() || PlayerCatch.AllPlayerControls.Any(pc => pc.Is(CustomRoles.Guesser)))
                    {
                        MeetingInfo.text = $"<size=50%>\n </size>{MeetingInfo.text}\n<size=50%><#999900>{GetString("GuessInfo")}</color></size>";
                    }
                    MeetingInfo.text += "<line-height=0%>\n</line-height></line-height><line-height=300%>\n</line-height></color> ";
                }
            }
            CustomRoleManager.AllActiveRoles.Values.Do(role => role.OnStartMeeting());
            SlowStarter.OnStartMeeting();
            Send = "<size=80%>";
            Title = "";

            if (!Options.firstturnmeeting || !MeetingStates.FirstMeeting) Title += string.Format(GetString("Message.Day"), UtilsGameLog.day).Color(Palette.Orange) + "\n";
            else Title += GetString("Message.first").Color(Palette.Orange) + "\n";

            foreach (var roleClass in CustomRoleManager.AllActiveRoles.Values)
            {
                var RoleText = roleClass.MeetingAddMessage();
                if (RoleText != "") Send += RoleText + "\n\n";
            }
            var Ghostrumortext = GhostRumour.SendMes();
            if (Ghostrumortext != "")
            {
                Send += Ghostrumortext + "\n";
            }
            if (Oniku != "")
            {
                Send += "<#001e43>※" + Oniku + "</color>\n";
            }
            if (Options.SyncButtonMode.GetBool())
            {
                Send += "<#006e54>★" + string.Format(GetString("Message.SyncButtonLeft"), Options.SyncedButtonCount.GetFloat() - Options.UsedButtonCount) + "</color>\n";
                Logger.Info("緊急会議ボタンはあと" + (Options.SyncedButtonCount.GetFloat() - Options.UsedButtonCount) + "回使用可能です。", "SyncButtonMode");
            }
            if (AntiBlackout.OverrideExiledPlayer())
            {
                Send += "<#640125>！" + GetString("Warning.OverrideExiledPlayer") + "</color>\n";
            }
            if (!SelfVoteManager.Canuseability())
            {
                Send += "<#998317>◇" + GetString("Warning.CannotUseAbility") + "</color>\n";
            }
            if (MeetingVoteManager.Voteresult != "")
            {
                if (Send.RemoveHtmlTags() != "") Send += "\n";
                Send += "<size=120%>【" + GetString("LastMeetingre") + "】\n</size>" + MeetingVoteManager.Voteresult;
            }
            Send += $"\n{GetString("MeetingHelp")}";
            TemplateManager.SendTemplate("OnMeeting", noErr: true);
            if (MeetingStates.FirstMeeting) TemplateManager.SendTemplate("OnFirstMeeting", noErr: true);
            if (Send != "") Utils.SendMessage(Send, title: Title);

            if (Options.CanSeeTimeLimit.GetBool())
            {
                string limittext = "";

                limittext = GetString("TimeLimit") + "<size=80%>";

                var admins = Check(DisableDevice.GetAddminTimer(false));
                if (admins is not "") admins = $"\n{GetString("Addmin")}{admins}";
                var vitals = Check(DisableDevice.GetVitalTimer(false));
                if (vitals is not "") vitals = $"\n{GetString("Vital")}{vitals}";
                var cams = Check(DisableDevice.GetCamTimr(false));
                if (cams is not "") cams = $"\n{(Main.NormalOptions.MapId is 1 ? GetString("Doorlog") : GetString("Camera"))}{cams}";

                var lt = admins + vitals + cams;

                if (lt is not "")
                {
                    if (Options.CanseeCrewTimeLimit.GetBool() && Options.CanseeImpTimeLimit.GetBool()
                    && Options.CanseeMadTimeLimit.GetBool() && Options.CanseeNeuTimeLimit.GetBool())
                    {
                        Utils.SendMessage(limittext + lt);
                    }
                    else
                    {
                        foreach (var pc in PlayerCatch.AllPlayerControls)
                        {
                            var team = pc.GetCustomRole().GetCustomRoleTypes();
                            if ((team == CustomRoleTypes.Impostor && Options.CanseeImpTimeLimit.GetBool()) || (team == CustomRoleTypes.Crewmate && Options.CanseeCrewTimeLimit.GetBool())
                                    || (team == CustomRoleTypes.Neutral && Options.CanseeNeuTimeLimit.GetBool()) || (team == CustomRoleTypes.Madmate && Options.CanseeMadTimeLimit.GetBool()))
                            {
                                Utils.SendMessage(limittext + lt + $"\n{GetString("LimitSendInfo")}", pc.PlayerId);
                            }
                        }
                    }
                }

                string Check(string text)
                {
                    text = text.RemoveColorTags();
                    switch (text)
                    {
                        case "×": text = GetString("DeviceCantUse"); break;
                        case "": text = ""; break;
                        default: text = $"{GetString("LimitTime")}:{text}"; break;
                    }
                    return text;
                }
            }
            MeetingVoteManager.Voteresult = "";
            Oniku = "";
            Utils.RoleSendList.Clear();
            if (AmongUsClient.Instance.AmHost)
            {
                //エアシなら始まった瞬間に展望いるならうるさいからワープさせる
                if (Main.NormalOptions.MapId == 4)
                {
                    foreach (var pl in PlayerCatch.AllPlayerControls)
                    {
                        if (pl.IsModClient()) continue;
                        Vector2 poji = pl.transform.position;
                        if (poji.y <= -13.6f) pl.RpcSnapToForced(new Vector2(poji.x, -13f));
                        if (poji.x >= 4.3f && poji.y <= -13.6f) pl.RpcSnapToForced(new Vector2(7.6f, -10.6f));
                    }
                }
                _ = new LateTask(() =>
                {
                    MeetingStates.Sending = false;
                    ChatUpdatePatch.DoBlockChat = false;
                    _ = new LateTask(() =>
                    {
                        foreach (var pva in __instance.playerStates)
                        {
                            var pc = PlayerCatch.GetPlayerById(pva.TargetPlayerId);
                            if (pc == null) continue;

                            if (MeetingStates.FirstMeeting) UtilsShowOption.SendRoleInfo(pc);
                            else if (Utils.RoleSendList.Contains(pva.TargetPlayerId)) UtilsShowOption.SendRoleInfo(pc);
                        }
                    }, 1, "sendroleinfo");
                }, 2.5f, "Send to Chat", true);
                _ = new LateTask(() =>
                {
                    ChatUpdatePatch.BlockSendName = false;
                    NameColorManager.RpcMeetingColorName();
                }, 5.5f, "SetName", true);
            }
            Main.IsActiveSabotage =
                Utils.IsActive(SystemTypes.Reactor)
                || Utils.IsActive(SystemTypes.Electrical)
                || Utils.IsActive(SystemTypes.Laboratory)
                || Utils.IsActive(SystemTypes.Comms)
                || Utils.IsActive(SystemTypes.LifeSupp)
                || Utils.IsActive(SystemTypes.HeliSabotage);

            foreach (var pva in __instance.playerStates)
            {
                if (pva == null) continue;
                var seer = PlayerControl.LocalPlayer;
                var seerRole = seer.GetRoleClass();

                var target = PlayerCatch.GetPlayerById(pva.TargetPlayerId);
                if (target == null)
                {
                    if (Camouflage.PlayerSkins.TryGetValue(pva.TargetPlayerId, out var cosm))
                    {
                        pva.NameText.text = cosm.PlayerName;
                    }
                    continue;
                }

                var sb = new StringBuilder();
                var fsb = new StringBuilder();

                //会議画面での名前変更
                //自分自身の名前の色を変更
                //NameColorManager準拠の処理
                var name = pva.NameText.text;
                if (Camouflage.PlayerSkins.TryGetValue(pva.TargetPlayerId, out var cos))
                {
                    name = cos.PlayerName;
                }
                pva.NameText.text = name.ApplyNameColorData(seer, target, true);

                //とりあえずSnitchは会議中にもインポスターを確認することができる仕様にしていますが、変更する可能性があります。

                if (seer.KnowDeathReason(target))
                    sb.Append($"<size=75%>({Utils.ColorString(UtilsRoleText.GetRoleColor(CustomRoles.Doctor), Utils.GetVitalText(target.PlayerId, seer.PlayerId.CanDeathReasonKillerColor()))})</size>");

                if (Amnesia.CheckAbility(seer))
                    sb.Append(seerRole?.GetMark(seer, target, true));
                sb.Append(CustomRoleManager.GetMarkOthers(seer, target, true));

                //相手のサブロール処理
                foreach (var subRole in target.GetCustomSubRoles())
                {
                    if (subRole is not CustomRoles.OneLove && subRole.IsLovers() && (seer.GetLoverRole() == subRole || seer.Data.IsDead))
                    {
                        sb.Append(Utils.ColorString(UtilsRoleText.GetRoleColor(subRole), "♥"));
                        continue;
                    }
                    switch (subRole)
                    {
                        case CustomRoles.Connecting:
                            if ((seer.Is(CustomRoles.Connecting) && !seer.Is(CustomRoles.WolfBoy)) || seer.Data.IsDead)
                                sb.Append(Utils.ColorString(UtilsRoleText.GetRoleColor(CustomRoles.Connecting), "Ψ"));
                            continue;
                    }
                }

                //本人のsubrole処理
                foreach (var subRole in seer.GetCustomSubRoles())
                {
                    switch (subRole)
                    {
                        case CustomRoles.Guesser:
                            if (!seer.Data.IsDead && !target.Data.IsDead && target != seer)
                                fsb.Append(Utils.ColorString(Color.yellow, target.PlayerId.ToString()) + " ");
                            continue;
                        case CustomRoles.LastImpostor:
                            if (!LastImpostor.giveguesser) continue;
                            if (!seer.Data.IsDead && !target.Data.IsDead && target != seer)
                                fsb.Append(Utils.ColorString(Color.yellow, target.PlayerId.ToString()) + " ");
                            continue;
                        case CustomRoles.LastNeutral:
                            if (!LastNeutral.GiveGuesser.GetBool()) continue;
                            if (!seer.Data.IsDead && !target.Data.IsDead && target != seer)
                                fsb.Append(Utils.ColorString(Color.yellow, target.PlayerId.ToString()) + " ");
                            continue;
                        case CustomRoles.OneLove:
                            if (target == seer) continue;
                            if (Lovers.OneLovePlayer.BelovedId == target.PlayerId || target.Is(CustomRoles.OneLove))
                                sb.Append(Utils.ColorString(UtilsRoleText.GetRoleColor(CustomRoles.OneLove), "♡"));
                            continue;
                    }
                }
                if (RoleAddAddons.GetRoleAddon(seer.GetCustomRole(), out var data, seer, subrole: CustomRoles.Guesser) && data.GiveGuesser.GetBool())
                {
                    if (!seer.Data.IsDead && !target.Data.IsDead && target != seer)
                        fsb.Append(Utils.ColorString(Color.yellow, target.PlayerId.ToString()) + " ");
                }

                if (Options.CanSeeNextRandomSpawn.GetBool() && seer == target)
                {
                    if (RandomSpawn.SpawnMap.NextSpornName.TryGetValue(seer.PlayerId, out var r))
                        pva.NameText.text += $"<size=40%><#9ae3bd>〔{r}〕</size></color>";
                }

                //名前の適応　　　　　ゲッサー番号等　　名前　　　　　　　　　ラバー等のマーク
                pva.NameText.text = fsb.ToString() + pva.NameText.text + sb.ToString();

                if (list.LastOrDefault() != null)
                    if (list.LastOrDefault() == target)
                    {
                        var team = seer.GetCustomRole().GetCustomRoleTypes();
                        if (Options.CanSeeTimeLimit.GetBool() && DisableDevice.optTimeLimitDevices)
                        {
                            var info = "<size=60%>" + DisableDevice.GetAddminTimer() + "</color>　" + DisableDevice.GetCamTimr() + "</color>　" + DisableDevice.GetVitalTimer() + "</color></size>";
                            if ((team == CustomRoleTypes.Impostor && Options.CanseeImpTimeLimit.GetBool()) || (team == CustomRoleTypes.Crewmate && Options.CanseeCrewTimeLimit.GetBool())
                            || (team == CustomRoleTypes.Neutral && Options.CanseeNeuTimeLimit.GetBool()) || (team == CustomRoleTypes.Madmate && Options.CanseeMadTimeLimit.GetBool()) || !seer.IsAlive())
                                if (info != "")
                                {
                                    var Name = info.RemoveText() + "\n" + pva.NameText.text + "\n" + info;
                                    pva.NameText.text = Name;
                                }
                        }
                    }
            }
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
    class UpdatePatch
    {
        public static void Postfix(MeetingHud __instance)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (Input.GetMouseButtonUp(1) && Input.GetKey(KeyCode.LeftControl))
            {
                __instance.playerStates.DoIf(x => x.HighlightedFX.enabled, x =>
                {
                    var state = PlayerState.GetByPlayerId(x.TargetPlayerId);
                    state.DeathReason = CustomDeathReason.Execution;
                    state.SetDead();
                    var player = PlayerCatch.GetPlayerById(x.TargetPlayerId);
                    if (player is null)
                    {
                        var data = GameData.Instance.AllPlayers.ToArray().Where(a => a.PlayerId == x.TargetPlayerId).FirstOrDefault();
                        if (data is not null) data.Disconnected = true;

                        __instance.CheckForEndVoting();
                        return;
                    }
                    player.RpcExileV2();
                    Utils.SendMessage(string.Format(GetString("Message.Executed"), UtilsName.GetPlayerColor(player, true)));
                    UtilsGameLog.AddGameLog("Executed", string.Format(GetString("Message.Executed"), UtilsName.GetPlayerColor(player, true)));
                    Logger.Info($"{player.GetNameWithRole().RemoveHtmlTags()}を処刑しました", "Execution");
                    __instance.CheckForEndVoting();

                    if (Options.ExHideChatCommand.GetBool() && !AntiBlackout.IsCached)
                    {
                        GameDataSerializePatch.SerializeMessageCount++;
                        foreach (var pc in PlayerCatch.AllAlivePlayerControls)
                        {
                            if (pc == player) continue;
                            pc.Data.IsDead = false;
                        }
                        RPC.RpcSyncAllNetworkedPlayer(player.GetClientId());
                        GameDataSerializePatch.SerializeMessageCount--;
                    }
                });
            }
            if (Balancer.Id != 255)
            {
                if (!PlayerCatch.GetPlayerById(Balancer.target1).IsAlive()
                    || !PlayerCatch.GetPlayerById(Balancer.target2).IsAlive())
                    MeetingVoteManager.Instance.EndMeeting(false);
            }
            if (Assassin.NowUse)
            {
                __instance.playerStates.Do(hud =>
                {
                    hud.AmDead = false;
                });
            }
        }
    }
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.OnDestroy))]
    class OnDestroyPatch
    {
        public static void Postfix()
        {
            MeetingStates.FirstMeeting = false;
            Logger.Info("------------会議終了------------", "Phase");
            if (AmongUsClient.Instance.AmHost)
            {
                if (!AntiBlackout.IsCached) AntiBlackout.SetIsDead();
                foreach (var data in SelfVoteManager.CheckVote)
                    SelfVoteManager.CheckVote[data.Key] = false;
            }
            // MeetingVoteManagerを通さずに会議が終了した場合の後処理
            MeetingVoteManager.Instance?.Destroy();
        }
    }

    public static void TryAddAfterMeetingDeathPlayers(CustomDeathReason deathReason, params byte[] playerIds)
    {
        string log = "";
        playerIds.Do(id => log += $"({id})");
        if (playerIds.Count() > 0) Logger.Info($"{log}を{deathReason}で会議後に処理するぜ!", "TryAddAfterMeetingDeathPlayers");

        var AddedIdList = new List<byte>();
        foreach (var playerId in playerIds)
            if (Main.AfterMeetingDeathPlayers.TryAdd(playerId, deathReason))
            {
                ReportDeadBodyPatch.IgnoreBodyids[playerId] = false;
                AddedIdList.Add(playerId);
                if (deathReason == CustomDeathReason.Revenge)
                {
                    MeetingVoteManager.Voteresult += "\n<size=60%>" + UtilsName.GetPlayerColor(PlayerCatch.GetPlayerById(playerId)) + GetString("votemi");
                    UtilsGameLog.AddGameLog("Revenge", UtilsName.GetPlayerColor(PlayerCatch.GetPlayerById(playerId)) + GetString("votemi"));
                }
            }

        //投票の道連れ処理は他でしてるのでここではしない。
        if (deathReason != CustomDeathReason.Vote) CheckForDeathOnExile(deathReason, AddedIdList.ToArray());
    }
    public static void CheckForDeathOnExile(CustomDeathReason deathReason, params byte[] playerIds)
    {
        foreach (var playerId in playerIds)
        {
            //Loversの後追い
            foreach (var data in ColorLovers.Alldatas.Values)
            {
                if (!data.IsLoversDead && data.LoverPlayer.Find(lp => lp.PlayerId == playerId) != null)
                    data.LoversSuicide(playerId, true);
            }
            if (CustomRoles.MadonnaLovers.IsPresent() && !Lovers.isMadonnaLoversDead && Lovers.MaMadonnaLoversPlayers.Find(lp => lp.PlayerId == playerId) != null)
                Lovers.MadonnLoversSuicide(playerId, true);
            if (CustomRoles.OneLove.IsPresent() && !Lovers.isOneLoveDead)
                Lovers.OneLoveSuicide(playerId, true);
            //道連れチェック
            RevengeOnExile(playerId, deathReason);
        }
    }
    private static void RevengeOnExile(byte playerId, CustomDeathReason deathReason)
    {
        var player = PlayerCatch.GetPlayerById(playerId);
        if (player == null) return;
        var target = PickRevengeTarget(player, deathReason);
        if (target == null) return;
        TryAddAfterMeetingDeathPlayers(CustomDeathReason.Vote, playerId);
        TryAddAfterMeetingDeathPlayers(CustomDeathReason.Revenge, target.PlayerId);
        target.SetRealKiller(player);
        Logger.Info($"{player.GetNameWithRole()}の道連れ先:{target.GetNameWithRole()}", "RevengeOnExile");
    }
    private static PlayerControl PickRevengeTarget(PlayerControl exiledplayer, CustomDeathReason deathReason)//道連れ先選定
    {
        List<PlayerControl> TargetList = new();

        if (deathReason != CustomDeathReason.Vote) return null;

        if (Amnesia.CheckAbility(exiledplayer))
            if (exiledplayer.GetRoleClass() is INekomata nekomata)
            {
                // 道連れしない状態ならnull
                if (!nekomata.DoRevenge(deathReason))
                {
                    return null;
                }
                TargetList = PlayerCatch.AllAlivePlayerControls.Where(candidate => candidate != exiledplayer && !Main.AfterMeetingDeathPlayers.ContainsKey(candidate.PlayerId) && nekomata.IsCandidate(candidate)).ToList();
            }
            else
            {
                var role = exiledplayer.GetCustomRole();
                var isMadmate =
                    role.IsMadmate() ||
                    // マッド属性化時に削除
                    (exiledplayer.GetRoleClass() is SchrodingerCat schrodingerCat && schrodingerCat.AmMadmate);
                foreach (var candidate in PlayerCatch.AllAlivePlayerControls)
                {
                    if (candidate == exiledplayer || Main.AfterMeetingDeathPlayers.ContainsKey(candidate.PlayerId)) continue;
                    switch (role)
                    {
                        // ここにINekomata未適用の道連れ役職を追加
                        default:
                            if (RoleAddAddons.GetRoleAddon(role, out var data, exiledplayer, subrole: CustomRoles.Revenger))
                            {
                                if (deathReason == CustomDeathReason.Vote && data.GiveRevenger.GetBool())
                                {
                                    if ((candidate.Is(CustomRoleTypes.Impostor) && data.RevengeToImpostor.GetBool()) ||
                                        (candidate.Is(CustomRoleTypes.Neutral) && data.RevengeToNeutral.GetBool()) ||
                                        (candidate.Is(CustomRoleTypes.Crewmate) && data.RevengeToCrewmate.GetBool()) ||
                                        (candidate.Is(CustomRoleTypes.Madmate) && data.RevengeToMadmate.GetBool()))
                                        TargetList.Add(candidate);
                                }
                            }
                            else
                                if (isMadmate && deathReason == CustomDeathReason.Vote && Options.MadmateRevengePlayer.GetBool())
                            {
                                if ((candidate.Is(CustomRoleTypes.Impostor) && Options.MadmateRevengeCanImpostor.GetBool()) ||
                                (candidate.Is(CustomRoleTypes.Neutral) && Options.MadmateRevengeNeutral.GetBool()) ||
                                (candidate.Is(CustomRoleTypes.Crewmate) && Options.MadmateRevengeCrewmate.GetBool()) ||
                                (candidate.Is(CustomRoleTypes.Madmate) && Options.MadmateRevengeMadmate.GetBool()))
                                    TargetList.Add(candidate);
                            }
                            else
                                foreach (var subRole in exiledplayer.GetCustomSubRoles())
                                {
                                    switch (subRole)
                                    {
                                        case CustomRoles.Revenger:
                                            if (exiledplayer.Is(CustomRoles.Revenger) && deathReason == CustomDeathReason.Vote)
                                            {
                                                if (
                                                (candidate.Is(CustomRoleTypes.Impostor) && Revenger.RevengeToImpostor.GetBool()) ||
                                                (candidate.Is(CustomRoleTypes.Neutral) && Revenger.RevengeToNeutral.GetBool()) ||
                                                (candidate.Is(CustomRoleTypes.Crewmate) && Revenger.RevengeToCrewmate.GetBool()) ||
                                                (candidate.Is(CustomRoleTypes.Madmate) && Revenger.RevengeToMadmate.GetBool()))
                                                    TargetList.Add(candidate);
                                            }
                                            break;
                                    }
                                }
                            break;
                    }
                }
            }
        if (TargetList == null || TargetList.Count == 0) return null;
        var rand = IRandom.Instance;
        var target = TargetList[rand.Next(TargetList.Count)];
        return target;
    }
}

[HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.SetHighlighted))]
class SetHighlightedPatch
{
    public static bool Prefix(PlayerVoteArea __instance, bool value)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        if (!__instance.HighlightedFX) return false;
        __instance.HighlightedFX.enabled = value;
        return false;
    }
}
