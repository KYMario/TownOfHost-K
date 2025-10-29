using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

using TownOfHost.Roles.Core;

using Object = UnityEngine.Object;
using static TownOfHost.Translator;

namespace TownOfHost
{
    public class LobbyInfoPanePatch
    {
        public static void Postfix()
        {
            var window = GameObject.Find("Main Camera/Hud/LobbyInfoPane/AspectSize/RulesPopOutWindow").GetComponent<LobbyViewSettingsPane>();
            var ModButton = Object.Instantiate(window.taskTabButton, window.transform);
            var ShowStgTMPButton = Object.Instantiate(HudManager.Instance.SettingsButton, window.transform).GetComponent<PassiveButton>();

            window.rolesTabButton.transform.localPosition += new Vector3(3.4938f, 0f);
            ModButton.transform.localPosition += new Vector3(3.4938f, 0f);
            ShowStgTMPButton.GetComponent<AspectPosition>().DistanceFromEdge += new Vector3(-0.35f, 1.2f);

            ModButton.buttonText.DestroyTranslator();
            ModButton.buttonText.text = "MOD";

            ShowStgTMPButton.ClickSound = ModButton.ClickSound;

            ModButton.OnClick = new();
            ModButton.OnClick.AddListener((Action)(() =>
            {
                window.rolesTabButton.SelectButton(false);
                window.taskTabButton.SelectButton(false);
                ModButton.SelectButton(true);

                for (int index2 = 0; index2 < window.settingsInfo.Count; ++index2)
                    Object.Destroy((Object)window.settingsInfo[index2].gameObject);
                window.settingsInfo.Clear();

                float y1 = 1.44f;

                CategoryHeaderMasked categoryHeaderMasked = Object.Instantiate<CategoryHeaderMasked>(window.categoryHeaderOrigin);
                SetHeader(categoryHeaderMasked, GetString("TabGroup.MainSettings"));
                categoryHeaderMasked.transform.SetParent(window.settingsContainer);
                categoryHeaderMasked.transform.localScale = Vector3.one;
                categoryHeaderMasked.transform.localPosition = new Vector3(-9.77f, y1, -2f);
                window.settingsInfo.Add(categoryHeaderMasked.gameObject);

                float y2 = y1 - 1f;
                int index = 0;
                foreach (OptionItem option in OptionItem.AllOptions)
                {
                    if (option.Tab != TabGroup.MainSettings || option.IsHiddenOn(Options.CurrentGameMode) || (!option?.Parent?.GetBool() ?? false)) continue;
                    ViewSettingsInfoPanel settingsInfoPanel = Object.Instantiate<ViewSettingsInfoPanel>(window.infoPanelOrigin);
                    settingsInfoPanel.transform.SetParent(window.settingsContainer);
                    settingsInfoPanel.transform.localScale = Vector3.one;
                    float x;
                    if (index % 2 == 0)
                    {
                        x = -8.95f;
                        if (index > 0)
                            y2 -= 0.59f;
                    }
                    else
                        x = -3f;
                    settingsInfoPanel.transform.localPosition = new Vector3(x, y2, -2f);
                    SetInfo(settingsInfoPanel, option.GetName(false), option.GetString());
                    window.settingsInfo.Add(settingsInfoPanel.gameObject);
                    y1 = y2 - 0.59f;
                    index++;
                }
                window.scrollBar.CalculateAndSetYBounds((float)(window.settingsInfo.Count + 10), 2f, 6f, 0.59f);
            }));

            ShowStgTMPButton.OnClick = new();
            ShowStgTMPButton.OnClick.AddListener((Action)(() =>
            {
                Main.ShowGameSettingsTMP.Value = !Main.ShowGameSettingsTMP.Value;
            }));
        }

        public static void SetHeader(CategoryHeaderMasked infoPane, string name)
        {
            infoPane.Title.text = name;
            infoPane.Background.material.SetInt(PlayerMaterial.MaskLayer, 61);
            if ((Object)infoPane.Divider != (Object)null)
                infoPane.Divider.material.SetInt(PlayerMaterial.MaskLayer, 61);
            infoPane.Title.fontMaterial.SetFloat("_StencilComp", 3f);
            infoPane.Title.fontMaterial.SetFloat("_Stencil", (float)61);
        }

