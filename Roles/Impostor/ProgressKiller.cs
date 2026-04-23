using AmongUs.GameOptions;

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
            OptionSort: (5, 1),
            from: From.TownOfHost_K
        );
    public ProgressKiller(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        ProgressKillerMadseen = OptionProgressKillerMadseen.GetBool();
        ProgressWorkhorseseen = OptionProgressWorkhorseseen.GetBool();
        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
    }
    public static OptionItem OptionProgressKillerMadseen;
    public static OptionItem OptionProgressWorkhorseseen;
    enum OptionName
    {
        ProgressKillerMadseen,
        ProgressWorkhorseseen,
    }
    public static bool ProgressKillerMadseen;
    public static bool ProgressWorkhorseseen;
    private static void SetupOptionItem()
    {
        OptionProgressKillerMadseen = BooleanOptionItem.Create(RoleInfo, 10, OptionName.ProgressKillerMadseen, true, false);
        OptionProgressWorkhorseseen = BooleanOptionItem.Create(RoleInfo, 11, OptionName.ProgressWorkhorseseen, true, false);
    }
    public override bool NotifyRolesCheckOtherName => true;
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        seen ??= seer;
        if (ProgressKillerMadseen && seen.Is(CustomRoleTypes.Madmate) && seer.Is(CustomRoles.ProgressKiller) && seer != seen)
        {
            if (seen.GetPlayerTaskState().IsTaskFinished)
                return Utils.ColorString(RoleInfo.RoleColor, "☆");
        }
        return "";
    }
    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;

        if (seer.Is(CustomRoles.ProgressKiller) && !seen.Is(CustomRoleTypes.Madmate) && seer != seen)
        {
            if (seen.GetPlayerTaskState().IsTaskFinished)
                return Utils.ColorString(RoleInfo.RoleColor, "〇");
        }
        return "";
    }
}