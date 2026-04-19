using AmongUs.GameOptions;
using UnityEngine;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;

public sealed class Snowman : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Snowman),
            player => new Snowman(player),
            CustomRoles.Snowman,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            12000,
            SetupOptionItem,
            "snm",
            "#c4d6e3",
            (8, 1)
        );
    public Snowman(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        FirstVision = OptionFirstVision.GetFloat();
        Minvision = OptionMinVision.GetFloat();
        MeltedSteps = OptionMeltedSteps.GetInt();
        ElectricalIgnoreMelt = OptionElectricalIgnoreMelt.GetBool();

        NowVision = FirstVision;
        OldProportion = 0;
        callcount = 0;
    }
    static OptionItem OptionFirstVision; static float FirstVision;
    static OptionItem OptionMinVision; static float Minvision;//最小の視野
    static OptionItem OptionMeltedSteps; static float MeltedSteps;//完全に溶ける歩数 
    static OptionItem OptionElectricalIgnoreMelt; static bool ElectricalIgnoreMelt;//停電中に解けない

    int callcount;
    enum OptionName
    {
        SnowmanFirstVision,
        SnowmanMinVision,
        ElectricalIgnoreMelt,
        SnowmanMeltedSteps
    }
    static void SetupOptionItem()
    {
        OptionFirstVision = FloatOptionItem.Create(RoleInfo, 10, OptionName.SnowmanFirstVision, new(0.05f, 5, 0.05f), 1.25f, false).SetValueFormat(OptionFormat.Multiplier);
        OptionMinVision = FloatOptionItem.Create(RoleInfo, 11, OptionName.SnowmanMinVision, new(0f, 5, 0.05f), 0.15f, false).SetValueFormat(OptionFormat.Multiplier);
        OptionMeltedSteps = FloatOptionItem.Create(RoleInfo, 12, OptionName.SnowmanMeltedSteps, new(100f, 3000f, 100f), 1800f, false);
        OptionElectricalIgnoreMelt = BooleanOptionItem.Create(RoleInfo, 13, OptionName.ElectricalIgnoreMelt, true, false);

        OverrideTasksData.Create(RoleInfo, 20);
    }
    float OldProportion;
    float NowVision;
    float stoptimer;
    float NowWalkCount;//歩数計
    Vector2 OldPosition;//前チェックの位置
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (AmongUsClient.Instance.AmHost is false || player.IsAlive() is false) return;
        if (MyTaskState.IsTaskFinished) return;
        if (Utils.IsActive(SystemTypes.Electrical) && ElectricalIgnoreMelt) return;

        if (player.GetTruePosition() == OldPosition)
        {
            stoptimer += Time.fixedDeltaTime;
            if (stoptimer > 1)
            {
                NowWalkCount += 5;
                stoptimer = 0;
                CheckVision();
            }
            return;
        }
        if (OldPosition == new Vector2(50f, 50f)
        || Player.inVent || Player.MyPhysics.Animations.IsPlayingEnterVentAnimation()
        || Player.MyPhysics.Animations.IsPlayingAnyLadderAnimation() || Player.inMovingPlat
        || !Player.CanMove)//歩行以外は除外
        {
            OldPosition = player.GetTruePosition();
            return;
        }

        var distance = Vector2.Distance(OldPosition, player.GetTruePosition());
        OldPosition = player.GetTruePosition();
        if (distance > 5) return; //Snapto等で長距離移動が起こった場合はノーカウント
        //歩数計の確認
        NowWalkCount += distance;
        stoptimer = 0;
        CheckVision();
    }
    public void CheckVision()
    {
        var Proportion = NowWalkCount / MeltedSteps;
        var MeltVision = FirstVision - Minvision;//減少を受ける視野
        if (Proportion > 1) Proportion = 1;
        var vision = (MeltVision - (MeltVision * Proportion)) + Minvision;

        if (Proportion - OldProportion > 0.05f)//最大20回更新
        {
            OldProportion = Proportion;
            Player.RpcProtectedMurderPlayer();
            NowVision = vision;
            Logger.Info($"減少率:{Proportion} 歩数:{NowWalkCount} , 現在の視野{NowVision}", "Snowman");
            Player.MarkDirtySettings();
            callcount++;
            if (callcount == 20) Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[0]);
        }
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetFloat(FloatOptionNames.CrewLightMod, NowVision);
    }
    public override void CheckWinner(GameOverReason reason)
    {
        if (callcount <= 2) Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[1]);
    }
    public static System.Collections.Generic.Dictionary<int, Achievement> achievements = new();
    [Attributes.PluginModuleInitializer]
    public static void Load()
    {
        var n1 = new Achievement(RoleInfo, 0, 1, 0, 0);
        var l1 = new Achievement(RoleInfo, 1, 1, 0, 1);
        achievements.Add(0, n1);
        achievements.Add(1, l1);
    }
}