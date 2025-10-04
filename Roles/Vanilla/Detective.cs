using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using Il2CppSystem.Collections.Generic;
using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Vanilla;

public sealed class Detective : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForVanilla(
            typeof(Detective),
            player => new Detective(player),
            RoleTypes.Detective,
            SetUpOptionItem,
            "#986f3a",
            from: From.AmongUs
        );
    public Detective(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        detectivesuspectlimit = DetectiveSuspectLimit.GetFloat();
        notesPageInfos.Clear();
    }
    public static List<DetectiveNotesPageInfo> notesPageInfos = new();
    static OptionItem DetectiveSuspectLimit; static float detectivesuspectlimit;
    static void SetUpOptionItem()
    {
        DetectiveSuspectLimit = FloatOptionItem.Create(RoleInfo, 3, StringNames.DetectiveSuspectLimit, new(1, 4, 1), 2, false);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.DetectiveSuspectLimit = detectivesuspectlimit;
    }

}
[HarmonyPatch(typeof(DetectiveRole), nameof(DetectiveRole.OnMeetingStart))]
class DetectiveRoleOnMeetingStartPatch
{
    public static void Postfix(DetectiveRole __instance)
    {
        if (__instance.Player.PlayerId == PlayerControl.LocalPlayer.PlayerId)
        {
            Detective.notesPageInfos = __instance.notesPageInfos;
        }
    }
}
[HarmonyPatch(typeof(DetectiveRole), nameof(DetectiveRole.OpenNotes))]
class DetectiveRoleOpenNotesPatch
{
    public static void Prefix(DetectiveRole __instance)
    {
        if (__instance.Player.PlayerId == PlayerControl.LocalPlayer.PlayerId)
        {
            __instance.notesPageInfos = Detective.notesPageInfos;
        }
    }
}