using System.Collections.Generic;
using AmongUs.GameOptions;
using UnityEngine;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using Hazel;

namespace TownOfHost.Roles.Neutral;

public sealed class CurseMaker : RoleBase, IKiller, IUsePhantomButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(CurseMaker),
            player => new CurseMaker(player),
            CustomRoles.CurseMaker,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Neutral,
            14100,
            SetupOptionItem,
            "Cm",
            "#554d59",
            (3, 3),
            true,
            introSound: () => GetIntroSound(RoleTypes.Phantom)
        );
    public CurseMaker(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        distance = Distance.GetFloat();
        noroitime = NoroiTime.GetFloat();
        delTurn = OptionDelTurn.GetInt();
        KillCooldown = OptionKillCoolDown.GetFloat();
        shapcool = OptionShepeCooldown.GetFloat();

        CursedPlayers.Clear();
        CanWin = false;
        fall = false;

        TargetInfo = null;
    }
    static OptionItem Distance; static float distance;
    static OptionItem NoroiTime; static float noroitime;
    static OptionItem OptionDelTurn; static int delTurn;
    static OptionItem OptionKillCoolDown; static float KillCooldown;
    static OptionItem OptionShepeCooldown; static float shapcool;

    static Dictionary<byte, int> CursedPlayers = new();
    public class TimerInfo
    {
        public byte TargetId;
        public float Timer;
        public TimerInfo(byte targetId, float timer)
        {
            TargetId = targetId;
            Timer = timer;
        }
    }
    public bool CanKill { get; private set; } = false;
    private TimerInfo TargetInfo;
    public bool CanWin;
    bool fall;
    public static HashSet<CurseMaker> curseMakers = new();
    enum OptionName
    {
        CueseMakerDelTurn,
        CueseMakerNoroiTime,
        CueseMakerDicstance
    }
    static void SetupOptionItem()
    {
        SoloWinOption.Create(RoleInfo, 9, defo: 15);
        OptionKillCoolDown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 0.5f), 10f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionShepeCooldown = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.Cooldown, new(0f, 180f, 0.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionDelTurn = IntegerOptionItem.Create(RoleInfo, 12, OptionName.CueseMakerDelTurn, new(1, 30, 1), 4, false);
        NoroiTime = FloatOptionItem.Create(RoleInfo, 13, OptionName.CueseMakerNoroiTime, new(0f, 30f, 0.5f), 3f, false)
                .SetValueFormat(OptionFormat.Seconds);
        Distance = FloatOptionItem.Create(RoleInfo, 14, OptionName.CueseMakerDicstance, new(1f, 30f, 0.25f), 1.75f, false);
        OverrideKilldistance.Create(RoleInfo, 15);
    }
    public override void Add()
    {
        curseMakers.Add(this);
    }
    public override void OnDestroy()
    {
        curseMakers.Clear();
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        fall = false;
        var (killer, target) = info.AttemptTuple;
        info.DoKill = false;
        if (CursedPlayers.ContainsKey(target.PlayerId) || TargetInfo != null) return;

        TargetInfo = new(target.PlayerId, 0f);
        RpcSetTarget(target.PlayerId);
        Player.SetKillCooldown(target: target, delay: true);
        _ = new LateTask(() => UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player), 0.4f, "CueseMaker");
    }

    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        if (GameStates.IsInTask && TargetInfo != null)
        {
            var cu_target = PlayerCatch.GetPlayerById(TargetInfo.TargetId);
            var cu_time = TargetInfo.Timer;
            if (!cu_target.IsAlive())
            {
                fall = true;
                Player.SetKillCooldown();
                TargetInfo = null;
                RpcSetTarget(byte.MaxValue);
            }
            else if (noroitime <= cu_time)
            {
                fall = false;
                Player.SetKillCooldown();
                TargetInfo = null;
                CursedPlayers.Add(cu_target.PlayerId, 0);
                RpcSetTarget(byte.MaxValue);
                RpcCursePlayer(cu_target.PlayerId);
                UtilsNotifyRoles.NotifyRoles();
            }
            else
            {
                float dis;
                dis = Vector2.Distance(Player.transform.position, cu_target.transform.position);
                if (dis <= distance)
                {
                    TargetInfo.Timer += Time.fixedDeltaTime;
                }
                else
                {
                    TargetInfo = null;
                    fall = true;
                    Player.SetKillCooldown();
                    UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player);

                    RpcSetTarget(byte.MaxValue);
                    Logger.Info($"Canceled: {Player.GetNameWithRole().RemoveHtmlTags()}", "CurseMaker");
                }
            }
        }
    }
    public override void OnReportDeadBody(PlayerControl a, NetworkedPlayerInfo target)
    {
        TargetInfo = null;
        CanWin = false;
        if (CursedPlayers.Count == 0) return;
        List<byte> DelList = new();
        foreach (var nr in CursedPlayers)
        {
            var np = PlayerCatch.GetPlayerById(nr.Key);
            if (!np) DelList.Add(nr.Key);
            if (!np.IsAlive()) DelList.Add(nr.Key);
            if (delTurn <= nr.Value + 1) DelList.Add(nr.Key);

            CursedPlayers[nr.Key] = nr.Value + 1;
        }
        DelList.ForEach(task => { CursedPlayers.Remove(task); });
        RpcCheck();
    }
    public override string MeetingAddMessage()
    {
        if (CursedPlayers.Count == 0) return "";
        if (!Player.IsAlive()) return "";

        return string.Format(GetString("CurseMakerMeetingMeg"), CursedPlayers.Count);
    }
    public override bool NotifyRolesCheckOtherName => true;
    public bool CanUseKillButton() => true;
    public bool CanUseImpostorVentButton() => false;
    public bool CanUseSabotageButton() => false;
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.PhantomCooldown = shapcool;
        opt.SetVision(false);
    }
    public float CalculateKillCooldown() => fall ? 0.00000000001f : KillCooldown;
    public void OnClick(ref bool AdjustKillCooldown, ref bool? ResetCooldown)
    {
        ResetCooldown = false;
        if (!Player.IsAlive()) return;
        if (CursedPlayers.Count == 0) return;
        AdjustKillCooldown = false;
        ResetCooldown = true;

        CursedPlayers.Add(Player.PlayerId, 0);
        RpcCursePlayer(Player.PlayerId);
        foreach (var Cursedid in CursedPlayers)
        {
            var target = PlayerCatch.GetPlayerById(Cursedid.Key);
            var state = PlayerState.GetByPlayerId(Cursedid.Key);
            CustomRoleManager.OnCheckMurder(Player, target, target, target, true, true, 10, CustomDeathReason.Spell);
        }

        CanWin = true;
        _ = new LateTask(() => CanWin = false, 2f, "ResetCanWin");
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (CursedPlayers.ContainsKey(seen.PlayerId))
            return "<color=#554d59>†</color>";
        if (seen.PlayerId == (TargetInfo?.TargetId ?? byte.MaxValue))
            return "<color=#554d59>◇</color>";
        return "";
    }
    public bool OverrideKillButtonText(out string text)
    {
        text = GetString("WarlockCurseButtonText");
        return true;
    }
    public override string GetAbilityButtonText() => GetString("CurseMakerbooom");
    public bool OverrideKillButton(out string text)
    {
        text = "CurseMaker_Kill";
        return true;
    }
    public override bool OverrideAbilityButton(out string text)
    {
        text = "CurseMaker_Ability";
        return true;
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (seen.PlayerId != seer.PlayerId || isForMeeting || !Player.IsAlive()) return "";

        if (isForHud) return GetString("CurseMakerLowerText");
        return $"<size=50%>{GetString("CurseMakerLowerText")}</size>";
    }
    public static void CheckWin()
    {
        foreach (var pc in PlayerCatch.AllPlayerControls)
        {
            if (pc.GetRoleClass() is CurseMaker curseMaker)
            {
                if (curseMaker.CanWin)
                {
                    if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.CurseMaker, byte.MaxValue))
                    {
                        CustomWinnerHolder.WinnerIds.Add(curseMaker.Player.PlayerId);
                        CustomWinnerHolder.NeutralWinnerIds.Add(curseMaker.Player.PlayerId);
                    }
                }
            }
        }
    }

    public void RpcSetTarget(byte targetId)
    {
        using var sender = CreateSender();
        sender.Writer.WritePacked((int)RPC_Types.SetTarget);
        sender.Writer.Write(targetId);
    }

    public void RpcCursePlayer(byte targetId)
    {
        using var sender = CreateSender();
        sender.Writer.WritePacked((int)RPC_Types.CursePlayer);
        sender.Writer.Write(targetId);
    }

    public void RpcCheck()
    {
        using var sender = CreateSender();
        sender.Writer.WritePacked((int)RPC_Types.Check);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        switch ((RPC_Types)reader.ReadPackedInt32())
        {
            case RPC_Types.SetTarget:
                var targetId = reader.ReadByte();
                TargetInfo = targetId == byte.MaxValue ? null : new(targetId, 0f);
                break;
            case RPC_Types.CursePlayer:
                CursedPlayers.Add(reader.ReadByte(), 0);
                break;
            case RPC_Types.Check:
                OnReportDeadBody(null, null);
                break;
        }
    }

    enum RPC_Types
    {
        SetTarget,
        CursePlayer,
        Check
    }

}
