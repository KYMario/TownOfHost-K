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
        flashCount = 0;
        flashTimer = 0f;
        isFlashActive = false;
        RemainingMonitoring = (int)OptionMaxMonitoring.GetFloat(); // 初期回数設定（float→int）
    }

    private int flashCount;
    private float flashTimer;
    private bool isFlashActive;
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

        if (!target.IsAlive() && !isFlashActive)
        {
            // 死亡検知 → フラッシュ開始
            isFlashActive = true;
            flashCount = 0;
            flashTimer = 0f;

            // 即1回目
            Utils.AllPlayerKillFlash();
            flashCount++;
            Utils.SendMessage($"{UtilsName.GetPlayerColor(target)} が死亡しました（by Secom）", Player.PlayerId);
        }

        if (isFlashActive)
        {
            flashTimer += Time.fixedDeltaTime;

            if (flashTimer >= 1.5f)
            {
                flashTimer = 0f;
                if (flashCount < 3)
                {
                    Utils.AllPlayerKillFlash();
                    flashCount++;
                }
                else
                {
                    // 完了 → リセット＆残回数を減らす
                    isFlashActive = false;
                    ObserverTarget = byte.MaxValue;
                    RemainingMonitoring = Math.Max(0, RemainingMonitoring - 1);
                }
            }
        }
    }
}