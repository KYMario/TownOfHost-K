using System.Linq;
using HarmonyLib;
using TownOfHost.Roles.Core;
using UnityEngine;
using static TownOfHost.GameSettingMenuStartPatch;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Update))]
    public class GameOptionsMenuUpdatePatch
    {
        private static float _timer = 1f;
        public static void Postfix(GameOptionsMenu __instance)
        {
            _timer += Time.deltaTime;
            if (_timer < 0.1f) return;
            _timer = 0f;

            try
            {
                if (priset)
                {
                    priset.transform.localPosition = new Vector3(0f, 3.2f);
                    search.transform.localPosition = new Vector3(0f, 3.5f);
                    search.transform.localScale = priset.transform.localScale = new Vector3(0.4f, 0.4f, 0f);

                    activeonly.transform.localPosition = new Vector3(-2.05f, 3.3f);
                    activeonly.transform.localScale = new Vector3(0.6f, 0.6f, 0f);

                    searchtext.enabled = search.textArea.text == "";
                    prisettext.enabled = priset.textArea.text == "";

                    var active = ModSettingsButton?.selected ?? false;

                    searchtext.gameObject.SetActive(active);
                    prisettext.gameObject.SetActive(active);
                    search.gameObject.SetActive(active);
                    priset.gameObject.SetActive(active);
                    activeonly.gameObject.SetActive(active);
                }
                if (timer > 0)
                {
                    timer -= Time.fixedDeltaTime;
                }
                else if (timer > -10)
                {
                    timer = -100;

                    var rand = IRandom.Instance;
                    int rect = IRandom.Instance.Next(1, 101);
                    if (rect < 40)
                        GameSettingMenuChangeTabPatch.meg = GetString("ModSettingInfo0");
                    else if (rect < 50)
                        GameSettingMenuChangeTabPatch.meg = GetString("ModSettingInfo10");
                    else if (rect < 60)
                        GameSettingMenuChangeTabPatch.meg = GetString("ModSettingInfo1");
                    else if (rect < 70)
                        GameSettingMenuChangeTabPatch.meg = GetString("ModSettingInfo2");
                    else if (rect < 80)
                        GameSettingMenuChangeTabPatch.meg = GetString("ModSettingInfo3");
                    else if (rect < 90)
                        GameSettingMenuChangeTabPatch.meg = GetString("ModSettingInfo4");
                    else if (rect < 95)
                        GameSettingMenuChangeTabPatch.meg = GetString("ModSettingInfo5");
                    else if (rect < 99)
                        GameSettingMenuChangeTabPatch.meg = GetString("ModSettingInfo6");
                    else
                        GameSettingMenuChangeTabPatch.meg = GetString("ModSettingInfo7");
                }

                var isOnline = AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame;
                var localString = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.LocalButton);

                if (InfoTimer) InfoTimer.text = isOnline ? GameStartManagerPatch.GetTimerString() : localString;
                if (InfoCount) InfoCount.text = GameStartManager._instance.PlayerCounter.text;
            }
            catch { }

            if (__instance.transform.name == "GAME SETTINGS TAB") return;

            if (NowRoleTab is not CustomRoles.NotAssigned)
            {
                float numItems = __instance.Children.Count;
                var offset = Heightratio is 1 ? 2.7f : 2f;
                var y = 0.713f;
                foreach (var option in OptionItem.AllOptions)
                {
                    if (option?.OptionBehaviour == null || option.OptionBehaviour.gameObject == null) continue;
                    if (option.CustomRole != NowRoleTab && option.ParentRole != NowRoleTab) continue;

                    var p = option;
                    var parentrole = option.ParentRole;
                    while (p.Parent != null)
                    {
                        p = p.Parent;
                    }
                    if (option.ParentRole == NowRoleTab)
                    {
                        if (!crOptions[NowRoleTab].Contains(p.OptionBehaviour))
                        {
                            p.OptionBehaviour.transform.parent = crlist[option.ParentRole].transform;
                            crOptions[NowRoleTab].Add(p.OptionBehaviour);
                            scOptions[p.Tab].Remove(p.OptionBehaviour);
                        }
                    }
                    if (!crOptions[NowRoleTab].Contains(option.OptionBehaviour) && option.Name == "Maximum")
                    {
                        option.OptionBehaviour.transform.parent = crlist[parentrole].transform;
                        crOptions[NowRoleTab].Add(option.OptionBehaviour);
                        scOptions[option.Tab].Remove(option.OptionBehaviour);
                    }

                    var enabled = true;
                    var parent = option.Parent;
                    var opt = option.OptionBehaviour.LabelBackground;
                    var isroleoption = option.CustomRole is not CustomRoles.NotAssigned;
                    /*if (isroleoption && rolebutton.TryGetValue(option.CustomRole, out var button))
                    {
                        button.gameObject.SetActive(false);
                    }*/
                    if (roleInfobutton.TryGetValue(option.CustomRole, out var infobutton))
                    {
                        if (!infobutton.isActiveAndEnabled)
                        {
                            infobutton.gameObject.SetActive(true);
                        }
                    }
                    enabled = AmongUsClient.Instance.AmHost && !option.IsHiddenOn(Options.CurrentGameMode);
                    Color color = new Color32(200, 200, 200, 255);
                    Vector2 size = new(5.0f * Widthratio, 0.68f * Heightratio);

                    if (option.Tab is TabGroup.MainSettings && (option.NameColor != Color.white || option.NameColorCode != "#ffffff"))
                    {
                        color = option.NameColor == Color.white ? StringHelper.CodeColor(option.NameColorCode) : option.NameColor;

                        color = color.ShadeColor(-6);
                    }
                    if (isroleoption)
                    {
                        color = option.NameColor.ShadeColor(-5);
                    }

                    Transform titleText = option.OptionBehaviour.transform.Find("Title Text");
                    RectTransform titleTextRect = titleText.GetComponent<RectTransform>();

                    var i = 0;
                    while (parent != null && enabled)
                    {
                        i++;
                        enabled = parent.CustomRole is not CustomRoles.NotAssigned || parent.GetBool() || (parent.CustomRole.IsAddOn() && option.Name is not "%roleTypes%Maximum" and not "Maximum" and not "FixedRole");
                        parent = parent.Parent;
                    }

                    switch (i)
                    {
                        case 0:
                            break;
                        case 1:
                            color = new Color32(40, 50, 80, 255);
                            size = new(4.6f * Widthratio, 0.68f * Heightratio);
                            titleText.transform.localPosition = new Vector3(-1.8566f * Widthratio, 0f);
                            titleTextRect.sizeDelta = new Vector2(6.4f, 0.6f);
                            break;
                        case 2:
                            color = new Color32(20, 60, 40, 255);
                            size = new(4.4f * Widthratio, 0.68f * Heightratio);
                            titleText.transform.localPosition = new Vector3(-1.7566f * Widthratio, 0f);
                            titleTextRect.sizeDelta = new Vector2(6.35f, 0.6f);
                            break;
                        case 3:
                            color = new Color32(60, 20, 40, 255);
                            size = new(4.2f * Widthratio, 0.68f * Heightratio);
                            titleText.transform.localPosition = new Vector3(-1.6566f * Widthratio, 0f);
                            titleTextRect.sizeDelta = new Vector2(6.3f, 0.6f);
                            break;
                        case 4:
                            color = new Color32(60, 40, 10, 255);
                            size = new(4.0f * Widthratio, 0.68f * Heightratio);
                            titleText.transform.localPosition = new Vector3(-1.6566f * Widthratio, 0f);
                            titleTextRect.sizeDelta = new Vector2(6.25f, 0.6f);
                            break;
                    }

                    option.OptionBehaviour.gameObject.SetActive(enabled);
                    if (enabled)
                    {
                        opt.color = color;
                        opt.size = size;

                        offset -= option.IsHeader ? (0.68f * Heightratio) : (0.45f * Heightratio);
                        option.OptionBehaviour.transform.localPosition = new Vector3(
                            option.OptionBehaviour.transform.localPosition.x,//0.952f,
                            offset - (1.5f * Heightratio),//y,
                            option.OptionBehaviour.transform.localPosition.z);//-120f);
                        y -= option.IsHeader ? (0.68f * Heightratio) : (0.45f * Heightratio);

                        if (option.IsHeader)
                        {
                            numItems += (0.5f * Heightratio);
                        }
                    }
                    else
                    {
                        numItems--;
                    }
                }
                __instance.GetComponentInParent<Scroller>().ContentYBounds.max = -(offset * 3 - (Heightratio * offset * (Heightratio == 1 ? 2f : 2.2f))) + 0.75f;
                return;
            }

            #region  Tab
            foreach (var tab in EnumHelper.GetAllValues<TabGroup>())
            {
                if (__instance.gameObject.name != tab + "-Stg") continue;

                float numItems = __instance.Children.Count;
                var offset = Heightratio is 1 ? 2.7f : 2f;
                var y = 0.713f;

                foreach (var option in OptionItem.AllOptions)
                {
                    if ((TabGroup)tab != option.Tab) continue;
                    if (option?.OptionBehaviour == null || option.OptionBehaviour.gameObject == null) continue;
                    var enabled = true;
                    var parent = option.Parent;

                    enabled = AmongUsClient.Instance.AmHost && !option.IsHiddenOn(Options.CurrentGameMode);

                    var isroleoption = option.CustomRole is not CustomRoles.NotAssigned;

                    if (option.CustomRole is not CustomRoles.NotAssigned)
                    {
                        var p = option;
                        while (p.Parent != null)
                        {
                            p = p.Parent;
                        }
                        if (!scOptions[option.Tab].Contains(p.OptionBehaviour))
                        {
                            p.OptionBehaviour.transform.parent = list[option.Tab].transform;
                            scOptions[option.Tab].Add(p.OptionBehaviour);
                            crOptions[option.CustomRole].Remove(p.OptionBehaviour);
                        }
                    }
                    else
                    {
                        if (!scOptions[option.Tab].Contains(option.OptionBehaviour) && option.Name == "Maximum")
                        {
                            option.OptionBehaviour.transform.parent = list[option.Tab].transform;
                            scOptions[option.Tab].Add(option.OptionBehaviour);
                            crOptions[option.ParentRole].Remove(option.OptionBehaviour);
                        }
                    }
                    if (!isroleoption && option.ParentRole is not CustomRoles.NotAssigned && option.Name is not "Maximum")
                    {
                        option.OptionBehaviour?.gameObject?.SetActive(false);
                        continue;
                    }

                    //起動時以外で表示/非表示を切り替える際に使う
                    if (enabled)
                    {
                        if (ActiveOnlyMode || (ShowFilter.NowSettingRole is not CustomRoles.NotAssigned && option.CustomRole.IsSubRole()))
                        {
                            if (isroleoption)
                            {
                                enabled = option.GetBool();
                            }
                            if (OptionShower.Checkenabled(option) is false or null)
                            {
                                var v = OptionShower.Checkenabled(option);
                                enabled = v is not null && option.GetBool();
                            }
                            if (option.Parent is not null)
                                if (OptionShower.Checkenabled(option.Parent) is false or null)
                                {
                                    var v = OptionShower.Checkenabled(option.Parent);
                                    enabled = v is not null && option.Parent.GetBool();
                                }
                        }
                        if (!Event.CheckRole(option.CustomRole))
                        {
                            enabled = false;
                        }
                    }
                    var opt = option.OptionBehaviour.LabelBackground;
                    if (isroleoption && rolebutton.TryGetValue(option.CustomRole, out var button))
                    {
                        button.gameObject.SetActive(!(3.5f < opt.transform.position.y || opt.transform.position.y <= -0.4));

                        if (roleInfobutton.TryGetValue(option.CustomRole, out var infobutton))
                        {
                            if (!infobutton.isActiveAndEnabled) infobutton.gameObject.SetActive(true);
                        }
                    }

                    Color color = new Color32(200, 200, 200, 255);
                    Vector2 size = new(5.0f * Widthratio, 0.68f * Heightratio);

                    if (option.Tab is TabGroup.MainSettings && (option.NameColor != Color.white || option.NameColorCode != "#ffffff"))
                    {
                        color = option.NameColor == Color.white ? StringHelper.CodeColor(option.NameColorCode) : option.NameColor;

                        color = color.ShadeColor(-6);
                    }
                    if (isroleoption)
                    {
                        color = option.NameColor.ShadeColor(-5);
                    }
                    if (!isroleoption && ShowFilter.NowSettingRole is not CustomRoles.NotAssigned)
                    {
                        enabled = false;
                    }
                    if (isroleoption && ShowFilter.NosetOptin is FilterOptionItem filterOption)
                    {
                        var notassings = filterOption?.NotAssin?.Invoke() ?? [];
                        if (notassings.Contains(option.CustomRole))
                        {
                            enabled = false;
                        }
                    }

                    Transform titleText = option.OptionBehaviour.transform.Find("Title Text");
                    RectTransform titleTextRect = titleText.GetComponent<RectTransform>();

                    var i = 0;
                    while (parent != null && enabled)
                    {
                        i++;
                        enabled = parent.GetBool();
                        parent = parent.Parent;
                    }

                    switch (i)
                    {
                        case 0:
                            break;
                        case 1:
                            color = new Color32(40, 50, 80, 255);
                            size = new(4.6f * Widthratio, 0.68f * Heightratio);
                            titleText.transform.localPosition = new Vector3(-1.8566f * Heightratio, 0f);
                            titleTextRect.sizeDelta = new Vector2(6.4f, 0.6f);
                            break;
                        case 2:
                            color = new Color32(20, 60, 40, 255);
                            size = new(4.4f * Widthratio, 0.68f * Heightratio);
                            titleText.transform.localPosition = new Vector3(-1.7566f * Widthratio, 0f);
                            titleTextRect.sizeDelta = new Vector2(6.35f, 0.6f);
                            break;
                        case 3:
                            color = new Color32(60, 20, 40, 255);
                            size = new(4.2f * Widthratio, 0.68f * Heightratio);
                            titleText.transform.localPosition = new Vector3(-1.6566f * Widthratio, 0f);
                            titleTextRect.sizeDelta = new Vector2(6.3f, 0.6f);
                            break;
                        case 4:
                            color = new Color32(60, 40, 10, 255);
                            size = new(4.0f * Widthratio, 0.68f * Heightratio);
                            titleText.transform.localPosition = new Vector3(-1.6566f * Widthratio, 0f);
                            titleTextRect.sizeDelta = new Vector2(6.25f, 0.6f);
                            break;
                    }

                    option.OptionBehaviour.gameObject.SetActive(enabled);
                    if (enabled)
                    {
                        opt.color = color;
                        opt.size = size;
                        offset -= option.IsHeader ? (0.68f * Heightratio) : (0.45f * Heightratio);
                        option.OptionBehaviour.transform.localPosition = new Vector3(
                            option.OptionBehaviour.transform.localPosition.x,//0.952f,
                            offset - (1.5f * Heightratio),//y,
                            option.OptionBehaviour.transform.localPosition.z);//-120f);
                        y -= option.IsHeader ? (0.68f * Heightratio) : (0.45f * Heightratio);

                        if (option.IsHeader)
                        {
                            numItems += (0.5f * Heightratio);
                        }
                    }
                    else
                    {
                        numItems--;
                    }
                }
                __instance.GetComponentInParent<Scroller>().ContentYBounds.max = -(offset * 3 - (Heightratio * offset * (Heightratio == 1 ? 2f : 2.2f))) + 0.75f;
            }
            #endregion
        }
    }
    [HarmonyPatch(typeof(GameOptionButton), nameof(GameOptionButton.SetInteractable))]
    class GameOptionButtonSetInteractablePatch
    {
        public static bool Prefix(GameOptionButton __instance)
        {
            __instance.isInteractable = true;
            return false;
        }
    }
    [HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.Update))]
    class GameSettingMenuUpdataPatch
    {
        public static void Postfix(GameSettingMenu __instance)
        {
            if (ModSettingsButton?.selected ?? false) __instance.MenuDescriptionText.text = GameSettingMenuChangeTabPatch.meg;
            var settingsButton = __instance.GameSettingsTab?.gameObject;
            var allButton = ModSettingsTab?.AllButton?.gameObject;
            if (settingsButton?.active == true && (ModSettingsButton?.selected ?? false) && __instance?.GameSettingsTab is not null)
            {
                __instance.GameSettingsTab?.gameObject?.SetActive(false);
            }
            if (allButton?.active == true)
                allButton?.SetActive(false);
        }
    }
}