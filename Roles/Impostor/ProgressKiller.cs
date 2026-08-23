using AmongUs.GameOptions;
using UnityEngine;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;

public sealed class ProgressKiller : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(ProgressKiller),
            player => new ProgressKiller(player),
            CustomRoles.ProgressKiller,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            5300,
            SetupOptionItem,
            "pk",
            OptionSort: (5, 1)
        );
    public ProgressKiller(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        RequiredKillCount = OptionRequiredKillCount.GetInt();
    }
    public static OptionItem OptionRequiredKillCount;
    enum OptionName
    {
        ProgressKillerRequiredKillCount,
    }
    public static int RequiredKillCount;
    private static void SetupOptionItem()
    {
        OptionRequiredKillCount = IntegerOptionItem.Create(RoleInfo, 12, OptionName.ProgressKillerRequiredKillCount, new(0, 15, 1), 1, false)
            .SetValueFormat(OptionFormat.Times);
    }
    public override bool NotifyRolesCheckOtherName => true;
    public bool CanSeeTaskProgress()
        => Player.IsAlive() && MyState.GetKillCount(true) >= RequiredKillCount;
    public bool CanSeeTaskProgress(PlayerControl seen)
        => CanSeeTaskProgress()
        && seen != null
        && seen != Player
        && UtilsTask.HasTasks(seen.Data, false);
    public void OnMurderPlayerAsKiller(MurderInfo info)
    {
        if (!info.IsSuicide)
            UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player);
    }
    public override void OverrideProgressTextAsSeer(PlayerControl seen, ref bool enabled, ref string text)
    {
        if (!CanSeeTaskProgress(seen)) return;
        enabled = true;
        var comms = Utils.IsActive(SystemTypes.Comms);
        text = UtilsRoleText.GetTaskProgressText(seen.PlayerId, comms, false);
    }
    public override string GetProgressText(bool comms = false, bool gamelog = false)
    {
        var killCount = MyState.GetKillCount(true);
        if (killCount >= RequiredKillCount)
            return Utils.ColorString(RoleInfo.RoleColor, "★");
        return Utils.ColorString(Palette.ImpostorRed.ShadeColor(0.5f), $"({killCount}/{RequiredKillCount})");
    }
}
