using AmongUs.GameOptions;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Neutral;

public sealed class God : RoleBase, ISystemTypeUpdateHook
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(God),
            player => new God(player),
            CustomRoles.God,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            24020,
            SetupOptionItem,
            "gd",
            "#FFD700",
            (6, 2),
            from: From.SuperNewRoles
        );

    public God(PlayerControl player)
    : base(RoleInfo, player, () => HasTask.ForRecompute)
    {
        WinTaskCount = OptWinTaskCount.GetInt();
        MyTaskState.NeedTaskCount = WinTaskCount;
        checktaskwinflag = false;
    }

    static OptionItem OptWinTaskCount;
    int WinTaskCount;
    bool checktaskwinflag;

    enum OptionName
    {
        GodWinTaskCount,
    }

    private static void SetupOptionItem()
    {
        SoloWinOption.Create(RoleInfo, 9, defo: 15);
        OptWinTaskCount = IntegerOptionItem.Create(RoleInfo, 10, OptionName.GodWinTaskCount, new(1, 99, 1), 6, false)
            .SetValueFormat(OptionFormat.Pieces);
        OverrideTasksData.Create(RoleInfo, 20);
    }

    public override bool OnCompleteTask(uint taskid)
    {
        if (MyTaskState.HasCompletedEnoughCountOfTasks(WinTaskCount))
            checktaskwinflag = true;
        return true;
    }

    // 全員の役職を神の画面にのみ表示
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (!Is(seer)) return "";
        if (seer.PlayerId == seen.PlayerId) return "";
        if (!Player.IsAlive()) return "";

        var role = seen.GetCustomRole();
        return $"<color={UtilsRoleText.GetRoleColorCode(role)}>{UtilsRoleText.GetRoleName(role)}</color>";
    }

    // サボタージュ修理をブロック（ドア以外）
    // falseを返すと修理をキャンセル
    bool ISystemTypeUpdateHook.UpdateReactorSystem(ReactorSystemType reactorSystem, byte amount) => false;
    bool ISystemTypeUpdateHook.UpdateHeliSabotageSystem(HeliSabotageSystem heliSabotageSystem, byte amount) => false;
    bool ISystemTypeUpdateHook.UpdateLifeSuppSystem(LifeSuppSystemType lifeSuppSystem, byte amount) => false;
    bool ISystemTypeUpdateHook.UpdateHqHudSystem(HqHudSystemType hqHudSystemType, byte amount) => false;
    // ★ ドアとスイッチ（停電）はUpdateSwitchSystemがfalseで修理不可になる
    bool ISystemTypeUpdateHook.UpdateSwitchSystem(SwitchSystem switchSystem, byte amount) => false;
    // ★ 通信サボ
    bool ISystemTypeUpdateHook.UpdateHudOverrideSystem(HudOverrideSystemType hudOverrideSystem, byte amount) => false;

    // 勝利判定
    public static bool CheckWin(ref GameOverReason reason)
    {
        foreach (var pc in PlayerCatch.AllPlayerControls)
        {
            if (pc.GetRoleClass() is not God god) continue;
            if (!pc.IsAlive()) continue;
            if (!god.checktaskwinflag) continue;

            if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.God, pc.PlayerId, true))
            {
                CustomWinnerHolder.NeutralWinnerIds.Add(pc.PlayerId);
                reason = GameOverReason.ImpostorsByKill;
                return true;
            }
        }
        return false;
    }

    public override string GetProgressText(bool comms = false, bool GameLog = false)
    {
        var color = checktaskwinflag ? "#FFD700" : "#5e5e5e";
        return $"<color={color}>({MyTaskState.CompletedTasksCount}/{WinTaskCount})</color>";
    }

    public override void OverrideProgressTextAsSeen(PlayerControl seer, ref bool enabled, ref string text)
    {
        if (seer == null) return;
        if (Is(seer) || seer.Is(CustomRoles.GM)) return;
        text = $"<#cccccc>(?/{MyTaskState.AllTasksCount})";
    }
}