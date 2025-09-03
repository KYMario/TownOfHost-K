using HarmonyLib;

namespace TownOfHost
{
    [HarmonyPatch(typeof(LoadingBarManager))]
    class LoadingBarManagerPatch
    {
        [HarmonyPatch(nameof(LoadingBarManager.SetLoadingPercent)), HarmonyPostfix]
        private static void SetLoadingPercentPostfix(LoadingBarManager __instance, [HarmonyArgument(1)] StringNames loadText)
        {
            if (loadText != StringNames.LoadingBarGameStartWaitingPlayers) return;
            var allClients = AmongUsClient.Instance.allClients;
            var IsReadyCount = 0;
            for (var i = 0; i < allClients.Count; i++)
                if (allClients[i].IsReady) IsReadyCount++;

            __instance.loadingBar.loadingText.text += $"({IsReadyCount}/{allClients.Count - 1})";
        }
        [HarmonyPatch(nameof(LoadingBarManager.ToggleLoadingBar)), HarmonyPostfix]
        private static void ToggleLoadingBarPostfix(LoadingBarManager __instance)
        {
            __instance.loadingBar.loadingText.enableWordWrapping = false;
        }
    }
}
