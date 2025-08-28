using HarmonyLib;
using Hazel;

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
        // カメラ無効時，バニラプレイヤーはカメラを開けるので点滅させない
        if (amount == SecurityCameraSystemType.IncrementOp)
        {
            var camerasDisabled = (MapNames)Main.NormalOptions.MapId switch
            {
                MapNames.Skeld => Options.DisableSkeldCamera.GetBool() || DisableDevice.LogAndCamUsecheck(player),
                MapNames.Polus => Options.DisablePolusCamera.GetBool() || DisableDevice.LogAndCamUsecheck(player),
                MapNames.Airship => Options.DisableAirshipCamera.GetBool() || DisableDevice.LogAndCamUsecheck(player),
                _ => false,
            };
            if (!camerasDisabled)
                DisableDevice.UseCount++;
            return !camerasDisabled;
        }
        if (amount == SecurityCameraSystemType.DecrementOp)
        {
            if (DisableDevice.UseCount <= 0) return true;

            var camerasDisabled = (MapNames)Main.NormalOptions.MapId switch
            {
                MapNames.Skeld => Options.DisableSkeldCamera.GetBool() || DisableDevice.LogAndCamUsecheck(player),
                MapNames.Polus => Options.DisablePolusCamera.GetBool() || DisableDevice.LogAndCamUsecheck(player),
                MapNames.Airship => Options.DisableAirshipCamera.GetBool() || DisableDevice.LogAndCamUsecheck(player),
                _ => false,
            };
            if (!camerasDisabled) DisableDevice.UseCount--;
        }
        return true;
    }
}
