using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;

public sealed class Evilgambler : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Evilgambler),
                player => new Evilgambler(player),
                CustomRoles.Evilgambler,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Impostor,
                3300,
                SetupOptionItem,
                "eg",
                OptionSort: (2, 2),
                from: From.SuperNewRoles
            );
    public Evilgambler(PlayerControl player)
    : base(
    RoleInfo,
    player
    )
    {
        gamblecollect = OptionGamblecollect.GetInt();
        collectkillCooldown = OptionCollectkillCooldown.GetFloat();
        notcollectkillCooldown = OptionNotcollectkillCooldown.GetFloat();
        spcount = 0;
        l1flug = true;
    }

    private static OptionItem OptionGamblecollect;
    private static OptionItem OptionCollectkillCooldown;
    private static OptionItem OptionNotcollectkillCooldown;
    enum OptionName
    {
        Evillgamblergamblecollect,
        EvillgamblercollectkillCooldown,
        EvillgamblernotcollectkillCooldown,
    }

    private static float gamblecollect;
    private static float collectkillCooldown;
    private static float notcollectkillCooldown;

    public bool CanBeLastImpostor { get; } = false;
    private static void SetupOptionItem()
    {
        OptionGamblecollect = FloatOptionItem.Create(RoleInfo, 10, OptionName.Evillgamblergamblecollect, new(0, 100, 5), 50, false)
            .SetValueFormat(OptionFormat.Percent);
        OptionCollectkillCooldown = FloatOptionItem.Create(RoleInfo, 11, OptionName.EvillgamblercollectkillCooldown, new(0f, 180f, 0.5f), 2.5f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionNotcollectkillCooldown = FloatOptionItem.Create(RoleInfo, 12, OptionName.EvillgamblernotcollectkillCooldown, new(0f, 180f, 0.5f), 50.0f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        if (!info.IsSuicide)
        {
            (var killer, var target) = info.AttemptTuple;
            int chance = IRandom.Instance.Next(1, 101);
            if (chance < gamblecollect)
            {//gamble成功
                Logger.Info($"{killer?.Data?.GetLogPlayerName()}:${chance}成功", "Evilgamble");
                Main.AllPlayerKillCooldown[killer.PlayerId] = collectkillCooldown;
                killer.SyncSettings();//キルクール処理を同期
                spcount++;
                Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[0]);
                if (spcount == 3) Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[2]);
                l1flug = false;
            }
            else
            {
                Logger.Info($"{killer?.Data?.GetLogPlayerName()}:${chance}失敗", "Evilgamble");
                Main.AllPlayerKillCooldown[killer.PlayerId] = notcollectkillCooldown;
                killer.SyncSettings();//キルクール処理を同期
                spcount = -30;
            }
        }
    }
    public override void AfterMeetingTasks()
    {
        spcount = 0;
    }
    public override void CheckWinner(GameOverReason reason)
    {
        if (3 <= MyState.GetKillCount() && l1flug) Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[1]);
    }
    int spcount;
    bool l1flug;
    public static System.Collections.Generic.Dictionary<int, Achievement> achievements = new();
    [Attributes.PluginModuleInitializer]
    public static void Load()
    {
        var n1 = new Achievement(RoleInfo, 0, 1, 0, 0);
        var l1 = new Achievement(RoleInfo, 1, 1, 0, 1, true);
        var sp1 = new Achievement(RoleInfo, 2, 1, 0, 2, true);
        achievements.Add(0, n1);
        achievements.Add(1, l1);
        achievements.Add(2, sp1);
    }
}