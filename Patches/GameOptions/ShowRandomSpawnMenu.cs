using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TownOfHost.Roles.Core;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost;

class ShowRandomSpawnOption
{
    public static bool IsShowFilter = false;
    public static SpriteRenderer CustomBackground { get; private set; }//背景
    public static SpriteRenderer Mapimage { get; private set; }
    public static Dictionary<OptionItem, GameObject> buttons = new();

    public static void CreateSpawanOptionMenu(Transform tabtransform, MapNames map)
    {
        if (CustomBackground is null)
        {
            buttons = new();
            if (IsShowFilter) return;
            IsShowFilter = true;

            if (CustomBackground?.gameObject is not null) Object.Destroy(CustomBackground.gameObject);
            if (Mapimage?.gameObject is not null) Object.Destroy(Mapimage.gameObject);

            CustomBackground = null;
            Mapimage = null;

            if (map == MapNames.Dleks) map = MapNames.Skeld;
            OptionsMenuBehaviour menuBehaviour = HudManager.Instance.GameMenu;
            {
                try
                {
                    var mouseMoveToggle = menuBehaviour.DisableMouseMovement;

                    if (CustomBackground is null)
                    {
                        CustomBackground = Object.Instantiate(menuBehaviour.Background, tabtransform.parent);
                        CustomBackground.name = "RandomSpawnBackground";
                        CustomBackground.transform.localScale = new(1.7f, 0.9f, 1f);
                        CustomBackground.transform.localPosition = new Vector3(0, 0, -500);
                        CustomBackground.gameObject.SetActive(false);
                        CustomBackground.GetComponent<BoxCollider2D>().size = new Vector2(100, 100);

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
                    Mapimage = Object.Instantiate(menuBehaviour.Background, CustomBackground.transform);
                    Mapimage.name = "Mapimage";
                    Mapimage.transform.localPosition = new Vector3(0, 0, -10);
                    Mapimage.transform.localScale = new Vector3(map is MapNames.Polus or MapNames.MiraHQ ? 0.75f : 0.85f, 0.85f);
                    Mapimage.transform.DestroyChildren();
                    Mapimage.sprite = UtilsSprite.LoadSprite($"TownOfHost.Resources.AmongUs.Map_{map}.png");
                    Mapimage.gameObject.SetActive(true);

                    var pinsprite = UtilsSprite.LoadSprite("TownOfHost.Resources.pin.png");
                    var id = 103000 + ((int)map * 100);
                    List<(ToggleButtonBehaviour button, OptionItem opt)> buttons = new();
                    var options = OptionItem.AllOptions.Where(option => option.Id < id + 100 && id < option.Id);
                    foreach (var option in options)
                    {
                        var button = Object.Instantiate(mouseMoveToggle, CustomBackground.transform);
                        button.GetComponent<BoxCollider2D>().size = new Vector2(0.2f, 0.5f);
                        button.transform.localPosition = GetMapposition(option.Id);
                        button.transform.SetLocalZ(-15);
                        button.name = option.Name;
                        button.Text.DestroyTranslator();
                        button.Text.text = "";
                        Object.Destroy(button.transform.GetChild(1).gameObject);
                        button.Background.sprite = pinsprite;
                        button.Background.transform.localScale = new(0.1455f, 1.44f, 1);
                        button.Background.color = option.CurrentValue is 0 ? Palette.DisabledGrey : Palette.EnabledColor;
                        var passive = button.GetComponent<PassiveButton>();
                        passive.HeldButtonSprite = null;
                        passive.OnMouseOver.AddListener(new System.Action(() =>
                        {
                            button.Background.color = Palette.AcceptedGreen;
                            ToolTip.Show(passive, option.GetName() + (option.CurrentValue is 0 ? " (Off)" : " (ON)"), null, 0.1f);
                        }));
                        passive.OnMouseOut.AddListener(new System.Action(() =>
                        {
                            button.Background.color = option.CurrentValue is 0 ? Palette.DisabledGrey : Palette.EnabledColor;
                            ToolTip.Hide();
                        }));
                        passive.OnClick = new();
                        passive.OnClick.AddListener(new System.Action(() =>
                        {
                            option.SetValue(option.CurrentValue + 1);
                            button.Background.color = option.CurrentValue is 0 ? Palette.DisabledGrey : Palette.EnabledColor;

                            if (options.All(opt => opt.CurrentValue == 0))
                            {
                                OptionItem.AllOptions.First(opt => opt.Id == id).SetValue(0);
                            }
                            else
                            {
                                OptionItem.AllOptions.First(opt => opt.Id == id).SetValue(1);
                            }
                            ToolTip.Hide();
                            ToolTip.Show(passive, option.GetName() + (option.CurrentValue is 0 ? " (Off)" : " (ON)"), null, 0.1f);
                        }));
                        buttons.Add((button, option));
                    }

                    var allonbutton = Object.Instantiate(mouseMoveToggle, CustomBackground.transform);
                    allonbutton.transform.localPosition = new(1.2f, 2.5f, -60f);
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
                            if (data.button.Background.color == Palette.DisabledGrey)
                            {
                                data.opt.SetValue(data.opt.CurrentValue + 1, false, false);
                                data.button.Background.color = data.opt.CurrentValue is 0 ? Palette.DisabledGrey : Palette.EnabledColor;
                            }
                        }
                        OptionItem.AllOptions.First(opt => opt.Id == id).SetValue(1, false, false);
                        OptionItem.SyncAllOptions();
                        Modules.OptionSaver.Save();
                    }));

                    var alloffbutton = Object.Instantiate(mouseMoveToggle, CustomBackground.transform);
                    alloffbutton.transform.localPosition = new(2.1f, 2.5f, -60f);
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
                            if (data.button.Background.color == Palette.EnabledColor)
                            {
                                data.opt.SetValue(0, false, false);
                                data.button.Background.color = data.opt.CurrentValue is 0 ? Palette.DisabledGrey : Palette.EnabledColor;
                            }
                        }
                        OptionItem.AllOptions.First(opt => opt.Id == id).SetValue(0, false, false);
                        OptionItem.SyncAllOptions();
                        Modules.OptionSaver.Save();
                    }));
                }
                catch (System.Exception ex) { Logger.Error(ex.ToString(), "eerrre"); }
            }
        }
        //Show 
        CustomBackground.gameObject.SetActive(true);
    }
    public static bool CallEsc(bool IsPrefix = false)
    {
        if (CustomBackground?.gameObject is not null) Object.Destroy(CustomBackground.gameObject);
        if (Mapimage?.gameObject is not null) Object.Destroy(Mapimage.gameObject);

        CustomBackground = null;
        Mapimage = null;
        buttons = new();
        var old = IsShowFilter;
        if (IsPrefix is false) IsShowFilter = false;
        return old;
    }
    public static void CloseOptionMenu()
    {
        Mapimage = null;
        CustomBackground = null;
        buttons = new();
    }
    static Vector2 GetMapposition(int id)
    {
        var pos = new Vector2();
        switch (id)
        {
            //Skeld
            case 103001: pos = new Vector2(0.1519f, 1.3571f); break;
            case 103002: pos = new Vector2(1.2436f, 1.3571f); break;
            case 103003: pos = new Vector2(1.2336f, -1.2869f); break;
            case 103004: pos = new Vector2(0.0722f, -1.9f); break;
            case 103005: pos = new Vector2(-1.5896f, -1.3339f); break;
            case 103006: pos = new Vector2(-1.5896f, 1.3445f); break;
            case 103007: pos = new Vector2(0.9452f, 0.4384f); break;
            case 103008: pos = new Vector2(2.0659f, 0.2333f); break;
            case 103009: pos = new Vector2(0.6647f, -1.9899f); break;
            case 103010: pos = new Vector2(0.7222f, -0.4921f); break;
            case 103011: pos = new Vector2(-0.6517f, -0.7061f); break;
            case 103012: pos = new Vector2(-1.1736f, 0.0778f); break;
            case 103013: pos = new Vector2(-2.0758f, 0.1222f); break;
            case 103014: pos = new Vector2(-0.665f, 0.2978f); break;
            //Mira
            case 103101: pos = new Vector2(1.6672f, -1.3533f); break;
            case 103102: pos = new Vector2(0.662f, -1.0931f); break;
            case 103103: pos = new Vector2(-0.2712f, -0.8034f); break;
            case 103104: pos = new Vector2(-0.6515f, 0.2111f); break;
            case 103105: pos = new Vector2(-1.3505f, -1.4889f); break;
            case 103106: pos = new Vector2(1.2308f, 1.4f); break;
            case 103107: pos = new Vector2(1.5061f, -1.9461f); break;
            case 103108: pos = new Vector2(1.0739f, -1.2164f); break;
            case 103109: pos = new Vector2(0.9143f, 0.2222f); break;
            case 103110: pos = new Vector2(0.662f, -1.8131f); break;
            case 103111: pos = new Vector2(0.096f, -1.2988f); break;
            case 103112: pos = new Vector2(0.0791f, 0.2111f); break;
            case 103113: pos = new Vector2(0.5844f, 1.4f); break;
            case 103114: pos = new Vector2(0.9143f, 2.0796f); break;
            //Polus
            case 103201: pos = new Vector2(-0.0997f, -0.681f); break;
            case 103202: pos = new Vector2(-1.8448f, -1.8513f); break;
            case 103203: pos = new Vector2(-1.7596f, 0.2778f); break;
            case 103204: pos = new Vector2(-0.4003f, 1.6228f); break;
            case 103205: pos = new Vector2(1.5685f, 0.9766f); break;
            case 103206: pos = new Vector2(1.5831f, -1.3659f); break;
            case 103207: pos = new Vector2(0.5601f, -0.681f); break;
            case 103208: pos = new Vector2(0.3221f, -1.6069f); break;
            case 103209: pos = new Vector2(-0.9046f, -0.6081f); break;
            case 103210: pos = new Vector2(-0.866f, -1.9133f); break;
            case 103211: pos = new Vector2(-1.859f, -0.6681f); break;
            case 103212: pos = new Vector2(-1.176f, 0.0552f); break;
            case 103213: pos = new Vector2(0.0037f, 0.298f); break;
            case 103214: pos = new Vector2(0.6091f, 0.9766f); break;
            case 103215: pos = new Vector2(1.3326f, 0.5257f); break;
            //AirShip
            case 103401: pos = new Vector2(-0.5786f, 1.3638f); break;
            case 103402: pos = new Vector2(-0.599f, 0.0222f); break;
            case 103403: pos = new Vector2(-0.9994f, -1.521f); break;
            case 103404: pos = new Vector2(1.8731f, -0.2f); break;
            case 103405: pos = new Vector2(0.9143f, 1.3638f); break;
            case 103406: pos = new Vector2(0.6013f, 0.1444f); break;
            case 103407: pos = new Vector2(-0.0706f, 0.5919f); break;
            case 103408: pos = new Vector2(0.6353f, 2.2909f); break;
            case 103409: pos = new Vector2(0.316f, 1.3891f); break;
            case 103410: pos = new Vector2(-1.1372f, 1.8281f); break;
            case 103411: pos = new Vector2(-1.465f, 0.2444f); break;
            case 103412: pos = new Vector2(-2.1474f, -0.2f); break;
            case 103413: pos = new Vector2(-1.2588f, -0.8465f); break;
            case 103414: pos = new Vector2(-1.5042f, -1.8426f); break;
            case 103415: pos = new Vector2(0f, -1.6408f); break;
            case 103416: pos = new Vector2(0.6616f, -1.0562f); break;
            case 103417: pos = new Vector2(1.1986f, -1.066f); break;
            case 103418: pos = new Vector2(1.8043f, 1.1667f); break;
            case 103419: pos = new Vector2(1.0778f, -0.0889f); break;
            //Fungle
            case 103501: pos = new Vector2(-1.2911f, -1.099f); break;
            case 103502: pos = new Vector2(-1.7499f, 0.4566f); break;
            case 103503: pos = new Vector2(-0.737f, 0.2333f); break;
            case 103504: pos = new Vector2(0.7185f, -1.5968f); break;
            case 103505: pos = new Vector2(1.7112f, 1.9f); break;
            case 103506: pos = new Vector2(1.2852f, 0.3329f); break;
            case 103507: pos = new Vector2(-1.5377f, 1.0364f); break;
            case 103508: pos = new Vector2(-1.3494f, -0.1444f); break;
            case 103509: pos = new Vector2(-0.7074f, 1.843f); break;
            case 103510: pos = new Vector2(-0.018f, 0.7608f); break;
            case 103511: pos = new Vector2(-0.2455f, -0.2333f); break;
            case 103512: pos = new Vector2(0.1506f, -0.0556f); break;
            case 103513: pos = new Vector2(-0.4142f, -1.3f); break;
            case 103514: pos = new Vector2(1.7265f, -1.1184f); break;
            case 103515: pos = new Vector2(0.318f, -0.6889f); break;
            case 103516: pos = new Vector2(1.3478f, -2.12f); break;
            case 103517: pos = new Vector2(0.5524f, 0.3612f); break;
            case 103518: pos = new Vector2(0.9881f, 1.3552f); break;
            case 103519: pos = new Vector2(1.8849f, 0.6444f); break;
            case 103520: pos = new Vector2(1.6039f, 1.2933f); break;
        }
        return pos;
    }
}