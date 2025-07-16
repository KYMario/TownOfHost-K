using System;
using AmongUs.GameOptions;
using TownOfHost.Roles.Core;
using UnityEngine;

namespace TownOfHost.Roles.Crewmate;

public sealed class Secom : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Secom),
            player => new Secom(player),
            CustomRoles.Secom,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            999999,  //(仮)
            SetupOptionItem,
            "Secom",
            "#8a99b7",
            (1, 0),
            false
        );
    public Secom(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Secom_Target = 255;
        flashCount = 0;
        flashTimer = 0f;
        isFlashActive = false;
        RemainingMonitoring = (int)OptionMaxMonitoring.GetFloat();
    }
    private int flashCount;
    private float flashTimer;
    private bool isFlashActive;
    public int RemainingMonitoring { get; private set; }

    public byte Secom_Target { get; private set; }
    public static OptionItem OptionMaxMonitoring;
    private static void SetupOptionItem()
    {
        OptionMaxMonitoring = FloatOptionItem.Create(RoleInfo, 10, Option.MaxMonitoring, new(0f, 99f, 1f), 1f, false)
            .SetValueFormat(OptionFormat.Times);
    }
    enum Option
    {
        MaxMonitoring, // Secomがキル検知できる回数
    }
    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        // Secom本人かつ、残回数が1以上なら→投票先をSecom_Targetに設定
        if (Is(voter) && RemainingMonitoring >= 1f)
        {
            Secom_Target = votedForId;
        }

        return true;
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (RemainingMonitoring <= 0) return;
        if (Secom_Target == byte.MaxValue) return;

        var target = PlayerCatch.GetPlayerById(Secom_Target);
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
                    Secom_Target = byte.MaxValue;
                    RemainingMonitoring = Math.Max(0, RemainingMonitoring - 1);
                }
            }
        }
    }
}