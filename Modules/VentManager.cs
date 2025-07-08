using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Hazel;

using TownOfHost.Roles.Core;
using TownOfHost.Patches.ISystemType;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Modules;

class VentManager
{
    public static Dictionary<byte, int> VentDuringDisabling = new();
    public static void UpdateDesyncVentCleaning(PlayerControl player, RoleBase roleclass)
    {
        if (Options.CurrentGameMode == CustomGameMode.Standard && GameStates.IsInTask && GameStates.introDestroyed && player.IsAlive() && !player.IsModClient())
        {
            Dictionary<int, float> Distance = new();
            Vector2 position = player.transform.position;
            foreach (var vent in ShipStatus.Instance.AllVents)
                Distance.Add(vent.Id, Vector2.Distance(position, vent.transform.position));
            var first = Distance.OrderBy(x => x.Value).First();

            if (VentDuringDisabling.TryGetValue(player.PlayerId, out var ventId) && (first.Key != ventId || first.Value > 2))
            {
                ushort num = (ushort)(VentilationSystemUpdateSystemPatch.last_opId + 1U);
                MessageWriter msgWriter = MessageWriter.Get(SendOption.None);
                msgWriter.Write(num);
                msgWriter.Write((byte)VentilationSystem.Operation.StopCleaning);
                msgWriter.Write((byte)ventId);
                player.RpcDesyncUpdateSystem(SystemTypes.Ventilation, msgWriter);
                msgWriter.Recycle();
                VentDuringDisabling.Remove(player.PlayerId);
                VentilationSystemUpdateSystemPatch.last_opId = num;
            }
            else if (first.Value <= 2 && !VentDuringDisabling.ContainsKey(player.PlayerId) && (((roleclass as IKiller)?.CanUseImpostorVentButton() is false) || (roleclass?.CanClickUseVentButton == false)))
            {
                ushort num = (ushort)(VentilationSystemUpdateSystemPatch.last_opId + 1U);
                MessageWriter msgWriter = MessageWriter.Get(SendOption.None);
                msgWriter.Write(num);
                msgWriter.Write((byte)VentilationSystem.Operation.StartCleaning);
                msgWriter.Write((byte)first.Key);
                player.RpcDesyncUpdateSystem(SystemTypes.Ventilation, msgWriter);
                msgWriter.Recycle();
                VentilationSystemUpdateSystemPatch.last_opId = num;
                VentDuringDisabling[player.PlayerId] = first.Key;
            }
        }
    }
    public static void CheckVentLimit(PlayerControl player)
    {
        if (Options.MaxInVentMode.GetBool())
        {
            List<byte> del = new();
            foreach (var ventpc in CoEnterVentPatch.VentPlayers)
            {
                var pc = PlayerCatch.GetPlayerById(ventpc.Key);
                if (pc == null) continue;

                if (ventpc.Value > Options.MaxInVentTime.GetFloat())
                {
                    if (!CoEnterVentPatch.VentPlayers.TryGetValue(ventpc.Key, out var a))
                    {
                        del.Add(ventpc.Key);
                        continue;
                    }
                    pc.MyPhysics.RpcBootFromVent(VentilationSystemUpdateSystemPatch.NowVentId.TryGetValue(ventpc.Key, out var r) ? r : 0);
                    del.Add(ventpc.Key);
                }
                CoEnterVentPatch.VentPlayers[ventpc.Key] += Time.fixedDeltaTime;
            }
            del.Do(id => CoEnterVentPatch.VentPlayers.Remove(id));
        }
    }
}