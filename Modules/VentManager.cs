using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Hazel;

using TownOfHost.Roles.Core;
using TownOfHost.Patches.ISystemType;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Attributes;

namespace TownOfHost.Modules;

class VentManager
{
    public static Dictionary<byte, int> VentDuringDisabling = null;
    private static readonly List<byte> _delList = new();
    private const float VentSqrThreshold = 4; //2^2
    private static bool MaxInVentMode;
    private static float MaxInVentTime;

    [GameModuleInitializer]
    public static void Init()
    {
        VentDuringDisabling = new(GameData.Instance.PlayerCount);
        MaxInVentMode = Options.MaxInVentMode.GetBool();
        MaxInVentTime = Options.MaxInVentTime.GetFloat();
    }

    public static void UpdateDesyncVentCleaning(PlayerControl player, RoleBase roleclass)
    {
        //ゲーム中でなければ実行しない
        if (!GameStates.IsInGame) return;

        if (GameModeManager.IsStandardClass() && GameStates.IsInTask && GameStates.introDestroyed && player.IsAlive() && !player.IsModClient())
        {
            Vector2 position = player.transform.position;

            int minId = -1;
            float minDist = 99;
            var ventCount = ShipStatus.Instance.AllVents.Count;
            for (var i = 0; i < ventCount; i++)
            {
                var vent = ShipStatus.Instance.AllVents[i];
                var dist = Vector2.SqrMagnitude(position - (Vector2)vent.transform.position);
                if (minDist < dist) continue;
                minId = vent.Id;
                minDist = dist;
            }

            if (minId == -1) return;

            if (VentDuringDisabling.TryGetValue(player.PlayerId, out var ventId))
            {
                if (minId != ventId || minDist > VentSqrThreshold)
                {
                    RpcDesyncUpdateVent(player, ventId, VentilationSystem.Operation.StopCleaning);
                    VentDuringDisabling.Remove(player.PlayerId);
                }
            }
            else if (minDist <= VentSqrThreshold && (((roleclass as IKiller)?.CanUseImpostorVentButton() is false) || (roleclass?.CanClickUseVentButton == false)))
            {
                RpcDesyncUpdateVent(player, minId, VentilationSystem.Operation.StartCleaning);
                VentDuringDisabling[player.PlayerId] = minId;
            }
        }
    }

    private static void RpcDesyncUpdateVent(PlayerControl player, int ventId, VentilationSystem.Operation operation)
    {
        ushort num = (ushort)(VentilationSystemUpdateSystemPatch.last_opId + 1U);
        MessageWriter msgWriter = MessageWriter.Get(SendOption.None);
        msgWriter.Write(num);
        msgWriter.Write((byte)operation);
        msgWriter.Write((byte)ventId);
        player.RpcDesyncUpdateSystem(SystemTypes.Ventilation, msgWriter);
        msgWriter.Recycle();
        VentilationSystemUpdateSystemPatch.last_opId = num;
    }

    public static void CheckVentLimit()
    {
        if (!MaxInVentMode) return;
        if (AmongUsClient.Instance.AmHost is false) return;

        _delList.Clear();

        foreach (var ventpc in CoEnterVentPatch.VentPlayers)
        {
            var pc = PlayerCatch.GetPlayerById(ventpc.Key);
            if (pc == null) continue;

            if (ventpc.Value > MaxInVentTime)
            {
                /*if (!CoEnterVentPatch.VentPlayers.ContainsKey(ventpc.Key))
                {
                    del.Add(ventpc.Key);
                    continue;
                }*/
                pc.MyPhysics.RpcBootFromVent(VentilationSystemUpdateSystemPatch.NowVentId.TryGetValue(ventpc.Key, out var ventid) ? ventid : 0);
                _delList.Add(ventpc.Key);
            }
            CoEnterVentPatch.VentPlayers[ventpc.Key] += Time.fixedDeltaTime;
        }
        _delList.Do(id => CoEnterVentPatch.VentPlayers.Remove(id));
    }
}