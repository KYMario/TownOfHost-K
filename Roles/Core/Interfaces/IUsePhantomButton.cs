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
    public void CheckOnClick(ref bool AdjustKillCooldown, ref bool? ResetCooldown)
    {
        if (!UseOneclickButton)
        {
            AdjustKillCooldown = true;
            return;
        }
        OnClick(ref AdjustKillCooldown, ref ResetCooldown);
    }
    /// <summary>
    /// ファントムワンクリックを使った時に呼ばれる関数<br/>
    /// クールダウンのリセットが発動後行われる。<br/><br/>
    /// AdjustKillCooldownがtureでキルクールの調整が行われる<br/>
    /// ↑ 役職で使用後キルクールダウンをリセットする時はfalse<br/><br/>
    /// ResetCooldownがtrueでファントムボタンのクールリセットを入れる<br/>
    /// ↑ falseでクール無し<br/><br/>
    /// [ホストのみ]
    ///  </summary>
    /// <param name="AdjustKillCooldown">trueで使用後キルクールの調整処理を行う</param>
    /// <param name="ResetCooldown">trueでアビリティリセット処理を入れる<br/> nullでファントム戻し処理もいれない</param>
    public void OnClick(ref bool AdjustKillCooldown, ref bool? ResetCooldown)
    { }
    /// <summary>ワンクリックボタンが使えるか</summary>
    public bool UseOneclickButton => true;
    /// <summary>ファントム置き換えにするかどうか</summary>
    public bool IsPhantomRole => true;
}