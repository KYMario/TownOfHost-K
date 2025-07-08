using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hazel;
using HarmonyLib;
using UnityEngine;
using AmongUs.Data;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.Crewmate;
using TownOfHost.Roles.Impostor;
using TownOfHost.Roles.Neutral;
using TownOfHost.Roles.AddOns.Common;
using TownOfHost.Modules.ChatManager;

namespace TownOfHost
{

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    class FixedUpdatePatch
    {
        private static StringBuilder Mark = new(20);
        private static StringBuilder Suffix = new(120);
        //public static float test = 13.1f;
        public static float text = 0;
        public static void Postfix(PlayerControl __instance)
        {
            var player = __instance;

            if (Main.EditMode && GameStates.IsFreePlay)
            {
                CustomSpawnEditor.FixedUpdate(__instance);
                return;
            }

            if (!GameStates.IsModHost) return;
            if (__instance == null) return;

            if (Main.RTAMode && GameStates.IsInTask && GameStates.introDestroyed)
            {
                if (Main.RTAPlayer != byte.MaxValue && Main.RTAPlayer == player.PlayerId)
                {
                    HudManagerPatch.LowerInfoText.enabled = true;
                    HudManagerPatch.LowerInfoText.text = HudManagerPatch.GetTaskBattleTimer();
                    if (HudManagerPatch.TaskBattleTimer != 0 || player.MyPhysics.Animations.IsPlayingRunAnimation())
                        HudManagerPatch.TaskBattleTimer += Time.deltaTime;
                }
            }

            TargetArrow.OnFixedUpdate(player);
            GetArrow.OnFixedUpdate(player);

            CustomRoleManager.OnFixedUpdate(player);

            var roleclass = player.GetRoleClass();
            var isAlive = player.IsAlive();
            var roleinfo = __instance.GetCustomRole().GetRoleInfo();

            (roleclass as IUsePhantomButton)?.FixedUpdate(player);

            if (AmongUsClient.Instance.AmHost)
            {//実行クライアントがホストの場合のみ実行
                if (GameStates.IsLobby)
                {
                    //非導入者が遥か彼方へ行かないように。
                    if (!player.IsModClient())
                    {
                        if (player?.transform?.position is null) return;
                        Vector2 c = new(0f, 0f);
                        Vector2 pj = player.transform.position;
                        if (pj.y < -8) player.RpcSnapToForced(c);
                        if (pj.y > 8) player.RpcSnapToForced(c);
                        if (pj.x < -8) player.RpcSnapToForced(c);
                        if (pj.x > 8) player.RpcSnapToForced(c);
                    }
                }
                if (__instance && GameStates.IsInTask)
                {
                    if (ReportDeadBodyPatch.CanReport[__instance.PlayerId] && ReportDeadBodyPatch.WaitReport[__instance.PlayerId].Count > 0)
                    {
                        var info = ReportDeadBodyPatch.WaitReport[__instance.PlayerId][0];
                        ReportDeadBodyPatch.WaitReport[__instance.PlayerId].Clear();
                        Logger.Info($"{__instance.GetNameWithRole().RemoveHtmlTags()}:通報可能になったため通報処理を行います", "ReportDeadbody");
                        __instance.ReportDeadBody(info);
                    }

                    if (ReportDeadBodyPatch.DontReport.TryGetValue(__instance.PlayerId, out var data))
                    {
                        try
                        {
                            var time = data.time += Time.fixedDeltaTime;

                            if (4f <= time)
                            {
                                ReportDeadBodyPatch.DontReport.Remove(__instance.PlayerId);
                                _ = new LateTask(() =>
                                {
                                    if (!GameStates.Meeting) UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: __instance);
                                }, 0.2f, "", true);
                            }
                            else
                                ReportDeadBodyPatch.DontReport[__instance.PlayerId] = (time, data.reason);
                        }
                        catch { Logger.Error($"{__instance.PlayerId}でエラー！", "DontReport"); }
                    }
                    //梯子ぼーんの奴
                    //Q.ジップラインどうするの？
                    //A.しらん。
                    if (isAlive && !((roleclass as Jumper)?.ability == true))
                    {
                        var nowpos = __instance.GetTruePosition();
                        if (!Main.AllPlayerLastkillpos.TryGetValue(__instance.PlayerId, out var tppos))
                            tppos = new Vector2(0, 0);

                        if (!__instance.MyPhysics.Animations.IsPlayingAnyLadderAnimation())
                        {
                            switch ((MapNames)Main.NormalOptions.MapId)
                            {
                                case MapNames.Airship:
                                    if ((4.0 <= nowpos.x && nowpos.x <= 5.2 && 10.1 <= nowpos.y && nowpos.y <= 12.9)
                                    || (10.2 <= nowpos.x && nowpos.x <= 11.7 && 6.9 <= nowpos.y && nowpos.y <= 7.2)
                                    || (12.4 <= nowpos.x && nowpos.x <= 13.4 && -5.4 <= nowpos.y && nowpos.y <= -4.6)
                                    )
                                        __instance.RpcSnapToForced(tppos);
                                    break;
                                case MapNames.Fungle:
                                    if ((10.8 <= nowpos.x && nowpos.x <= 12.4 && -5.3 <= nowpos.y && nowpos.y <= -2.1)
                                    || (17.3 <= nowpos.x && nowpos.x <= 18.9 && -5.0 <= nowpos.y && nowpos.y <= -1.9)
                                    || (18.5 <= nowpos.x && nowpos.x <= 19.8 && 4.8 <= nowpos.y && nowpos.y <= 5.7)
                                    || (21.0 <= nowpos.x && nowpos.x <= 22.1 && 8.1 <= nowpos.y && nowpos.y <= 9.4)
                                    )
                                        __instance.RpcSnapToForced(tppos);
                                    break;
                            }
                        }
                        if (!__instance.inMovingPlat && (MapNames)Main.NormalOptions.MapId == MapNames.Airship)
                        {
                            if (6.3 <= nowpos.x && nowpos.x <= 9.3 && 7.8 <= nowpos.y && nowpos.y <= 9.1)
                                __instance.RpcSnapToForced(tppos);
                        }
                    }
                }

                DoubleTrigger.OnFixedUpdate(player);

                //ターゲットのリセット
                if (GameStates.IsInTask && isAlive && Options.LadderDeath.GetBool())
                {
                    FallFromLadder.FixedUpdate(player);
                }
                VentManager.UpdateDesyncVentCleaning(player, roleclass);

                Utils.ApplySuffix(__instance);
            }
            //LocalPlayer専用
            if (__instance.AmOwner)
            {
                SuddenDeathMode.UpdateTeam();
                if (GameStates.InGame)
                {
                    VentManager.CheckVentLimit(player);
                    DisableDevice.FixedUpdate();
                    //情報機器制限
                    var nowuseing = true;
                    if (DisableDevice.optTimeLimitCamAndLog > 0 && DisableDevice.GameLogAndCamTimer > DisableDevice.optTimeLimitCamAndLog)
                        nowuseing = false;

                    if (DisableDevice.optTurnTimeLimitCamAndLog > 0 && DisableDevice.TurnLogAndCamTimer > DisableDevice.optTurnTimeLimitCamAndLog)
                        nowuseing = false;

                    if (DisableDevice.UseCount > 0)
                    {
                        if (nowuseing)
                        {
                            if (DisableDevice.optTimeLimitDevices)
                                DisableDevice.GameLogAndCamTimer += Time.fixedDeltaTime * DisableDevice.UseCount;
                            if (DisableDevice.optTurnTimeLimitDevice)
                                DisableDevice.TurnLogAndCamTimer += Time.fixedDeltaTime * DisableDevice.UseCount;
                        }
                        else
                        {
                            DisableDevice.UseCount = 0;
                        }
                    }
                    if (Main.NowSabotage)
                    {
                        Main.sabotagetime += Time.fixedDeltaTime;
                        if (!Utils.IsActive(Main.SabotageType))
                        {
                            var sb = Translator.GetString($"sb.{Main.SabotageType}");

                            if (Main.SabotageType == SystemTypes.MushroomMixupSabotage)
                                UtilsGameLog.AddGameLog($"MushroomMixup", string.Format(Translator.GetString("Log.FixSab"), sb));
                            else UtilsGameLog.AddGameLog($"{Main.SabotageType}", string.Format(Translator.GetString("Log.FixSab"), sb));
                            Main.NowSabotage = false;
                            Main.sabotagetime = 0;

                            foreach (var role in CustomRoleManager.AllActiveRoles.Values)
                            {
                                role.AfterSabotage(Main.SabotageType);
                            }
                        }
                    }

                    ChatManager.IntaskCheckSendMessage(player);
                    if (!GameStates.IsMeeting)
                    {
                        foreach (var data in ColorLovers.Alldatas.Values)
                        {
                            data.LoversSuicide();
                        }
                        Lovers.MadonnLoversSuicide();
                        Lovers.OneLoveSuicide();
                    }
                    //サドンデスモード
                    if (SuddenDeathMode.NowSuddenDeathMode)
                    {
                        if (Options.SuddenDeathTimeLimit.GetFloat() > 0) SuddenDeathMode.SuddenDeathReactor();
                        if (Options.SuddenItijohoSend.GetBool()) SuddenDeathMode.ItijohoSend();
                    }

                    //ネームカラー
                    if (!(__instance.Is(CustomRoleTypes.Impostor) || __instance.Is(CustomRoles.Egoist)) && (roleinfo?.IsDesyncImpostor ?? false) && !__instance.Data.IsDead)
                        foreach (var p in PlayerCatch.AllPlayerControls)
                        {
                            if (!p || (p?.Data == null)) continue;
                            p.Data.Role.NameColor = Color.white;
                        }

                    //カモフラ
                    if (Camouflage.ventplayr.Count > 0)
                    {
                        var remove = new List<byte>();
                        foreach (var id in Camouflage.ventplayr)
                        {
                            var target = PlayerCatch.GetPlayerById(id);
                            if (target.inVent) continue;

                            if (Camouflage.IsCamouflage)
                            {
                                var sender = CustomRpcSender.Create(name: $"Camouflage.RpcSetSkin({target.Data.GetLogPlayerName()})");
                                byte color = (byte)ModColors.PlayerColor.Gray;

                                target.SetColor(color);
                                sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetColor)
                                    .Write(target.Data.NetId)
                                    .Write(color)
                                    .EndRpc();

                                target.SetHat("", color);
                                sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetHatStr)
                                    .Write("")
                                    .Write(target.GetNextRpcSequenceId(RpcCalls.SetHatStr))
                                    .EndRpc();

                                target.SetSkin("", color);
                                sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetSkinStr)
                                    .Write("")
                                    .Write(target.GetNextRpcSequenceId(RpcCalls.SetSkinStr))
                                    .EndRpc();

                                target.SetVisor("", color);
                                sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetVisorStr)
                                    .Write("")
                                    .Write(target.GetNextRpcSequenceId(RpcCalls.SetVisorStr))
                                    .EndRpc();
                                sender.SendMessage();
                            }
                            else Camouflage.RpcSetSkin(target);

                            remove.Add(id);
                        }

