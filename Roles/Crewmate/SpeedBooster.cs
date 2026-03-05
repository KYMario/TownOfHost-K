using System.Linq;
using System.Collections.Generic;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;

public sealed class SpeedBooster : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(SpeedBooster),
            player => new SpeedBooster(player),
            CustomRoles.SpeedBooster,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            11500,
            SetupOptionItem,
            "sb",
            "#00ffff",
            (7, 1),
            from: From.TownOfHost
        );
    public SpeedBooster(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        UpSpeed = OptionUpSpeed.GetFloat();
        TaskTrigger = OptionTaskTrigger.GetInt();

        BoostTarget = byte.MaxValue;
    }

    static OptionItem OptionUpSpeed; //加速値
    static OptionItem OptionTaskTrigger; //効果を発動するタスク完了数
    enum OptionName
    {
        SpeedBoosterUpSpeed,
        SpeedBoosterTaskTrigger
    }
    static float UpSpeed;
    static int TaskTrigger;
    byte BoostTarget;

    private static void SetupOptionItem()
    {
        OptionUpSpeed = FloatOptionItem.Create(RoleInfo, 10, OptionName.SpeedBoosterUpSpeed, new(0.2f, 5.0f, 0.2f), 1.4f, false)
                .SetValueFormat(OptionFormat.Multiplier);
        OptionTaskTrigger = IntegerOptionItem.Create(RoleInfo, 11, OptionName.SpeedBoosterTaskTrigger, new(1, 99, 1), 5, false)
            .SetValueFormat(OptionFormat.Pieces);
    }
    public override bool OnCompleteTask(uint taskid)
    {
        var playerId = Player.PlayerId;
        if (Player.IsAlive()
            && BoostTarget == byte.MaxValue
            && MyTaskState.HasCompletedEnoughCountOfTasks(TaskTrigger))
        {   //ｽﾋﾟﾌﾞが生きていて、SpeedBoostTargetに登録済みでなく、全タスク完了orトリガー数までタスクを完了している場合
            var rand = IRandom.Instance;
            List<PlayerControl> targetPlayers = new();
            targetPlayers.AddRange(PlayerCatch.AllAlivePlayerControls.ToArray());
            if (targetPlayers.Count >= 1)
            {
                var target = targetPlayers[rand.Next(0, targetPlayers.Count)];
                Logger.Info("スピードブースト先:" + target.GetNameWithRole().RemoveHtmlTags(), "SpeedBooster");
                BoostTarget = target.PlayerId;
                Main.AllPlayerSpeed[BoostTarget] *= UpSpeed;
                target.MarkDirtySettings();
                Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[0]);
            }
            else //ターゲットが0ならアップ先をプレイヤーをnullに
            {
                BoostTarget = byte.MaxValue;
                Logger.seeingame("Error.SpeedBoosterNullException");
                Logger.Warn("スピードブースト先がnullです。", "SpeedBooster");
            }
        }

        return true;
    }
    public static Dictionary<int, Achievement> achievements = new();
    [Attributes.PluginModuleInitializer]
    public static void Load()
    {
        var n1 = new Achievement(RoleInfo, 0, 1, 0, 0);
        achievements.Add(0, n1);
    }
}