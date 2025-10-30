using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using BepInEx.Unity.IL2CPP.Utils.Collections;

using TownOfHost.Roles.Core;
using static TownOfHost.Translator;
using Object = UnityEngine.Object;

namespace TownOfHost.Patches
{
    [HarmonyPatch(typeof(LobbyViewSettingsPane))]
    public class LobbyInfoPanePatch
    {
        private const float buttonX = 3.4938f;
        private const StringNames ModCategory = StringNames.None;
        public static PassiveButton settingsTabButton;
        public static Dictionary<CustomRoles, AdvancedRoleViewPanel> jumpToRoleMaps;

        public static Sprite GetRoleIcon(CustomRoles role)
        {
            if (role.IsVanilla()) return RoleManager.Instance.GetRole(role.GetRoleTypes()).RoleIconSolid;
            return UtilsSprite.LoadSprite($"TownOfHost.Resources.Label.{role}.png", 30);
        }
        public static string GetRoleText(CustomRoles role) => role.GetCombinationName(false).RemoveColorTags();
        public static int GetParentCount(OptionItem opt)
        {
            int num = 0;
            for (; opt?.Parent != null; opt = opt.Parent, num++) ;
            return num;
        }

        /// <returns>子オブジェクトがシングル設定 or マルチ設定かつ子設定のいずれかがON の場合にtrueを返す</returns>
        public static bool CheckChild(OptionItem opt, bool checkParent = false)
        {
            if (!checkParent && opt.Parent == null) return true;
            return opt.Children.Count <= 0 || (opt.GetBool() && opt.Children.Any(x => CheckChild(x)));
        }
        public static List<OptionItem> SortChildren(OptionItem opt)
            => [.. opt.Children.OrderBy(x => CheckChild(x, true)).ThenBy(x => x.Children.Count)];

        public static Color32 DarkenColor(Color32 c, float factor)
        {
            // factor: 暗くする割合 (0.0 = 真っ黒, 1.0 = 元の色)
            byte r = (byte)(c.r * factor);
            byte g = (byte)(c.g * factor);
            byte b = (byte)(c.b * factor);
            return new Color32(r, g, b, c.a); // alphaはそのまま
        }

        [HarmonyPatch(nameof(LobbyViewSettingsPane.Awake)), HarmonyPostfix]
        public static void AwakePostfix(LobbyViewSettingsPane __instance)
        {
            //ボタンを手前に移動させる
            __instance.taskTabButton.transform.SetLocalZ(-15f);
            __instance.rolesTabButton.transform.SetLocalZ(-15f);

            //MOD設定Viewを表示させるボタン
            settingsTabButton = Object.Instantiate(__instance.taskTabButton, __instance.transform);

            settingsTabButton.transform.localPosition += new Vector3(buttonX, 0f);
            __instance.rolesTabButton.transform.localPosition += new Vector3(buttonX, 0f);

            settingsTabButton.buttonText.DestroyTranslator();
            settingsTabButton.buttonText.text = "MOD";

            settingsTabButton.OnClick = new();
            settingsTabButton.OnClick.AddListener((Action)(() => __instance.ChangeTab(ModCategory)));

            ///設定TMPを表示させるかのトグル
            var ShowStgTMPButton = Object.Instantiate(HudManager.Instance.SettingsButton, __instance.transform).GetComponent<PassiveButton>();
            ShowStgTMPButton.GetComponent<AspectPosition>().DistanceFromEdge += new Vector3(-0.35f, 1.2f);
            ShowStgTMPButton.ClickSound = settingsTabButton.ClickSound;
            ShowStgTMPButton.OnClick = new();
            ShowStgTMPButton.OnClick.AddListener((Action)(() =>
            {
                Main.ShowGameSettingsTMP.Value = !Main.ShowGameSettingsTMP.Value;
            }));
        }

        [HarmonyPatch(nameof(LobbyViewSettingsPane.ChangeTab)), HarmonyPrefix]
        public static bool ChangeTabPrefix(LobbyViewSettingsPane __instance, [HarmonyArgument(0)] StringNames category)
        {
            if (category != ModCategory)
            {
                settingsTabButton.SelectButton(false);
                return true;
            }
            __instance.currentTab = category;

            for (int index = 0; index < __instance.settingsInfo.Count; ++index)
                Object.Destroy(__instance.settingsInfo[index].gameObject);
            __instance.settingsInfo.Clear();

            settingsTabButton.SelectButton(true);
            __instance.rolesTabButton.SelectButton(false);
            __instance.taskTabButton.SelectButton(false);
            __instance.scrollBar.ScrollToTop();

            __instance.StartCoroutine(DrawSettingsTab(__instance).WrapToIl2Cpp());
            return false;
        }

