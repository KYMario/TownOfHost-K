using HarmonyLib;
using Hazel;
//using TownOfHost.Roles.Ghost;

namespace TownOfHost.Patches.ISystemType;

[HarmonyPatch(typeof(SecurityCameraSystemType), nameof(SecurityCameraSystemType.UpdateSystem))]
public static class SecurityCameraSystemTypeUpdateSystemPatch
{
    public static bool Prefix(PlayerControl player, [HarmonyArgument(1)] MessageReader msgReader)
    {
        byte amount;
        {
            var newReader = MessageReader.Get(msgReader);
            amount = newReader.ReadByte();
            newReader.Recycle();
        }
#if DEBUG
        var state = PlayerState.GetByPlayerId(player.PlayerId);

        if (!ExileControllerWrapUpPatch.AllSpawned && ExileControllerWrapUpPatch.SpawnTimer <= 0
            && !state.TeleportedWithAntiBlackout && amount == SecurityCameraSystemType.DecrementOp)
        {
            state.TeleportedWithAntiBlackout = true;
        }
#endif
        // カメラ無効時，バニラプレイヤーはカメラを開けるので点滅させない
        if (amount == SecurityCameraSystemType.IncrementOp)
        {
            var camerasDisabled = (MapNames)Main.NormalOptions.MapId switch
            {
                MapNames.Skeld => Options.DisableSkeldCamera.GetBool(),
                MapNames.Polus => Options.DisablePolusCamera.GetBool(),
                MapNames.Airship => Options.DisableAirshipCamera.GetBool(),
                _ => false,
            };
            return !camerasDisabled;
        }
        return true;
    }
}
