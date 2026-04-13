using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Hazel;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
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
            "cp",
            "#ff69b4",
            (6, 2),
            true,
            from: From.SuperNewRoles
        );

    public Cupid(PlayerControl player)
    : base(RoleInfo, player, () => HasTask.False)
    {
        KillCooldown = OptKillCooldown.GetFloat();
        target1 = byte.MaxValue;
        target2 = byte.MaxValue;
        hasDesignated = false;
    }

    static OptionItem OptKillCooldown;
    static float KillCooldown;

    byte target1;
    byte target2;
    bool hasDesignated;

    // ★ CupidLovers管理（static: ゲーム全体で共有）
    public static List<PlayerControl> CupidLoversPlayers = new();
    public static bool IsCupidLoversDead = false;

    enum OptionName { CupidKillCooldown }

    private static void SetupOptionItem()
    {
        OptKillCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.CupidKillCooldown,
            new(0f, 60f, 0.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void Add()
    {
        CupidLoversPlayers.Clear();
        IsCupidLoversDead = false;
    }

    public float CalculateKillCooldown() => hasDesignated ? 0f : KillCooldown;
    public bool CanUseKillButton() => Player.IsAlive() && !hasDesignated;
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => false;

    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(false);
    }

    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        info.DoKill = false;

        if (hasDesignated) return;
        if (target.PlayerId == killer.PlayerId) return;

        // ★ 1人目の指名
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

        // ★ 1人目と同じ人は不可
        if (target.PlayerId == target1) return;

        // ★ 2人目の指名完了
        target2 = target.PlayerId;
        hasDesignated = true;

        var t1 = GetPlayerById(target1);
        var t2 = GetPlayerById(target2);

        if (t1 != null && t2 != null)
        {
            // ★ CupidLoversとして登録
            CupidLoversPlayers.Add(t1);
            CupidLoversPlayers.Add(t2);
            Lovers.HaveLoverDontTaskPlayers.Add(t1.PlayerId);
            Lovers.HaveLoverDontTaskPlayers.Add(t2.PlayerId);

            // ★ SubRoleに追加
            PlayerState.GetByPlayerId(t1.PlayerId).SetSubRole(CustomRoles.CupidLovers);
            PlayerState.GetByPlayerId(t2.PlayerId).SetSubRole(CustomRoles.CupidLovers);

            // ★ 名前色
            NameColorManager.Add(killer.PlayerId, t1.PlayerId, "#ff69b4");
            NameColorManager.Add(killer.PlayerId, t2.PlayerId, "#ff69b4");

            // ★ RPC同期
            SyncCupidLovers();

            // ★ メッセージ
            Utils.SendMessage(
                string.Format(GetString("CupidLoversSet"),
                    UtilsName.GetPlayerColor(t1, true),
                    UtilsName.GetPlayerColor(t2, true)),
                killer.PlayerId);
            Utils.SendMessage(
                string.Format(GetString("CupidYouAreLovers"),
                    UtilsName.GetPlayerColor(t2, true)),
                t1.PlayerId);
            Utils.SendMessage(
                string.Format(GetString("CupidYouAreLovers"),
                    UtilsName.GetPlayerColor(t1, true)),
                t2.PlayerId);

            UtilsGameLog.AddGameLog("Cupid",
                $"{UtilsName.GetPlayerColor(killer)}が{UtilsName.GetPlayerColor(t1)}と{UtilsName.GetPlayerColor(t2)}をラバーズに指名");
        }

        SendRPC();
        UtilsNotifyRoles.NotifyRoles();
    }

    // ★ IAdditionalWinner: 指名したラバーズが勝利したら共存勝利
    public bool CheckWin(ref CustomRoles winnerRole)
    {
        if (target1 == byte.MaxValue || target2 == byte.MaxValue) return false;
        return CustomWinnerHolder.WinnerIds.Contains(target1)
            || CustomWinnerHolder.WinnerIds.Contains(target2);
    }

    // ★ CupidLovers心中処理（OnMurderPlayer等から呼ぶ）
    public static void CupidLoversSuicide(byte deathId = 0x7f, bool isExiled = false)
    {
        if (IsCupidLoversDead) return;
        if (CupidLoversPlayers.Count == 0) return;

        foreach (var loversPlayer in CupidLoversPlayers)
        {
            if (!loversPlayer.Data.IsDead && loversPlayer.PlayerId != deathId) continue;

            isExiled |= ExtendedPlayerControl.GetDeadBodys().Contains(loversPlayer.Data) is false;
            IsCupidLoversDead = true;

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

    // ★ 勝利判定（LoversSoloWin相当）
    public static void CupidLoversWinCheck(ref GameOverReason reason)
    {
        if (IsCupidLoversDead) return;
        if (CupidLoversPlayers.Count == 0) return;
        if (CustomWinnerHolder.WinnerTeam == (CustomWinner)CustomRoles.CupidLovers) return;

        if (CupidLoversPlayers.All(p => p.IsAlive())
            || CupidLoversPlayers.Any(p => CustomWinnerHolder.NeutralWinnerIds.Contains(p.PlayerId)))
        {
            if (CustomWinnerHolder.ResetAndSetAndChWinner(
                (CustomWinner)CustomRoles.CupidLovers, byte.MaxValue))
            {
                foreach (var p in PlayerCatch.AllPlayerControls
                    .Where(p => p.Is(CustomRoles.CupidLovers) && p.IsAlive()))
                {
                    CustomWinnerHolder.WinnerIds.Add(p.PlayerId);
                    CustomWinnerHolder.CantWinPlayerIds.Remove(p.PlayerId);
                }
                reason = GameOverReason.ImpostorsByKill;
            }
        }
    }

    // ★ 人数チェック勝利（2人だけ残ったとき）
    public static bool CheckCupidLoversCountWin()
    {
        if (CupidLoversPlayers.Count == 0) return false;
        if (PlayerCatch.AllAlivePlayerControls.All(p => p.Is(CustomRoles.CupidLovers)))
        {
            CustomWinnerHolder.ResetAndSetAndChWinner(
                (CustomWinner)CustomRoles.CupidLovers, byte.MaxValue);
            foreach (var p in PlayerCatch.AllPlayerControls
                .Where(p => p.Is(CustomRoles.CupidLovers) && p.IsAlive()))
            {
                CustomWinnerHolder.WinnerIds.Add(p.PlayerId);
                CustomWinnerHolder.CantWinPlayerIds.Remove(p.PlayerId);
            }
            return true;
        }
        return false;
    }

    // ★ 切断処理
    public static void CupidLoversDisconnected(PlayerControl player)
    {
        if (!player.Is(CustomRoles.CupidLovers) || player.Data.IsDead) return;
        IsCupidLoversDead = true;
        foreach (var lv in CupidLoversPlayers)
            lv.GetPlayerState().RemoveSubRole(CustomRoles.CupidLovers);
        CupidLoversPlayers.Clear();
    }

    // ★ RPC同期
    private static void SyncCupidLovers()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        using var sender = new SubRoleRPCSender(CustomRoles.CupidLovers, 0);
        sender.Writer.Write(CupidLoversPlayers.Count);
        foreach (var p in CupidLoversPlayers)
            sender.Writer.Write(p.PlayerId);
    }

    public override string GetProgressText(bool comms = false, bool GameLog = false)
    {
        if (hasDesignated) return "";
        return target1 != byte.MaxValue
            ? "<color=#ff69b4>(1/2)</color>"
            : "<color=#ff69b4>(0/2)</color>";
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null,
        bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (!Is(seer) || seer.PlayerId != seen.PlayerId || isForMeeting || !Player.IsAlive()) return "";
        if (hasDesignated) return "";
        return target1 == byte.MaxValue
            ? $"{(isForHud ? "" : "<size=60%>")}<color=#ff69b4>キルボタンで1人目を指名</color>"
            : $"{(isForHud ? "" : "<size=60%>")}<color=#ff69b4>キルボタンで2人目を指名</color>";
    }

    void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(target1);
        sender.Writer.Write(target2);
        sender.Writer.Write(hasDesignated);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        target1 = reader.ReadByte();
        target2 = reader.ReadByte();
        hasDesignated = reader.ReadBoolean();
    }

    private static string GetString(string key) => Translator.GetString(key);
}