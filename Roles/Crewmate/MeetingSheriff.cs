using AmongUs.GameOptions;
using UnityEngine;
using Hazel;
using System.Linq;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Impostor;
using TownOfHost.Roles.Neutral;
using TownOfHost.Modules.ChatManager;
using static TownOfHost.Modules.SelfVoteManager;
using TownOfHost.Modules;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Crewmate;

public sealed class MeetingSheriff : RoleBase, ISelfVoter
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(MeetingSheriff),
            player => new MeetingSheriff(player),
            CustomRoles.MeetingSheriff,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            9000,
            SetupOptionItem,
            "Ms",
            "#f8cd46",
            (2, 2),
            from: From.SuperNewRoles
            );
    public MeetingSheriff(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Max = OptionSheriffShotLimit.GetFloat();
        Usedcount = 0;
        MeetingUsedcount = 0;
        MeetingSheriffCanKillMadMate = OptionMeetingSheriffCanKillMadMate.GetBool();
        MeetingSheriffCanKillNeutrals = OptionMeetingSheriffCanKillNeutrals.GetBool();
        cantaskcount = Optioncantaskcount.GetFloat();
        OneMeetingMaximum = Option1MeetingMaximum.GetFloat();
    }

    private static OptionItem OptionSheriffShotLimit;
    private static OptionItem OptionMeetingSheriffCanKillMadMate;
    private static OptionItem OptionMeetingSheriffCanKillNeutrals;
    private static OptionItem OptionMeetingSheriffCanKillLovers;
    private static OptionItem Optioncantaskcount;
    private static OptionItem Option1MeetingMaximum;
    static float Max;
    static float cantaskcount;
    bool MeetingSheriffCanKillMadMate;
    bool MeetingSheriffCanKillNeutrals;
    int Usedcount;
    static float OneMeetingMaximum;
    int MeetingUsedcount;

    enum Option
    {
        SheriffShotLimit,
        cantaskcount,//効果を発揮タスク数
        MeetingSheriffCanKillMadMate,
        MeetingSheriffCanKillNeutrals,
        SheriffCanKillLovers
    }
    private static void SetupOptionItem()
    {
        OptionSheriffShotLimit = IntegerOptionItem.Create(RoleInfo, 10, Option.SheriffShotLimit, new(1, 15, 1), 1, false)
            .SetValueFormat(OptionFormat.Times);
        Optioncantaskcount = IntegerOptionItem.Create(RoleInfo, 11, Option.cantaskcount, new(0, 99, 1), 5, false);
        OptionMeetingSheriffCanKillMadMate = BooleanOptionItem.Create(RoleInfo, 12, Option.MeetingSheriffCanKillMadMate, true, false);
        OptionMeetingSheriffCanKillNeutrals = BooleanOptionItem.Create(RoleInfo, 13, Option.MeetingSheriffCanKillNeutrals, true, false);
        OptionMeetingSheriffCanKillLovers = BooleanOptionItem.Create(RoleInfo, 15, Option.SheriffCanKillLovers, true, false);
        Option1MeetingMaximum = IntegerOptionItem.Create(RoleInfo, 14, GeneralOption.MeetingMaxTime, new(0, 99, 1), 0, false)
            .SetValueFormat(OptionFormat.Times).SetZeroNotation(OptionZeroNotation.Infinity);
    }
    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(Usedcount);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        Usedcount = reader.ReadInt32();
    }
    public override void OnStartMeeting() => MeetingUsedcount = 0;
    public override string GetProgressText(bool comms = false, bool gamelog = false) => Utils.ColorString(MyTaskState.CompletedTasksCount < cantaskcount ? Color.gray : Max <= Usedcount ? Color.gray : Color.cyan, $"({Max - Usedcount})");
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (isForMeeting && Player.IsAlive() && seer.PlayerId == seen.PlayerId && Canuseability() && Max > Usedcount && MyTaskState.HasCompletedEnoughCountOfTasks(cantaskcount))
        {
            var mes = $"<color={RoleInfo.RoleColorCode}>{GetString("SelfVoteRoleInfoMeg")}</color>";
            return isForHud ? mes : $"<size=40%>{mes}</size>";
        }
        return "";
    }
    bool ISelfVoter.CanUseVoted() => Canuseability() && Max > Usedcount && MyTaskState.HasCompletedEnoughCountOfTasks(cantaskcount) && (MeetingUsedcount < OneMeetingMaximum || OneMeetingMaximum == 0);

    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (!Canuseability()) return true;
        if (Max > Usedcount && Is(voter) && MyTaskState.HasCompletedEnoughCountOfTasks(cantaskcount) && (MeetingUsedcount < OneMeetingMaximum || OneMeetingMaximum == 0))
        {
            if (CheckSelfVoteMode(Player, votedForId, out var status))
            {
                if (status is VoteStatus.Self)
                    Utils.SendMessage(string.Format(GetString("SkillMode"), GetString("Mode.MeetingSheriff"), GetString("Vote.MeetingSheriff")) + GetString("VoteSkillMode"), Player.PlayerId);
                if (status is VoteStatus.Skip)
                    Utils.SendMessage(GetString("VoteSkillFin"), Player.PlayerId);
                if (status is VoteStatus.Vote)
                    Sheriff(votedForId);
                SetMode(Player, status is VoteStatus.Self);
                return false;
            }
        }
        return true;
    }
    public void Sheriff(byte votedForId)
    {
        PlayerState state;
        var target = PlayerCatch.GetPlayerById(votedForId);
        if (!target.IsAlive()) return;
        if (!AmongUsClient.Instance.AmHost) return;
        var meetingHud = MeetingHud.Instance;
        var hudManager = DestroyableSingleton<HudManager>.Instance.KillOverlay;
        Usedcount++;
        MeetingUsedcount++;//1会議のカウント
        SendRPC();

        //ゲッサーがいるなら～
        if ((PlayerCatch.AllPlayerControls.Any(pc => pc.Is(CustomRoles.Guesser)) || CustomRolesHelper.CheckGuesser()) && !Options.ExHideChatCommand.GetBool())
            ChatManager.SendPreviousMessagesToAll();

        var AlienTairo = false;
        var targetroleclass = target.GetRoleClass();
        if ((targetroleclass as Alien)?.CheckSheriffKill(target) == true) AlienTairo = true;
        if ((targetroleclass as JackalAlien)?.CheckSheriffKill(target) == true) AlienTairo = true;
        if ((targetroleclass as AlienHijack)?.CheckSheriffKill(target) == true) AlienTairo = true;

        if ((CanBeKilledBy(target.GetCustomRole()) && !AlienTairo) || (target.IsLovers() && OptionMeetingSheriffCanKillLovers.GetBool()) || (target.Is(CustomRoles.Amanojaku) && OptionMeetingSheriffCanKillNeutrals.GetBool()))
        {
            state = PlayerState.GetByPlayerId(target.PlayerId);
            target.RpcExileV2();
            state.DeathReason = CustomDeathReason.Kill;
            state.SetDead();

            UtilsGameLog.AddGameLog($"MeetingSheriff", $"{UtilsName.GetPlayerColor(target, true)}(<b>{UtilsRoleText.GetTrueRoleName(target.PlayerId, false)}</b>) [{Utils.GetVitalText(target.PlayerId, true)}]");
            UtilsGameLog.AddGameLogsub($"\n\t⇐ {UtilsName.GetPlayerColor(Player, true)}(<b>{UtilsRoleText.GetTrueRoleName(Player.PlayerId, false)}</b>)");

            if (Options.ExHideChatCommand.GetBool())
            {
                ChatManager.OnDisconnectOrDeadPlayer(target.PlayerId);
            }
            Logger.Info($"{Player.GetNameWithRole().RemoveHtmlTags()}がシェリフ成功({target.GetNameWithRole().RemoveHtmlTags()}) 残り{Max - Usedcount}", "MeetingSheriff");
            Utils.SendMessage(UtilsName.GetPlayerColor(target, true) + GetString("Meetingkill"), title: GetString("MSKillTitle"));
            foreach (var go in PlayerCatch.AllPlayerControls.Where(pc => pc != null && !pc.IsAlive()))
            {
                Utils.SendMessage(string.Format(GetString("MMeetingKill"), UtilsName.GetPlayerColor(Player, true), UtilsName.GetPlayerColor(target, true)), go.PlayerId, GetString("RMSKillTitle"));
            }

            MeetingVoteManager.ResetVoteManager(target.PlayerId);
            Player.RpcMeetingKill(target);
            return;
        }
        Player.RpcExileV2();
        MyState.DeathReason = target.Is(CustomRoles.Tairou) && Tairou.TairoDeathReason ? CustomDeathReason.Counter :
                            target.Is(CustomRoles.Alien) && Alien.TairoDeathReason ? CustomDeathReason.Counter :
                            (target.Is(CustomRoles.JackalAlien) && JackalAlien.TairoDeathReason ? CustomDeathReason.Counter :
                            (target.Is(CustomRoles.AlienHijack) && Alien.TairoDeathReason ? CustomDeathReason.Counter : CustomDeathReason.Misfire));
        MyState.SetDead();

        UtilsGameLog.AddGameLog($"MeetingSheriff", $"{UtilsName.GetPlayerColor(Player, true)}(<b>{UtilsRoleText.GetTrueRoleName(Player.PlayerId, false)}</b>) [{Utils.GetVitalText(Player.PlayerId, true)}]");
        UtilsGameLog.AddGameLogsub($"\n\t┗ {GetString("Skillplayer")}{UtilsName.GetPlayerColor(target, true)}(<b>{UtilsRoleText.GetTrueRoleName(target.PlayerId, false)}</b>)");

        if (Options.ExHideChatCommand.GetBool())
        {
            ChatManager.OnDisconnectOrDeadPlayer(Player.PlayerId);
        }
        Logger.Info($"{Player.GetNameWithRole().RemoveHtmlTags()}がシェリフ失敗({target.GetNameWithRole().RemoveHtmlTags()}) 残り{Max - Usedcount}", "MeetingSheriff");
        Utils.SendMessage(UtilsName.GetPlayerColor(Player, true) + GetString("Meetingkill"), title: GetString("MSKillTitle"));
        foreach (var go in PlayerCatch.AllPlayerControls.Where(pc => pc != null && !pc.IsAlive()))
        {
            Utils.SendMessage(string.Format(GetString("MMeetingKillfall"), UtilsName.GetPlayerColor(Player, true), UtilsName.GetPlayerColor(target, true)), go.PlayerId, GetString("RMSKillTitle"));
        }

        MeetingVoteManager.ResetVoteManager(Player.PlayerId);
        Player.RpcMeetingKill(Player);
    }
    bool CanBeKilledBy(CustomRoles role)
    {
        if (role == CustomRoles.SKMadmate) return MeetingSheriffCanKillMadMate;
        if (role == CustomRoles.Jackaldoll) return MeetingSheriffCanKillNeutrals;

        return role.GetCustomRoleTypes() switch
        {
            CustomRoleTypes.Impostor => role is not CustomRoles.Tairou,
            CustomRoleTypes.Madmate => MeetingSheriffCanKillMadMate,
            CustomRoleTypes.Neutral => MeetingSheriffCanKillNeutrals,
            CustomRoleTypes.Crewmate => role is CustomRoles.WolfBoy,
            _ => false
        };
    }// ↓改良したの作っちゃった☆ 動くかはわかんない byけーわい
     //ｶｲﾘｮｳｼﾃﾓﾗｯﾀﾅﾗﾂｶﾜﾅｲﾜｹｶﾞﾅｲ!!(大狼の処理とマッドの処理が出来てたからニュートラルもできるはず!!)

}
//コード多分改良できるけど動いてるからヨシ。(´・ω・｀)