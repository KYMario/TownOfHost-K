using System;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;
using AmongUs.Data;

namespace TownOfHost.Modules.ClientOptions;

public static class SoundSettingsScreen
{
    public static SpriteRenderer Popup { get; private set; }
    public static TextMeshPro Text { get; private set; }
    public static SlideBar Music { get; private set; }
    public static SlideBar Sound { get; private set; }
    public static SlideBar Ambience { get; private set; }
    public static SlideBar MapTheme { get; private set; }

    public static void Init(OptionsMenuBehaviour optionsMenuBehaviour)
    {
        Popup = Object.Instantiate(optionsMenuBehaviour.Background, ClientActionItem.CustomBackground.transform.parent);
        Popup.name = "SoundSettingsPopup";
        Popup.transform.localPosition = new(0f, 0f, -8f);
        Popup.transform.localScale = new(0.8f, 0.8f, 1f);
        Popup.gameObject.SetActive(false);

        Text = Object.Instantiate(optionsMenuBehaviour.DisableMouseMovement.Text, Popup.transform);
        Text.name = "Sound Settings";
        Text.text = $"<size=3>{Translator.GetString("SoundOptions")}";
        Text.transform.localPosition = new(0f, 2.25f, -1f);
        Text.gameObject.SetActive(true);

        var closeButton = Object.Instantiate(optionsMenuBehaviour.DisableMouseMovement, Popup.transform);
        closeButton.transform.localPosition = new(1.3f, -2.3f, -6f);
        closeButton.name = "Close";
        closeButton.Text.text = Translator.GetString("Close");
        closeButton.Background.color = Palette.DisabledGrey;
        var closePassiveButton = closeButton.GetComponent<PassiveButton>();
        closePassiveButton.OnClick = new();
        closePassiveButton.OnClick.AddListener(new Action(() =>
        {
            Popup.gameObject.SetActive(false);
        }));

        CreateButton(optionsMenuBehaviour, Buttons.Music, 1f);
        CreateButton(optionsMenuBehaviour, Buttons.Sound, 1f);
        CreateButton(optionsMenuBehaviour, Buttons.Ambience, 0.298f);
        CreateButton(optionsMenuBehaviour, Buttons.MapTheme, -0.1f);

        Sound.SetValue(DataManager.Settings.Audio.SfxVolume);
        Music.SetValue(DataManager.Settings.Audio.MusicVolume);
        Ambience.SetValue(DataManager.Settings.Audio.AmbienceVolume);
        MapTheme.SetValue(Main.MapTheme.Value);

        optionsMenuBehaviour.MusicSlider.transform.localPosition = new Vector3(100, 100);
        optionsMenuBehaviour.SoundSlider.transform.localPosition = new Vector3(100, 100);
    }

