using HarmonyLib;
using Hazel;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Patches.ISystemType;

[HarmonyPatch(typeof(LifeSuppSystemType), nameof(LifeSuppSystemType.UpdateSystem))]
public static class LifeSuppSystemUpdateSystemPatch
{
    public static bool Prefix(LifeSuppSystemType __instance, [HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] MessageReader msgReader, ref byte __state /* amount */)
    {
        byte amount;
        {
            var newReader = MessageReader.Get(msgReader);
            amount = newReader.ReadByte();
            newReader.Recycle();
        }
        __state = amount;

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
            if (player.GetRoleClass() is ISystemTypeUpdateHook systemTypeUpdateHook && !systemTypeUpdateHook.UpdateLifeSuppSystem(__instance, amount))
            {
                return false;
            }
        return true;
    }
    public static void Postfix(LifeSuppSystemType __instance, byte __state /* amount */ )
    {
        // サボタージュ発動時
        if (__state == LifeSuppSystemType.StartCountdown)
        {
            if (!Options.SabotageActivetimerControl.GetBool())
            {
                return;
            }
            var duration = (MapNames)Main.NormalOptions.MapId switch
            {
                MapNames.Skeld => Options.SkeldO2TimeLimit.GetFloat(),
                MapNames.MiraHQ => Options.MiraO2TimeLimit.GetFloat(),
                _ => float.NaN,
            };
            if (!float.IsNaN(duration))
            {
                __instance.Countdown = duration;
            }
        }
    }
}
