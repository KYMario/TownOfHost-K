using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TownOfHost.Roles.Core;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost;

class ShowFilter
{
    public static bool IsShowFilter = false;
    public static OptionItem NowOption;//現在のオプション
    public static SpriteRenderer CustomBackground { get; private set; }//背景
    public static GameObject Scrollbargameobject;
    public static float CloseButtonY;
    public static Dictionary<CustomRoles, ToggleButtonBehaviour> buttons = new();
    public static List<CustomRoles> Activeroles = new();
    public static TabGroup NowTab;

    public static void CreateFilterOptionMenu(Transform tabtransform
    , List<CustomRoles> activeroles, CustomRoles[] NotAssign, (bool imp, bool mad, bool crew, bool neu, bool addon) role)
    {
        if (CustomBackground is null)
        {
            Activeroles = activeroles;
            buttons = new();
            IsShowFilter = true;
            OptionsMenuBehaviour menuBehaviour = HudManager.Instance.GameMenu;
            {
                try
                {
                    if (Activeroles is null) Activeroles = new();
                    var mouseMoveToggle = menuBehaviour.DisableMouseMovement;

                    NowTab = TabGroup.MainSettings;
                    List<PassiveButton> tabbuttons = new();
                    if (CustomBackground is null)
                    {
                        CustomBackground = Object.Instantiate(menuBehaviour.Background, tabtransform.parent);
                        CustomBackground.name = "RoleSetBackground";
                        CustomBackground.transform.localScale = new(1.7f, 0.9f, 1f);
                        CustomBackground.transform.localPosition = new Vector3(0, 0, -500);
                        CustomBackground.gameObject.SetActive(false);

                        var pilldown = Object.Instantiate(mouseMoveToggle, CustomBackground.transform);
                        pilldown.transform.localPosition = new(1.6f, 2.5f, -100f);
                        pilldown.transform.localScale = new(0.5f, 0.9f, 1f);
                        pilldown.name = "pulldown";
                        pilldown.Text.text = "All Roles";
                        pilldown.Background.color = Palette.DisabledGrey;
                        var pilldownButton = pilldown.GetComponent<PassiveButton>();
                        pilldownButton.OnClick = new();
                        pilldownButton.OnClick.AddListener(new System.Action(() =>
                        {
                            if (tabbuttons.All(button => button.gameObject.active))
                            {
                                tabbuttons.Do(button => button.gameObject.SetActive(false));
                                return;
                            }
                            tabbuttons.Do(button => button.gameObject.SetActive(true));
                        }));

                        var closeButton = Object.Instantiate(mouseMoveToggle, CustomBackground.transform);
                        closeButton.transform.localPosition = new(-1.6f, 2.5f, -6f);
                        closeButton.transform.localScale = new(0.5f, 0.9f, 1f);
                        closeButton.name = "Close";
                        closeButton.Text.text = GetString("Close");
                        closeButton.Background.color = Palette.DisabledGrey;
                        var closePassiveButton = closeButton.GetComponent<PassiveButton>();
                        closePassiveButton.OnClick = new();
                        closePassiveButton.OnClick.AddListener(new System.Action(() =>
                        {
                            CallEsc();
                        }));
                        foreach (var tab in EnumHelper.GetAllValues<TabGroup>())
                        {
                            var tabbutton = Object.Instantiate(mouseMoveToggle, CustomBackground.transform);
                            tabbutton.transform.localPosition = new(1.6f, 2.5f + (((int)tab + 1) * -0.4f), -200f);
                            tabbutton.transform.localScale = new(0.5f, 0.9f, 1f);
                            tabbutton.name = $"{tab}";
                            tabbutton.Text.text = tab is TabGroup.MainSettings ? "All Roles" : GetString($"TabGroup.{tab}");
                            tabbutton.Background.color = Palette.DisabledGrey;
                            var tabbuttonPassiveButton = tabbutton.GetComponent<PassiveButton>();
                            tabbuttonPassiveButton.OnClick = new();
                            tabbuttonPassiveButton.OnClick.AddListener(new System.Action(() =>
                                {
                                    pilldown.Text.text = "Now:" + (tab is TabGroup.MainSettings ? "All Roles" : GetString($"TabGroup.{tab}"));
                                    NowTab = tab;
                                    tabbuttons.Do(button => button.gameObject.SetActive(false));
                                }));
                            tabbutton.gameObject.SetActive(false);
                            tabbuttons.Add(tabbuttonPassiveButton);
                        }

                        CloseButtonY = closeButton.transform.position.y;

                        UiElement[] selectableButtons = menuBehaviour.ControllerSelectable.ToArray();
                        PassiveButton leaveButton = null;
                        PassiveButton returnButton = null;
                        for (int i = 0; i < selectableButtons.Length; i++)
                        {
                            var button = selectableButtons[i];
                            if (button == null)
                            {
                                continue;
                            }

                            if (button.name == "LeaveGameButton")
                            {
                                leaveButton = button.GetComponent<PassiveButton>();
                            }
                            else if (button.name == "ReturnToGameButton")
                            {
                                returnButton = button.GetComponent<PassiveButton>();
                            }
                        }
                        var generalTab = mouseMoveToggle.transform.parent.parent.parent;
                    }

                    var scrollbargameobjecttem = GameObject.Find("Main Camera/PlayerOptionsMenu(Clone)/MainArea/ModSettingTab/Scroller");

                    Scrollbargameobject = Object.Instantiate(scrollbargameobjecttem, tabtransform.parent);
                    var scrollbar = Scrollbargameobject.GetComponent<Scroller>;
                    Scrollbargameobject.GetComponentInParent<Scroller>().Inner.transform.DestroyChildren();
                    Scrollbargameobject.transform.localPosition = new(0.2043f, -0.143f, -540);
                    Scrollbargameobject.GetComponentInParent<Scroller>().Inner.transform.localScale = new(1.4f, 0.8f, 1);
                    scrollbar.Invoke().ScrollPercentY(0.8f);
                    scrollbar.Invoke().Hitbox.transform.localPosition = new(-1.5358f, -0.7333f, 4f);
                    scrollbar.Invoke().Hitbox.transform.localScale = new(1.4f, 1.6f, 1);

                    var numItems = 0;
                    bool IsFilterOption = NowOption is not AssignOptionItem;
                    List<CustomRoles> rolelist = new();
                    if (role.addon is false)
                    {
                        rolelist.Add(CustomRoles.Crewmate);
                        rolelist.Add(CustomRoles.Impostor);
                        rolelist.Add(CustomRoles.Merlin);
                        rolelist.Add(CustomRoles.Braid);
                        rolelist.Add(CustomRoles.Fool);
                    }
                    Options.CustomRoleSpawnChances.Keys.Do(r => rolelist.Add(r));
                    foreach (var customrole in rolelist)
                    {
                        if (customrole.IsMainRole() is false && !role.addon) continue;
                        if (customrole is CustomRoles.SKMadmate or CustomRoles.Emptiness or CustomRoles.HASFox or CustomRoles.HASTroll) continue;
                        if (IsFilterOption is false && !role.addon)
                        {
                            if ((customrole is CustomRoles.Merlin && CustomRoles.Assassin.IsEnable()) ||
                                (customrole is CustomRoles.Braid && CustomRoles.Driver.IsEnable()) ||
                                (customrole is CustomRoles.Fool && CustomRoles.Nue.IsEnable())
                                )
                            { }
                            else
                            {
                                if (customrole.IsEnable() is false && !(customrole is CustomRoles.Crewmate or CustomRoles.Impostor) &&
                                        !activeroles.Contains(customrole)) continue;
                            }
                        }
                        if (NotAssign.Contains(customrole)) continue;
                        if (!customrole.IsAddOn() && !(customrole is CustomRoles.Amanojaku))
                        {
                            var roletype = customrole.GetCustomRoleTypes();
                            if (roletype is CustomRoleTypes.Impostor && role.imp is false) continue;
                            if (roletype is CustomRoleTypes.Madmate && role.mad is false) continue;
                            if (roletype is CustomRoleTypes.Crewmate && role.crew is false) continue;
                            if (roletype is CustomRoleTypes.Neutral && role.neu is false) continue;
                            if (!Event.CheckRole(customrole)) continue;
                        }
                        // ボタン生成
                        var ToggleButton = Object.Instantiate(mouseMoveToggle, Scrollbargameobject.GetComponentInParent<Scroller>().Inner.transform);
                        ToggleButton.transform.localPosition = new Vector3(
                            numItems % 4 == 0 ? -1.9f :
                            (numItems % 4 == 1 ? -0.7f :
                            (numItems % 4 == 2 ? 0.5f : 1.7f)), 2.4f - (0.5f * (numItems / 4)),
                            -6f);
                        ToggleButton.transform.localScale = new Vector3(0.5f, 1, 1);
                        ToggleButton.name = $"{customrole}";
                        ToggleButton.Text.text = UtilsRoleText.GetRoleColorAndtext(customrole);
                        var passiveButton = ToggleButton.GetComponent<PassiveButton>();
                        ToggleButton.gameObject.AddComponent<UIScrollbarHelper>();
                        passiveButton.OnClick = new();
                        passiveButton.OnClick.AddListener(new System.Action(() =>
                        {
                            if (NowOption is FilterOptionItem filterOptionItem)
                            {
                                filterOptionItem.SetRoleValue(customrole);
                                CallEsc();
                            }
                            {
                                var color = IsFilterOption ? Color.white : (Activeroles.Contains(customrole) ? Color.grey : Color.green);
                                ToggleButton.Background.color = color;
                                if (ToggleButton.Rollover != null)
                                {
                                    ToggleButton.Rollover.ChangeOutColor(color);
                                }
                            }
                            if (NowOption is AssignOptionItem assignOptionItem)
                            {
                                if (Activeroles.Contains(customrole))
                                {
                                    Activeroles.Remove(customrole);
                                }
                                else Activeroles.Add(customrole);
                            }
                        }));
                        {
                            var color = IsFilterOption ? Color.white : (Activeroles.Contains(customrole) ? Color.green : Color.grey);
                            ToggleButton.Background.color = color;
                            if (ToggleButton.Rollover != null)
                            {
                                ToggleButton.Rollover.ChangeOutColor(color);
                            }
                        }
                        buttons.Add(customrole, ToggleButton);
                        numItems++;
                    }                        // AllOn/AllOff
                    var allonbutton = Object.Instantiate(mouseMoveToggle, CustomBackground.transform);
                    allonbutton.transform.localPosition = new(-0.5f, 2.5f, -60f);
                    allonbutton.transform.localScale = new(0.37f, 0.72f, 1f);
                    allonbutton.name = "AllON";
                    allonbutton.Text.text = "AllON";
                    allonbutton.Background.color = Palette.Orange;
                    var allonpassive = allonbutton.GetComponent<PassiveButton>();
                    allonpassive.OnClick = new();
                    allonpassive.OnClick.AddListener(new System.Action(() =>
                    {
                        foreach (var data in buttons)
                        {
                            bool IsActive = NowTab switch
                            {
                                TabGroup.MainSettings => true,
                                TabGroup.ImpostorRoles => data.Key.IsImpostor(),
                                TabGroup.MadmateRoles => data.Key.IsMadmate(),
                                TabGroup.NeutralRoles => data.Key.IsNeutral(),
                                TabGroup.CrewmateRoles => data.Key.IsCrewmate(),
                                TabGroup.GhostRoles => data.Key.IsGhostRole(),
                                TabGroup.Addons => data.Key.IsAddOn(),
                                TabGroup.Combinations => data.Key.IsCombinationRole(),
                                _ => false
                            };
                            IsActive = (IsActive && data.Key.IsEnable()) || (IsActive && data.Key.IsVanilla())//配役可能性有 or ばにら
                            || (IsActive && (data.Key.GetCombination().IsEnable() || data.Key.IsEnable()))//コンビの親が配役可能性有
                            || (IsActive && data.Key.IsAddOn());//属性
                            if (IsActive && data.Value.Background.color == Color.grey)
                            {
                                data.Value.GetComponent<PassiveButton>()?.OnClick?.Invoke();
                            }
                        }
                    }));

                    var alloffbutton = Object.Instantiate(mouseMoveToggle, CustomBackground.transform);
                    alloffbutton.transform.localPosition = new(0.4f, 2.5f, -60f);
                    alloffbutton.transform.localScale = new(0.37f, 0.72f, 1f);
                    alloffbutton.name = "AllOff";
                    alloffbutton.Text.text = "AllOff";
                    alloffbutton.Background.color = Palette.Blue;
                    var alloffpassive = alloffbutton.GetComponent<PassiveButton>();
                    alloffpassive.OnClick = new();
                    alloffpassive.OnClick.AddListener(new System.Action(() =>
                    {
                        foreach (var data in buttons)
                        {
                            bool IsActive = NowTab switch
                            {
                                TabGroup.MainSettings => true,
                                TabGroup.ImpostorRoles => data.Key.IsImpostor(),
                                TabGroup.MadmateRoles => data.Key.IsMadmate(),
                                TabGroup.NeutralRoles => data.Key.IsNeutral(),
                                TabGroup.CrewmateRoles => data.Key.IsCrewmate(),
                                TabGroup.GhostRoles => data.Key.IsGhostRole(),
                                TabGroup.Addons => data.Key.IsAddOn(),
                                TabGroup.Combinations => data.Key.IsCombinationRole(),
                                _ => false
                            };
                            IsActive = (IsActive && data.Key.IsEnable()) || (IsActive && data.Key.IsVanilla())
                            || (IsActive && (data.Key.GetCombination().IsEnable() || data.Key.IsEnable()))
                            || (IsActive && data.Key.IsAddOn());
                            if (IsActive && data.Value.Background.color == Color.green)
                            {
                                data.Value.GetComponent<PassiveButton>()?.OnClick?.Invoke();
                            }
                        }
                    }));
                    if (NowOption is FilterOptionItem)
                    {
                        allonbutton.gameObject.SetActive(false);
                        alloffbutton.gameObject.SetActive(false);
                    }
                    Scrollbargameobject.GetComponentInParent<Scroller>().ContentYBounds.max = numItems > 9 ? 0.5f * (numItems / 4 - 9) : 0;
                    Scrollbargameobject.GetComponentInParent<Scroller>().Inner.localPosition = Vector3.zero;
                }
                catch { }
            }
        }
        //Show 
        CustomBackground.gameObject.SetActive(true);
        Scrollbargameobject.gameObject.SetActive(true);
    }
    public static bool CallEsc(bool IsPrefix = false)
    {
        if (Scrollbargameobject?.gameObject is not null) Object.Destroy(Scrollbargameobject?.gameObject);
        if (CustomBackground?.gameObject is not null) Object.Destroy(CustomBackground.gameObject);

        if (NowOption is AssignOptionItem SetRoleValue)
        {
            SetRoleValue.SetRoleValue(Activeroles);
        }

        NowOption = null;
        CustomBackground = null;
        Scrollbargameobject = null;
        buttons = new();
        Activeroles = new();
        var old = IsShowFilter;
        if (IsPrefix is false) IsShowFilter = false;
        return old;
    }
    public static void CloseOptionMenu()
    {
        NowOption = null;
        CustomBackground = null;
        Scrollbargameobject = null;
        buttons = new();
        Activeroles = new();
    }
    public static void FixUpdate()
    {
        var numItems = 0;
        foreach (var buttongameobjectdata in buttons)
        {
            var buttongameobject = buttongameobjectdata.Value;
            if (buttongameobject is null) continue;
            bool IsActive = NowTab switch
            {
                TabGroup.MainSettings => true,
                TabGroup.ImpostorRoles => buttongameobjectdata.Key.IsImpostor(),
                TabGroup.MadmateRoles => buttongameobjectdata.Key.IsMadmate(),
                TabGroup.NeutralRoles => buttongameobjectdata.Key.IsNeutral(),
                TabGroup.CrewmateRoles => buttongameobjectdata.Key.IsCrewmate(),
                TabGroup.GhostRoles => buttongameobjectdata.Key.IsGhostRole(),
                TabGroup.Addons => buttongameobjectdata.Key.IsAddOn(),
                TabGroup.Combinations => buttongameobjectdata.Key.IsCombinationRole(),
                _ => false
            };
            if (IsActive)
            {
                buttongameobject.transform.localPosition = new Vector3(
                                numItems % 4 == 0 ? -1.9f :
                                (numItems % 4 == 1 ? -0.7f :
                                (numItems % 4 == 2 ? 0.5f : 1.7f)), 2.4f - (0.5f * (numItems / 4)),
                                -6f);
                numItems++;
            }
            if ((buttongameobject.transform.position.y - CloseButtonY) > -0.2f ||
            (buttongameobject.transform.position.y - CloseButtonY) < -4.2f)
            {
                IsActive = false;
            }
            if (!buttongameobject.gameObject.active && IsActive)
            {
                buttongameobject.gameObject.SetActive(true);
            }
            else
                if (buttongameobject.gameObject.active && !IsActive)
                {
                    buttongameobject.gameObject.SetActive(false);
                }
        }
        if (Scrollbargameobject is null) return;
        Scrollbargameobject.GetComponentInParent<Scroller>().ContentYBounds.max = numItems > 9 ? 0.5f * (numItems / 4 - 9) : 0;
    }
}