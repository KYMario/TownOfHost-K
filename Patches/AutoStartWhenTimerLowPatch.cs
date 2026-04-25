using HarmonyLib;
using UnityEngine;

namespace TownOfHost.Patches
{
    [HarmonyPatch(typeof(LobbyBehaviour), nameof(LobbyBehaviour.Update))]
    public static class AutoStartPatch
    {
        private static float timer = 0f;
        private static int lastPlayerCount = 0;
        private static float timeWhenFull = 0f;

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

            timer += Time.deltaTime;

            if (lastPlayerCount < 15 && playerCount == 15)
            {
                timeWhenFull = timer;
            }
            lastPlayerCount = playerCount;

            if (playerCount < 15) return;

            float limit = Mathf.Max(180f + 60f, timeWhenFull + 60f);

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