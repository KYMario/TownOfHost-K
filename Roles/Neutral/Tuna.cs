using AmongUs.GameOptions;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Neutral;

public sealed class Tuna : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Tuna),
            player => new Tuna(player),
            CustomRoles.Tuna,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            23030,
            SetupOptionItem,
            "tn",
            "#8cffff",
            (6, 2),
            from: From.SuperNewRoles
        );

    public Tuna(PlayerControl player)
        : base(RoleInfo, player, () => HasTask.False)
    {
        StopTime = OptStopTime.GetFloat();
        stopTimer = 0f;
        isStopped = false;
        lastPosition = Vector2.zero;
        positionInitialized = false;

        spawnTimer = 0f;   // ★ スポーン後の無敵タイマー
    }

    static OptionItem OptStopTime;
    static float StopTime;

    float stopTimer;
    bool isStopped;
    Vector2 lastPosition;
    bool positionInitialized;

    float spawnTimer;   // ★ スポーン後の無敵時間（5秒）

    enum OptionName
    {
        TunaStopTime,
    }

    private static void SetupOptionItem()
    {
        SoloWinOption.Create(RoleInfo, 9, defo: 15);
        OptStopTime = FloatOptionItem.Create(RoleInfo, 10, OptionName.TunaStopTime, new(0.5f, 5f, 0.5f), 3f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!player.IsAlive()) return;
        if (GameStates.CalledMeeting || GameStates.Intro) return;

        // ★ スポーン後 5 秒間はカウントしない
        spawnTimer += Time.fixedDeltaTime;
        if (spawnTimer < 5f)
        {
            stopTimer = 0f;
            isStopped = false;
            lastPosition = player.GetTruePosition();
            return;
        }

        var currentPos = player.GetTruePosition();

        // 初回は位置を記録するだけ
        if (!positionInitialized)
        {
            lastPosition = currentPos;
            positionInitialized = true;
            return;
        }

        float moved = Vector2.Distance(currentPos, lastPosition);
        lastPosition = currentPos;

        if (moved < 0.01f)
        {
            // 止まっている
            if (!isStopped)
                isStopped = true;

            stopTimer += Time.fixedDeltaTime;

            if (stopTimer >= StopTime)
            {
                // 自爆
                PlayerState.GetByPlayerId(player.PlayerId).DeathReason = CustomDeathReason.Suicide;
                player.RpcMurderPlayerV2(player);
                stopTimer = 0f;
                isStopped = false;
            }
        }
        else
        {
            // 動いたらリセット
            stopTimer = 0f;
            isStopped = false;
        }
    }

    // 会議後リセット
    public override void AfterMeetingTasks()
    {
        stopTimer = 0f;
        isStopped = false;
        positionInitialized = false;

        spawnTimer = 0f;   // ★ 会議後も 5 秒の猶予を再付与
    }

    // 勝利判定
    public static bool CheckWin(ref GameOverReason reason)
    {
        foreach (var pc in PlayerCatch.AllPlayerControls)
        {
            if (pc.GetRoleClass() is not Tuna) continue;
            if (!pc.IsAlive()) continue;

            if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Tuna, pc.PlayerId))
            {
                CustomWinnerHolder.NeutralWinnerIds.Add(pc.PlayerId);
                reason = GameOverReason.ImpostorsByKill;
                return true;
            }
        }
        return false;
    }
}
