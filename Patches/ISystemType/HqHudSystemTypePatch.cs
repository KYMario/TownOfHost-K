using HarmonyLib;
using Hazel;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.Neutral;

namespace TownOfHost.Patches.ISystemType;

[HarmonyPatch(typeof(HqHudSystemType), nameof(HqHudSystemType.UpdateSystem))]
public static class HqHudSystemTypeUpdateSystemPatch
{
    public static bool Prefix(HqHudSystemType __instance, [HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] MessageReader msgReader)
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
        if (tags == HqHudSystemType.Tags.FixBit && isMadmate && !Options.MadmateCanFixComms.GetBool())
        {
            return false;
        }
        if (Options.CommsDonttouch.GetBool())
            if (Options.CommsDonttouchTime.GetFloat() > Main.sabotagetime)
            {
                return false;
            }

        if (player.Is(CustomRoles.Clumsy))
        {
            return false;
        }
        if (RoleAddAddons.AllData.TryGetValue(player.GetCustomRole(), out var data) && data.GiveAddons.GetBool() && data.GiveClumsy.GetBool()) return false;

        if (Roles.AddOns.Common.Amnesia.CheckAbility(player))
            if (playerRole is ISystemTypeUpdateHook systemTypeUpdateHook && !systemTypeUpdateHook.UpdateHqHudSystem(__instance, amount))
            {
                return false;
            }
        return true;
    }
    public static void Postfix()
    {
        Camouflage.CheckCamouflage();
        Utils.NotifyRoles();
    }
}
