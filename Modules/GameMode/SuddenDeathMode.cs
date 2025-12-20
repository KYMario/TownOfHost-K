using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

using TownOfHost.Roles.Core;
using System.Linq;
using AmongUs.GameOptions;

namespace TownOfHost.Modules
{
    public static class SuddenDeathMode
    {
        public static float SuddenDeathtime;
        public static float ArrowSendTimer;
        public static float GpsStartTime;
        public static bool IsActiveSabotage;
        public static bool ShowArrow;
        public static Color ArrowColor;
        public static int ArrowColorNumber;
        //null→通知しない　false→未通知 true→通知済み
        public static bool? Remaining60s;
        public static bool? Remaining30s;
        public static bool? Remaining15s;
        public static bool? Remaining10s;
        public static Dictionary<byte, Vector3> ArrowPosition = new();
        static float SuddendeathRoundTime;
        public static bool NowSuddenDeathMode;
        public static bool NowSuddenDeathTemeMode;
        public static bool SuddenCannotSeeName;
        public static List<byte> TeamRed = new();
        public static List<byte> TeamBlue = new();
        public static List<byte> TeamYellow = new();
        public static List<byte> TeamGreen = new();
        public static List<byte> TeamPurple = new();
        public static bool IsAffiliationAllPlayer
            => PlayerCatch.AllPlayerControls.All(pc => TeamRed.Contains(pc.PlayerId) || TeamBlue.Contains(pc.PlayerId)
            || TeamYellow.Contains(pc.PlayerId) || TeamGreen.Contains(pc.PlayerId) || TeamPurple.Contains(pc.PlayerId));

