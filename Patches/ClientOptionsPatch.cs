using HarmonyLib;

using TownOfHost.Modules.ClientOptions;

namespace TownOfHost
{
    [HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Start))]
    public static class OptionsMenuBehaviourStartPatch
    {
        private static ClientActionItem ForceJapanese;
        private static ClientActionItem JapaneseRoleName;
        private static ClientActionItem UnloadMod;
        private static ClientActionItem DumpLog;
        private static ClientActionItem ChangeSomeLanguage;
        private static ClientActionItem ForceEnd;
        private static ClientActionItem WebHookD;
        private static ClientActionItem Yomiage;
        private static ClientActionItem UseZoom;
        private static ClientActionItem SyncYomiage;

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
            if (UnloadMod == null || UnloadMod.ToggleButton == null)
            {
                UnloadMod = ClientActionItem.Create("UnloadMod", ModUnloaderScreen.Show, __instance);
            }
            if (DumpLog == null || DumpLog.ToggleButton == null)
            {
                DumpLog = ClientActionItem.Create("DumpLog", Utils.DumpLog, __instance);
            }
            if (ChangeSomeLanguage == null || ChangeSomeLanguage.ToggleButton == null)
            {
                ChangeSomeLanguage = ClientOptionItem.Create("ChangeSomeLanguage", Main.ChangeSomeLanguage, __instance);
            }
            if (ForceEnd == null || ForceEnd.ToggleButton == null)
            {
                ForceEnd = ClientActionItem.Create("ForceEnd", ForceEndProcess, __instance);
            }
            if (WebHookD == null || WebHookD.ToggleButton == null)
            {
                WebHookD = ClientOptionItem.Create("UseWebHook", Main.UseWebHook, __instance);
            }
            if (Yomiage == null || Yomiage.ToggleButton == null)
            {
                Yomiage = ClientOptionItem.Create("UseYomiage", Main.UseYomiage, __instance);
            }
            if (UseZoom == null || UseZoom.ToggleButton == null)
            {
                UseZoom = ClientOptionItem.Create("UseZoom", Main.UseZoom, __instance);
            }
            if (SyncYomiage == null || SyncYomiage.ToggleButton == null)
            {
                SyncYomiage = ClientOptionItem.Create("SyncYomiage", Main.SyncYomiage, __instance);
            }
            if (ModUnloaderScreen.Popup == null)
            {
                ModUnloaderScreen.Init(__instance);
            }
        }
        private static void ForceEndProcess()
        {
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
        }
    }
}
