using System.Collections.Generic;
using AmongUs.GameOptions;
using HarmonyLib;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Madmate;

public sealed class MadHacker : RoleBase, IKiller, IKillFlashSeeable, IDeathReasonSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(MadHacker),
            player => new MadHacker(player),
            CustomRoles.MadHacker,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Madmate,
            24800,
            SetupOptionItem,
            "mh",
            OptionSort: (2, 5),
            isDesyncImpostor: true,
            introSound: () => GetIntroSound(RoleTypes.Tracker)
        );
    public MadHacker(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        DeadPlayerList = new();

        CanSeeDeathReason = OptionCanSeeDeathReason.GetBool();
        CanSeeKillFlash = OptionCanSeeKillFlash.GetBool();
        CanSeeDeadBodyPosition = OptionCanSeeDeadBodyPosition.GetBool();
        IsCheckImposotor = OptionCheckImpostor.GetBool();
        CanUseVent = OptionCanUseVent.GetBool();
    }
    static List<byte> DeadPlayerList = new();

    static OptionItem OptionCanSeeDeathReason; static bool CanSeeDeathReason;
    static OptionItem OptionCanSeeKillFlash; static bool CanSeeKillFlash;
    static OptionItem OptionCanSeeDeadBodyPosition; static bool CanSeeDeadBodyPosition;
    static OptionItem OptionCheckImpostor; static bool IsCheckImposotor;
    static OptionItem OptionCanUseVent; static bool CanUseVent;
    enum OptionNames
    {
        MadHackerCanSeeDeadBodyPosition,
        MadHackerCheckImposotor
    }
    public static void SetupOptionItem()
    {
        OptionCanUseVent = BooleanOptionItem.Create(RoleInfo, 10, GeneralOption.CanVent, false, false);
        OptionCanSeeKillFlash = BooleanOptionItem.Create(RoleInfo, 11, "MadmateCanSeeKillFlash", true, false);
        OptionCanSeeDeathReason = BooleanOptionItem.Create(RoleInfo, 12, "MadmateCanSeeDeathReason", true, false);
        OptionCanSeeDeadBodyPosition = BooleanOptionItem.Create(RoleInfo, 13, OptionNames.MadHackerCanSeeDeadBodyPosition, true, false);
        OptionCheckImpostor = BooleanOptionItem.Create(RoleInfo, 14, OptionNames.MadHackerCheckImposotor, true, false);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(false);
        AURoleOptions.ShapeshifterCooldown = 1;
    }
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (Player.IsAlive() is false) return "";
        if (CanSeeDeadBodyPosition)
        {
            var seenstate = seen.GetPlayerState();
            if (seen.IsAlive() is false && DeadPlayerList.Contains(seen.PlayerId) is false)
                return $"<size=60%><#ff1919>{(seenstate.KillRoom == "" ? $"@???" : $"@{seenstate.KillRoom}")}</color>";
        }
        return "";
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (Player.IsAlive() is false) return "";
        if (IsCheckImposotor is false && isForMeeting) return "";
        if (seen.IsAlive() is false && DeadPlayerList.Contains(seen.PlayerId) is false)
        {
            var realkiller = seen.GetRealKiller();
            if (IsCheckImposotor && realkiller != seen)
            {
                return (Player.IsModClient() ? "" : "\n") + ((realkiller.Is(CustomRoleTypes.Impostor) || realkiller.GetCustomRole() is CustomRoles.WolfBoy or CustomRoles.Egoist) ?
                        "<#ff1919>×</color>" : "<#dddddd>×</color>");
            }
            return "<#ff7f50>×</color>";
        }
        return "";
    }
    public bool? CheckKillFlash(MurderInfo info) => CanSeeKillFlash;
    public bool? CheckSeeDeathReason(PlayerControl seen) => CanSeeDeathReason;
    public override void AfterMeetingTasks()
    {
        PlayerCatch.AllPlayerControls.DoIf(pc => DeadPlayerList.Contains(pc.PlayerId) is false && !pc.IsAlive()
        , pc => DeadPlayerList.Add(pc.PlayerId));
    }
    public override CustomRoles TellResults(PlayerControl player) => Options.MadTellOpt();
    public override void OnDead(PlayerControl player)
    {
        if (GameStates.CalledMeeting || !AmongUsClient.Instance.AmHost) return;
        _ = new LateTask(() =>
            UtilsNotifyRoles.NotifyRoles(ForceLoop: true, SpecifySeer: Player), 0.3f, "SendNotifyRoles", true);
    }
    bool IKiller.CanUseSabotageButton() => false;
    bool IKiller.IsKiller => false;
    bool IKiller.CanUseKillButton() => false;
    float IKiller.CalculateKillCooldown() => 0;
    bool IKiller.CanUseImpostorVentButton() => CanUseVent;
    public override bool CheckShapeshift(PlayerControl target, ref bool shouldAnimate)
    {
        shouldAnimate = false;
        return false;
    }
}
