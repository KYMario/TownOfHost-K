using System;
using AmongUs.GameOptions;
using TownOfHost.Roles.Core;
using UnityEngine;

namespace TownOfHost.Roles.Crewmate;

public sealed class Observer : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Observer),
            player => new Observer(player),
            CustomRoles.Observer,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            999999,  //(仮)
            SetupOptionItem,
            "Observer",
            "#8a99b7",
            (1, 0),
            false
        );
    public Observer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        ObserverTarget = 255;
        RemainingMonitoring = (int)OptionMaxMonitoring.GetFloat(); // 初期回数設定（float→int）
    }

    private byte ObserverTarget;

    public int RemainingMonitoring { get; private set; }

    public static OptionItem OptionMaxMonitoring;

    private static void SetupOptionItem()
    {
        OptionMaxMonitoring = FloatOptionItem.Create(RoleInfo, 10, Option.MaxMonitoring, new(0f, 99f, 1f), 1f, false)
            .SetValueFormat(OptionFormat.Times);
    }

    enum Option
    {
        MaxMonitoring, // Observerがキル検知できる回数
    }

    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        // Observer本人かつ、残回数が1以上なら→投票先をObserverTargetに設定
        if (Is(voter) && RemainingMonitoring >= 1)
        {
            ObserverTarget = votedForId;
        }

        return true;
    }

    public override void OnFixedUpdate(PlayerControl player)
    {
        if (RemainingMonitoring <= 0) return;
        if (ObserverTarget == byte.MaxValue) return;

        var target = PlayerCatch.GetPlayerById(ObserverTarget);
        if (target == null) return;

        if (!target.IsAlive())
        {
            // 死亡検知 → 即1回だけキルフラッシュ
            Utils.AllPlayerKillFlash();
            Utils.SendMessage($"{UtilsName.GetPlayerColor(target)} が死亡しました（by Observer）", Player.PlayerId);

            // 状態リセット＆残回数を減らす
            ObserverTarget = byte.MaxValue;
            RemainingMonitoring = Math.Max(0, RemainingMonitoring - 1);
        }
    }
}