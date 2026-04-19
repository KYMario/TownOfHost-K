using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Hazel;
using UnityEngine;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Attributes;
using static TownOfHost.PlayerCatch;

namespace TownOfHost.Roles.Neutral;

public sealed class Cupid : RoleBase, IKiller, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Cupid),
            player => new Cupid(player),
            CustomRoles.Cupid,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            30300,
            SetupOptionItem,
            "Cu",
            "#f09199",
            (5, 0),
            true,
            from: From.SuperNewRoles,
            introSound: () => GetIntroSound(RoleTypes.Scientist),
            assignInfo: new RoleAssignInfo(CustomRoles.Cupid, CustomRoleTypes.Neutral)
            {
                AssignCountRule = new(1, 1, 1)
            }
        );

    public Cupid(PlayerControl player)
    : base(RoleInfo, player, () => HasTask.False)
    {
        KillCooldown = OptKillCooldown.GetFloat();
        LoverChenge = ChangeRoles[OptionLoverChenge.GetValue()];
        IsKnowRole = CuLoversKnowRole.GetBool();

        target1 = byte.MaxValue;
        hasDesignated = false;
        Breakup = false;
        IsAmnesia = false;
    }

    static OptionItem OptKillCooldown;
    static float KillCooldown;
    static OptionItem OptionLoverChenge;
    public static CustomRoles LoverChenge;
    public static OptionItem CupidLoverAddwin;
    public static OptionItem CuLoversSolowin3players;
    static OptionItem CuLoversKnowRole;
    public static bool IsKnowRole;

    byte target1;
    bool hasDesignated;
    bool Breakup;
    bool IsAmnesia;

    public static List<PlayerControl> CupidLoversPlayers = new();
    public static bool IsCupidLoversDead = false;

    public static readonly CustomRoles[] ChangeRoles =
    {
        CustomRoles.Crewmate, CustomRoles.Jester, CustomRoles.Opportunist, CustomRoles.Madmate, CustomRoles.Monochromer
    };

    enum Option
    {
        CupidKillCooldown,
        CupidFallChenge,
        LoversRoleAddwin,
        LoverSoloWin3players,
    }

    [GameModuleInitializer]
    public static void Mareset()
    {
        CupidLoversPlayers.Clear();
        Lovers.CuCupidLoversPlayers.Clear();
        IsCupidLoversDead = false;
        Lovers.isCupidLoversDead = false;
    }

    private static void SetupOptionItem()
    {
        OptKillCooldown = FloatOptionItem.Create(RoleInfo, 10, Option.CupidKillCooldown,
            new(0f, 60f, 0.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        SoloWinOption.Create(RoleInfo, 9, CustomRoles.CupidLovers, () => !CupidLoverAddwin.GetBool(), defo: 13);
        var cRolesString = ChangeRoles.Select(x => x.ToString()).ToArray();
        OptionLoverChenge = StringOptionItem.Create(RoleInfo, 11, Option.CupidFallChenge, cRolesString, 4, false);
        CuLoversKnowRole = BooleanOptionItem.Create(RoleInfo, 14, "LoversRole", false, false);
        CupidLoverAddwin = BooleanOptionItem.Create(RoleInfo, 12, Option.LoversRoleAddwin, false, false);
        CuLoversSolowin3players = BooleanOptionItem.Create(RoleInfo, 13, Option.LoverSoloWin3players, false, false);
    }

    public override void Add()
    {
        CupidLoversPlayers.Clear();
        Lovers.CuCupidLoversPlayers.Clear();
        IsCupidLoversDead = false;
        Lovers.isCupidLoversDead = false;
    }

    public float CalculateKillCooldown() => hasDesignated ? 0f : KillCooldown;
    public bool CanUseKillButton() => Player.IsAlive() && !hasDesignated;
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => false;

    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(false);
    }

    // ★ キューピッド視点でラバーズに♥と役職名を名前の右に表示（名前は消えない）
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (!Is(seer)) return "";
        if (seer.PlayerId == seen.PlayerId) return "";
        if (!Player.IsAlive()) return "";
        // ★ seenがCupidLoversなら♥表示
        if (!CupidLoversPlayers.Any(p => p.PlayerId == seen.PlayerId)) return "";
        return $"<color={RoleInfo.RoleColorCode}>♥</color>";
    }

    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        info.DoKill = false;

        if (hasDesignated) return;
        if (target.PlayerId == killer.PlayerId) return;

        if (target.IsLovers() || target.Is(CustomRoles.OneLove)
            || target.Is(CustomRoles.Vega) || target.Is(CustomRoles.Altair)
            || target.Is(CustomRoles.Madonna))
        {
            hasDesignated = true;
            SendRPC();
            Utils.SendMessage(
                string.Format(GetString("Skill.CupidMynotcollect"),
                    UtilsName.GetPlayerColor(target, true),
                    GetString($"{LoverChenge}")) + GetString("VoteSkillFin"),
                killer.PlayerId);
            UtilsGameLog.AddGameLog("Cupid",
                string.Format(GetString("Log.CupidFa"),
                    UtilsName.GetPlayerColor(killer, true),
                    UtilsName.GetPlayerColor(target, true)));
            // ★ 即座に役職変化（会議後ではなくキル時点で変化）
            if (!Utils.RoleSendList.Contains(killer.PlayerId)) Utils.RoleSendList.Add(killer.PlayerId);
            killer.RpcSetCustomRole(LoverChenge, true, log: true);
            UtilsNotifyRoles.NotifyRoles();
            return;
        }

        if (target1 == byte.MaxValue)
        {
            target1 = target.PlayerId;
            killer.ResetKillCooldown();
            killer.SetKillCooldown();
            SendRPC();
            Utils.SendMessage(
                string.Format(GetString("CupidTarget1Set"),
                    UtilsName.GetPlayerColor(target, true)),
                killer.PlayerId);
            UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player);
            return;
        }

        if (target.PlayerId == target1) return;

        hasDesignated = true;

        var t1 = GetPlayerById(target1);
        var t2 = target;

        if (t1 != null && t2 != null)
        {
            CupidLoversPlayers.Add(t1);
            CupidLoversPlayers.Add(t2);
            Lovers.CuCupidLoversPlayers.Add(t1);
            Lovers.CuCupidLoversPlayers.Add(t2);
            Lovers.HaveLoverDontTaskPlayers.Add(t1.PlayerId);
            Lovers.HaveLoverDontTaskPlayers.Add(t2.PlayerId);

            t1.RpcSetCustomRole(CustomRoles.CupidLovers);
            t2.RpcSetCustomRole(CustomRoles.CupidLovers);


            RPC.SyncCupidLoversPlayers();

            Utils.SendMessage(
                string.Format(GetString("Skill.CupidMyCollect"),
                    UtilsName.GetPlayerColor(t1, true) + "と" + UtilsName.GetPlayerColor(t2, true))
                + GetString("VoteSkillFin"),
                killer.PlayerId);
            Utils.SendMessage(
                string.Format(GetString("Skill.CupidCollect"),
                    UtilsName.GetPlayerColor(killer, true)),
                t1.PlayerId);
            Utils.SendMessage(
                string.Format(GetString("Skill.CupidCollect"),
                    UtilsName.GetPlayerColor(killer, true)),
                t2.PlayerId);

            UtilsGameLog.AddGameLog("Cupid",
                string.Format(GetString("Log.CupidCo"),
                    UtilsName.GetPlayerColor(killer, true),
                    UtilsName.GetPlayerColor(t1, true) + "&" + UtilsName.GetPlayerColor(t2, true)));

            t1.RpcProtectedMurderPlayer();
            t2.RpcProtectedMurderPlayer();
        }

        SendRPC();
        UtilsNotifyRoles.NotifyRoles();
    }

    public bool CheckWin(ref CustomRoles winnerRole)
    {
        if (CupidLoversPlayers.Count == 0) return false;
        return CustomWinnerHolder.WinnerTeam == (CustomWinner)CustomRoles.CupidLovers
            || CustomWinnerHolder.AdditionalWinnerRoles.Contains(CustomRoles.CupidLovers);
    }

    public override void AfterMeetingTasks()
    {
        if (Player.Is(CustomRoles.Amnesia) || IsAmnesia)
        {
            IsAmnesia = Player.Is(CustomRoles.Amnesia);
            return;
        }
        if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;

        if (Player.Is(CustomRoles.CupidLovers))
        {
            Breakup = true;
        }
        else if (Player.IsAlive() && !Player.Is(CustomRoles.CupidLovers) && Breakup)
        {
            Utils.SendMessage(
                string.Format(GetString("Skill.MadoonnaHAMETU"), GetString($"{LoverChenge}")),
                Player.PlayerId);
            Breakup = false;
            if (!Utils.RoleSendList.Contains(Player.PlayerId)) Utils.RoleSendList.Add(Player.PlayerId);
            Player.RpcSetCustomRole(LoverChenge, true, log: true);
        }

    }

    public static void CupidLoversSuicide(byte deathId = 0x7f, bool isExiled = false)
    {
        if (IsCupidLoversDead) return;
        if (CupidLoversPlayers.Count == 0) return;

        foreach (var loversPlayer in CupidLoversPlayers)
        {
            if (!loversPlayer.Data.IsDead && loversPlayer.PlayerId != deathId) continue;

            isExiled |= ExtendedPlayerControl.GetDeadBodys().Contains(loversPlayer.Data) is false;
            IsCupidLoversDead = true;
            Lovers.isCupidLoversDead = true;

            foreach (var partner in CupidLoversPlayers)
            {
                if (partner.PlayerId == loversPlayer.PlayerId) continue;
                if (partner.PlayerId != deathId && !partner.Data.IsDead)
                {
                    PlayerState.GetByPlayerId(partner.PlayerId).DeathReason =
                        CustomDeathReason.FollowingSuicide;
                    if (isExiled)
                    {
                        MeetingHudPatch.TryAddAfterMeetingDeathPlayers(
                            CustomDeathReason.FollowingSuicide, partner.PlayerId);
                        ReportDeadBodyPatch.IgnoreBodyids[loversPlayer.PlayerId] = false;
                    }
                    else
                        partner.RpcMurderPlayer(partner, true);
                }
            }
        }
    }

    public static void CupidLoversWinCheck(ref GameOverReason reason)
    {
        if (IsCupidLoversDead) return;
        if (CupidLoversPlayers.Count == 0) return;
        if (CustomWinnerHolder.WinnerTeam == (CustomWinner)CustomRoles.CupidLovers) return;

        bool shouldWin =
    CupidLoversPlayers.All(p => p.IsAlive()) ||
    CupidLoversPlayers.Any(p => CustomWinnerHolder.NeutralWinnerIds.Contains(p.PlayerId));

        if (!shouldWin) return;

        if (!CupidLoverAddwin.GetBool())
        {
            if (CustomWinnerHolder.ResetAndSetAndChWinner(
                (CustomWinner)CustomRoles.CupidLovers, byte.MaxValue))
            {
                foreach (var p in AllPlayerControls
                    .Where(p => p.Is(CustomRoles.CupidLovers) && p.IsAlive()))
                {
                    CustomWinnerHolder.WinnerIds.Add(p.PlayerId);
                    CustomWinnerHolder.CantWinPlayerIds.Remove(p.PlayerId);
                }
                reason = GameOverReason.ImpostorsByKill;
            }
        }
        else
        {
            CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.CupidLovers);
            foreach (var p in AllPlayerControls
                .Where(p => p.Is(CustomRoles.CupidLovers) && p.IsAlive()))
            {
                CustomWinnerHolder.WinnerIds.Add(p.PlayerId);
                CustomWinnerHolder.CantWinPlayerIds.Remove(p.PlayerId);
            }
        }
    }

    public static bool CheckCupidLoversCountWin()
    {
        if (CupidLoversPlayers.Count == 0) return false;

        bool allAliveAreCupidLovers = AllAlivePlayerControls.All(p => p.Is(CustomRoles.CupidLovers));
        bool soloWin3 = CuLoversSolowin3players.GetBool()
            && AllAlivePlayersCount <= 3
            && CupidLoversPlayers.Count != 0
            && CupidLoversPlayers.All(p => p.IsAlive());

        if (!allAliveAreCupidLovers && !soloWin3) return false;

        CustomWinnerHolder.ResetAndSetAndChWinner(
            (CustomWinner)CustomRoles.CupidLovers, byte.MaxValue);
        foreach (var p in AllPlayerControls
            .Where(p => p.Is(CustomRoles.CupidLovers) && p.IsAlive()))
        {
            CustomWinnerHolder.WinnerIds.Add(p.PlayerId);
            CustomWinnerHolder.CantWinPlayerIds.Remove(p.PlayerId);
        }
        return true;
    }

    public static void CupidLoversDisconnected(PlayerControl player)
    {
        if (!player.Is(CustomRoles.CupidLovers) || player.Data.IsDead) return;
        IsCupidLoversDead = true;
        Lovers.isCupidLoversDead = true;
        foreach (var lv in CupidLoversPlayers)
            lv.GetPlayerState().RemoveSubRole(CustomRoles.CupidLovers);
        CupidLoversPlayers.Clear();
        Lovers.CuCupidLoversPlayers.Clear();
    }

    public override string GetProgressText(bool comms = false, bool GameLog = false)
    {
        if (hasDesignated) return "";
        return target1 != byte.MaxValue
            ? $"<color={RoleInfo.RoleColorCode}>(1/2)</color>"
            : $"<color={RoleInfo.RoleColorCode}>(0/2)</color>";
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null,
        bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (!Is(seer) || seer.PlayerId != seen.PlayerId || isForMeeting || !Player.IsAlive()) return "";
        if (hasDesignated) return "";
        return target1 == byte.MaxValue
            ? $"{(isForHud ? "" : "<size=60%>")}<color={RoleInfo.RoleColorCode}>キルボタンで1人目を指名</color>"
            : $"{(isForHud ? "" : "<size=60%>")}<color={RoleInfo.RoleColorCode}>キルボタンで2人目を指名</color>";
    }

    void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(target1);
        sender.Writer.Write(hasDesignated);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        target1 = reader.ReadByte();
        hasDesignated = reader.ReadBoolean();
    }

    private static string GetString(string key) => Translator.GetString(key);

    public static Dictionary<int, Achievement> achievements = new();

    [PluginModuleInitializer]
    public static void Load()
    {
        var n1 = new Achievement(RoleInfo, 0, 5, 0, 0);
        var l1 = new Achievement(RoleInfo, 1, 1, 0, 1);
        var l2 = new Achievement(RoleInfo, 2, 1, 0, 1, true);
        achievements.Add(0, n1);
        achievements.Add(1, l1);
        achievements.Add(2, l2);
    }
}