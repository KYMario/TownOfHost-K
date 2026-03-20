using System.Linq;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Neutral;

public sealed class Terrorist : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Terrorist),
            player => new Terrorist(player),
            CustomRoles.Terrorist,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Neutral,
            15400,
            SetupOptionItem,
            "te",
            "#00ff00",
            (7, 0),
            introSound: () => ShipStatus.Instance.CommonTasks.FirstOrDefault(task => task.TaskType == TaskTypes.FixWiring).MinigamePrefab.OpenSound,
            from: From.FoolersMod,
            Desc: () =>
            {
                var info = "";
                if (!OptionCanSuicideWin.GetBool()) info = GetString("TerroristDescInfo");
                return GetString("TerroristInfoLong") + info;
            }
            );
    public Terrorist(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.ForRecompute
    )
    {
        canSuicideWin = OptionCanSuicideWin.GetBool();
    }
    private static OptionItem OptionCanSuicideWin;
    private static OverrideTasksData Tasks;
    private enum OptionName
    {
        CanTerroristSuicideWin
    }
    private static bool canSuicideWin;

    private static void SetupOptionItem()
    {
        SoloWinOption.Create(RoleInfo, 9, defo: 1);
        OptionCanSuicideWin = BooleanOptionItem.Create(RoleInfo, 10, OptionName.CanTerroristSuicideWin, false, false);
        // 20-23を使用
        Tasks = OverrideTasksData.Create(RoleInfo, 20);
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = 0;
        AURoleOptions.EngineerInVentMaxTime = 0;
    }
    public override void OnMurderPlayerAsTarget(MurderInfo info)
    {
        Logger.Info($"{Player.GetRealName()}はTerroristだった", nameof(Terrorist));
        if (CanWin())
        {
            MyState.DeathReason = CustomDeathReason.Suicide;
            Win();
        }
    }
    public override void OnExileWrapUp(NetworkedPlayerInfo exiled, ref bool DecidedWinner)
    {
        if (exiled.PlayerId != Player.PlayerId)
        {
            return;
        }

        if (CanWin())
        {
            Win();
            DecidedWinner = true;
        }
    }
    public bool CanWin()
    {
        if (!canSuicideWin && MyState.IsSuicide())
        {
            return false;
        }
        return IsTaskFinished;
    }
    public void Win()
    {
        foreach (var otherPlayer in PlayerCatch.AllAlivePlayerControls)
        {
            if (otherPlayer.Is(CustomRoles.Terrorist))
            {
                continue;
            }
            otherPlayer.SetRealKiller(Player);
            otherPlayer.RpcMurderPlayer(otherPlayer);
            var playerState = PlayerState.GetByPlayerId(otherPlayer.PlayerId);
            playerState.DeathReason = CustomDeathReason.Bombed;
            playerState.SetDead();
        }
        if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Terrorist, Player.PlayerId, true))
        {
            CustomWinnerHolder.NeutralWinnerIds.Add(Player.PlayerId);
        }
    }
    public static System.Collections.Generic.Dictionary<int, Achievement> achievements = new();
    [Attributes.PluginModuleInitializer]
    public static void Load()
    {
        var n1 = new Achievement(RoleInfo, 0, 1, 0, 0);
        achievements.Add(0, n1);
    }
}
