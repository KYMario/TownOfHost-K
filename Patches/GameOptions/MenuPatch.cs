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
            OptionShower.Update = true;
            OptionItem.SyncAllOptions();
        }
    }
    [HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.Close))]
    class GameSettingMenuClosePatch
    {
        public static bool Prefix() => ShowFilter.CallEsc(true) is false;
        public static void Postfix()
        {
            if (ShowFilter.CallEsc()) return;
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
            IsClick = false;
            ModoruTabu = (TabGroup.MainSettings, 0);
            timer = -100;
            tabGenerated = null;
            ShowFilter.CloseOptionMenu();
        }
    }

    [HarmonyPatch(typeof(GameOptionsMenu))]
    class GameOptionsMenuInitializePatch
    {
        public static bool CheckModMenu(GameOptionsMenu __instance) => __instance.name.IndexOf("-Stg".AsSpan().ToString(), StringComparison.Ordinal) > 0;
        [HarmonyPrefix]
        [HarmonyPatch(nameof(GameOptionsMenu.Awake))]
        [HarmonyPatch(nameof(GameOptionsMenu.CloseMenu))]
        public static bool CheckPrefix(GameOptionsMenu __instance)
            => !CheckModMenu(__instance);

        [HarmonyPatch(nameof(GameOptionsMenu.Initialize)), HarmonyPrefix]
        public static bool InitializePrefix(GameOptionsMenu __instance)
        {
            if (!CheckModMenu(__instance)) return true;
            __instance.cachedData = GameOptionsManager.Instance.CurrentGameOptions;
            return false;
        }
    }

    [HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.Start))]
    class GameSettingMenuStartPatch
    {
        public static bool ShowModSetting;
        public static PassiveButton ModSettingsButton;
        public static RolesSettingsMenu ModSettingsTab;
        public static PassiveButton activeonly;
        public static bool ActiveOnlyMode;
        public static FreeChatInputField priset;
        public static TMPro.TextMeshPro prisettext;
        public static FreeChatInputField search;
        public static TMPro.TextMeshPro searchtext;
        public static Il2CppSystem.Collections.Generic.List<PassiveButton> tabButtons;
        public static CustomRoles NowRoleTab;
        public static CustomRoles Nowinfo;
        public static Dictionary<TabGroup, GameOptionsMenu> list = new();
        public static Dictionary<TabGroup, Il2CppSystem.Collections.Generic.List<OptionBehaviour>> scOptions = new();
        public static Dictionary<CustomRoles, GameOptionsMenu> crlist = new();
        public static Dictionary<CustomRoles, Il2CppSystem.Collections.Generic.List<OptionBehaviour>> crOptions = new();
        public static List<OptionItem> roleopts = new();
        public static Dictionary<CustomRoles, PassiveButton> rolebutton = new();
        public static Dictionary<CustomRoles, PassiveButton> roleInfobutton = new();
        public static (TabGroup, float) ModoruTabu;
        public static float timer;
        public static bool IsClick = false;
        public static TMPro.TextMeshPro InfoTimer;
        public static TMPro.TextMeshPro InfoCount;
        public static StringOption BaseOption;
        public static HashSet<TabGroup> tabGenerated;

        public static void Postfix(GameSettingMenu __instance)
        {
            var ErrorNumber = 0;
            PassiveButton settingsButton = null;

            try
            {
                var size = __instance.transform.localScale;
                __instance.transform.localScale = new(1, 1, 1);
                timer = -100;
                ModoruTabu = (TabGroup.MainSettings, 0);
                roleopts = new();
                rolebutton = new();
                roleInfobutton = new();
                NowRoleTab = CustomRoles.NotAssigned;
                tabGenerated = new();
                if (HudManager.Instance?.TaskPanel?.open is true)
                {
                    HudManager.Instance.TaskPanel.ToggleOpen();
                }
                else if (HudManager.Instance?.TaskPanel?.open is null)
                {
                    Logger.Error("HudManagerがnull!", "OptionMenu");
                }
                ActiveOnlyMode = false;
                var GamePresetButton = __instance.GamePresetsButton;
                var GameSettingsButton = __instance.GameSettingsButton;
                var RoleSettingsButton = __instance.RoleSettingsButton;

                ModSettingsButton = Object.Instantiate(RoleSettingsButton, RoleSettingsButton.transform.parent);
                activeonly = Object.Instantiate(GamePresetButton, __instance.RoleSettingsTab.transform.parent);

                ErrorNumber = 1;
                if (activeonly)
                {
                    activeonly.buttonText.text = $"{GetString("ActiveOptionOnly")} <size=5>(OFF)</size>";
                    activeonly.gameObject.name = "ActiveOnly";

                    activeonly.inactiveSprites.GetComponent<SpriteRenderer>().color =
                    activeonly.activeSprites.GetComponent<SpriteRenderer>().color =
                    activeonly.selectedSprites.GetComponent<SpriteRenderer>().color = ModColors.bluegreen;
                    activeonly.buttonText.DestroyTranslator();
                }

                GamePresetButton.transform.localScale = new(0.45f, 0.45f);
                GamePresetButton.transform.localPosition = new Vector3(-3.76f, -0.62f, -2);

                GamePresetButton.OnClick = new();
                GamePresetButton.OnClick.AddListener((Action)(() =>
                {
                    IsClick = true;
                    __instance.ChangeTab(0, false);
                }));

                RoleSettingsButton.gameObject.SetActive(false);

                ModSettingsButton.gameObject.name = "TownOfHostSetting";
                ModSettingsButton.buttonText.text = "TownOfHost-K";
                var activeSprite = ModSettingsButton.activeSprites.GetComponent<SpriteRenderer>();
                var selectedSprite = ModSettingsButton.selectedSprites.GetComponent<SpriteRenderer>();
                activeSprite.color = StringHelper.CodeColor(Main.ModColor);
                selectedSprite.color = StringHelper.CodeColor(Main.ModColor).ShadeColor(-0.2f);
                ModSettingsButton.buttonText.DestroyTranslator();//翻訳破壊☆

                ModSettingsButton.OnMouseOver.AddListener((Action)(() => { if (Controller.currentTouchType == Controller.TouchType.Joystick) __instance.ChangeTab(3, true); }));
                ControllerManager.Instance.CurrentUiState.SelectableUiElements.Add(ModSettingsButton);

                ErrorNumber = 2;
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
                        activeonly.buttonText.text = $"{GetString("ActiveOptionOnly")} <size=5>({now})</size>";
                        activeonly.selected = false;
                        ModSettingsTab.scrollBar.velocity = Vector2.zero;
                        ModSettingsTab.scrollBar.Inner.localPosition = new Vector3(ModSettingsTab.scrollBar.Inner.localPosition.x, 0, ModSettingsTab.scrollBar.Inner.localPosition.z);
                        ModSettingsTab.scrollBar.ScrollRelative(Vector2.zero);
                    }
                }));
                activeonly.gameObject.SetActive(false);

                ModSettingsTab = Object.Instantiate(__instance.RoleSettingsTab, __instance.RoleSettingsTab.transform.parent).GetComponent<RolesSettingsMenu>();
                var backButton = ModSettingsTab.BackButton.Cast<PassiveButton>();
                backButton.OnClick = new();
                backButton.OnClick.AddListener((Action)(() => { ModSettingsTab.CloseMenu(); __instance.ChangeTab(3, true); }));

                if (priset == null)
                {
                    try
                    {
                        priset = Object.Instantiate(HudManager.Instance.Chat.freeChatField, __instance.RoleSettingsTab.transform.parent);
                        search = Object.Instantiate(HudManager.Instance.Chat.freeChatField, __instance.RoleSettingsTab.transform.parent);

                        prisettext = Object.Instantiate(HudManager.Instance.TaskPanel.taskText, priset.transform);
                        prisettext.text = $"<size=120%><#cccccc><b>{GetString("SetPresetName")}</b></color></size>";
                        prisettext.transform.localPosition = new Vector3(-2f, -1.1f);
                        searchtext = Object.Instantiate(HudManager.Instance.TaskPanel.taskText, priset.transform);
                        searchtext.text = $"<size=120%><#ffa826><b>{GetString("Search")}</b></color></size>";
                        searchtext.transform.localPosition = new Vector3(-2f, -0.3f);
                        priset.name = "PresetSet";
                        search.name = "SearchSet";
                    }
                    catch (Exception ex)
                    {
                        Logger.Exception(ex, "OptionsManager");
                    }
                }

                ErrorNumber = 3;
                if (priset)
                {
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
                }
                else { Logger.Error("prisetでError!", "MenuPatch"); }
                Dictionary<TabGroup, GameObject> menus = new();
                Dictionary<CustomRoles, GameObject> crmenus = new();

                __instance?.GameSettingsTab?.gameObject?.SetActive(true);
                GameObject.Find("Main Camera/PlayerOptionsMenu(Clone)/MainArea/ROLES TAB(Clone)/Gradient")?.SetActive(false);

                BaseOption = GameObject.Find("Main Camera/PlayerOptionsMenu(Clone)/MainArea/GAME SETTINGS TAB/Scroller/SliderInner/GameOption_String(Clone)")?.GetComponent<StringOption>();

                ErrorNumber = 4;

                list = new();
                scOptions = new();
                crlist = new();
                crOptions = new();

                var TabLength = EnumHelper.GetAllValues<TabGroup>().Length;

                ErrorNumber = 5;
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

                ErrorNumber = 6;

                ErrorNumber = 7;
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
                    tabButton.transform.position = templateTabButton.transform.position + new Vector3((0.762f * i * 0.8f) + (0.762f * i * 0.2f), 0, -300f);
                    tabButton.inactiveSprites.GetComponent<SpriteRenderer>().sprite = UtilsSprite.LoadSprite($"TownOfHost.Resources.Tab.TabIcon_{tab}.png", 60);
                    tabButton.activeSprites.GetComponent<SpriteRenderer>().sprite = UtilsSprite.LoadSprite($"TownOfHost.Resources.Tab.TabIcon_S_{tab}.png", 120);
                    tabButton.selectedSprites.GetComponent<SpriteRenderer>().sprite = UtilsSprite.LoadSprite($"TownOfHost.Resources.Tab.TabIcon_{tab}.png", 120);

                    tabButtons.Add(tabButton);
                }
                ErrorNumber = 8;

                foreach (var role in EnumHelper.GetAllValues<CustomRoles>())
                {
                    var tabs = crlist[role];
                    tabs.Children = crOptions[role];
                    tabs.gameObject.SetActive(false);
                    tabs.enabled = true;
                    crmenus.Add(role, tabs.gameObject);
                }

                ErrorNumber = 9;
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

                        CreateOptions(tab, menus, crmenus, __instance.RoleSettingsTab.transform.parent);
                    }));

                    ModSettingsTab.roleTabs.Add(tabButton);
                }

                ErrorNumber = 10;
                if (search)
                {
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
                }
                ErrorNumber = 11;

                ModSettingsButton.OnClick = new();
                ModSettingsButton.OnClick.AddListener((Action)(() =>
                {
                    __instance.ChangeTab(3, false);

                    if (ShowModSetting)
                    {
                        _ = new LateTask(() =>
                        {
                            if (!(ModSettingsTab?.gameObject?.active ?? false)) return;
                            ShowModSetting = false;
                            if (tabButtons[0] != null)
                                tabButtons[0].GetComponent<PassiveButton>().OnClick.Invoke();
                        }, 0.05f, "", true);
                    }
                }));

                ErrorNumber = 12;
                __instance.GameSettingsTab.gameObject.SetActive(false);

                settingsButton = HudManager.Instance.SettingsButton.GetComponent<PassiveButton>();

                // ボタン生成
                CreateButton("OptionReset", Color.red, -4.55f, new Action(() =>
                {
                    OptionItem.AllOptions.ToArray().Where(x => x.Id > 0 && x.Id is not 2 and not 3 && 1_000_000 > x.Id && x.CurrentValue != x.DefaultValue).Do(x => x.SetValue(x.DefaultValue, false, false));
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
                    OptionItem.SyncAllOptions();
                    OptionSaver.Save();
                }), UtilsSprite.LoadSprite("TownOfHost.Resources.RESET-STG.png", 150f));
                CreateButton("OptionCopy", Color.green, -3.9f, new Action(() =>
                {
                    OptionSerializer.SaveToClipboard();
                    GameSettingMenuChangeTabPatch.meg = GetString("OptionCopyMeg");
                    timer = 3;
                }), UtilsSprite.LoadSprite("TownOfHost.Resources.COPY-STG.png", 180f));
                CreateButton("OptionLoad", Color.green, -3.25f, new Action(() =>
                {
                    OptionSerializer.LoadFromClipboard();
                    GameSettingMenuChangeTabPatch.meg = GetString("OptionLoadMeg");
                    timer = 3;
                }), UtilsSprite.LoadSprite("TownOfHost.Resources.LOAD-STG.png", 180f));
                ErrorNumber = 13;

                CreateLobbyInfo();

                ErrorNumber = 14;
                __instance.transform.localScale = size;
            }
            catch (Exception Error)
            {
                Logger.Error($"Error:{ErrorNumber}\n{Error.ToString()}", "OptionMenu");
            }

            void CreateButton(string text, Color color, float xPos, Action action, Sprite sprite = null)
            {
                settingsButton ??= HudManager.Instance.SettingsButton.GetComponent<PassiveButton>();
                var ToggleButton = Object.Instantiate(settingsButton, __instance.transform);

                ToggleButton.transform.localScale -= new Vector3(0.25f, 0.25f);
                ToggleButton.name = text;
                if (sprite != null)
                {
                    ToggleButton.inactiveSprites.GetComponent<SpriteRenderer>().sprite = sprite;
                    ToggleButton.activeSprites.GetComponent<SpriteRenderer>().sprite = sprite;
                    ToggleButton.selectedSprites.GetComponent<SpriteRenderer>().sprite = sprite;
                }

                ToggleButton.OnClick = new();
                ToggleButton.OnClick.AddListener(action);

                var aspectPosition = ToggleButton.GetComponent<AspectPosition>();
                aspectPosition.DistanceFromEdge = new Vector3(xPos, 2.49f, -200f);
                aspectPosition.Alignment = AspectPosition.EdgeAlignments.Center;

                var textTMP = new GameObject("Text_TMP").AddComponent<TMPro.TextMeshPro>();
                textTMP.text = Utils.ColorString(color, GetString(text));
                textTMP.transform.SetParent(ToggleButton.transform);
                textTMP.transform.localPosition = new Vector3(0.8f, 0.8f);
                textTMP.transform.localScale = new Vector3(0, -0.5f);
                textTMP.alignment = TMPro.TextAlignmentOptions.Top;
                textTMP.fontSize = 10f;
            }

            void CreateLobbyInfo()
            {
                var baseTimer = GameStartManager._instance.RulesPresetText.transform.parent;
                var baseCount = GameStartManager._instance.PlayerCounter.transform.parent;

                var timer = GameObject.Instantiate(baseTimer, __instance.transform);
                var count = GameObject.Instantiate(baseCount, __instance.transform);

                timer.transform.localPosition = new(-1.7905f, 3.9055f, -200f);
                count.transform.localPosition = new(-2.35f, 2.6545f, -200f);

                timer.transform.localScale = new(0.45f, 0.45f, 1);
                count.transform.localScale = new(0.45f, 0.45f, 1);

                InfoTimer = timer.transform.GetComponentInChildren<TMPro.TextMeshPro>();
                InfoCount = count.transform.GetComponentInChildren<TMPro.TextMeshPro>();
            }
        }

        public static void CreateOptions(TabGroup tab, Dictionary<TabGroup, GameObject> menus, Dictionary<CustomRoles, GameObject> crmenus, Transform tabtransfrom, bool forceAllTabs = false)
        {
            if (!forceAllTabs && tabGenerated.Contains(tab)) return;
            var template = GetTeamplate();
            if (template == null) return;
            var LabelBackgroundSprite = UtilsSprite.LoadSprite($"TownOfHost.Resources.Label.LabelBackground.png");
            var ShowOptionSprite = UtilsSprite.LoadSprite($"TownOfHost.Resources.ShowOption.png");

            foreach (var option in OptionItem.AllOptions)
            {
                if (!forceAllTabs && option.Tab != tab) continue;
                if (option.OptionBehaviour == null)
                {
                    var parentrole = option.ParentRole;
                    //役職設定の場合
                    if (parentrole is not CustomRoles.NotAssigned && option.CustomRole is CustomRoles.NotAssigned)
                    {
                        var optionsMenu = crlist[parentrole];
                        var stringOption = Object.Instantiate(template, optionsMenu.transform);
                        crOptions[parentrole].Add(stringOption);
                        roleopts.Add(option);
                        stringOption.TitleText.text = $"<b>{option.GetName()}</b>";
                        stringOption.Value = stringOption.oldValue = option.CurrentValue;
                        stringOption.ValueText.text = "読み込み中..";
                        stringOption.name = option.Name;
                        stringOption.LabelBackground.sprite = LabelBackgroundSprite;
                        if (option.HideValue)
                        {
                            stringOption.PlusBtn.transform.localPosition = new Vector3(100, 100, 100);
                            stringOption.MinusBtn.transform.localPosition = new Vector3(100, 100, 100);
                        }
                        // フィルターオプション、属性設定なら
                        if (option is FilterOptionItem or AssignOptionItem)
                        {
                            stringOption.MinusBtn.OnClick = new();
                            stringOption.MinusBtn.OnClick.AddListener((System.Action)(() =>
                            {
                                if (option is FilterOptionItem filterOptionItem) filterOptionItem.SetRoleValue(parentrole);
                                if (option is AssignOptionItem assignoptionitem) assignoptionitem.SetRoleValue(new());
                            }));
                            stringOption.MinusBtn.transform.FindChild("Text_TMP").GetComponent<TMPro.TextMeshPro>().text = "<size=80%>←";
                            stringOption.PlusBtn.transform.FindChild("Text_TMP").GetComponent<TMPro.TextMeshPro>().text = "<rotate=-20>ρ";
                            stringOption.PlusBtn.OnClick = new();
                            stringOption.PlusBtn.OnClick.AddListener((System.Action)(() =>
                            {
                                CustomRoles[] notAssign = [];
                                var (imp, mad, crew, neu) = (true, true, true, true);
                                if (option is FilterOptionItem filterOptionItem)
                                {
                                    notAssign = filterOptionItem.NotAssin?.Invoke() ?? [];
                                    (imp, mad, crew, neu) = filterOptionItem.roles;
                                    ShowFilter.NowOption = option;
                                    ShowFilter.CreateFilterOptionMenu(tabtransfrom, null, notAssign, (imp, mad, crew, neu));
                                    return;
                                }
                                if (option is AssignOptionItem assignoptionitem)
                                {
                                    notAssign = assignoptionitem.NotAssin?.Invoke() ?? [];
                                    (imp, mad, crew, neu) = assignoptionitem.roles;

                                    ShowFilter.NowOption = option;
                                    ShowFilter.CreateFilterOptionMenu(tabtransfrom, assignoptionitem.RoleValues[AssignOptionItem.Getpresetid()], notAssign, (imp, mad, crew, neu));
                                    return;
                                }
                            }));
                        }

                        var transform = stringOption.ValueText.transform;
                        var pos = transform.localPosition;
                        transform.localPosition = new Vector3(pos.x + 0.7322f, pos.y, pos.z);
                        stringOption.SetClickMask(optionsMenu.ButtonClickMask);
                        option.OptionBehaviour = stringOption;
                    }
                    else
                    {
                        var optionsMenu = list[option.Tab];
                        var stringOption = Object.Instantiate(template, optionsMenu.transform);
                        scOptions[option.Tab].Add(stringOption);
                        stringOption.TitleText.text = $"<b>{option.GetName()}</b>";
                        stringOption.Value = stringOption.oldValue = option.CurrentValue;
                        stringOption.ValueText.text = "読み込み中..";
                        stringOption.name = option.Name;

                        stringOption.LabelBackground.sprite = LabelBackgroundSprite;

                        if (option.IsHeader)
                        {
                            var marksprite = UtilsSprite.LoadSprite($"TownOfHost.Resources.Label.{option.Name}.png");
                            if (UtilsRoleInfo.GetRoleByInputName(GetString(option.Name), out var role, true) && role.IsVanilla())
                            {
                                var roleb = RoleManager.Instance.AllRoles.ToArray().Where(x => x.Role == role.GetRoleTypes()).FirstOrDefault();

                                if (roleb is not null)
                                {
                                    marksprite = roleb.RoleIconSolid;
                                }
                            }
                            if (marksprite is not null)
                            {
                                var mark = Object.Instantiate(stringOption.LabelBackground, stringOption.transform);
                                mark.sprite = marksprite;
                                mark.transform.localPosition = new Vector3(0.53f, -0.062f, -10.1f);
                                mark.transform.localScale = new Vector3(0.26f, 0.85f, 1);
                            }
                        }
                        if (option.HideValue)
                        {
                            stringOption.PlusBtn.transform.localPosition = new Vector3(100, 100, 100);
                            stringOption.MinusBtn.transform.localPosition = new Vector3(100, 100, 100);
                        }
                        // フィルターオプション、属性設定なら
                        if (option is FilterOptionItem or AssignOptionItem)
                        {
                            stringOption.MinusBtn.OnClick = new();
                            stringOption.MinusBtn.OnClick.AddListener((System.Action)(() =>
                            {
                                if (option is FilterOptionItem filterOptionItem) filterOptionItem.SetRoleValue(parentrole);
                                if (option is AssignOptionItem assignoptionitem) assignoptionitem.SetRoleValue(new());
                            }));
                            stringOption.MinusBtn.transform.FindChild("Text_TMP").GetComponent<TMPro.TextMeshPro>().text = "<size=80%>←";
                            stringOption.PlusBtn.transform.FindChild("Text_TMP").GetComponent<TMPro.TextMeshPro>().text = "<rotate=-20>ρ";
                            stringOption.PlusBtn.OnClick = new();
                            stringOption.PlusBtn.OnClick.AddListener((System.Action)(() =>
                            {
                                CustomRoles[] notAssign = [];
                                var (imp, mad, crew, neu) = (true, true, true, true);
                                if (option is FilterOptionItem filterOptionItem)
                                {
                                    notAssign = filterOptionItem.NotAssin?.Invoke() ?? [];
                                    (imp, mad, crew, neu) = filterOptionItem.roles;
                                }
                                if (option is AssignOptionItem assignoptionitem)
                                {
                                    notAssign = assignoptionitem.NotAssin?.Invoke() ?? [];
                                    (imp, mad, crew, neu) = assignoptionitem.roles;

                                    ShowFilter.NowOption = option;
                                    ShowFilter.CreateFilterOptionMenu(tabtransfrom, assignoptionitem.RoleValues[AssignOptionItem.Getpresetid()], notAssign, (imp, mad, crew, neu));
                                    return;
                                }
                                ShowFilter.NowOption = option;
                                ShowFilter.CreateFilterOptionMenu(tabtransfrom, null, notAssign, (imp, mad, crew, neu));
                            }));
                        }
                        if (option.CustomRole is not CustomRoles.NotAssigned and not CustomRoles.GM)
                        {
                            var button = Object.Instantiate(GameSettingMenu.Instance.GameSettingsButton, stringOption.transform);
                            button.inactiveSprites.GetComponent<SpriteRenderer>().sprite =
                            button.selectedSprites.GetComponent<SpriteRenderer>().sprite = null;

                            button.OnClick = new();
                            button.buttonText.DestroyTranslator();
                            button.buttonText.text = " ";
                            button.gameObject.name = $"{option.Name}OptionButton";
                            button.transform.localPosition = new Vector3(-2.46f, 0.0446f, -2);
                            button.transform.localScale = new Vector3(1.44f, 1.14f, 1f);
                            button.activeSprites.GetComponent<SpriteRenderer>().sprite = LabelBackgroundSprite;
                            button.activeSprites.GetComponent<SpriteRenderer>().color = UtilsRoleText.GetRoleColor(option.CustomRole).ShadeColor(0.2f).SetAlpha(0.35f);

                            button.OnClick.AddListener((System.Action)(() =>
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
                                    crmenus[option.CustomRole].SetActive(false);
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
                                        if (option.CustomRole.IsLovers()) color = UtilsRoleText.GetRoleColor(option.CustomRole);
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
                                    infobutton.OnClick.AddListener((System.Action)(() =>
                                    {
                                        var oldinfo = Nowinfo;
                                        Nowinfo = option.CustomRole;
                                        if (HudManager.Instance.TaskPanel.open && oldinfo.IsCombinationRole() && Nowinfo.IsCombinationRole())
                                        {
                                            switch (oldinfo)
                                            {
                                                case CustomRoles.Assassin: if (Nowinfo is CustomRoles.Assassin) Nowinfo = CustomRoles.Merlin; break;
                                                case CustomRoles.Merlin: if (Nowinfo is CustomRoles.Assassin) Nowinfo = CustomRoles.Assassin; break;
                                                case CustomRoles.Driver: if (Nowinfo is CustomRoles.Driver) Nowinfo = CustomRoles.Braid; break;
                                                case CustomRoles.Braid: if (Nowinfo is CustomRoles.Driver) Nowinfo = CustomRoles.Driver; break;
                                                case CustomRoles.Vega: if (Nowinfo is CustomRoles.Vega) Nowinfo = CustomRoles.Altair; break;
                                                case CustomRoles.Altair: if (Nowinfo is CustomRoles.Vega) Nowinfo = CustomRoles.Vega; break;
                                                case CustomRoles.Fool: if (Nowinfo is CustomRoles.Nue) Nowinfo = CustomRoles.Nue; break;
                                                case CustomRoles.Nue: if (Nowinfo is CustomRoles.Nue) Nowinfo = CustomRoles.Fool; break;
                                            }
                                            return;
                                        }
                                        if (HudManager.Instance.TaskPanel.open is false || Nowinfo == oldinfo)
                                            HudManager.Instance.TaskPanel.ToggleOpen();
                                    }));
                                    infobutton.gameObject.transform.SetLocalX(-0.1f);
                                    infobutton.gameObject.transform.SetLocalZ(-50);

                                    roleInfobutton.Add(option.CustomRole, infobutton);
                                }
                            }
                            {
                                var Showoptionbutton = Object.Instantiate(stringOption.MinusBtn, stringOption.transform);
                                {
                                    Showoptionbutton.gameObject.name = $"{option.Name}-SetOption";
                                    Showoptionbutton.transform.FindChild("Text_TMP").GetComponent<TMPro.TextMeshPro>().text = "";
                                    Showoptionbutton.buttonSprite.sprite = ShowOptionSprite;

                                    Showoptionbutton.OnClick = new();
                                    Showoptionbutton.OnClick.AddListener((System.Action)(() =>
                                    {
                                        button.OnClick.Invoke();
                                    }));
                                    Showoptionbutton.gameObject.transform.SetLocalX(-0.6f);
                                    Showoptionbutton.gameObject.transform.SetLocalZ(-50);
                                }
                            }
                        }
                        var transform = stringOption.ValueText.transform;
                        var pos = transform.localPosition;
                        transform.localPosition = new Vector3((pos.x + 0.7322f), pos.y, pos.z);
                        stringOption.SetClickMask(optionsMenu.ButtonClickMask);
                        option.OptionBehaviour = stringOption;
                    }
                }
                option.OptionBehaviour.gameObject.active = true;
            }

            tabGenerated.Add(tab);
            Object.Destroy(template.gameObject);
        }

        public static StringOption GetTeamplate()
        {
            var template = Object.Instantiate(BaseOption);
            Vector3 pos = new();
            Vector3 scale = new();

            template.stringOptionName = AmongUs.GameOptions.Int32OptionNames.TaskBarMode;
            //Background
            var label = template.LabelBackground.transform;
            {
                label.localScale = new Vector3(1.3f, 1.14f, 1f);
                label.SetLocalX(-2.2695f);
            }
            //プラスボタン
            var plusButton = template.PlusBtn.transform;
            {
                pos = plusButton.localPosition;
                scale = plusButton.localScale;
                plusButton.localScale = new Vector3(scale.x, scale.y);
                plusButton.localPosition = new Vector3((pos.x + 1.1434f), pos.y, pos.z);
            }
            //マイナスボタン
            var minusButton = template.MinusBtn.transform;
            {
                pos = minusButton.localPosition;
                scale = minusButton.localScale;
                minusButton.localPosition = new Vector3((pos.x + 0.3463f), (pos.y), pos.z);
                minusButton.localScale = new Vector3(scale.x, scale.y);
            }
            //値を表示するテキスト
            var valueTMP = template.ValueText.transform;
            {
                pos = valueTMP.localPosition;
                valueTMP.localPosition = new Vector3((pos.x + 2.5f), pos.y, pos.z);
                scale = valueTMP.localScale;
                valueTMP.localScale = new Vector3(scale.x, scale.y, scale.z);
            }
            //上のテキストを囲む箱(ﾀﾌﾞﾝ)
            var valueBox = template.transform.FindChild("ValueBox");
            {
                pos = valueBox.localPosition;
                valueBox.localPosition = new Vector3((pos.x + 0.7322f), pos.y, pos.z);
                scale = valueBox.localScale;
                valueBox.localScale = new Vector3((scale.x + 0.2f), scale.y, scale.z);
            }
            //タイトル(設定名)
            var titleText = template.TitleText;
            {
                var transform = titleText.transform;
                pos = transform.localPosition;
                transform.localPosition = new Vector3((pos.x + -1.096f), pos.y, pos.z);
                scale = transform.localScale;
                transform.localScale = new Vector3(scale.x, scale.y, scale.z);
                titleText.rectTransform.sizeDelta = new Vector2(6.5f, 0.37f);
                titleText.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
                titleText.SetOutlineColor(Color.black);
                titleText.SetOutlineThickness(0.125f);
            }
            template.OnValueChanged = new System.Action<OptionBehaviour>((o) => { });
            return template;
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
            GameSettingMenuChangeTabPatch.ClickCount = 0;
        }
    }

    [HarmonyPatch(typeof(RolesSettingsMenu), nameof(RolesSettingsMenu.Update))]
    class ModSettingsMenuUpdatePatch
    {
        public static bool Prefix(RolesSettingsMenu __instance)
        {
            if (__instance != ModSettingsTab) return true;

            if (!(ControllerManager.Instance.CurrentUiState.MenuName == __instance.name))
                return false;
            Rewired.Player player = Rewired.ReInput.players.GetPlayer(0);
            bool flag = false;
            if (__instance.selectedRoleTab > 0 && player.GetButtonDown(35))
            {
                --__instance.selectedRoleTab;
                flag = true;
            }
            if (__instance.selectedRoleTab < __instance.roleTabs.Count - 1 && player.GetButtonDown(34))
            {
                ++__instance.selectedRoleTab;
                flag = true;
            }
            if (flag)
            {
                __instance.roleTabs[__instance.selectedRoleTab].OnClick.Invoke();
            }
            __instance.glyphL.color = __instance.selectedRoleTab <= 0 ? __instance.glyphUnavailableColor : Color.white;
            if (__instance.selectedRoleTab < __instance.roleTabs.Count - 1)
                __instance.glyphR.color = Color.white;
            else
                __instance.glyphR.color = __instance.glyphUnavailableColor;
            return false;
        }
    }
}