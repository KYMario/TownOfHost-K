using System.Collections.Generic;
using HarmonyLib;

namespace TownOfHost
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckUseZipline))]
    class PlayerControlRpcUseZiplinePatch
    {
        static List<byte> ZipdiePlayers = new();
        static List<byte> ZiplineCools = new();
        [Attributes.GameModuleInitializer]
        public static void reset()
        {
            ZiplineCools.Clear();
            ZipdiePlayers.Clear();
        }
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target, [HarmonyArgument(1)] ZiplineBehaviour ziplineBehaviour, [HarmonyArgument(2)] bool fromTop)
        {
            if (!fromTop && Options.CantUseZipLineTotop.GetBool()) return false;
            if (fromTop && Options.CantUseZipLineTodown.GetBool()) return false;

            if (ZiplineCools.Contains(__instance.PlayerId))
            {
                __instance.RpcUseZipline(target, ziplineBehaviour, fromTop);
                return false;
            }

            ZiplineCools.Add(__instance.PlayerId);
            _ = new LateTask(() =>
            {
                if (ZiplineCools.Contains(__instance.PlayerId))
                    ZiplineCools.Remove(__instance.PlayerId);
            }, 3f, "ZiplineCoolsremove", true);

            if (!GameStates.Meeting && !__instance.Data.IsDead && Options.LadderDeathZipline.GetBool())
            {
                int chance = IRandom.Instance.Next(1, 101);
                if (chance <= FallFromLadder.Chance)
                {
                    if (__instance.Data.IsDead) return false;
                    var speed = Main.AllPlayerSpeed[__instance.PlayerId];

                    _ = new LateTask(() =>
                    {
                        if (!GameStates.Meeting && !__instance.Data.IsDead)
                        {
                            Main.AllPlayerSpeed[__instance.PlayerId] = Main.MinSpeed;
                            __instance.SyncSettings();
                            ZipdiePlayers.Add(__instance.PlayerId);
                        }
                    }, 3f, "ZipLineDeath", true);
                    _ = new LateTask(() =>
                    {
                        Main.AllPlayerSpeed[__instance.PlayerId] = speed;
                        __instance.SyncSettings();
                        if (!GameStates.Meeting && !__instance.Data.IsDead)
                        {
                            __instance.RpcMurderPlayer(__instance);
                            var state = PlayerState.GetByPlayerId(__instance.PlayerId);
                            state.DeathReason = CustomDeathReason.Fall;
                            state.SetDead();
                        }
                    }, fromTop ? 5f : 8f, "ZipLineFall", null);
                }
            }

            __instance.RpcUseZipline(target, ziplineBehaviour, fromTop);
            return false;
        }
        public static void OnMeeting(PlayerControl reporter, NetworkedPlayerInfo oniku)
        {
            //速度は処理止まらんぜ
            foreach (var playerId in ZipdiePlayers)
            {
                var player = PlayerCatch.GetPlayerById(playerId);
                if (player == null) continue;
                if (player.Data.IsDead) continue;
                player.Data.IsDead = true;
                player.RpcMurderPlayer(player);
                var state = PlayerState.GetByPlayerId(player.PlayerId);
                state.DeathReason = CustomDeathReason.Fall;
                state.SetDead();
            }
            ZiplineCools.Clear();
            ZipdiePlayers.Clear();
        }
    }
}