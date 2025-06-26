using AmongUs.GameOptions;
using TownOfHost;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Crewmate;
using UnityEngine;

namespace TownOfHost.Roles.Crewmate;
public sealed class CandleLighter : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Lighter),
            player => new CandleLighter(player),
            CustomRoles.CandleLighter,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            25100,
            SetupOptionItem,
            "cl",
            "#ff7f50",
            from: From.TownOfHost_Y
        );

    public CandleLighter(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        startVision = OptionTaskStartVision.GetFloat();
        endVision = OptionTaskEndVision.GetFloat();
        countStartTime = OptionCountStartTime.GetInt();
        endVisionTime = OptionTaskEndVisionTime.GetInt();
    }

    private static OptionItem OptionTaskStartVision;
    private static OptionItem OptionCountStartTime;
    private static OptionItem OptionTaskEndVisionTime;
    private static OptionItem OptionTaskEndVision;

    enum OptionName
    {
        CandleLighterStartVision,
        CandleLighterCountStartTime,
        CandleLighterEndVisionTime,
        CandleLighterEndVision,
        CandleLighterTimeMoveMeeting,
    }

    private static float startVision;
    private static float endVision;
    private static int endVisionTime;
    private static int countStartTime;

    private float visionTimer;
    private int lastProcessedSecond;

    private static void SetupOptionItem()
    {
        OptionTaskStartVision = FloatOptionItem.Create(RoleInfo, 10, OptionName.CandleLighterStartVision, new(0.5f, 5f, 0.1f), 2.0f, false)
            .SetValueFormat(OptionFormat.Multiplier);
        OptionCountStartTime = IntegerOptionItem.Create(RoleInfo, 11, OptionName.CandleLighterCountStartTime, new(0, 300, 10), 0, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionTaskEndVisionTime = IntegerOptionItem.Create(RoleInfo, 12, OptionName.CandleLighterEndVisionTime, new(60, 1200, 60), 480, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionTaskEndVision = FloatOptionItem.Create(RoleInfo, 13, OptionName.CandleLighterEndVision, new(0f, 0.5f, 0.05f), 0.1f, false)
            .SetValueFormat(OptionFormat.Multiplier);
    }

    public override void Add()
    {
        lastProcessedSecond = -1;
        visionTimer = endVisionTime + countStartTime;
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        float Vision = 0f;

        // 初めの変化待機時間中は最大視界
        if (visionTimer > endVisionTime)
        {
            Vision = startVision;
        }
        else
        {
            Vision = startVision * (visionTimer / endVisionTime);
        }

        // 視界が設定最小視界より小さくなった時は強制
        if (Vision <= endVision)
        {
            Vision = endVision;
        }

        // 停電は無効
        if (Utils.IsActive(SystemTypes.Electrical))
        {
            opt.SetFloat(FloatOptionNames.CrewLightMod, Vision * 5);
        }
        else
        {
            opt.SetFloat(FloatOptionNames.CrewLightMod, Vision);
        }
    }

    public override bool OnCompleteTask(uint taskid)
    {
        if (Player.IsAlive() && IsTaskFinished)
        {
            // タスク完了で視界を一番広く(更新時間をリセット)する
            visionTimer = endVisionTime;
        }

        return true;
    }

    public override void OnFixedUpdate(PlayerControl player)
    {
        if (visionTimer > 0f)
        {
            visionTimer -= Time.fixedDeltaTime;
            int currentSecond = Mathf.FloorToInt(visionTimer);

            if (currentSecond != lastProcessedSecond)
            {
                lastProcessedSecond = currentSecond;
                player.SyncSettings();
            }
        }
    }
}