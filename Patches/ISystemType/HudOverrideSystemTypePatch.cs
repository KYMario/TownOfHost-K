using HarmonyLib;
using Hazel;
using TownOfHost.Roles.AddOns.Common;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.Neutral;

namespace TownOfHost.Patches.ISystemType;

[HarmonyPatch(typeof(HudOverrideSystemType), nameof(HudOverrideSystemType.UpdateSystem))]
public static class HudOverrideSystemTypeUpdateSystemPatch
{
    public static bool Prefix(HudOverrideSystemType __instance, [HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] MessageReader msgReader)
    {
        byte amount;
        {
            var newReader = MessageReader.Get(msgReader);
            amount = newReader.ReadByte();
            newReader.Recycle();
        }
        if (!AmongUsClient.Instance.AmHost)
        {
            return true;
        }
        if (amount.HasBit(SwitchSystem.DamageSystem))
        {
            return true;
        }
        var tags = (HqHudSystemType.Tags)(amount & HqHudSystemType.TagMask);
        var playerRole = player.GetRoleClass();
        var isMadmate =
            player.Is(CustomRoleTypes.Madmate) ||
            // マッド属性化時に削除
            (playerRole is SchrodingerCat schrodingerCat && schrodingerCat.AmMadmate);

        if (isMadmate && !Options.MadmateCanFixComms.GetBool())
        {
            return false;
        }
        if (player.Is(CustomRoles.Clumsy) || player.Is(CustomRoles.Satellite))
        {
            return false;
        }
        if (Options.CommsDonttouch.GetBool())
            if (Options.CommsDonttouchTime.GetFloat() > Main.sabotagetime)
            {
                return false;
            }

        if (RoleAddAddons.GetRoleAddon(player.GetCustomRole(), out var data, player, subrole: CustomRoles.Clumsy) && data.GiveClumsy.GetBool()) return false;

        if (Amnesia.CheckAbility(player))
            if (playerRole is ISystemTypeUpdateHook systemTypeUpdateHook && !systemTypeUpdateHook.UpdateHudOverrideSystem(__instance, amount))
            {
                return false;
            }
        return true;
    }
    public static void Postfix()
    {
        Camouflage.CheckCamouflage();
        UtilsNotifyRoles.NotifyRoles();
    }
}
