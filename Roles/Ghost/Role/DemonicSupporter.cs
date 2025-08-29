using System.Collections.Generic;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.Ghost
{
    public class DemonicSupporter
    {
        private static readonly int Id = 23000;
        public static List<byte> playerIdList = new();
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.GhostRoles, CustomRoles.DemonicSupporter);
            GhostRoleAssingData.Create(Id + 1, CustomRoles.DemonicSupporter, CustomRoleTypes.Madmate);
        }
        public static void Init()
        {
            playerIdList = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);

            var pc = playerId.GetPlayerControl();
            pc.RpcSetRole(AmongUs.GameOptions.RoleTypes.ImpostorGhost);
        }
    }
}