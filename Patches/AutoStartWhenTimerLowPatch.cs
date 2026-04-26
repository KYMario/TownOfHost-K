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
            // ホスト以外は動作しない
            if (!AmongUsClient.Instance.AmHost) return;

            // 自動スタート設定がOFFなら動作しない
            if (!Options.OptionAutoStartSetting.GetBool()) return;

            // GMのみ有効 → GMじゃないなら動作しない
            if (Options.OptionAutoStartGM.GetBool() && !Options.EnableGM.GetBool()) return;

            // ロビー以外ではリセット
            if (!GameStates.IsLobby)
            {
                timer = 0f;
                return;
            }

            int playerCount = PlayerControl.AllPlayerControls.Count;

            // タイマー進行
            timer += Time.deltaTime;

            // ★ 15人時の別設定がONならそちらを優先
            float limit;

            if (Options.OptionAutoStartLimitAnotherSetting.GetBool() && playerCount == 15)
            {
                limit = Options.OptionAutoStartLimitAnother.GetFloat();
            }
            else
            {
                limit = Options.OptionAutoStartLimit.GetFloat();
            }

            // タイマーが規定値を超えたら開始
            if (timer >= limit)
            {
                timer = 0f;

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
