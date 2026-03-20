using System.Linq;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;

public sealed class BorderKiller : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(BorderKiller),
            player => new BorderKiller(player),
            CustomRoles.BorderKiller,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            7300,
            SetupOptionItem,
            "Br",
            OptionSort: (8, 1),
            Desc: () =>
            {
                return string.Format(GetString("BorderKillerDesc"), OptionMissionKillcount.GetInt());
            }
        );
    public BorderKiller(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
    }
    static OptionItem OptionKillCoolDown;
    static OptionItem OptionMissionKillcount;
    enum OptionName
    {
        BorderKillerMissionKillcount
    }

    private static void SetupOptionItem()
    {
        OptionKillCoolDown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, OptionBaseCoolTime, 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionMissionKillcount = IntegerOptionItem.Create(RoleInfo, 11, OptionName.BorderKillerMissionKillcount, new(1, 14, 1), 3, false).SetValueFormat(OptionFormat.Players);
    }
    public float CalculateKillCooldown() => OptionKillCoolDown.GetFloat();
    public override string GetProgressText(bool comms = false, bool GameLog = false) => $"({MyState.GetKillCount(false)}/{OptionMissionKillcount.GetInt()})";

    public override void CheckWinner(GameOverReason reason)
    {
        //目標キルカウント ＞ 現在のキルカウント
        if (OptionMissionKillcount.GetInt() > MyState.GetKillCount(false) && Player.IsWinner(CustomWinner.Impostor))
        {
            CustomWinnerHolder.CantWinPlayerIds.Add(Player.PlayerId);
            CustomWinnerHolder.WinnerIds.Remove(Player.PlayerId);
        }
        else if (OptionMissionKillcount.GetInt() <= MyState.GetKillCount(false))
        {
            Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[0]);
            if (Player.IsWinner(CustomWinner.Impostor))
                Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[1]);
        }
        if (5 <= MyState.GetKillCount())
            Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[2]);
    }
    public static System.Collections.Generic.Dictionary<int, Achievement> achievements = new();
    [Attributes.PluginModuleInitializer]
    public static void Load()
    {
        var n1 = new Achievement(RoleInfo, 0, 1, 0, 0);
        var l1 = new Achievement(RoleInfo, 1, 1, 0, 1);
        var l2 = new Achievement(RoleInfo, 2, 1, 0, 1);
        achievements.Add(0, n1);
        achievements.Add(1, l1);
        achievements.Add(2, l2);
    }
}