using AmongUs.GameOptions;
using Hazel;
using TownOfHost.Roles.Core;
using static TownOfHost.PlayerCatch;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Neutral;

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
            "#808080",
            (7, 2),
            true,
            from: From.SuperNewRoles
        );

    public Oblivion(PlayerControl player)
        : base(RoleInfo, player, () => HasTask.False)
    {
        hasTransformed = false;
        pendingRoleId = byte.MaxValue;
    }

    bool hasTransformed;
    // ★ 会議後に変化する役職を保留
    byte pendingRoleId;

    static void SetUpOptionItem() { }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(false);
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (reporter == null || reporter.PlayerId != Player.PlayerId) return;
        if (hasTransformed) return;
        if (target == null) return;
        if (target.Disconnected) return;

        var deadPlayer = GetPlayerById(target.PlayerId);
        if (deadPlayer == null) return;

        var newRole = deadPlayer.GetCustomRole();

        if (newRole is CustomRoles.GM or CustomRoles.NotAssigned or CustomRoles.Oblivion) return;

        // ★ 会議後に変化するよう保留
        pendingRoleId = target.PlayerId;
        SendRPC();
    }

    public override void AfterMeetingTasks()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!Player.IsAlive()) return;
        if (pendingRoleId == byte.MaxValue) return;

        var deadPlayer = GetPlayerById(pendingRoleId);
        pendingRoleId = byte.MaxValue;

        if (deadPlayer == null) return;

        var newRole = deadPlayer.GetCustomRole();
        if (newRole is CustomRoles.GM or CustomRoles.NotAssigned or CustomRoles.Oblivion) return;

        hasTransformed = true;

        if (!Utils.RoleSendList.Contains(Player.PlayerId))
            Utils.RoleSendList.Add(Player.PlayerId);

        Player.RpcSetCustomRole(newRole, log: null);

        UtilsGameLog.AddGameLog(
            "Oblivion",
            $"{UtilsName.GetPlayerColor(Player)}(忘却者)が" +
            $"{UtilsName.GetPlayerColor(deadPlayer)}の死体をレポートし" +
            $"{UtilsRoleText.GetRoleName(newRole)}に変化した"
        );

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
        sender.Writer.Write(pendingRoleId);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        hasTransformed = reader.ReadBoolean();
        pendingRoleId = reader.ReadByte();
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
        if (pendingRoleId != byte.MaxValue)
            return $"{(isForHud ? "" : "<size=60%>")}<color=#808080>会議後に役職が変化する...</color>";
        return $"{(isForHud ? "" : "<size=60%>")}<color=#808080>死体をレポートすると役職が変化する</color>";
    }
}