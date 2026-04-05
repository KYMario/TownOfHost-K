using HarmonyLib;
using TownOfHost.Roles.Core;

namespace TownOfHost.Patches
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckUseZipline))]
    class 
        ZiplinePatch
    {
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target, [HarmonyArgument(1)] ZiplineBehaviour ziplineBehaviour, [HarmonyArgument(2)] bool fromTop)
        {
            // ★ ホストのみ処理
            if (AmongUsClient.Instance.AmHost is false) return true;

            // ★ 波動砲がチャージ中またはビーム中ならジップライン使用禁止
            if (__instance.GetRoleClass() is Roles.Impostor.HadouHo hadouHo)
            {
                if (hadouHo.IsCharging || hadouHo.ShowBeamMark)
                {
                    return false;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(MovingPlatformBehaviour))]
    class HadouHoMovingPlatformPatch
    {
        [HarmonyPatch(nameof(MovingPlatformBehaviour.Use), typeof(PlayerControl)), HarmonyPrefix]
        public static bool UsePrefix(PlayerControl player)
        {
            // ★ ホストのみ処理
            if (AmongUsClient.Instance.AmHost is false) return true;

            // ★ 波動砲がチャージ中またはビーム中なら梯子使用禁止
            if (player.GetRoleClass() is Roles.Impostor.HadouHo hadouHo)
            {
                if (hadouHo.IsCharging || hadouHo.ShowBeamMark)
                {
                    return false;
                }
            }
            return true;
        }
    }
}