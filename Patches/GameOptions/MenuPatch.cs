using System;
using System.Collections.Generic;
using System.Linq;
using Il2CppSystem.Linq;
using HarmonyLib;
using UnityEngine;

using Object = UnityEngine.Object;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using static TownOfHost.Translator;
using static TownOfHost.GameSettingMenuStartPatch;

namespace TownOfHost
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSyncSettings))]
    public class RpcSyncSettingsPatch
    {
        public static void Postfix()
        {
            OptionItem.SyncAllOptions();
        }
    }
    [HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.Close))]
    class GameSettingMenuClosePatch
    {
        public static void Postfix()
        {
            ModSettingsButton = null;
            ModSettingsTab = null;
            activeonly = null;
            ActiveOnlyMode = false;
            priset = null;
            prisettext = null;
            search = null;
            searchtext = null;
            list = null;
            scOptions = null;
            crlist = null;
            crOptions = null;
            roleopts = new();
            rolebutton = new();
            roleInfobutton = new();
            ModoruTabu = (TabGroup.MainSettings, 0);
            timer = -100;
        }
    }


    [HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.Start))]
    class GameSettingMenuStartPatch
    {
        public static float w = 1;
        public static float h = 1;
        public static bool dasu;
        public static PassiveButton ModSettingsButton;
        public static RolesSettingsMenu ModSettingsTab;
        public static PassiveButton activeonly;
        public static bool ActiveOnlyMode;
        public static FreeChatInputField priset;
        public static TMPro.TextMeshPro prisettext;
        public static FreeChatInputField search;
        public static TMPro.TextMeshPro searchtext;
        static Il2CppSystem.Collections.Generic.List<PassiveButton> tabButtons;
        public static CustomRoles NowRoleTab;
        public static Dictionary<TabGroup, GameOptionsMenu> list = new();
        public static Dictionary<TabGroup, Il2CppSystem.Collections.Generic.List<OptionBehaviour>> scOptions = new();
        public static Dictionary<CustomRoles, GameOptionsMenu> crlist = new();
        public static Dictionary<CustomRoles, Il2CppSystem.Collections.Generic.List<OptionBehaviour>> crOptions = new();
        public static List<OptionItem> roleopts = new();
        public static Dictionary<CustomRoles, PassiveButton> rolebutton = new();
        public static Dictionary<CustomRoles, PassiveButton> roleInfobutton = new();
        public static (TabGroup, float) ModoruTabu;
        public static float timer;
        public static void Postfix(GameSettingMenu __instance)
        {
            timer = -100;
            ModoruTabu = (TabGroup.MainSettings, 0);
            roleopts = new();
            rolebutton = new();
            roleInfobutton = new();
            NowRoleTab = CustomRoles.NotAssigned;
            if (HudManager.Instance.TaskPanel.open)
            {
                HudManager.Instance.TaskPanel.ToggleOpen();
            }
            ActiveOnlyMode = false;
            var GamePresetButton = __instance.GamePresetsButton;
            var GameSettingsButton = __instance.GameSettingsButton;
            var RoleSettingsButton = __instance.RoleSettingsButton;

            ModSettingsButton = Object.Instantiate(RoleSettingsButton, RoleSettingsButton.transform.parent);
            activeonly = Object.Instantiate(GamePresetButton, __instance.RoleSettingsTab.transform.parent);

            activeonly.buttonText.text = "有効なMap設定/役職のみ表示する <size=5>(OFF)</size>";
            activeonly.gameObject.name = "ActiveOnly";

            activeonly.inactiveSprites.GetComponent<SpriteRenderer>().color =
            activeonly.activeSprites.GetComponent<SpriteRenderer>().color =
            activeonly.selectedSprites.GetComponent<SpriteRenderer>().color = ModColors.bluegreen;
            activeonly.buttonText.DestroyTranslator();

            GamePresetButton.gameObject.SetActive(false);
            RoleSettingsButton.gameObject.SetActive(false);

            ModSettingsButton.gameObject.name = "TownOfHostSetting";
            ModSettingsButton.buttonText.text = "TownOfHost-Kの設定";
            var activeSprite = ModSettingsButton.activeSprites.GetComponent<SpriteRenderer>();
            var selectedSprite = ModSettingsButton.selectedSprites.GetComponent<SpriteRenderer>();
            activeSprite.color = StringHelper.CodeColor(Main.ModColor);
            selectedSprite.color = StringHelper.CodeColor(Main.ModColor).ShadeColor(-0.2f);
            ModSettingsButton.buttonText.DestroyTranslator();//翻訳破壊☆

            activeonly.OnClick = new();
            activeonly.OnClick.AddListener((Action)(() =>
            {
                if (ModSettingsButton.selected)
                {
                    ActiveOnlyMode = !ActiveOnlyMode;
                    activeonly.inactiveSprites.GetComponent<SpriteRenderer>().color =
                    activeonly.activeSprites.GetComponent<SpriteRenderer>().color =
                    activeonly.selectedSprites.GetComponent<SpriteRenderer>().color = ActiveOnlyMode ? ModColors.GhostRoleColor : ModColors.bluegreen;
                    var now = ActiveOnlyMode ? "ON" : "OFF";
                    activeonly.buttonText.text = $"有効なMap設定/役職のみ表示する <size=5>({now})</size>";
                    activeonly.selected = false;
                    ModSettingsTab.scrollBar.velocity = Vector2.zero;
                    ModSettingsTab.scrollBar.Inner.localPosition = new Vector3(ModSettingsTab.scrollBar.Inner.localPosition.x, 0, ModSettingsTab.scrollBar.Inner.localPosition.z);
                    ModSettingsTab.scrollBar.ScrollRelative(Vector2.zero);
                }
            }));
            activeonly.gameObject.SetActive(false);

            ModSettingsTab = Object.Instantiate(__instance.RoleSettingsTab, __instance.RoleSettingsTab.transform.parent).GetComponent<RolesSettingsMenu>();

            if (priset == null)
            {
                try
                {
                    priset = Object.Instantiate(HudManager.Instance.Chat.freeChatField, __instance.RoleSettingsTab.transform.parent);
                    search = Object.Instantiate(HudManager.Instance.Chat.freeChatField, __instance.RoleSettingsTab.transform.parent);

                    prisettext = Object.Instantiate(HudManager.Instance.TaskPanel.taskText, priset.transform);
                    prisettext.text = "<size=120%><#cccccc><b>プリセット名編集</b></color></size>";
                    prisettext.transform.localPosition = new Vector3(-2f, -1.1f);
                    searchtext = Object.Instantiate(HudManager.Instance.TaskPanel.taskText, priset.transform);
                    searchtext.text = "<size=120%><#ffa826><b>検索</b></color></size>";
                    searchtext.transform.localPosition = new Vector3(-2f, -0.3f);
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex, "OptionsManager");
                }
            }
            priset.transform.localPosition = new Vector3(0f, 3.2f);
            priset.transform.localScale = new Vector3(0.4f, 0.4f, 0f);
            priset?.gameObject?.SetActive(true);
            priset.submitButton.OnPressed = (Action)(() =>
            {
                if (priset.textArea.text != "")
                {
                    var pr = OptionItem.AllOptions.Where(op => op.Id == 0).FirstOrDefault();
                    switch (pr.CurrentValue)
                    {
                        case 0: Main.Preset1.Value = priset.textArea.text; break;
                        case 1: Main.Preset2.Value = priset.textArea.text; break;
                        case 2: Main.Preset3.Value = priset.textArea.text; break;
                        case 3: Main.Preset4.Value = priset.textArea.text; break;
                        case 4: Main.Preset5.Value = priset.textArea.text; break;
                        case 5: Main.Preset6.Value = priset.textArea.text; break;
                        case 6: Main.Preset7.Value = priset.textArea.text; break;
                    }
                    priset.textArea.Clear();
                }
            });
            Dictionary<TabGroup, GameObject> menus = new();
            Dictionary<CustomRoles, GameObject> crmenus = new();

            __instance.GameSettingsTab.gameObject.SetActive(true);
            GameObject.Find("Main Camera/PlayerOptionsMenu(Clone)/MainArea/ROLES TAB(Clone)/Gradient").SetActive(false);

            var template = GameObject.Find("Main Camera/PlayerOptionsMenu(Clone)/MainArea/GAME SETTINGS TAB/Scroller/SliderInner/GameOption_String(Clone)").GetComponent<StringOption>();

            if (template == null) return;

            template = Object.Instantiate(template);
            {
                Vector3 pos = new();
                Vector3 scale = new();

                template.stringOptionName = AmongUs.GameOptions.Int32OptionNames.TaskBarMode;
                //Background
                var label = template.LabelBackground.transform;
                {
                    label.localScale = new Vector3(1.3f, 1.14f, 1f);
                    label.SetLocalX(-2.2695f * w);
                }
                //プラスボタン
                var plusButton = template.PlusBtn.transform;
                {
                    pos = plusButton.localPosition;
                    scale = plusButton.localScale;
                    plusButton.localScale = new Vector3(scale.x * w, scale.y * h);
                    plusButton.localPosition = new Vector3((pos.x + 1.1434f) * w, pos.y * h, pos.z);
                }
                //マイナスボタン
                var minusButton = template.MinusBtn.transform;
                {
                    pos = minusButton.localPosition;
                    scale = minusButton.localScale;
                    minusButton.localPosition = new Vector3((pos.x + 0.3463f) * w, (pos.y * h), pos.z);
                    minusButton.localScale = new Vector3(scale.x * w, scale.y * h);
                }
                //値を表示するテキスト
                var valueTMP = template.ValueText.transform;
                {
                    pos = valueTMP.localPosition;
                    valueTMP.localPosition = new Vector3((pos.x + 2.5f) * w, pos.y * h, pos.z);
                    scale = valueTMP.localScale;
                    valueTMP.localScale = new Vector3(scale.x * w, scale.y * h, scale.z);
                }
                //上のテキストを囲む箱(ﾀﾌﾞﾝ)
                var valueBox = template.transform.FindChild("ValueBox");
                {
                    pos = valueBox.localPosition;
                    valueBox.localPosition = new Vector3((pos.x + 0.7322f) * w, pos.y * h, pos.z);
                    scale = valueBox.localScale;
                    valueBox.localScale = new Vector3((scale.x + 0.2f) * w, scale.y * h, scale.z);
                }
                //タイトル(設定名)
                var titleText = template.TitleText;
                {
                    var transform = titleText.transform;
                    pos = transform.localPosition;
                    transform.localPosition = new Vector3((pos.x + -1.096f) * w, pos.y * h, pos.z);
                    scale = transform.localScale;
                    transform.localScale = new Vector3(scale.x * w, scale.y * h, scale.z);
                    titleText.rectTransform.sizeDelta = new Vector2(6.5f, 0.37f);
                    titleText.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
                    titleText.SetOutlineColor(Color.black);
                    titleText.SetOutlineThickness(0.125f);
                }
                template.OnValueChanged = new Action<OptionBehaviour>((o) => { });
            }
            list = new();
            scOptions = new();
            crlist = new();
            crOptions = new();

            var TabLength = EnumHelper.GetAllValues<TabGroup>().Length;

            for (var i = 0; i < TabLength; i++)
            {
                var tab = (TabGroup)i;
                var optionsMenu = new GameObject($"{tab}-Stg").AddComponent<GameOptionsMenu>();
                var transform = optionsMenu.transform;
                transform.SetParent(ModSettingsTab.AdvancedRolesSettings.transform.parent);
                transform.localPosition = new Vector3(0.7789f, -0.5101f);
                list.Add(tab, optionsMenu);
                scOptions[tab] = new();
            }
            foreach (var role in EnumHelper.GetAllValues<CustomRoles>())
            {
                var optionsMenu = new GameObject($"{role}-Stg").AddComponent<GameOptionsMenu>();
                var transform = optionsMenu.transform;
                transform.SetParent(ModSettingsTab.AdvancedRolesSettings.transform.parent);
                transform.localPosition = new Vector3(0.7789f, -0.5101f);
                crlist.Add(role, optionsMenu);
                crOptions[role] = new();
            }

            var LabelBackgroundSprite = UtilsSprite.LoadSprite($"TownOfHost.Resources.Label.LabelBackground.png");

            foreach (var option in OptionItem.AllOptions)
            {
                if (option.OptionBehaviour == null)
                {
                    var role = CustomRoles.NotAssigned;
                    var ropt = option;

                    while (ropt.Parent != null)
                    {
                        ropt = ropt.Parent;
                    }
                    role = ropt.CustomRole;
                    if (ropt == option) role = CustomRoles.NotAssigned;

                    if (role is not CustomRoles.NotAssigned)
                    {
                        var optionsMenu = crlist[role];
                        var stringOption = Object.Instantiate(template, optionsMenu.transform);
                        crOptions[role].Add(stringOption);
                        roleopts.Add(option);
                        stringOption.TitleText.text = $"<b>{option.Name}</b>";
                        stringOption.Value = stringOption.oldValue = option.CurrentValue;
                        stringOption.ValueText.text = option.GetString();
                        stringOption.name = option.Name;

                        stringOption.LabelBackground.sprite = LabelBackground.OptionLabelBackground(option.Name) ?? LabelBackgroundSprite;
                        if (option.HideValue)
                        {
                            stringOption.PlusBtn.transform.localPosition = new Vector3(100, 100, 100);
                            stringOption.MinusBtn.transform.localPosition = new Vector3(100, 100, 100);
                        }

                        var transform = stringOption.ValueText.transform;
                        var pos = transform.localPosition;
                        transform.localPosition = new Vector3((pos.x + 0.7322f) * w, pos.y * h, pos.z);
                        stringOption.SetClickMask(optionsMenu.ButtonClickMask);
                        option.OptionBehaviour = stringOption;
                    }
                    else
                    {
                        var optionsMenu = list[option.Tab];
                        var stringOption = Object.Instantiate(template, optionsMenu.transform);
                        scOptions[option.Tab].Add(stringOption);
                        stringOption.TitleText.text = $"<b>{option.Name}</b>";
                        stringOption.Value = stringOption.oldValue = option.CurrentValue;
                        stringOption.ValueText.text = option.GetString();
                        stringOption.name = option.Name;

                        stringOption.LabelBackground.sprite = LabelBackground.OptionLabelBackground(option.Name) ?? LabelBackgroundSprite;
                        if (option.HideValue)
                        {
                            stringOption.PlusBtn.transform.localPosition = new Vector3(100, 100, 100);
                            stringOption.MinusBtn.transform.localPosition = new Vector3(100, 100, 100);
                        }
                        if (option.CustomRole is not CustomRoles.NotAssigned and not CustomRoles.GM)
                        {
                            var button = Object.Instantiate(GameSettingsButton, stringOption.transform);
                            button.inactiveSprites.GetComponent<SpriteRenderer>().sprite =
                            button.selectedSprites.GetComponent<SpriteRenderer>().sprite = null;

                            button.OnClick = new();
                            button.buttonText.DestroyTranslator();
                            button.buttonText.text = " ";
                            button.gameObject.name = $"{option.Name}OptionButton";
                            button.transform.localPosition = new Vector3(-2.06f * w, 0.0446f, -2);
                            button.transform.localScale = new Vector3(1.64f * w, 1.14f * h, 1f);
                            button.activeSprites.GetComponent<SpriteRenderer>().sprite = LabelBackgroundSprite;
                            button.activeSprites.GetComponent<SpriteRenderer>().color = UtilsRoleText.GetRoleColor(option.CustomRole).ShadeColor(0.2f).SetAlpha(0.35f);

                            button.OnClick.AddListener((Action)(() =>
                            {
                                if (NowRoleTab is not CustomRoles.NotAssigned)
                                {
                                    var atabtitle = ModSettingsTab.transform.FindChild("Scroller/SliderInner/ChancesTab/CategoryHeaderMasked").GetComponent<CategoryHeaderMasked>();
                                    CategoryHeaderEditRole[] stabsubtitle = atabtitle.transform.parent.GetComponentsInChildren<CategoryHeaderEditRole>();
                                    atabtitle.Title.DestroyTranslator();
                                    atabtitle.Title.text = GetString("TabGroup." + ModoruTabu.Item1);

                                    atabtitle.Background.color = ModColors.Gray;
                                    atabtitle.Title.color = Color.white;
                                    NowRoleTab = CustomRoles.NotAssigned;
                                    menus[ModoruTabu.Item1].SetActive(true);
                                    foreach (var sub in stabsubtitle)
                                    {
                                        Object.Destroy(sub.gameObject);
                                    }
                                    ModSettingsTab.scrollBar.velocity = Vector2.zero;
                                    ModSettingsTab.scrollBar.ScrollRelative(Vector2.zero);
                                    ModSettingsTab.scrollBar.Inner.localPosition = new Vector3(ModSettingsTab.scrollBar.Inner.localPosition.x, ModoruTabu.Item2, ModSettingsTab.scrollBar.Inner.localPosition.z);
                                    return;
                                }
                                button.selected = false;
                                NowRoleTab = option.CustomRole;
                                ModoruTabu = (option.Tab, ModSettingsTab.scrollBar.Inner.localPosition.y);

                                menus[option.Tab].SetActive(false);
                                crmenus[option.CustomRole].SetActive(true);
                                var tabtitle = ModSettingsTab.transform.FindChild("Scroller/SliderInner/ChancesTab/CategoryHeaderMasked").GetComponent<CategoryHeaderMasked>();
                                CategoryHeaderEditRole[] tabsubtitle = tabtitle.transform.parent.GetComponentsInChildren<CategoryHeaderEditRole>();
                                tabtitle.Title.DestroyTranslator();
                                Color.RGBToHSV(UtilsRoleText.GetRoleColor(option.CustomRole, true), out var h, out var s, out var v);
                                if (v < 0.6f)
                                {
                                    v = 0.6f;
                                }
                                var rolecolor = Color.HSVToRGB(h, s, v);
                                tabtitle.Title.text = Utils.ColorString(rolecolor, GetString(option.CustomRole.ToString()));
                                tabtitle.Title.color = Color.white;
                                var type = option.CustomRole.GetCustomRoleTypes();
                                Color color = ModColors.CrewMateBlue;

                                switch (type)
                                {
                                    case CustomRoleTypes.Impostor: color = ModColors.ImpostorRed; break;
                                    case CustomRoleTypes.Madmate: color = ModColors.MadMateOrenge; break;
                                    case CustomRoleTypes.Neutral: color = ModColors.NeutralGray; break;
                                    case CustomRoleTypes.Crewmate:
                                        color = ModColors.CrewMateBlue;
                                        if (option.CustomRole.IsAddOn()) color = ModColors.AddonsColor;
                                        if (option.CustomRole.IsGhostRole()) color = ModColors.GhostRoleColor;
                                        if (option.CustomRole.IsRiaju()) color = UtilsRoleText.GetRoleColor(option.CustomRole);
                                        break;
                                }

                                tabtitle.Background.color = color.ShadeColor(0.7f);

                                ModSettingsTab.scrollBar.velocity = Vector2.zero;
                                ModSettingsTab.scrollBar.Inner.localPosition = new Vector3(ModSettingsTab.scrollBar.Inner.localPosition.x, 0, ModSettingsTab.scrollBar.Inner.localPosition.z);
                                ModSettingsTab.scrollBar.ScrollRelative(Vector2.zero);
                                foreach (var sub in tabsubtitle)
                                {
                                    Object.Destroy(sub.gameObject);
                                }
                            }));
                            rolebutton.Add(option.CustomRole, button);

                            {
                                var infobutton = Object.Instantiate(stringOption.MinusBtn, stringOption.transform);
                                {
                                    infobutton.gameObject.name = $"{option.Name}-InfoButton";
                                    infobutton.transform.FindChild("Text_TMP").GetComponent<TMPro.TextMeshPro>().text = "?";
                                    infobutton.OnClick = new();
                                    infobutton.OnClick.AddListener((Action)(() =>
                                    {
                                        HudManager.Instance.TaskPanel.ToggleOpen();
                                    }));
                                    infobutton.gameObject.transform.SetLocalX(0);
                                    infobutton.gameObject.transform.SetLocalZ(-50);

                                    roleInfobutton.Add(option.CustomRole, infobutton);
                                }
                            }
                        }
                        var transform = stringOption.ValueText.transform;
                        var pos = transform.localPosition;
                        transform.localPosition = new Vector3((pos.x + 0.7322f) * w, pos.y * h, pos.z);
                        stringOption.SetClickMask(optionsMenu.ButtonClickMask);
                        option.OptionBehaviour = stringOption;
                    }
                }
                option.OptionBehaviour.gameObject.active = true;
            }

            var templateTabButton = ModSettingsTab.AllButton;
            {
                Object.Destroy(templateTabButton.buttonText.gameObject);
            }

            ModSettingsTab.roleTabs = new();
            tabButtons = new();

            for (var i = 0; i < TabLength; i++)
            {
                var tab = (TabGroup)i;
                var tabs = list[tab];
                tabs.Children = scOptions[tab];
                tabs.gameObject.SetActive(false);
                tabs.enabled = true;
                menus.Add(tab, tabs.gameObject);

                var tabButton = Object.Instantiate(templateTabButton, templateTabButton.transform.parent);
                tabButton.name = tab.ToString();
                tabButton.transform.position = templateTabButton.transform.position + new Vector3((0.762f * i * 0.8f) + (0.762f * i * w * 0.2f), 0, -300f);
                tabButton.inactiveSprites.GetComponent<SpriteRenderer>().sprite = UtilsSprite.LoadSprite($"TownOfHost.Resources.Tab.TabIcon_{tab}.png", 60);
                tabButton.activeSprites.GetComponent<SpriteRenderer>().sprite = UtilsSprite.LoadSprite($"TownOfHost.Resources.Tab.TabIcon_S_{tab}.png", 120);
                tabButton.selectedSprites.GetComponent<SpriteRenderer>().sprite = UtilsSprite.LoadSprite($"TownOfHost.Resources.Tab.TabIcon_{tab}.png", 120);

                tabButtons.Add(tabButton);
            }

            foreach (var role in EnumHelper.GetAllValues<CustomRoles>())
            {
                var tabs = crlist[role];
                tabs.Children = crOptions[role];
                tabs.gameObject.SetActive(false);
                tabs.enabled = true;
                crmenus.Add(role, tabs.gameObject);
            }

            //一旦全部作ってから
            for (var i = 0; i < TabLength; i++)
            {
                var tab = (TabGroup)i;
                var tabButton = tabButtons[i];
                if (tabButton == null) continue;

                tabButton.OnClick = new();
                tabButton.OnClick.AddListener((Action)(() =>
                {
                    for (var i = 0; i < TabLength; i++)
                    {
                        var n = (TabGroup)i;
                        var tabButton = tabButtons[i];
                        if (tab != n) menus[n].SetActive(false);
                        tabButton.SelectButton(false);
                        tabButton.selectedSprites.GetComponent<SpriteRenderer>().sprite = UtilsSprite.LoadSprite($"TownOfHost.Resources.Tab.TabIcon_{n}.png", 120);
                    }
                    crmenus[NowRoleTab].SetActive(false);
                    NowRoleTab = CustomRoles.NotAssigned;
                    tabButton.SelectButton(true);
                    tabButton.selectedSprites.GetComponent<SpriteRenderer>().sprite = UtilsSprite.LoadSprite($"TownOfHost.Resources.Tab.TabIcon_S_{tab}.png", 120);
                    menus[tab].SetActive(true);
                    var tabtitle = ModSettingsTab.transform.FindChild("Scroller/SliderInner/ChancesTab/CategoryHeaderMasked").GetComponent<CategoryHeaderMasked>();
                    CategoryHeaderEditRole[] tabsubtitle = tabtitle.transform.parent.GetComponentsInChildren<CategoryHeaderEditRole>();
                    tabtitle.Title.DestroyTranslator();
                    tabtitle.Title.text = GetString("TabGroup." + tab);

                    tabtitle.Background.color = ModColors.Gray;
                    tabtitle.Title.color = Color.white;

                    ModSettingsTab.scrollBar.velocity = Vector2.zero;
                    ModSettingsTab.scrollBar.Inner.localPosition = new Vector3(ModSettingsTab.scrollBar.Inner.localPosition.x, 0, ModSettingsTab.scrollBar.Inner.localPosition.z);
                    ModSettingsTab.scrollBar.ScrollRelative(Vector2.zero);
                    foreach (var sub in tabsubtitle)
                    {
                        Object.Destroy(sub.gameObject);
                    }
                }));

                ModSettingsTab.roleTabs.Add(tabButton);
            }

            search.transform.localPosition = new Vector3(0f, 3.5f);
            search.transform.localScale = new Vector3(0.4f, 0.4f, 0f);
            search?.gameObject?.SetActive(true);
            search.submitButton.OnPressed = (Action)(() =>
            {
                bool ch = false;
                List<OptionItem> subopt = new();
                foreach (var op in OptionItem.AllOptions)
                {
                    var name = op.GetName().RemoveHtmlTags();

                    if (name == search.textArea.text)
                    {
                        scroll(op);
                        ch = true;
                        break;
                    }

                    if (name.Contains(search.textArea.text))
                    {
                        subopt.Add(op);
                        break;
                    }
                }

                //不必要なループをなくしてみる
                if (!ch)
                {
                    foreach (var op in subopt)
                    {
                        scroll(op);
                        break;
                    }
                }
                search.textArea.Clear();

                //スクロール処理
                void scroll(OptionItem op)
                {
                    var opt = op;
                    while (opt.Parent != null && (!opt.GetBool() || roleopts.Contains(opt)))
                    {
                        opt = opt.Parent;
                    }

                    int tabIndex = (int)opt.Tab;

                    if (tabIndex >= 0 && tabIndex < tabButtons.Count && tabButtons[tabIndex] != null)
                    {
                        tabButtons[tabIndex].OnClick.Invoke();
                    }

                    _ = new LateTask(() =>
                    {
                        if (!(ModSettingsTab?.gameObject?.active ?? false)) return;
                        ModSettingsTab.scrollBar.velocity = Vector2.zero;
                        var relativePosition = ModSettingsTab.scrollBar.transform.InverseTransformPoint(opt.OptionBehaviour.transform.FindChild("Title Text").transform.position);// Scrollerのローカル空間における座標に変換
                        var scrollAmount = 1 - relativePosition.y;
                        ModSettingsTab.scrollBar.Inner.localPosition = ModSettingsTab.scrollBar.Inner.localPosition + Vector3.up * scrollAmount;  // 強制スクロール
                        ModSettingsTab.scrollBar.ScrollRelative(Vector2.zero);
                    }, 0.1f, "", true);
                }
            });

            ModSettingsButton.OnClick = new();
            ModSettingsButton.OnClick.AddListener((Action)(() =>
            {
                __instance.ChangeTab(3, false);

                if (dasu)
                {
                    _ = new LateTask(() =>
                    {
                        if (!(ModSettingsTab?.gameObject?.active ?? false)) return;
                        dasu = false;
                        if (tabButtons[0] != null)
                            tabButtons[0].GetComponent<PassiveButton>().OnClick.Invoke();
                    }, 0.05f, "", true);
                }
            }));

            __instance.GameSettingsTab.gameObject.SetActive(false);

            // ボタン生成
            CreateButton("OptionReset", Color.red, new Vector2(8.5f, 0f), new Action(() =>
            {
                OptionItem.AllOptions.ToArray().Where(x => x.Id > 0 && x.Id is not 2 and not 3 && 1_000_000 > x.Id && x.CurrentValue != x.DefaultValue).Do(x => x.SetValue(x.DefaultValue));
                var pr = OptionItem.AllOptions.Where(op => op.Id == 0).FirstOrDefault();
                switch (pr.CurrentValue)
                {
                    case 0: Main.Preset1.Value = GetString("Preset_1"); break;
                    case 1: Main.Preset2.Value = GetString("Preset_2"); break;
                    case 2: Main.Preset3.Value = GetString("Preset_3"); break;
                    case 3: Main.Preset4.Value = GetString("Preset_4"); break;
                    case 4: Main.Preset5.Value = GetString("Preset_5"); break;
                    case 5: Main.Preset6.Value = GetString("Preset_6"); break;
                    case 6: Main.Preset7.Value = GetString("Preset_7"); break;
                }
                GameSettingMenuChangeTabPatch.meg = GetString("OptionResetMeg");
                timer = 3;
            }), UtilsSprite.LoadSprite("TownOfHost.Resources.RESET-STG.png", 150f));
            CreateButton("OptionCopy", Color.green, new Vector2(7.89f, 0), new Action(() =>
            {
                OptionSerializer.SaveToClipboard();
                GameSettingMenuChangeTabPatch.meg = GetString("OptionCopyMeg");
                timer = 3;
            }), UtilsSprite.LoadSprite("TownOfHost.Resources.COPY-STG.png", 180f), true);
            CreateButton("OptionLoad", Color.green, new Vector2(7.28f, 0), new Action(() =>
            {
                OptionSerializer.LoadFromClipboard();
                GameSettingMenuChangeTabPatch.meg = GetString("OptionLoadMeg");
                timer = 3;
            }), UtilsSprite.LoadSprite("TownOfHost.Resources.LOAD-STG.png", 180f));

            void CreateButton(string text, Color color, Vector2 position, Action action, Sprite sprite = null, bool csize = false)
            {
                var ToggleButton = Object.Instantiate(HudManager.Instance.SettingsButton.GetComponent<PassiveButton>(), GameObject.Find("Main Camera/PlayerOptionsMenu(Clone)").transform);
                ToggleButton.GetComponent<AspectPosition>().DistanceFromEdge += new Vector3(position.x, 0, 200f);
                ToggleButton.transform.localScale -= new Vector3(0.25f * w, 0.25f * h);
                ToggleButton.name = text;
                if (sprite != null)
                {
                    ToggleButton.inactiveSprites.GetComponent<SpriteRenderer>().sprite = sprite;
                    ToggleButton.activeSprites.GetComponent<SpriteRenderer>().sprite = sprite;
                    ToggleButton.selectedSprites.GetComponent<SpriteRenderer>().sprite = sprite;
                }
                var textTMP = new GameObject("Text_TMP").AddComponent<TMPro.TextMeshPro>();
                textTMP.text = Utils.ColorString(color, GetString(text));
                textTMP.transform.SetParent(ToggleButton.transform);
                textTMP.transform.localPosition = new Vector3(0.8f, 0.8f);
                textTMP.transform.localScale = new Vector3(0, -0.5f);
                textTMP.alignment = TMPro.TextAlignmentOptions.Top;
                textTMP.fontSize = 10f;
                ToggleButton.OnClick = new();
                ToggleButton.OnClick.AddListener(action);
            }
        }
    }

    [HarmonyPatch(typeof(StringOption), nameof(StringOption.FixedUpdate))]
    class PrisetNamechengePatch
    {
        public static void Postfix(StringOption __instance)
        {
            if (ModSettingsTab == null) return;

            var option = PresetOptionItem.Preset;
            if (option == null) return;
            if (option.OptionBehaviour != __instance) return;

            __instance.ValueText.text = option.GetString();
        }
    }
    [HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.Start))]
    class Prisetkesu
    {
        public static void Postfix(GameSettingMenu __instance)
        {
            __instance.ChangeTab(1, false);
            GameSettingMenuChangeTabPatch.Hima = 0;
        }
    }
    class LabelBackground
    {
        public static Sprite OptionLabelBackground(string OptionName)
        {
            var path = "TownOfHost.Resources.Label.";
            var la = "LabelBackground.png";
            return OptionName switch
            {
                "MapModification" => UtilsSprite.LoadSprite($"{path}MapModification{la}"),
                "MadmateOption" => UtilsSprite.LoadSprite($"{path}MadmateOption{la}"),
                "Sabotage" => UtilsSprite.LoadSprite($"{path}Sabotage{la}"),
                "RandomSpawn" => UtilsSprite.LoadSprite($"{path}RandomSpawn{la}"),
                "Preset" => UtilsSprite.LoadSprite($"{path}Preset{la}"),
                "GameMode" => UtilsSprite.LoadSprite($"{path}GameMode{la}"),
                "Shyboy" => UtilsSprite.LoadSprite($"{path}Shyboy{la}"),
                "MadTeller" => UtilsSprite.LoadSprite($"{path}Madteller{la}"),
                "PonkotuTeller" => UtilsSprite.LoadSprite($"{path}PonkotuTeller{la}"),
                "FortuneTeller" => UtilsSprite.LoadSprite($"{path}FortuneTeller{la}"),
                "AmateurTeller" => UtilsSprite.LoadSprite($"{path}AmateurTeller{la}"),
                "NiceAddoer" => UtilsSprite.LoadSprite($"{path}NiceAddoer{la}"),
                "EvilAddoer" => UtilsSprite.LoadSprite($"{path}EvilAddoer{la}"),
                "Alien" => UtilsSprite.LoadSprite($"{path}Alien{la}"),
                "JackalAlien" => UtilsSprite.LoadSprite($"{path}JackalAlien{la}"),
                "DevicesOption" => UtilsSprite.LoadSprite($"{path}Device{la}"),
                "Jumper" => UtilsSprite.LoadSprite($"{path}Jumper{la}"),
                "LadderDeath" => UtilsSprite.LoadSprite($"{path}LadderDeath{la}"),
                "ONspecialMode" => UtilsSprite.LoadSprite($"{path}ONspecialMode{la}"),
                "UltraStar" => UtilsSprite.LoadSprite($"{path}UltraStar{la}"),
                _ => null,
            };
        }
    }
}