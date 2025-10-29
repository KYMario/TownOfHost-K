using System.Collections.Generic;
using AmongUs.GameOptions;
using Hazel;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;

public sealed class EarnestWolf : RoleBase, IImpostor, IUsePhantomButton
{
    //Memo:回数0時無限にする
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(EarnestWolf),
            player => new EarnestWolf(player),
            CustomRoles.EarnestWolf,
            () => OptionOverKillCanCount.GetBool() ? RoleTypes.Phantom : RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            4600,
            SetupOptionItem,
            "EW",
            OptionSort: (3, 8)
        );
    public EarnestWolf(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCoolDown = OptionKillCoolDown.GetFloat();
        count = 0;
        OverKillMode = OptionOverKillCanCount.GetBool() is false;

        CantReport = OptionOverKillCantReport.GetBool();
        OverKillList = new();
    }
    static OptionItem OptionKillCoolDown;
    static OptionItem OptionOverKillCanCount;
    static OptionItem OptionOverKillBairitu;
    static OptionItem OptionNomalKillDistance;
    static OptionItem OptionOverKillDistance;
    static OptionItem OptionOverKillDontKillM;
    static OptionItem OptionOverKillCantReport; static bool CantReport;
    List<byte> OverKillList;
    float KillCoolDown;
    int count;
    bool OverKillMode;
    public bool CanBeLastImpostor { get; } = false;
    enum OptionName
    {
        EarnestWolfOverKillCount,
        EarnestWolfOverBairitu,
        EarnestWolfNomalKllDistance,
        EarnestWolfOverKillDistance,
        EarnestWolfOverKillDontKillM,
        EarnestWolfOverKillCantReport
    }

    static void SetupOptionItem()
    {
        OptionKillCoolDown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 0.5f), 25f, false).SetValueFormat(OptionFormat.Seconds);
        OptionOverKillCanCount = IntegerOptionItem.Create(RoleInfo, 11, OptionName.EarnestWolfOverKillCount, new(0, 15, 1), 2, false).SetZeroNotation(OptionZeroNotation.Infinity).SetValueFormat(OptionFormat.Times);
        OptionOverKillBairitu = FloatOptionItem.Create(RoleInfo, 12, OptionName.EarnestWolfOverBairitu, new(0.25f, 10f, 0.01f), 1.05f, false).SetValueFormat(OptionFormat.Multiplier);
        OptionNomalKillDistance = StringOptionItem.Create(RoleInfo, 13, OptionName.EarnestWolfNomalKllDistance, EnumHelper.GetAllNames<OverrideKilldistance.KillDistance>(), 0, false);
        OptionOverKillDistance = StringOptionItem.Create(RoleInfo, 14, OptionName.EarnestWolfOverKillDistance, EnumHelper.GetAllNames<OverrideKilldistance.KillDistance>(), 2, false);
        OptionOverKillDontKillM = BooleanOptionItem.Create(RoleInfo, 15, OptionName.EarnestWolfOverKillDontKillM, false, false);
        OptionOverKillCantReport = BooleanOptionItem.Create(RoleInfo, 16, OptionName.EarnestWolfOverKillCantReport, false, false);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.KillDistance = OverKillMode ? OptionOverKillDistance.GetInt() : OptionNomalKillDistance.GetInt();
        AURoleOptions.PhantomCooldown = 0;
    }
    public bool OnCheckMurderAsEarnestWolf(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;

        //falseだったとしても知らないねっ!
        if (OverKillMode)
        {
            count++;
            KillCoolDown = KillCoolDown * OptionOverKillBairitu.GetFloat();

            info.DontRoleAbility = null;
            info.KillPower = 10;
            var dummykiller = OptionOverKillDontKillM.GetBool() ? target : killer;

            if (info.IsCanKilling is false) return false;
            CustomRoleManager.CheckMurderInfos[info.AppearanceKiller.PlayerId] = info;
            dummykiller.RpcMurderPlayer(target);
            OverKillMode = OptionOverKillCanCount.GetBool() is false;
            OverKillList.Add(target.PlayerId);

            SendRPC();

            _ = new LateTask(() =>
            {
                UtilsNotifyRoles.NotifyRoles(Player);
                Player.SetKillCooldown(delay: true);
                Player.SyncSettings();
            }, 0.2f, "EarnestWolf");
            return true;
        }
        return false;
    }
    public override string GetProgressText(bool comms = false, bool gamelog = false)
    {
        var limit = OptionOverKillCanCount.GetInt() - count;
        return Utils.ColorString(limit > 0 ? Palette.ImpostorRed : Palette.DisabledGrey, $"({limit})");
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (seer == seen && !isForMeeting) return OverKillMode ? "<color=#ff1919>◎</color>" : "";
        return "";
    }
    public void OnClick(ref bool AdjustKillCooldown, ref bool? ResetCooldown)
    {
        AdjustKillCooldown = true;
        ResetCooldown = false;
        if (!Player.IsAlive()) return;
        if (count >= OptionOverKillCanCount.GetFloat())
        {
            OverKillMode = false;
            return;
        }
        OverKillMode = !OverKillMode;
        SendRPC();
        _ = new LateTask(() =>
        {
            UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player);
            Player.SyncSettings();
        }, 0.2f, "EarnestWolf OnClick");
    }
    public float CalculateKillCooldown() => KillCoolDown;
    public override string GetAbilityButtonText() => GetString("Modechenge");
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (seen.PlayerId != seer.PlayerId || isForMeeting || count >= OptionOverKillCanCount.GetFloat() || !Player.IsAlive()) return "";

        if (isForHud) return GetString("EarnestWolfLowerText");
        return $"<size=50%>{GetString("EarnestWolfLowerText")}</size>";
    }
    public override bool CancelReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target, ref DontReportreson reason)
    {
        if (target is null || CantReport is false) return false;

        if (OverKillList.Contains(target.PlayerId))
        {
            reason = DontReportreson.Impostor;
            return true;
        }
        return false;
    }

    public void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(count);
        sender.Writer.Write(OverKillMode);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        count = reader.ReadInt32();
        OverKillMode = reader.ReadBoolean();
    }
}