        [HarmonyPatch(nameof(LobbyViewSettingsPane.DrawRolesTab)), HarmonyPrefix]
        public static bool DrawRolesTabPrefix(LobbyViewSettingsPane __instance)
        {
            __instance.StartCoroutine(DrawRolesTab(__instance).WrapToIl2Cpp());
            return false;
        }

        public static IEnumerator DrawSettingsTab(LobbyViewSettingsPane __instance)
        {
            float y1 = 1.44f;
            float y2 = 0;
            int optionIndex = 0;

            List<OptionItem> options = new();

            CategoryHeaderMaskedPatch.CreateHeader(__instance, GetString("TabGroup.MainSettings"), new Vector3(-9.77f, y1, -2f), 61);
            y1 -= 1.05f;

            //シングルオプション
            for (var index = 0; index < OptionItem.AllOptions.Count; ++index)
            {
                var option = OptionItem.AllOptions[index];
                if (option.Tab != TabGroup.MainSettings || option.IsHiddenOn(Options.CurrentGameMode) || (!option?.Parent?.GetBool() ?? false)) continue;

                bool hasChildren = option.Children.Count > 0;

                if (!hasChildren && option.Parent == null)
                {
                    if (option.IsHeader)
                    {
                        bool isSingleHeader = index <= 0 || OptionItem.AllOptions[index - 1].IsHeader;
                        bool isNextSingleHeader = index + 2 < OptionItem.AllOptions.Count && OptionItem.AllOptions[index + 1].IsHeader && OptionItem.AllOptions[index + 2].IsHeader;
                        if (!(isSingleHeader && isNextSingleHeader))
                        {
                            optionIndex = 0;
                            y2 = y1 - 0.25f;
                        }
                    }
                    CreateSettingView(__instance, option, optionIndex, ref y2);
                    optionIndex++;
                    y1 = y2 - 0.85f;
                }
                else if (option.Parent == null)
                {
                    options.Add(option);
                }

                if (index % 1500 == 0) yield return null;
            }

            yield return DrawSubOptions(options);

            IEnumerator DrawSubOptions(List<OptionItem> subOptions)
            {
                //サブオプション持ちの処理 
                for (var index = 0; index < subOptions.Count; ++index)
                {
                    var option = subOptions[index];
                    if (option.IsHiddenOn(Options.CurrentGameMode) || (!option?.Parent?.GetBool() ?? false)) continue;
                    bool hasChildren = option.Children.Count > 0;

                    if (hasChildren && CheckChild(option))
                    {
                        if (!CheckChild(option, true)) continue; //全部子入り&全部OFFならスキップ

                        List<OptionItem> Children = SortChildren(option);
                        OptionItem nextOption = Children.First();
                        List<OptionItem> nextChildren = SortChildren(nextOption);

                        bool isChild = option.Parent != null;
                        bool isGroupChild = isChild && SortChildren(option.Parent).Count == 1;
                        bool isNextGroup = Children.Count == 1 && nextChildren.Count == 1;
                        bool hasNextChild = nextChildren.Count > 0 && CheckChild(nextOption, true);

                        float stepX = isGroupChild ? 3f : 0.05f;
                        float stepY = isNextGroup ? 0 : (hasNextChild ? 0.1f : 1.05f);
                        float parentCount = GetParentCount(option);

                        float x1 = -9.77f + (stepX * parentCount);
                        var header = CategoryHeaderMaskedPatch.CreateHeader(__instance, option.GetName(false), new Vector3(x1, y1, -2f), 61);
                        header.Background.color = DarkenColor(header.Background.color, 1 - (parentCount * 0.2f));

                        y2 = y1 - stepY;
                        optionIndex = 0;
                        y1 = y2 - 0.85f;

                        yield return DrawSubOptions(Children);
                    }
                    else
                    {
                        if (option.IsHeader)
                        {
                            bool isSingleHeader = index <= 0 || subOptions[index - 1].IsHeader;
                            bool isNextSingleHeader = index + 2 < subOptions.Count && subOptions[index + 1].IsHeader && subOptions[index + 2].IsHeader;
                            if (!(isSingleHeader && isNextSingleHeader))
                            {
                                optionIndex = 0;
                                y2 = y1 - 0.25f;
                            }
                        }
                        var view = CreateSettingView(__instance, option, optionIndex, ref y2);
                        var parentCount = GetParentCount(option.Parent);

                        view.transform.localPosition += new Vector3(parentCount * 0.1f, 0f);
                        view.labelBackground.color = DarkenColor(view.labelBackground.color, 1 - (parentCount * 0.15f));

                        optionIndex++;
                        y1 = y2 - 0.85f;
                    }

                    if (index % 1500 == 0) yield return null;
                }
            }

            float contentHeight = Mathf.Abs(y1 - (1.44f - 1.05f));
            __instance.scrollBar.SetYBoundsMax(contentHeight);
        }

