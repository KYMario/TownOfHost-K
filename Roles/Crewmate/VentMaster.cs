using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;

public sealed class VentMaster : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(VentMaster),
            player => new VentMaster(player),
            CustomRoles.VentMaster,
            () => CanUseVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            12600,
            SetUpOptionItem,
            "vm",
            "#ff6666",
            (9, 4),
            introSound: () => GetIntroSound(RoleTypes.Noisemaker)
        );
    public VentMaster(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CustomRoleManager.OnEnterVentOthers.Add(OnEnterVentOthers);
        callcount = 0;
    }
    int callcount;
    static OptionItem CanUseVent;
    static void SetUpOptionItem()
    {
        CanUseVent = BooleanOptionItem.Create(RoleInfo, 10, GeneralOption.CanVent, true, false);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = 0;
        AURoleOptions.EngineerInVentMaxTime = 0;
    }
    public static bool OnEnterVentOthers(PlayerPhysics physics, int ventId)
    {
        var user = physics.myPlayer;
        if (!user.Is(CustomRoles.VentMaster))
        {
            foreach (var seer in PlayerCatch.AllPlayerControls)
            {
                if (seer.Is(CustomRoles.VentMaster) && seer.PlayerId != user.PlayerId)
                {
                    if (seer.IsAlive() && GameStates.IsInTask)
                        seer.KillFlash();
                    if (seer.GetRoleClass() is VentMaster ventMaster) ventMaster.callcount++;
                }
            }
        }
        return true;
    }
    public override void CheckWinner(GameOverReason reason)
    {
        Achievements.RpcCompleteAchievement(Player.PlayerId, 1, achievements[0], callcount);
        Achievements.RpcCompleteAchievement(Player.PlayerId, 1, achievements[1], callcount);
    }
    public static System.Collections.Generic.Dictionary<int, Achievement> achievements = new();
    [Attributes.PluginModuleInitializer]
    public static void Load()
    {
        var n1 = new Achievement(RoleInfo, 0, 3, 0, 0);
        var l1 = new Achievement(RoleInfo, 1, 30, 0, 1);
        achievements.Add(0, n1);
        achievements.Add(1, l1);
    }
}