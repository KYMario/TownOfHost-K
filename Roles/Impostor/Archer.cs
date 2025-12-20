using AmongUs.GameOptions;
using UnityEngine;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using System.Collections.Generic;
using System.Linq;
using Hazel;
using TownOfHost.Modules;

namespace TownOfHost.Roles.Impostor;

public sealed class Archer : RoleBase, IImpostor, IUsePhantomButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Archer),
            player => new Archer(player),
            CustomRoles.Archer,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Impostor,
            23800,
            SetUpOptionItem,
            "ar",
            OptionSort: (3, 11),
            introSound: () => GetIntroSound(RoleTypes.Shapeshifter)
        );
    public Archer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Cooldown = OptionCoolDown.GetFloat();
        Arrowtime = OptionArrowTime.GetInt() is 0 ? null : OptionArrowTime.GetInt();
        IsMyArrow = OptionMyArrow.GetBool();
        LostArrowtimer = OptionLostArrowtimer.GetFloat();
        IsCanUseKill = OptionCanNomalKill.GetBool();
        IsFriendlyFire = OptionFriendlyFire.GetBool();
        ArrowSpeedValue = OptionArrowSpeed.GetValue() + 2;//0.25 * Value

        ArrowPosition = Vector2.zero;
        PlayerPosition = Vector2.zero;
        ArrowLastPos = Vector2.zero;
        IsUseing = false;
        IsSetting = false;
        timer = 0;
        Teleporttimer = 0;
    }
    Vector2 ArrowPosition; Vector2 ArrowLastPos; Vector2 PlayerPosition;
    bool IsUseing; float timer; float Teleporttimer;
    bool IsSetting; float PlayerSpeed;

    static OptionItem OptionCoolDown; static float Cooldown;//クールダウン
    static OptionItem OptionLostArrowtimer; static float LostArrowtimer;//矢が止まるまでの時間
    static OptionItem OptionArrowTime; int? Arrowtime;//発射可能回数∞ならnull
    static OptionItem OptionMyArrow; static bool IsMyArrow;//自身が矢として飛ぶか
    static OptionItem OptionCanNomalKill; static bool IsCanUseKill;//矢が残ってる時にキルが行えるか
    static OptionItem OptionFriendlyFire; static bool IsFriendlyFire;
    static OptionItem OptionArrowSpeed; static int ArrowSpeedValue;//矢の速さ

    enum OptionName
    {
        ArcherArrowTime, ArcherMyArrow, ArcherCanNomalKill, ArcherLostArrowtimer, SniperFriendlyFire, ArcherArrowSpeed
    }
    public override void Add() => PlayerSpeed = Main.AllPlayerSpeed[Player.PlayerId];
    static void SetUpOptionItem()
    {
        OptionCoolDown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.Cooldown, OptionBaseCoolTime, 35, false).SetValueFormat(OptionFormat.Seconds);
        OptionLostArrowtimer = FloatOptionItem.Create(RoleInfo, 13, OptionName.ArcherLostArrowtimer, new(0.5f, 10, 0.1f), 3f, false).SetValueFormat(OptionFormat.Seconds);
        OptionArrowTime = IntegerOptionItem.Create(RoleInfo, 11, OptionName.ArcherArrowTime, new(0, 99, 1), 3, false).SetZeroNotation(OptionZeroNotation.Infinity);
        OptionArrowSpeed = FloatOptionItem.Create(RoleInfo, 16, OptionName.ArcherArrowSpeed, new(0.5f, 10, 0.25f), 1f, false);
        OptionMyArrow = BooleanOptionItem.Create(RoleInfo, 12, OptionName.ArcherMyArrow, false, false);
        OptionCanNomalKill = BooleanOptionItem.Create(RoleInfo, 14, OptionName.ArcherCanNomalKill, false, false);
        OptionFriendlyFire = BooleanOptionItem.Create(RoleInfo, 15, OptionName.SniperFriendlyFire, true, false);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.PhantomCooldown = Cooldown;
    }
    bool IKiller.CanUseKillButton() => IsCanUseKill || Arrowtime is 0;
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
            if (IsMyArrow)
            {
                Main.AllPlayerSpeed[Player.PlayerId] = Main.MinSpeed;
                Player.MarkDirtySettings();
            }
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
                if (IsMyArrow)
                {
                    Main.AllPlayerSpeed[player.PlayerId] = Main.MinSpeed;
                    Player.MarkDirtySettings();
                }
                UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);
                Player.SetKillCooldown(force: true);
            }
            return;
        }
        if (IsUseing)
        {
            timer += Time.fixedDeltaTime;
            Teleporttimer += Time.fixedDeltaTime;
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
        if (IsMyArrow)
        {
            if (Teleporttimer > 0.1 && IsShipRoom(i, true))
            {
                Player.RpcSnapToForced(ArrowLastPos + new Vector2(0, 0.1f));
                Teleporttimer = 0;
            }
        }
        {
            Dictionary<byte, float> distances = new();
            foreach (var target in PlayerCatch.AllAlivePlayerControls)
            {
                if (target.PlayerId == Player.PlayerId) continue;
                if (!IsFriendlyFire && target.GetCustomRole().IsImpostor() && !SuddenDeathMode.NowSuddenDeathMode) continue;
                if (!IsFriendlyFire && SuddenDeathMode.NowSuddenDeathTemeMode && SuddenDeathMode.IsSameteam(target.PlayerId, Player.PlayerId)) continue;
                float Distance = Vector2.Distance(ArrowLastPos, target.transform.position);
                if (Distance <= 0.6f)
                {
                    distances.Add(target.PlayerId, Distance);
                }
            }
            if (distances.Count <= 0) return true;
            var nearplayerId = distances.OrderBy(x => x.Value).First().Key;
            var nearplayer = PlayerCatch.GetPlayerById(nearplayerId);
            if (CustomRoleManager.OnCheckMurder(Player, nearplayer, nearplayer, nearplayer, true, Killpower: 1))
            {
                PlayerState.GetByPlayerId(nearplayerId).DeathReason = CustomDeathReason.Hit;
                if (Player.IsModClient()) RPC.PlaySoundRPC(Player.PlayerId, Sounds.KillSound);
                else Player.KillFlash();
            }
            if (IsMyArrow && IsShipRoom(i, true))
                Player.RpcSnapToForced(ArrowLastPos + new Vector2(0, 0.1f));
            Reset();
            if (IsMyArrow)
            {
                Main.AllPlayerSpeed[Player.PlayerId] = PlayerSpeed;
                Player.MarkDirtySettings();
            }
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
        Teleporttimer = 0;
        SendRpc();
        Main.AllPlayerSpeed[Player.PlayerId] = PlayerSpeed;
        Player.MarkDirtySettings();
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

        if (IsUseing) return $"{(isForHud ? "" : "<size=60%>")}<#ff1919>{GetString("ArcherLower_ArrowActive")}";
        return $"{(isForHud ? "" : "<size=60%>")}<#ff1919>{(IsSetting ? GetString("ArcherLower_SetBow") : GetString("ArcherLower_Phantom"))}</color>";
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        Main.AllPlayerSpeed[Player.PlayerId] = PlayerSpeed;
        Reset();
    }
    public override string GetAbilityButtonText() => GetString("Archer_AbilityButton");
    public override bool OverrideAbilityButton(out string text)
    {
        text = "Archer_Ability";
        return true;
    }
}

