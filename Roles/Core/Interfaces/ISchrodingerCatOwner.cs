using AmongUs.GameOptions;
using TownOfHost.Roles.Neutral;

namespace TownOfHost.Roles.Core.Interfaces;

/// <summary>
/// シュレディンガーの猫をキルして仲間に引き入れる事ができる役職のインタフェイス
/// </summary>
public interface ISchrodingerCatOwner
{
    /// <summary>
    /// シュレディンガーの猫を切った際の変化先役職
    /// </summary>
    public TeamType SchrodingerCatChangeTo { get; }
    /// <summary>
    /// この役職に切られたシュレディンガーの猫へのオプション変更<br/>
    /// デフォルトではなにもしない
    /// </summary>
    public void ApplySchrodingerCatOptions(IGameOptions option) { }

    /// <summary>
    /// シュレディンガーの猫をキルした際に追加で実行するアクション
    /// </summary>
    public void OnSchrodingerCatKill(SchrodingerCat schrodingerCat) { }
    public void OnBakeCatKill(BakeCat bakeCat) { }
    /// <summary>
    /// 陣営状態
    /// </summary>
    public enum TeamType : byte
    {
        /// <summary>
        /// どこの陣営にも属していない状態
        /// </summary>
        None = 0,

        // 10-49 シェリフキルオプションを作成しない変化先

        /// <summary>
        /// インポスター陣営に所属する状態
        /// </summary>
        Mad = 10,
        /// <summary>
        /// クルー陣営に所属する状態
        /// </summary>
        Crew,

        // 50- シェリフキルオプションを作成する変化先

        /// <summary>
        /// ジャッカル陣営に所属する状態
        /// </summary>
        Jackal = 50,
        /// <summary>
        /// エゴイスト陣営に所属する状態
        /// </summary>
        Egoist,
        /// <summary>
        /// カウントキラーに所属する状態
        /// </summary>
        CountKiller,
        /// <summary>
        /// リモートキラーに所属する状態
        /// </summary>
        Remotekiller,
        /// <summary>
        /// ドッペルゲンガーに所属する状態
        /// </summary>
        DoppelGanger,
        /// <summary>
        /// 天の川陣営に所属する状態
        /// </summary>
        MilkyWay,
        /// <summary>
        /// ベトレイヤーに所属する状態
        /// </summary>
        Betrayer
    }
}
