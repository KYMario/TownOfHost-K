using System.Linq;

using TownOfHost.Roles;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Impostor;
using TownOfHost.Roles.Neutral;
// ===== ゲーム終了条件 =====
// 通常ゲーム用
namespace TownOfHost
{
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
            if (Assassin.assassin?.NowState is Assassin.AssassinMeeting.Collected) return false;

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
                    TownOfHost.Roles.Madmate.MadBetrayer.CheckCount(ref Crew, ref MadBetrayer);
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
}