using System.Text;
using System.Linq;
using System.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;
using HarmonyLib;
using UnityEngine;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Impostor;
using TownOfHost.Roles.Neutral;
using TownOfHost.Roles.AddOns.Common;
using TownOfHost.Roles.AddOns.Neutral;
using TownOfHost.Roles.AddOns.Impostor;
using TownOfHost.Roles.Core.Interfaces;

using static TownOfHost.Utils;
using static TownOfHost.Translator;
using static TownOfHost.RandomSpawn;
using static TownOfHost.UtilsRoleText;

namespace TownOfHost
{
    public static class UtilsNotifyRoles
    {
        private static StringBuilder SelfMark = new(20);
        private static StringBuilder SelfSuffix = new(20);
        private static StringBuilder TargetMark = new(20);
        private static StringBuilder TargetSuffix = new(20);

        public static bool NowSend;
        /// <summary>
        /// タスクターン中の役職名の変更
        /// </summary>
        /// <param name="NoCache">前の変更を参照せず、強制的にRpcをぶっぱなすか</param>
        /// <param name="ForceLoop">他視点の名前も変更するか</param>
        /// <param name="OnlyMeName">自視点の名前のみ変更するか</param>
        /// <param name="SpecifySeer">ここにぶち込まれたplayer視点でのみ名前を変える</param>
        public static void NotifyRoles(bool NoCache = false, bool ForceLoop = false, bool OnlyMeName = false, params PlayerControl[] SpecifySeer)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (PlayerCatch.AllPlayerControls == null) return;

            //ミーティング中の呼び出しは不正
            if (GameStates.IsMeeting) return;

            if (GameStates.IsLobby) return;

            if (Main.introDestroyed)
                foreach (var pp in PlayerCatch.AllPlayerControls)
                {
                    var str = GetProgressText(pp.PlayerId, Mane: false, gamelog: true);
                    str = Regex.Replace(str, "ffffff", "000000");
                    if (!UtilsGameLog.LastLogPro.TryAdd(pp.PlayerId, str))
                        UtilsGameLog.LastLogPro[pp.PlayerId] = str;

                    if (Options.CurrentGameMode == CustomGameMode.Standard)
                    {
                        var mark = GetSubRolesText(pp.PlayerId, mark: true);
                        if (!UtilsGameLog.LastLogSubRole.TryAdd(pp.PlayerId, mark))
                            UtilsGameLog.LastLogSubRole[pp.PlayerId] = mark;
                    }
                }
            if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default && !Main.DontGameSet) return;
            var caller = new StackFrame(1, false);
            var callerMethod = caller?.GetMethod();
            string callerMethodName = callerMethod?.Name ?? "ぬーるっ!!";
            string callerClassName = callerMethod?.DeclaringType?.FullName ?? "null!!";
            Logger.Info($"Call :{callerClassName}.{callerMethodName}", "NotifyRoles");
            HudManagerPatch.NowCallNotifyRolesCount++;
            HudManagerPatch.LastSetNameDesyncCount = 0;

            var seerList = PlayerControl.AllPlayerControls;

            //誰かは突っ込まれてて、なおかつぬるぽじゃない
            if (SpecifySeer?.Count() is not 0 and not null)
            {
                seerList = new();
                SpecifySeer.Do(pc => seerList.Add(pc));
            }

