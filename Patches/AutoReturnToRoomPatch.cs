using HarmonyLib;

namespace TownOfHost.Patches
{
    [HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.ShowButtons))]
    public static class AutoReturnToRoomPatch
    {
        public static void Postfix(EndGameManager __instance)
        {
            // ホスト以外は実行しない
            if (!AmongUsClient.Instance.AmHost) return;

            // GM設定がOFFなら何もしない
            if (!Options.EnableGM.GetBool()) return;

            // EndGameNavigation は ShowButtons のタイミングで必ず存在する
            var nav = DestroyableSingleton<EndGameNavigation>.Instance;
            if (nav != null)
            {
                // ★ GMがONのときだけ即ルーム（部屋）へ戻る
                nav.NextGame();
            }
        }
    }
}