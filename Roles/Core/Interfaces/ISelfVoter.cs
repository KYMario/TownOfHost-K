using TownOfHost.Modules;

namespace TownOfHost.Roles.Core.Interfaces;

public interface ISelfVoter
{
    public void AddSelfVoter(PlayerControl player) => SelfVoteManager.AddSelfVotes(player);

    /// <summary>
    /// 投票完了後でも能力を発動できます。
    /// </summary>
    /// <returns></returns>
    public bool CanUseVoted() => false;

    public bool IsCantUse() => false;
}
