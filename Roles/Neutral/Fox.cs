using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using UnityEngine;
using HarmonyLib;

using TownOfHost.Modules;
using TownOfHost.Modules.ChatManager;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Patches;

namespace TownOfHost.Roles.Neutral;

public sealed class Fox : RoleBase, ISystemTypeUpdateHook
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Fox),
            player => new Fox(player),
            CustomRoles.Fox,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Neutral,
            15000,
            SetupOptionItem,
            "Fox",
            "#d288ee",
            (6, 0),
            false,
            countType: CountTypes.Fox,
            assignInfo: new RoleAssignInfo(CustomRoles.Fox, CustomRoleTypes.Neutral)
            {
                AssignCountRule = new(1, 1, 1)
            },
            from: From.None,
            Desc: () =>
            {
                return string.Format(GetString("FoxDesc"), OptWinTaskCount.GetInt(), OptGiveGuardTaskCount.GetInt(), OptGiveGuardMax.GetInt());
            }
        );
    public Fox(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.ForRecompute
    )
    {
        MyTaskState.NeedTaskCount = OptWinTaskCount.GetInt();
        GiveGuardMax = OptGiveGuardMax.GetInt();
        GiveGuardTaskCount = OptGiveGuardTaskCount.GetInt();
        WinTaskCount = OptWinTaskCount.GetInt();
        Engventcool = OptEngVentCoolDown.GetFloat();
        Engventinmax = OptEngVentInmaxtime.GetFloat();
        TellDie = OptTellDie.GetBool();
        canseeguardcount = OptCanseeGuardCount.GetBool();
        canwin3player = OptCanWin3players.GetBool();

        Guard = 0;
        checktaskwinflag = false;
        FoxRoom = null;
    }
    int Guard;
    int Taskcount;
    static OptionItem OptGiveGuardTaskCount; static int GiveGuardTaskCount;
    static OptionItem OptGiveGuardMax; static int GiveGuardMax;
    static OptionItem OptWinTaskCount; static int WinTaskCount;
    static OptionItem OptEngVentCoolDown; static float Engventcool;
    static OptionItem OptEngVentInmaxtime; static float Engventinmax;
    static OptionItem OptTellDie; static bool TellDie;
    static OptionItem OptCanseeGuardCount; static bool canseeguardcount;
    static OptionItem OptCanWin3players; static bool canwin3player;
    bool checktaskwinflag;
    SystemTypes? FoxRoom;
    enum OptionName
    {
        FoxGiveGuardTaskcount,
        FoxGiveGuardMax,
        FoxCanseeGuardCount,
        FoxTellDie,
        Foxwintaskcount,
        Fox3playersCanwin
    }

    private static void SetupOptionItem()
    {
        SoloWinOption.Create(RoleInfo, 9, defo: 15);
        OptEngVentCoolDown = FloatOptionItem.Create(RoleInfo, 10, StringNames.EngineerCooldown, OptionBaseCoolTime, 10, false).SetValueFormat(OptionFormat.Seconds);
        OptEngVentInmaxtime = FloatOptionItem.Create(RoleInfo, 11, StringNames.EngineerInVentCooldown, new(0.5f, 30, 0.5f), 3, false).SetValueFormat(OptionFormat.Seconds);
        OptGiveGuardTaskCount = IntegerOptionItem.Create(RoleInfo, 12, OptionName.FoxGiveGuardTaskcount, new(1, 99, 1), 3, false);
        OptGiveGuardMax = IntegerOptionItem.Create(RoleInfo, 13, OptionName.FoxGiveGuardMax, new(0, 99, 1), 2, false);
        OptCanseeGuardCount = BooleanOptionItem.Create(RoleInfo, 14, OptionName.FoxCanseeGuardCount, false, false);
        OptTellDie = BooleanOptionItem.Create(RoleInfo, 16, OptionName.FoxTellDie, false, false);
        OptWinTaskCount = IntegerOptionItem.Create(RoleInfo, 20, OptionName.Foxwintaskcount, new(1, 99, 1), 6, false);
        OptCanWin3players = BooleanOptionItem.Create(RoleInfo, 21, OptionName.Fox3playersCanwin, false, false);

        OverrideTasksData.Create(RoleInfo, 25);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = Engventcool;
        AURoleOptions.EngineerInVentMaxTime = Engventinmax;
    }
    public override bool OnCompleteTask(uint taskid)
    {
        if (MyTaskState.HasCompletedEnoughCountOfTasks(WinTaskCount)) checktaskwinflag = true;
        //もう上限に達しているなら処理終わり
        if (GiveGuardMax <= Guard) return true;
        //タスクカウントを増やす
        Taskcount++;
        //ガードを追加するタスク数に達したら
        if (GiveGuardTaskCount <= Taskcount)
        {
            Taskcount = 0;
            Guard++;
        }
        return true;
    }
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        if (info.IsAccident || info.IsSuicide || info.IsFakeSuicide) return true;
        if (Guard > 0)
        {
            var (killer, target) = info.AttemptTuple;

            killer.SetKillCooldown(target: target, force: true);
            Guard--;
            Logger.Info($"ガード残り:{Guard}", "Fox");
            info.GuardPower = 2;
            if (canseeguardcount) UtilsNotifyRoles.NotifyRoles(SpecifySeer: target);
            return true;
        }
        return true;
    }
    public override CustomRoles TellResults(PlayerControl player)
    {
        //ぽんこつ占い師のぽんこつ占い師で死ぬのはかわいそう('ω')
        if (TellDie && player.IsAlive() && player != null)
        {
            Player.RpcExileV2();
            MyState.DeathReason = CustomDeathReason.Spell;
            MyState.SetDead();

            if ((PlayerCatch.AllPlayerControls.Any(pc => pc.Is(CustomRoles.Guesser)) || CustomRolesHelper.CheckGuesser()) && !Options.ExHideChatCommand.GetBool())
                ChatManager.SendPreviousMessagesToAll();

            UtilsGameLog.AddGameLog($"Fox", $"{UtilsName.GetPlayerColor(Player, true)}(<b>{UtilsRoleText.GetTrueRoleName(Player.PlayerId, false)}</b>) [{Utils.GetVitalText(Player.PlayerId, true)}]");
            UtilsGameLog.AddGameLogsub($"\n\t┗ {GetString("Skillplayer")}{UtilsName.GetPlayerColor(player, true)}(<b>{UtilsRoleText.GetTrueRoleName(player.PlayerId, false)}</b>)");

            var meetingHud = MeetingHud.Instance;
            var hudManager = DestroyableSingleton<HudManager>.Instance.KillOverlay;
            {
                GameDataSerializePatch.SerializeMessageCount++;
                foreach (var pc in PlayerCatch.AllAlivePlayerControls)
                {
                    if (pc == Player) continue;
                    pc.Data.IsDead = false;
                }
                RPC.RpcSyncAllNetworkedPlayer(Player.GetClientId());
                GameDataSerializePatch.SerializeMessageCount--;
            }

            Utils.SendMessage(UtilsName.GetPlayerColor(Player, true) + GetString("Meetingkill"), title: GetString("MSKillTitle"));
            foreach (var go in PlayerCatch.AllPlayerControls.Where(pc => pc != null && !pc.IsAlive()))
            {
                Utils.SendMessage(string.Format(GetString("FoxTelldie"), UtilsName.GetPlayerColor(Player, true)), go.PlayerId, GetString("RMSKillTitle"));
            }
            MeetingVoteManager.ResetVoteManager(Player.PlayerId);
        }
        return CustomRoles.NotAssigned;
    }
    public override void AfterMeetingTasks()
    {
        timer = 0;
        List<SystemTypes> rooms = new();
        ShipStatus.Instance.AllRooms.Where(room => room?.RoomId is not null and not SystemTypes.Hallway).Do(r => rooms.Add(r.RoomId));

        var rand = IRandom.Instance;
        FoxRoom = rooms[rand.Next(0, rooms.Count)];
        Logger.Info($"NextTask : {FoxRoom}", "Fox");
    }
    float timer = 0;
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (FoxRoom == null || !player.IsAlive() || !AmongUsClient.Instance.AmHost) return;

        if (MyState.HasSpawned) timer += Time.fixedDeltaTime;

        var nowroom = player.GetPlainShipRoom();
        if (nowroom == null) return;
        if (FoxRoom == nowroom.RoomId)
        {
            if (timer > 0.5f)
            {
                player.RpcProtectedMurderPlayer();
                Logger.Info($"{FoxRoom}に{player.name}が来たよ", "Fox");
                FoxRoom = null;
                _ = new LateTask(() => UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player), 0.3f, "FoxChengeRoom", null);
                return;
            }
            //スポーンがもうすぐそこならぬるいので変えてやる!!
            List<SystemTypes> rooms = new();
            ShipStatus.Instance.AllRooms.Where(room => room?.RoomId is not null and not SystemTypes.Hallway && room?.RoomId != FoxRoom).Do(r => rooms.Add(r.RoomId));

            var rand = IRandom.Instance;
            FoxRoom = rooms[rand.Next(0, rooms.Count)];
            Logger.Info($"NextTask : {FoxRoom}", "Fox");
            _ = new LateTask(() => UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player), 0.3f, "FoxChengeRoom", null);
        }
    }
    public override string MeetingAddMessage()
    {
        if (!Player.IsAlive() || FoxRoom == null) return "";

        var chance = IRandom.Instance.Next(100);
        if (chance > 95) return $"<color=#d288ee>{GetString("FoxAliveMeg1")}</color>";
        if (chance > 90) return $"<color=#d288ee>{GetString("FoxAliveMeg2")}</color>";
        if (chance > 85) return $"<color=#d288ee>{GetString("FoxAliveMeg3")}</color>";
        return $"<color=#d288ee>{GetString("FoxAliveMeg")}</color>";
    }
    public override string GetProgressText(bool comms = false, bool GameLog = false) => $"<color=#{(checktaskwinflag ? "d288ee" : "5e5e5e")}>({(canseeguardcount ? $"{Guard}" : "?")})</color>";
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (isForMeeting || seer != seen || !Player.IsAlive() || FoxRoom == null) return "";
        return $"<color=#d288ee>{string.Format(GetString("FoxRoomMission"), $"<color=#cccccc><b>{GetString($"{FoxRoom}")}<b></color>")}</color>";
    }
    public static bool SFoxCheckWin(ref GameOverReason reason)
    {
        foreach (var pc in PlayerCatch.AllPlayerControls.Where(pc => pc.Is(CustomRoles.Fox)))
        {
            if (pc.GetRoleClass() is Fox fox)
            {
                if (fox.FoxCheckWin(ref reason)) return true;
            }
        }
        return false;
    }
    public bool FoxCheckWin(ref GameOverReason reason)
    {
        //3人1w1c1foxの状態を避けるために3人以下なら勝てないようにする
        //4人以上か3人以下でも勝利がOn　　　　　　　　　　　　　　　　　　生存していて　　　　　タスクが完了している
        if ((PlayerCatch.AllAlivePlayersCount > 3 || canwin3player) && Player.IsAlive() && checktaskwinflag)
        {
            if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Fox, Player.PlayerId))
            {
                CustomWinnerHolder.NeutralWinnerIds.Add(Player.PlayerId);
                reason = GameOverReason.ImpostorsByKill;
                return true;
            }
        }
        return false;
    }
    public int FoxCount()
    {
        if (!Player.IsAlive()) return 0;
        //     4人以上で　　　　　　　　　　タスク完了しているフラグがある　　　生存Countに入れる : 入れない
        return PlayerCatch.AllAlivePlayersCount > 3 && checktaskwinflag ? 1 : 0;
    }
    public override void OverrideProgressTextAsSeen(PlayerControl seer, ref bool enabled, ref string text)
    {
        if (seer == null) return;
        if (Is(seer) || seer.Is(CustomRoles.GM)) return;

        text = $"<#cccccc>(?/{MyTaskState.AllTasksCount})";
    }
    bool ISystemTypeUpdateHook.UpdateReactorSystem(ReactorSystemType reactorSystem, byte amount) => false;
    bool ISystemTypeUpdateHook.UpdateHeliSabotageSystem(HeliSabotageSystem heliSabotageSystem, byte amount) => false;
    bool ISystemTypeUpdateHook.UpdateLifeSuppSystem(LifeSuppSystemType lifeSuppSystem, byte amount) => false;
    bool ISystemTypeUpdateHook.UpdateHqHudSystem(HqHudSystemType hqHudSystemType, byte amount) => false;
    bool ISystemTypeUpdateHook.UpdateSwitchSystem(SwitchSystem switchSystem, byte amount) => false;
    bool ISystemTypeUpdateHook.UpdateHudOverrideSystem(HudOverrideSystemType hudOverrideSystem, byte amount) => false;
}