        public static void TeamReset()
        {
            TeamRed.Clear();
            TeamBlue.Clear();
            TeamYellow.Clear();
            TeamGreen.Clear();
            TeamPurple.Clear();
        }
        [Attributes.GameModuleInitializer]
        public static void Reset()
        {
            NowSuddenDeathTemeMode = SuddenTeam.GetBool();
            NowSuddenDeathMode = Options.CurrentGameMode is CustomGameMode.SuddenDeath;
            SuddenCannotSeeName = SuddenCantSeeOtherName.GetBool();
            SuddenDeathtime = 0;
            ArrowSendTimer = 0;
            GpsStartTime = 0;
            IsActiveSabotage = false;
            ShowArrow = false;
            ArrowColorNumber = -1;
            ArrowColor = ModColors.MadMateOrenge;
            ArrowPosition.Clear();
            Remaining60s = false;
            Remaining30s = false;
            Remaining15s = false;
            Remaining10s = false;

            SuddendeathRoundTime = SuddenDeathTimeLimit.GetFloat();
            var time = SuddenDeathTimeLimit.GetFloat();
            if (time <= 60) Remaining60s = null;
            if (time <= 30) Remaining30s = null;
            if (time <= 15) Remaining15s = null;
            if (time <= 10) Remaining10s = null;
            CustomRoleManager.LowerOthers.Add(GetLowerTextOthers);
            CustomRoleManager.MarkOthers.Add(GetMarkOthers);
        }
        public static void TeamSet()
        {
            if (!NowSuddenDeathTemeMode) return;
            if (!SuddenTeamOption.GetBool())
            {
                TeamReset();
                var teammax = SuddenTeamMaxPlayers.GetInt();
                List<PlayerControl> Assing = new();
                PlayerCatch.AllAlivePlayerControls.DoIf(p => !p.Is(CustomRoles.GM), p => Assing.Add(p));

                for (var i = 0; i < teammax; i++)
                {
                    if (Assing.Count() == 0) break;
                    var chance = IRandom.Instance.Next(Assing.Count());
                    var pc = Assing[chance];
                    TeamRed.Add(pc.PlayerId);
                    Assing.RemoveAt(chance);
                    Logger.Info($"{pc?.Data?.GetLogPlayerName() ?? "?"} => red", "SuddenDeathTeam");
                }
                for (var i = 0; i < teammax; i++)
                {
                    if (Assing.Count() == 0) break;
                    var chance = IRandom.Instance.Next(Assing.Count());
                    var pc = Assing[chance];
                    TeamBlue.Add(pc.PlayerId);
                    Assing.RemoveAt(chance);
                    Logger.Info($"{pc?.Data?.GetLogPlayerName() ?? "?"} => Blue", "SuddenDeathTeam");
                }
                for (var i = 0; i < teammax; i++)
                {
                    if (Assing.Count() == 0 || !SuddenAddTeamYellow.GetBool()) break;
                    var chance = IRandom.Instance.Next(Assing.Count());
                    var pc = Assing[chance];
                    TeamYellow.Add(pc.PlayerId);
                    Assing.RemoveAt(chance);
                    Logger.Info($"{pc?.Data?.GetLogPlayerName() ?? "?"} => Yellow", "SuddenDeathTeam");
                }
                for (var i = 0; i < teammax; i++)
                {
                    if (Assing.Count() == 0 || !SuddenAddTeamGreen.GetBool()) break;
                    var chance = IRandom.Instance.Next(Assing.Count());
                    var pc = Assing[chance];
                    TeamGreen.Add(pc.PlayerId);
                    Assing.RemoveAt(chance);
                    Logger.Info($"{pc?.Data?.GetLogPlayerName() ?? "?"} => Green", "SuddenDeathTeam");
                }
                for (var i = 0; i < teammax; i++)
                {
                    if (Assing.Count() == 0 || !SuddenAddTeamPurple.GetBool()) break;
                    var chance = IRandom.Instance.Next(Assing.Count());
                    var pc = Assing[chance];
                    TeamPurple.Add(pc.PlayerId);
                    Assing.RemoveAt(chance);
                    Logger.Info($"{pc?.Data?.GetLogPlayerName() ?? "?"} => Purple", "SuddenDeathTeam");
                }
            }

            var list = CustomRolesHelper.AllRoles.Where(role => !InvalidRoles.Contains(role)).ToArray();
            if (SuddenTeamRole.GetBool())
            {
                foreach (var id in TeamRed.Distinct())
                {
                    PlayerCatch.GetPlayerById(id)?.RpcSetCustomRole(list[SuddenRedTeamRole.GetValue()], log: true);
                }
                foreach (var id in TeamBlue.Distinct())
                {
                    PlayerCatch.GetPlayerById(id)?.RpcSetCustomRole(list[SuddenBlueTeamRole.GetValue()], log: true);
                }
                foreach (var id in TeamYellow.Distinct())
                {
                    PlayerCatch.GetPlayerById(id)?.RpcSetCustomRole(list[SuddenYellowTeamRole.GetValue()], log: true);
                }
                foreach (var id in TeamGreen.Distinct())
                {
                    PlayerCatch.GetPlayerById(id)?.RpcSetCustomRole(list[SuddenGreenTeamRole.GetValue()], log: true);
                }
                foreach (var id in TeamPurple.Distinct())
                {
                    PlayerCatch.GetPlayerById(id)?.RpcSetCustomRole(list[SuddenPurpleTeamRole.GetValue()], log: true);
                }
            }
            ColorSetAndRoleset();
        }
        public static void ColorSetAndRoleset()
        {
            if (TeamRed.Distinct().Count() > 0)
                foreach (var myid in TeamRed.Distinct())
                {
                    PlayerCatch.AllPlayerControls.Do(p => NameColorManager.Add(p.PlayerId, myid, ModColors.codered));
                    var mypc = PlayerCatch.GetPlayerById(myid);
                    if (mypc == null) continue;
                    foreach (var pc in PlayerCatch.AllPlayerControls)
                    {
                        if (pc.PlayerId == myid) continue;
                        pc.RpcSetRoleDesync(TeamRed.Contains(pc.PlayerId) ? RoleTypes.Impostor : RoleTypes.Crewmate, mypc.GetClientId(), Hazel.SendOption.None);
                    }
                }

            if (TeamBlue.Distinct().Count() > 0)
                foreach (var myid in TeamBlue.Distinct())
                {
                    PlayerCatch.AllPlayerControls.Do(p => NameColorManager.Add(p.PlayerId, myid, ModColors.codeblue));
                    var mypc = PlayerCatch.GetPlayerById(myid);
                    if (mypc == null) continue;
                    foreach (var pc in PlayerCatch.AllPlayerControls)
                    {
                        if (pc.PlayerId == myid) continue;
                        pc.RpcSetRoleDesync(TeamBlue.Contains(pc.PlayerId) ? RoleTypes.Impostor : RoleTypes.Crewmate, mypc.GetClientId(), Hazel.SendOption.None);
                    }
                }

            if (TeamYellow.Distinct().Count() > 0)
                foreach (var myid in TeamYellow.Distinct())
                {
                    PlayerCatch.AllPlayerControls.Do(p => NameColorManager.Add(p.PlayerId, myid, ModColors.codeyellow));
                    var mypc = PlayerCatch.GetPlayerById(myid);
                    if (mypc == null) continue;
                    foreach (var pc in PlayerCatch.AllPlayerControls)
                    {
                        if (pc.PlayerId == myid) continue;
                        pc.RpcSetRoleDesync(TeamYellow.Contains(pc.PlayerId) ? RoleTypes.Impostor : RoleTypes.Crewmate, mypc.GetClientId(), Hazel.SendOption.None);
                    }
                }

            if (TeamGreen.Distinct().Count() > 0)
                foreach (var myid in TeamGreen.Distinct())
                {
                    PlayerCatch.AllPlayerControls.Do(p => NameColorManager.Add(p.PlayerId, myid, ModColors.codegreen));
                    var mypc = PlayerCatch.GetPlayerById(myid);
                    if (mypc == null) continue;
                    foreach (var pc in PlayerCatch.AllPlayerControls)
                    {
                        if (pc.PlayerId == myid) continue;
                        pc.RpcSetRoleDesync(TeamGreen.Contains(pc.PlayerId) ? RoleTypes.Impostor : RoleTypes.Crewmate, mypc.GetClientId(), Hazel.SendOption.None);
                    }
                }

            if (TeamPurple.Distinct().Count() > 0)
                foreach (var myid in TeamPurple.Distinct())
                {
                    PlayerCatch.AllPlayerControls.Do(p => NameColorManager.Add(p.PlayerId, myid, ModColors.codepurple));
                    var mypc = PlayerCatch.GetPlayerById(myid);
                    if (mypc == null) continue;
                    foreach (var pc in PlayerCatch.AllPlayerControls)
                    {
                        if (pc.PlayerId == myid) continue;
                        pc.RpcSetRoleDesync(TeamPurple.Contains(pc.PlayerId) ? RoleTypes.Impostor : RoleTypes.Crewmate, mypc.GetClientId(), Hazel.SendOption.None);
                    }
                }
        }
        public static void NotTeamKill()
        {
            if (AmongUsClient.Instance.AmHost is false) return;
            if (NowSuddenDeathTemeMode)
            {
                foreach (var p in PlayerCatch.AllAlivePlayerControls)
                {
                    if (p.Is(CustomRoles.GM) || TeamRed.Contains(p.PlayerId) || TeamBlue.Contains(p.PlayerId)
                    || TeamYellow.Contains(p.PlayerId) || TeamGreen.Contains(p.PlayerId) || TeamPurple.Contains(p.PlayerId)) continue;

                    p.RpcSetCustomRole(CustomRoles.Emptiness, true, true);
                    p.RpcMurderPlayerV2(p);
                    PlayerState.GetByPlayerId(p.PlayerId).DeathReason = CustomDeathReason.etc;
                    Logger.Warn($"{p?.Data?.name} => チームアサインで余ったゾ", "SuddenDesthMode");
                }
            }
        }
        public static void SuddenDeathReactor()
        {
            if (IsActiveSabotage) return;

            if (!GameStates.Intro) SuddenDeathtime += Time.fixedDeltaTime;

            if (SuddenDeathtime > SuddendeathRoundTime)
            {
                IsActiveSabotage = true;

                if (AmongUsClient.Instance.AmHost)
                {
                    var systemtypes = Utils.GetCriticalSabotageSystemType();
                    ShipStatus.Instance.RpcUpdateSystem(systemtypes, 128);
                    Logger.Info("ｷﾐﾊﾓｳｼﾞｷｼﾇ...!!", "SuddenDeath");
                    UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);
                }
                return;
            }
            if (SuddendeathRoundTime - SuddenDeathtime < 10 && Remaining10s is false)
            {
                Remaining10s = true;

                if (AmongUsClient.Instance.AmHost) UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);
                return;
            }
            if (SuddendeathRoundTime - SuddenDeathtime < 15 && Remaining15s is false)
            {
                Remaining15s = true;
                if (AmongUsClient.Instance.AmHost) UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);
                return;
            }
            if (SuddendeathRoundTime - SuddenDeathtime < 30 && Remaining30s is false)
            {
                Remaining30s = true;
                if (AmongUsClient.Instance.AmHost) UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);
                return;
            }
            if (SuddendeathRoundTime - SuddenDeathtime < 60 && Remaining60s is false)
            {
                Remaining60s = true;
                if (AmongUsClient.Instance.AmHost) UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);
                return;
            }
        }
        public static void ItijohoSend()
        {
            if (!GameStates.Intro)
            {
                if (ShowArrow) ArrowSendTimer += Time.fixedDeltaTime;
                else GpsStartTime += Time.fixedDeltaTime;
            }

            if (GpsStartTime > SuddenArrowSendTime.GetFloat()) ShowArrow = true;

            if (ArrowSendTimer > SuddenArrowSenddis.GetFloat() && ShowArrow)
            {
                if (SuddenArrowSenddis.GetFloat() is 0)
                {
                    foreach (var pc in PlayerCatch.AllAlivePlayerControls)
                    {
                        foreach (var pl in PlayerCatch.AllAlivePlayerControls)
                        {
                            if (pc.PlayerId == pl.PlayerId) continue;
                            if (TeamRed.Contains(pl.PlayerId) && TeamRed.Contains(pc.PlayerId)) continue;
                            if (TeamBlue.Contains(pl.PlayerId) && TeamBlue.Contains(pc.PlayerId)) continue;
                            if (TeamYellow.Contains(pl.PlayerId) && TeamYellow.Contains(pc.PlayerId)) continue;
                            if (TeamGreen.Contains(pl.PlayerId) && TeamGreen.Contains(pc.PlayerId)) continue;
                            if (TeamPurple.Contains(pl.PlayerId) && TeamPurple.Contains(pc.PlayerId)) continue;

                            TargetArrow.Add(pc.PlayerId, pl.PlayerId);
                        }
                    }
                    return;
                }
                ArrowSendTimer = 0;
                foreach (var pc in PlayerCatch.AllAlivePlayerControls) ArrowPosition.Do(pos => GetArrow.Remove(pc.PlayerId, pos.Value));
                ArrowPosition.Clear();
                foreach (var pc in PlayerCatch.AllAlivePlayerControls) ArrowPosition.Add(pc.PlayerId, pc.transform.position);
                foreach (var pc in PlayerCatch.AllAlivePlayerControls)
                {
                    var p = pc.transform.position;
                    foreach (var po in ArrowPosition)
                    {
                        //同チームなら消す
                        if (TeamRed.Contains(po.Key) && TeamRed.Contains(pc.PlayerId)) continue;
                        if (TeamBlue.Contains(po.Key) && TeamBlue.Contains(pc.PlayerId)) continue;
                        if (TeamYellow.Contains(po.Key) && TeamYellow.Contains(pc.PlayerId)) continue;
                        if (TeamGreen.Contains(po.Key) && TeamGreen.Contains(pc.PlayerId)) continue;
                        if (TeamPurple.Contains(po.Key) && TeamPurple.Contains(pc.PlayerId)) continue;

                        if (po.Value != p) GetArrow.Add(pc.PlayerId, po.Value);
                    }
                }
                if (SuddenArrowSenddis.GetFloat() > 0)
                    switch (ArrowColorNumber)
                    {
                        case -1:
                            ArrowColor = Palette.Orange;
                            ArrowColorNumber = 1;
                            break;
                        case 1:
                            ArrowColor = Palette.CrewmateBlue;
                            ArrowColorNumber = 2;
                            break;
                        case 2:
                            ArrowColor = Palette.AcceptedGreen;
                            ArrowColorNumber = 3;
                            break;
                        case 3:
                            ArrowColor = Color.yellow;
                            ArrowColorNumber = 1;
                            break;
                    }
            }
        }
        public static string SuddenDeathProgersstext(PlayerControl seer)
        {
            var Remaining = "";
            if (!IsActiveSabotage)
            {
                if (Remaining60s ?? false) Remaining = Utils.ColorString(Palette.AcceptedGreen, "60s");
                if (Remaining30s ?? false) Remaining = Utils.ColorString(Color.yellow, "30s");
                if (Remaining15s ?? false) Remaining = Utils.ColorString(Palette.Orange, "15s");
                if (Remaining10s ?? false) Remaining = Utils.ColorString(Color.red, "10s");
            }
            return Remaining;
        }
        public static string GetLowerTextOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
        {
            seen ??= seer;
            if (seer != seen) return "";
            var ar = "";
            if (SuddenPlayerArrow.GetBool())
            {
                if (SuddenArrowSenddis.GetFloat() is 0)
                {
                    foreach (var sen in PlayerCatch.AllAlivePlayerControls)
                    {
                        ar += " " + TargetArrow.GetArrows(seer, sen.PlayerId);
                    }
                }
                else
                {
                    foreach (var p in ArrowPosition)
                    {
                        ar += " " + GetArrow.GetArrows(seer, p.Value);
                    }
                }
                ar = Utils.ColorString(ArrowColor, ar);
            }
            return ar;
        }
        public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
        {
            var tex = "";
            seen ??= seer;
            if (!SuddenRemainingPlayerCount.GetBool()) return "";
            if (NowSuddenDeathTemeMode && seen == seer)
            {
                var t1 = PlayerCatch.AllAlivePlayerControls.Where(pc => TeamRed.Contains(pc.PlayerId)).Count();
                var t2 = PlayerCatch.AllAlivePlayerControls.Where(pc => TeamBlue.Contains(pc.PlayerId)).Count();
                var t3 = PlayerCatch.AllAlivePlayerControls.Where(pc => TeamYellow.Contains(pc.PlayerId)).Count();
                var t4 = PlayerCatch.AllAlivePlayerControls.Where(pc => TeamGreen.Contains(pc.PlayerId)).Count();
                var t5 = PlayerCatch.AllAlivePlayerControls.Where(pc => TeamPurple.Contains(pc.PlayerId)).Count();
                if (t1 > 0) tex += $"<{ModColors.codered}>({t1})</color>";
                if (t2 > 0) tex += $"<{ModColors.codeblue}>({t2})</color>";
                if (t3 > 0) tex += $"<{ModColors.codeyellow}>({t3})</color>";
                if (t4 > 0) tex += $"<{ModColors.codegreen}>({t4})</color>";
                if (t5 > 0) tex += $"<{ModColors.codepurple}>({t5})</color>";
                return $" <size=70%>{tex}</size>";
            }
            if (seen == seer) return $"<#03fcb6> ({PlayerCatch.AllAlivePlayersCount})</color>";
            return "";
        }

        public static bool IsSameteam(this byte pc, byte targetid)
        {
            if (TeamRed.Contains(pc) && TeamRed.Contains(targetid)) return true;
            if (TeamBlue.Contains(pc) && TeamBlue.Contains(targetid)) return true;
            if (TeamYellow.Contains(pc) && TeamYellow.Contains(targetid)) return true;
            if (TeamGreen.Contains(pc) && TeamGreen.Contains(targetid)) return true;
            if (TeamPurple.Contains(pc) && TeamPurple.Contains(targetid)) return true;

            return false;
        }
        public static bool CheckTeamWin()
        {
            if (NowSuddenDeathTemeMode)
            {
                if (PlayerCatch.AllAlivePlayerControls.All(pc => TeamRed.Contains(pc.PlayerId)))
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.SuddenDeathRed);
                    TeamRed.Do(r => CustomWinnerHolder.WinnerIds.Add(r));
                    return true;
                }
                if (PlayerCatch.AllAlivePlayerControls.All(pc => TeamBlue.Contains(pc.PlayerId)))
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.SuddenDeathBlue);
                    TeamBlue.Do(r => CustomWinnerHolder.WinnerIds.Add(r));
                    return true;
                }
                if (PlayerCatch.AllAlivePlayerControls.All(pc => TeamYellow.Contains(pc.PlayerId)))
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.SuddenDeathYellow);
                    TeamYellow.Do(r => CustomWinnerHolder.WinnerIds.Add(r));
                    return true;
                }
                if (PlayerCatch.AllAlivePlayerControls.All(pc => TeamGreen.Contains(pc.PlayerId)))
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.SuddenDeathGreen);
                    TeamGreen.Do(r => CustomWinnerHolder.WinnerIds.Add(r));
                    return true;
                }
                if (PlayerCatch.AllAlivePlayerControls.All(pc => TeamPurple.Contains(pc.PlayerId)))
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.SuddenDeathPurple);
                    TeamPurple.Do(r => CustomWinnerHolder.WinnerIds.Add(r));
                    return true;
                }
            }
            return false;
        }
        public static void TeamAllWin()
        {
            foreach (var wi in CustomWinnerHolder.WinnerIds)
            {
                if (TeamRed.Contains(wi))
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.SuddenDeathRed);
                    TeamRed.Do(id => CustomWinnerHolder.WinnerIds.Add(id));
                    break;
                }
                if (TeamBlue.Contains(wi))
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.SuddenDeathBlue);
                    TeamBlue.Do(id => CustomWinnerHolder.WinnerIds.Add(id));
                    break;
                }
                if (TeamYellow.Contains(wi))
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.SuddenDeathYellow);
                    TeamYellow.Do(id => CustomWinnerHolder.WinnerIds.Add(id));
                    break;
                }
                if (TeamGreen.Contains(wi))
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.SuddenDeathGreen);
                    TeamGreen.Do(id => CustomWinnerHolder.WinnerIds.Add(id));
                    break;
                }
                if (TeamPurple.Contains(wi))
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.SuddenDeathPurple);
                    TeamPurple.Do(id => CustomWinnerHolder.WinnerIds.Add(id));
                    break;
                }
            }
        }
        public static void UpdateTeam()
        {
            if (GameStates.IsLobby && (SuddenTeamOption.GetBool() || IsAffiliationAllPlayer is false))
            {
                if (IsAffiliationAllPlayer is false && !SuddenTeamOption.GetBool())
                {
                    TeamReset();
                    return;
                }
                foreach (var pc in PlayerCatch.AllPlayerControls)
                {
                    var pos = pc.GetTruePosition();

                    if (-3 <= pos.x && pos.x <= -1.1 && -1 <= pos.y && pos.y <= 0.4)
                    {
                        if (!TeamRed.Contains(pc.PlayerId))
                            TeamRed.Add(pc.PlayerId);
                        TeamBlue.Remove(pc.PlayerId);
                        TeamYellow.Remove(pc.PlayerId);
                        TeamGreen.Remove(pc.PlayerId);
                        TeamPurple.Remove(pc.PlayerId);
                    }
                    else
                    if (-0.2 <= pos.x && pos.x <= 0.7 && -1.1 <= pos.y && pos.y <= 0.4)
                    {
                        TeamRed.Remove(pc.PlayerId);
                        if (!TeamBlue.Contains(pc.PlayerId))
                            TeamBlue.Add(pc.PlayerId);
                        TeamYellow.Remove(pc.PlayerId);
                        TeamGreen.Remove(pc.PlayerId);
                        TeamPurple.Remove(pc.PlayerId);
                    }
                    else
                    if (1.7 <= pos.x && pos.x <= 3 && -1.1 <= pos.y && pos.y <= 0.7 && SuddenAddTeamYellow.GetBool())
                    {
                        TeamRed.Remove(pc.PlayerId);
                        TeamBlue.Remove(pc.PlayerId);
                        if (!TeamYellow.Contains(pc.PlayerId))
                            TeamYellow.Add(pc.PlayerId);
                        TeamGreen.Remove(pc.PlayerId);
                        TeamPurple.Remove(pc.PlayerId);
                    }
                    else
                    if (0.5f <= pos.x && pos.x <= 2.1 && 2.1 <= pos.y && pos.y <= 3.2 && SuddenAddTeamGreen.GetBool())
                    {
                        TeamRed.Remove(pc.PlayerId);
                        TeamBlue.Remove(pc.PlayerId);
                        TeamYellow.Remove(pc.PlayerId);
                        if (!TeamGreen.Contains(pc.PlayerId))
                            TeamGreen.Add(pc.PlayerId);
                        TeamPurple.Remove(pc.PlayerId);
                    }
                    else
                    if (-2.9 <= pos.x && pos.x <= -1.1 && 2.2 <= pos.y && pos.y <= 3.0 && SuddenAddTeamPurple.GetBool())
                    {
                        TeamRed.Remove(pc.PlayerId);
                        TeamBlue.Remove(pc.PlayerId);
                        TeamYellow.Remove(pc.PlayerId);
                        TeamGreen.Remove(pc.PlayerId);
                        if (!TeamPurple.Contains(pc.PlayerId))
                            TeamPurple.Add(pc.PlayerId);
                    }
                    else if (!GameStates.IsCountDown && !GameStates.Intro)
                    {
                        TeamRed.Remove(pc.PlayerId);
                        TeamBlue.Remove(pc.PlayerId);
                        TeamYellow.Remove(pc.PlayerId);
                        TeamGreen.Remove(pc.PlayerId);
                        TeamPurple.Remove(pc.PlayerId);
                    }
                }

                foreach (var pc in PlayerCatch.AllPlayerControls)
                {
                    if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
                    var color = "#ffffff";
                    if (TeamRed.Contains(pc.PlayerId)) color = ModColors.codered;
                    if (TeamBlue.Contains(pc.PlayerId)) color = ModColors.codeblue;
                    if (TeamYellow.Contains(pc.PlayerId)) color = ModColors.codeyellow;
                    if (TeamGreen.Contains(pc.PlayerId)) color = ModColors.codegreen;
                    if (TeamPurple.Contains(pc.PlayerId)) color = ModColors.codepurple;
                    foreach (var seer in PlayerCatch.AllPlayerControls)
                    {
                        if (pc.name != "Player(Clone)" && seer.name != "Player(Clone)" && seer.PlayerId != PlayerControl.LocalPlayer.PlayerId && !seer.IsModClient())
                            pc.RpcSetNamePrivate($"<{color}>{pc.Data.PlayerName}", true, seer, false);
                    }
                }
                if (!IsAffiliationAllPlayer && GameStates.IsCountDown)
                {
                    GameStartManager.Instance.ResetStartState();
                    Utils.SendMessage(Translator.GetString("SuddendeathLobbyError"));
                }
            }
        }
        public static OptionItem SuddenDeathModeActive;
        public static OptionItem SuddenSharingRoles;
        public static OptionItem SuddenCantSeeOtherName;
        //時間制限
        public static OptionItem SuddenDeathTimeLimit;
        public static OptionItem SuddenDeathReactortime;
        //矢印
        public static OptionItem SuddenPlayerArrow;
        public static OptionItem SuddenArrowSendTime;
        public static OptionItem SuddenArrowSenddis;

        public static OptionItem SuddenRemainingPlayerCount;
        public static OptionItem SuddenCanSeeKillflash;
        public static OptionItem SuddenKillcooltime;
        public static OptionItem SuddenfinishTaskWin;
        //チーム設定
        public static OptionItem SuddenTeam;
        public static OptionItem SuddenAddTeamYellow;
        public static OptionItem SuddenAddTeamGreen;
        public static OptionItem SuddenAddTeamPurple;
        public static OptionItem SuddenTeamOption;
        public static OptionItem SuddenTeamMaxPlayers;
        //役職の上書き
        public static OptionItem SuddenTeamRole;
        public static OptionItem SuddenRedTeamRole;
        public static OptionItem SuddenBlueTeamRole;
        public static OptionItem SuddenYellowTeamRole;
        public static OptionItem SuddenGreenTeamRole;
        public static OptionItem SuddenPurpleTeamRole;
        public static void CreateOption()
        {
            SuddenSharingRoles = BooleanOptionItem.Create(101001, "SuddenSharingRoles", false, TabGroup.MainSettings, false).SetParent(SuddenDeathModeActive).SetTag(CustomOptionTags.SuddenDeath);
            SuddenCantSeeOtherName = BooleanOptionItem.Create(101012, "SuddenCannotSeeName", false, TabGroup.MainSettings, false).SetParent(SuddenDeathModeActive).SetTag(CustomOptionTags.SuddenDeath);
            SuddenRemainingPlayerCount = BooleanOptionItem.Create(101013, "SuddenRemainingPlayerCount", true, TabGroup.MainSettings, false).SetParent(SuddenDeathModeActive);
            SuddenCanSeeKillflash = BooleanOptionItem.Create(101014, "SuddenCanSeeKillflash", true, TabGroup.MainSettings, false).SetParent(SuddenDeathModeActive);
            SuddenKillcooltime = FloatOptionItem.Create(101015, "SuddenKillcooltime", RoleBase.OptionBaseCoolTime, 15f, TabGroup.MainSettings, false).SetZeroNotation(OptionZeroNotation.Off).SetParent(SuddenDeathModeActive).SetValueFormat(OptionFormat.Seconds);
            SuddenfinishTaskWin = BooleanOptionItem.Create(101016, "SuddenfinishTaskWin", true, TabGroup.MainSettings, false).SetParent(SuddenDeathModeActive);
            SuddenDeathTimeLimit = FloatOptionItem.Create(101017, "SuddenDeathTimeLimit", new(0, 300, 1f), 120f, TabGroup.MainSettings, false).SetZeroNotation(OptionZeroNotation.Infinity).SetParent(SuddenDeathModeActive).SetValueFormat(OptionFormat.Seconds).SetTag(CustomOptionTags.SuddenDeath);
            SuddenDeathReactortime = FloatOptionItem.Create(101018, "SuddenDeathReactortime", new(1, 300, 1f), 15f, TabGroup.MainSettings, false).SetParent(SuddenDeathModeActive).SetValueFormat(OptionFormat.Seconds).SetTag(CustomOptionTags.SuddenDeath);
            SuddenPlayerArrow = BooleanOptionItem.Create(101019, "SuddenPlayerArrow", true, TabGroup.MainSettings, false).SetParent(SuddenDeathModeActive).SetTag(CustomOptionTags.SuddenDeath);
            SuddenArrowSendTime = FloatOptionItem.Create(101020, "SuddenArrowSendTime", new(0, 300, 0.5f), 90f, TabGroup.MainSettings, false).SetParent(SuddenPlayerArrow).SetValueFormat(OptionFormat.Seconds).SetTag(CustomOptionTags.SuddenDeath);
            SuddenArrowSenddis = FloatOptionItem.Create(101021, "SuddenArrowSenddis", new(0, 180, 0.5f), 5f, TabGroup.MainSettings, false).SetParent(SuddenPlayerArrow).SetValueFormat(OptionFormat.Seconds).SetTag(CustomOptionTags.SuddenDeath);
            SuddenTeam = BooleanOptionItem.Create(101022, "SuddenTeam", false, TabGroup.MainSettings, false).SetParent(SuddenDeathModeActive);
            SuddenAddTeamYellow = BooleanOptionItem.Create(101023, "SuddenTeamYellow", true, TabGroup.MainSettings, false).SetParent(SuddenTeam).SetColor(ModColors.Yellow);
            SuddenAddTeamGreen = BooleanOptionItem.Create(101024, "SuddenTeamGreen", true, TabGroup.MainSettings, false).SetParent(SuddenTeam).SetColor(ModColors.Green);
            SuddenAddTeamPurple = BooleanOptionItem.Create(101025, "SuddenTeamPurple", true, TabGroup.MainSettings, false).SetParent(SuddenTeam).SetColor(ModColors.Purple);
            SuddenTeamOption = BooleanOptionItem.Create(101026, "SuddenTeamOption", false, TabGroup.MainSettings, false).SetParent(SuddenTeam);
            SuddenTeamMaxPlayers = IntegerOptionItem.Create(101027, "SuddenTeamMax", new(1, 15, 1), 2, TabGroup.MainSettings, false).SetParent(SuddenTeam);
            SuddenTeamRole = BooleanOptionItem.Create(101028, "SuddenTeamRole", false, TabGroup.MainSettings, false).SetParent(SuddenTeam);

            var StringArray = CustomRolesHelper.AllRoles.Where(role => !InvalidRoles.Contains(role)).Select(role => role.ToString()).ToArray();
            SuddenRedTeamRole = StringOptionItem.Create(101029, "SuddenRedTeamRole", StringArray, 0, TabGroup.MainSettings, false).SetColor(ModColors.Red).SetParent(SuddenTeamRole);
            SuddenBlueTeamRole = StringOptionItem.Create(101030, "SuddenBlueTeamRole", StringArray, 0, TabGroup.MainSettings, false).SetColor(ModColors.Blue).SetParent(SuddenTeamRole);
            SuddenYellowTeamRole = StringOptionItem.Create(101031, "SuddenYellowTeamRole", StringArray, 0, TabGroup.MainSettings, false).SetColor(ModColors.Yellow).SetParent(SuddenTeamRole);
            SuddenGreenTeamRole = StringOptionItem.Create(101032, "SuddenGreenTeamRole", StringArray, 0, TabGroup.MainSettings, false).SetColor(ModColors.Green).SetParent(SuddenTeamRole);
            SuddenPurpleTeamRole = StringOptionItem.Create(101033, "SuddenPurpleTeamRole", StringArray, 0, TabGroup.MainSettings, false).SetColor(ModColors.Purple).SetParent(SuddenTeamRole);
        }
        public static readonly CustomRoles[] InvalidRoles =
        {
            CustomRoles.Crewmate,
            CustomRoles.Emptiness,
            CustomRoles.Phantom,
            CustomRoles.GuardianAngel,
            CustomRoles.SKMadmate,
            CustomRoles.HASFox,
            CustomRoles.HASTroll,
            CustomRoles.GM,
            CustomRoles.TaskPlayerB,
        };
        public static void SideKickChangeTeam(this PlayerControl target, PlayerControl Owner)
        {
            if (TeamRed.Contains(Owner.PlayerId))
            {
                TeamRed.Add(target.PlayerId);
                TeamBlue.Remove(target.PlayerId);
                TeamYellow.Remove(target.PlayerId);
                TeamGreen.Remove(target.PlayerId);
                TeamPurple.Remove(target.PlayerId);
            }
            if (TeamBlue.Contains(Owner.PlayerId))
            {
                TeamRed.Remove(target.PlayerId);
                TeamBlue.Add(target.PlayerId);
                TeamYellow.Remove(target.PlayerId);
                TeamGreen.Remove(target.PlayerId);
                TeamPurple.Remove(target.PlayerId);
            }
            if (TeamYellow.Contains(Owner.PlayerId))
            {
                TeamRed.Remove(target.PlayerId);
                TeamBlue.Remove(target.PlayerId);
                TeamYellow.Add(target.PlayerId);
                TeamGreen.Remove(target.PlayerId);
                TeamPurple.Remove(target.PlayerId);
            }
            if (TeamGreen.Contains(Owner.PlayerId))
            {
                TeamRed.Remove(target.PlayerId);
                TeamBlue.Remove(target.PlayerId);
                TeamYellow.Remove(target.PlayerId);
                TeamGreen.Add(target.PlayerId);
                TeamPurple.Remove(target.PlayerId);
            }
            if (TeamPurple.Contains(Owner.PlayerId))
            {
                TeamRed.Remove(target.PlayerId);
                TeamBlue.Remove(target.PlayerId);
                TeamYellow.Remove(target.PlayerId);
                TeamGreen.Remove(target.PlayerId);
                TeamPurple.Add(target.PlayerId);
            }
        }
    }

    public class SadnessGameEndPredicate : GameEndPredicate
    {
        public override bool CheckForEndGame(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorsByKill;
            if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) return false;
            if (Checkplayer(out reason)) return true;
            if (FinishTaskCheck(out reason)) return true;
            if (CheckGameEndBySabotage(out reason)) return true;

            return false;
        }
        public static bool Checkplayer(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorsByKill;

            if (!PlayerCatch.AllAlivePlayerControls.Any())
            {
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.None);
                return true;
            }
            if (SuddenDeathMode.CheckTeamWin())
            {
                return true;
            }

            if (PlayerCatch.AllAlivePlayersCount == 1)
            {
                var winner = PlayerCatch.AllAlivePlayerControls.FirstOrDefault();
                CustomWinnerHolder.ResetAndSetWinner((CustomWinner)winner.GetCustomRole());
                CustomWinnerHolder.WinnerIds.Add(winner.PlayerId);
                return true;
            }
            if (Lovers.CheckPlayercountWin()) return true;
            return false;
        }
        public static bool FinishTaskCheck(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorsByKill;
            if (SuddenDeathMode.SuddenfinishTaskWin.GetBool() is false) return false;

            foreach (var player in PlayerCatch.AllAlivePlayerControls)
            {
                if (player.GetPlayerState() is not PlayerState state) continue;

                if (!player.IsAlive() || !state.taskState.hasTasks) continue;

                if (state.taskState.IsTaskFinished && player.GetCustomRole().IsCrewmate())
                {
                    CustomWinnerHolder.ResetAndSetWinner((CustomWinner)player.GetCustomRole());
                    CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                    foreach (var dead in PlayerCatch.AllPlayerControls.Where(x => x.PlayerId != player.PlayerId))
                    {
                        if (dead.IsAlive()) dead.RpcMurderPlayerV2(dead);
                        dead.SetRealKiller(player);
                        dead.GetPlayerState().DeathReason = CustomDeathReason.Vote;
                    }
                    return true;
                }
            }
            return false;
        }
    }
}