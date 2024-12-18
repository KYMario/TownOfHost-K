using HarmonyLib;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using UnityEngine;

namespace TownOfHost.Patches;

[HarmonyPatch(typeof(SabotageButton), nameof(SabotageButton.DoClick))]
public static class SabotageButtonDoClickPatch
{
    public static bool Prefix()
    {
        if (!PlayerControl.LocalPlayer.inVent && GameManager.Instance.SabotagesEnabled())
        {
            DestroyableSingleton<HudManager>.Instance.ToggleMapVisible(new MapOptions
            {
                Mode = MapOptions.Modes.Sabotage
            });
        }

        return false;
    }
}
[HarmonyPatch(typeof(SabotageButton), nameof(SabotageButton.Refresh))]
public static class SabotageButtonRefreshPatch
{
    public static void Postfix()
    {
        //ホストがMODを導入していないorロビーなら実行しない
        if (!GameStates.IsModHost || GameStates.IsLobby) return;
        if (GameStates.Meeting) return;

        HudManager.Instance.SabotageButton.ToggleVisible(PlayerControl.LocalPlayer.CanUseSabotageButton());
    }
}

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.DoClick))]
public static class AbilityButtonDoClickPatch
{
    public static bool Prefix()
    {
        if (!AmongUsClient.Instance.AmHost || HudManager._instance.AbilityButton.isCoolingDown || !PlayerControl.LocalPlayer.CanMove || Utils.IsActive(SystemTypes.MushroomMixupSabotage) || !PlayerControl.LocalPlayer.IsAlive()) return true;

        var role = PlayerControl.LocalPlayer.GetCustomRole();
        var roleInfo = role.GetRoleInfo();
        var roleclas = PlayerControl.LocalPlayer.GetRoleClass();

        if (role.GetRoleTypes() is AmongUs.GameOptions.RoleTypes.Scientist)
        {
            CloseVitals.Ability = true;
            return true;
        }
        if (roleclas is IUseTheShButton sb && sb.UseOCButton)
        {
            PlayerControl.LocalPlayer.Data.Role.SetCooldown();
            sb.OnClick();
            return false;
        }
        else
        if (roleInfo?.IsDesyncImpostor == true && roleInfo.BaseRoleType.Invoke() == AmongUs.GameOptions.RoleTypes.Shapeshifter)
        {
            if (!(roleclas?.CanUseAbilityButton() ?? false)) return false;
            foreach (var p in PlayerCatch.AllPlayerControls)
            {
                p.Data.Role.NameColor = Color.white;
            }
            PlayerControl.LocalPlayer.Data.Role.Cast<ShapeshifterRole>().UseAbility();
            foreach (var p in PlayerCatch.AllPlayerControls)
            {
                p.Data.Role.NameColor = Color.white;
            }
            return true;
        }
        else
        if (roleInfo?.IsDesyncImpostor == true && roleInfo?.BaseRoleType.Invoke() == AmongUs.GameOptions.RoleTypes.Phantom)
        {
            if (!(roleclas?.CanUseAbilityButton() ?? false)) return false;
            foreach (var p in PlayerCatch.AllPlayerControls)
            {
                p.Data.Role.NameColor = Color.white;
            }
            PlayerControl.LocalPlayer.Data.Role.Cast<PhantomRole>().UseAbility();
            return true;
        }
        return true;
    }
}

/*[HarmonyPatch(typeof(KillButton), nameof(KillButton.DoClick))]
public static class KillButtonDoClickPatch
{
    public static void Prefix()
    {
        var players = PlayerControl.LocalPlayer.GetPlayersInAbilityRangeSorted(false);
        PlayerControl closest = players.Count <= 0 ? null : players[0];
        if (!GameStates.IsInTask || !PlayerControl.LocalPlayer.CanUseKillButton() || closest == null
            || PlayerControl.LocalPlayer.Data.IsDead || HudManager._instance.KillButton.isCoolingDown) return;
        PlayerControl.LocalPlayer.CheckMurder(closest); //一時的な修正
    }
}*/
