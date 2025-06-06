using System.Collections.Generic;
using UnityEngine;

namespace TownOfHost.Roles.Core.Interfaces;

/// <summary>
///ワンクリックファントムボタンを使う役職
/// <summary>
public interface IUsePhantomButton
{
    public static Dictionary<byte, float> IPPlayerKillCooldown = new();
    public void Init(PlayerControl player)
    {
        if (!IPPlayerKillCooldown.TryAdd(player.PlayerId, 0))
        {
            IPPlayerKillCooldown[player.PlayerId] = 0;
            //Logger.Info($"{player.Data.PlayerName}ファントムワンクリに追加済みなのでリセット", "IusePhantomButton");
            return;
        }
        Logger.Info($"{player.Data.GetLogPlayerName()}:ファントムワンクリに追加", "IusePhantomButton");
    }
    //キルクールを...
    public void FixedUpdate(PlayerControl player)
    {
        if (!player.IsAlive()) return;
        if (player.GetRoleClass() is IUsePhantomButton)
            if (!GameStates.Intro && GameStates.InGame && GameStates.IsInTask && !GameStates.IsMeeting)
            {
                if (player.inVent) return;
                if (IPPlayerKillCooldown.TryGetValue(player.PlayerId, out var now))
                {
                    var killcool = now + Time.fixedDeltaTime;
                    IPPlayerKillCooldown[player.PlayerId] = killcool;
                }
                else Init(player);
            }
    }
    public void CheckOnClick(ref bool resetkillcooldown, ref bool? fall)
    {
        if (!UseOneclickButton)
        {
            resetkillcooldown = false;
            return;
        }
        OnClick(ref resetkillcooldown, ref fall);
    }
    /// <summary>
    /// ファントムワンクリックを使った時に呼ばれる関数<br/>
    /// クールダウンのリセットが発動後行われる。<br/><br/>
    /// resetkillcooldownがtureでキルクールダウンの調整リセットが入らない<br/>
    /// ↑ 役職で使用後キルクールダウンをリセットする時はtrue<br/><br/>
    /// fallがtrueでファントムワンクリックの調整リセットが入らない<br/>
    /// ↑ ジャッカルのサイドキック失敗ですぐ使えるようになったりラジバンダリ<br/><br/>
    ///  </summary>
    /// <param name="resetkillcooldown">trueで使用後キルクールダウンの調整処理を行わない</param>
    /// <param name="fall">trueでアビリティリセット処理を入れない nullで役職処理もいれない</param>
    public void OnClick(ref bool resetkillcooldown, ref bool? fall)
    { }
    /// <summary>ワンクリックボタンが使えるか</summary>
    public bool UseOneclickButton => true;
    /// <summary>ファントム置き換えにするかどうか</summary>
    public bool IsPhantomRole => true;
}