using AmongUs.GameOptions;
using UnityEngine;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Roles.Core.Interfaces.ISchrodingerCatOwner;
using Hazel;

namespace TownOfHost.Roles.Neutral;

public sealed class DoppelGanger : RoleBase, ILNKiller, ISchrodingerCatOwner, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(DoppelGanger),
            player => new DoppelGanger(player),
            CustomRoles.DoppelGanger,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Neutral,
            13400,
            SetupOptionItem,
            "dg",
            "#47266e",
            (2, 1),
            true,
            assignInfo: new RoleAssignInfo(CustomRoles.DoppelGanger, CustomRoleTypes.Neutral)
            {
                AssignCountRule = new(1, 1, 1)
            },
            Desc: () => string.Format(GetString("DoppelGangerDesc"), OptionShapeCountUp.GetFloat(), OptionAddWinCount.GetInt(), OptionSoloWinCount.GetInt())
            );
    public DoppelGanger(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCooldown = OptionKillCooldown.GetFloat();
        shapecountup = OptionShapeCountUp.GetFloat();
        Cankill = false;
        Target = byte.MaxValue;
        Afterkill = false;
        SecondsWin = false;
        Seconds = 0;
        Count = 0;
        win = false;
    }

    static OptionItem OptionKillCooldown;
    static OptionItem OptionShepeCoolDown;
    static OptionItem OptionAddWinCount;
    static OptionItem OptionSoloWinCount;
    static OptionItem OptionShapeCountUp;
    static float KillCooldown;
    static float shapecountup;
    bool Cankill;
    bool Afterkill;
    bool SecondsWin;
    float Seconds;
    int Count;
    byte Target;
    bool win;
    public TeamType SchrodingerCatChangeTo => TeamType.DoppelGanger;

    enum OptionName
    {
        DoppelGangerAddWinCount,
        DoppelGangerSoloWinCount,
        DoppelGangerShapeCountUp
    }

    private static void SetupOptionItem()
    {
        SoloWinOption.Create(RoleInfo, 9, defo: 1);
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 0.5f), 25f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionShepeCoolDown = FloatOptionItem.Create(RoleInfo, 12, GeneralOption.Cooldown, new(0f, 180f, 0.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionShapeCountUp = FloatOptionItem.Create(RoleInfo, 90, OptionName.DoppelGangerShapeCountUp, new(0, 0.9f, 0.1f), 0.1f, false);
        OptionAddWinCount = FloatOptionItem.Create(RoleInfo, 13, OptionName.DoppelGangerAddWinCount, new(0f, 300f, 1f), 45f, false);
        OptionSoloWinCount = FloatOptionItem.Create(RoleInfo, 14, OptionName.DoppelGangerSoloWinCount, new(0f, 300f, 1f), 70f, false);
        RoleAddAddons.Create(RoleInfo, 15);
    }
    public float CalculateKillCooldown() => KillCooldown;
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => false;
    public void ApplySchrodingerCatOptions(IGameOptions option)
    {
        option.SetVision(false);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterCooldown = OptionShepeCoolDown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 0f;
        AURoleOptions.ShapeshifterLeaveSkin = false;
    }

    public override bool CheckShapeshift(PlayerControl target, ref bool animate)
    {
        if (Is(target))
        {
            animate = false;
            return false;
        }
        Cankill = true;
        Target = target.PlayerId;
        SendRPC();
        _ = new LateTask(() => UtilsNotifyRoles.NotifyRoles(), 1f, "DoppelSetNotify", true);
        return true;
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        Player.RpcShapeshift(Player, false);
        Cankill = false;
        Target = byte.MaxValue;
        Afterkill = false;
        SendRPC();
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AppearanceTuple;

        if (Target == byte.MaxValue || target.PlayerId != Target || !Cankill || Afterkill)
        {
            info.DoKill = false;
            return;
        }
    }
    public void OnMurderPlayerAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AppearanceTuple;
        if (Target == byte.MaxValue || target.PlayerId != Target || !Cankill || Afterkill)
            return;

        if (info.CanKill && info.DoKill)
        {
            Afterkill = true;
            SendRPC();
        }
    }
    public override string GetProgressText(bool comms = false, bool GameLog = false)
    {
        if (GameLog)
        {
            return Utils.ColorString(Palette.Purple, $"({Count})");
        }
        return "";
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        seen ??= seer;
        if (seer == seen || seen.PlayerId == Target)
        {
            var AddDenominator = OptionAddWinCount.GetFloat();
            var SoloWinDenominator = OptionSoloWinCount.GetFloat();
            if (!Player.IsAlive()) return "";
            if (SecondsWin) return Utils.ColorString(Palette.Purple.ShadeColor(-0.5f), $"({Count}/{SoloWinDenominator}) {Utils.AdditionalWinnerMark}");
            else if (Target != byte.MaxValue)
                return Utils.ColorString(Palette.Purple.ShadeColor(-0.3f), $"({Count}/{AddDenominator})");
            else
                return Utils.ColorString(Palette.Purple.ShadeColor(-0.1f), $"({Count}/{AddDenominator})");
        }
        return "";
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!player.IsAlive()) return;
        var UseingShape = false;
        if (Afterkill)
        {
            UseingShape = true;
            Seconds += Time.fixedDeltaTime;
        }
        else
        if (Target != byte.MaxValue)
        {
            UseingShape = true;
            Seconds += Time.fixedDeltaTime * shapecountup;
        }

        if (UseingShape is false) return;

        if (OptionAddWinCount.GetFloat() <= Seconds && !SecondsWin)
        {
            SecondsWin = true;
            if (AmongUsClient.Instance.AmHost) SendRPC();//念のためホスト側から同期
        }
        if (OptionSoloWinCount.GetFloat() <= Seconds)
        {
            win = true;
            if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.DoppelGanger, Player.PlayerId, false))
            {
                CustomWinnerHolder.NeutralWinnerIds.Add(Player.PlayerId);
            }
            Cankill = false;
            Target = byte.MaxValue;
            Afterkill = false;
            return;
        }
        if (Count != (int)Seconds)
        {
            Count = (int)Seconds;
            UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player);
        }
    }

    public bool CheckWin(ref CustomRoles winnerRole) => Player.IsAlive() && SecondsWin && !win;

    public void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(Count);
        sender.Writer.Write(SecondsWin);
        sender.Writer.Write(Target);
        sender.Writer.Write(Afterkill);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        Count = reader.ReadInt32();
        SecondsWin = reader.ReadBoolean();
        Target = reader.ReadByte();
        Afterkill = reader.ReadBoolean();
    }
}