namespace TownOfHost
{
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
