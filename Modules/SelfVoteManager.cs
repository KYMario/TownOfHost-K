using System.Collections.Generic;
using TownOfHost.Roles.Madmate;
using static TownOfHost.Modules.MeetingVoteManager;

namespace TownOfHost.Modules
{
    public static class SelfVoteManager
    {
        ///<summary>
        ///MeetingVoteManagerのSkip
        ///</summary>
        public static byte SkipId = Skip;
        public static Dictionary<byte, bool> CheckVote = new();
        [Attributes.GameModuleInitializer]
        public static void Init()
        {
            CheckVote.Clear();
        }
        public static void AddS(PlayerControl player)
        {
            CheckVote.TryAdd(player.PlayerId, false);
        }

        public enum VoteStatus
        {
            Skip,
            Self,
            Vote,
        }
        ///<summary>
        /// 自投票モードのチェック
        ///</summary>
        ///<returns>自投票モードならtrueを返す</returns>
        /// <param name="status">投票のステータスを返す</param>
        public static bool CheckSelfVoteMode(PlayerControl player, byte id, out VoteStatus status)
        {
            Check(player);
            var mode = CheckVote[player.PlayerId];
            if (player.PlayerId == id)
            {
                status = VoteStatus.Self;
                CheckVote[player.PlayerId] = !mode;
                mode = !mode;
            }
            else if (Skip == id)
                status = VoteStatus.Skip;
            else
                status = VoteStatus.Vote;
            Logger.Info($"player: {Main.AllPlayerNames[player.PlayerId]} mode: {mode} status: {status}", "SelfVoteManager");
            return mode;
        }

        private static void Check(PlayerControl player)
        {
            if (!CheckVote.ContainsKey(player.PlayerId))
            {
                AddS(player);
                Logger.Info($"×チェックに失敗 {player.PlayerId}を追加しました", "SelfVoteManager");
            }
        }

        public static void SetMode(PlayerControl player, bool mode)
            => CheckVote[player.PlayerId] = mode;

        public static bool Canuseability()
        {
            if (MadAvenger.Skill) return false;
            if (Options.firstturnmeeting && Options.FirstTurnMeetingCantability.GetBool() && MeetingStates.FirstMeeting) return false;
            return true;
        }
    }
}