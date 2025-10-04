using HarmonyLib;
using UnityEngine;

namespace TownOfHost.Patches
{
    [HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetName))]
    class ChatBubbleSetNamePatch
    {
        public static void Postfix(ChatBubble __instance, ref Color color)
        {
            color = Palette.White;
            if (GameStates.IsInGame)
            {
                if (!__instance.playerInfo._object) return;
                if (__instance.TextArea.text != string.Empty) //投票通知ではないなら
                {
                    __instance.NameText.text = __instance.NameText.text.ApplyNameColorData(PlayerControl.LocalPlayer, __instance.playerInfo._object, true);
                    return;
                }
            }
            if (__instance.NameText.text.RemoveaAlign() != __instance.NameText.text)
            {
                __instance.SetLeft();
                __instance.SetCosmetics(__instance.playerInfo);
            }
        }
    }
}
