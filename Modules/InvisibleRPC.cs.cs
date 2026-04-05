using Hazel;
using InnerNet;

namespace TownOfHost.Modules
{
    public static class InvisibleRPC
    {
        public static void SendInvisible(byte playerId, bool invisible)
        {
            var writer = AmongUsClient.Instance.StartRpcImmediately(
                PlayerControl.LocalPlayer.NetId,
                (byte)CustomRPC.SetInvisible,
                SendOption.Reliable
            );

            writer.Write(playerId);
            writer.Write(invisible);

            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
    }
}