using HarmonyLib;

namespace TownOfHost.Patches;

[HarmonyPatch(typeof(NetworkedPlayerInfo), nameof(NetworkedPlayerInfo.Serialize))]
class GameDataSerializePatch
{
    public static int SerializeMessageCount;
    public static bool DontTouch;

    public static bool Prefix(NetworkedPlayerInfo __instance, ref bool __result)
    {
        if (AmongUsClient.Instance == null || !GameStates.InGame)
        {
            __result = true;
            return true;
        }
        if (DontTouch)
        {
            __instance.ClearDirtyBits();
            __result = false;
            return false;
        }
        if (SerializeMessageCount > 0)
        {
            __result = true;
            return true;
        }
        if (Options.CurrentGameMode != CustomGameMode.Standard || !GameStates.IsMeeting || GameStates.ExiledAnimate || AntiBlackout.IsCached)
        {
            return true;
        }

        __instance.ClearDirtyBits();
        __result = false;
        return false;
    }
}