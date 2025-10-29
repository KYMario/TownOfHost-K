using TownOfHost.Roles.Core;
using AmongUs.GameOptions;
using System.Collections.Generic;

namespace TownOfHost.Roles.Crewmate;

public sealed class Gasp : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Gasp),
            player => new Gasp(player),
            CustomRoles.Gasp,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            10700,
            SetupOptionItem,
            "gp",
            "#ab9d44",
            (5, 3)
        );
    public Gasp(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        TaskTrigger = OptionTaskTrigger.GetInt();
        CanSeeMark = false;
        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
    }
    private static OptionItem OptionTaskTrigger;
    private static int TaskTrigger;
    private bool CanSeeMark;
    private bool AfterAbility;
    private static HashSet<Gasp> Gasps = new();

    private static void SetupOptionItem()
    {
        OptionTaskTrigger = IntegerOptionItem.Create(RoleInfo, 10, GeneralOption.TaskTrigger, new(0, 99, 1), 7, false).SetValueFormat(OptionFormat.Pieces);
        OverrideTasksData.Create(RoleInfo, 20);
    }
    public override void Add() => Gasps.Add(this);
    public override void OnDestroy() => Gasps.Clear();

    public override void OnFixedUpdate(PlayerControl player)
    {
        //まだ生きている、もう表示をしたならreturn
        if (player.IsAlive() || AfterAbility || CanSeeMark) return;

        if (!player.IsAlive())
        {
            //死亡時にタスクを完了させている場合
            if (MyTaskState.HasCompletedEnoughCountOfTasks(TaskTrigger) && !GameStates.CalledMeeting)
            {
                //見えるフラグをOnにする
                CanSeeMark = true;
                UtilsNotifyRoles.NotifyRoles(ForceLoop: true);
            }
            else//そうじゃないならただのおにくに
            {
                AfterAbility = true;
            }
        }
    }
    public override void OnStartMeeting()
    {
        //見えるフラグが経ってる場合
        if (CanSeeMark)
        {
            //能力を使用済み判定に
            CanSeeMark = false;
            AfterAbility = true;
        }
    }

    //他人がseerの場合の処理
    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        //流石に会議中も見えたら可哀そうすぎる
        if (isForMeeting) return "";

        foreach (var gasp in Gasps)
        {
            // Gaspがタスクを完了して切られた場合、切ったプレイヤーに★マークを付ける処理
            if (gasp.CanSeeMark)
            {
                var killer = gasp.Player.GetRealKiller();
                if (killer != null && killer.PlayerId == seen.PlayerId)
                {
                    // タスクを終えたGaspを切ったプレイヤーに★マークを表示し、全員から見える
                    return "<color=#ab9d44>★</color>";
                }
            }
        }
        return "";
    }
}