        public static IEnumerator DrawRolesTab(LobbyViewSettingsPane __instance)
        {
            float y = 0.95f;
            float x1 = -6.53f;

            CategoryHeaderMaskedPatch.CreateHeader(__instance, GetString(StringNames.RoleQuotaLabel), new Vector3(-9.77f, 1.26f, -2f), 61);

            List<CustomRoles> activeRoles = new();
            var tabLength = EnumHelper.GetAllNames<TabGroup>().Length;

            for (int index1 = 1; index1 < tabLength; ++index1)
            {
                var tab = (TabGroup)index1;
                CategoryHeaderRoleVariantPatch.CreateHeader(__instance, GetString($"TabGroup.{tab}"), new Vector3(0.09f, y, -2f), 61);
                y -= 0.696f;
                foreach (var (role, option) in Options.CustomRoleCounts)
                {
                    if (option.Tab != tab || !Event.CheckRole(role)) continue;

                    int chancePerGame = Options.GetRoleChance(role);
                    int numPerGame = Options.GetRoleCount(role);
                    bool showDisabledBackground = numPerGame == 0;

                    Color32 infoColor = DarkenColor(UtilsRoleText.GetRoleColor(role, true), 0.5f);
                    var infoSprite = GetRoleIcon(role);

                    ViewSettingsInfoPanelRoleVariant panelRoleVariant = Object.Instantiate(__instance.infoPanelRoleOrigin);
                    panelRoleVariant.transform.SetParent(__instance.settingsContainer);
                    panelRoleVariant.transform.localScale = Vector3.one;
                    panelRoleVariant.transform.localPosition = new Vector3(x1, y, -2f);
                    panelRoleVariant.iconSprite.transform.localPosition += new Vector3(-0.15f, 0f, 0f);
                    if (!showDisabledBackground)
                    {
                        activeRoles.Add(role);
                        CreateJumpToRoleButton(__instance, panelRoleVariant, role, infoColor);
                    }
                    panelRoleVariant.SetInfo(GetRoleText(role), numPerGame, chancePerGame, 61, infoColor, infoSprite, index1 == 0, showDisabledBackground);
                    __instance.settingsInfo.Add(panelRoleVariant.gameObject);
                    y -= 0.664f;
                }
                if (__instance.settingsInfo.Count % 1000 == 0) yield return null;
            }

            if (activeRoles.Count > 0)
            {
                jumpToRoleMaps = new(activeRoles.Count);
                CategoryHeaderMaskedPatch.CreateHeader(__instance, GetString(StringNames.RoleSettingsLabel), new Vector3(-9.77f, y, -2f), 61);
                y -= 2.1f;
                float num1 = 0.0f;
                for (int index = 0; index < activeRoles.Count; ++index)
                {
                    bool isEven = (index & 1) == 0;
                    bool isLast = activeRoles.Count - 1 == index;
                    float x2 = isEven ? -5.8f : 0.149999619f;

                    if (isEven && index > 0)
                    {
                        y -= num1 + 0.85f;
                        num1 = 0f;
                    }

                    AdvancedRoleViewPanel advancedRoleViewPanel = Object.Instantiate(__instance.advancedRolePanelOrigin);
                    advancedRoleViewPanel.transform.SetParent(__instance.settingsContainer);
                    advancedRoleViewPanel.transform.localScale = Vector3.one;
                    advancedRoleViewPanel.transform.localPosition = new Vector3(x2, y, -2f);
                    float num2 = advancedRoleViewPanel.SetUp(activeRoles[index], 0.85f, 61);
                    if ((double)num2 > (double)num1)
                        num1 = num2;
                    __instance.settingsInfo.Add(advancedRoleViewPanel.gameObject);
                    if (index % 800 == 0) yield return null;

                    if (isLast) y -= num1;
                }
            }

            float scrollHeight = Mathf.Abs(y - 0.95f);
            __instance.scrollBar.SetYBoundsMax(scrollHeight);
        }