        public static void SetInfo(ViewSettingsInfoPanel info, string title, string valueString)
        {
            info.titleText.text = title;
            info.settingText.text = valueString;
            info.disabledBackground.gameObject.SetActive(false);
            info.background.gameObject.SetActive(true);
            info.SetMaskLayer(61);

            info.titleText.enableWordWrapping = false;
            info.titleText.fontSizeMin = 0;
        }
        [HarmonyPatch(typeof(LobbyViewSettingsPane), nameof(LobbyViewSettingsPane.DrawRolesTab))]
        class LobbyViewSettingsPaneDrawRolesTabPatch
        {
            public static bool Prefix(LobbyViewSettingsPane __instance)
            {
                float y = 0.95f;
                float x1 = -6.53f;
                CategoryHeaderMasked categoryHeaderMasked1 = Object.Instantiate<CategoryHeaderMasked>(__instance.categoryHeaderOrigin);
                categoryHeaderMasked1.SetHeader(StringNames.RoleQuotaLabel, 61);
                categoryHeaderMasked1.transform.SetParent(__instance.settingsContainer);
                categoryHeaderMasked1.transform.localScale = Vector3.one;
                categoryHeaderMasked1.transform.localPosition = new Vector3(-9.77f, 1.26f, -2f);
                __instance.settingsInfo.Add(categoryHeaderMasked1.gameObject);
                List<CustomRoles> roleRulesCategoryList = new();
                for (int index1 = 0; index1 < 5; index1++)
                {
                    CategoryHeaderRoleVariant headerRoleVariant = Object.Instantiate(__instance.categoryHeaderRoleOrigin);
                    SetHeader(headerRoleVariant, GetString($"TabGroup.{(TabGroup)index1 + 1}"));
                    headerRoleVariant.transform.SetParent(__instance.settingsContainer);
                    headerRoleVariant.transform.localScale = Vector3.one;
                    headerRoleVariant.transform.localPosition = new Vector3(0.09f, y, -2f);
                    __instance.settingsInfo.Add(headerRoleVariant.gameObject);
                    y -= 0.696f;
                    for (int index2 = 0; index2 < Options.CustomRoleSpawnChances.Count; ++index2)
                    {
                        CustomRoles role = Options.CustomRoleSpawnChances.Keys.ToList()[index2];
                        if (!Event.CheckRole(role)) continue;
                        var info = role.GetRoleInfo();
                        var tab = (TabGroup)index1 + 1;
                        if ((info?.Tab != null) && tab == info.Tab)
                        {
                            int chancePerGame = Options.GetRoleChance(role);
                            int numPerGame = Options.GetRoleCount(role);
                            bool showDisabledBackground = numPerGame == 0;
                            ViewSettingsInfoPanelRoleVariant panelRoleVariant = Object.Instantiate(__instance.infoPanelRoleOrigin);
                            panelRoleVariant.transform.SetParent(__instance.settingsContainer);
                            panelRoleVariant.transform.localScale = Vector3.one;
                            panelRoleVariant.transform.localPosition = new Vector3(x1, y, -2f);
                            if (!showDisabledBackground)
                                roleRulesCategoryList.Add(role);
                            _ = ColorUtility.TryParseHtmlString("#696969", out Color ncolor);
                            var infoColor = (Color32)(tab == TabGroup.CrewmateRoles ? Palette.CrewmateRoleBlue : tab == TabGroup.NeutralRoles ? ncolor : Palette.ImpostorRoleRed);
                            var infoSprite = UtilsSprite.LoadSprite($"TownOfHost.Resources.Label.{role}.png", 30);
                            SetInfo(panelRoleVariant, GetString($"{role}"), numPerGame, chancePerGame, 61, infoColor, infoSprite, tab >= TabGroup.CrewmateRoles, showDisabledBackground);
                            __instance.settingsInfo.Add(panelRoleVariant.gameObject);
                            y -= 0.664f;
                        }
                    }
                }
                float sc = 0f;
                if (roleRulesCategoryList.Count > 0)
                {
                    CategoryHeaderMasked categoryHeaderMasked2 = Object.Instantiate<CategoryHeaderMasked>(__instance.categoryHeaderOrigin);
                    categoryHeaderMasked2.SetHeader(StringNames.RoleSettingsLabel, 61);
                    categoryHeaderMasked2.transform.SetParent(__instance.settingsContainer);
                    categoryHeaderMasked2.transform.localScale = Vector3.one;
                    categoryHeaderMasked2.transform.localPosition = new Vector3(-9.77f, y, -2f);
                    __instance.settingsInfo.Add(categoryHeaderMasked2.gameObject);
                    y -= 1.7f;
                    float num1 = 0.0f;
                    for (int index = 0; index < roleRulesCategoryList.Count; ++index)
                    {
                        float x2;
                        if (index % 2 == 0)
                        {
                            x2 = -5.8f;
                            if (index > 0)
                            {
                                y -= num1 + 0.59f;
                                num1 = 0.0f;
                            }
                        }
                        else
                            x2 = 0.149999619f;
                        AdvancedRoleViewPanel advancedRoleViewPanel = Object.Instantiate(__instance.advancedRolePanelOrigin);
                        advancedRoleViewPanel.transform.SetParent(__instance.settingsContainer);
                        advancedRoleViewPanel.transform.localScale = Vector3.one;
                        advancedRoleViewPanel.transform.localPosition = new Vector3(x2, y, -2f);
                        float num2 = SetUp(advancedRoleViewPanel, roleRulesCategoryList[index], 0.59f, 61);
                        if ((double)num2 > (double)num1)
                            num1 = num2;
                        __instance.settingsInfo.Add(advancedRoleViewPanel.gameObject);
                        var co = Options.CustomRoleSpawnChances[roleRulesCategoryList[index]].Children.ToArray().Length;
                        if (co > 4)
                        {
                            if (sc < (co - 4) * 0.5f)
                                sc = (co - 4) * 0.5f;
                        }
                    }
                }
                __instance.scrollBar.SetYBoundsMax((-y) + sc);
                return false;
            }
        }

