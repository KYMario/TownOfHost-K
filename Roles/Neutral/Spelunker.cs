using AmongUs.GameOptions;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using UnityEngine;

namespace TownOfHost.Roles.Neutral;

public sealed class Spelunker : RoleBase, ISystemTypeUpdateHook
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Spelunker),
            player => new Spelunker(player),
            CustomRoles.Spelunker,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            55660,
            SetupOptionItem,
            "sp",
            "#f8cd46",
            (6, 3),
            from: From.SuperNewRoles
        );

    static OptionItem OptVentDeathChance;
    static OptionItem OptLadderFallChance;
    static OptionItem OptLiftZiplineFallChance;
    static OptionItem OptDoorDeathChance;
    static OptionItem OptDieBySabotage;
    static OptionItem OptLightSabotageDeathTime;
    static OptionItem OptCommsSabotageDeathTime;

    enum OptionName
    {
        SpelunkerVentDeathChance,
        SpelunkerLadderFallChance,
        SpelunkerLiftZiplineFallChance,
        SpelunkerDoorDeathChance,
        SpelunkerDieBySabotage,
        SpelunkerLightSabotageDeathTime,
        SpelunkerCommsSabotageDeathTime,
    }

    const float NearVentDistance = 1.1f;
    const float DoorRollCooldown = 0.2f;

    int nearVentId;
    bool ventStateInitialized;
    float lightsSabotageTimer;
    float commsSabotageTimer;
    float lastDoorRollAt;

    public Spelunker(PlayerControl player)
        : base(RoleInfo, player, () => HasTask.False)
    {
        ResetRuntimeState();
    }

    static void SetupOptionItem()
    {
        SoloWinOption.Create(RoleInfo, 9, defo: 50);

        OptVentDeathChance = IntegerOptionItem.Create(RoleInfo, 10, OptionName.SpelunkerVentDeathChance, new(0, 100, 1), 30, false)
            .SetValueFormat(OptionFormat.Percent);
        OptLadderFallChance = IntegerOptionItem.Create(RoleInfo, 11, OptionName.SpelunkerLadderFallChance, new(0, 100, 1), 30, false)
            .SetValueFormat(OptionFormat.Percent);
        OptLiftZiplineFallChance = IntegerOptionItem.Create(RoleInfo, 12, OptionName.SpelunkerLiftZiplineFallChance, new(0, 100, 1), 30, false)
            .SetValueFormat(OptionFormat.Percent);
        OptDoorDeathChance = IntegerOptionItem.Create(RoleInfo, 13, OptionName.SpelunkerDoorDeathChance, new(0, 100, 1), 30, false)
            .SetValueFormat(OptionFormat.Percent);

        OptDieBySabotage = BooleanOptionItem.Create(RoleInfo, 14, OptionName.SpelunkerDieBySabotage, false, false);
        OptLightSabotageDeathTime = FloatOptionItem.Create(RoleInfo, 15, OptionName.SpelunkerLightSabotageDeathTime, new(0f, 120f, 2.5f), 15f, false, OptDieBySabotage)
            .SetValueFormat(OptionFormat.Seconds);
        OptCommsSabotageDeathTime = FloatOptionItem.Create(RoleInfo, 16, OptionName.SpelunkerCommsSabotageDeathTime, new(0f, 120f, 2.5f), 15f, false, OptDieBySabotage)
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void Add() => ResetRuntimeState();

    public override void AfterMeetingTasks() => ResetRuntimeState();

    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!Is(player)) return;
        if (!player.IsAlive())
        {
            lightsSabotageTimer = 0f;
            commsSabotageTimer = 0f;
            return;
        }
        if (!GameStates.IsInTask || GameStates.CalledMeeting || GameStates.Intro) return;

        CheckVentProximityDeath();
        CheckSabotageDeath();
    }

    public static bool CheckWin(ref GameOverReason reason)
    {
        foreach (var pc in PlayerCatch.AllPlayerControls)
        {
            if (!pc.IsAlive()) continue;
            if (pc.GetRoleClass() is not Spelunker) continue;

            if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Spelunker, pc.PlayerId, AddWin: false))
            {
                CustomWinnerHolder.NeutralWinnerIds.Add(pc.PlayerId);
                reason = GameOverReason.ImpostorsByKill;
                return true;
            }
        }
        return false;
    }

    public static bool OnLadderClimbed(PlayerControl player)
    {
        if (!TryGetActiveRole(player, out var spelunker)) return false;
        spelunker.TryLadderFallDeath();
        return true;
    }

    public static bool OnZiplineUsed(PlayerControl player, bool fromTop)
    {
        if (!TryGetActiveRole(player, out var spelunker)) return false;
        spelunker.TryZiplineFallDeath(fromTop);
        return true;
    }

    public static bool OnMovingPlatformUsed(PlayerControl player)
    {
        if (!TryGetActiveRole(player, out var spelunker)) return false;
        spelunker.TryMovingPlatformFallDeath();
        return true;
    }

    bool ISystemTypeUpdateHook.UpdateDoorsSystem(DoorsSystemType doorsSystem, byte amount)
    {
        TryDoorDeath();
        return true;
    }

    void CheckVentProximityDeath()
    {
        if (ShipStatus.Instance?.AllVents == null) return;

        int nearestVentId = -1;
        float nearestDistance = float.MaxValue;
        var position = Player.GetTruePosition();

        foreach (var vent in ShipStatus.Instance.AllVents)
        {
            var distance = Vector2.Distance(position, vent.transform.position);
            if (distance > NearVentDistance) continue;
            if (distance >= nearestDistance) continue;

            nearestDistance = distance;
            nearestVentId = vent.Id;
        }

        if (!ventStateInitialized)
        {
            nearVentId = nearestVentId;
            ventStateInitialized = true;
            return;
        }

        if (nearestVentId != -1 && nearestVentId != nearVentId)
        {
            if (RollChance(OptVentDeathChance.GetInt()))
            {
                KillSelf(CustomDeathReason.Fall);
                return;
            }
        }

        nearVentId = nearestVentId;
    }

    void CheckSabotageDeath()
    {
        if (!OptDieBySabotage.GetBool())
        {
            lightsSabotageTimer = 0f;
            commsSabotageTimer = 0f;
            return;
        }

        if (Utils.IsActive(SystemTypes.Electrical))
        {
            lightsSabotageTimer += Time.fixedDeltaTime;
            if (lightsSabotageTimer >= OptLightSabotageDeathTime.GetFloat())
            {
                KillSelf(CustomDeathReason.Suffocation);
                return;
            }
        }
        else
        {
            lightsSabotageTimer = 0f;
        }

        if (Utils.IsActive(SystemTypes.Comms))
        {
            commsSabotageTimer += Time.fixedDeltaTime;
            if (commsSabotageTimer >= OptCommsSabotageDeathTime.GetFloat())
            {
                KillSelf(CustomDeathReason.Suffocation);
                return;
            }
        }
        else
        {
            commsSabotageTimer = 0f;
        }
    }

    void TryDoorDeath()
    {
        if (Time.time - lastDoorRollAt < DoorRollCooldown) return;
        lastDoorRollAt = Time.time;

        if (!RollChance(OptDoorDeathChance.GetInt())) return;
        KillSelf(CustomDeathReason.Fall);
    }

    void TryLadderFallDeath()
    {
        if (!RollChance(OptLadderFallChance.GetInt())) return;

        _ = new LateTask(() =>
        {
            if (!CanDieNow(checkMeeting: true)) return;
            KillSelf(CustomDeathReason.Fall);
        }, 0.35f, "SpelunkerLadderFall", true);
    }

    void TryZiplineFallDeath(bool fromTop)
    {
        if (!RollChance(OptLiftZiplineFallChance.GetInt())) return;

        if (!Main.AllPlayerSpeed.TryGetValue(Player.PlayerId, out var speedBefore))
            speedBefore = Main.MinSpeed;

        _ = new LateTask(() =>
        {
            if (!CanDieNow(checkMeeting: true)) return;
            Main.AllPlayerSpeed[Player.PlayerId] = Main.MinSpeed;
            Player.SyncSettings();
        }, 3f, "SpelunkerZiplineSlow", true);

        _ = new LateTask(() =>
        {
            Main.AllPlayerSpeed[Player.PlayerId] = speedBefore;
            Player.SyncSettings();

            if (!CanDieNow(checkMeeting: true)) return;
            KillSelf(CustomDeathReason.Fall);
        }, fromTop ? 5f : 8f, "SpelunkerZiplineFall", true);
    }

    void TryMovingPlatformFallDeath()
    {
        if (!RollChance(OptLiftZiplineFallChance.GetInt())) return;
        KillSelf(CustomDeathReason.Fall);
    }

    void KillSelf(CustomDeathReason reason)
    {
        if (!CanDieNow()) return;

        var state = PlayerState.GetByPlayerId(Player.PlayerId);
        if (state == null) return;

        state.DeathReason = reason;
        state.SetDead();
        Player.RpcMurderPlayerV2(Player);
    }

    bool CanDieNow(bool checkMeeting = false)
    {
        if (!AmongUsClient.Instance.AmHost) return false;
        if (Player == null || Player.Data == null) return false;
        if (!Player.IsAlive()) return false;
        if (checkMeeting && GameStates.CalledMeeting) return false;
        return true;
    }

    void ResetRuntimeState()
    {
        nearVentId = -1;
        ventStateInitialized = false;
        lightsSabotageTimer = 0f;
        commsSabotageTimer = 0f;
        lastDoorRollAt = -10f;
    }

    static bool TryGetActiveRole(PlayerControl player, out Spelunker spelunker)
    {
        spelunker = null;

        if (!AmongUsClient.Instance.AmHost) return false;
        if (player == null || player.Data == null) return false;
        if (player.Data.IsDead || !player.IsAlive()) return false;
        if (!GameStates.IsInTask || GameStates.CalledMeeting || GameStates.Intro) return false;

        spelunker = player.GetRoleClass() as Spelunker;
        return spelunker != null;
    }

    static bool RollChance(int chance)
    {
        chance = Mathf.Clamp(chance, 0, 100);
        return chance > 0 && IRandom.Instance.Next(1, 101) <= chance;
    }
}