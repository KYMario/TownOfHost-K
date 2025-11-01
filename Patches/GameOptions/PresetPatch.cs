using System;
using System.Linq;
using HarmonyLib;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost;

[HarmonyPatch(typeof(GamePresetsTab))]
class PresetMenu
{
    [HarmonyPatch(nameof(GamePresetsTab.Start)), HarmonyPostfix]
    public static void StartPostfixPatch(GamePresetsTab __instance)
    {
        var AlternateRules = __instance.SecondPresetButton;
        AlternateRules.transform.localScale = new(0.6f, 0.6f);
        AlternateRules.transform.localPosition = new(-0.638f, 0.8f);//StandardRules
        var StandardRules = __instance.StandardPresetButton;
        StandardRules.transform.localScale = new(0.6f, 0.6f);
        StandardRules.transform.localPosition = new(-2.38f, 0.8f, 0);

        var RoleReset = CreatePresetButton(GetString("RoleReset"), new Color32(255, 100, 255, byte.MaxValue), 0, () =>
        {
            foreach (var roleopt in Options.CustomRoleSpawnChances)
            {
                if (roleopt.Value.GetValue() is not 0)
                    roleopt.Value.SetValue(0, false, false);
            }
            foreach (var slotopt in OptionItem.AllOptions.Where(opt => opt.Name.Contains("SlotRole")))
            {
                if (slotopt is AssignOptionItem assignOption)
                {
                    assignOption.SetRoleValue([]);
                }
            }
            OptionItem.SyncAllOptions();
            OptionSaver.Save();
        });

        var SheriffAndMad = CreatePresetButton(GetString("SheriffAndMadPreset"), new Color32(255, 178, 40, byte.MaxValue), 1, () =>
        {
            foreach (var roleopt in Options.CustomRoleSpawnChances)
            {
                bool IsShrima = roleopt.Key is CustomRoles.Sheriff or CustomRoles.MadSnitch or CustomRoles.EvilHacker or CustomRoles.EvilTracker;
                roleopt.Value.SetValue(IsShrima ? 10 : 0, false, false);
            }
            OptionItem.SyncAllOptions();
            OptionSaver.Save();
        });

        var SetMenyRole = CreatePresetButton(GetString("SetMenyRole"), new Color32(255, 0, 40, byte.MaxValue), 2, () =>
        {
            foreach (var roleopt in Options.CustomRoleSpawnChances)
            {
                bool IsShrima = roleopt.Key is CustomRoles.Jumper or CustomRoles.EvilSatellite or CustomRoles.MadGuardian or CustomRoles.SwitchSheriff or CustomRoles.PonkotuTeller or CustomRoles.Insider
                or CustomRoles.Stolener or CustomRoles.Snowman or CustomRoles.Walker or CustomRoles.Jackal or CustomRoles.Jester;
                if ((IsShrima ? 10 : 0) != roleopt.Value.GetValue()) roleopt.Value.SetValue(IsShrima ? 10 : 0, false, false);
            }
            OptionItem.SyncAllOptions();
            OptionSaver.Save();
        });

        var SetAllRole = CreatePresetButton($"<#aa84f0>{GetString("AllRole")}</color>", new Color32(69, 24, 153, byte.MaxValue), 3, () =>
        {
            foreach (var option in Options.CustomRoleSpawnChances)
            {
                var role = option.Key;
                if (role is CustomRoles.NotAssigned) continue;
                if (Event.CheckRole(role) is false) continue;
                if (role.IsImpostor() || role.IsCrewmate() || role.IsMadmate() || role.IsNeutral())
                {
                    if (option.Value.GetValue() is not 10)
                        option.Value.SetValue(10, false, false);
                }
            }
            OptionItem.SyncAllOptions();
            OptionSaver.Save();
        });

        var SetAllRoleAndAddon = CreatePresetButton($"<#aa84f0>{GetString("AllRoleAndSubRole")}</color>", new Color32(60, 60, 60, byte.MaxValue), 4, () =>
        {
            foreach (var option in Options.CustomRoleSpawnChances)
            {
                var role = option.Key;
                if (role is CustomRoles.NotAssigned) continue;
                if (Event.CheckRole(role) is false) continue;
                if (option.Value.GetValue() is not 10)
                    option.Value.SetValue(10, false, false);
            }
            OptionItem.SyncAllOptions();
            OptionSaver.Save();
        });

        var SetSuddenDeathMode = CreatePresetButton($"<#ffaf8a>{GetString("SetSuddenDeathMode")}</color>", new Color32(242, 125, 70, byte.MaxValue), 5, () =>
        {
            foreach (var option in Options.CustomRoleSpawnChances.Where(option => option.Key is not CustomRoles.NotAssigned && Event.CheckRole(option.Key)))
            {
                int IsShrima = option.Key is CustomRoles.Jumper or CustomRoles.Evilgambler or CustomRoles.EvilHacker or CustomRoles.Mole or CustomRoles.QuickKiller or CustomRoles.Sniper
                or CustomRoles.UltraStar or CustomRoles.Shyboy or CustomRoles.DoppelGanger or CustomRoles.Terrorist or CustomRoles.Vulture ? 10 : 0;
                if (option.Value.GetValue() != IsShrima) option.Value.SetValue(IsShrima, false, false);
            }
            OptionItem.SyncAllOptions();
            OptionSaver.Save();
        });
    }

    private static PassiveButton CreatePresetButton(string text, Color32 color, int yNum, Action onClick)
    {
        var setPresetButton = GameObject.Instantiate(GameSettingMenu.Instance.GamePresetsButton, GameSettingMenu.Instance.PresetsTab.AlternateRulesText.transform.parent);
        if (setPresetButton)
        {
            setPresetButton.buttonText.text = text;
            setPresetButton.buttonText.DestroyTranslator();
            setPresetButton.inactiveSprites.GetComponent<SpriteRenderer>().color =
            setPresetButton.activeSprites.GetComponent<SpriteRenderer>().color =
            setPresetButton.selectedSprites.GetComponent<SpriteRenderer>().color = color;
            setPresetButton.transform.localPosition = new Vector3(5.561f, 1.7467f - (0.89803f * yNum), 0);
            setPresetButton.transform.localScale = new Vector3(1.25f, 1.25f, 0);
            setPresetButton.OnClick = new();
            setPresetButton.OnClick.AddListener(onClick);

            if (ControllerManager.Instance.CurrentUiState.MenuName == GameSettingMenu.Instance.PresetsTab.name)
                ControllerManager.Instance.CurrentUiState.SelectableUiElements.Add(setPresetButton);
            else
                GameSettingMenu.Instance.PresetsTab.ControllerSelectable.Add(setPresetButton);
        }
        return setPresetButton;
    }
}