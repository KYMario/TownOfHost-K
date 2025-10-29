using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using UnityEngine;

namespace TownOfHost.Roles.Crewmate;

public sealed class Android : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Android),
            player => new Android(player),
            CustomRoles.Android,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            12300,
            SetupOptionItem,
            "And",
            "#8a99b7",
            (9, 1),
            false
        );
    public Android(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        removetimer = 0;
        Battery = 0;
        NowVent = 999;
        BatteryText = zero;
        optrmovebattery = RemoveBattery.GetBool();
        optremove = Remove.GetFloat();
        optremovetime = RemoveTime.GetFloat();
        maxcooltime = CoolTime.GetFloat();
        addbattery = TaskAddBattery.GetFloat() * 0.01f;
    }
    static OptionItem TaskAddBattery;
    static OptionItem CoolTime;
    static OptionItem InVentTime;
    static OptionItem RemoveBattery;
    static OptionItem Remove;//減る値
    static OptionItem RemoveTime;//減る時間
    static float addbattery;
    static float maxcooltime;
    static bool optrmovebattery;
    static float optremove;
    static float optremovetime;
    string BatteryText;
    float Battery;
    float removetimer;
    int NowVent;
    enum OptionName
    {
        AndroidRemoveBattery, AndroidRemove, AndroidRemoveTime, AndroidAddTaskBattery
    }

    private static void SetupOptionItem()
    {
        CoolTime = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.Cooldown, new(0f, 180f, 0.5f), 20f, false).SetValueFormat(OptionFormat.Seconds);
        InVentTime = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.EngineerInVentCooldown, new(1f, 60f, 0.5f), 7.5f, false).SetValueFormat(OptionFormat.Seconds);
        TaskAddBattery = FloatOptionItem.Create(RoleInfo, 12, OptionName.AndroidAddTaskBattery, new(1f, 100f, 1f), 10f, false, RemoveBattery).SetValueFormat(OptionFormat.Percent);
        RemoveBattery = BooleanOptionItem.Create(RoleInfo, 13, OptionName.AndroidRemoveBattery, true, false);
        Remove = FloatOptionItem.Create(RoleInfo, 14, OptionName.AndroidRemove, new(1f, 100f, 0.1f), 7.5f, false, RemoveBattery).SetValueFormat(OptionFormat.Percent);
        RemoveTime = FloatOptionItem.Create(RoleInfo, 15, OptionName.AndroidRemoveTime, new(1f, 180f, 0.5f), 4.0f, false, RemoveBattery).SetValueFormat(OptionFormat.Seconds);
        OverrideTasksData.Create(RoleInfo, 16);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        var InMax = Battery * InVentTime.GetFloat();
        if (InMax <= 1f) InMax = 1f;

        AURoleOptions.EngineerCooldown = Battery == 0 ? 200f : ((maxcooltime * 3) - (Battery * maxcooltime * 2));
        AURoleOptions.EngineerInVentMaxTime = Battery == 0 ? 1f : InMax;
    }
    public override bool OnCompleteTask(uint taskid)
    {
        var lastbatt = Battery;

        Battery += addbattery;

        //0なら更新入れる
        if (lastbatt <= 0)
            Player.RpcResetAbilityCooldown(Sync: true);

        BatteryText = GetNowBattery();

        if (AmongUsClient.Instance.AmHost)
            Player.MarkDirtySettings();
        return true;
    }

    //サボタージュ来たら通信障害起きるんでベントから強制排出
    public override bool OnSabotage(PlayerControl __, SystemTypes systemType)
    {
        if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return true;

        if (Player.inVent && NowVent != 999)
            Player.MyPhysics.RpcExitVent(NowVent);
        return true;
    }
    public override void AfterSabotage(SystemTypes systemType) => Player.RpcResetAbilityCooldown(Sync: true);
    public override bool OnEnterVent(PlayerPhysics physics, int ventId) => !Main.IsActiveSabotage;//サボタージュ中なら入れないよっ!

    public override void OnFixedUpdate(PlayerControl player)
    {
        //もう充電がパンパンなら
        if (Battery > 1)
        {
            Battery = 1;
            return;
        }

        //もうすでに充電切れなら
        if (Battery <= 0) return;
        //減らさないなら
        if (!optrmovebattery) return;
        //タスクターンじゃないなら
        if (GameStates.Intro || GameStates.CalledMeeting) return;

        removetimer += Time.fixedDeltaTime;

        if (optremovetime <= removetimer)
        {
            Battery -= optremove * 0.01f;//1/100にして小数に対応させる
            removetimer = 0;
            if (Battery < 0) Battery = 0;

            if (Battery <= 0 && AmongUsClient.Instance.AmHost)//追い出す
            {
                if (Player.inVent && NowVent != 999)
                    Player.MyPhysics.RpcExitVent(NowVent);
            }
            if (GetNowBattery() != BatteryText)
            {
                UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player);
                BatteryText = GetNowBattery();
                if (AmongUsClient.Instance.AmHost)
                {
                    Player.MarkDirtySettings();
                }
            }
        }
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (seer == seen)
            return "<u>" + GetNowBattery() + "</u>";

        return "";
    }
    string GetNowBattery()//バッテリー量の表示
    {
        var battery = Battery * 100;
        if (battery <= 0) return zero;
        if (battery <= 5) return "<mark=#d95327><color=#000000>||</mark>           </size></color>";
        if (battery <= 10) return "<mark=#d96e27><color=#000000>|||</mark>          </size></color>";
        if (battery <= 20) return "<mark=#d9b827><color=#000000>||||</mark>         </size></color>";
        if (battery <= 30) return "<mark=#d6d927><color=#000000>|||||</mark>        </size></color>";
        if (battery <= 40) return "<mark=#b8d13b><color=#000000>||||||</mark>       </size></color>";
        if (battery <= 50) return "<mark=#a7ba47><color=#000000>|||||||</mark>      </size></color>";
        if (battery <= 60) return "<mark=#96ba47><color=#000000>||||||||</mark>     </size></color>";
        if (battery <= 70) return "<mark=#84ba47><color=#000000>|||||||||</mark>    </size></color>";
        if (battery <= 80) return "<mark=#75ba47><color=#000000>||||||||||</mark>   </size></color>";
        if (battery <= 90) return "<mark=#3fb81d><color=#000000>|||||||||||</mark>  </size></color>";
        else return "<mark=#03ff4a><color=#000000>||||||||||</mark> </size></color>";
    }
    const string zero = "<mark=#676767><color=#000000>|</mark>                  </size></color>";
}