        private static ViewSettingsInfoPanel CreateSettingView(LobbyViewSettingsPane __instance, OptionItem option, int index, ref float y2)
        {
            ViewSettingsInfoPanel settingsInfoPanel = Object.Instantiate(__instance.infoPanelOrigin);
            settingsInfoPanel.transform.SetParent(__instance.settingsContainer);
            settingsInfoPanel.transform.localScale = Vector3.one;

            bool isEven = index % 2 == 0;
            float x = isEven ? -8.95f : -3f;
            if (isEven && index > 0)
                y2 -= 0.85f;

            settingsInfoPanel.transform.localPosition = new Vector3(x, y2, -2f);
            settingsInfoPanel.SetInfo(option.GetName(false), option.GetString(), 61);
            __instance.settingsInfo.Add(settingsInfoPanel.gameObject);
            return settingsInfoPanel;
        }

        private static void CreateJumpToRoleButton(LobbyViewSettingsPane __instance, ViewSettingsInfoPanelRoleVariant panelRoleVariant, CustomRoles role, Color color)
        {
            var labelBackground = panelRoleVariant.labelBackground;
            var button = labelBackground.gameObject.AddComponent<PassiveButton>();
            labelBackground.gameObject.AddComponent<BoxCollider2D>().autoTiling = true;

            button.OnMouseOut = new();
            button.OnMouseOut.AddListener((Action)(() => labelBackground.color = color));

            button.OnMouseOver = new();
            button.OnMouseOver.AddListener((Action)(() => labelBackground.color = DarkenColor(color, 1.5f)));

            button.OnClick = new();
            button.OnClick.AddListener((Action)(() =>
            {
                if (jumpToRoleMaps == null || !jumpToRoleMaps.TryGetValue(role, out var jumpTo)) return;
                // Town Of Host参考 https://github.com/tukasa0001/TownOfHost/blob/v5.1.14/Patches/GameOptionsMenuPatch.cs#L113
                __instance.scrollBar.velocity = Vector2.zero;  // ドラッグの慣性によるスクロールを止める
                var relativePosition = __instance.scrollBar.transform.InverseTransformPoint(jumpTo.transform.position);  // Scrollerのローカル空間における座標に変換
                __instance.scrollBar.Inner.localPosition = __instance.scrollBar.Inner.localPosition + Vector3.up * -relativePosition.y;  // 強制スクロール
                __instance.scrollBar.ScrollRelative(Vector2.zero);  // スクロール範囲内に収め，スクロールバーを更新する
            }));

            button.ClickSound = __instance.rolesTabButton.ClickSound;
        }
    }

    //[HarmonyPatch(typeof(CategoryHeaderMasked))]
    public static class CategoryHeaderMaskedPatch
    {
        public static void SetHeader(this CategoryHeaderMasked header, string title, int maskLayer)
        {
            header.SetHeader(StringNames.None, maskLayer);
            header.Title.text = title;
        }

        public static CategoryHeaderMasked CreateHeader(LobbyViewSettingsPane __instance, string title, Vector3 pos, int maskLayer)
        {
            CategoryHeaderMasked categoryHeaderMasked = Object.Instantiate(__instance.categoryHeaderOrigin);
            categoryHeaderMasked.SetHeader(title, maskLayer);
            categoryHeaderMasked.transform.SetParent(__instance.settingsContainer);
            categoryHeaderMasked.transform.localScale = Vector3.one;
            categoryHeaderMasked.transform.localPosition = pos;
            __instance.settingsInfo.Add(categoryHeaderMasked.gameObject);
            return categoryHeaderMasked;
        }
    }

