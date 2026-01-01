using AmongUs.GameOptions;
using UnityEngine;
using Hazel;
using System.Collections.Generic;
using HarmonyLib;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;

public sealed class TeleportKiller : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(TeleportKiller),
            player => new TeleportKiller(player),
            CustomRoles.TeleportKiller,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            4300,
            SetupOptionItem,
            "tk",
            OptionSort: (3, 5)
        );
    public TeleportKiller(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CustomRoleManager.OnEnterVentOthers.Add(OnEnterVentOthers);
        KillCooldown = OptionKillCoolDown.GetFloat();
        Cooldown = OptionCoolDown.GetFloat();
        Maximum = OptionmMaximum.GetFloat();
        Duration = OptionDuration.GetFloat();
        TeleportKillerVentgaaa = OptionTeleportKillerVentgaaa.GetBool();
        TeleportKillerPlatformFall = OptionTeleportKillerPlatformFall.GetBool();
        TeleportKillerLadderFall = OptionTeleportKillerLadderFall.GetBool();
        //ZiplineFall = OptionZiplineFall.GetBool(); ziplineってどうやってチェックするの..
        TeleportKillerDokkaaaan = OptionTeleportKillerDokkaaaan.GetBool();
        //LeaveSkin = OptionLeaveSkin.GetBool();
        TeleportKillerKillCooldownReset = OptionTeleportKillerKillCooldownReset.GetBool();
        DeathReason = OptionDeathReason.GetBool();
        usecount = 0;
        TeleportandKill = new();
        LadderPatch.Ladder.Clear();
        isAnimation = false;
        CheckVentD.Clear();
    }
    enum OptionName
    {
        KillCooldown,
        Cooldown,
        TeleportKillerMaximum,
        Duration,
        TeleportKillerFall,
        TeleportKillerVentgaaa,
        TeleportKillerPlatformFall,
        TeleportKillerLadderFall,
        //ZiplineFall,
        TeleportKillerDokkaaaan,
        //LeaveSkin,
        TeleportKillerKillCooldownReset,
        TeleportKillerChangeDeathReason
    }
    static OptionItem OptionKillCoolDown;
    static OptionItem OptionCoolDown;
    static OptionItem OptionmMaximum;
    static OptionItem OptionDuration;
    static OptionItem OptionTeleportKillerFall;
    static OptionItem OptionTeleportKillerVentgaaa;
    static OptionItem OptionTeleportKillerPlatformFall;
    static OptionItem OptionTeleportKillerLadderFall;
    //static OptionItem OptionZiplineFall;
    static OptionItem OptionTeleportKillerDokkaaaan;
    //static OptionItem OptionLeaveSkin;
    static OptionItem OptionTeleportKillerKillCooldownReset;
    static OptionItem OptionDeathReason;
    static float KillCooldown;
    static float Cooldown;
    static float Maximum;
    int usecount;
    static float Duration;
    static bool TeleportKillerVentgaaa; //↓ターゲットが使ってると自爆する系
    static bool TeleportKillerPlatformFall;
    static bool TeleportKillerLadderFall;
    //static bool ZiplineFall;
    static bool TeleportKillerDokkaaaan; //ターゲットが死んでいると自爆する
    //static bool LeaveSkin;
    static bool TeleportKillerKillCooldownReset;
    static bool DeathReason;
    List<byte> TeleportandKill;
    bool isAnimation;
    (Vector2, Vector2, float) AnimationData;
    static Dictionary<byte, int> CheckVentD = new();
    private static void SetupOptionItem()
    {
        OptionKillCoolDown = FloatOptionItem.Create(RoleInfo, 10, OptionName.KillCooldown, new(0f, 180f, 0.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionCoolDown = FloatOptionItem.Create(RoleInfo, 11, OptionName.Cooldown, new(0f, 180f, 0.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionmMaximum = IntegerOptionItem.Create(RoleInfo, 12, OptionName.TeleportKillerMaximum, new(0, 999, 1), 2, false)
            .SetValueFormat(OptionFormat.Times).SetZeroNotation(OptionZeroNotation.Infinity);
        OptionDuration = FloatOptionItem.Create(RoleInfo, 13, OptionName.Duration, new(0f, 15, 1f), 5f, false)
            .SetValueFormat(OptionFormat.Seconds).SetZeroNotation(OptionZeroNotation.Infinity);
        OptionTeleportKillerFall = BooleanOptionItem.Create(RoleInfo, 14, OptionName.TeleportKillerFall, false, false);
        OptionTeleportKillerVentgaaa = BooleanOptionItem.Create(RoleInfo, 15, OptionName.TeleportKillerVentgaaa, false, false, OptionTeleportKillerFall);
        OptionTeleportKillerPlatformFall = BooleanOptionItem.Create(RoleInfo, 16, OptionName.TeleportKillerPlatformFall, false, false, OptionTeleportKillerFall);
        OptionTeleportKillerLadderFall = BooleanOptionItem.Create(RoleInfo, 17, OptionName.TeleportKillerLadderFall, false, false, OptionTeleportKillerFall);
        //OptionZiplineFall = BooleanOptionItem.Create(RoleInfo, 18, OptionName.ZiplineFall, false, false,OptionTeleportKillerFall);
        OptionTeleportKillerDokkaaaan = BooleanOptionItem.Create(RoleInfo, 19, OptionName.TeleportKillerDokkaaaan, false, false, OptionTeleportKillerFall);
        //OptionLeaveSkin = BooleanOptionItem.Create(RoleInfo, 15, OptionName.LeaveSkin, false, false);
        OptionTeleportKillerKillCooldownReset = BooleanOptionItem.Create(RoleInfo, 20, OptionName.TeleportKillerKillCooldownReset, false, false);
        OptionDeathReason = BooleanOptionItem.Create(RoleInfo, 21, OptionName.TeleportKillerChangeDeathReason, false, false);
    }

    public bool CanBeLastImpostor { get; } = false;
    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(usecount);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        usecount = reader.ReadInt32();
    }
    public override void OnShapeshift(PlayerControl target)
    {
        if (!AmongUsClient.Instance.AmHost || Is(target) || (!target.IsAlive() && !TeleportKillerDokkaaaan) || (usecount >= Maximum && Maximum != 0)) return;
        usecount++;
        SendRPC();
        Logger.Info($"Player: {Player.name},Target: {target.name}, count: {usecount}", "TeleportKiller");
        _ = new LateTask(() =>
        {
            if (!target.IsAlive() && TeleportKillerDokkaaaan)
            {
                Logger.Info($"ターゲットが生きてないから自爆☆ Killer:{Player.name} Target:{target.name}", "TeleportKiller");
                MyState.DeathReason = CustomDeathReason.Bombed;
                Player.RpcMurderPlayer(Player);
                return;
            }
            if (!TPCheck(target, true))
            {
                Logger.Info($"ターゲットはキル可能な状態ではないためキルがブロックされました Killer:{Player.name} Target:{target.name}", "TeleportKiller");
                Player.RpcProtectedMurderPlayer();
                if ((target.inVent || target.MyPhysics.Animations.IsPlayingEnterVentAnimation())
                        && TeleportKillerVentgaaa)
                {
                    Player.RpcSnapToForced(target.transform.position);
                    MyState.DeathReason = CustomDeathReason.Bombed;
                    Player.RpcMurderPlayer(Player, true);
                    Logger.Info($"ターゲットがベントに入ってたせいでTPした時ベントに体があああ(自爆) Killer:{Player.name} Target:{target.name}", "TeleportKiller");
                    return;
                }
                if (!target.IsAlive()) return;
                Logger.Info($"キル待機中", "TeleportKiller");
                TeleportandKill.Add(target.PlayerId);
            }
            else
            {
                TeleportKill(Player, target);
            }
        }, 1.5f, "TeleportKiller-1");
    }
    public static bool TPCheck(PlayerControl target, bool KillerTP = false)
    {
        if (target.MyPhysics.Animations.IsPlayingAnyLadderAnimation())
        {
            if (!KillerTP) return false;
            return TeleportKillerLadderFall;
        }

        if (target.inMovingPlat)
        {
            if (!KillerTP) return false;
            return TeleportKillerPlatformFall;
        }

        if (target.MyPhysics.Animations.IsPlayingEnterVentAnimation()
                || target.inVent)
        {
            if (!KillerTP) return false;
            return !TeleportKillerVentgaaa;
        }

        if (!target.IsAlive()) return false;

        return true;
    }

    public void TeleportKill(PlayerControl Player, PlayerControl target)
    {
        if (target.Is(CustomRoles.King))
        {
            MyState.DeathReason = CustomDeathReason.Bombed;
            Player.RpcMurderPlayer(Player, true);
            Logger.Info($"この我を殺そうなど無謀な。ガッハッハ Killer:{Player.name} Target:{target.name}", "TeleportKiller");
            return;
        }
        //キラーのTP
        var position = Player.transform.position;
        var check = TPCheck(target);
        if ((target.inVent || target.MyPhysics.Animations.IsPlayingEnterVentAnimation()) && !TeleportKillerVentgaaa)
        {
            target.MyPhysics.RpcBootFromVent(CheckVentD[target.PlayerId]);
            Logger.Info($"ベントでもキルするのだ", "TeleportKiller");
            _ = new LateTask(() => TeleportandKill.Add(target.PlayerId), 1.5f);
            check = false;
        }
        else
        {
            Player.RpcSnapToForced(target.transform.position);
        }
        if (check)
        {
            //ターゲットのTP
            target.RpcSnapToForced(position);
            _ = new LateTask(() =>
            {
                if (!target.inVent && !target.MyPhysics.Animations.IsPlayingEnterVentAnimation())
                {
                    if (target.GetCustomRole().IsImpostor()) return;
                    if (CustomRoleManager.OnCheckMurder(Player, target, target, target, true, false, deathReason: DeathReason ? CustomDeathReason.TeleportKill : CustomDeathReason.Kill))
                    {
                        var state = PlayerState.GetByPlayerId(target.PlayerId);
                        state.SetDead();
                        target.SetRealKiller(Player);
                    }
                    if (TeleportKillerKillCooldownReset) Player.SetKillCooldown(KillCooldown);
                }
            }, 0.5f, "TeleportKiller-2");
        }

        if (target.MyPhysics.Animations.IsPlayingAnyLadderAnimation() || target.inMovingPlat)
        {
            var start = Player.transform.position;
            var goal = target.inMovingPlat ? (Vector2)Player.transform.position - new Vector2(0, 4) : new Vector2(Player.transform.position.x, LadderPatch.Ladder[target.PlayerId].y);
            var t = 0.0f;
            AnimationData = (start, goal, t);
            isAnimation = true;
        }
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (TeleportandKill.Count != 0)
        {
            foreach (var targetid in TeleportandKill)
            {
                var target = PlayerCatch.GetPlayerById(targetid);
                if (TPCheck(target))
                {
                    TeleportKill(Player, target);
                    TeleportandKill.Remove(targetid);
                }
            }
        }
        if (isAnimation)
        {
            var (start, goal, t) = AnimationData;
            t += Time.deltaTime / 2.0f;
            AnimationData.Item3 = (t > 1.0f) ? 1.0f : t; // 上限は1.0
            Player.RpcSnapToForced(Vector2.Lerp(start, goal, t));
            if (t >= 1)
            {
                isAnimation = false;
                MyState.DeathReason = CustomDeathReason.Fall;
                Player.RpcMurderPlayer(Player, true);
                Logger.Info($"TPした先には足場がぁ!? うあああ落ちるうう(落下死) Killer:{Player.name}", "TeleportKiller");
            }
        }
    }

    public static bool OnEnterVentOthers(PlayerPhysics physics, int id)
    {
        CheckVentD[physics.myPlayer.PlayerId] = id;
        return true;
    }
    public override string GetProgressText(bool comms = false, bool gamelog = false) => Maximum == 0 ? "" : Utils.ColorString(Maximum >= usecount ? Color.red : Color.gray, $"({Maximum - usecount})");

    public float CalculateKillCooldown() => KillCooldown;

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterCooldown = Cooldown;
        //AURoleOptions.ShapeshifterLeaveSkin = LeaveSkin;
        AURoleOptions.ShapeshifterDuration = Duration;
        AURoleOptions.KillCooldown = KillCooldown;
    }

    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.ClimbLadder))]
    class LadderPatch
    {
        public static Dictionary<byte, Vector2> Ladder = new();
        public static void Postfix(PlayerPhysics __instance, Ladder source, byte climbLadderSid)
        {
            var sourcePos = source.transform.position;
            var targetPos = source.Destination.transform.position;
            if (sourcePos.y > targetPos.y)
                Ladder[__instance.myPlayer.PlayerId] = targetPos;
            else
                Ladder[__instance.myPlayer.PlayerId] = sourcePos;
        }
    }
    public override string GetAbilityButtonText() => GetString(StringNames.ShapeshiftAbility);
    public override bool OverrideAbilityButton(out string text)
    {
        text = "TeleportKiller_Ability";
        return true;
    }
}