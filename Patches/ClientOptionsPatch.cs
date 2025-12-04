using HarmonyLib;
using UnityEngine;

using TownOfHost.Modules.ClientOptions;
using Rewired.Utils;

namespace TownOfHost
{
    [HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Start))]
    public static class OptionsMenuBehaviourStartPatch
    {
        private static ClientActionItem ForceJapanese;
        private static ClientActionItem JapaneseRoleName;
        private static ClientActionItem UnloadMod;
        private static ClientActionItem DumpLog;
        private static ClientActionItem OpenLogFolder;
        private static ClientActionItem ForceEnd;
        private static ClientActionItem UseWebHook;
        private static ClientActionItem Yomiage;
        private static ClientActionItem CustomName;
        private static ClientActionItem CustomSprite;
        private static ClientActionItem HideSomeFriendCodes;
        private static ToggleButtonBehaviour soundSettingsButton;
        public static ToggleButtonBehaviour StreamHopeButton;
        private static ClientActionItem ViewPingDetails;
        private static ClientActionItem DebugChatopen;
        private static ClientActionItem DebugSendAmout;
        private static ClientActionItem DebugTours;
        private static ClientActionItem ShowDistance;
        private static ClientActionItem FpsLimitRemoval;
        private static ClientActionItem AutoSaveScreenShot;

        public static void Postfix(OptionsMenuBehaviour __instance)
        {
            if (__instance.DisableMouseMovement == null)
            {
                return;
            }

            if (ForceJapanese == null || ForceJapanese.ToggleButton == null)
            {
                ForceJapanese = ClientOptionItem.Create("ForceJapanese", Main.ForceJapanese, __instance);
            }
            if (JapaneseRoleName == null || JapaneseRoleName.ToggleButton == null)
            {
                JapaneseRoleName = ClientOptionItem.Create("JapaneseRoleName", Main.JapaneseRoleName, __instance);
            }
            if (DumpLog == null || DumpLog.ToggleButton == null)
            {
                if (Main.IsAndroid() is false)
                    DumpLog = ClientActionItem.Create("DumpLog", UtilsOutputLog.DumpLog, __instance);
            }
            if (OpenLogFolder == null || OpenLogFolder.ToggleButton == null)
            {
                if (Main.IsAndroid() is false)
                    OpenLogFolder = ClientActionItem.Create("OpenLogFolder", UtilsOutputLog.OpenLogFolder, __instance);
            }
            if (UnloadMod == null || UnloadMod.ToggleButton == null)
            {
                if (Main.IsAndroid() is false)
                    UnloadMod = ClientActionItem.Create("UnloadMod", ModUnloaderScreen.Show, __instance);
            }
            if ((ForceEnd == null || ForceEnd.ToggleButton == null) && AmongUsClient.Instance.AmHost && !CustomSpawnEditor.ActiveEditMode)
            {
                ForceEnd = ClientActionItem.Create("ForceEnd", ForceEndProcess, __instance);
            }
            if (UseWebHook == null || UseWebHook.ToggleButton == null)
            {
                if (Main.IsAndroid() is false)
                    UseWebHook = ClientOptionItem.Create("UseWebHook", Main.UseWebHook, __instance);
            }
            if (Yomiage == null || Yomiage.ToggleButton == null)
            {
                if (Main.IsAndroid() is false)
                    Yomiage = ClientOptionItem.Create("UseYomiage", Main.UseYomiage, __instance);
            }
            if (CustomSprite == null || CustomSprite.ToggleButton == null)
            {
                CustomSprite = ClientOptionItem.Create("CustomSprite", Main.CustomSprite, __instance, () =>
                {
                    if (GameStates.InGame)
                        CustomButtonHud.BottonHud();
                });
            }
            if (HideSomeFriendCodes == null || HideSomeFriendCodes.ToggleButton == null)
            {
                HideSomeFriendCodes = ClientOptionItem.Create("HideSomeFriendCodes", Main.HideSomeFriendCodes, __instance);
            }
            if (AutoSaveScreenShot == null || AutoSaveScreenShot.ToggleButton == null)
            {
                if (Main.IsAndroid() is false)
                    AutoSaveScreenShot = ClientOptionItem.Create("AutoSaveScreenShot", Main.AutoSaveScreenShot, __instance);
            }
#if DEBUG
            if (ViewPingDetails == null || ViewPingDetails.ToggleButton == null)
            {
                ViewPingDetails = ClientOptionItem.Create("ViewPingDetails", Main.ViewPingDetails, __instance);
            }
            if (DebugChatopen == null || DebugChatopen.ToggleButton == null)
            {
                DebugChatopen = ClientOptionItem.Create("DebugChatopen", Main.DebugChatopen, __instance);
            }
            if (DebugSendAmout == null || DebugSendAmout.ToggleButton == null)
            {
                DebugSendAmout = ClientOptionItem.Create("DebugSendAmout", Main.DebugSendAmout, __instance);
            }
            if (ShowDistance == null || ShowDistance.ToggleButton == null)
            {
                ShowDistance = ClientOptionItem.Create("ShowDistance", Main.ShowDistance, __instance);
            }
            if (DebugTours == null || DebugTours.ToggleButton == null)
            {
                DebugTours = ClientOptionItem.Create("DebugTours", Main.DebugTours, __instance);
            }
            if (FpsLimitRemoval == null || FpsLimitRemoval.ToggleButton == null)
            {
                Application.targetFrameRate = Main.FpsLimitRemoval.Value ? -1 : 60;//起動時
                FpsLimitRemoval = ClientOptionItem.Create("FpsLimitRemoval", Main.FpsLimitRemoval, __instance, () =>
                {
                    Application.targetFrameRate = Main.FpsLimitRemoval.Value ? -1 : 60;//クリック時
                });
            }
#endif
            if ((CustomName == null || CustomName.ToggleButton == null) && Event.IsEventDay)
            {
                CustomName = ClientOptionItem.Create("CustomName", Main.CustomName, __instance);
            }
            if (ModUnloaderScreen.Popup == null)
            {
                ModUnloaderScreen.Init(__instance);
            }
            if (SoundSettingsScreen.Popup == null)
            {
                SoundSettingsScreen.Init(__instance);
            }
            if (StreamerHopeMenu.Popup == null)
            {
                StreamerHopeMenu.Init(__instance);
            }
            if (soundSettingsButton.IsDestroyedOrNull())
            {
                soundSettingsButton = Object.Instantiate(__instance.DisableMouseMovement, __instance.transform.FindChild("GeneralTab/MiscGroup"));
                soundSettingsButton.transform.localPosition = new(0, 1.27f, __instance.DisableMouseMovement.transform.localPosition.z);//左側:-1.3127f,1.5588f
                soundSettingsButton.transform.localScale = new(0.7f, 0.7f);
                soundSettingsButton.name = "SoundStgButton";
                soundSettingsButton.Text.text = Translator.GetString("SoundOptions");
                soundSettingsButton.Background.color = Palette.DisabledGrey;
                var soundSettingsPassiveButton = soundSettingsButton.GetComponent<PassiveButton>();
                soundSettingsPassiveButton.OnClick = new();
                soundSettingsPassiveButton.OnClick.AddListener((System.Action)(() =>
                {
                    SoundSettingsScreen.Show();
                }));
            }
            if (StreamHopeButton.IsNullOrDestroyed())
            {
                StreamHopeButton = Object.Instantiate(__instance.DisableMouseMovement, __instance.transform.FindChild("GeneralTab/MiscGroup"));
                StreamHopeButton.transform.localPosition = new(0f, 0.9264f, __instance.DisableMouseMovement.transform.localPosition.z);//左側:-1.3127f,1.5588f
                StreamHopeButton.transform.localScale = new(0.7f, 0.7f);
                StreamHopeButton.name = "StreamList";
                StreamHopeButton.Text.text = Translator.GetString("StreamList");
                StreamHopeButton.Background.color = Palette.ImpostorRed;
                StreamHopeButton.gameObject.SetActive(StreamerInfo.StreamURL is not "");
                var soundSettingsPassiveButton = StreamHopeButton.GetComponent<PassiveButton>();
                soundSettingsPassiveButton.OnClick = new();
                soundSettingsPassiveButton.OnClick.AddListener((System.Action)(() =>
                {
                    StreamerHopeMenu.Show(__instance);
                }));
            }

            if ((!AmongUsClient.Instance.AmHost || CustomSpawnEditor.ActiveEditMode) && ForceEnd != null)
                ForceEnd = null;

        }
        private static void ForceEndProcess()
        {
            //左シフトが押されているなら強制廃村
            if (Input.GetKey(KeyCode.LeftShift) || ((Main.ForcedGameEndColl != 0) && !GameStates.IsLobby))
            {
                GameManager.Instance.enabled = false;
                CustomWinnerHolder.WinnerTeam = CustomWinner.Draw;
                GameManager.Instance.RpcEndGame(GameOverReason.ImpostorDisconnect, false);
                return;
            }
            if (!GameStates.IsLobby) Main.ForcedGameEndColl++;
            Logger.Info($"廃村コール{Main.ForcedGameEndColl}回目", "fe");
            if (!GameStates.IsInGame) return;
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Draw);
            GameManager.Instance.LogicFlow.CheckEndCriteria();
        }
    }

    [HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Close))]
    public static class OptionsMenuBehaviourClosePatch
    {
        public static void Postfix()
        {
            if (ClientActionItem.CustomBackground != null)
            {
                ClientActionItem.CustomBackground.gameObject.SetActive(false);
            }
            ModUnloaderScreen.Hide();
            SoundSettingsScreen.Hide();
            StreamerHopeMenu.Hide();
        }
    }
    [HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Open))]
    public static class OptionsMenuBehaviourOpenPatch
    {
        public static void Prefix()
        {
            if (OptionsMenuBehaviourStartPatch.StreamHopeButton.IsNullOrDestroyed() is false)
            {
                OptionsMenuBehaviourStartPatch.StreamHopeButton.gameObject.SetActive(StreamerInfo.StreamURL is not "");
            }
        }
    }
}
