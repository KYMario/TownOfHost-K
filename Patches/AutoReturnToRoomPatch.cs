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

            // 自動戻り設定がOFFなら終了
            if (!Options.OptionAutoReturnRoom.GetBool()) return;

            // 「GMの場合のみ」がONなら GM 以外は終了
            if (Options.OptionAutoReturnRoomGM.GetBool() && !Options.EnableGM.GetBool())
                return;

            // EndGameNavigation は ShowButtons のタイミングで必ず存在する
            var nav = DestroyableSingleton<EndGameNavigation>.Instance;
            if (nav != null)
            {
                nav.NextGame(); // ★ 自動で部屋に戻る
            }
        }
    }
}
