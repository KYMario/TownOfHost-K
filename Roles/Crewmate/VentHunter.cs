using AmongUs.GameOptions;
using Hazel;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using Sentry.Unity.NativeUtils;

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
            12800,
            SetupOptionItem,
            "vh",
            "#83FFF2",
            (9, 6),
            introSound: () => PlayerControl.LocalPlayer.KillSfx
        );
    public VentHunter(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        count = OptionCount.GetInt();
        cooldown = OptionCooldown.GetInt();
        trapDisappearTime = OptionDisappearTime.GetFloat();
        task = OptionTask.GetFloat();
        Isinfinity = count is 0;
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
    static bool Isinfinity;
    int count;

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
        OptionCount = IntegerOptionItem.Create(RoleInfo, 10, OptionName.VentHunterCount, new(0, 99, 1), 2, false).SetZeroNotation(OptionZeroNotation.Infinity);
        OptionCooldown = IntegerOptionItem.Create(RoleInfo, 11, OptionName.Cooldown, new(0, 120, 1), 30, false);
        OptionDisappearTime = FloatOptionItem.Create(RoleInfo, 12, OptionName.VentHunterTime, new(0, 30, 2.5f), 10, false);
        OptionTask = IntegerOptionItem.Create(RoleInfo, 13, OptionName.cantaskcount, new(0, 99, 1), 5, false);
    }

    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (!CanUseAbility || TrapVents.ContainsKey(ventId)) return false;

        count--;
        SendRPC();
        var players = Vent.Where(x => x.Value == ventId).Select(x => PlayerCatch.GetPlayerById(x.Key));
        var checkKiller = players.Any(x => x.GetRoleClass() is IKiller { CanKill: true });

        if (checkKiller)
        {
            var state = PlayerState.GetByPlayerId(Player.PlayerId);
            state.DeathReason = CustomDeathReason.Counter;
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
        if (!AmongUsClient.Instance.AmHost || Is(user)) return;
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

    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(count);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        count = reader.ReadInt32();
    }
    public override bool CanVentMoving(PlayerPhysics physics, int ventId) => false;
    public override string GetProgressText(bool comms = false, bool GameLog = false) => Isinfinity ? "" : Utils.ColorString(CanUseAbility ? RoleInfo.RoleColor : IsTaskCompleted ? Color.red : Color.gray, $"({count})");
    public bool CanUseAbility => (Isinfinity || count > 0) && IsTaskCompleted;
    public bool IsTaskCompleted => MyTaskState.HasCompletedEnoughCountOfTasks(task);
    public override bool CanClickUseVentButton => CanUseAbility;

    public void TrapKill(PlayerControl pc)
    {
        if (!pc.IsAlive()) return;
        CustomRoleManager.OnCheckMurder(Player, pc, pc, pc, true, false, deathReason: CustomDeathReason.Trap);
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        TrapVents.Clear();
        Vent.Clear();
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (trapDisappearTime == 0) return;
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
    public override bool OverrideAbilityButton(out string text)
    {
        text = "VentHunter_Ability";
        return true;
    }
}
