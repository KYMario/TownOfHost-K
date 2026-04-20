using HarmonyLib;
using UnityEngine;

namespace TownOfHost.Patches
{
    [HarmonyPatch(typeof(LobbyBehaviour), nameof(LobbyBehaviour.Update))]
    public static class AutoStartPatch
    {
        private static float timer = 0f;
        private static int lastPlayerCount = 0;

        public static void Postfix()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (!Options.EnableGM.GetBool()) return;

            if (!GameStates.IsLobby)
            {
                timer = 0f;
                lastPlayerCount = 0;
                return;
            }

            int playerCount = PlayerControl.AllPlayerControls.Count;

            // ★ 基本は7分（420秒）
            float limit = 420f;

            // ★ 途中参加で15人になった瞬間
            if (lastPlayerCount < 15 && playerCount == 15)
            {
                // カウント中なら残り1分に調整
                if (timer > 0f && timer < limit)
                {
                    timer = limit - 60f; // 420 - 60 = 360秒 → 残り1分
                }
            }

            lastPlayerCount = playerCount;

            timer += Time.deltaTime;

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
