/*using HarmonyLib;

using TownOfHost.Roles.Core;

namespace TownOfHost
{
    //参考元:https://github.com/yukieiji/ExtremeRoles/blob/master/ExtremeRoles/Patches/AirShipStatusPatch.cs
    [HarmonyPatch(typeof(AirshipStatus), nameof(AirshipStatus.PrespawnStep))]
    public static class AirshipStatusPrespawnStepPatch
    {
        public static bool Prefix()
        {
            多分GMが暗転しやすい原因ここなので一回スルー。
            if (PlayerControl.LocalPlayer.Is(CustomRoles.GM))
            {
                RandomSpawn.AirshipSpawn(PlayerControl.LocalPlayer);
                // GMは湧き画面をスキップ
                return true;
            }
            return true;
        }
    }
}*/