using HarmonyLib;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Neutral;

namespace TownOfHost;

[HarmonyPatch(typeof(OneWayShadows), nameof(OneWayShadows.IsIgnored))]
public static class OneWayShadowsIsIgnoredPatch
{
    public static bool Prefix(OneWayShadows __instance, ref bool __result)
    {
        var roleInfo = PlayerControl.LocalPlayer.GetCustomRole().GetRoleInfo();
        var amDesyncImpostor = roleInfo?.IsDesyncImpostor == true;
        if (__instance.IgnoreImpostor && amDesyncImpostor && ((PlayerControl.LocalPlayer?.GetRoleClass() as BakeCat)?.CanKill is null or true))
        {
            __result = true;
            return false;
        }
        return true;
    }
}
