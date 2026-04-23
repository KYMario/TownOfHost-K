/*NextUpdete
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;
public sealed class VentBeginner : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(VentBeginner),
            player => new VentBeginner(player),
            CustomRoles.VentBeginner,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            22300,
            SetupOptionItem,
            "vb",
            "#ff6666",
            introSound: () => DestroyableSingleton<HudManager>.Instance.TaskCompleteSound,
from: From.TownOfHost_K
        );
    public VentBeginner(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        first = FirstCooldown.GetFloat();
        SetAbilityCooldown = -1f;
    }

    private static OptionItem FirstCooldown;
    private float first;
    private float SetAbilityCooldown;

    enum Options
    {
        FirstCooldown
    }

    private static void SetupOptionItem()
    {
        FirstCooldown = FloatOptionItem.Create(RoleInfo, 10, Options.FirstCooldown, new(15f, 60f, 1f), 45f, false);
    }

    public override bool OnCompleteTask(uint taskid)
    {
        if (IsTaskFinished)
        {
            Player.RpcSetCustomRole(CustomRoles.VentMaster);
            return true;
        }
        Player.MarkDirtySettings();
        return true;
    }

    public override bool CantVentIdo(PlayerPhysics physics, int ventId)
    {
        var state = Player.GetPlayerTaskState();
        return state.AllTasksCount / 1.5 >= state.AllTasksCount - state.CompletedTasksCount;
    }

    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        var state = Player.GetPlayerTaskState();
        if (state.AllTasksCount / 1.5 >= state.AllTasksCount - state.CompletedTasksCount)
        {
            SetAbilityCooldown = 0;
            Player.MarkDirtySettings();
            Player.RpcResetAbilityCooldown();
            Player.MarkDirtySettings();
        }
        return true;
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        var state = Player.GetPlayerTaskState();
        AURoleOptions.EngineerCooldown = SetAbilityCooldown != -1f ? SetAbilityCooldown : first - (first / state.AllTasksCount * state.CompletedTasksCount);
        AURoleOptions.EngineerInVentMaxTime = 0;
        SetAbilityCooldown = -1f;
    }
}
*/