    private static void CreateButton(OptionsMenuBehaviour optionsMenuBehaviour, Buttons buttons, float y)
    {
        var Slideer = Object.Instantiate(buttons is Buttons.Music ? optionsMenuBehaviour.MusicSlider : optionsMenuBehaviour.SoundSlider, Popup.transform);
        Slideer.transform.localPosition += new Vector3(-0.4f, y);
        Slideer.Bar.transform.localPosition -= new Vector3(0.4f, 0);
        Slideer.transform.transform.SetLocalZ(-1f);
        Slideer.gameObject.SetActive(true);
        Slideer.OnValueChange = new();
        Slideer.OnValueChange.AddListener((Action)(() =>
        {
            switch (buttons)
            {
                case Buttons.Music:
                    DataManager.Settings.Audio.MusicVolume = Music.Value;
                    break;
                case Buttons.Sound:
                    DataManager.Settings.Audio.SfxVolume = Sound.Value;
                    break;
                case Buttons.Ambience:
                    DataManager.Settings.Audio.AmbienceVolume = Ambience.Value;
                    break;
                case Buttons.MapTheme:
                    Main.MapTheme.Value = MapTheme.Value;
                    UpdateMapTheme();
                    break;
            }
            SoundManager.instance.OnAudioSettingsChanged();
        }));
        Slideer.gameObject.name = $"{buttons}-Setting";

        var titleTMP = Slideer.transform.FindChild("Text_TMP").GetComponent<TextMeshPro>();
        var titleRect = Slideer.transform.FindChild("Text_TMP").GetComponent<RectTransform>();
        titleTMP.enableWordWrapping = true;
        titleTMP.overflowMode = TextOverflowModes.Overflow;
        titleTMP.enableAutoSizing = true;
        titleTMP.fontSizeMax = 2.1f;
        titleTMP.fontSizeMin = 1.25f;
        titleRect.offsetMax = new(0.18f, titleRect.offsetMax.y);
        titleTMP.transform.SetLocalX(0.2498f);

        var button = Object.Instantiate(optionsMenuBehaviour.DisableMouseMovement, Slideer.transform);
        button.transform.localPosition = new Vector3(4.5f, 0f, 0f);
        button.transform.localScale -= new Vector3(0.4f, 0.4f);
        button.Text.text = Translator.GetString("Reset");

        var passiveButton = button.GetComponent<PassiveButton>();
        button.Background.size -= new Vector2(0.2f, 0f);
        button.transform.FindChild("ButtonHighlight").GetComponent<SpriteRenderer>().size -= new Vector2(0.2f, 0f);

        passiveButton.OnClick = new();
        switch (buttons)
        {
            case Buttons.Music:
                Music = Slideer;
                passiveButton.OnClick.AddListener((Action)(() =>
                {
                    DataManager.Settings.Audio.MusicVolume = AmongUs.Data.Settings.AudioSettingsData.DEFAULT_MUSIC_VOLUME;
                    Music.Value = DataManager.Settings.Audio.MusicVolume;
                    Music.UpdateValue();
                }));
                break;
            case Buttons.Sound:
                Sound = Slideer;
                passiveButton.OnClick.AddListener((Action)(() =>
                {
                    DataManager.Settings.Audio.SfxVolume = AmongUs.Data.Settings.AudioSettingsData.DEFAULT_SFX_VOLUME;
                    Sound.Value = DataManager.Settings.Audio.SfxVolume;
                    Sound.UpdateValue();
                }));
                break;
            case Buttons.Ambience:
                titleTMP.DestroyTranslator();
                titleTMP.text = Translator.GetString("Ambience");
                Ambience = Slideer;
                passiveButton.OnClick.AddListener((Action)(() =>
                {
                    DataManager.Settings.Audio.AmbienceVolume = AmongUs.Data.Settings.AudioSettingsData.DEFAULT_AMBIENCE_VOLUME;
                    Ambience.Value = DataManager.Settings.Audio.AmbienceVolume;
                    Ambience.UpdateValue();
                }));
                break;
            case Buttons.MapTheme:
                titleTMP.DestroyTranslator();
                titleTMP.text = $"{Translator.GetString("LobbyBGM")}";
                MapTheme = Slideer;
                passiveButton.OnClick.AddListener((Action)(() =>
                {
                    Main.MapTheme.Value = AmongUs.Data.Settings.AudioSettingsData.DEFAULT_AMBIENCE_VOLUME - 0.1f;
                    MapTheme.Value = Main.MapTheme.Value;
                    MapTheme.UpdateValue();
                    UpdateMapTheme();
                }));
                break;
        }
    }
    public static void Show()
    {
        if (Popup != null)
        {
            Popup.gameObject.SetActive(true);
        }
    }
    public static void Hide()
    {
        if (Popup != null)
        {
            Popup.gameObject.SetActive(false);
        }
    }
    public static void UpdateMapTheme()
    {
        for (int index = 0; index < SoundManager.instance.soundPlayers.Count; ++index)
        {
            ISoundPlayer soundPlayer = SoundManager.Instance.soundPlayers[index];
            if (soundPlayer.Name == "MapTheme")
            {
                soundPlayer.Player.maxDistance = Main.MapTheme.Value;
                soundPlayer.Player.volume = Main.MapTheme.Value;
                break;
            }
        }
    }
    enum Buttons
    {
        Sound,
        Music,
        Ambience,
        MapTheme
    }
}
