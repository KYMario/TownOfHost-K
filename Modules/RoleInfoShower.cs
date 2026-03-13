using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using Rewired.Utils;
using TMPro;
using TownOfHost.Roles.Core;
using TownOfHost.Templates;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost;

class RoleInfoShower
{
    public static bool IsShowInfo = false;
    public static bool IsShowRoleInfo = false;
    public static SpriteRenderer RoleSprite { get; private set; }
    public static SpriteRenderer CustomBackground { get; private set; }//背景
    public static GameObject Scrollbargameobject;
    public static TextMeshPro RoleInfo;
    public static float CloseButtonY;
    public static TabGroup NowTab;
    public static PassiveButton AchievementPassiveButton;
    public static string AchievementText;
    public static Dictionary<CustomRoles, GameObject> buttons = new();

    public static void CreateMenu(MainMenuManager mainmenu)
    {
        if (CustomBackground is null)
        {
            buttons = new();
            IsShowInfo = true;
            IsShowRoleInfo = false;
            OptionsMenuBehaviour menuBehaviour = OptionsMenuBehaviourStartPatch.Instance;
            if (menuBehaviour is null)
            {
                mainmenu.settingsButton.OnClick.Invoke();
                _ = new LateTask(() => CreateMenu(mainmenu), 0.05f, "", true);
                return;
            }
            bool IsError = false;
            try
            {
                if (menuBehaviour.IsOpen) menuBehaviour.Close();
            }
            catch
            {
                mainmenu.settingsButton.OnClick.Invoke();
                _ = new LateTask(() => CreateMenu(mainmenu), 0.05f, "", true);
                IsError = true;
                return;
            }
            if (IsError is false)
            {
                try
                {
                    var mouseMoveToggle = menuBehaviour.DisableMouseMovement;

                    NowTab = TabGroup.MainSettings;
                    List<PassiveButton> tabbuttons = new();
                    if (CustomBackground is null)
                    {
                        CustomBackground = Object.Instantiate(menuBehaviour.Background, mainmenu.transform);
                        CustomBackground.name = "RoleSetBackground";
                        CustomBackground.transform.localScale = new(1.7f, 0.9f, 1f);
                        CustomBackground.transform.localPosition = new Vector3(0, 0, -500);
                        CustomBackground.gameObject.SetActive(false);
                        RoleSprite = Object.Instantiate(menuBehaviour.Background, CustomBackground.transform);
                        RoleSprite.name = "RoleSetBackground";
                        RoleSprite.transform.localScale = new(0.28f, 0.5f, 1f);
                        RoleSprite.transform.localPosition = new Vector3(1.7369f, 1.2444f, -1f);
                        RoleSprite.gameObject.SetActive(false);
                        var achievementtext = TMPTemplate.Create("achievement"
                        , "", Color.white, 2, TextAlignmentOptions.TopLeft, true, CustomBackground.transform);
                        achievementtext.transform.localPosition = new(0f, 2.55f, -50);
                        achievementtext.transform.localScale = new(0.5f, 1f, 1);
                        (int comp, int all) nomal = (0, 0);
                        (int comp, int all) Rare = (0, 0);
                        (int comp, int all) SRare = (0, 0);
                        (int comp, int all) All = (0, 0);
                        foreach (var achi in Achievement.AllAchievements.Values)
                        {
                            switch (achi.Difficulty)
                            {
                                case 0:
                                    nomal.comp += achi.IsCompleted ? 1 : 0;
                                    nomal.all++;
                                    break;
                                case 1:
                                    Rare.comp += achi.IsCompleted ? 1 : 0;
                                    Rare.all++;
                                    break;
                                case 2:
                                    SRare.comp += achi.IsCompleted ? 1 : 0;
                                    SRare.all++;
                                    break;
                            }
                            All.all++;
                            All.comp += achi.IsCompleted ? 1 : 0;
                        }
                        achievementtext.text = $"{GetString("Achievement")}:<#674020>◎</color>{nomal.comp}/{nomal.all} <#aacbf7>◆</color>{Rare.comp}/{Rare.all} "
                        + $"<#ffea4e>★</color>{SRare.comp}/{SRare.all}  All:{All.comp}/{All.all}";

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
                            tabbuttons.Do(button => button.gameObject.SetActive(true));
                        }));

                        var closeButton = Object.Instantiate(mouseMoveToggle, CustomBackground.transform);
                        closeButton.transform.localPosition = new(-1.6f, 2.5f, -100f);
                        closeButton.transform.localScale = new(0.5f, 0.9f, 1f);
                        closeButton.name = "Close";
                        closeButton.Text.text = GetString("Close");
                        closeButton.Background.color = Palette.DisabledGrey;
                        var closePassiveButton = closeButton.GetComponent<PassiveButton>();
                        closePassiveButton.OnClick = new();
                        closePassiveButton.OnClick.AddListener(new System.Action(() =>
                        {
                            if (IsShowRoleInfo)
                            {
                                pilldown.gameObject.SetActive(true);
                                try
                                {
                                    if (AchievementPassiveButton is not null)
                                    {
                                        GameObject.Destroy(AchievementPassiveButton.gameObject);
                                    }
                                }
                                catch { }
                            }
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
                        var scrollbargameobjecttem = StoreMenu.Instance.Scroller.gameObject;

                        Scrollbargameobject = Object.Instantiate(scrollbargameobjecttem, mainmenu.transform);
                        var scrollbar = Scrollbargameobject.GetComponent<Scroller>;
                        Scrollbargameobject.GetComponentInParent<Scroller>().Inner.transform.DestroyChildren();
                        Scrollbargameobject.transform.localPosition = new(0.2043f, -0.143f, -540);
                        Scrollbargameobject.transform.GetChild(1).gameObject.SetActive(false);
                        Scrollbargameobject.transform.GetChild(2).gameObject.SetActive(false);
                        Scrollbargameobject.transform.GetChild(0).localPosition = new(2.1f, 2.6f, 2f);
                        Scrollbargameobject.transform.GetChild(0).localScale = new Vector3(0.7f, 1, 0);
                        Scrollbargameobject.transform.GetChild(3).SetLocalY(0);
                        Scrollbargameobject.GetComponentInParent<Scroller>().Inner.transform.localScale = new(1.4f, 0.8f, 1);
                        scrollbar.Invoke().ScrollPercentY(0.8f);
                        scrollbar.Invoke().Hitbox.transform.localPosition = new(-1.5358f, -0.7333f, 4f);
                        scrollbar.Invoke().Hitbox.transform.localScale = new(1.4f, 1.6f, 1);

                        var numItems = 0;
                        List<CustomRoles> rolelist = new();
                        CustomRoleManager.SortCustomRoles.DoIf(r => r.IsStartedRole(), r => rolelist.Add(r));
                        foreach (var customrole in rolelist.OrderBy(role => role.IsVanilla() is false))
                        {
                            if (!Event.CheckRole(customrole)) continue;
                            if (customrole is CustomRoles.Phantom) continue;
                            // ボタン生成
                            var ToggleButton = Object.Instantiate(mouseMoveToggle, Scrollbargameobject.GetComponentInParent<Scroller>().Inner.transform);
                            ToggleButton.transform.localPosition = new Vector3(
                                numItems % 4 == 0 ? -1.9f :
                                (numItems % 4 == 1 ? -0.7f :
                                (numItems % 4 == 2 ? 0.5f : 1.7f)), 2.4f - (0.5f * (numItems / 4)),
                                -6f);
                            ToggleButton.transform.localScale = new Vector3(0.5f, 1, 1);
                            ToggleButton.name = $"{customrole}";
                            ToggleButton.Text.text = $"<b>{UtilsRoleText.GetRoleColorAndtext(customrole)}</b>";
                            var passiveButton = ToggleButton.GetComponent<PassiveButton>();
                            ToggleButton.gameObject.AddComponent<UIScrollbarHelper>();
                            ToggleButton.Background.color = Color.gray;
                            passiveButton.OnClick = new();
                            passiveButton.OnClick.AddListener(new System.Action(() =>
                            {
                                pilldown.gameObject.SetActive(false);
                                tabbuttons.Do(button => button.gameObject.SetActive(false));
                                var marksprite = UtilsSprite.LoadSprite($"TownOfHost.Resources.Label.{customrole}.png");
                                if (marksprite is not null)
                                {
                                    RoleSprite.sprite = marksprite;
                                    RoleSprite.gameObject.SetActive(true);
                                }
                                IsShowRoleInfo = true;
                                buttons.Values.Do(button => button.gameObject.SetActive(false));
                                if (RoleInfo.IsNullOrDestroyed())
                                {
                                    RoleInfo = TMPTemplate.Create("RoleInfo"
                                    , "", Color.white, 3, TextAlignmentOptions.TopLeft, true, CustomBackground.transform);
                                    RoleInfo.transform.localPosition = new(-1.6f, 2.1267f, -50);
                                    RoleInfo.transform.localScale = new(0.5f, 1f, 1);
                                }
                                RoleInfo.text = "<size=160%><b>" + UtilsRoleText.GetRoleColorAndtext(customrole).RemoveDeltext("</color>");
                                RoleInfo.text += "\n<size=100%>" + (customrole.IsVanilla() ? customrole.GetRoleInfo().Description.Blurb : GetString($"{customrole}Info"))
                                + "</color></b>";
                                if (customrole.GetRoleInfo() is not null)
                                {
                                    var info = customrole.GetRoleInfo();
                                    var count = info.CountType;
                                    var overrideRoleText = CustomRoles.NotAssigned;
                                    var countText = GetString(count.ToString());
                                    switch (count)
                                    {
                                        case CountTypes.Impostor: overrideRoleText = CustomRoles.Impostor; break;
                                        case CountTypes.Jackal: overrideRoleText = CustomRoles.Jackal; break;
                                        case CountTypes.Fox: overrideRoleText = CustomRoles.Fox; break;
                                        case CountTypes.GrimReaper: overrideRoleText = CustomRoles.GrimReaper; break;
                                        case CountTypes.Remotekiller: overrideRoleText = CustomRoles.Remotekiller; break;
                                        case CountTypes.MilkyWay: countText = Roles.Neutral.Vega.TeamText; break;
                                        default: overrideRoleText = CustomRoles.Crewmate; break;
                                    }

                                    var roleTeam = info.CustomRoleType == CustomRoleTypes.Madmate ? CustomRoleTypes.Impostor : info.CustomRoleType;
                                    if (overrideRoleText != CustomRoles.NotAssigned) countText = GetString(overrideRoleText.ToString());

                                    RoleInfo.text += $"\n<size=65%>{GetString("Team")}:{GetString($"CustomRoleTypes.{roleTeam}")}  " + $"{GetString("Count")}:{countText}  ";
                                    RoleInfo.text += $"{GetString("Basis")}:{GetString(info.BaseRoleType.Invoke().ToString())}";

                                    if (info.From != From.None) RoleInfo.text += "    " + UtilsOption.GetFrom(info, false).RemoveSizeTags() + "\n";

                                    if (customrole.IsVanilla())
                                    {
                                        RoleInfo.text += $"\n<size=50%>{info.Description.Description.RemoveDeltext("。", "。\n")}\n";
                                    }
                                    else if (customrole is CustomRoles.MadAvenger)
                                    {
                                        var text = "";
                                        var i = 0;
                                        foreach (var desc in info.Description.Description.Split("\n"))
                                        {
                                            text += desc;
                                            i++;
                                            if (i % 2 == 1) text += "\n";
                                        }
                                        RoleInfo.text += $"\n<size=50%>{text}\n";
                                    }
                                    else
                                        RoleInfo.text += $"\n<size=50%>{info.Description.Description}\n";
                                }
                                else
                                {
                                    RoleInfo.text += $"\n\n<size=50%>{GetString($"{customrole}InfoLong")}\n";
                                }
                                {
                                    var sb = new StringBuilder();
                                    if (Options.CustomRoleSpawnChances.TryGetValue(customrole, out var op)) UtilsShowOption.ShowChildrenSettings(op, ref sb, IsOmitted: false);
                                    else if (customrole is CustomRoles.Braid) UtilsShowOption.ShowChildrenSettings(Options.CustomRoleSpawnChances[CustomRoles.Driver], ref sb, IsOmitted: false);
                                    else if (customrole is CustomRoles.Altair) UtilsShowOption.ShowChildrenSettings(Options.CustomRoleSpawnChances[CustomRoles.Vega], ref sb, IsOmitted: false);
                                    else if (customrole is CustomRoles.Nue) UtilsShowOption.ShowChildrenSettings(Options.CustomRoleSpawnChances[CustomRoles.Nue], ref sb, IsOmitted: false);

                                    if (sb.ToString() != "")
                                    {
                                        RoleInfo.text += $"\n<size=70%>{GetString("Settings")}</size>";
                                        var i = 0;
                                        foreach (var _text in sb.ToString().Split("\n"))
                                        {
                                            var text = _text;
                                            if (text == "") continue;
                                            text = text.RemoveDeltext("┗ ", "").RemoveDeltext("┣ ", "").RemoveDeltext("×", GetString("ColoredOff").RemoveDeltext("○", "ColoredOn"));
                                            RoleInfo.text += (i % 2 == 0) ? $"\n・{text}" : $"<pos=120%>・{text}";
                                            i++;
                                        }
                                    }
                                }
                                {
                                    List<CustomRoles> showlist = [customrole];
                                    switch (customrole)
                                    {
                                        case CustomRoles.JackalMafia:
                                        case CustomRoles.JackalAlien:
                                            showlist.Add(CustomRoles.Jackal);
                                            break;
                                        case CustomRoles.SwitchSheriff:
                                            showlist.Add(CustomRoles.Sheriff);
                                            break;
                                    }
                                    var AllAchievements = Achievement.AllAchievements.Where(x => showlist.Contains(x.Value.role));
                                    var text = "";
                                    AchievementText = "";
                                    (float c, float a) d = (0, 0);
                                    foreach (var achievementdata in AllAchievements)
                                    {
                                        var achievement = achievementdata.Value;
                                        var mark = "";
                                        var title = "";
                                        var constraint = "";
                                        var color = "";
                                        //if (achievement.IsHidden && achievement.IsCompleted is false && (50 > (d.c / d.a))) continue;
                                        switch (achievement.Difficulty)
                                        {
                                            case 0: mark += "◎"; color = "<#674020>"; break;
                                            case 1: mark += "◆"; color = "<#aacbf7>"; break;
                                            case 2: mark += "★"; color = "<#ffea4e>"; break;
                                        }
                                        if (achievement.IsHidden) mark = achievement.IsCompleted ? $"?-{mark}" : "？";
                                        text += $"{(achievement.IsCompleted ? $"{color}{mark}" : $"<#555555>-")}" + "  ";

                                        if (achievement.IsHidden is false || achievement.IsCompleted)
                                        {
                                            title += $"～{Achievements.GetAchievementNames(achievement, "Title")}～</color>";
                                            if (achievement.step > 1) title += $"   ({achievement.states}/{achievement.step})";
                                            var infotext = $"<size=70%>             {Achievements.GetAchievementNames(achievement, "Info", 2)}</size>";
                                            constraint = Achievements.GetAchievementNames(achievement, "Constraint", 2);
                                            if (achievement.IsCompleted is false)
                                            {
                                                infotext = $"<size=70%>             {Achievements.GetAchievementNames(achievement, "Info", 1)}</size>";
                                                constraint = Achievements.GetAchievementNames(achievement, "Constraint", 1);
                                            }
                                            if (infotext != "")
                                                title += $"{infotext}</color>";
                                            if (constraint != "")
                                                title += $"<size=50%>\n<#cccccc>{constraint}</color></size>";
                                        }
                                        else
                                        {
                                            title += $"～{(Main.UseingJapanese ? "隠し実績..." : "Hide Achievement")}～</color>";
                                            if (achievement.step > 1) title += $"   ({achievement.states}/{achievement.step})";
                                            title += $"<size=50%>\n<#cccccc>{Achievements.GetAchievementNames(achievement, "Constraint", 1)}</color></size>";
                                        }

                                        text += title + "\n";
                                        if (achievement.IsHidden is false) d = (d.c + (achievement.IsCompleted ? 1 : 0), d.a + 1);
                                    }
                                    if (text.RemoveHtmlTags() != "")
                                    {
                                        var achievementButton = Object.Instantiate(mouseMoveToggle, CustomBackground.transform);
                                        achievementButton.transform.localPosition = new(-2.4f, 2.5f, -100f);
                                        achievementButton.transform.localScale = new(0.2f, 0.9f, 1f);
                                        achievementButton.name = "Achievement";
                                        achievementButton.Text.text = " ";
                                        achievementButton.Background.color = ModColors.ModColor;
                                        AchievementPassiveButton = achievementButton.GetComponent<PassiveButton>();
                                        AchievementPassiveButton.OnMouseOut = new();
                                        AchievementPassiveButton.OnMouseOver = new();
                                        AchievementPassiveButton.OnClick = new();
                                        AchievementPassiveButton.OnMouseOut
                                            .AddListener((System.Action)(() => ToolTip.Hide()));
                                        AchievementPassiveButton.OnMouseOver
                                            .AddListener((System.Action)(() => ToolTip.Show(AchievementPassiveButton, AchievementText, null)));
                                        AchievementText += $"<align=\"left\"><size=120%>{GetString("Achievement")}\n{text}";
                                    }
                                    RoleInfo.gameObject.SetActive(true);
                                }
                            }));
                            buttons.Add(customrole, ToggleButton.gameObject);
                            numItems++;
                        }
                        Scrollbargameobject.GetComponentInParent<Scroller>().ContentYBounds.max = numItems > 9 ? 0.5f * (numItems / 4 - 9) : 0;
                        Scrollbargameobject.GetComponentInParent<Scroller>().Inner.localPosition = Vector3.zero;
                    }
                }
                catch (System.Exception ex) { Logger.Error($"{ex}", "RoleInfo"); }
            }
        }
        //Show 
        CustomBackground.gameObject.SetActive(true);
        Scrollbargameobject.gameObject.SetActive(true);
    }
    public static void CallEsc()
    {
        if (IsShowRoleInfo)
        {
            if (RoleInfo.IsNullOrDestroyed() is false)
            {
                IsShowRoleInfo = false;
                RoleInfo.gameObject.SetActive(false);
                RoleSprite.gameObject.SetActive(false);
            }
            return;
        }
        if (Scrollbargameobject?.gameObject is not null) Object.Destroy(Scrollbargameobject?.gameObject);
        if (CustomBackground?.gameObject is not null) Object.Destroy(CustomBackground.gameObject);
        if (RoleSprite?.gameObject is not null) Object.Destroy(RoleSprite.gameObject);

        IsShowInfo = false;
        CustomBackground = null;
        Scrollbargameobject = null;
        buttons = new();
    }
    public static void CloseOptionMenu()
    {
        IsShowInfo = false;
        IsShowRoleInfo = false;
        CustomBackground = null;
        Scrollbargameobject = null;
        buttons = new();
    }
}


[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.LateUpdate))]
class MainMenuManagerUpdatePatch
{
    public static void Postfix()
    {
        if (RoleInfoShower.IsShowRoleInfo) return;
        var numItems = 0;
        if ((RoleInfoShower.buttons?.Count ?? 0) == 0) return;
        foreach (var buttongameobjectdata in RoleInfoShower.buttons)
        {
            var buttongameobject = buttongameobjectdata.Value;
            if (buttongameobject is null) continue;
            bool IsActive = RoleInfoShower.NowTab switch
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
            if ((buttongameobject.transform.position.y - RoleInfoShower.CloseButtonY) > -0.2f ||
            (buttongameobject.transform.position.y - RoleInfoShower.CloseButtonY) < -4.2f)
            {
                IsActive = false;
            }
            if (!buttongameobject.active && IsActive)
            {
                buttongameobject.SetActive(true);
            }
            else
                if (buttongameobject.active && !IsActive)
                {
                    buttongameobject.SetActive(false);
                }
        }
        if (RoleInfoShower.Scrollbargameobject is null) return;
        RoleInfoShower.Scrollbargameobject.GetComponentInParent<Scroller>().ContentYBounds.max = numItems > 9 ? 0.5f * (numItems / 4 - 9) : 0;
    }
}