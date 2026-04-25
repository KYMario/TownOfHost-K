using HarmonyLib;
using UnityEngine;

namespace TownOfHost.Patches
{
    [HarmonyPatch(typeof(LobbyBehaviour), nameof(LobbyBehaviour.Update))]
    public static class AutoStartPatch
    {
        private static float timer = 0f;
        private static int lastPlayerCount = 0;
        private static float timeWhenFull = 0f; // 15人揃った瞬間の時間を記録

        public static void Postfix()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (!Options.EnableGM.GetBool()) return;

            if (!GameStates.IsLobby)
            {
                timer = 0f;
                lastPlayerCount = 0;
                timeWhenFull = 0f;
                return;
            }

            int playerCount = PlayerControl.AllPlayerControls.Count;

            // ★ 基本のスタート時間は3分（180秒）
            float limit = 180f;

            timer += Time.deltaTime;

            // ★ 途中参加で15人になった瞬間、その時点の時間を記録する
            if (lastPlayerCount < 15 && playerCount == 15)
            {
                timeWhenFull = timer;
            }
            lastPlayerCount = playerCount;

            // ★ 15人揃っている場合の特別ルール
            if (playerCount == 15)
            {
                // 「基本の3分＋60秒(240秒)」 か 「揃った時間＋60秒」 の、どちらか遅い方をリミットにする
                limit = Mathf.Max(180f + 60f, timeWhenFull + 60f);
            }

            // タイマーがリミットに到達したらスタート
            if (timer >= limit)
            {
                timer = 0f;

                var gsm = DestroyableSingleton<GameStartManager>.Instance;
                if (gsm != null)
                {
                    gsm.countDownTimer = 0.1f;
                    gsm.startState = GameStartManager.StartingStates.Countdown;
                }
            }
        }
    }
}