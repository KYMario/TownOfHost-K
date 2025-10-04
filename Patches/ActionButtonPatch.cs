using HarmonyLib;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

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
        if (GameStates.CalledMeeting) return;

        HudManager.Instance.SabotageButton.ToggleVisible(PlayerControl.LocalPlayer.CanUseSabotageButton());
    }
}

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.DoClick))]
public static class AbilityButtonDoClickPatch
{
    public static bool Prefix(AbilityButton __instance)
    {
        var player = PlayerControl.LocalPlayer;

        if (!AmongUsClient.Instance.AmHost || HudManager._instance.AbilityButton.isCoolingDown
        || !player.CanMove || !player.IsAlive()
        || (Utils.IsActive(SystemTypes.MushroomMixupSabotage) && player.Data.RoleType == RoleTypes.Shapeshifter)) return true;

        var role = player.GetCustomRole();
        var roleInfo = role.GetRoleInfo();
        var roleclass = player.GetRoleClass();

        if (role.GetRoleTypes() is RoleTypes.Scientist)
        {
            CloseVitals.Ability = true;
            return true;
        }
        if (roleclass is IUsePhantomButton pb && pb.UseOneclickButton)
        {
            //Shと違い、クリックしたときクールが発生しないことがあるため、
            //クリックしたってのを最低限可視化させる。
            __instance.OverrideColor(Palette.DisabledGrey);
            _ = new LateTask(() =>
            {
                __instance.OverrideColor(Palette.EnabledColor);
            }, 0.07f, "", true);
            //非クライアントの場合、役職調整の影響でキルクール弄らないとキルクールが正常の値にならないが、
            //クライアントの場合、別に役職変えてファントム状態解除をしなくていいので関係ない関数になる★

            bool AdjustKillCooldown = true;
            bool? ResetCooldown = true;

            pb.CheckOnClick(ref AdjustKillCooldown, ref ResetCooldown);

            if (ResetCooldown == true)
            {
                player.Data.Role.SetCooldown();
            }

            return false;
        }
        else
        if (roleInfo?.IsDesyncImpostor == true && roleInfo.BaseRoleType.Invoke() == RoleTypes.Shapeshifter)
        {
            if (!(roleclass?.CanUseAbilityButton() ?? false)) return false;
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                pc.Data.Role.NameColor = Color.white;
            }
            player.Data.Role.Cast<ShapeshifterRole>().UseAbility();
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                pc.Data.Role.NameColor = Color.white;
            }
            return true;
        }
        else
        if (roleInfo?.IsDesyncImpostor == true && roleInfo?.BaseRoleType.Invoke() == RoleTypes.Phantom)
        {
            if (!(roleclass?.CanUseAbilityButton() ?? false)) return false;
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                pc.Data.Role.NameColor = Color.white;
            }
            player.Data.Role.Cast<PhantomRole>().UseAbility();
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