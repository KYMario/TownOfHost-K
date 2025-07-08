using System;
using System.Linq;
using Il2CppSystem.Linq;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using AmongUs.GameOptions;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;

namespace TownOfHost
{
    [HarmonyPatch(typeof(StringOption), nameof(StringOption.Initialize))]
    public class StringOptionInitializePatch
    {
        public static bool Prefix(StringOption __instance)
        {
            var option = OptionItem.AllOptions.FirstOrDefault(opt => opt.OptionBehaviour == __instance);
            if (option == null)
            {
                return __instance?.stringOptionName is not Int32OptionNames.TaskBarMode;
            }
            var role = CustomRoles.NotAssigned;
            var size = "<size=105%>";
            string mark = "";
            if (Enum.TryParse(typeof(CustomRoles), option.Name, false, out var id))
            {
                role = (CustomRoles)id;
                size = "<size=125%>";
                if (role.IsAddOn())
                {
                    List<CustomRoles> list = new(1) { role };
                    mark = $" {UtilsRoleText.GetSubRoleMarks(list, CustomRoles.NotAssigned)}";
                }
            }
            __instance.OnValueChanged = new Action<OptionBehaviour>((o) => { });
            __instance.TitleText.text = size + "<b>" + option.GetName(isoption: true) + mark + option.Fromtext + "</b></size>"/* + addinfo*/;

            __instance.Value = __instance.oldValue = option.CurrentValue;
            __instance.ValueText.text = option.GetString();

            return false;
        }
    }
    [HarmonyPatch(typeof(NumberOption), nameof(NumberOption.Increase))]
    public class NumberOptionIncreasePatch
    {
        public static bool Prefix(NumberOption __instance)
        {
            if (__instance.floatOptionName is FloatOptionNames.ImpostorLightMod or FloatOptionNames.CrewLightMod)
            {
                if (__instance.Value < 2f)
                {
                    var che = true;
                    switch (__instance.Value)
                    {
                        case 0.25f: __instance.Value = 0.38f; break;
                        case 0.38f: __instance.Value = 0.5f; break;
                        case 0.5f: __instance.Value = 0.63f; break;
                        case 0.63f: __instance.Value = 0.75f; break;
                        case 0.75f: __instance.Value = 0.88f; break;
                        case 0.88f: __instance.Value = 1f; break;
                        case 1.00f: __instance.Value = 1.13f; break;
                        case 1.13f: __instance.Value = 1.25f; break;
                        case 1.25f: __instance.Value = 1.38f; break;
                        case 1.38f: __instance.Value = 1.5f; break;
                        case 1.5f: __instance.Value = 1.63f; break;
                        case 1.63f: __instance.Value = 1.75f; break;
                        case 1.75f: __instance.Value = 1.88f; break;
                        case 1.88f: __instance.Value = 2f; break;
                        default: che = false; break;
                    }
                    if (!che) return true;
                    __instance.UpdateValue();
                    GameOptionsSender.RpcSendOptions();
                    return false;
                }
            }
            else
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    var v = __instance.Increment * 5 + __instance.Value;
                    if (__instance.ValidRange.max <= v) v = __instance.ValidRange.max;
                    __instance.Value = v;
                    __instance.UpdateValue();
                    GameOptionsSender.RpcSendOptions();
                    return false;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(StringOption), nameof(StringOption.Increase))]
    public class StringOptionIncreasePatch
    {
        public static bool Prefix(StringOption __instance)
        {
            var option = OptionItem.AllOptions.FirstOrDefault(opt => opt.OptionBehaviour == __instance);
            if (option == null) return true;
            //if (option.Id == 1 && option.CurrentValue == 1 && !Main.TaskBattleOptionv) option.CurrentValue++;
            if (option.Name == "KickModClient") Main.LastKickModClient.Value = true;
            if (option.Name is "Role" or "SuddenRedTeamRole" or "SuddenBlueTeamRole" or "SuddenYellowTeamRole" or "SuddenGreenTeamRole" or "SuddenPurpleTeamRole")
            {
                var ch = true;
                var v = option.CurrentValue;
                while (ch)
                {
                    v++;
                    if (!UtilsRoleInfo.GetRoleByInputName((option as StringOptionItem).GetString(v).RemoveHtmlTags(), out var role, true))
                    {
                        v = -1;
                        continue;
                    }

                    if ((Options.CustomRoleSpawnChances.TryGetValue(role, out var op) && (op?.GetBool() ?? false)) || role is CustomRoles.Crewmate or CustomRoles.Impostor or CustomRoles.Madmate or CustomRoles.Opportunist or CustomRoles.Arsonist or CustomRoles.Braid or CustomRoles.NotAssigned)//マッド/オポチュは処理落ち対策。
                    {
                        ch = false;
                        option.SetValue(v);
                    }
                }
            }
            else
                option.SetValue(option.CurrentValue + (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 5 : 1));
            return false;
        }
    }
    [HarmonyPatch(typeof(NumberOption), nameof(NumberOption.Decrease))]
    public class NumberOptionDecreasePatch
    {
        public static bool Prefix(NumberOption __instance)
        {
            if (__instance.floatOptionName is FloatOptionNames.ImpostorLightMod or FloatOptionNames.CrewLightMod)
            {
                if (__instance.Value <= 2f)
                {
                    var che = true;
                    switch (__instance.Value)
                    {
                        case 0.25f: __instance.Value = 0.25f; break;
                        case 0.38f: __instance.Value = 0.25f; break;
                        case 0.5f: __instance.Value = 0.38f; break;
                        case 0.63f: __instance.Value = 0.5f; break;
                        case 0.75f: __instance.Value = 0.63f; break;
                        case 0.88f: __instance.Value = 0.75f; break;
                        case 1f: __instance.Value = 0.88f; break;
                        case 1.13f: __instance.Value = 1f; break;
                        case 1.25f: __instance.Value = 1.13f; break;
                        case 1.38f: __instance.Value = 1.25f; break;
                        case 1.5f: __instance.Value = 1.38f; break;
                        case 1.63f: __instance.Value = 1.5f; break;
                        case 1.75f: __instance.Value = 1.63f; break;
                        case 1.88f: __instance.Value = 1.75f; break;
                        case 2.00f: __instance.Value = 1.88f; break;
                        default: che = false; break;
                    }
                    if (!che) return true;
                    __instance.UpdateValue();
                    GameOptionsSender.RpcSendOptions();
                    return false;
                }
            }
            else
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    var v = __instance.Increment * -5 + __instance.Value;
                    if (__instance.ValidRange.min >= v) v = __instance.ValidRange.min;
                    __instance.Value = v;
                    __instance.UpdateValue();
                    GameOptionsSender.RpcSendOptions();
                    return false;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(StringOption), nameof(StringOption.Decrease))]
    public class StringOptionDecreasePatch
    {
        public static bool Prefix(StringOption __instance)
        {
            var option = OptionItem.AllOptions.FirstOrDefault(opt => opt.OptionBehaviour == __instance);
            if (option == null) return true;
            //if (option.Id == 1 && option.CurrentValue == 0 && !Main.TaskBattleOptionv) option.CurrentValue--;
            if (option.Name == "KickModClient") Main.LastKickModClient.Value = false;

            if (option.Name is "Role" or "SuddenRedTeamRole" or "SuddenBlueTeamRole" or "SuddenYellowTeamRole" or "SuddenGreenTeamRole" or "SuddenPurpleTeamRole")
            {
                var ch = true;
                var v = option.CurrentValue;
                while (ch)
                {
                    v--;
                    if (v < 0)
                        v = (option as StringOptionItem).Selections.Count() - 1;

                    if (!UtilsRoleInfo.GetRoleByInputName((option as StringOptionItem).GetString(v).RemoveHtmlTags(), out var role, true))
                    {
                        role = CustomRoles.Opportunist;
                        continue;
                    }

                    if ((Options.CustomRoleSpawnChances.TryGetValue(role, out var op) && (op?.GetBool() ?? false)) || role is CustomRoles.Crewmate or CustomRoles.Impostor or CustomRoles.Madmate or CustomRoles.Opportunist or CustomRoles.Arsonist or CustomRoles.Braid or CustomRoles.NotAssigned)//マッド/オポチュは処理落ち対策。
                    {
                        ch = false;
                        option.SetValue(v);
                    }
                }
            }
            else
                option.SetValue(option.CurrentValue - (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 5 : 1));
            return false;
        }
    }
}