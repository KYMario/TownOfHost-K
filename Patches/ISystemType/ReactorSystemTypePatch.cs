using HarmonyLib;
using Hazel;
using TownOfHost.Roles.AddOns.Common;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Patches.ISystemType;

[HarmonyPatch(typeof(ReactorSystemType), nameof(ReactorSystemType.UpdateSystem))]
public static class ReactorSystemTypeUpdateSystemPatch
{
    public static bool Prefix(ReactorSystemType __instance, [HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] MessageReader msgReader, ref byte __state /* amount */)
    {
        byte amount;
        {
            var newReader = MessageReader.Get(msgReader);
            amount = newReader.ReadByte();
            newReader.Recycle();
        }
        __state = amount;
        if (!AmongUsClient.Instance.AmHost)
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
        if (Options.SuddenDeathMode.GetBool()) return false;
        if (RoleAddAddons.AllData.TryGetValue(player.GetCustomRole(), out var data) && data.GiveAddons.GetBool() && data.GiveSlacker.GetBool()) return false;

        if (Amnesia.CheckAbility(player))
            if (player.GetRoleClass() is ISystemTypeUpdateHook systemTypeUpdateHook && !systemTypeUpdateHook.UpdateReactorSystem(__instance, amount))
            {
                return false;
            }
        return true;
    }
    public static void Postfix(ReactorSystemType __instance, byte __state /* amount */ )
    {
        // サボタージュ発動時
        if (__state == ReactorSystemType.StartCountdown)
        {
            if (Options.SuddenDeathMode.GetBool())
            {
                __instance.Countdown = Options.SuddenDeathReactortime.GetFloat();
                return;
            }
            if (!Options.SabotageTimeControl.GetBool())
            {
                return;
            }
            var duration = (MapNames)Main.NormalOptions.MapId switch
            {
                MapNames.Skeld => Options.SkeldReactor.GetFloat(),
                MapNames.Mira => Options.Mirare.GetFloat(),
                MapNames.Polus => Options.PolusReactorTimeLimit.GetFloat(),
                MapNames.Fungle => Options.FungleReactorTimeLimit.GetFloat(),
                _ => float.NaN,
            };
            if (!float.IsNaN(duration))
            {
                __instance.Countdown = duration;
            }
        }
    }
}
