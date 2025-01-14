using AmongUs.GameOptions;
using System.Collections.Generic;
using UnityEngine;

using TownOfHost.Roles.Core;
using System.Linq;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Crewmate;
public sealed class VentHunter : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(VentHunter),
            player => new VentHunter(player),
            CustomRoles.VentHunter,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            23700,
            SetupOptionItem,
            "vh",
            "#83FFF2",
            introSound: () => PlayerControl.LocalPlayer.KillSfx
        );
    public VentHunter(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        count = OptionCount.GetFloat();
        cooldown = OptionCooldown.GetInt();
        trapDisappearTime = OptionDisappearTime.GetFloat();
        task = OptionTask.GetFloat();
        nasi = count is 0;
        TrapVents = new();
        Vent = new();
        CustomRoleManager.OnEnterVentOthers.Add(OnEnterVentOthers);
    }

    private static OptionItem OptionCount;
    private static OptionItem OptionCooldown;
    private static OptionItem OptionDisappearTime;
    private static OptionItem OptionTask;
    static int cooldown;
    static float trapDisappearTime;
    static float task;
    static bool nasi;
    float count;

    Dictionary<int, float> TrapVents;
    static Dictionary<byte, int> Vent;

    enum OptionName
    {
        VentHunterCount,
        VentHunterTime,
        Cooldown,
        cantaskcount
    }

    private static void SetupOptionItem()
    {
        OptionCount = FloatOptionItem.Create(RoleInfo, 10, OptionName.VentHunterCount, new(0, 99, 1), 2, false);
        OptionCooldown = IntegerOptionItem.Create(RoleInfo, 11, OptionName.Cooldown, new(0, 120, 1), 30, false);
        OptionDisappearTime = FloatOptionItem.Create(RoleInfo, 12, OptionName.VentHunterTime, new(0, 30, 2.5f), 10, false);
        OptionTask = FloatOptionItem.Create(RoleInfo, 13, OptionName.cantaskcount, new(0, 99, 1), 5, false);
    }

    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (!CanUseAbility || TrapVents.ContainsKey(ventId)) return false;

        count--;
        var players = Vent.Where(x => x.Value == ventId).Select(x => PlayerCatch.GetPlayerById(x.Key));
        var checkKiller = players.Any(x => x.GetRoleClass() is IKiller { CanKill: true });

        if (checkKiller)
        {
            var state = PlayerState.GetByPlayerId(Player.PlayerId);
            state.DeathReason = CustomDeathReason.Revenge2;
            Player.RpcMurderPlayer(Player, true);
            state.SetDead();
            UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player);
            return false;
        }

        TrapVents.Add(ventId, 0f);
        Player.RpcProtectedMurderPlayer();
        UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player);

        foreach (var pc in players)
            TrapKill(pc);

        if (players.Any())
            Player.RpcProtectedMurderPlayer(Player);

        return false;
    }
    public static bool OnEnterVentOthers(PlayerPhysics physics, int ventId)
    {
        Vent[physics.myPlayer.PlayerId] = ventId;
        return true;
    }
    public override void OnVentilationSystemUpdate(PlayerControl user, VentilationSystem.Operation Operation, int ventId)
    {
        if (Is(user)) return;
        if (Operation is VentilationSystem.Operation.Move)
            Vent[user.PlayerId] = ventId;
        if (Operation is VentilationSystem.Operation.Exit or VentilationSystem.Operation.BootImpostors)
            Vent.Remove(user.PlayerId);

        if (Operation is VentilationSystem.Operation.Enter or VentilationSystem.Operation.Move or VentilationSystem.Operation.Exit)
        {
            if (TrapVents.ContainsKey(ventId))
            {
                TrapKill(user);
            }
        }
    }

    public override bool CantVentIdo(PlayerPhysics physics, int ventId) => false;
    public override string GetProgressText(bool comms = false, bool GameLog = false) => nasi ? "" : Utils.ColorString(CanUseAbility ? RoleInfo.RoleColor : IsTaskCompleted ? Color.red : Color.gray, $"({count})");
    public bool CanUseAbility => (nasi || count > 0) && IsTaskCompleted;
    public bool IsTaskCompleted => MyTaskState.HasCompletedEnoughCountOfTasks(task);
    public override bool CanClickUseVentButton => CanUseAbility;

    public void TrapKill(PlayerControl pc)
    {
        pc.SetRealKiller(Player);
        var state = PlayerState.GetByPlayerId(pc.PlayerId);
        state.DeathReason = CustomDeathReason.Trap;
        pc.RpcMurderPlayer(pc, true);
        state.SetDead();
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        TrapVents.Clear();
        Vent.Clear();
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        foreach (var (ventid, time) in TrapVents)
        {
            if (time >= trapDisappearTime)
                TrapVents.Remove(ventid);
            else
                TrapVents[ventid] += Time.fixedDeltaTime;
        }
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = cooldown;
        AURoleOptions.EngineerInVentMaxTime = 1;
    }
    public override string GetAbilityButtonText() => GetString("VentHunterAbility");
}