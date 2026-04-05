using HarmonyLib;
using UnityEngine;

namespace TownOfHost.Patches
{
    [HarmonyPatch(typeof(LobbyBehaviour), nameof(LobbyBehaviour.Update))]
    public static class AutoStartPatch
    {
        private static float timer = 0f;

        public static void Postfix()
        {
            // ホスト以外は実行しない
            if (!AmongUsClient.Instance.AmHost) return;

            // GMがONのときだけ動作
            if (!Options.EnableGM.GetBool()) return;

            // ロビー以外では動作しない
            if (!GameStates.IsLobby)
            {
                timer = 0f;
                return;
            }

            // 現在の人数
            int playerCount = PlayerControl.AllPlayerControls.Count;

            // ★ 人数によって制限時間を変える
            float limit = (playerCount == 15) ? 180f : 420f;

            // タイマー進行
            timer += Time.deltaTime;

            // 規定時間経過で開始
            if (timer >= limit)
            {
                timer = 0f;

                // ★ TOH-P 正式のゲーム開始処理
                var gsm = DestroyableSingleton<GameStartManager>.Instance;
                if (gsm != null)
                {
                    gsm.countDownTimer = 0.1f; // 即開始
                    gsm.startState = GameStartManager.StartingStates.Countdown;
                }
            }
        }
    }
}