using System;
using HarmonyLib;
using TMPro;
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
            renderer.material.color = Color.white;
            if (hostVersion == null) return;
            renderer.material.color = new Color(0.5f, 0.8f, 125f);
            textTMP.text = $"{hostVersion.forkId}v{hostVersion.version}";
        }
        [HarmonyPatch(nameof(GameContainer.OnClick)), HarmonyPrefix]
        public static bool OnClickPrefix(GameContainer __instance)
        {
            var version = EnterCodeManagerPatch.CheckHostVersion(__instance.gameListing);
            if (version == null || version.forkId != Main.ForkId)
            {
                __instance.ClickMore();
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(FindGameMoreInfoPopup))]
    class FindGameMoreInfoPopupPatch
    {
        private static GameObject TagUI = null;
        private static TextMeshPro VersionText = null;
        private static SpriteRenderer ModSprite = null;
        private static PassiveButton JoinGame = null;
        private static TextMeshPro RoomInfoText = null;
        private static string versionInfo = string.Empty;

        [HarmonyPatch(nameof(FindGameMoreInfoPopup.SetupInfo))]
        public static void Postfix(FindGameMoreInfoPopup __instance, InnerNet.GameListing gameL)
        {
            if (TagUI.IsDestroyedOrNull())
            {
                TagUI = new GameObject("TagUI");
                TagUI.transform.SetParent(__instance.mapBackground.transform, false);
                TagUI.transform.localPosition = Vector3.zero;
                TagUI.transform.localScale = new(1f, 1f, 1f);

                Transform[] obj = [
                    __instance.tagText.transform.parent,
                    __instance.languageText.transform.parent,
                    __instance.chatTypeText.transform.parent,
                    __instance.regionText.transform.parent
                ];

                obj.Do(x => x.SetParent(TagUI.transform, false));
                var baseText = __instance.regionText;
                var baseIcon = baseText.transform.parent.GetChild(1).GetComponent<SpriteRenderer>();

                VersionText = GameObject.Instantiate(baseText, __instance.mapBackground.transform, false);
                VersionText.transform.position = baseText.transform.position;
                VersionText.transform.SetLocalX(0);
                VersionText.transform.localScale = baseText.transform.localScale;

                ModSprite = GameObject.Instantiate(baseIcon, __instance.mapBackground.transform, false);
                ModSprite.transform.position = baseIcon.transform.position;
                ModSprite.transform.localScale = baseIcon.transform.localScale;
                ModSprite.sprite = UtilsSprite.LoadSprite("TownOfHost.Resources.TOHK.Tab.TabIcon_MainSettings.png", 200f);

                JoinGame = __instance.transform.FindChild("Join Game")?.GetComponent<PassiveButton>();

                RoomInfoText = GameObject.Instantiate(__instance.impostorsText, __instance.transform, false);
                RoomInfoText.transform.localPosition = JoinGame?.transform?.localPosition ?? new Vector3(0f, -2f, 0f);
                RoomInfoText.transform.localPosition += new Vector3(0f, 0.5f);
                RoomInfoText.transform.localScale = new(0.7f, 0.7f);
                RoomInfoText.enableAutoSizing = false;
                RoomInfoText.fontSize = 2;
                RoomInfoText.alignment = TextAlignmentOptions.Center;
                RoomInfoText.rectTransform.sizeDelta += new Vector2(5f, 0f);
            }

            var version = EnterCodeManagerPatch.CheckHostVersion(gameL);

            var isUnknow = version == null;
            var isMatchForkId = !isUnknow && version.forkId == Main.ForkId; //↓フォークIdとバージョンが一致するかどうか
            var isMatchVersion = !isUnknow && EnterCodeManagerPatch.MatchVersions(version);

            var yOffset = isUnknow ? 0f : 0.696f; //バージョンを識別可能な部屋の場合はバニラタグを少し上にずらす
            TagUI.transform.localPosition = new(0f, yOffset, 0f);

            VersionText.gameObject.SetActive(!isUnknow);
            ModSprite.gameObject.SetActive(!isUnknow);

            if (!isUnknow) //タグ
            {
                var color = isMatchVersion ? 1f : 0.5f;
                VersionText.text = $"{version.forkId} v{version.version}";
                VersionText.color = new(1, color, color);
                ModSprite.color = new(color, color, color);
            }

            //参加ボタンを押せるかを制御する
            JoinGame?.SetButtonEnableState(isMatchForkId); //バニラor別MODならMMからは入れさせない
            RoomInfoText.text = GetRoomInfo();

            string GetRoomInfo()
            {
                if (isUnknow) return Translator.GetString("MM_Unknow");
                if (!isMatchForkId) return Translator.GetString("MM_MismatchedForkId");
                if (!isMatchVersion) return string.Format(Translator.GetString("Warning.MismatchedHostVersion"), version.version);
                return string.Empty;
            }
        }
    }
}
