using System;
using HarmonyLib;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;
using TownOfHost.Modules.ClientOptions;
using System.Collections.Generic;
using System.Linq;

namespace TownOfHost;

public static class StreamerHopeMenu
{
    public static SpriteRenderer Popup { get; private set; }
    public static TextMeshPro TitleText { get; private set; }
    public static ToggleButtonBehaviour CancelButton { get; private set; }
    public static List<Hopeplayebutton> Hopeplayerinfos = new();

    public static void Init(OptionsMenuBehaviour optionsMenuBehaviour)
    {
        Popup = Object.Instantiate(optionsMenuBehaviour.Background, ClientActionItem.CustomBackground.transform.parent);
        Popup.name = "StreamHopePopup";
        Popup.transform.localPosition = new(0f, 0f, -5f);
        Popup.transform.localScale = new(1.7f, 0.9f, 1f);
        Popup.gameObject.SetActive(false);

        TitleText = Object.Instantiate(optionsMenuBehaviour.DisableMouseMovement.Text, Popup.transform);
        TitleText.name = "TitleText";
        TitleText.transform.localPosition = new(0, 2.4f, -1);
        TitleText.transform.localScale = new(0.7f, 1.3f, 1);
        TitleText.gameObject.SetActive(true);

        CancelButton = Object.Instantiate(optionsMenuBehaviour.DisableMouseMovement, Popup.transform);
        CancelButton.name = "Cancel";
        CancelButton.transform.localPosition = new(2.2f, -2.4f, -2);
        CancelButton.transform.localScale = new(0.3f, 0.7f, 1f);
        CancelButton.Text.text = Translator.GetString("ED.back");
        var cancelPassiveButton = CancelButton.GetComponent<PassiveButton>();
        cancelPassiveButton.OnClick = new();
        cancelPassiveButton.OnClick.AddListener((Action)Hide);
        CancelButton.gameObject.SetActive(true);
    }

    public static void Show(OptionsMenuBehaviour menuBehaviour)
    {
        if (Popup != null)
        {
            Popup.gameObject.SetActive(true);
            TitleText.text = Translator.GetString("StreamList");

            var i = 0;
            foreach (var hope in StreamerInfo.Hopeplayers.OrderBy(x => x.Key))
            {
                var button = Hopeplayebutton.Cratehopeplayerbutton(hope.Value, menuBehaviour, i);
                i++;
                Hopeplayerinfos.Add(button);
            }
        }
    }
    public static void Hide()
    {
        if (Popup != null)
        {
            Popup.gameObject.SetActive(false);
            Hopeplayerinfos.Do(x => x.Hide());
            Hopeplayerinfos.Clear();
        }
    }
}
public class Hopeplayebutton
{
    public TextMeshPro Nametext { get; private set; }
    public ToggleButtonBehaviour removebutton { get; private set; }
    public int Number { get; private set; }

    public static Hopeplayebutton Cratehopeplayerbutton(HopeInfo info, OptionsMenuBehaviour menuBehaviour, int number)
    {
        return new Hopeplayebutton(info, menuBehaviour, number);
    }

    public Hopeplayebutton(HopeInfo info, OptionsMenuBehaviour menuBehaviour, int number)
    {
        Nametext = Object.Instantiate(menuBehaviour.DisableMouseMovement.Text, StreamerHopeMenu.Popup.transform);
        Nametext.text = $"{number + 1}　<size=80%>{info.PlayerName} <size=60%>({info.AccountName}) [{info.Id}]";
        Nametext.name = $"{number + 1}Name";
        Nametext.alignment = TextAlignmentOptions.TopLeft;
        Nametext.transform.localPosition = new(number > 14 ? 1.136f : -1.5902f, 1.8f - (number % 15 * 0.3f), -5);
        Nametext.transform.localScale = new(0.5f, 1, 1);

        Number = number + 1;

        removebutton = Object.Instantiate(menuBehaviour.DisableMouseMovement, Nametext.transform);
        removebutton.name = $"{number + 1}removebutton";
        removebutton.transform.localPosition = new(-1.6f, 0, -5);
        removebutton.transform.localScale = new(0.4f, 0.5f, 1);
        removebutton.Text.text = Translator.GetString("Delete");
        var removePassiveButton = removebutton.GetComponent<PassiveButton>();
        removePassiveButton.OnClick = new();
        removePassiveButton.OnClick.AddListener((Action)(() =>
        {
            RemoveHope(info);
        }));
        removebutton.gameObject.SetActive(true);

        void RemoveHope(HopeInfo info)
        {
            Logger.Info($"{info.Id}", "del");
            StreamerInfo.Hopeplayers.Remove(info.Id);
            Nametext.text = $"<s>{Nametext.text}</s>";
            this.removebutton.gameObject.SetActive(false);
        }
    }
    public void Hide()
    {
        Object.Destroy(this.Nametext.gameObject); Object.Destroy(this.removebutton.gameObject);
    }
}
