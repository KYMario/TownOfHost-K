namespace TownOfHost.Roles.Core.Interfaces;

public interface IDeathReasonSeeable
{
    /// <summary>
    /// 死因を見られるかどうか
    /// nullならfalseとして強制終了する
    /// </summary>
    /// <param name="seen">死亡済みの対象プレイヤー</param>
    /// <returns>見られるならtrue</returns>
    public bool? CheckSeeDeathReason(PlayerControl seen) => true;
}
