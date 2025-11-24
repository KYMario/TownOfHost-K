using HarmonyLib;
using Hazel;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Patches.ISystemType;

[HarmonyPatch(typeof(HeliSabotageSystem), nameof(HeliSabotageSystem.UpdateSystem))]
public static class HeliSabotageSystemUpdateSystemPatch
{
    public static bool Prefix(HeliSabotageSystem __instance, [HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] MessageReader msgReader)
    {
        byte amount;
        {
            var newReader = MessageReader.Get(msgReader);
            amount = newReader.ReadByte();
            newReader.Recycle();
        }
        if (!AmongUsClient.Instance.AmHost || Utils.NowKillFlash)
        {
            return true;
        }
        if (amount.HasBit(SwitchSystem.DamageSystem))
        {
            return true;
        }

        if (player.Is(CustomRoles.Slacker))
        {
            return false;
        }
        if (Modules.SuddenDeathMode.NowSuddenDeathMode) return false;
        if (RoleAddAddons.GetRoleAddon(player.GetCustomRole(), out var data, player, subrole: CustomRoles.Slacker) && data.GiveSlacker.GetBool()) return false;

        if (Roles.AddOns.Common.Amnesia.CheckAbility(player))
            if (player.GetRoleClass() is ISystemTypeUpdateHook systemTypeUpdateHook && !systemTypeUpdateHook.UpdateHeliSabotageSystem(__instance, amount))
            {
                return false;
            }
        return true;
    }
}

//参考
//https://github.com/Koke1024/Town-Of-Moss/blob/main/TownOfMoss/Patches/MeltDownBoost.cs

[HarmonyPatch(typeof(HeliSabotageSystem), nameof(HeliSabotageSystem.Deteriorate))]
public static class HeliSabotageSystemPatch
{
    public static void Prefix(HeliSabotageSystem __instance)
    {
        if (!__instance.IsActive || (!Options.SabotageActivetimerControl.GetBool() && !SuddenDeathMode.NowSuddenDeathMode))
            return;
        if (AirshipStatus.Instance != null)
            if (SuddenDeathMode.NowSuddenDeathMode)
            {
                if (__instance.Countdown >= SuddenDeathMode.SuddenDeathReactortime.GetFloat())
                    __instance.Countdown = SuddenDeathMode.SuddenDeathReactortime.GetFloat();
                return;
            }
        if (AirshipStatus.Instance != null)
            if (__instance.Countdown >= Options.AirshipReactorTimeLimit.GetFloat())
                __instance.Countdown = Options.AirshipReactorTimeLimit.GetFloat();
    }
}
