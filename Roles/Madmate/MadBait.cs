using System.Linq;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.Crewmate;

namespace TownOfHost.Roles.Madmate;

public sealed class MadBait : RoleBase, IKillFlashSeeable, IDeathReasonSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(MadBait),
            player => new MadBait(player),
            CustomRoles.MadBait,
            () => OptionCanVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
            CustomRoleTypes.Madmate,
            8000,
            SetupOptionItem,
            "mb",
            OptionSort: (2, 4),
            introSound: () => GetIntroSound(RoleTypes.Impostor),
            from: From.TownOfHost_K
        );
    public MadBait(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        canSeeKillFlash = Options.MadmateCanSeeKillFlash.GetBool();
        canSeeDeathReason = Options.MadmateCanSeeDeathReason.GetBool();
        CanVent = OptionCanVent.GetBool();
    }

    private static bool canSeeKillFlash;
    private static bool canSeeDeathReason;
    public static bool CanVent;
    static OptionItem RandomRepo;
    static OptionItem ImpRepo;
    static OptionItem OptionReportDelay;
    static OptionItem OptionMaxReportDelay;
    public static OptionItem OptionCanVent;
    enum Option
    {
        BaitReportDelay, BaitMaxDelay,
        MadBaitRandomReport, MadBaitIgnoreImpostor
    }
    public bool? CheckKillFlash(MurderInfo info) => canSeeKillFlash;
    public bool? CheckSeeDeathReason(PlayerControl seen) => canSeeDeathReason;
    public override CustomRoles TellResults(PlayerControl player) => Options.MadTellOpt();
    public static void SetupOptionItem()
    {
        RandomRepo = BooleanOptionItem.Create(RoleInfo, 10, Option.MadBaitRandomReport, true, false);
        ImpRepo = BooleanOptionItem.Create(RoleInfo, 11, Option.MadBaitIgnoreImpostor, false, false, RandomRepo);
        OptionReportDelay = FloatOptionItem.Create(RoleInfo, 12, Option.BaitReportDelay, new(0f, 180f, 0.5f), 3f, false).SetValueFormat(OptionFormat.Seconds);
        OptionMaxReportDelay = FloatOptionItem.Create(RoleInfo, 13, Option.BaitMaxDelay, new(0f, 180f, 0.5f), 3f, false).SetValueFormat(OptionFormat.Seconds);
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 14, GeneralOption.CanVent, true, false);

    }
    public override void OnMurderPlayerAsTarget(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;

        if (Utils.IsActive(SystemTypes.Comms) && Bait.OptCanUseActiveComms.OptionMeGetBool() is false) return;

        var tien = 0f;
        if (OptionMaxReportDelay.GetFloat() != 0)
        {
            int ti = IRandom.Instance.Next(0, (int)OptionMaxReportDelay.GetFloat() * 10);
            tien = ti * 0.1f;
            Logger.Info($"{tien}sの追加遅延発生!!", "Bait");
        }
        var killerrole = killer.GetCustomRole();
        if (!target.Is(CustomRoles.MadBait) || info.IsSuicide) return;

        //インポスター以外のキル
        if (!killerrole.IsImpostor() || killerrole == CustomRoles.WolfBoy)
        {
            _ = new LateTask(() =>
                ReportDeadBodyPatch.ExReportDeadBody(Player, target.Data)
                , 0.15f + OptionReportDelay.GetFloat() + tien, "MadBait Self Report");
        }//インポスターのキル
        else if (RandomRepo.GetBool())
        {
            var nise = PlayerCatch.AllAlivePlayerControls.Where(x => !x.GetCustomRole().IsImpostor() && !x.Is(CustomRoles.WolfBoy)).ToArray();
            if (!ImpRepo.GetBool()) nise = PlayerCatch.AllAlivePlayerControls.ToArray();
            var rand = IRandom.Instance;
            var P = nise[rand.Next(0, nise.Length)];
            _ = new LateTask(() =>
                ReportDeadBodyPatch.ExReportDeadBody(P, target.Data)
                , 0.15f + OptionReportDelay.GetFloat() + tien, "Bait Self Report");
        }
    }
}