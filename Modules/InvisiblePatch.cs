using HarmonyLib;
using Hazel;
using UnityEngine;

namespace TownOfHost.Modules
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
    public static class InvisiblePatch
    {
        public static void Postfix(PlayerControl __instance, byte callId, MessageReader reader)
        {
            if (callId == (byte)CustomRPC.SetInvisible)
            {
                byte targetId = reader.ReadByte();
                bool invisible = reader.ReadBoolean();

                var pc = PlayerCatch.GetPlayerById(targetId);
                if (pc == null) return;

                if (invisible)
                {
                    pc.cosmetics.currentBodySprite.BodySprite.enabled = false;
                    pc.cosmetics.gameObject.SetActive(false);
                    pc.cosmetics.ToggleNameVisible(false);
                }
                else
                {
                    pc.cosmetics.currentBodySprite.BodySprite.enabled = true;
                    pc.cosmetics.gameObject.SetActive(true);
                    pc.cosmetics.ToggleNameVisible(true);
                }
            }
        }
    }
}