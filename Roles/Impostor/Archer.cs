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

        ArrowPosition = Vector2.zero;
        PlayerPosition = Vector2.zero;
        ArrowLastPos = Vector2.zero;
        IsUseing = false;
        IsSetting = false;
        timer = 0;
        fixtimer = 0;
    }
    Vector2 ArrowPosition; Vector2 ArrowLastPos; Vector2 PlayerPosition;
    bool IsUseing; float timer; float fixtimer;
    bool IsSetting; float speed;

    static OptionItem OptionCoolDown; static float Cooldown;//クールダウン
    static OptionItem OptionLostArrowtimer; static float LostArrowtimer;//矢が止まるまでの時間
    static OptionItem OptionArrowTime; int? Arrowtime;//発射可能回数∞ならnull
    static OptionItem OptionMyArrow; static bool IsMyArrow;//自身が矢として飛ぶか
    static OptionItem OptionCanNomalKill; static bool IsCanUseKill;//矢が残ってる時にキルが行えるか
    static OptionItem OptionFriendlyFire; static bool IsFriendlyFire;

    enum OptionName
    {
        ArcherArrowTime, ArcherMyArrow, ArcherCanNomalKill, ArcherLostArrowtimer, SniperFriendlyFire
    }
    public override void Add() => speed = Main.AllPlayerSpeed[Player.PlayerId];
    static void SetUpOptionItem()
    {
        OptionCoolDown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.Cooldown, OptionBaseCoolTime, 35, false).SetValueFormat(OptionFormat.Seconds);
        OptionLostArrowtimer = FloatOptionItem.Create(RoleInfo, 13, OptionName.ArcherLostArrowtimer, new(1, 180, 1), 5, false).SetValueFormat(OptionFormat.Seconds);
        OptionArrowTime = IntegerOptionItem.Create(RoleInfo, 11, OptionName.ArcherArrowTime, new(0, 99, 1), 3, false).SetZeroNotation(OptionZeroNotation.Infinity);
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

        if (IsUseing || !Player.IsAlive() || IsSetting) return;
        if (Arrowtime is 0) return;

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

            if (timer > 1.2f)
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
                ArrowLastPos = PlayerPosition + new Vector2(0, 0.3f);
                IsUseing = true;
                SendRpc();
                Main.AllPlayerSpeed[player.PlayerId] = Main.MinSpeed;
                Player.MarkDirtySettings();
                UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);
            }
            return;
        }
        if (IsUseing)
        {
            timer += Time.fixedDeltaTime;
            fixtimer += Time.fixedDeltaTime;
            if (IsShipRoom() && timer <= LostArrowtimer)
            {
                ArrowLastPos = ArrowLastPos + ArrowPosition;
                if (IsMyArrow)
                {
                    if (fixtimer > 0.2)
                    {
                        Player.RpcSnapToForced(ArrowLastPos);
                        fixtimer = 0;
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
                        if (Distance <= 1.22f)
                        {
                            distances.Add(target.PlayerId, Distance);
                        }
                    }
                    if (distances.Count <= 0) return;
                    var nearplayerId = distances.OrderBy(x => x.Value).First().Key;
                    var nearplayer = PlayerCatch.GetPlayerById(nearplayerId);
                    if (CustomRoleManager.OnCheckMurder(player, nearplayer, nearplayer, nearplayer, true, Killpower: 1))
                    {
                        PlayerState.GetByPlayerId(nearplayerId).DeathReason = CustomDeathReason.Hit;
                    }
                    Player.RpcSnapToForced(ArrowLastPos);
                    Reset();
                    Main.AllPlayerSpeed[player.PlayerId] = speed;
                    Player.MarkDirtySettings();
                    UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);
                }
            }
            else
            {
                Reset();
                UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);
            }
        }
    }
    bool IsShipRoom()
    {
        var nextpos = ArrowLastPos + ArrowPosition;
        var last = ArrowLastPos - (ArrowPosition * 2);
        var vector = nextpos - last;
        if (PhysicsHelpers.AnyNonTriggersBetween(last, vector.normalized, vector.magnitude, Constants.ShipAndAllObjectsMask)) return false;
        return true;
    }
    void Reset()
    {
        ArrowPosition = Vector2.zero;
        PlayerPosition = Vector2.zero;
        IsUseing = false;
        IsSetting = false;
        timer = 0;
        fixtimer = 0;
        SendRpc();
        Main.AllPlayerSpeed[Player.PlayerId] = speed;
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
        if (!Player.IsAlive() || isForMeeting || Arrowtime is 0 || IsUseing) return "";

        return $"{(isForHud ? "" : "<size=60%>")}<#ff1919>{(IsSetting ? GetString("ArcherLower_SetBow") : GetString("ArcherLower_Phantom"))}</color>";
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        Main.AllPlayerSpeed[Player.PlayerId] = speed;
        Reset();
    }
}

