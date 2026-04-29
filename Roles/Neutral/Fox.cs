using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Hazel;
using TownOfHost.Modules;
using TownOfHost.Modules.ChatManager;
using TownOfHost.Patches;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using UnityEngine;

namespace TownOfHost.Roles.Neutral;

public sealed class Fox : RoleBase, ISystemTypeUpdateHook, IRoomTasker
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
            }
        );
    public Fox(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.ForRecompute
    )
    {
        Maxmeter = OptionMeterCount.GetFloat();
        MeterDistance = OptionDistance.GetFloat();
        OnlySeekiller = OptionOnlySeeKiller.GetBool();
        IsActiveNotice = OptionNoticeRoomTask.GetBool();
        GiveGuardTaskCount = OptionGiveGuardTaskCount.GetInt();
        GiveGuardMax = OptionGiveGuardMax.GetInt();
        DieAliveCount = OptionDieAliveCount.GetInt();
        CrewTaskFinishFlug = (FoxCrewTaskFin)OptionCrewTaskFinish.GetValue();
        IsTellDie = OptionTellDie.GetBool();
        Engventcool = OptionEngVentCoolDown.GetFloat();
        Engventinmax = OptionEngVentInmaxtime.GetFloat();
        CanSeeImpostor = OptionCanSeeImpostor.GetBool();
        CanSeeNeutralKiller = OptionCanSeeNeutralKiller.GetBool();
        CanSeeOther = OptionCanSeeOther.GetBool();

        CompleteRoomTask = false;
        Taskcount = 0;
        Guard = 0;
        NowMeter = Maxmeter;
        IsShowMyRole = false;
        isnotice = false;
        meatermark = "";
        MyTaskState.NeedTaskCount = GiveGuardTaskCount;
    }
    bool CompleteRoomTask;

    float NowMeter; float timer; bool IsShowMyRole; string meatermark;
    int Taskcount; int Guard;
    static void SetupOptionItem()
    {
        string[] values = EnumHelper.GetAllNames<FoxCrewTaskFin>();
        SoloWinOption.Create(RoleInfo, 10, defo: 15);
        OptionCrewTaskFinish = StringOptionItem.Create(RoleInfo, 11, OptionName.FoxCrewTaskFin, values, 0, false);
        OptionDieAliveCount = IntegerOptionItem.Create(RoleInfo, 12, OptionName.FoxDieAliveCount, new(0, 15, 1), 3, false);
        ObjectOptionitem.Create(RoleInfo, 13, "FoxAbilitySetting", true, null).SetOptionName(() => "Ability Setting");
        OptionEngVentCoolDown = FloatOptionItem.Create(RoleInfo, 14, StringNames.EngineerCooldown, OptionBaseCoolTime, 10, false).SetValueFormat(OptionFormat.Seconds);
        OptionEngVentInmaxtime = FloatOptionItem.Create(RoleInfo, 15, StringNames.EngineerInVentCooldown, new(0.5f, 30, 0.5f), 3, false).SetValueFormat(OptionFormat.Seconds);
        OptionTellDie = BooleanOptionItem.Create(RoleInfo, 16, OptionName.FoxTellDie, false, false);
        OptionNoticeRoomTask = BooleanOptionItem.Create(RoleInfo, 24, OptionName.FoxNoticeRoomTask, true, false);
        ObjectOptionitem.Create(RoleInfo, 17, "FoxMeterSetting", true, null).SetOptionName(() => "Meter Setting");
        OptionMeterCount = FloatOptionItem.Create(RoleInfo, 18, OptionName.FoxMeterCount, new(5, 1200, 5), 60, false).SetValueFormat(OptionFormat.Seconds);
        OptionDistance = FloatOptionItem.Create(RoleInfo, 19, OptionName.FoxMeterDistance, new(0.5f, 5f, 0.25f), 1.25f, false).SetValueFormat(OptionFormat.Multiplier);
        OptionOnlySeeKiller = BooleanOptionItem.Create(RoleInfo, 20, OptionName.FoxShowRoleOnlyKiller, false, false);
        OptionCanSeeImpostor = BooleanOptionItem.Create(RoleInfo, 25, OptionName.FoxCanSeeImposotr, true, false, OptionOnlySeeKiller);
        OptionCanSeeNeutralKiller = BooleanOptionItem.Create(RoleInfo, 26, OptionName.FoxCanSeeNeutralKiller, true, false, OptionOnlySeeKiller);
        OptionCanSeeOther = BooleanOptionItem.Create(RoleInfo, 27, OptionName.FoxCanSeeOtherKiller, true, false, OptionOnlySeeKiller);
        ObjectOptionitem.Create(RoleInfo, 21, "FoxGuardSetting", true, null).SetOptionName(() => "Guard Setting");
        OptionGiveGuardTaskCount = IntegerOptionItem.Create(RoleInfo, 22, OptionName.FoxGiveGuardTaskcount, new(1, 99, 1), 3, false);
        OptionGiveGuardMax = IntegerOptionItem.Create(RoleInfo, 23, OptionName.FoxGiveGuardMax, new(0, 99, 1), 2, false);
        OverrideTasksData.Create(RoleInfo, 30);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = Engventcool;
        AURoleOptions.EngineerInVentMaxTime = Engventinmax;
    }
    #region Meter / Die
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (player.IsAlive() is false) return;

        timer += Time.fixedDeltaTime;
        if (5 < timer)
        {
            var mypos = player.GetTruePosition();
            foreach (var otherpc in PlayerCatch.AllAlivePlayerControls)
            {
                if (otherpc.PlayerId == player.PlayerId) continue;

                if (Vector2.Distance(mypos, otherpc.GetTruePosition()) < MeterDistance)
                {
                    NowMeter -= Time.fixedDeltaTime;
                    break;
                }
            }
            if (GetNow() != meatermark)
            {
                meatermark = GetNow();
                if (AmongUsClient.Instance.AmHost)
                    UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);
            }
            if (IsShowMyRole is false && NowMeter < 0)
            {
                IsShowMyRole = true;
                isnotice = true;
                SendRPC_ShowMyRole();

                foreach (var seer in PlayerCatch.AllAlivePlayerControls)
                {
                    if (CanSeeFox(seer))
                    {
                        NameColorManager.Add(seer.PlayerId, Player.PlayerId, RoleInfo.RoleColorCode);
                    }
                }
                if (AmongUsClient.Instance.AmHost)
                    UtilsNotifyRoles.NotifyRoles();
            }
        }

        //非ホスト導入者はタイマー処理のみ行う。
        if (AmongUsClient.Instance.AmHost is false)
        {
            if (PlayerCatch.AllAlivePlayersCount <= DieAliveCount)
            {
                MyState.DeathReason = CustomDeathReason.Spell;
                Player.RpcExileV3();
                UtilsGameLog.AddGameLog($"Fox", $"{UtilsName.GetPlayerColor(Player, true)}(<b>{UtilsRoleText.GetTrueRoleName(Player.PlayerId, false)}</b>) [{Utils.GetVitalText(Player.PlayerId, true)}]");
            }
        }
    }
    public override void AfterMeetingTasks()
    {
        timer = 0;
        Logger.Info($"現在のメーター{NowMeter}", "Fox");
    }
    public override void OverrideDisplayRoleNameAsSeen(PlayerControl seer, ref bool enabled, ref Color roleColor, ref string roleText, ref bool addon)
    {
        if (IsShowMyRole is false) return;
        if (seer == Player) return;

        if (CanSeeFox(seer))
        {//生きてて キラーであるかキラー以外も見えるか
            enabled = true;
            addon = false;
        }
    }
    bool CanSeeFox(PlayerControl seer)
    {
        if (seer.IsAlive())
        {
            // キル役のみ制限無し
            if (OnlySeekiller is false) return true;

            if (seer.Is(CustomRoleTypes.Impostor)) return CanSeeImpostor;
            if (seer.IsNeutralKiller()) return CanSeeNeutralKiller;

            /* インポスターでも第三キラーでもない場合 */
            return CanSeeOther;
        }
        return false;
    }
    void SendRPC_ShowMyRole()
    {
        using var sender = CreateSender();
        sender.Writer.WritePacked((int)RPC_Types.IsShowMyRole);
    }
    #endregion
    #region RoomTask
    int? IRoomTasker.GetMaxTaskCount() => null;
    bool IRoomTasker.IsAssignRoomTask() => IsActiveNotice && !CompleteRoomTask && Player.IsAlive();
    void IRoomTasker.OnComplete(int completeroom) => SendRPC_CompleteRoom();
    void IRoomTasker.ChangeRoom(PlainShipRoom TaskRoom) => SendRPC_ChengeRoom(TaskRoom);
    public void SendRPC_CompleteRoom()
    {
        CompleteRoomTask = true;
        using var sender = CreateSender();
        sender.Writer.WritePacked((int)RPC_Types.CompleteRoom);
    }
    public void SendRPC_ChengeRoom(PlainShipRoom TaskPSR)
    {
        using var sender = CreateSender();
        sender.Writer.WritePacked((int)RPC_Types.ChengeRoom);
        sender.Writer.Write((byte)TaskPSR.RoomId);
    }
    public override string MeetingAddMessage()
    {
        if (SelfVoteManager.Canuseability() is false) return "";
        var oldcomptask = CompleteRoomTask;
        CompleteRoomTask = false;
        if (!Player.IsAlive() || oldcomptask) return "";

        isnotice = true;
        var chance = IRandom.Instance.Next(100);
        if (chance > 95) return $"<color=#d288ee>{GetString("FoxAliveMeg1")}</color>";
        if (chance > 90) return $"<color=#d288ee>{GetString("FoxAliveMeg2")}</color>";
        if (chance > 85) return $"<color=#d288ee>{GetString("FoxAliveMeg3")}</color>";
        return $"<color=#d288ee>{GetString("FoxAliveMeg")}</color>";
    }
    #endregion
    #region Guard
    public override bool OnCompleteTask(uint taskid)
    {
        //もう上限に達しているなら処理終わり
        if (GiveGuardMax <= Guard) return true;
        //タスクカウントを増やす
        Taskcount++;
        //ガードを追加するタスク数に達したら
        if (GiveGuardTaskCount <= Taskcount)
        {
            Taskcount = 0;
            Guard++;
            Logger.Info($"ガード追加:{Guard}", "Fox");
            MyTaskState.NeedTaskCount += GiveGuardTaskCount;
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
            return true;
        }
        return true;
    }
    #endregion
    #region Sabotage
    bool ISystemTypeUpdateHook.UpdateReactorSystem(ReactorSystemType reactorSystem, byte amount) => false;
    bool ISystemTypeUpdateHook.UpdateHeliSabotageSystem(HeliSabotageSystem heliSabotageSystem, byte amount) => false;
    bool ISystemTypeUpdateHook.UpdateLifeSuppSystem(LifeSuppSystemType lifeSuppSystem, byte amount) => false;
    bool ISystemTypeUpdateHook.UpdateHqHudSystem(HqHudSystemType hqHudSystemType, byte amount) => false;
    bool ISystemTypeUpdateHook.UpdateSwitchSystem(SwitchSystem switchSystem, byte amount) => false;
    bool ISystemTypeUpdateHook.UpdateHudOverrideSystem(HudOverrideSystemType hudOverrideSystem, byte amount) => false;
    #endregion
    #region win
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
        if (Player.IsAlive() is false) return false;

        if (reason is GameOverReason.CrewmatesByTask && CustomWinnerHolder.WinnerTeam is CustomWinner.Crewmate)
        {
            switch (CrewTaskFinishFlug)
            {
                case FoxCrewTaskFin.FoxCrewTaskFin_MyWin:
                    if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Fox, Player.PlayerId))
                    {
                        CustomWinnerHolder.NeutralWinnerIds.Add(Player.PlayerId);
                        reason = GameOverReason.ImpostorsByKill;
                        Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[0]);
                        if (isnotice is false && 5 <= UtilsGameLog.day && IsActiveNotice) Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[2]);
                        return true;
                    }
                    break;
                case FoxCrewTaskFin.FoxCrewTaskFin_Addwin:
                    CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.Fox);
                    CustomWinnerHolder.WinnerIds.Add(Player.PlayerId);
                    return false;
            }
        }
        else
        {
            if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Fox, Player.PlayerId))
            {
                CustomWinnerHolder.NeutralWinnerIds.Add(Player.PlayerId);
                reason = GameOverReason.ImpostorsByKill;
                Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[0]);
                if (isnotice is false && 5 <= UtilsGameLog.day && IsActiveNotice) Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[2]);
                return true;
            }
        }
        return false;
    }
    public static bool BlockTaskWin()
    {
        if (GameModeManager.IsStandardClass() is false) return false;
        foreach (var pc in PlayerCatch.AllPlayerControls.Where(pc => pc.Is(CustomRoles.Fox)))
        {
            if (pc.GetRoleClass() is Fox fox)
            {
                if (pc.IsAlive() && CrewTaskFinishFlug is FoxCrewTaskFin.FoxCrewTaskFin_NoGameEnd)
                {
                    return true;
                }
            }
        }
        return false;
    }
    #endregion
    #region name
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (seen != seer || !seer.IsAlive()) return "";

        return $"<size=40%><u>{GetNow()}</u></size>";
    }
    public override void OverrideProgressTextAsSeen(PlayerControl seer, ref bool enabled, ref string text)
    {
        if (seer == null) return;
        if (Is(seer) || seer.Is(CustomRoles.GM) || !seer.IsAlive()) return;

        text = $"<#cccccc>(?/{MyTaskState.AllTasksCount})";
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (isForMeeting || seer != seen) return "";
        return (seer.GetRoleClass() as IRoomTasker)?.GetLowerText(seer, RoleInfo.RoleColorCode) ?? "";
    }
    string GetNow()//バッテリー量の表示
    {
        var battery = (NowMeter / Maxmeter) * 100;
        if (battery <= 0) return "　";
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
    #endregion
    #region RPC
    public override void ReceiveRPC(MessageReader reader)
    {
        var iroomtasker = Player.GetRoleClass() is IRoomTasker roomTasker ? roomTasker : null;
        switch ((RPC_Types)reader.ReadPackedInt32())
        {
            case RPC_Types.ChengeRoom:
                iroomtasker?.ReceiveRoom(Player.PlayerId, reader);
                break;
            case RPC_Types.CompleteRoom:
                var a = MessageReader.Get(reader);
                iroomtasker?.ReceiveCompleteRoom(Player.PlayerId, reader);
                CompleteRoomTask = true;
                break;
            case RPC_Types.IsShowMyRole:
                IsShowMyRole = true;
                break;
        }
    }

    enum RPC_Types
    {
        ChengeRoom,
        CompleteRoom,
        IsShowMyRole
    }
    #endregion
    public override CustomRoles TellResults(PlayerControl player)
    {
        //ぽんこつ占い師のぽんこつ占い師で死ぬのはかわいそう('ω')
        if (AmongUsClient.Instance.AmHost && IsTellDie && player.IsAlive() && player != null)
        {
            Player.RpcExileV3();
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
            Player.RpcMeetingKill(Player);
            Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[1]);
        }
        return CustomRoles.NotAssigned;
    }
    /* 自身が妖狐であると暴露されるまでのカウント */
    static OptionItem OptionMeterCount; static float Maxmeter;
    static OptionItem OptionDistance; static float MeterDistance;
    static OptionItem OptionOnlySeeKiller; static bool OnlySeekiller;
    static OptionItem OptionCanSeeImpostor; static bool CanSeeImpostor;
    static OptionItem OptionCanSeeNeutralKiller; static bool CanSeeNeutralKiller;
    static OptionItem OptionCanSeeOther; static bool CanSeeOther;
    /* 毎ターン指定した部屋に行かないと通知される */
    static OptionItem OptionNoticeRoomTask; static bool IsActiveNotice;
    /* キルガード */
    static OptionItem OptionGiveGuardTaskCount; static int GiveGuardTaskCount;
    static OptionItem OptionGiveGuardMax; static int GiveGuardMax;
    /* 設定人数以下になったら成仏 */
    static OptionItem OptionDieAliveCount; static int DieAliveCount;
    /* タスク勝利時の挙動 */
    static OptionItem OptionCrewTaskFinish; static FoxCrewTaskFin CrewTaskFinishFlug;
    enum FoxCrewTaskFin { FoxCrewTaskFin_MyWin, FoxCrewTaskFin_Lose, FoxCrewTaskFin_NoGameEnd, FoxCrewTaskFin_Addwin }
    /* その他*/
    static OptionItem OptionTellDie; static bool IsTellDie;
    static OptionItem OptionEngVentCoolDown; static float Engventcool;
    static OptionItem OptionEngVentInmaxtime; static float Engventinmax;
    enum OptionName
    {
        FoxMeterCount, FoxMeterDistance, FoxShowRoleOnlyKiller,
        FoxCanSeeImposotr, FoxCanSeeNeutralKiller, FoxCanSeeOtherKiller,
        FoxNoticeRoomTask,
        FoxDieAliveCount,
        FoxCrewTaskFin,
        FoxGiveGuardTaskcount, FoxGiveGuardMax,
        FoxTellDie
    }
    bool isnotice;
    public static Dictionary<int, Achievement> achievements = new();
    [Attributes.PluginModuleInitializer]
    public static void Load()
    {
        var n1 = new Achievement(RoleInfo, 0, 1, 0, 0);
        var n2 = new Achievement(RoleInfo, 1, 1, 0, 0);
        var sp1 = new Achievement(RoleInfo, 2, 1, 0, 2, true);
        achievements.Add(0, n1);
        achievements.Add(1, n2);
        achievements.Add(2, sp1);
    }
}