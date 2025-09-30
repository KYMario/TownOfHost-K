using System.Linq;
using AmongUs.GameOptions;
using Hazel;
using TownOfHost.Modules;
using TownOfHost.Modules.ChatManager;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using UnityEngine;

namespace TownOfHost.Roles.Neutral;

public sealed class Nue : RoleBase, ISelfVoter, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Nue),
            player => new Nue(player),
            CustomRoles.Nue,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Madmate,
            23500,
            NueSetUpOptionItem,
            "Nue",
            "#be7c35",
            (3, 1),
            true,
            tab: TabGroup.Combinations,
            assignInfo: new RoleAssignInfo(CustomRoles.Nue, CustomRoleTypes.Madmate)
            {
                AssignUnitRoles = [CustomRoles.Fool, CustomRoles.Nue]
            },
            combination: CombinationRoles.FoolandNue
        );
    public Nue(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.ForRecompute
    )
    {
        nuekillcool = OptionNueKillcool.GetFloat();
        canseetaskcount = OptionNueCanSeeImpostorTaskcount.GetInt();
        canusevotecount = OptionNueCanUseVotecount.GetInt();
        suicidetime = OptionSuicidetime.GetFloat();

        Guessed = false;
        GuessedFool = false;
        IsKilled = false;
        IsSeeImpostor = false;
    }
    static OptionItem OptionNueKillcool; static float nuekillcool;
    static OptionItem OptionNueCanSeeImpostorTaskcount; static int canseetaskcount;
    static OptionItem OptionNueCanUseVotecount; static int canusevotecount;
    static OptionItem OptionSuicidetime;
    float suicidetime;
    bool IsSeeImpostor;
    bool GuessedFool; bool Guessed; byte FoolId; bool IsKilled;
    enum OptionName
    {
        MadBetrayerImpostorRevealTaskcount,
        FoolAndNueCanUseVotecount,
        NueSuicidetimer
    }
    public static void NueSetUpOptionItem()
    {
        Fool.FoolSetupOptionItem(RoleInfo);
        OptionNueKillcool = FloatOptionItem.Create(RoleInfo, 20, GeneralOption.KillCooldown, OptionBaseCoolTime, 20, false).SetValueFormat(OptionFormat.Seconds).SetInfo(UtilsRoleText.GetRoleColorAndtext(CustomRoles.Nue));
        OptionNueCanSeeImpostorTaskcount = IntegerOptionItem.Create(RoleInfo, 21, OptionName.MadBetrayerImpostorRevealTaskcount, new(0, 99, 1), 3, false)
            .SetZeroNotation(OptionZeroNotation.Off).SetInfo(UtilsRoleText.GetRoleColorAndtext(CustomRoles.Nue));
        OptionNueCanUseVotecount = IntegerOptionItem.Create(RoleInfo, 22, OptionName.FoolAndNueCanUseVotecount, new(0, 99, 1), 3, false).SetInfo(UtilsRoleText.GetRoleColorAndtext(CustomRoles.Nue));
        OptionSuicidetime = FloatOptionItem.Create(RoleInfo, 23, OptionName.NueSuicidetimer, new(0.5f, 30, 0.5f), 5f, false).SetValueFormat(OptionFormat.Seconds).SetInfo(UtilsRoleText.GetRoleColorAndtext(CustomRoles.Nue));
    }
    public override void Add()
    {
        FoolId = PlayerCatch.AllPlayerControls.Where(pc => pc.GetCustomRole() is CustomRoles.Fool).FirstOrDefault()?.PlayerId ?? byte.MaxValue;
    }

    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (!SelfVoteManager.Canuseability()) return true;
        if (!GuessedFool && !Guessed && Is(voter) && MyTaskState.HasCompletedEnoughCountOfTasks(canusevotecount))
        {
            if (SelfVoteManager.CheckSelfVoteMode(Player, votedForId, out var status))
            {
                if (status is SelfVoteManager.VoteStatus.Self)
                    Utils.SendMessage(string.Format(GetString("SkillMode"), GetString("Mode.Guess"), GetString("Vote.Nue")) + GetString("VoteSkillMode"), Player.PlayerId);
                if (status is SelfVoteManager.VoteStatus.Skip)
                    Utils.SendMessage(GetString("VoteSkillFin"), Player.PlayerId);
                if (status is SelfVoteManager.VoteStatus.Vote)
                    TryGuessFool(votedForId);
                SelfVoteManager.SetMode(Player, status is SelfVoteManager.VoteStatus.Self);
                return false;
            }
        }
        return true;
    }
    void TryGuessFool(byte votedForId)
    {
        Guessed = true;

        // ゲッサーがいる場合はチャットメッセージの区別をつけなく
        if ((PlayerCatch.AllPlayerControls.Any(pc => pc.Is(CustomRoles.Guesser)) || CustomRolesHelper.CheckGuesser()) && !Options.ExHideChatCommand.GetBool())
            ChatManager.SendPreviousMessagesToAll();

        if (FoolId == votedForId)
        {
            GuessedFool = true;
            MeetingKill(votedForId);
            _ = new LateTask(() =>
            {
                MeetingVoteManager.Instance.ClearAndExile(votedForId, 253);
            }, 3f, "Nue MeetingEnd", true);
        }
        else//↑成功  ↓失敗
        {
            MeetingKill(Player.PlayerId);
        }
        SendRpc();
    }
    void MeetingKill(byte targetId)
    {
        var target = PlayerCatch.GetPlayerById(targetId);
        var state = PlayerState.GetByPlayerId(targetId);
        target.RpcExileV2();
        state.DeathReason = targetId == Player.PlayerId ? CustomDeathReason.Misfire : CustomDeathReason.Kill;
        state.SetDead();

        UtilsGameLog.AddGameLog($"Nue", $"{UtilsName.GetPlayerColor(target, true)}(<b>{UtilsRoleText.GetTrueRoleName(target.PlayerId, false)}</b>) [{Utils.GetVitalText(target.PlayerId, true)}]");
        UtilsGameLog.AddGameLogsub($"\n\t⇐ {UtilsName.GetPlayerColor(Player, true)}(<b>{UtilsRoleText.GetTrueRoleName(Player.PlayerId, false)}</b>)");

        if (Options.ExHideChatCommand.GetBool())
        {
            ChatManager.OnDisconnectOrDeadPlayer(target.PlayerId);
        }
        Logger.Info($"{Player.GetNameWithRole().RemoveHtmlTags()}が({target.GetNameWithRole().RemoveHtmlTags()}を推測 {(target.PlayerId == Player.PlayerId ? "失敗" : "成功")}", "Nue");
        Utils.SendMessage(UtilsName.GetPlayerColor(target, true) + GetString("Meetingkill"), title: GetString("MSKillTitle"));
        foreach (var go in PlayerCatch.AllPlayerControls.Where(pc => pc != null && !pc.IsAlive()))
        {
            Utils.SendMessage(string.Format(GetString("NunGuess" + (targetId == Player.PlayerId ? "Fall" : "")), UtilsName.GetPlayerColor(Player, true), UtilsName.GetPlayerColor(target, true)), go.PlayerId, GetString("RMSKillTitle"));
        }

        MeetingVoteManager.ResetVoteManager(target.PlayerId);
        Player.RpcMeetingKill(target);
    }
    public override RoleTypes? AfterMeetingRole => GuessedFool ? RoleTypes.Impostor : RoleTypes.Crewmate;

    float IKiller.CalculateKillCooldown() => nuekillcool;
    bool IKiller.CanUseSabotageButton() => false;
    bool IKiller.CanUseImpostorVentButton() => false;
    bool IKiller.CanUseKillButton() => GuessedFool;

    void IKiller.OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AppearanceTuple;

        if (IsKilled && info.IsCanKilling)
        {
            info.CanKill = false;
        }
        info.KillPower = 3;
    }

    void IKiller.OnMurderPlayerAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AppearanceTuple;

        if (Is(killer) && info.IsCanKilling)
        {
            IsKilled = true;
        }
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (AmongUsClient.Instance.AmHost is false || !IsKilled || !Player.IsAlive()) return;

        suicidetime -= Time.fixedDeltaTime;
        if (suicidetime <= 0)
        {
            suicidetime = 999;
            MyState.DeathReason = CustomDeathReason.Suicide;
            Player.SetRealKiller(Player);
            Player.RpcMurderPlayer(Player);
        }
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (Player.IsAlive() && IsKilled)
        {
            suicidetime = 999;
            MyState.DeathReason = CustomDeathReason.Suicide;
            Player.SetRealKiller(Player);
            Player.RpcMurderPlayer(Player);
        }
    }

    void SendRpc()
    {
        using var sender = CreateSender();
        sender.Writer.Write(Guessed);
        sender.Writer.Write(GuessedFool);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        Guessed = reader.ReadBoolean();
        GuessedFool = reader.ReadBoolean();
    }

    public override bool OnCompleteTask(uint taskid)
    {
        if (MyTaskState.HasCompletedEnoughCountOfTasks(canseetaskcount) && canseetaskcount > 0)
        {
            IsSeeImpostor = true;
            _ = new LateTask(() => UtilsNotifyRoles.NotifyRoles(ForceLoop: true), Main.LagTime, "SendNotifyRole", true);
        }
        return true;
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        var seenrole = seen.GetCustomRole();
        if (Is(seer) && IsSeeImpostor && (seenrole.IsImpostor() || seenrole is CustomRoles.Egoist or CustomRoles.WolfBoy))
        {
            return "<#ff1919>★</color>";
        }
        return "";
    }
}