            var isMushroomMixupActive = IsActive(SystemTypes.MushroomMixupSabotage);
            //seer:ここで行われた変更を見ることができるプレイヤー
            //target:seerが見ることができる変更の対象となるプレイヤー
            foreach (var seer in seerList)
            {
                //seerが落ちているときに何もしない
                if (seer == null || seer.Data.Disconnected) continue;

                if (seer.IsModClient()) continue;
                var clientId = seer.GetClientId();
                if (clientId == -1) continue;
                var sender = CustomRpcSender.Create("NotifyRoles");
                sender.StartMessage(clientId);
                string fontSize = Main.RoleTextSize.ToString();
                Logger.Info($"{seer?.Data?.PlayerName} (Me)", "NotifyRoles");

                var role = seer.GetCustomRole();
                var seerRole = seer.GetRoleClass();
                var seerRoleInfo = seer.GetCustomRole().GetRoleInfo();
                var seerSubrole = seer.GetCustomSubRoles();
                var seerisAlive = seer.IsAlive();
                var Amnesiacheck = Amnesia.CheckAbility(seer);
                var seercone = seer.Is(CustomRoles.Connecting);
                var jikaku = seerRole?.Jikaku() is CustomRoles.NotAssigned;
                RoleAddAddons.GetRoleAddon(role, out var data, seer, subrole: [CustomRoles.Guesser]);
                // 会議じゃなくて，キノコカオス中で，seerが生きていてdesyncインポスターの場合に自身の名前を消す
                if (isMushroomMixupActive && seerisAlive && !role.IsImpostor() && seerRoleInfo?.IsDesyncImpostor == true)
                {
                    if (seer.SetNameCheck("<size=0>", seer, NoCache))
                    {
                        sender.StartRpc(seer.NetId, (byte)RpcCalls.SetName)
                        .Write(seer.NetId)
                        .Write("<size=0>")
                        .Write(true)
                        .EndRpc();
                    }
                }
                else
                {
                    //名前の後ろに付けるマーカー
                    SelfMark.Clear();

                    //seerの名前を一時的に上書きするかのチェック
                    string name = ""; bool nomarker = false;
                    var TemporaryName = seerRole?.GetTemporaryName(ref name, ref nomarker, seer);

                    //seer役職が対象のMark
                    if (Amnesiacheck && jikaku)
                        SelfMark.Append(seerRole?.GetMark(seer, isForMeeting: false) ?? "");

                    //seerに関わらず発動するMark
                    SelfMark.Append(CustomRoleManager.GetMarkOthers(seer, isForMeeting: false));

                    //ハートマークを付ける(自分に)
                    var lover = seer.GetRiaju();
                    if (lover is not CustomRoles.NotAssigned and not CustomRoles.OneLove) SelfMark.Append(ColorString(GetRoleColor(lover), "♥"));

                    if ((seercone && role is not CustomRoles.WolfBoy)
                    || (seercone && !seerisAlive)) SelfMark.Append(ColorString(GetRoleColor(CustomRoles.Connecting), "Ψ"));

                    if (Options.CurrentGameMode == CustomGameMode.TaskBattle)
                    {
                        TaskBattle.GetMark(seer, null, ref SelfMark);
                    }
                    //Markとは違い、改行してから追記されます。
                    SelfSuffix.Clear();

                    //seer役職が対象のLowerText
                    if (Amnesiacheck && jikaku)
                        SelfSuffix.Append(seerRole?.GetLowerText(seer, isForMeeting: false) ?? "");
                    //seerに関わらず発動するLowerText
                    SelfSuffix.Append(CustomRoleManager.GetLowerTextOthers(seer, isForMeeting: false));
                    //追放者
                    if (Options.CanseeVoteresult.GetBool() && MeetingVoteManager.Voteresult != "")
                    {
                        if (SelfSuffix.ToString() != "") SelfSuffix.Append('\n');
                        SelfSuffix.Append("<color=#ffffff><size=75%>" + MeetingVoteManager.Voteresult + "</color></size>");
                    }
                    //seer役職が対象のSuffix
                    if (Amnesiacheck)
                        SelfSuffix.Append(seerRole?.GetSuffix(seer, isForMeeting: false) ?? "");
                    //seerに関わらず発動するSuffix
                    SelfSuffix.Append(CustomRoleManager.GetSuffixOthers(seer, isForMeeting: false));

                    //RealNameを取得 なければ現在の名前をRealNamesに書き込む
                    string SeerRealName = (seerRole is IUseTheShButton) ? Main.AllPlayerNames[seer.PlayerId] : seer.GetRealName(false);

                    if (SuddenDeathMode.SuddenCannotSeeName)
                    {
                        SeerRealName = "";
                    }

                    if (TemporaryName ?? false)
                        SeerRealName = name;

                    if (MeetingStates.FirstMeeting && (Options.ChangeNameToRoleInfo.GetBool() || SuddenDeathMode.NowSuddenDeathMode) && Main.IntroHyoji)
                        SeerRealName = seer?.GetRoleInfo() ?? "";

                    var colorName = SeerRealName.ApplyNameColorData(seer, seer, false);

                    //seerの役職名とSelfTaskTextとseerのプレイヤー名とSelfMarkを合成
                    var (enabled, text) = GetRoleNameAndProgressTextData(seer);
                    string SelfRoleName = enabled ? $"<size={fontSize}>{text}</size>" : "";
                    string SelfDeathReason = ((TemporaryName ?? false) && nomarker) ? "" : seer.KnowDeathReason(seer) ? $"<size=75%>({ColorString(GetRoleColor(CustomRoles.Doctor), GetVitalText(seer.PlayerId, seer.PlayerId.CanDeathReasonKillerColor()))})</size>" : "";
                    string SelfName = $"{colorName}{SelfDeathReason}{(((TemporaryName ?? false) && nomarker) ? "" : SelfMark)}";
                    SelfName = SelfRoleName + "\r\n" + SelfName;
                    var g = "<line-height=85%>";
                    SelfName += SelfSuffix.ToString() == "" ? "" : (g + "\r\n " + SelfSuffix.ToString() + "</line-height>");
                    SelfName = "<line-height=85%>" + SelfName + "\r\n";

                    //適用
                    if (seer.SetNameCheck(SelfName, seer, NoCache))
                    {
                        sender.StartRpc(seer.NetId, (byte)RpcCalls.SetName)
                        .Write(seer.NetId)
                        .Write(SelfName)
                        .Write(true)
                        .EndRpc();
                    }
                    if (OnlyMeName)
                    {
                        sender.EndMessage();
                        sender.SendMessage();
                        continue;
                    }
                }
                var rolech = seerRole?.NotifyRolesCheckOtherName ?? false;

                //seerが死んでいる場合など、必要なときのみ第二ループを実行する
                if (seer.Data.IsDead //seerが死んでいる
                    || role.IsImpostor() //seerがインポスター
                    || seer.IsNeutralKiller() //seerがキル出来るニュートラル
                    || PlayerState.GetByPlayerId(seer.PlayerId).TargetColorData.Count > 0 //seer視点用の名前色データが一つ以上ある
                    || Witch.IsSpelled()
                    || seer.IsRiaju()
                    || seercone
                    || IsActive(SystemTypes.Electrical)
                    || IsActive(SystemTypes.Comms)
                    || isMushroomMixupActive
                    || rolech
                    || Options.CurrentGameMode == CustomGameMode.TaskBattle
                    //|| (seerRole is IUseTheShButton) //ﾜﾝｸﾘｯｸシェイプボタン持ち
                    || NoCache
                    || ForceLoop
                )
                {
                    //死者じゃない場合タスクターン中、霊の名前を変更する必要がないので少しでも処理を減らす敵な感じで
                    var ForeachList = !seerisAlive ? PlayerCatch.AllPlayerControls : PlayerCatch.OldAlivePlayerControles;
                    foreach (var target in ForeachList.Where(pc => pc?.PlayerId != seer.PlayerId))
                    {
                        //targetがseer自身の場合は何もしない
                        if (target?.PlayerId == seer?.PlayerId || target == null) continue;

                        Logger.Info($"{seer?.Data?.PlayerName ?? "null"} => {target?.Data?.PlayerName ?? "null"}", "NotifyRoles");
                        var targetisalive = target.IsAlive();

                        // 会議じゃなくて，キノコカオス中で，targetが生きていてseerがdesyncインポスターの場合にtargetの名前を消す
                        if (isMushroomMixupActive && targetisalive && !role.IsImpostor() && seerRoleInfo?.IsDesyncImpostor == true)
                        {
                            if (target.SetNameCheck("<size=0>", seer, NoCache))
                            {
                                sender.StartRpc(target.NetId, (byte)RpcCalls.SetName)
                                .Write(target.NetId)
                                .Write("<size=0>")
                                .Write(true)
                                .EndRpc();
                            }
                        }
                        else
                        {
                            var targetrole = target.GetRoleClass();
                            //名前の後ろに付けるマーカー
                            TargetMark.Clear();

                            /// targetの名前を一時的に上書きするかのチェック
                            string name = ""; bool nomarker = false;
                            var TemporaryName = targetrole?.GetTemporaryName(ref name, ref nomarker, seer, target) ?? false;

                            //seerに関わらず発動するMark
                            TargetMark.Append(CustomRoleManager.GetMarkOthers(seer, target, false));

                            //ハートマークを付ける(相手に)
                            var seerri = seer.GetRiaju();
                            var tageri = target.GetRiaju();
                            var seerisone = seerSubrole.Contains(CustomRoles.OneLove);
                            var targetsubrole = target.GetCustomSubRoles();
                            var seenIsOne = targetsubrole.Contains(CustomRoles.OneLove);
                            if (seerri == tageri && seer.IsRiaju() && !seerisone)
                                TargetMark.Append(ColorString(GetRoleColor(seerri), "♥"));
                            else if (seer.Data.IsDead && !seer.Is(tageri) && tageri != CustomRoles.NotAssigned && !seerisone)
                                TargetMark.Append(ColorString(GetRoleColor(tageri), "♥"));

                            if ((seerisone && seenIsOne)
                            || (seer.Data.IsDead && !seerisone && seenIsOne)
                            || (seerisone && target.PlayerId == Lovers.OneLovePlayer.Ltarget)
                            )
                                TargetMark.Append("<color=#ff7961>♡</color>");

                            if (seercone && targetsubrole.Contains(CustomRoles.Connecting) && (role is not CustomRoles.WolfBoy || !seerisAlive)
                            || (seer.Data.IsDead && !seercone && targetsubrole.Contains(CustomRoles.Connecting))
                            ) //狼少年じゃないか死亡なら処理
                                TargetMark.Append($"<color=#96514d>Ψ</color>");

                            if (Options.CurrentGameMode == CustomGameMode.TaskBattle)
                                TaskBattle.GetMark(target, seer, ref TargetMark);

                            //インサイダーモードタスク表示
                            if (Options.Taskcheck.GetBool())
                            {
                                if (target.GetPlayerTaskState() != null && target.GetPlayerTaskState().AllTasksCount > 0)
                                {
                                    if (role.IsImpostor())
                                    {
                                        TargetMark.Append($"<color=yellow>({target.GetPlayerTaskState().CompletedTasksCount}/{target.GetPlayerTaskState().GetNeedCountOrAll()})</color>");
                                    }
                                }
                            }

                            //他人の役職とタスクは幽霊が他人の役職を見れるようになっていてかつ、seerが死んでいる場合のみ表示されます。それ以外の場合は空になります。
                            var targetRoleData = GetRoleNameAndProgressTextData(seer, target, false);
                            var TargetRoleText = targetRoleData.enabled ? $"<size={fontSize}>{targetRoleData.text}</size>\r\n" : $"";

                            TargetSuffix.Clear();
                            //seerに関わらず発動するLowerText
                            TargetSuffix.Append(CustomRoleManager.GetLowerTextOthers(seer, target, isForMeeting: false));

                            //seerに関わらず発動するSuffix
                            TargetSuffix.Append(CustomRoleManager.GetSuffixOthers(seer, target, isForMeeting: false));
                            // 空でなければ先頭に改行を挿入
                            if (TargetSuffix.Length > 0)
                                TargetSuffix.Insert(0, "\r\n");

                            if (Amnesiacheck)
                            {
                                //seer役職が対象のMark
                                TargetMark.Append(seerRole?.GetMark(seer, target, false) ?? "");
                                TargetSuffix.Append(seerRole?.GetSuffix(seer, target, isForMeeting: false) ?? "");

                                if (targetsubrole.Contains(CustomRoles.Workhorse))
                                {
                                    if (((seerRole as Alien)?.modeProgresskiller == true && Alien.ProgressWorkhorseseen)
                                    || ((seerRole as JackalAlien)?.modeProgresskiller == true && JackalAlien.ProgressWorkhorseseen)
                                    || (role is CustomRoles.ProgressKiller && ProgressKiller.ProgressWorkhorseseen)
                                    || ((seerRole as AlienHijack)?.modeProgresskiller == true && Alien.ProgressWorkhorseseen))
                                    {
                                        TargetMark.Append($"<color=blue>♦</color>");
                                    }
                                }
                            }

                            //RealNameを取得 なければ現在の名前をRealNamesに書き込む
                            string TargetPlayerName = (seerRole is IUseTheShButton) ? Main.AllPlayerNames[target.PlayerId] : target.GetRealName(false);

                            //ターゲットのプレイヤー名の色を書き換えます。
                            TargetPlayerName = TargetPlayerName.ApplyNameColorData(seer, target, false);

                            string TargetDeathReason = "";
                            if (seer.KnowDeathReason(target))
                                TargetDeathReason = $"<size=75%>({GetVitalText(target.PlayerId, seer.PlayerId.CanDeathReasonKillerColor() == true ? true : null)})</size>";

                            if (((IsActive(SystemTypes.Comms) && Options.CommsCamouflage.GetBool())
                            || (role is CustomRoles.Monochromer && seerisAlive)
                            || Camouflager.NowUse
                            || (SuddenDeathMode.SuddenCannotSeeName && !TemporaryName))
                            && (!((targetrole as Jumper)?.ability == true)))
                            {
                                TargetPlayerName = $"<size=0>{TargetPlayerName}</size> ";
                                name = $"<size=0>{name}</color>";
                            }
                            //全てのテキストを合成します。
                            var g = string.Format("<line-height={0}%>", "85");
                            string TargetName = $"{g}{TargetRoleText}{(TemporaryName ? name : TargetPlayerName)}{((TemporaryName && nomarker) ? "" : TargetDeathReason + TargetMark + TargetSuffix)}</line-height>";
                            if (!seerisAlive && !((targetrole as Jumper)?.ability == true))
                                TargetName = $"<size=65%><line-height=85%><line-height=-18%>\n</line-height>{TargetRoleText.RemoveSizeTags()}</size><size=70%><line-height=-17%>\n</line-height>{(TemporaryName ? name.RemoveSizeTags() : TargetPlayerName.RemoveSizeTags())}{((TemporaryName && nomarker) ? "" : TargetDeathReason.RemoveSizeTags() + TargetMark.ToString().RemoveSizeTags() + TargetSuffix.ToString().RemoveSizeTags())}";

                            //適用
                            if (target.SetNameCheck(TargetName, seer, NoCache))
                            {
                                sender.StartRpc(target.NetId, (byte)RpcCalls.SetName)
                                .Write(target.NetId)
                                .Write(TargetName)
                                .Write(true)
                                .EndRpc();
                            }
                        }
                    }
                }
                {
                    sender.EndMessage();
                    sender.SendMessage();
                }
            }
        }
        #region NotifyMeeting
        public static void NotifyMeetingRoles()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (PlayerCatch.AllPlayerControls == null) return;

            //ミーティング中の呼び出しは不正
            if (GameStates.IsMeeting) return;

            if (GameStates.IsLobby) return;

            if (Main.introDestroyed)
                foreach (var pp in PlayerCatch.AllPlayerControls)
                {
                    var str = GetProgressText(pp.PlayerId, Mane: false, gamelog: true);
                    str = Regex.Replace(str, "ffffff", "000000");
                    if (!UtilsGameLog.LastLogPro.TryAdd(pp.PlayerId, str))
                        UtilsGameLog.LastLogPro[pp.PlayerId] = str;

                    if (Options.CurrentGameMode == CustomGameMode.Standard)
                    {
                        var mark = GetSubRolesText(pp.PlayerId, mark: true);
                        if (!UtilsGameLog.LastLogSubRole.TryAdd(pp.PlayerId, mark))
                            UtilsGameLog.LastLogSubRole[pp.PlayerId] = mark;
                    }
                }
            if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default && !Main.DontGameSet) return;

            /* 会議拡張の奴 */
            var Info = $" <color=#ffffff><size=1.5f>\n\n</size><line-height=0%><color={Main.ModColor}>TownOfHost-K\t\t  <size=60%>　</size>\n　　\t\t</color><size=70%>";
            Info += $"v{Main.PluginShowVersion}</size>\n　</line-height></color><line-height=50%>\n</line-height><line-height=95%>";
            Info += $"Day.{UtilsGameLog.day}".Color(Palette.Orange) + $"\n{MeetingMoji}<line-height=0%>\n</line-height></line-height><line-height=250%>\n</line-height></color>";
            var TInfo = $" <color=#ffffff><size=1.5f>\n\n</size><line-height=0%><color={Main.ModColor}>TownOfHost-K\t\t  <size=60%>　</size>\n　　\t\t</color><size=70%>";

            var IInfo = $"v{Main.PluginShowVersion}</size>\n　</line-height></color><line-height=50%>\n</line-height><line-height=95%>";
            IInfo += $"Day.{UtilsGameLog.day}".Color(Palette.Orange) + $"\n{MeetingMoji}<line-height=0%>\n</line-height></line-height><line-height=330%>\n</line-height></color> ";

            TInfo += IInfo;
            var Finfo = $" <size=0.9f>\n</size><color=#ffffff><size=1.5f>\n\n</size><line-height=0%><color={Main.ModColor}>TownOfHost-K\t\t  <size=60%>　</size>\n　　\t\t</color><size=70%>";
            Finfo += IInfo;

            //seer:ここで行われた変更を見ることができるプレイヤー
            //target:seerが見ることができる変更の対象となるプレイヤー
            foreach (var seer in PlayerControl.AllPlayerControls)
            {
                //seerが落ちているときに何もしない
                if (seer == null || seer.Data.Disconnected) continue;

                if (seer.IsModClient()) continue;
                var clientid = seer.GetClientId();
                if (clientid == -1) continue;

                var sender = CustomRpcSender.Create("MeetingNotifyRoles");
                sender.StartMessage(clientid);

                string fontSize = "1.5";
                if (seer.GetClient()?.PlatformData?.Platform is Platforms.Playstation or Platforms.Switch) fontSize = "70%";

                var role = seer.GetCustomRole();
                var seerRole = seer.GetRoleClass();
                var seerRoleInfo = seer.GetCustomRole().GetRoleInfo();
                var seerSubrole = seer.GetCustomSubRoles();
                var seerisAlive = seer.IsAlive();
                var Amnesiacheck = Amnesia.CheckAbility(seer);
                var seercone = seer.Is(CustomRoles.Connecting);
                var jikaku = seerRole?.Jikaku() is CustomRoles.NotAssigned;
                RoleAddAddons.GetRoleAddon(role, out var data, seer, subrole: [CustomRoles.Guesser]);

                {
                    //名前の後ろに付けるマーカー
                    SelfMark.Clear();

                    //seerの名前を一時的に上書きするかのチェック
                    string name = ""; bool nomarker = false;
                    var TemporaryName = seerRole?.GetTemporaryName(ref name, ref nomarker, seer);

                    //seer役職が対象のMark
                    if (Amnesiacheck && jikaku)
                        SelfMark.Append(seerRole?.GetMark(seer, isForMeeting: true) ?? "");

                    //seerに関わらず発動するMark
                    SelfMark.Append(CustomRoleManager.GetMarkOthers(seer, isForMeeting: true));

                    //ハートマークを付ける(自分に)
                    var lover = seer.GetRiaju();
                    if (lover is not CustomRoles.NotAssigned and not CustomRoles.OneLove) SelfMark.Append(ColorString(GetRoleColor(lover), "♥"));

                    if ((seercone && role is not CustomRoles.WolfBoy)
                    || (seercone && !seerisAlive)) SelfMark.Append(ColorString(GetRoleColor(CustomRoles.Connecting), "Ψ"));

                    //Markとは違い、改行してから追記されます。
                    SelfSuffix.Clear();

                    //seer役職が対象のLowerText
                    if (Amnesiacheck && jikaku)
                        SelfSuffix.Append(seerRole?.GetLowerText(seer, isForMeeting: true) ?? "");
                    //seerに関わらず発動するLowerText
                    SelfSuffix.Append(CustomRoleManager.GetLowerTextOthers(seer, isForMeeting: true));
                    if (seerSubrole.Contains(CustomRoles.Guesser) ||
                    (LastNeutral.GiveGuesser.GetBool() && seerSubrole.Contains(CustomRoles.LastNeutral)) ||
                    (LastImpostor.giveguesser && seerSubrole.Contains(CustomRoles.LastImpostor)) ||
                    data.GiveGuesser.GetBool()
                    )
                    {
                        var gi = $" <line-height=10%>\n<color=#999900><size=50%>{GetString("GuessInfo")}</color></size></line-height>";
                        SelfSuffix.Append(gi);
                    }
                    //seer役職が対象のSuffix
                    if (Amnesiacheck)
                        SelfSuffix.Append(seerRole?.GetSuffix(seer, isForMeeting: true) ?? "");
                    //seerに関わらず発動するSuffix
                    SelfSuffix.Append(CustomRoleManager.GetSuffixOthers(seer, isForMeeting: true));

                    //RealNameを取得 なければ現在の名前をRealNamesに書き込む
                    string SeerRealName = (seerRole is IUseTheShButton) ? Main.AllPlayerNames[seer.PlayerId] : seer.GetRealName(true);

                    if (TemporaryName ?? false)
                        SeerRealName = name;

                    var next = "";
                    if (Options.CanSeeNextRandomSpawn.GetBool())
                    {
                        if (SpawnMap.NextSpornName.TryGetValue(seer.PlayerId, out var r))
                            next += $"<size=40%><color=#9ae3bd>〔{r}〕</size>";
                    }
                    var colorName = SeerRealName.ApplyNameColorData(seer, seer, true);

                    //seerの役職名とSelfTaskTextとseerのプレイヤー名とSelfMarkを合成
                    var (enabled, text) = GetRoleNameAndProgressTextData(seer);
                    string SelfRoleName = enabled ? $"<size={fontSize}>{text}</size>" : "";
                    string SelfDeathReason = ((TemporaryName ?? false) && nomarker) ? "" : seer.KnowDeathReason(seer) ? $"<size=75%>({ColorString(GetRoleColor(CustomRoles.Doctor), GetVitalText(seer.PlayerId, seer.PlayerId.CanDeathReasonKillerColor()))})</size>" : "";
                    string SelfName = $"{colorName}{SelfDeathReason}{(((TemporaryName ?? false) && nomarker) ? "" : SelfMark)}";
                    SelfName = SelfRoleName + "\r\n" + SelfName + next;
                    var g = "<line-height=85%>";
                    SelfName += SelfSuffix.ToString() == "" ? "" : (g + "\r\n " + SelfSuffix.ToString() + "</line-height>");

                    var p = PlayerCatch.AllAlivePlayerControls.OrderBy(x => x.PlayerId);
                    var a = PlayerCatch.AllPlayerControls.Where(x => !x.IsAlive()).OrderBy(x => x.PlayerId);
                    var list = p.ToArray().AddRangeToArray(a.ToArray());

                    if (list[0] != null)
                        if (list[0] == seer)
                        {
                            var Name = (SelfSuffix.ToString() == "" ? "" : (SelfSuffix.ToString().RemoveText() + g + " \r\n " + "</line-height>")) + Info + SelfName + Info.RemoveText() + "\r\n<size=1.5> ";
                            SelfName = Name;
                        }
                        else
                        {
                            var Name = (SelfSuffix.ToString() == "" ? "" : (SelfSuffix.ToString().RemoveText() + g + " \r\n " + "</line-height>")) + SelfName;
                            SelfName = Name;
                        }

                    if (list.LastOrDefault() != null)
                        if (list.LastOrDefault() == seer)
                        {
                            var team = role.GetCustomRoleTypes();
                            if (Options.CanSeeTimeLimit.GetBool() && DisableDevice.optTimeLimitDevices)
                            {
                                var info = "<size=60%>" + DisableDevice.GetAddminTimer() + "</color>　" + DisableDevice.GetCamTimr() + "</color>　" + DisableDevice.GetVitalTimer() + "</color></size>";
                                if ((team == CustomRoleTypes.Impostor && Options.CanseeImpTimeLimit.GetBool()) || (team == CustomRoleTypes.Crewmate && Options.CanseeCrewTimeLimit.GetBool())
                                || (team == CustomRoleTypes.Neutral && Options.CanseeNeuTimeLimit.GetBool()) || (team == CustomRoleTypes.Madmate && Options.CanseeMadTimeLimit.GetBool()) || !seerisAlive)
                                    if (info != "")
                                    {
                                        var Name = info.RemoveText() + "\n" + SelfName + "\n" + info;
                                        SelfName = Name;
                                    }
                            }
                        }
                    //適用
                    //Logger.Info(SelfName, "Name");
                    sender.StartRpc(seer.NetId, (byte)RpcCalls.SetName)
                    .Write(seer.NetId)
                    .Write(SelfName)
                    .Write(true)
                    .EndRpc();
                }
                var rolech = seerRole?.NotifyRolesCheckOtherName ?? false;

                {
                    //死者じゃない場合タスクターン中、霊の名前を変更する必要がないので少しでも処理を減らす敵な感じで
                    var ForeachList = PlayerCatch.AllPlayerControls;
                    foreach (var target in ForeachList)
                    {
                        //targetがseer自身の場合は何もしない
                        if (target == seer || target == null) continue;

                        var targetisalive = target.IsAlive();

                        {
                            var targetrole = target.GetRoleClass();
                            //名前の後ろに付けるマーカー
                            TargetMark.Clear();

                            /// targetの名前を一時的に上書きするかのチェック
                            string name = ""; bool nomarker = false;
                            var TemporaryName = targetrole?.GetTemporaryName(ref name, ref nomarker, seer, target) ?? false;

                            //seerに関わらず発動するMark
                            TargetMark.Append(CustomRoleManager.GetMarkOthers(seer, target, true));

                            //ハートマークを付ける(相手に)
                            var seerri = seer.GetRiaju();
                            var tageri = target.GetRiaju();
                            var seerisone = seerSubrole.Contains(CustomRoles.OneLove);
                            var targetsubrole = target.GetCustomSubRoles();
                            var seenIsOne = targetsubrole.Contains(CustomRoles.OneLove);
                            if (seerri == tageri && seer.IsRiaju() && !seerisone)
                                TargetMark.Append(ColorString(GetRoleColor(seerri), "♥"));
                            else if (seer.Data.IsDead && !seer.Is(tageri) && tageri != CustomRoles.NotAssigned && !seerisone)
                                TargetMark.Append(ColorString(GetRoleColor(tageri), "♥"));

                            if ((seerisone && seenIsOne)
                            || (seer.Data.IsDead && !seerisone && seenIsOne)
                            || (seerisone && target.PlayerId == Lovers.OneLovePlayer.Ltarget)
                            )
                                TargetMark.Append("<color=#ff7961>♡</color>");

                            if (seercone && targetsubrole.Contains(CustomRoles.Connecting) && (role is not CustomRoles.WolfBoy || !seerisAlive)
                            || (seer.Data.IsDead && !seercone && targetsubrole.Contains(CustomRoles.Connecting))
                            ) //狼少年じゃないか死亡なら処理
                                TargetMark.Append($"<color=#96514d>Ψ</color>");

                            //インサイダーモードタスク表示
                            if (Options.Taskcheck.GetBool())
                            {
                                if (target.GetPlayerTaskState() != null && target.GetPlayerTaskState().AllTasksCount > 0)
                                {
                                    if (role.IsImpostor())
                                    {
                                        TargetMark.Append($"<color=yellow>({target.GetPlayerTaskState().CompletedTasksCount}/{target.GetPlayerTaskState().GetNeedCountOrAll()})</color>");
                                    }
                                }
                            }

                            //他人の役職とタスクは幽霊が他人の役職を見れるようになっていてかつ、seerが死んでいる場合のみ表示されます。それ以外の場合は空になります。
                            var targetRoleData = GetRoleNameAndProgressTextData(seer, target, false);
                            var TargetRoleText = targetRoleData.enabled ? $"<size={fontSize}>{targetRoleData.text}</size>\r\n" : $"";

                            TargetSuffix.Clear();
                            //seerに関わらず発動するLowerText
                            TargetSuffix.Append(CustomRoleManager.GetLowerTextOthers(seer, target, isForMeeting: true));

                            //seerに関わらず発動するSuffix
                            TargetSuffix.Append(CustomRoleManager.GetSuffixOthers(seer, target, isForMeeting: true));
                            // 空でなければ先頭に改行を挿入
                            if (TargetSuffix.Length > 0)
                                TargetSuffix.Insert(0, "\r\n");

                            if (Amnesiacheck)
                            {
                                //seer役職が対象のMark
                                TargetMark.Append(seerRole?.GetMark(seer, target, true) ?? "");
                                TargetSuffix.Append(seerRole?.GetSuffix(seer, target, isForMeeting: true) ?? "");

                                if (targetsubrole.Contains(CustomRoles.Workhorse))
                                {
                                    if (((seerRole as Alien)?.modeProgresskiller == true && Alien.ProgressWorkhorseseen)
                                    || ((seerRole as JackalAlien)?.modeProgresskiller == true && JackalAlien.ProgressWorkhorseseen)
                                    || (role is CustomRoles.ProgressKiller && ProgressKiller.ProgressWorkhorseseen)
                                    || ((seerRole as AlienHijack)?.modeProgresskiller == true && Alien.ProgressWorkhorseseen))
                                    {
                                        TargetMark.Append($"<color=blue>♦</color>");
                                    }
                                }
                            }

                            //RealNameを取得 なければ現在の名前をRealNamesに書き込む
                            string TargetPlayerName = (seerRole is IUseTheShButton) ? Main.AllPlayerNames[target.PlayerId] : target.GetRealName(true);

                            //ターゲットのプレイヤー名の色を書き換えます。
                            TargetPlayerName = TargetPlayerName.ApplyNameColorData(seer, target, true);

                            if (seerSubrole.Contains(CustomRoles.Guesser)
                            || data.GiveGuesser.GetBool()
                            || (seerSubrole.Contains(CustomRoles.LastImpostor) && LastImpostor.giveguesser)
                            || (seerSubrole.Contains(CustomRoles.LastNeutral) && LastNeutral.GiveGuesser.GetBool()))
                            {
                                if (seerisAlive && targetisalive)
                                {
                                    TargetPlayerName = ColorString(Color.yellow, target.PlayerId.ToString()) + " " + TargetPlayerName;
                                }
                            }

                            string TargetDeathReason = "";
                            if (seer.KnowDeathReason(target))
                                TargetDeathReason = $"<size=75%>({GetVitalText(target.PlayerId, seer.PlayerId.CanDeathReasonKillerColor() == true ? true : null)})</size>";

                            //全てのテキストを合成します。
                            var g = string.Format("<line-height={0}%>", "90");
                            string TargetName = $"{g}{TargetRoleText}{(TemporaryName ? name : TargetPlayerName)}{((TemporaryName && nomarker) ? "" : TargetDeathReason + TargetMark + TargetSuffix)}</line-height>";

                            var p = PlayerCatch.AllAlivePlayerControls.OrderBy(x => x.PlayerId);
                            var a = PlayerCatch.AllPlayerControls.Where(x => !x.IsAlive()).OrderBy(x => x.PlayerId);

                            var list = p.ToArray().AddRangeToArray(a.ToArray());
                            if (list[0] != null)
                                if (list[0] == target)
                                {
                                    if (targetRoleData.enabled)
                                    {
                                        var Name = (TargetSuffix.ToString() == "" ? "" : (TargetSuffix.ToString().RemoveText() + g + " \r\n " + "</line-height>")) + Info + TargetName + Info.RemoveText() + "\r\n<size=1.5> ";
                                        TargetName = Name;
                                    }
                                    else
                                    {
                                        var Name = (TargetSuffix.ToString() == "" ? "" : (TargetSuffix.ToString().RemoveText() + g + " \r\n " + "</line-height>")) + TInfo + TargetName + Finfo.RemoveText();
                                        TargetName = Name;
                                    }
                                }
                            if (list.LastOrDefault() != null)
                                if (list.LastOrDefault() == target)
                                {
                                    var team = role.GetCustomRoleTypes();
                                    if (Options.CanSeeTimeLimit.GetBool() && DisableDevice.optTimeLimitDevices)
                                    {
                                        var info = "<size=60%>" + DisableDevice.GetAddminTimer() + "</color>　" + DisableDevice.GetCamTimr() + "</color>　" + DisableDevice.GetVitalTimer() + "</color></size>";
                                        if ((team == CustomRoleTypes.Impostor && Options.CanseeImpTimeLimit.GetBool()) || (team == CustomRoleTypes.Crewmate && Options.CanseeCrewTimeLimit.GetBool())
                                        || (team == CustomRoleTypes.Neutral && Options.CanseeNeuTimeLimit.GetBool()) || (team == CustomRoleTypes.Madmate && Options.CanseeMadTimeLimit.GetBool()) || !seerisAlive)
                                            if (info != "")
                                            {
                                                var Name = info.RemoveText() + "\n" + TargetName + "\n" + info;
                                                TargetName = Name;
                                            }
                                    }
                                }
                            //適用
                            sender.StartRpc(target.NetId, (byte)RpcCalls.SetName)
                            .Write(target.NetId)
                            .Write(TargetName)
                            .Write(true)
                            .EndRpc();
                        }
                    }
                }
                {
                    sender.EndMessage();
                    sender.SendMessage();
                }
            }
        }
        public static string MeetingMoji;
    }
}
#endregion