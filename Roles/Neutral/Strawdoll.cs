using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using Hazel;

namespace TownOfHost.Roles.Neutral;

public sealed class Strawdoll : RoleBase, IKiller, IUsePhantomButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Strawdoll),
            player => new Strawdoll(player),
            CustomRoles.Strawdoll,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Neutral,
            22900,
            SetupOptionItem,
            "St",
            "#7b4122",
            (7, 2),
            true,
            introSound: () => GetIntroSound(RoleTypes.Crewmate),
            from: From.None,
            Desc: () => string.Format(GetString("StrawdollDesc"), OptionWinKilledCount.GetInt(), OptionReprisalDistance.GetBool() ? GetString("StrawdollDescDistance") : ""
            , OptionTpToVent.GetBool() ? GetString("StrawdollDescVent") : "")
        );
    public Strawdoll(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        KillCooldown = OptionKillCoolDown.GetFloat();
        ShapeCooldown = OptionShapeCoolDown.GetFloat();
        CanUseVent = OptionCanUseVent.GetBool();
        WinKilledCount = OptionWinKilledCount.GetInt();
        IsTpToVent = OptionTpToVent.GetBool();
        reprisaldistance = OptionReprisalDistance.GetFloat();
        StopTime = OptionStopTime.GetFloat();

        Target = null;
        killedcount = 0;
        ShapePosition = Vector2.zero;
        Ventid = -1;
        IsShapeShift = false;
    }
    public bool CanKill { get; private set; } = false;
    static OptionItem OptionWinKilledCount; static int WinKilledCount;
    static OptionItem OptionKillCoolDown; static float KillCooldown;
    static OptionItem OptionShapeCoolDown; static float ShapeCooldown;
    static OptionItem OptionCanUseVent; static bool CanUseVent;
    static OptionItem OptionTpToVent; static bool IsTpToVent;
    static OptionItem OptionReprisalDistance; static float reprisaldistance;
    static OptionItem OptionStopTime; static float StopTime;

    PlayerControl Target;
    int killedcount;
    Vector2 ShapePosition;
    int Ventid;
    bool IsShapeShift;
    enum OptionName
    {
        StrawdollTargetCooldown,
        StrawdollShapeCooldown,
        StrawdollWinKilledCount,
        StrawdollTpToVent,
        StrawdollReprisaldistance,
        StrawdollStoptime
    }

    private static void SetupOptionItem()
    {
        SoloWinOption.Create(RoleInfo, 9, defo: 1);
        OptionWinKilledCount = IntegerOptionItem.Create(RoleInfo, 8, OptionName.StrawdollWinKilledCount, new(1, 10, 1), 3, false);
        OptionKillCoolDown = FloatOptionItem.Create(RoleInfo, 10, OptionName.StrawdollTargetCooldown, OptionBaseCoolTime, 10f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionShapeCoolDown = FloatOptionItem.Create(RoleInfo, 15, OptionName.StrawdollShapeCooldown, OptionBaseCoolTime, 10f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionCanUseVent = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.CanVent, false, false);
        OptionTpToVent = BooleanOptionItem.Create(RoleInfo, 12, OptionName.StrawdollTpToVent, false, false, OptionCanUseVent);
        OptionReprisalDistance = FloatOptionItem.Create(RoleInfo, 13, OptionName.StrawdollReprisaldistance, new(0, 3, 0.5f), 1f, false)
            .SetValueFormat(OptionFormat.Multiplier).SetZeroNotation(OptionZeroNotation.Off);
        OptionStopTime = FloatOptionItem.Create(RoleInfo, 14, OptionName.StrawdollStoptime, new(0, 10, 0.5f), 3, false)
            .SetValueFormat(OptionFormat.Seconds).SetZeroNotation(OptionZeroNotation.Off);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(false);
        AURoleOptions.PhantomCooldown = ShapeCooldown;
    }
    public bool CanUseKillButton() => Target is null;
    public bool CanUseImpostorVentButton() => CanUseVent;
    public bool CanUseSabotageButton() => false;
    public float CalculateKillCooldown() => KillCooldown;
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        bool IsSeenTarget = (seen?.PlayerId ?? 250) == (Target?.PlayerId ?? byte.MaxValue);
        if ((IsSeenTarget && IsShapeShift) || Is(seen))
            return Utils.ColorString(RoleInfo.RoleColor.ShadeColor(0.2f), $"({killedcount}/{WinKilledCount})");

        if (IsSeenTarget && !IsShapeShift) return $"<{RoleInfo.RoleColorCode}>★</color>";

        return "";
    }
    public override string GetProgressText(bool comms = false, bool GameLog = false) => GameLog ? Utils.ColorString(RoleInfo.RoleColor.ShadeColor(0.2f), $"({killedcount}/{WinKilledCount})") : "";
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (seen == seer)
        {
            var lowertext = Target == null ? GetString("StrawdollSetTarget") : GetString("StrawdollShape");
            return isForHud ? lowertext : $"<size=60%>{lowertext}</size>";
        }
        return "";
    }

    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        info.DoKill = false;
        var (killer, target) = info.AttemptTuple;

        if (Target == null)
        {
            Target = target;
            SendRPC();
            Player.SetKillCooldown(target: target, force: true, delay: true);
            UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player);
        }
    }
    void IUsePhantomButton.OnClick(ref bool AdjustKillCooldown, ref bool? ResetCooldown)
    {
        AdjustKillCooldown = false;
        ResetCooldown = false;
        if (IsShapeShift) return;

        if (Target == null)
        {
            AdjustKillCooldown = true;
            ResetCooldown = false;
            return;
        }
        IsShapeShift = true;
        SendRPC();
        Player.RpcShapeshift(Target, true);
        ShapePosition = Player.GetTruePosition();
        UtilsNotifyRoles.NotifyRoles(true, SpecifySeer: [Player]);
        if (IsTpToVent)
        {
            Dictionary<Vent, float> Distance = new();
            Vector2 position = Player.transform.position;
            foreach (var vent in ShipStatus.Instance.AllVents)
                Distance.Add(vent, Vector2.Distance(position, vent.transform.position));

            Ventid = Distance.OrderByDescending(x => x.Value).Last().Key.Id;
            ShapePosition = Distance.OrderByDescending(x => x.Value).Last().Key.transform.position + new Vector3(0, 0.1f);
        }
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (reprisaldistance is 0) return;
        if (AmongUsClient.Instance.AmHost is false || player.IsAlive() is false || Target.IsAlive() is false || IsShapeShift is false) return;

        var distance = Vector2.Distance(player.GetTruePosition(), Target.GetTruePosition());
        if (distance < reprisaldistance)
        {
            IsShapeShift = false;
            MyState.DeathReason = CustomDeathReason.Spell;
            Player.RpcMurderPlayer(Player);
            SendRPC();
        }
    }
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        var (killer, target) = info.AppearanceTuple;
        if (Target.IsAlive() && IsShapeShift && info.CheckHasGuard() is false)
        {
            if (CustomRoleManager.OnCheckMurder(killer, Target, Target, Target, true, true, 10))
            {
                Target.GetPlayerState().DeathReason = CustomDeathReason.Spell;
                Target = null;
                killedcount++;
                UtilsGameLog.AddGameLog("Strawdoll", string.Format(GetString("StrawdollKilledLog"), killedcount));
                if (WinKilledCount <= killedcount)
                {
                    if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Strawdoll, Player.PlayerId))
                    {
                        SendRPC();
                        return false;
                    }
                }
                IsShapeShift = false;
                SendRPC();
                if (StopTime > 0)
                {
                    var tmpSpeed = Main.AllPlayerSpeed[Player.PlayerId];
                    Main.AllPlayerSpeed[Player.PlayerId] = Main.MinSpeed;
                    Player.MarkDirtySettings();
                    _ = new LateTask(() =>
                    {
                        Main.AllPlayerSpeed[Player.PlayerId] = tmpSpeed;
                        Player.MarkDirtySettings();
                        RPC.PlaySoundRPC(Player.PlayerId, Sounds.TaskComplete);
                    }, StopTime, "Strawdoll Stopmove", null);
                }
                Player.RpcShapeshift(Player, false);
                Player.RpcSnapToForced(ShapePosition);
                if (IsTpToVent && Ventid > -1)
                {
                    Player.MyPhysics.RpcEnterVent(Ventid);
                    return false;
                }

                return false;
            }
            Target = null;
            SendRPC();
            UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player);
        }
        return true;
    }

    public override void OnStartMeeting()
    {
        Target = null;
        ShapePosition = Vector2.zero;
        Ventid = -1;
        IsShapeShift = false;
    }

    public override void OnLeftPlayer(PlayerControl player)
    {
        if (Target == null) return; //null対策
        if (player.PlayerId == Target.PlayerId && player?.PlayerId.GetPlayerState().IsDead == false)
        {
            Target = null;
            ShapePosition = Vector2.zero;
            Ventid = -1;
            IsShapeShift = false;

            if (!AmongUsClient.Instance.AmHost) return;

            //ターゲット切断時の仕様どうしましょう。
            Player.RpcShapeshift(Player, false);
            Player.RpcSnapToForced(ShapePosition);
            if (IsTpToVent && Ventid > -1)
            {
                Player.MyPhysics.RpcEnterVent(Ventid);
            }
            Player.SetKillCooldown(1);
        }
    }

    public void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(killedcount);
        sender.Writer.Write(IsShapeShift);
        sender.Writer.Write(Target?.PlayerId ?? byte.MaxValue);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        killedcount = reader.ReadInt32();
        IsShapeShift = reader.ReadBoolean();
        var targetId = reader.ReadByte();
        Target = targetId == byte.MaxValue ? null : PlayerCatch.GetPlayerById(targetId);
    }
}