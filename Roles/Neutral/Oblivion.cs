using AmongUs.GameOptions;
using Hazel;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.PlayerCatch;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Neutral;

/// <summary>
/// 忘却者 (Oblivion)
/// 第三陣営。死体をレポートするとそのプレイヤーの役職に変化する。
/// 切断者の死体をレポートした場合は変化しない。
/// 勝利条件：変化後の役職に準ずる。
/// </summary>
public sealed class Oblivion : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Oblivion),
            player => new Oblivion(player),
            CustomRoles.Oblivion,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            30400,
            SetUpOptionItem,
            "ob",
            "#b0b0d0",
            (6, 2),
            true,
            from: From.SuperNewRoles
        );

    public Oblivion(PlayerControl player)
        : base(RoleInfo, player, () => HasTask.False)
    {
        hasTransformed = false;
    }

    bool hasTransformed;

    static void SetUpOptionItem()
    {
        // 現時点でオプションなし（将来拡張用）
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(false);
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        // ★ レポートしたのが自分でない場合は無視
        if (reporter == null || reporter.PlayerId != Player.PlayerId) return;

        // ★ すでに変化済みなら無視
        if (hasTransformed) return;

        // ★ targetがnull（緊急会議ボタン）の場合は無視
        if (target == null) return;

        // ★ 切断者の死体の場合は変化しない
        if (target.Disconnected) return;

        // ★ レポート相手がいない場合は無視
        var deadPlayer = GetPlayerById(target.PlayerId);
        if (deadPlayer == null) return;

        var newRole = deadPlayer.GetCustomRole();

        // ★ GM・切断者・無効役職は除外
        if (newRole is CustomRoles.GM or CustomRoles.NotAssigned) return;

        // ★ 自分と同じ役職は意味がないのでスキップ
        if (newRole == CustomRoles.Oblivion) return;

        hasTransformed = true;

        // ★ 役職変化処理
        if (!Utils.RoleSendList.Contains(Player.PlayerId))
            Utils.RoleSendList.Add(Player.PlayerId);

        Player.RpcSetCustomRole(newRole, log: null);

        // ★ ログ
        UtilsGameLog.AddGameLog(
            "Oblivion",
            $"{UtilsName.GetPlayerColor(Player)}(忘却者)が" +
            $"{UtilsName.GetPlayerColor(deadPlayer)}の死体をレポートし" +
            $"{UtilsRoleText.GetRoleName(newRole)}に変化した"
        );

        // ★ 本人にメッセージ
        Utils.SendMessage(
            string.Format(GetString("OblivionTransformed"),
                UtilsRoleText.GetRoleName(newRole)),
            Player.PlayerId
        );

        SendRPC();
        UtilsNotifyRoles.NotifyRoles();
    }

    void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(hasTransformed);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        hasTransformed = reader.ReadBoolean();
    }

    public override string GetProgressText(bool comms = false, bool GameLog = false)
    {
        if (hasTransformed) return "";
        return "<color=#b0b0d0>(未変化)</color>";
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null,
        bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (!Is(seer) || seer.PlayerId != seen.PlayerId || !Player.IsAlive()) return "";
        if (hasTransformed) return "";
        return $"{(isForHud ? "" : "<size=60%>")}<color=#b0b0d0>死体をレポートすると役職が変化する</color>";
    }
}