                        if (remove.Count > 0)
                        {
                            ExtendedPlayerControl.AllPlayerOnlySeeMePet();
                            remove.ForEach(task => Camouflage.ventplayr.Remove(task));
                        }
                    }
                }

                var kiruta = GameStates.IsInTask && !GameStates.Intro && __instance.Is(CustomRoles.Amnesiac) && !(roleclass as Amnesiac).omoidasita;
                //キルターゲットの上書き処理
                if (GameStates.IsInTask && !GameStates.Intro && ((!(__instance.Is(CustomRoleTypes.Impostor) || __instance.Is(CustomRoles.Egoist)) && (roleinfo?.IsDesyncImpostor ?? false)) || kiruta) && !__instance.Data.IsDead)
                {
                    var target = __instance.killtarget();
                    if (!__instance.CanUseKillButton()) target = null;
                    HudManager.Instance.KillButton.SetTarget(target);
                }
            }

            if (GameStates.InGame && !GameStates.Intro)
            {
                if (PlayerCatch.AllPlayerControls.Any(pc => pc.Is(CustomRoles.Monochromer)))
                {
                    if (Camouflage.PlayerSkins.TryGetValue(__instance.PlayerId, out var outfit))
                    {
                        __instance.Data.DefaultOutfit.ColorId = outfit.ColorId;
                        __instance.Data.DefaultOutfit.HatId = outfit.HatId;
                        __instance.Data.DefaultOutfit.SkinId = outfit.SkinId;
                        __instance.Data.DefaultOutfit.VisorId = outfit.VisorId;
                    }
                }
            }

            //役職テキストの表示
            var RoleTextTransform = __instance.cosmetics.nameText.transform.Find("RoleText");
            var RoleText = RoleTextTransform.GetComponent<TMPro.TextMeshPro>();
            if (RoleText != null && __instance != null)
            {
                if (GameStates.IsLobby)
                {
                    if (PlayerControl.LocalPlayer.PlayerId == __instance.PlayerId)
                        PlayerControl.LocalPlayer.cosmetics.nameText.text = Main.lobbyname == "" ? DataManager.player.Customization.Name : Main.lobbyname;
                    if (Main.playerVersion.TryGetValue(__instance.PlayerId, out var ver))
                    {
                        if (Main.ForkId != ver.forkId) // フォークIDが違う場合
                            __instance.cosmetics.nameText.text = $"<#f07d0d><size=1.2>{ver.forkId}</size>\n{__instance?.name}</color>";
                        else if (Main.version.CompareTo(ver.version) == 0)
                            __instance.cosmetics.nameText.text = ver.tag == $"{ThisAssembly.Git.Commit}({ThisAssembly.Git.Branch})" ? $"<#a1b0ff>{__instance.name}</color>" : $"<#ffff00><size=1.2>{ver.tag}</size>\n{__instance?.name}</color>";
                        else __instance.cosmetics.nameText.text = $"<#ff0000><size=1.2>v{ver.version}</size>\n{__instance?.name}</color>";
                    }
                    else __instance.cosmetics.nameText.text = __instance?.Data?.PlayerName;

                    if (Options.SuddenTeamOption.GetBool())
                    {
                        var color = "#ffffff";
                        if (SuddenDeathMode.TeamRed.Contains(__instance.PlayerId)) color = ModColors.codered;
                        if (SuddenDeathMode.TeamBlue.Contains(__instance.PlayerId)) color = ModColors.codeblue;
                        if (SuddenDeathMode.TeamYellow.Contains(__instance.PlayerId)) color = ModColors.codeyellow;
                        if (SuddenDeathMode.TeamGreen.Contains(__instance.PlayerId)) color = ModColors.codegreen;
                        if (SuddenDeathMode.TeamPurple.Contains(__instance.PlayerId)) color = ModColors.codepurple;

                        __instance.cosmetics.nameText.text = $"{__instance.cosmetics.nameText.text}<color={color}>★</color>";
                    }

                    var client = __instance.GetClient();
                    if (BanManager.CheckWhiteList(client?.FriendCode, client?.ProductUserId))
                        __instance.cosmetics.nameText.text = "<#feffe2>◎</color>" + __instance.cosmetics.nameText.text;
                }
                if (GameStates.IsInGame)
                {
                    (RoleText.enabled, RoleText.text) = UtilsRoleText.GetRoleNameAndProgressTextData(PlayerControl.LocalPlayer, __instance, PlayerControl.LocalPlayer == __instance);
                    if (!AmongUsClient.Instance.IsGameStarted && AmongUsClient.Instance.NetworkMode != NetworkModes.FreePlay)
                    {
                        RoleText.enabled = false; //ゲームが始まっておらずフリープレイでなければロールを非表示
                        if (!__instance.AmOwner) __instance.cosmetics.nameText.text = __instance?.Data?.PlayerName;
                    }

                    //変数定義
                    var seer = PlayerControl.LocalPlayer;
                    var seerRole = seer.GetRoleClass();
                    var seerSubrole = seer.GetCustomSubRoles();
                    var target = __instance;
                    string name = "";
                    bool nomarker = false;
                    string RealName;
                    Mark.Clear();
                    Suffix.Clear();

                    //見る側が双子で相方が双子の場合
                    if (Twins.TwinsList.TryGetValue(PlayerControl.LocalPlayer.PlayerId, out var targetid) && seer.IsAlive())
                    {
                        if (targetid == target.PlayerId) RoleText.text = UtilsRoleText.GetRoleColorAndtext(CustomRoles.Twins) + RoleText.text;
                    }

                    //名前を一時的に上書きするかのチェック
                    var TemporaryName = roleclass?.GetTemporaryName(ref name, ref nomarker, seer, target) ?? false;

                    //名前変更
                    RealName = TemporaryName ? name : target.GetRealName();

                    //NameColorManager準拠の処理
                    RealName = RealName.ApplyNameColorData(seer, target, false);

                    //seerに関わらず発動するMark
                    Mark.Append(CustomRoleManager.GetMarkOthers(seer, target, false));

                    if (Options.CurrentGameMode == CustomGameMode.TaskBattle)
                    {
                        //タスクバトルのマーク処理
                        TaskBattle.GetMark(target, seer, ref Mark);
                    }
                    else
                    {
                        var targetlover = target.GetRiaju();
                        var seerisonelover = seerSubrole.Contains(CustomRoles.OneLove);
                        //ハートマークを付ける(会議中MOD視点)
                        if ((targetlover == seer.GetRiaju() && targetlover is not CustomRoles.OneLove and not CustomRoles.NotAssigned)
                        || (seer.Data.IsDead && target.IsRiaju() && targetlover != CustomRoles.OneLove))
                        {
                            Mark.Append(Utils.ColorString(UtilsRoleText.GetRoleColor(targetlover), "♥"));
                        }
                        else
                        if ((Lovers.OneLovePlayer.Ltarget == target.PlayerId && target.PlayerId != seer.PlayerId && seerisonelover)
                        || (target.Is(CustomRoles.OneLove) && target.PlayerId != seer.PlayerId && seerisonelover)
                        || (seer.Data.IsDead && target.Is(CustomRoles.OneLove) && !seerisonelover))
                        {
                            Mark.Append(Utils.ColorString(UtilsRoleText.GetRoleColor(CustomRoles.OneLove), "♡"));
                        }

                        if ((target.Is(CustomRoles.Connecting) && seerSubrole.Contains(CustomRoles.Connecting)
                        && !target.Is(CustomRoles.WolfBoy) && seerRole is not WolfBoy)
                        || (target.Is(CustomRoles.Connecting) && seer.Data.IsDead))
                        {
                            Mark.Append($"<color={UtilsRoleText.GetRoleColorCode(CustomRoles.Connecting)}>Ψ</color>");
                        }

                        //seerに関わらず発動するLowerText
                        Suffix.Append(CustomRoleManager.GetLowerTextOthers(seer, target));
                        //追放者
                        if (Options.CanseeVoteresult.GetBool() && MeetingVoteManager.Voteresult != "" && seer.PlayerId == target.PlayerId)
                        {
                            Suffix.Append("<#ffffff><size=75%>" + MeetingVoteManager.Voteresult + "</color></size>");
                        }
                        //seer役職が対象のSuffix
                        if (Amnesia.CheckAbility(player))
                        {
                            Mark.Append(seerRole?.GetMark(seer, target, false));

                            if (target.Is(CustomRoles.Workhorse))
                            {
                                if (((seerRole as Alien)?.modeProgresskiller == true && Alien.ProgressWorkhorseseen)
                                || ((seerRole as JackalAlien)?.modeProgresskiller == true && JackalAlien.ProgressWorkhorseseen)
                                || ((seerRole is ProgressKiller) && ProgressKiller.ProgressWorkhorseseen)
                                || ((seerRole as AlienHijack)?.modeProgresskiller == true && Alien.ProgressWorkhorseseen))
                                {
                                    Mark.Append($"<color=blue>♦</color>");
                                }
                            }
                            Suffix.Append(seerRole?.GetSuffix(seer, target));
                        }

                        //seerに関わらず発動するSuffix
                        Suffix.Append(CustomRoleManager.GetSuffixOthers(seer, target));

                        if ((Utils.IsActive(SystemTypes.Comms) && Options.CommsCamouflage.GetBool())
                        || (seer.Is(CustomRoles.Monochromer) && seer.IsAlive())
                        || Camouflager.NowUse
                        || (SuddenDeathMode.SuddenCannotSeeName && !TemporaryName))
                            RealName = $"<size=0>{RealName}</size> ";
                    }
                    bool? canseedeathreasoncolor = seer.PlayerId.CanDeathReasonKillerColor() == true ? true : null;
                    string DeathReason = seer.Data.IsDead && seer.KnowDeathReason(target) ? $"<size=75%>({Utils.GetVitalText(target.PlayerId, canseedeathreasoncolor)})</size>" : "";

                    //Mark・Suffixの適用
                    if (!seer.GetCustomRole().GetRoleInfo()?.IsDesyncImpostor ?? false)
                        target.cosmetics.nameText.text = $"{RealName}{((TemporaryName && nomarker) ? "" : DeathReason + Mark)}";
                    else
                        target.cosmetics.nameText.text = $"<#ffffff>{RealName}{((TemporaryName && nomarker) ? "" : DeathReason + Mark)}</color>";

                    if (Suffix.ToString() != "" && (!TemporaryName || (TemporaryName && !nomarker)))
                    {
                        //名前が2行になると役職テキストを上にずらす必要がある
                        RoleText.transform.SetLocalY(0.35f);
                        target.cosmetics.nameText.text += "\r\n" + Suffix.ToString();
                    }
                    else
                    {
                        //役職テキストの座標を初期値に戻す
                        RoleText.transform.SetLocalY(0.2f);
                    }
                }
                else
                {
                    //役職テキストの座標を初期値に戻す
                    RoleText.transform.SetLocalY(0.2f);
                }
            }
        }
    }
}