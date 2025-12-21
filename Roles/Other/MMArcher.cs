using AmongUs.GameOptions;
using UnityEngine;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using System.Collections.Generic;
using System.Linq;
using Hazel;

namespace TownOfHost.Roles;

public sealed class MMArcher : RoleBase, IKiller, IUsePhantomButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(MMArcher),
            player => new MMArcher(player),
            CustomRoles.MMArcher,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Crewmate,
            24000,
            SetUpOptionItem,
            "MMar",
            "#30b6ef",
            OptionSort: (0, 0),
            isDesyncImpostor: true,
            introSound: () => GetIntroSound(RoleTypes.Shapeshifter)
        );
    public MMArcher(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
    }
    Vector2 ArrowPosition; Vector2 ArrowLastPos; Vector2 PlayerPosition;
    bool IsUseing; float timer;
    bool IsSetting;
    public bool IsPromotioned;

    static OptionItem OptionCoolDown; static float Cooldown;//クールダウン
    static OptionItem OptionLostArrowtimer; static float LostArrowtimer;//矢が止まるまでの時間
    public static OptionItem OptionArrowTime; public int? Arrowtime;//発射可能回数∞ならnull
    static OptionItem OptionArrowTimeForTaskend;
    static OptionItem OptionArrowSpeed; static int ArrowSpeedValue;//矢の速さ
    static OptionItem OptionArrowMiss;
    // 0 =>両社死亡 | 1=>自身のみ死亡 | 2 => 相手のみ死亡

    enum OptionName
    {
        ArcherArrowTime, ArcherLostArrowtimer, ArcherArrowSpeed,
        MMArcherArrowMiss, MMArcherArrowTimeForTaskend
    }
    static void SetUpOptionItem()
    {
        string[] values = ["MMArcher_Miss0", "MMArcher_Miss1", "MMArcher_Miss2"];
        OptionCoolDown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.Cooldown, OptionBaseCoolTime, 10, false).SetValueFormat(OptionFormat.Seconds);
        OptionLostArrowtimer = FloatOptionItem.Create(RoleInfo, 11, OptionName.ArcherLostArrowtimer, new(0.5f, 10, 0.1f), 3f, false).SetValueFormat(OptionFormat.Seconds);
        OptionArrowTime = IntegerOptionItem.Create(RoleInfo, 12, OptionName.ArcherArrowTime, new(0, 99, 1), 0, false).SetZeroNotation(OptionZeroNotation.Infinity);
        OptionArrowTimeForTaskend = IntegerOptionItem.Create(RoleInfo, 13, OptionName.MMArcherArrowTimeForTaskend, new(0, 99, 1), 3, false).SetZeroNotation(OptionZeroNotation.Infinity);
        OptionArrowSpeed = FloatOptionItem.Create(RoleInfo, 14, OptionName.ArcherArrowSpeed, new(0.5f, 10, 0.25f), 1f, false);
        OptionArrowMiss = StringOptionItem.Create(RoleInfo, 15, OptionName.MMArcherArrowMiss, values, 0, false);
    }
    public override void Add()
    {
        ArrowPosition = Vector2.zero;
        PlayerPosition = Vector2.zero;
        ArrowLastPos = Vector2.zero;
        IsUseing = false;
        IsPromotioned = false;
        IsSetting = false;
        timer = 0;

        Cooldown = OptionCoolDown.GetFloat();
        Arrowtime = OptionArrowTime.GetInt() is 0 ? null : OptionArrowTime.GetInt();
        if (MurderMystery.TaskArchers.Contains(Player.PlayerId))
        {
            Arrowtime = OptionArrowTimeForTaskend.GetInt() is 0 ? null : OptionArrowTimeForTaskend.GetInt();
            IsPromotioned = true;//タスク完了保安官は引き続きを行わない。
        }
        LostArrowtimer = OptionLostArrowtimer.GetFloat();
        ArrowSpeedValue = OptionArrowSpeed.GetValue() + 2;//0.25 * Value
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(false);
        AURoleOptions.PhantomCooldown = Cooldown;
    }
    bool IKiller.CanUseKillButton() => false;
    bool IUsePhantomButton.IsPhantomRole => Arrowtime is not 0;
    void IUsePhantomButton.OnClick(ref bool AdjustKillCooldown, ref bool? ResetCooldown)
    {
        AdjustKillCooldown = true;
        ResetCooldown = true;

        if (IsUseing || !Player.IsAlive()) return;
        if (IsSetting)
        {
            ResetCooldown = true;
            IsSetting = false;
            timer = 0;

            var dir = (Player.GetTruePosition() - PlayerPosition).normalized;
            ArrowPosition = dir;

            while (ArrowPosition.x + ArrowPosition.y > 0.4f || ArrowPosition.x + ArrowPosition.y < -0.4f
            || ArrowPosition.x > 0.15f || ArrowPosition.x < -0.15f
            || ArrowPosition.y > 0.15f || ArrowPosition.y < -0.15f)
            {
                ArrowPosition *= 0.9f;
            }
            ArrowPosition *= -1;
            ArrowLastPos = PlayerPosition + new Vector2(0, 0.3f);
            IsUseing = true;
            SendRpc();
            UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);
            Player.SetKillCooldown(force: true);
            return;
        }
        if (Arrowtime is 0) return;

        ResetCooldown = false;
        IsSetting = true;
        PlayerPosition = Player.GetTruePosition();
        if (Arrowtime.HasValue)
        {
            Arrowtime--;
            SendRpc();
        }
        UtilsNotifyRoles.NotifyRoles();
    }

    public override void OnFixedUpdate(PlayerControl player)
    {
        if (AmongUsClient.Instance.AmHost is false || (!IsUseing && !IsSetting) || !Player.IsAlive()) return;

        if (IsSetting)
        {
            timer += Time.fixedDeltaTime;

            if (timer > 5f)
            {
                IsSetting = false;
                timer = 0;

                var dir = (Player.GetTruePosition() - PlayerPosition).normalized;
                ArrowPosition = dir;

                while (ArrowPosition.x + ArrowPosition.y > 0.4f || ArrowPosition.x + ArrowPosition.y < -0.4f
                || ArrowPosition.x > 0.15f || ArrowPosition.x < -0.15f
                || ArrowPosition.y > 0.15f || ArrowPosition.y < -0.15f)
                {
                    ArrowPosition *= 0.9f;
                }
                ArrowPosition *= -1;
                ArrowLastPos = PlayerPosition;
                IsUseing = true;
                SendRpc();
                UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);
                Player.SetKillCooldown(force: true);
            }
            return;
        }
        if (IsUseing)
        {
            timer += Time.fixedDeltaTime;
            if (timer <= LostArrowtimer)
            {
                for (var i = 0; i <= ArrowSpeedValue; i++)
                {
                    if (CheckTargetAndTeleport(i) is false) break;
                }
            }
            else
            {
                Reset();
                UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);
            }
        }
    }
    bool IsShipRoom(int i = 0, bool IsTp = false)
    {
        var nextpos = ArrowLastPos + ArrowPosition;
        var last = PlayerPosition;
        var vector = nextpos - last;
        float dis = vector.magnitude;
        if (IsTp) dis = Mathf.Clamp(dis + 2f, 0.01f, 99);
        if (PhysicsHelpers.AnyNonTriggersBetween(last, vector.normalized, dis, Constants.ShipAndAllObjectsMask)) return false;
        if (PhysicsHelpers.AnyNonTriggersBetween(last, vector.normalized, dis, Constants.ShadowMask)) return false;

        return true;
    }
    bool CheckTargetAndTeleport(int i = 0)
    {
        ArrowLastPos = ArrowLastPos + (ArrowPosition * 0.25f);
        if (IsShipRoom(i) is false)
        {
            Reset();
            UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);
            return false;
        }
        {
            Dictionary<byte, float> distances = new();
            foreach (var target in PlayerCatch.AllAlivePlayerControls)
            {
                if (target.PlayerId == Player.PlayerId) continue;
                float Distance = Vector2.Distance(ArrowLastPos, target.transform.position);
                if (Distance <= 0.6f)
                {
                    distances.Add(target.PlayerId, Distance);
                }
            }
            if (distances.Count <= 0) return true;
            var nearplayerId = distances.OrderBy(x => x.Value).First().Key;
            var nearplayer = PlayerCatch.GetPlayerById(nearplayerId);

            if (nearplayer.GetCustomRole().IsCrewmate())
            {
                if (OptionArrowMiss.GetValue() is 1)
                {
                    Player.RpcMurderPlayerV2(Player);
                    MyState.DeathReason = CustomDeathReason.Misfire;
                    return false;
                }
            }
            if (CustomRoleManager.OnCheckMurder(Player, nearplayer, nearplayer, nearplayer, true, Killpower: 1))
            {
                PlayerState.GetByPlayerId(nearplayerId).DeathReason = CustomDeathReason.Hit;
                if (Player.IsModClient()) RPC.PlaySoundRPC(Player.PlayerId, Sounds.KillSound);
                else Player.KillFlash();

                if (nearplayer.GetCustomRole().IsCrewmate())
                {
                    if (OptionArrowMiss.GetValue() is 0)
                    {
                        Player.RpcMurderPlayerV2(Player);
                        MyState.DeathReason = CustomDeathReason.Misfire;
                        return false;
                    }
                }
            }
            Reset();
            UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);
            Player.RpcResetAbilityCooldown();
        }
        return true;
    }
    void Reset()
    {
        ArrowPosition = Vector2.zero;
        PlayerPosition = Vector2.zero;
        IsUseing = false;
        IsSetting = false;
        timer = 0;
        SendRpc();
    }

    void SendRpc()
    {
        using var sender = CreateSender();
        sender.Writer.Write(IsUseing);
        sender.Writer.Write(IsSetting);
        sender.Writer.Write(Arrowtime is null ? -1 : Arrowtime.Value);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        IsUseing = reader.ReadBoolean();
        IsSetting = reader.ReadBoolean();
        var time = reader.ReadInt32();
        Arrowtime = time is -1 ? null : time;
    }

    public override string GetProgressText(bool comms = false, bool GameLog = false) => Arrowtime is null ? "" : $"<#{(Arrowtime is 0 ? "ff1919" : "cccccc")}> ({Arrowtime.Value})";

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (!Player.IsAlive() || isForMeeting || Arrowtime is 0) return "";

        if (IsUseing) return $"{(isForHud ? "" : "<size=60%>")}<#8cffff>{GetString("ArcherLower_ArrowActive")}</size>\n";
        return $"{(isForHud ? "" : "<size=60%>")}<#8cffff>{(IsSetting ? GetString("ArcherLower_SetBow") : GetString("ArcherLower_Phantom"))}</color></size>\n";
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        Reset();
    }
    public override string GetAbilityButtonText() => GetString("Archer_AbilityButton");
    public override bool OverrideAbilityButton(out string text)
    {
        text = "Archer_Ability";
        return true;
    }

    public bool CanUseSabotageButton() => false;
    bool IKiller.CanUseImpostorVentButton() => false;
}

