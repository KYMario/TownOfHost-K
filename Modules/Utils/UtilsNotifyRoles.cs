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

using static TownOfHost.Utils;
using static TownOfHost.RandomSpawn;
using static TownOfHost.UtilsRoleText;
using TownOfHost.Roles.Crewmate;

namespace TownOfHost
{
    public static class UtilsNotifyRoles
    {
        public const int chengepake = 800;
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

            if (GameStates.introDestroyed)
                foreach (var pp in PlayerCatch.AllPlayerControls)
                {
                    var str = GetProgressText(pp.PlayerId, ShowManegementText: false, gamelog: true);
                    str = Regex.Replace(str, "ffffff", "000000");
                    if (!UtilsGameLog.LastLogPro.TryAdd(pp.PlayerId, str))
                        UtilsGameLog.LastLogPro[pp.PlayerId] = str;

                    if (Options.CurrentGameMode == CustomGameMode.Standard)
                    {
                        var mark = GetSubRolesText(pp.PlayerId, mark: true);
                        if (!UtilsGameLog.LastLogSubRole.TryAdd(pp.PlayerId, mark))
                            UtilsGameLog.LastLogSubRole[pp.PlayerId] = mark;
                        if (!UtilsGameLog.LastLogLoveRole.TryAdd(pp.PlayerId, ""))
                            UtilsGameLog.LastLogLoveRole[pp.PlayerId] = GetLoverRoleMark(pp.PlayerId);
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
                var sender = CustomRpcSender.Create("NotifyRoles", Hazel.SendOption.None);
                sender.StartMessage(clientId);
                string fontSize = Main.RoleTextSize.ToString();

                var role = seer.GetCustomRole();
                var seerRole = seer.GetRoleClass();
                var seerRoleInfo = seer.GetCustomRole().GetRoleInfo();
                var seerSubrole = seer.GetCustomSubRoles();
                var seerisAlive = seer.IsAlive();
                var Amnesiacheck = Amnesia.CheckAbility(seer);
                var seerConnecting = seer.Is(CustomRoles.Connecting);
                var IsMisidentify = seer.GetMisidentify(out var missrole);
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
                    if (Amnesiacheck && !IsMisidentify)
                        SelfMark.Append(seerRole?.GetMark(seer, isForMeeting: false) ?? "");

                    //seerに関わらず発動するMark
                    SelfMark.Append(CustomRoleManager.GetMarkOthers(seer, isForMeeting: false));

                    //ハートマークを付ける(自分に)
                    var lover = seer.GetLoverRole();
                    if (lover is not CustomRoles.NotAssigned and not CustomRoles.OneLove) SelfMark.Append(ColorString(GetRoleColor(lover), "♥"));

                    if ((seerConnecting && role is not CustomRoles.WolfBoy)
                    || (seerConnecting && !seerisAlive)) SelfMark.Append(ColorString(GetRoleColor(CustomRoles.Connecting), "Ψ"));

                    if (Options.CurrentGameMode == CustomGameMode.TaskBattle)
                    {
                        TaskBattle.GetMark(seer, null, ref SelfMark);
                    }
                    //Markとは違い、改行してから追記されます。
                    SelfSuffix.Clear();

                    //seer役職が対象のLowerText
                    if (Amnesiacheck && !IsMisidentify)
                        SelfSuffix.Append(seerRole?.GetLowerText(seer, isForMeeting: false) ?? "");
                    //seerに関わらず発動するLowerText
                    SelfSuffix.Append(CustomRoleManager.GetLowerTextOthers(seer, isForMeeting: false));
                    //追放者
                    if (Options.CanseeVoteresult.GetBool() && MeetingVoteManager.Voteresult != "")
                    {
                        if (SelfSuffix.ToString() != "") SelfSuffix.Append('\n');
                        SelfSuffix.Append("<#ffffff><size=75%>" + MeetingVoteManager.Voteresult + "</color></size>");
                    }
                    //seer役職が対象のSuffix
                    if (Amnesiacheck)
                        SelfSuffix.Append(seerRole?.GetSuffix(seer, isForMeeting: false) ?? "");
                    //seerに関わらず発動するSuffix
                    SelfSuffix.Append(CustomRoleManager.GetSuffixOthers(seer, isForMeeting: false));

                    //RealNameを取得 なければ現在の名前をRealNamesに書き込む
                    string SeerRealName = seer.GetRealName(false);

                    if (SuddenDeathMode.SuddenCannotSeeName)
                    {
                        SeerRealName = "";
                    }

                    if (TemporaryName ?? false)
                        SeerRealName = name;

                    if (MeetingStates.FirstMeeting && (Options.ChangeNameToRoleInfo.GetBool() || SuddenDeathMode.NowSuddenDeathMode) && Main.ShowRoleIntro)
                    {
                        SeerRealName = seer?.GetRoleDesc() ?? "";
                        if (GameStates.introDestroyed is false)
                        {
                            SelfMark.Clear();
                            SelfSuffix.Clear();
                            if (lover is not CustomRoles.NotAssigned and not CustomRoles.OneLove) SelfMark.Append(ColorString(GetRoleColor(lover), "♥"));
                        }
                    }
                    var colorName = SeerRealName.ApplyNameColorData(seer, seer, false);

                    //seerの役職名とSelfTaskTextとseerのプレイヤー名とSelfMarkを合成
                    var (enabled, text) = GetRoleNameAndProgressTextData(seer);
                    string SelfRoleName = enabled ? $"<size={fontSize}>{text}</size>" : "";
                    string SelfDeathReason = ((TemporaryName ?? false) && nomarker) ? "" : seer.KnowDeathReason(seer) ? $"<size=75%>({ColorString(GetRoleColor(CustomRoles.Doctor), GetVitalText(seer.PlayerId, seer.PlayerId.CanDeathReasonKillerColor()))})</size>" : "";
                    string SelfName = $"{colorName}{SelfDeathReason}{(((TemporaryName ?? false) && nomarker) ? "" : SelfMark)}";
                    SelfName = SelfRoleName + "\r\n" + SelfName;
                    SelfName += SelfSuffix.ToString() == "" ? "" : ("<line-height=85%>\r\n " + SelfSuffix.ToString() + "</line-height>");
                    SelfName = "<line-height=85%>" + SelfName + "\r\n";
                    SelfName = SelfName.RemoveDeltext("color=#", "#");

                    //適用
                    if (seer.SetNameCheck(SelfName, seer, NoCache))
                    {
                        Logger.Info($"{seer?.Data?.GetLogPlayerName()} (Me) => {SelfName}", "NotifyRoles");
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
                bool Sended = false;

                //seerが死んでいる場合など、必要なときのみ第二ループを実行する
                if (seer.Data.IsDead //seerが死んでいる
                    || role.IsImpostor() //seerがインポスター
                    || seer.IsNeutralKiller() //seerがキル出来るニュートラル
                    || PlayerState.GetByPlayerId(seer.PlayerId).TargetColorData.Count > 0 //seer視点用の名前色データが一つ以上ある
                    || Witch.IsSpelled()
                    || seer.IsLovers()
                    || seerConnecting
                    || IsActive(SystemTypes.Electrical)
                    || IsActive(SystemTypes.Comms)
                    || isMushroomMixupActive
                    || rolech
                    || Options.CurrentGameMode == CustomGameMode.TaskBattle
                    || NoCache
                    || ForceLoop
                )
                {
                    //死者じゃない場合タスクターン中、霊の名前を変更する必要がないので少しでも処理を減らす敵な感じで
                    var ForeachList = seerisAlive ? PlayerCatch.OldAlivePlayerControles : PlayerCatch.AllPlayerControls;
                    foreach (var target in ForeachList.Where(pc => pc?.PlayerId != seer.PlayerId))
                    {
                        //targetがseer自身の場合は何もしない
                        if (target?.PlayerId == seer?.PlayerId || target == null) continue;

                        if (Sended)
                        {
                            sender = CustomRpcSender.Create("NotifyRoles", Hazel.SendOption.None);
                            sender.StartMessage(clientId);
                            Sended = false;
                        }
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
                            var seerri = seer.GetLoverRole();
                            var tageri = target.GetLoverRole();
                            var seerisone = seerSubrole.Contains(CustomRoles.OneLove);
                            var targetsubrole = target.GetCustomSubRoles();
                            var seenIsOne = targetsubrole.Contains(CustomRoles.OneLove);
                            if (seerri == tageri && seer.IsLovers() && !seerisone)
                                TargetMark.Append(ColorString(GetRoleColor(seerri), "♥"));
                            else if (seer.Data.IsDead && !seer.Is(tageri) && tageri != CustomRoles.NotAssigned && !seerisone)
                                TargetMark.Append(ColorString(GetRoleColor(tageri), "♥"));

                            if ((seerisone && seenIsOne)
                            || ((seer.Data.IsDead || seerisone) && target.PlayerId == Lovers.OneLovePlayer.BelovedId)
                            )
                                TargetMark.Append("<#ff7961>♡</color>");

                            if (seerConnecting && targetsubrole.Contains(CustomRoles.Connecting) && (role is not CustomRoles.WolfBoy || !seerisAlive)
                            || (seer.Data.IsDead && !seerConnecting && targetsubrole.Contains(CustomRoles.Connecting))
                            ) //狼少年じゃないか死亡なら処理
                                TargetMark.Append($"<#96514d>Ψ</color>");

                            //インサイダーモードタスク表示
                            if (Options.InsiderModeCanSeeTask.GetBool())
                            {
                                if (target.GetPlayerTaskState() != null && target.GetPlayerTaskState().AllTasksCount > 0)
                                {
                                    if (role.IsImpostor())
                                    {
                                        TargetMark.Append($"<yellow>({target.GetPlayerTaskState().CompletedTasksCount}/{target.GetPlayerTaskState().GetNeedCountOrAll()})</color>");
                                    }
                                }
                            }

                            //他人の役職とタスクは幽霊が他人の役職を見れるようになっていてかつ、seerが死んでいる場合のみ表示されます。それ以外の場合は空になります。
                            var targetRoleData = GetRoleNameAndProgressTextData(seer, target, false);
                            var TargetRoleText = targetRoleData.enabled ? $"<size={fontSize}>{targetRoleData.text}</size>\r\n" : $"";

                            //見る側が双子で相方が双子の場合
                            if (Twins.TwinsList.TryGetValue(seer.PlayerId, out var targetid))
                            {
                                if (targetid == target.PlayerId) TargetRoleText = GetRoleColorAndtext(CustomRoles.Twins) + TargetRoleText;
                            }

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
                            string TargetPlayerName = target.GetRealName(false);

                            //ターゲットのプレイヤー名の色を書き換えます。
                            TargetPlayerName = TargetPlayerName.ApplyNameColorData(seer, target, false);

                            string TargetDeathReason = "";
                            if (seer.KnowDeathReason(target))
                                TargetDeathReason = $"<size=75%>({GetVitalText(target.PlayerId, seer.PlayerId.CanDeathReasonKillerColor() == true ? true : null)})</size>";

                            if (((IsActive(SystemTypes.Comms) && Options.CommsCamouflage.GetBool())
                            || (role is CustomRoles.Monochromer && seerisAlive)
                            || Camouflager.NowUse
                            || (SuddenDeathMode.SuddenCannotSeeName && !TemporaryName))
                            && (!((targetrole as Jumper)?.Jumping == true)))
                            {
                                TargetPlayerName = $"<size=0>{TargetPlayerName}</size> ";
                                name = $"<size=0>{name}</size>";
                            }
                            //全てのテキストを合成します。
                            var lineheight = string.Format("<line-height={0}%>", "85");
                            string TargetName = $"{lineheight}{TargetRoleText}{(TemporaryName ? name : TargetPlayerName)}{((TemporaryName && nomarker) ? "" : TargetDeathReason + TargetMark + TargetSuffix)}";
                            if (!seerisAlive && !((targetrole as Jumper)?.Jumping == true) && !targetisalive)
                                TargetName = $"<size=65%><line-height=-18%>\n<line-height=85%>{TargetRoleText.RemoveSizeTags()}<line-height=-17%>\n</line-height>{(TemporaryName ? name.RemoveSizeTags() : TargetPlayerName.RemoveSizeTags())}{((TemporaryName && nomarker) ? "" : TargetDeathReason.RemoveSizeTags() + TargetMark.ToString().RemoveSizeTags() + TargetSuffix.ToString().RemoveSizeTags())}";

                            TargetName = TargetName.RemoveDeltext("color=#", "#");
                            if ($"{lineheight}{(TemporaryName ? name : TargetPlayerName)}" == TargetName) TargetName = TargetName.RemoveDeltext("</?line-height[^>]*?>");
                            //適用

                            if (target.SetNameCheck(TargetName, seer, NoCache))
                            {
                                Logger.Info($"{seer?.Data?.GetLogPlayerName() ?? "null"} => {target?.Data?.GetLogPlayerName() ?? "null"} {TargetName}", "NotifyRoles");

                                sender.StartRpc(target.NetId, (byte)RpcCalls.SetName)
                                .Write(target.NetId)
                                .Write(TargetName)
                                .Write(true)
                                .EndRpc();
                                if (sender.stream.Length > chengepake)
                                {
                                    sender.EndMessage();
                                    sender.SendMessage();
                                    Sended = true;
                                    Logger.Info($"780超えたから分けるね！", "NotifyRoles");
                                }
                            }
                        }
                    }
                }
                if (!Sended)
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

            if (GameStates.introDestroyed)
                foreach (var pp in PlayerCatch.AllPlayerControls)
                {
                    var str = GetProgressText(pp.PlayerId, ShowManegementText: false, gamelog: true);
                    str = Regex.Replace(str, "ffffff", "000000");
                    if (!UtilsGameLog.LastLogPro.TryAdd(pp.PlayerId, str))
                        UtilsGameLog.LastLogPro[pp.PlayerId] = str;

                    if (Options.CurrentGameMode == CustomGameMode.Standard)
                    {
                        var mark = GetSubRolesText(pp.PlayerId, mark: true);
                        if (!UtilsGameLog.LastLogSubRole.TryAdd(pp.PlayerId, mark))
                            UtilsGameLog.LastLogSubRole[pp.PlayerId] = mark;
                        if (!UtilsGameLog.LastLogLoveRole.TryAdd(pp.PlayerId, ""))
                            UtilsGameLog.LastLogLoveRole[pp.PlayerId] = GetLoverRoleMark(pp.PlayerId);
                    }
                }
            if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default && !Main.DontGameSet) return;

            /* 会議拡張の奴 */
            var Minfo = $"<voffset=20><line-height=0><{Main.ModColor}><size=85%>TownOfHost-K</size>\t\t \n \t\t</color><size=70%><#ffffff>v{Main.PluginShowVersion}</color></size></voffset>";
            Minfo += $"<voffset=17.5>\n<#fc9003>Day.{UtilsGameLog.day}</color>" + Bakery.BakeryMark() + $"<voffset=15>\n{ExtendedMeetingText}</voffset>";

            //seer:ここで行われた変更を見ることができるプレイヤー
            //target:seerが見ることができる変更の対象となるプレイヤー
            foreach (var seer in PlayerControl.AllPlayerControls)
            {
                //seerが落ちているときに何もしない
                if (seer == null || seer.Data.Disconnected) continue;

                if (seer.IsModClient()) continue;
                var clientid = seer.GetClientId();
                if (clientid == -1) continue;

                var sender = CustomRpcSender.Create("MeetingNotifyRoles", Hazel.SendOption.None);
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
                var IsMisidentify = seer.GetMisidentify(out _);
                RoleAddAddons.GetRoleAddon(role, out var data, seer, subrole: [CustomRoles.Guesser]);

                {
                    var aliveplayer = PlayerCatch.AllAlivePlayerControls.OrderBy(x => x.PlayerId);
                    var deadplayer = PlayerCatch.AllPlayerControls.Where(x => !x.IsAlive()).OrderBy(x => x.PlayerId);
                    var list = aliveplayer.ToArray().AddRangeToArray(deadplayer.ToArray());

                    bool isMeMinfo = false;
                    if (Assassin.NowUse) isMeMinfo = false;
                    if (list[0] is not null)
                    {
                        if (list[0] == seer) isMeMinfo = true;
                    }

                    //名前の後ろに付けるマーカー
                    SelfMark.Clear();

                    //seerの名前を一時的に上書きするかのチェック
                    string name = ""; bool nomarker = false;
                    var TemporaryName = seerRole?.GetTemporaryName(ref name, ref nomarker, seer);

                    //seer役職が対象のMark
                    if (Amnesiacheck && !IsMisidentify)
                        SelfMark.Append(seerRole?.GetMark(seer, isForMeeting: true) ?? "");

                    //seerに関わらず発動するMark
                    SelfMark.Append(CustomRoleManager.GetMarkOthers(seer, isForMeeting: true));

                    //ハートマークを付ける(自分に)
                    var lover = seer.GetLoverRole();
                    if (lover is not CustomRoles.NotAssigned and not CustomRoles.OneLove) SelfMark.Append(ColorString(GetRoleColor(lover), "♥"));

                    if ((seercone && role is not CustomRoles.WolfBoy)
                    || (seercone && !seerisAlive)) SelfMark.Append(ColorString(GetRoleColor(CustomRoles.Connecting), "Ψ"));

                    //Markとは違い、改行してから追記されます。
                    SelfSuffix.Clear();

                    //seer役職が対象のLowerText
                    if (Amnesiacheck && !IsMisidentify)
                        SelfSuffix.Append(seerRole?.GetLowerText(seer, isForMeeting: true) ?? "");
                    //seerに関わらず発動するLowerText
                    SelfSuffix.Append(CustomRoleManager.GetLowerTextOthers(seer, isForMeeting: true));

                    //seer役職が対象のSuffix
                    if (Amnesiacheck)
                        SelfSuffix.Append(seerRole?.GetSuffix(seer, isForMeeting: true) ?? "");
                    //seerに関わらず発動するSuffix
                    SelfSuffix.Append(CustomRoleManager.GetSuffixOthers(seer, isForMeeting: true));

                    //RealNameを取得 なければ現在の名前をRealNamesに書き込む
                    string SeerRealName = seer.GetRealName(true);

                    if (TemporaryName ?? false)
                        SeerRealName = name;

                    var next = "";
                    if (Options.CanSeeNextRandomSpawn.GetBool())
                    {
                        if (SpawnMap.NextSpornName.TryGetValue(seer.PlayerId, out var r))
                            next += $"<size=40%><#9ae3bd>〔{r}〕</size>";
                    }
                    var colorName = SeerRealName.ApplyNameColorData(seer, seer, true);

                    //seerの役職名とSelfTaskTextとseerのプレイヤー名とSelfMarkを合成
                    var (enabled, text) = GetRoleNameAndProgressTextData(seer);
                    string SelfRoleName = enabled ? $"<size={fontSize}>{text}</size>" : "";
                    string SelfDeathReason = ((TemporaryName ?? false) && nomarker) ? "" : seer.KnowDeathReason(seer) ? $"<size=75%>({ColorString(GetRoleColor(CustomRoles.Doctor), GetVitalText(seer.PlayerId, seer.PlayerId.CanDeathReasonKillerColor()))})</size>" : "";
                    string SelfName = $"{colorName}{SelfDeathReason}{(((TemporaryName ?? false) && nomarker) ? "" : SelfMark)}";
                    SelfName = SelfRoleName + "\r\n" + SelfName + next;
                    var line = "<line-height=85%>";
                    SelfName += SelfSuffix.ToString() == "" ? "" : (line + "\r\n " + SelfSuffix.ToString());

                    if (isMeMinfo)
                    {
                        var Name = SelfName;
                        SelfName = Minfo + $"<voffset=10>\n<line-height=85%>{Name.RemoveDeltext("</?line-height[^>]*?>", "")}{(SelfSuffix.ToString().RemoveHtmlTags() == "" ? "\r\n " : "")}</line-height></voffset>";
                    }
                    else
                    {
                        var Name = (SelfSuffix.ToString() == "" ? "" : (SelfSuffix.ToString().RemoveText() + line + " \r\n " + "</line-height>")) + SelfName;
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
                    SelfName = SelfName.RemoveDeltext("color=#", "#");
                    Logger.Info($"{seer.Data?.GetLogPlayerName() ?? "???"}(Me) => {SelfName}", "NotifyRoles");
                    //適用
                    //Logger.Info(SelfName, "Name");
                    sender.StartRpc(seer.NetId, (byte)RpcCalls.SetName)
                    .Write(seer.NetId)
                    .Write(SelfName)
                    .Write(true)
                    .EndRpc();
                }
                var rolech = seerRole?.NotifyRolesCheckOtherName ?? false;
                bool Sended = false;

                {
                    //死者じゃない場合タスクターン中、霊の名前を変更する必要がないので少しでも処理を減らす敵な感じで
                    var ForeachList = PlayerCatch.AllPlayerControls;
                    foreach (var target in ForeachList)
                    {
                        //targetがseer自身の場合は何もしない
                        if (target == seer || target == null) continue;

                        if (Sended)
                        {
                            sender = CustomRpcSender.Create("MeetingNotifyRoles", Hazel.SendOption.None);
                            sender.StartMessage(clientid);
                            Sended = false;
                        }
                        var targetisalive = target.IsAlive();

                        {
                            var aliveplayer = PlayerCatch.AllAlivePlayerControls.OrderBy(x => x.PlayerId);
                            var deadplayer = PlayerCatch.AllPlayerControls.Where(x => !x.IsAlive()).OrderBy(x => x.PlayerId);
                            var list = aliveplayer.ToArray().AddRangeToArray(deadplayer.ToArray());
                            bool tageismeI = false;

                            if (Assassin.NowUse) tageismeI = target.PlayerId == PlayerControl.LocalPlayer.PlayerId;

                            if (list[0] != null)
                            {
                                if (list[0] == target)
                                {
                                    tageismeI = true;
                                }
                            }
                            var targetrole = target.GetRoleClass();
                            //名前の後ろに付けるマーカー
                            TargetMark.Clear();

                            /// targetの名前を一時的に上書きするかのチェック
                            string name = ""; bool nomarker = false;
                            var TemporaryName = targetrole?.GetTemporaryName(ref name, ref nomarker, seer, target) ?? false;

                            //seerに関わらず発動するMark
                            TargetMark.Append(CustomRoleManager.GetMarkOthers(seer, target, true));

                            //ハートマークを付ける(相手に)
                            var seerri = seer.GetLoverRole();
                            var tageri = target.GetLoverRole();
                            var seerisone = seerSubrole.Contains(CustomRoles.OneLove);
                            var targetsubrole = target.GetCustomSubRoles();
                            var seenIsOne = targetsubrole.Contains(CustomRoles.OneLove);
                            if (seerri == tageri && seer.IsLovers() && !seerisone)
                                TargetMark.Append(ColorString(GetRoleColor(seerri), "♥"));
                            else if (seer.Data.IsDead && !seer.Is(tageri) && tageri is not CustomRoles.NotAssigned and not CustomRoles.OneLove && !seerisone)
                                TargetMark.Append(ColorString(GetRoleColor(tageri), "♥"));

                            if ((seerisone && seenIsOne)
                            || ((seer.Data.IsDead || seerisone) && target.PlayerId == Lovers.OneLovePlayer.BelovedId)
                            )
                                TargetMark.Append("<#ff7961>♡</color>");

                            if (seercone && targetsubrole.Contains(CustomRoles.Connecting) && (role is not CustomRoles.WolfBoy || !seerisAlive)
                            || (seer.Data.IsDead && !seercone && targetsubrole.Contains(CustomRoles.Connecting))
                            ) //狼少年じゃないか死亡なら処理
                                TargetMark.Append($"<#96514d>Ψ</color>");

                            //インサイダーモードタスク表示
                            if (Options.InsiderModeCanSeeTask.GetBool())
                            {
                                if (target.GetPlayerTaskState() != null && target.GetPlayerTaskState().AllTasksCount > 0)
                                {
                                    if (role.IsImpostor())
                                    {
                                        TargetMark.Append($"<yellow>({target.GetPlayerTaskState().CompletedTasksCount}/{target.GetPlayerTaskState().GetNeedCountOrAll()})</color>");
                                    }
                                }
                            }

                            //他人の役職とタスクは幽霊が他人の役職を見れるようになっていてかつ、seerが死んでいる場合のみ表示されます。それ以外の場合は空になります。
                            var targetRoleData = GetRoleNameAndProgressTextData(seer, target, false);
                            var TargetRoleText = targetRoleData.enabled ? $"<size={fontSize}>{targetRoleData.text}</size>\r\n" : $"";
                            var meetingageru = targetRoleData.enabled;

                            //見る側が双子で相方が双子の場合
                            if (Twins.TwinsList.TryGetValue(seer.PlayerId, out var targetid) && seerisAlive)
                            {
                                if (targetid == target.PlayerId)
                                {
                                    meetingageru |= true;
                                    if (TargetRoleText == "") TargetRoleText = $"<size={fontSize}>{GetRoleColorAndtext(CustomRoles.Twins)}</size>\r\n";
                                    else TargetRoleText = GetRoleColorAndtext(CustomRoles.Twins) + TargetRoleText;
                                }
                            }

                            if (TargetRoleText is "" && tageismeI)
                            {
                                TargetRoleText = $"\r\n";
                            }

                            TargetSuffix.Clear();
                            //seerに関わらず発動するLowerText
                            TargetSuffix.Append(CustomRoleManager.GetLowerTextOthers(seer, target, isForMeeting: true));

                            //seerに関わらず発動するSuffix
                            TargetSuffix.Append(CustomRoleManager.GetSuffixOthers(seer, target, isForMeeting: true));
                            bool HasData = false;
                            // 空でなければ先頭に改行を挿入
                            if (TargetSuffix.Length > 0)
                            {
                                TargetSuffix.Insert(0, "\r\n");
                                HasData = true;
                            }

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
                            string TargetPlayerName = target.GetRealName(true);

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
                            var line = string.Format("<line-height={0}%>", "90");
                            string TargetName = $"{line}{TargetRoleText}{(TemporaryName ? name : TargetPlayerName)}{((TemporaryName && nomarker) ? "" : TargetDeathReason + TargetMark + TargetSuffix)}";

                            if (tageismeI)
                            {
                                var Name = TargetName;
                                TargetName = Minfo + $"\n<voffset=10>{Name}{(HasData ? "" : "\r\n ")}</line-height></voffset>";
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
                            TargetName = TargetName.RemoveDeltext("color=#", "#");
                            Logger.Info($"{target?.Data?.GetLogPlayerName() ?? "???"} =>{TargetName}", "NotifyRoles");

                            sender.StartRpc(target.NetId, (byte)RpcCalls.SetName)
                            .Write(target.NetId)
                            .Write(TargetName)
                            .Write(true)
                            .EndRpc();
                            if (sender.stream.Length > chengepake)
                            {
                                sender.EndMessage();
                                sender.SendMessage();
                                Sended = true;
                                Logger.Info($"780超えたから分けるね！", "NotifyRoles");
                            }
                        }
                    }
                }
                if (!Sended)
                {
                    sender.EndMessage();
                    sender.SendMessage();
                }
            }
        }
        public static string ExtendedMeetingText;
    }
}
#endregion