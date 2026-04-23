using AmongUs.GameOptions;
using UnityEngine;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Neutral;

public sealed class Tuna : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Tuna),
            player => new Tuna(player),
            CustomRoles.Tuna,
            () => OptionCanVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
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
        spawnTimer = 0f;
    }

    static OptionItem OptStopTime;
    static float StopTime;
    static OptionItem OptionCanVent;
    static OptionItem OptionVentCooldown;
    static OptionItem OptionVentMaxTime;

    float stopTimer;
    bool isStopped;
    Vector2 lastPosition;
    bool positionInitialized;
    float spawnTimer;

    enum OptionName
    {
        TunaStopTime,
    }

    private static void SetupOptionItem()
    {
        SoloWinOption.Create(RoleInfo, 9, defo: 15);
        OptStopTime = FloatOptionItem.Create(RoleInfo, 10, OptionName.TunaStopTime, new(0.5f, 5f, 0.5f), 3f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.CanVent, false, false);
        OptionVentCooldown = FloatOptionItem.Create(RoleInfo, 12, GeneralOption.Cooldown, new(0f, 180f, 0.5f), 15f, false, OptionCanVent)
            .SetValueFormat(OptionFormat.Seconds);
        OptionVentMaxTime = FloatOptionItem.Create(RoleInfo, 13, GeneralOption.EngineerInVentCooldown, new(0f, 180f, 0.5f), 0f, false, OptionCanVent)
            .SetZeroNotation(OptionZeroNotation.Infinity)
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = OptionVentCooldown.GetFloat();
        AURoleOptions.EngineerInVentMaxTime = OptionVentMaxTime.GetFloat();
    }

    public override bool CanVentMoving(PlayerPhysics physics, int ventId) => OptionCanVent.GetBool();

    static bool IsUsingMovingPlatform(PlayerControl pc)
    {
        if (pc.MyPhysics.Animations.IsPlayingAnyLadderAnimation()) return true;
        if (pc.onLadder) return true;
        if ((MapNames)Main.NormalOptions.MapId == MapNames.Airship
            && Vector2.Distance(pc.GetTruePosition(), new Vector2(7.76f, 8.56f)) <= 1.9f) return true;
        if (pc.MyPhysics.Animations.Animator.GetCurrentAnimation()?.name?.Contains("Zipline") == true) return true;
        if (pc.MyPhysics.Animations.Animator.GetCurrentAnimation()?.name?.Contains("Platform") == true) return true;
        return false;
    }

    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!player.IsAlive()) return;
        if (GameStates.CalledMeeting || GameStates.Intro) return;

        spawnTimer += Time.fixedDeltaTime;
        if (spawnTimer < 5f)
        {
            stopTimer = 0f;
            isStopped = false;
            lastPosition = player.GetTruePosition();
            return;
        }

        // ★ 梯子・ぬーん・ジップラインはカウントしない
        if (IsUsingMovingPlatform(player))
        {
            stopTimer = 0f;
            isStopped = false;
            lastPosition = player.GetTruePosition();
            return;
        }

        var currentPos = player.GetTruePosition();

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
            if (!isStopped)
                isStopped = true;

            stopTimer += Time.fixedDeltaTime;

            if (stopTimer >= StopTime)
            {
                PlayerState.GetByPlayerId(player.PlayerId).DeathReason = CustomDeathReason.Suicide;
                player.RpcMurderPlayerV2(player);
                stopTimer = 0f;
                isStopped = false;
            }
        }
        else
        {
            stopTimer = 0f;
            isStopped = false;
        }
    }

    public override void AfterMeetingTasks()
    {
        stopTimer = 0f;
        isStopped = false;
        positionInitialized = false;
        spawnTimer = 0f;
    }

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