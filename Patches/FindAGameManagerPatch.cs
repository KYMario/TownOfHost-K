using HarmonyLib;
using UnityEngine;

namespace TownOfHost
{
    [HarmonyPatch(typeof(FindAGameManager))]
    class FindAGameManagerPatch
    {
        [HarmonyPatch(nameof(FindAGameManager.CoShow)), HarmonyPostfix]
        public static void CoShowPostfix(FindAGameManager __instance)
        {
            var text = CredentialsPatch.CreateText();
            if (__instance == null || text == null) return;
            text.transform.position += __instance.container.position;
            text.transform.parent = __instance.container;
        }
    }

    [HarmonyPatch(typeof(GameContainer))]
    class GameListingPatch
    {
        [HarmonyPatch(nameof(GameContainer.SetupGameInfo))]
        public static void Postfix(GameContainer __instance)
        {
            var hostVersion = EnterCodeManagerPatch.CheckHostVersion(__instance.gameListing);
            var textTMP = __instance.tag2;
            var renderer = textTMP.transform.parent.gameObject.GetComponent<SpriteRenderer>();
            renderer.color = Color.white;
            if (hostVersion == null) return;
            renderer.material.color = new Color(0.5f, 0.8f, 125f);
            textTMP.text = $"{hostVersion.forkId}v{hostVersion.version}";
        }
    }
}
