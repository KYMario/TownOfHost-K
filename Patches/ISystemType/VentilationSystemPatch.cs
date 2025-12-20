using System.Collections.Generic;
using HarmonyLib;
using Hazel;

using TownOfHost.Roles.Core;

namespace TownOfHost.Patches.ISystemType;

[HarmonyPatch(typeof(VentilationSystem), nameof(VentilationSystem.UpdateSystem))]
class VentilationSystemUpdateSystemPatch
{
    public static ushort last_opId = 0;
    public static Dictionary<byte, byte> NowVentId = new();
    public static bool Prefix([HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] MessageReader msgReader)
    {
        ushort opId;
        VentilationSystem.Operation op;
        byte ventId;
        {
            var newReader = MessageReader.Get(msgReader);
            opId = newReader.ReadUInt16();
            op = (VentilationSystem.Operation)newReader.ReadByte();
            ventId = newReader.ReadByte();
            newReader.Recycle();
        }
        last_opId = opId;

        if (!NowVentId.TryAdd(player.PlayerId, ventId))
        {
            NowVentId[player.PlayerId] = ventId;
        }

        foreach (var roleClass in CustomRoleManager.AllActiveRoles.Values)
        {
            roleClass.OnVentilationSystemUpdate(player, op, ventId);
        }

        if (Options.CurrentGameMode == CustomGameMode.TaskBattle)
            return false; //タスバトだとベント掃除で追い出されないように～

        //タスクを持っていないならベント掃除をなかったことにする
        if (GameModeManager.IsStandardClass())
            return UtilsTask.HasTasks(player.Data);

        return true;
    }
}
