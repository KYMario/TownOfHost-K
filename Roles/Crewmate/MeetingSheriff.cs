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

namespace TownOfHost.Roles.Crewmate;
public sealed class MeetingSheriff : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(MeetingSheriff),
            player => new MeetingSheriff(player),
            CustomRoles.MeetingSheriff,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            17200,
            SetupOptionItem,
            "Ms",
            "#f8cd46",
            from: From.SuperNewRoles
            );
    public MeetingSheriff(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Max = OptionSheriffShotLimit.GetFloat();
        count = 0;
        mcount = 0;
        MeetingSheriffCanKillMadMate = OptionMeetingSheriffCanKillMadMate.GetBool();
        MeetingSheriffCanKillNeutrals = OptionMeetingSheriffCanKillNeutrals.GetBool();
        cantaskcount = Optioncantaskcount.GetFloat();
        onemeetingmaximum = Option1MeetingMaximum.GetFloat();
    }

    private static OptionItem OptionSheriffShotLimit;
    private static OptionItem OptionMeetingSheriffCanKillMadMate;
    private static OptionItem OptionMeetingSheriffCanKillNeutrals;
    private static OptionItem OptionMeetingSheriffCanKillLovers;
    private static OptionItem Optioncantaskcount;
    private static OptionItem Option1MeetingMaximum;
    public float Max;
    float cantaskcount;
    bool MeetingSheriffCanKillMadMate;
    bool MeetingSheriffCanKillNeutrals;
    int count;
    float onemeetingmaximum;
    float mcount;

    enum Option
    {
        SheriffShotLimit,
        cantaskcount,//効果を発揮タスク数
        MeetingSheriffCanKillMadMate,
        MeetingSheriffCanKillNeutrals,
        meetingmc,
        SheriffCanKillLovers
    }
    private static void SetupOptionItem()
    {
        OptionSheriffShotLimit = FloatOptionItem.Create(RoleInfo, 10, Option.SheriffShotLimit, new(1f, 15f, 1f), 1f, false)
            .SetValueFormat(OptionFormat.Times);
        Optioncantaskcount = FloatOptionItem.Create(RoleInfo, 11, Option.cantaskcount, new(0, 99, 1), 5, false);
        OptionMeetingSheriffCanKillMadMate = BooleanOptionItem.Create(RoleInfo, 12, Option.MeetingSheriffCanKillMadMate, true, false);
        OptionMeetingSheriffCanKillNeutrals = BooleanOptionItem.Create(RoleInfo, 13, Option.MeetingSheriffCanKillNeutrals, true, false);
        OptionMeetingSheriffCanKillLovers = BooleanOptionItem.Create(RoleInfo, 15, Option.SheriffCanKillLovers, true, false);
        Option1MeetingMaximum = FloatOptionItem.Create(RoleInfo, 14, Option.meetingmc, new(0f, 99f, 1f), 0f, false, infinity: true)
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Add()
        => AddS(Player);
    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(count);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        count = reader.ReadInt32();
    }
    public override void OnStartMeeting() => mcount = 0;
    public override string GetProgressText(bool comms = false, bool gamelog = false) => Utils.ColorString(MyTaskState.CompletedTasksCount < cantaskcount ? Color.gray : Max <= count ? Color.gray : Color.cyan, $"({Max - count})");
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (isForMeeting && Player.IsAlive() && seer.PlayerId == seen.PlayerId && Canuseability() && Max > count && MyTaskState.HasCompletedEnoughCountOfTasks(cantaskcount))
        {
            var mes = $"<color={RoleInfo.RoleColorCode}>{GetString("SelfVoteRoleInfoMeg")}</color>";
            return isForHud ? mes : $"<size=40%>{mes}</size>";
        }
        return "";
    }
    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (!Canuseability()) return true;
        if (Max > count && Is(voter) && MyTaskState.HasCompletedEnoughCountOfTasks(cantaskcount) && (mcount < onemeetingmaximum || onemeetingmaximum == 0))
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
        count++;
        mcount++;//1会議のカウント
        SendRPC();

        //ゲッサーがいるなら～
        if ((PlayerCatch.AllPlayerControls.Any(pc => pc.Is(CustomRoles.Guesser)) || CustomRolesHelper.CheckGuesser()) && !Options.ExHideChatCommand.GetBool())
            ChatManager.SendPreviousMessagesToAll();

        var AlienTairo = false;
        var targetroleclass = target.GetRoleClass();
        if ((targetroleclass as Alien)?.CheckSheriffKill(target) == true) AlienTairo = true;
        if ((targetroleclass as JackalAlien)?.CheckSheriffKill(target) == true) AlienTairo = true;
        if ((targetroleclass as AlienHijack)?.CheckSheriffKill(target) == true) AlienTairo = true;

        if ((CanBeKilledBy(target.GetCustomRole()) && !AlienTairo) || (target.IsRiaju() && OptionMeetingSheriffCanKillLovers.GetBool()) || (target.Is(CustomRoles.Amanojaku) && OptionMeetingSheriffCanKillNeutrals.GetBool()))
        {
            state = PlayerState.GetByPlayerId(target.PlayerId);
            target.RpcExileV2();
            state.DeathReason = CustomDeathReason.Kill;
            state.SetDead();

            UtilsGameLog.AddGameLog($"MeetingSheriff", $"{Utils.GetPlayerColor(target, true)}(<b>{UtilsRoleText.GetTrueRoleName(target.PlayerId, false)}</b>) [{Utils.GetVitalText(target.PlayerId, true)}]");
            UtilsGameLog.gamelog += $"\n\t\t⇐ {Utils.GetPlayerColor(Player, true)}(<b>{UtilsRoleText.GetTrueRoleName(Player.PlayerId, false)}</b>)";

            if (Options.ExHideChatCommand.GetBool())
            {
                MeetingHudPatch.StartPatch.Serialize = true;
                foreach (var pc in PlayerCatch.AllAlivePlayerControls)
                {
                    if (pc == target) continue;
                    pc.Data.IsDead = false;
                }
                RPC.RpcSyncAllNetworkedPlayer(target.GetClientId());
                MeetingHudPatch.StartPatch.Serialize = false;
            }
            Logger.Info($"{Player.GetNameWithRole().RemoveHtmlTags()}がシェリフ成功({target.GetNameWithRole().RemoveHtmlTags()}) 残り{Max - count}", "MeetingSheriff");
            Utils.SendMessage(Utils.GetPlayerColor(target, true) + GetString("Meetingkill"), title: GetString("MSKillTitle"));
            foreach (var go in PlayerCatch.AllPlayerControls.Where(pc => pc != null && !pc.IsAlive()))
            {
                Utils.SendMessage(string.Format(GetString("MMeetingKill"), Utils.GetPlayerColor(Player, true), Utils.GetPlayerColor(target, true)), go.PlayerId, GetString("RMSKillTitle"));
            }

            MeetingVoteManager.ResetVoteManager(target.PlayerId);
            return;
        }
        Player.RpcExileV2();
        MyState.DeathReason = target.Is(CustomRoles.Tairou) && Tairou.TairoDeathReason ? CustomDeathReason.Revenge1 :
                            target.Is(CustomRoles.Alien) && Alien.TairoDeathReason ? CustomDeathReason.Revenge1 :
                            (target.Is(CustomRoles.JackalAlien) && JackalAlien.TairoDeathReason ? CustomDeathReason.Revenge1 :
                            (target.Is(CustomRoles.AlienHijack) && Alien.TairoDeathReason ? CustomDeathReason.Revenge1 : CustomDeathReason.Misfire));
        MyState.SetDead();

        UtilsGameLog.AddGameLog($"MeetingSheriff", $"{Utils.GetPlayerColor(Player, true)}(<b>{UtilsRoleText.GetTrueRoleName(Player.PlayerId, false)}</b>) [{Utils.GetVitalText(Player.PlayerId, true)}]");
        UtilsGameLog.gamelog += $"\n\t\t┗ {GetString("Skillplayer")}{Utils.GetPlayerColor(target, true)}(<b>{UtilsRoleText.GetTrueRoleName(target.PlayerId, false)}</b>)";

        if (Options.ExHideChatCommand.GetBool())
        {
            MeetingHudPatch.StartPatch.Serialize = true;
            foreach (var pc in PlayerCatch.AllAlivePlayerControls)
            {
                if (pc == Player) continue;
                pc.Data.IsDead = false;
            }
            RPC.RpcSyncAllNetworkedPlayer(Player.GetClientId());
            MeetingHudPatch.StartPatch.Serialize = false;
        }
        Logger.Info($"{Player.GetNameWithRole().RemoveHtmlTags()}がシェリフ失敗({target.GetNameWithRole().RemoveHtmlTags()}) 残り{Max - count}", "MeetingSheriff");
        Utils.SendMessage(Utils.GetPlayerColor(Player, true) + GetString("Meetingkill"), title: GetString("MSKillTitle"));
        foreach (var go in PlayerCatch.AllPlayerControls.Where(pc => pc != null && !pc.IsAlive()))
        {
            Utils.SendMessage(string.Format(GetString("MMeetingKillfall"), Utils.GetPlayerColor(Player, true), Utils.GetPlayerColor(target, true)), go.PlayerId, GetString("RMSKillTitle"));
        }

        MeetingVoteManager.ResetVoteManager(Player.PlayerId);
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