    //[HarmonyPatch(typeof(ViewSettingsInfoPanel))]
    public static class ViewSettingsInfoPanelPatch
    {
        public static void SetInfo(this ViewSettingsInfoPanel viewPanel, string title, string valueString, int maskLayer)
        {
            viewPanel.SetInfo(StringNames.Name, valueString, maskLayer);
            viewPanel.titleText.text = title;
            viewPanel.titleText.enableWordWrapping = false;
            viewPanel.titleText.fontSizeMin = 0;
        }
    }

    //[HarmonyPatch(typeof(AdvancedRoleViewPanel))]
    public static class AdvancedRoleViewPanelPatch
    {
        public static float SetUp(this AdvancedRoleViewPanel viewPanel, CustomRoles role, float spacingY, int maskLayer)
        {
            float num1 = 1.08f;
            viewPanel.header.SetHeader(role, maskLayer);
            viewPanel.divider.material.SetInt(PlayerMaterial.MaskLayer, maskLayer);
            float yPosStart = viewPanel.yPosStart;
            var option = Options.CustomRoleSpawnChances[role];
            LobbyInfoPanePatch.jumpToRoleMaps[role] = viewPanel;

            num1 += viewPanel.SetUp(option, spacingY, ref yPosStart, maskLayer, 0);

            return num1;
        }

        public static float SetUp(this AdvancedRoleViewPanel viewPanel, OptionItem option, float spacingY, ref float yPosStart, int maskLayer, int depth = 0)
        {
            float num1 = 0;
            for (int index = 0; index < option.Children.Count; ++index)
            {
                OptionItem roleSetting = option.Children[index];
                ViewSettingsInfoPanel settingsInfoPanel = Object.Instantiate(viewPanel.infoPanelOrigin);
                settingsInfoPanel.transform.SetParent(viewPanel.transform);
                settingsInfoPanel.transform.localScale = Vector3.one;
                settingsInfoPanel.transform.localPosition = new Vector3(viewPanel.xPosStart, yPosStart, -2f);

                settingsInfoPanel.SetInfo(roleSetting.GetName(false), roleSetting.GetString(), maskLayer);
                yPosStart -= spacingY - (0.03f * depth);
                num1 += spacingY - (0.03f * depth);

                var labelBackground = settingsInfoPanel.labelBackground;
                labelBackground.transform.localScale -= new Vector3(0.01f * depth, 0.01f * depth);
                labelBackground.transform.localPosition += new Vector3(0.03f * depth, 0, 0.01f * depth);
                labelBackground.color = LobbyInfoPanePatch.DarkenColor(labelBackground.color, 1 - (0.1f * depth));

                if (roleSetting.Children.Count > 0)
                    num1 += viewPanel.SetUp(roleSetting, spacingY, ref yPosStart, maskLayer, depth + 1);
            }
            return num1;
        }
    }

    //[HarmonyPatch(typeof(CategoryHeaderRoleVariant))]
    public static class CategoryHeaderRoleVariantPatch
    {
        public static void SetHeader(this CategoryHeaderRoleVariant header, CustomRoles role, int maskLayer)
        {
            var roleColor = UtilsRoleText.GetRoleColor(role, true);
            var roleIcon = LobbyInfoPanePatch.GetRoleIcon(role);
            header.SetHeader(StringNames.None, maskLayer, false, roleIcon);

            header.Background.color = roleColor;
            header.Divider.color = roleColor;

            header.Title.text = LobbyInfoPanePatch.GetRoleText(role);
        }

        public static CategoryHeaderRoleVariant CreateHeader(LobbyViewSettingsPane __instance, string title, Vector3 pos, int maskLayer)
        {
            CategoryHeaderRoleVariant categoryHeaderMasked = Object.Instantiate(__instance.categoryHeaderRoleOrigin);
            categoryHeaderMasked.SetHeader(title, maskLayer);
            categoryHeaderMasked.transform.SetParent(__instance.settingsContainer);
            categoryHeaderMasked.transform.localScale = Vector3.one;
            categoryHeaderMasked.transform.localPosition = pos;
            __instance.settingsInfo.Add(categoryHeaderMasked.gameObject);
            return categoryHeaderMasked;
        }
    }
}