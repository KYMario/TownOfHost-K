using AmongUs.GameOptions;
using Hazel;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Neutral;

public sealed class Banker : RoleBase, IKiller, IAdditionalWinner
{
    //Memo
    //バランスがいまいちわからないので要調整
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Banker),
            player => new Banker(player),
            CustomRoles.Banker,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Neutral,
            14900,
            SetUpOptionItem,
            "bu",
            "#489972",
            (5, 5),
            true,
            introSound: () => GetIntroSound(RoleTypes.Tracker),
            Desc: () =>
            {
                return string.Format(GetString("BankerDesc"), AddWinCoin.GetInt(), TaskAddCoin.GetInt(), KillAddCoin.GetInt(), ChengeCoin.GetInt(), TurnRemoveCoin.GetInt());
            }
        );
    public Banker(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.ForRecompute
    )
    {
        TaskMode = true;
        HaveCoin = FarstCoin.GetInt();
        IsDead = false;
    }
    static OptionItem FarstCoin;
    static OptionItem KillCoolDown;
    static OptionItem KillAddCoin;
    static OptionItem TaskAddCoin;
    static OptionItem AddWinCoin;
    static OptionItem ChengeCoin;
    static OptionItem TurnRemoveCoin;
    static OptionItem DieCanWin;
    static OptionItem DieRemoveCoin;
    static OptionItem DieRemoveTurn;
    enum Option
    {
        BankerFarstCoin,
        BankerTaskAddCoin,
        BankerKillAddCoin,
        BankerChengeCoin,
        BankerTranRemoveCoin,
        BankerDieCanWin,
        BankerDieRemoveCoin,
        BankerDieRemoveTurn,
        BankerWincoin,
    }
    bool TaskMode;
    int HaveCoin;
    bool IsDead;
    static void SetUpOptionItem()
    {
        FarstCoin = IntegerOptionItem.Create(RoleInfo, 9, Option.BankerFarstCoin, new(1, 100, 1), 5, false);
        TaskAddCoin = IntegerOptionItem.Create(RoleInfo, 10, Option.BankerTaskAddCoin, new(1, 100, 1), 5, false);
        KillAddCoin = IntegerOptionItem.Create(RoleInfo, 11, Option.BankerKillAddCoin, new(1, 100, 1), 15, false);
        ChengeCoin = IntegerOptionItem.Create(RoleInfo, 12, Option.BankerChengeCoin, new(0, 100, 1), 5, false);
        TurnRemoveCoin = IntegerOptionItem.Create(RoleInfo, 13, Option.BankerTranRemoveCoin, new(0, 100, 1), 3, false);
        AddWinCoin = IntegerOptionItem.Create(RoleInfo, 14, Option.BankerWincoin, new(1, 100, 1), 60, false);
        DieCanWin = BooleanOptionItem.Create(RoleInfo, 15, Option.BankerDieCanWin, true, false);
        DieRemoveCoin = IntegerOptionItem.Create(RoleInfo, 16, Option.BankerDieRemoveCoin, new(1, 100, 1), 30, false, DieCanWin);
        DieRemoveTurn = IntegerOptionItem.Create(RoleInfo, 17, Option.BankerDieRemoveTurn, new(1, 100, 1), 10, false, DieCanWin);
        KillCoolDown = FloatOptionItem.Create(RoleInfo, 18, GeneralOption.KillCooldown, new(0f, 180f, 0.5f), 15f, false).SetValueFormat(OptionFormat.Seconds);
        OverrideTasksData.Create(RoleInfo, 19);
        RoleAddAddons.Create(RoleInfo, 23);
    }
    public bool CanUseSabotageButton() => false;
    public bool CanUseKillButton() => !TaskMode;
    public float CalculateKillCooldown() => KillCoolDown.GetFloat();
    public override bool CanVentMoving(PlayerPhysics physics, int ventId) => false;
    public override bool CanUseAbilityButton() => Player != PlayerControl.LocalPlayer;
    public override bool OnCompleteTask(uint taskid)
    {
        if (!Player.IsAlive()) return true;

        HaveCoin += TaskAddCoin.GetInt();
        return true;
    }
    public override string GetProgressText(bool comms = false, bool gamelog = false)
    => Player.IsAlive() || DieCanWin.GetBool() ? (gamelog ? "" : (TaskMode ? "[Task]" : "[Kill]") + Utils.ColorString(UtilsRoleText.GetRoleColor(CustomRoles.Banker), $"({HaveCoin})")) : "";
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (Player.IsAlive() || DieCanWin.GetBool())
            if (seen == seer && Is(seen))
            {
                if (AddWinCoin.GetInt() <= HaveCoin) return Utils.AdditionalWinnerMark;
            }
        return "";
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(false);
        AURoleOptions.EngineerCooldown = 0;
        AURoleOptions.EngineerInVentMaxTime = 0.5f;
    }
    public void OnMurderPlayerAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AppearanceTuple;
        if (Is(killer))
        {
            HaveCoin += KillAddCoin.GetInt();
            SendRPC();
        }
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!IsDead && !player.IsAlive())
        {
            IsDead = true;
            HaveCoin -= DieRemoveCoin.GetInt();
            _ = new LateTask(() => UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player), Main.LagTime, "Bankerdie");
        }
    }
    public override RoleTypes? AfterMeetingRole => TaskMode ? RoleTypes.Engineer : RoleTypes.Impostor;
    public override void AfterMeetingTasks()
    {
        if (Player.IsAlive()) HaveCoin -= TurnRemoveCoin.GetInt();
        else HaveCoin -= DieRemoveTurn.GetInt();

        UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player);
    }
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (ChengeCoin.GetInt() <= HaveCoin)
        {
            if (TaskMode && Utils.IsActive(SystemTypes.Comms)) return false;//Hostはタスクモード(エンジ)での切り替えできるからさせないようにする

            if (AmongUsClient.Instance.AmHost)
            {
                foreach (var pc in PlayerCatch.AllPlayerControls)
                {
                    Player.RpcSetRoleDesync(pc == Player && TaskMode ? RoleTypes.Impostor : RoleTypes.Engineer, pc.GetClientId());
                }
                TaskMode = !TaskMode;
            }
            HaveCoin -= ChengeCoin.GetInt();
            SendRPC();
            _ = new LateTask(() =>
            {
                Player.SetKillCooldown();
                UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player);
            }, Main.LagTime, "Bankerchenge");
        }
        return false;
    }
    public override bool CanTask()
    {
        if (!Player.IsAlive()) return false;
        return TaskMode;
    }

    public bool CheckWin(ref CustomRoles winnerRole)
        => AddWinCoin.GetInt() <= HaveCoin && (Player.IsAlive() || DieCanWin.GetBool());

    public void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(TaskMode);
        sender.Writer.Write(HaveCoin);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        TaskMode = reader.ReadBoolean();
        HaveCoin = reader.ReadInt32();
    }
    bool IKiller.OverrideImpVentButton(out string text)
    {
        text = "Banker_Vent";
        return true;
    }
}