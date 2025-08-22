using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using UnityEngine;

using TownOfHost.Roles;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.Neutral;
using TownOfHost.Roles.AddOns.Neutral;
using TownOfHost.Roles.AddOns.Common;
using TownOfHost.Roles.Crewmate;
using TownOfHost.Roles.Ghost;
using TownOfHost.Roles.Impostor;
using TownOfHost.Roles.Madmate;

namespace TownOfHost
{
    [HarmonyPatch(typeof(LogicGameFlowNormal), nameof(LogicGameFlowNormal.CheckEndCriteria))]
    class GameEndChecker
    {
        private static GameEndPredicate predicate;
        public static bool Prefix()
        {
            if (!AmongUsClient.Instance.AmHost) return true;

            //ゲーム終了判定済みなら中断
            if (predicate == null) return false;

            //ゲーム終了しないモードで廃村以外の場合は中断
            if (Main.DontGameSet && CustomWinnerHolder.WinnerTeam != CustomWinner.Draw) return false;

            //後追い処理等が終わってないなら中断
            if (predicate is NormalGameEndPredicate && Main.AfterMeetingDeathPlayers.Count is not 0) return false;

            //廃村用に初期値を設定
            var reason = GameOverReason.ImpostorsByKill;

            //ゲーム終了判定
            predicate.CheckForEndGame(out reason);

            //ゲーム終了時
            if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Default)
            {
                //カモフラージュ強制解除
                PlayerCatch.AllPlayerControls.Do(pc => Camouflage.RpcSetSkin(pc, ForceRevert: true, RevertToDefault: true));

                if (Options.CurrentGameMode != CustomGameMode.Standard || !SuddenDeathMode.NowSuddenDeathMode)
                    switch (CustomWinnerHolder.WinnerTeam)
                    {
                        case CustomWinner.Crewmate:
                            PlayerCatch.AllPlayerControls
                                .Where(pc => pc.Is(CustomRoleTypes.Crewmate) && !pc.GetCustomRole().IsLovers()
                                && !pc.Is(CustomRoles.Amanojaku) && !pc.Is(CustomRoles.Jackaldoll) && !pc.Is(CustomRoles.SKMadmate)
                                && ((pc.Is(CustomRoles.Staff) && (pc.GetRoleClass() as Staff).EndedTaskInAlive) || !pc.Is(CustomRoles.Staff)))
                                .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                            if (Monochromer.CheckWin(reason)) break;
                            foreach (var pc in PlayerCatch.AllPlayerControls)
                            {
                                if (pc.GetCustomRole() is CustomRoles.SKMadmate or CustomRoles.Jackaldoll ||
                                    pc.IsLovers())
                                    CustomWinnerHolder.CantWinPlayerIds.Add(pc.PlayerId);
                            }
                            break;
                        case CustomWinner.Impostor:

                            PlayerCatch.AllPlayerControls
                                .Where(pc => (pc.Is(CustomRoleTypes.Impostor) || pc.Is(CustomRoleTypes.Madmate) || pc.Is(CustomRoles.SKMadmate)) && (!pc.GetCustomRole().IsLovers() || !pc.Is(CustomRoles.Jackaldoll)))
                                .Do(pc => CustomWinnerHolder.WinnerIds.Add(pc.PlayerId));
                            if (Egoist.CheckWin()) break;
                            foreach (var pc in PlayerCatch.AllPlayerControls)
                            {
                                if (pc.GetCustomRole() is CustomRoles.Jackaldoll ||
                                    pc.IsLovers())
                                    CustomWinnerHolder.CantWinPlayerIds.Add(pc.PlayerId);
                            }
                            break;
                        default:
                            // クルーでもインポスター勝利でもない場合のみ。徒党の処理をする
                            Faction.CheckWin();
                            //ラバー勝利以外の時にラバーをしめt...勝利を剥奪する処理。
                            //どーせ追加なら追加勝利するやろし乗っ取りなら乗っ取りやし。
                            if (CustomWinnerHolder.WinnerTeam.IsLovers())
                                break;
                            PlayerCatch.AllPlayerControls
                                .Where(p => p.IsLovers())
                                .Do(p => CustomWinnerHolder.CantWinPlayerIds.Add(p.PlayerId));
                            break;
                    }
                //チーム戦で勝者がチームじゃない時(単独勝利とかね)
                if (SuddenDeathMode.NowSuddenDeathTemeMode && !(CustomWinnerHolder.WinnerTeam is CustomWinner.SuddenDeathRed or CustomWinner.SuddenDeathBlue or CustomWinner.SuddenDeathGreen or CustomWinner.SuddenDeathYellow or CustomWinner.PurpleLovers))
                {
                    SuddenDeathMode.TeamAllWin();
                }
                if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Draw and not CustomWinner.None)
                {
                    if (!reason.Equals(GameOverReason.CrewmatesByTask))
                    {
                        Lovers.LoversSoloWin(ref reason);
                    }
                    if (reason.Equals(GameOverReason.CrewmatesByTask))//タスクの場合リア充敗北☆
                    {
                        PlayerCatch.AllPlayerControls
                            .Where(pc => pc.IsLovers())
                            .Do(lover => CustomWinnerHolder.CantWinPlayerIds.Add(lover.PlayerId));
                    }
                    Lovers.LoversAddWin();

                    //追加勝利陣営
                    foreach (var pc in PlayerCatch.AllPlayerControls.Where(pc => !CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId) || pc.GetCustomRole() is CustomRoles.Turncoat or CustomRoles.AllArounder))
                    {
                        if (!pc.IsLovers() && !pc.Is(CustomRoles.Amanojaku))
                        {
                            if (pc.GetRoleClass() is IAdditionalWinner additionalWinner)
                            {
                                var winnerRole = pc.GetCustomRole();
                                if (additionalWinner.CheckWin(ref winnerRole))
                                {
                                    Logger.Info($"{pc.Data.GetLogPlayerName()}:{winnerRole}での追加勝利", "AdditinalWinner");
                                    CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                                    CustomWinnerHolder.AdditionalWinnerRoles.Add(winnerRole);
                                    continue;
                                }
                            }
                        }
                        LastNeutral.CheckAddWin(pc, reason);
                        Amanojaku.CheckWin(pc, reason);

                    }
                }
                if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Draw)
                {
                    CurseMaker.CheckWin();
                    Fox.SFoxCheckWin(ref reason);
                }
                AsistingAngel.CheckAddWin();
                foreach (var phantomthiefplayer in PlayerCatch.AllAlivePlayerControls.Where(pc => pc.GetCustomRole() is CustomRoles.PhantomThief))
                {
                    if (phantomthiefplayer.GetRoleClass() is PhantomThief phantomThief)
                    {
                        phantomThief.CheckWin();
                    }
                }
                foreach (var player in PlayerCatch.AllPlayerControls)
                {
                    var roleclass = player.GetRoleClass();
                    roleclass?.CheckWinner();
                }
                Twins.CheckAddWin();

                ShipStatus.Instance.enabled = false;
                if (CustomWinnerHolder.WinnerTeam != CustomWinner.Crewmate && (reason.Equals(GameOverReason.CrewmatesByTask) || reason.Equals(GameOverReason.CrewmatesByVote)))
                    reason = GameOverReason.ImpostorsByKill;

                Logger.Info($"{CustomWinnerHolder.WinnerTeam} ({reason})", "Winner");

                if (Options.OutroCrewWinreasonchenge.GetBool() && (reason.Equals(GameOverReason.CrewmatesByTask) || reason.Equals(GameOverReason.CrewmatesByVote)))
                    reason = GameOverReason.ImpostorsByVote;

                StartEndGame(reason);
                predicate = null;
            }
            return false;
        }
        public static void StartEndGame(GameOverReason reason)
        {
            AmongUsClient.Instance.StartCoroutine(CoEndGame(AmongUsClient.Instance, reason).WrapToIl2Cpp());
        }
        private static IEnumerator CoEndGame(AmongUsClient self, GameOverReason reason)
        {
            GameStates.IsOutro = true;
            // サーバー側のパケットサイズ制限によりCustomRpcSenderが利用できないため，遅延を挟むことで順番の整合性を保つ．

            // バニラ画面でのアウトロを正しくするためのゴーストロール化
            List<byte> ReviveRequiredPlayerIds = new();
            var winner = CustomWinnerHolder.WinnerTeam;
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                if (winner == CustomWinner.Draw)
                {
                    SetGhostRole(ToGhostImpostor: true);
                    continue;
                }
                bool canWin = CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId) ||
                        CustomWinnerHolder.WinnerRoles.Contains(pc.GetCustomRole());
                canWin &= !CustomWinnerHolder.CantWinPlayerIds.Contains(pc.PlayerId);
                bool isCrewmateWin = reason.Equals(GameOverReason.CrewmatesByVote) || reason.Equals(GameOverReason.CrewmatesByTask);
                SetGhostRole(ToGhostImpostor: canWin ^ isCrewmateWin);

                void SetGhostRole(bool ToGhostImpostor)
                {
                    var isDead = pc.Data.IsDead;
                    if (!isDead) ReviveRequiredPlayerIds.Add(pc.PlayerId);
                    if (ToGhostImpostor)
                    {
                        Logger.Info($"{pc.GetNameWithRole().RemoveHtmlTags()}: ImpostorGhostに変更", "ResetRoleAndEndGame");
                        pc.RpcSetRole(RoleTypes.ImpostorGhost, false);
                    }
                    else
                    {
                        Logger.Info($"{pc.GetNameWithRole().RemoveHtmlTags()}: CrewmateGhostに変更", "ResetRoleAndEndGame");
                        pc.RpcSetRole(RoleTypes.CrewmateGhost, false);
                    }
                    // 蘇生までの遅延の間にオートミュートをかけられないように元に戻しておく
                    pc.Data.IsDead = isDead;
                }
            }

            // CustomWinnerHolderの情報の同期
            /*var winnerWriter = self.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.EndGame, SendOption.Reliable);
            CustomWinnerHolder.WriteTo(winnerWriter);
            self.FinishRpcImmediately(winnerWriter);*/

            // 蘇生を確実にゴーストロール設定の後に届けるための遅延
            yield return new WaitForSeconds(EndGameDelay);

            if (ReviveRequiredPlayerIds.Count > 0)
            {
                // 蘇生 パケットが膨れ上がって死ぬのを防ぐため，1送信につき1人ずつ蘇生する
                for (int i = 0; i < ReviveRequiredPlayerIds.Count; i++)
                {
                    var playerId = ReviveRequiredPlayerIds[i];
                    var playerInfo = GameData.Instance.GetPlayerById(playerId);
                    // 蘇生
                    playerInfo.IsDead = false;
                    // 送信
                    playerInfo.MarkDirty();
                    AmongUsClient.Instance.SendAllStreamedObjects();
                }
                // ゲーム終了を確実に最後に届けるための遅延
                yield return new WaitForSeconds(EndGameDelay);
            }
            yield return new WaitForSeconds(EndGameDelay);
            //ちゃんとバニラに試合結果表示させるための遅延
            try
            {
                SetRoleSummaryText();
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, "SetRoleSummaryText");
                Logger.seeingame("非クライアントへのアウトロテキスト生成中にエラーが発生しました。");
            }
            yield return new WaitForSeconds(EndGameDelay);

            // ゲーム終了
            GameManager.Instance.RpcEndGame(reason, false);
        }
        private static void SetRoleSummaryText(CustomRpcSender sender = null)
        {
            var winners = new List<PlayerControl>(); //先に処理
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                if (CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId)) winners.Add(pc);
            }
            foreach (var team in CustomWinnerHolder.WinnerRoles)
            {
                winners.AddRange(PlayerCatch.AllPlayerControls.Where(p => p.Is(team) && !winners.Contains(p)));
            }
            foreach (var id in CustomWinnerHolder.CantWinPlayerIds)
            {
                var pc = PlayerCatch.GetPlayerById(id);
                if (pc == null) continue;
                winners.Remove(pc);
            }

            List<byte> winnerList = new();
            if (winners.Count != 0)
                foreach (var pc in winners)
                {
                    if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Draw && pc.Is(CustomRoles.GM)) continue;
                    if (CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId) && winnerList.Contains(pc.PlayerId)) continue;
                    if (CustomWinnerHolder.CantWinPlayerIds.Contains(pc.PlayerId)) continue;

                    winnerList.Add(pc.PlayerId);
                }
            var (CustomWinnerText, CustomWinnerColor, _, _, _) = UtilsGameLog.GetWinnerText(winnerList: winnerList);
            var winnerSize = GetScale(CustomWinnerText.RemoveHtmlTags().Length, 2, 3.3);
            // フォントサイズを制限
            CustomWinnerText = $"<size={winnerSize}>{CustomWinnerText}</size>";
            static double GetScale(int input, double min, double max)
                => min + (max - min) * (1 - (double)(input - 1) / 13);

            /*
            var sb = new StringBuilder();
            string[] rtaStr = null;
            if (Main.RTAMode && Options.CurrentGameMode == CustomGameMode.TaskBattle && CustomWinnerHolder.WinnerTeam is not (CustomWinner.Draw or CustomWinner.None))
            rtaStr = UtilsGameLog.GetRTAText(winnerList).ToString().Split('\n');//RTA&廃村以外の時のみ取得/他はnull
            sb.Append("<align=left><ktag-voffset><pos=-44><size=1><color=white>" + (rtaStr == null ? Translator.GetString("RoleSummaryText") : rtaStr[0]) + "</voffset>");
            //大きさ調整するやつ

            if (rtaStr == null)//nullならRTAではないので通常ログを追加
            {
            List<byte> cloneRoles = new(PlayerState.AllPlayerStates.Keys);

                if (winnerList.Count != 0)
                    foreach (var id in winnerList)
                    {
                        sb.Replace("<ktag-voffset>", "");
                        sb.Append($"\n<pos=-44><ktag-voffset><{CustomWinnerColor}>★</color> </pos>").Append(Regex.Replace(UtilsGameLog.SummaryTexts(id), @"<pos=(\d+(\.\d+)?)em>", m => $"<pos={float.Parse(m.Groups[1].Value) - 41}em>") + "</voffset>");
                        cloneRoles.Remove(id);
                    }
                    if (cloneRoles.Count != 0)
                    foreach (var id in cloneRoles)
                    {
                        sb.Replace("<ktag-voffset>", "");
                        sb.Append($"\n<pos=-44><ktag-voffset>　 </pos>").Append(Regex.Replace(UtilsGameLog.SummaryTexts(id), @"<pos=(\d+(\.\d+)?)em>", m => $"<pos={float.Parse(m.Groups[1].Value) - 41}em>") + "</voffset>");
                    }
            }
            else
            {
                rtaStr = rtaStr.Skip(1).ToArray();
                foreach (var text in rtaStr)
                {
                    sb.Replace("<ktag-voffset>", "");
                    sb.Append($"\n<pos=-44><ktag-voffset>{text}</pos></voffset>");
                }
            sb.Replace("<ktag-voffset>", $"<voffset={15 - (1.5 * sb.ToString().Split('\n').Length * (0.2 * winnerSize))}>");
        }*/
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
                if (pc == null) continue;
                var target = (winnerList.Contains(pc.PlayerId) ? pc : (winnerList.Count == 0 ? pc : PlayerCatch.GetPlayerById(winnerList.OrderBy(pc => pc).FirstOrDefault()) ?? pc)) ?? pc;
                var targetname = Main.AllPlayerNames[target.PlayerId].Color(UtilsRoleText.GetRoleColor(target.GetCustomRole()));
                var text = $"<voffset=25>{CustomWinnerText}\n<voffset=24>{targetname}";// sb.ToString() +$"\n</align><voffset=23>{CustomWinnerText}\n<voffset=45><size=1.75>{targetname}";
                if (sender == null)
                {
                    target.RpcSetNamePrivate(text, true, pc, true);
                }
                else
                {
                    sender.AutoStartRpc(pc.NetId, (byte)RpcCalls.SetName, pc.GetClientId())
                        .Write(pc.Data.NetId)
                        .Write(text)
                        .Write(true)
                        .EndRpc();
                }
            }
        }
        private const float EndGameDelay = 0.2f;

        public static void SetPredicateToNormal() => predicate = new NormalGameEndPredicate();
        public static void SetPredicateToHideAndSeek() => predicate = new HideAndSeekGameEndPredicate();
        public static void SetPredicateToTaskBattle() => predicate = new TaskBattle.TaskBattleGameEndPredicate();

        public static void SetPredicateToSadness() => predicate = new SadnessGameEndPredicate();

        // ===== ゲーム終了条件 =====
        // 通常ゲーム用
        class NormalGameEndPredicate : GameEndPredicate
        {
            public override bool CheckForEndGame(out GameOverReason reason)
            {
                reason = GameOverReason.ImpostorsByKill;
                if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) return false;
                if (CheckGameEndByLivingPlayers(out reason)) return true;
                if (CheckGameEndByTask(out reason)) return true;
                if (CheckGameEndBySabotage(out reason)) return true;

                return false;
            }

            public bool CheckGameEndByLivingPlayers(out GameOverReason reason)
            {
                reason = GameOverReason.ImpostorsByKill;

                int Imp = 0;
                int Jackal = 0;
                int Crew = 0;
                int Remotekiller = 0;
                int GrimReaper = 0;
                int MilkyWay = 0;
                int Fox = 0;
                int FoxAndCrew = 0;
                int MadBetrayer = 0;

                foreach (var pc in PlayerCatch.AllAlivePlayerControls)
                {
                    if (pc.GetCustomRole() is CustomRoles.MadBetrayer)
                    {
                        Roles.Madmate.MadBetrayer.CheckCount(ref Crew, ref MadBetrayer);
                        continue;
                    }
                    switch (pc.GetCountTypes())
                    {
                        case CountTypes.Crew: Crew++; FoxAndCrew++; break;
                        case CountTypes.Impostor: Imp++; break;
                        case CountTypes.Jackal: Jackal++; break;
                        case CountTypes.Remotekiller: Remotekiller++; break;
                        case CountTypes.GrimReaper: GrimReaper++; break;
                        case CountTypes.MilkyWay: MilkyWay++; break;
                        case CountTypes.Fox:
                            if (pc.GetRoleClass() is Fox fox)
                            {
                                Fox++;
                                FoxAndCrew += fox.FoxCount();
                            }
                            break;
                    }
                }
                if (Jackal == 0 && (CustomRoles.Jackal.IsPresent() || CustomRoles.JackalMafia.IsPresent() || CustomRoles.JackalAlien.IsPresent()))
                    foreach (var player in PlayerCatch.AllAlivePlayerControls)
                    {
                        if (player.Is(CustomRoles.Jackaldoll) && JackalDoll.BossAndSidekicks.ContainsKey(player.PlayerId))
                        {
                            Jackal++;
                            Crew--;
                            FoxAndCrew--;
                            break;
                        }
                    }
                foreach (var pc in PlayerCatch.AllPlayerControls)
                {
                    if (pc.GetRoleClass() is Assassin assassin && !pc.IsAlive())
                    {
                        Imp += assassin.NowState is Assassin.AssassinMeeting.WaitMeeting or Assassin.AssassinMeeting.CallMetting or Assassin.AssassinMeeting.Guessing ? 1 : 0;
                    }
                }

                if (Imp == 0 && FoxAndCrew == 0 && Jackal == 0 && Remotekiller == 0 && MilkyWay == 0 && MadBetrayer == 0) //全滅
                {
                    reason = GameOverReason.ImpostorsByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.None);
                }
                else if (Lovers.CheckPlayercountWin())
                {
                    reason = GameOverReason.ImpostorsByKill;
                }
                else if (Imp == 1 && Crew == 0 && GrimReaper == 1)//死神勝利(1)
                {
                    reason = GameOverReason.ImpostorsByKill;
                    CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.GrimReaper, byte.MaxValue);
                    CustomWinnerHolder.WinnerRoles.Add(CustomRoles.GrimReaper);
                    CustomWinnerHolder.NeutralWinnerIds.Add(PlayerCatch.AllPlayerControls
                        .Where(pc => pc.GetCustomRole() is CustomRoles.GrimReaper).FirstOrDefault()?.PlayerId ?? byte.MaxValue);
                }
                else if (Jackal == 0 && Remotekiller == 0 && MilkyWay == 0 && MadBetrayer == 0 && FoxAndCrew <= Imp) //インポスター勝利
                {
                    reason = GameOverReason.ImpostorsByKill;
                    CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Impostor, byte.MaxValue);
                }
                else if (Imp == 0 && Remotekiller == 0 && MilkyWay == 0 && MadBetrayer == 0 && FoxAndCrew <= Jackal) //ジャッカル勝利
                {
                    reason = GameOverReason.ImpostorsByKill;
                    CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Jackal, byte.MaxValue);
                    CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Jackal);
                    CustomWinnerHolder.WinnerRoles.Add(CustomRoles.JackalMafia);
                    CustomWinnerHolder.WinnerRoles.Add(CustomRoles.JackalAlien);
                    CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Jackaldoll);
                }
                else if (Imp == 0 && Jackal == 0 && MilkyWay == 0 && MadBetrayer == 0 && FoxAndCrew <= Remotekiller)
                {
                    reason = GameOverReason.ImpostorsByKill;
                    CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Remotekiller, byte.MaxValue);
                    CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Remotekiller);
                    CustomWinnerHolder.NeutralWinnerIds.Add(PlayerCatch.AllPlayerControls
                        .Where(pc => pc.GetCustomRole() is CustomRoles.Remotekiller).FirstOrDefault()?.PlayerId ?? byte.MaxValue);
                }
                else if (Jackal == 0 && Imp == 0 && GrimReaper == 1 && Remotekiller == 0 && MilkyWay == 0 && MadBetrayer == 0)//死神勝利(2)
                {
                    reason = GameOverReason.ImpostorsByKill;
                    CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.GrimReaper, byte.MaxValue);
                    CustomWinnerHolder.WinnerRoles.Add(CustomRoles.GrimReaper);
                    CustomWinnerHolder.NeutralWinnerIds.Add(PlayerCatch.AllPlayerControls
                        .Where(pc => pc.GetCustomRole() is CustomRoles.GrimReaper).FirstOrDefault()?.PlayerId ?? byte.MaxValue);
                }
                else if (Imp == 0 && Jackal == 0 && Remotekiller == 0 && MadBetrayer == 0 && FoxAndCrew <= MilkyWay)
                {
                    reason = GameOverReason.ImpostorsByKill;
                    CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.MilkyWay, byte.MaxValue);
                    CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Vega);
                    CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Altair);
                }
                else if (Imp == 0 && Jackal == 0 && Remotekiller == 0 && MilkyWay == 0 && FoxAndCrew <= MadBetrayer)
                {
                    reason = GameOverReason.ImpostorsByKill;
                    CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.MadBetrayer, byte.MaxValue);
                    CustomWinnerHolder.WinnerRoles.Add(CustomRoles.MadBetrayer);
                }
                else if (Jackal == 0 && Remotekiller == 0 && MadBetrayer == 0 && MilkyWay == 0 && Imp == 0) //クルー勝利
                {
                    reason = GameOverReason.CrewmatesByVote;
                    CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Crewmate, byte.MaxValue);
                }
                else return false; //勝利条件未達成

                return true;
            }
        }

        // HideAndSeek用
        class HideAndSeekGameEndPredicate : GameEndPredicate
        {
            public override bool CheckForEndGame(out GameOverReason reason)
            {
                reason = GameOverReason.ImpostorsByKill;
                if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) return false;

                if (CheckGameEndByLivingPlayers(out reason)) return true;
                if (CheckGameEndByTask(out reason)) return true;

                return false;
            }

            public bool CheckGameEndByLivingPlayers(out GameOverReason reason)
            {
                reason = GameOverReason.ImpostorsByKill;

                int Imp = PlayerCatch.AlivePlayersCount(CountTypes.Impostor);
                int Crew = PlayerCatch.AlivePlayersCount(CountTypes.Crew);

                if (Imp == 0 && Crew == 0) //全滅
                {
                    reason = GameOverReason.ImpostorsByKill;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.None);
                }
                else if (Crew <= 0) //インポスター勝利
                {
                    reason = GameOverReason.ImpostorsByKill;
                    CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Impostor, byte.MaxValue);
                }
                else if (Imp == 0) //クルー勝利(インポスター切断など)
                {
                    reason = GameOverReason.CrewmatesByVote;
                    CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Crewmate, byte.MaxValue);
                }
                else return false; //勝利条件未達成

                return true;
            }
        }
    }

    public abstract class GameEndPredicate
    {
        /// <summary>ゲームの終了条件をチェックし、CustomWinnerHolderに値を格納します。</summary>
        /// <params name="reason">バニラのゲーム終了処理に使用するGameOverReason</params>
        /// <returns>ゲーム終了の条件を満たしているかどうか</returns>
        public abstract bool CheckForEndGame(out GameOverReason reason);

        /// <summary>GameData.TotalTasksとCompletedTasksをもとにタスク勝利が可能かを判定します。</summary>
        public virtual bool CheckGameEndByTask(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorsByKill;
            if (Options.DisableTaskWin.GetBool() || TaskState.InitialTotalTasks == 0) return false;

            if (GameData.Instance.TotalTasks <= GameData.Instance.CompletedTasks)
            {
                reason = GameOverReason.CrewmatesByTask;
                CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Crewmate, byte.MaxValue);
                return true;
            }
            return false;
        }
        /// <summary>ShipStatus.Systems内の要素をもとにサボタージュ勝利が可能かを判定します。</summary>
        public virtual bool CheckGameEndBySabotage(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorsByKill;
            if (ShipStatus.Instance.Systems == null) return false;
            if (GameStates.IsMeeting) return false;

            // TryGetValueは使用不可
            var systems = ShipStatus.Instance.Systems;
            LifeSuppSystemType LifeSupp;
            if (systems.ContainsKey(SystemTypes.LifeSupp) && // サボタージュ存在確認
                (LifeSupp = systems[SystemTypes.LifeSupp].TryCast<LifeSuppSystemType>()) != null && // キャスト可能確認
                LifeSupp.Countdown < 0f) // タイムアップ確認
            {
                // 酸素サボタージュ
                if (Options.ChangeSabotageWinRole.GetBool())
                {
                    var pc = PlayerCatch.GetPlayerById(Main.LastSab);
                    var role = pc.GetCustomRole();

                    switch (role)
                    {
                        case CustomRoles.Jackal:
                        case CustomRoles.JackalMafia:
                        case CustomRoles.JackalAlien:
                            CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Jackal, byte.MaxValue);
                            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Jackal);
                            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.JackalMafia);
                            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.JackalAlien);
                            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Jackaldoll);
                            break;
                        case CustomRoles.GrimReaper:
                            CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.GrimReaper, byte.MaxValue);
                            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.GrimReaper);
                            CustomWinnerHolder.NeutralWinnerIds.Add(PlayerCatch.AllPlayerControls
                                .Where(pc => pc.GetCustomRole() is CustomRoles.GrimReaper).FirstOrDefault()?.PlayerId ?? byte.MaxValue);
                            break;
                        case CustomRoles.Egoist:
                            CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Egoist, byte.MaxValue);
                            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Egoist);
                            CustomWinnerHolder.NeutralWinnerIds.Add(PlayerCatch.AllPlayerControls
                                .Where(pc => pc.GetCustomRole() is CustomRoles.Egoist).FirstOrDefault()?.PlayerId ?? byte.MaxValue);
                            break;
                        default:
                            CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Impostor, byte.MaxValue);
                            break;
                    }
                    reason = GameOverReason.ImpostorsBySabotage;
                    Main.IsActiveSabotage = false;
                    LifeSupp.Countdown = 10000f;
                    return true;
                }
                CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Impostor, byte.MaxValue);
                Main.IsActiveSabotage = false;
                reason = GameOverReason.ImpostorsBySabotage;
                LifeSupp.Countdown = 10000f;
                return true;
            }

            ISystemType sys = null;
            if (systems.ContainsKey(SystemTypes.Reactor)) sys = systems[SystemTypes.Reactor];
            else if (systems.ContainsKey(SystemTypes.Laboratory)) sys = systems[SystemTypes.Laboratory];
            else if (systems.ContainsKey(SystemTypes.HeliSabotage)) sys = systems[SystemTypes.HeliSabotage];
            ICriticalSabotage critical;
            if (sys != null && // サボタージュ存在確認
                (critical = sys.TryCast<ICriticalSabotage>()) != null && // キャスト可能確認
                critical.Countdown < 0f) // タイムアップ確認
            {
                if (SuddenDeathMode.NowSuddenDeathMode)
                {
                    PlayerCatch.AllAlivePlayerControls.Do(p => p.RpcMurderPlayerV2(p));
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.None);
                    Main.IsActiveSabotage = false;
                    reason = GameOverReason.ImpostorsBySabotage;
                    critical.ClearSabotage();
                    return true;
                }
                // リアクターサボタージュ
                if (Options.ChangeSabotageWinRole.GetBool())
                {
                    var pc = PlayerCatch.GetPlayerById(Main.LastSab);
                    var role = pc.GetCustomRole();

                    switch (role)
                    {
                        case CustomRoles.Jackal:
                        case CustomRoles.JackalMafia:
                        case CustomRoles.JackalAlien:
                            CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Jackal, byte.MaxValue);
                            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Jackal);
                            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.JackalMafia);
                            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.JackalAlien);
                            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Jackaldoll);
                            break;
                        case CustomRoles.GrimReaper:
                            CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.GrimReaper, byte.MaxValue);
                            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.GrimReaper);
                            CustomWinnerHolder.NeutralWinnerIds.Add(PlayerCatch.AllPlayerControls
                                .Where(pc => pc.GetCustomRole() is CustomRoles.GrimReaper).FirstOrDefault()?.PlayerId ?? byte.MaxValue);
                            break;
                        case CustomRoles.Egoist:
                            CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Egoist, byte.MaxValue);
                            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Egoist);
                            CustomWinnerHolder.NeutralWinnerIds.Add(PlayerCatch.AllPlayerControls
                                .Where(pc => pc.GetCustomRole() is CustomRoles.Egoist).FirstOrDefault()?.PlayerId ?? byte.MaxValue);
                            break;
                        default:
                            CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Impostor, byte.MaxValue);
                            break;
                    }
                    Main.IsActiveSabotage = false;
                    reason = GameOverReason.ImpostorsBySabotage;
                    critical.ClearSabotage();
                    return true;
                }
                CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Impostor, byte.MaxValue);
                Main.IsActiveSabotage = false;
                reason = GameOverReason.ImpostorsBySabotage;
                critical.ClearSabotage();
                return true;
            }

            return false;
        }
    }
}