        public static void SetInfo(
          ViewSettingsInfoPanelRoleVariant infoPanelRole,
          string name,
          int count,
          int chance,
          int maskLayer,
          Color32 color,
          Sprite roleIcon,
          bool crewmateTeam,
          bool showDisabledBackground = false)
        {
            infoPanelRole.titleText.text = name;
            infoPanelRole.titleText.enableWordWrapping = false;
            infoPanelRole.titleText.fontSizeMin = 0;
            infoPanelRole.settingText.text = count.ToString();
            infoPanelRole.chanceText.text = chance.ToString();
            infoPanelRole.iconSprite.sprite = roleIcon;
            infoPanelRole.iconSprite.transform.localPosition += new Vector3(-0.15f, 0f, 0f);
            if (showDisabledBackground)
            {
                infoPanelRole.titleText.color = Palette.White_75Alpha;
                infoPanelRole.chanceTitle.color = Palette.White_75Alpha;
                infoPanelRole.chanceBackground.sprite = infoPanelRole.disabledCube;
                infoPanelRole.background.sprite = infoPanelRole.disabledCube;
                infoPanelRole.labelBackground.color = Palette.DisabledGrey;
            }
            else
            {
                infoPanelRole.labelBackground.color = (Color)color;
                if (crewmateTeam)
                {
                    infoPanelRole.chanceBackground.sprite = infoPanelRole.crewmateCube;
                    infoPanelRole.background.sprite = infoPanelRole.crewmateCube;
                }
                else
                {
                    infoPanelRole.chanceBackground.sprite = infoPanelRole.impostorCube;
                    infoPanelRole.background.sprite = infoPanelRole.impostorCube;
                }
            }
            infoPanelRole.SetMaskLayer(maskLayer);
        }
        public static float SetUp(AdvancedRoleViewPanel view, CustomRoles role, float spacingY, int maskLayer)
        {
            SetHeader2(view.header, role);
            view.divider.material.SetInt(PlayerMaterial.MaskLayer, maskLayer);
            float yPosStart = view.yPosStart;
            float num1 = 1.08f;
            OptionItem[] all = Options.CustomRoleSpawnChances[role].Children.ToArray();
            for (int index = 0; index < all.Length; ++index)
            {
                OptionItem option = all[index];
                ViewSettingsInfoPanel settingsInfoPanel = Object.Instantiate<ViewSettingsInfoPanel>(view.infoPanelOrigin);
                settingsInfoPanel.transform.SetParent(view.transform);
                settingsInfoPanel.transform.localScale = Vector3.one;
                settingsInfoPanel.transform.localPosition = new Vector3(view.xPosStart, yPosStart, -2f);
                SetInfo(settingsInfoPanel, option.GetName(false), option.GetString());
                yPosStart -= spacingY;
                if (index > 0)
                    num1 += 0.8f;
            }
            return num1;
        }
        public static void SetHeader2(CategoryHeaderRoleVariant roleVariant, CustomRoles role)
        {
            SetHeader(roleVariant, GetString($"{role}"));
            _ = ColorUtility.TryParseHtmlString("#696969", out Color ncolor);
            var color = (Color32)(role.GetCustomRoleTypes() == CustomRoleTypes.Crewmate ? Palette.CrewmateRoleBlue : role.GetCustomRoleTypes() == CustomRoleTypes.Neutral ? ncolor : Palette.ImpostorRoleRed);
            roleVariant.Background.color = color;
            roleVariant.Divider.color = color;
        }
    }
}