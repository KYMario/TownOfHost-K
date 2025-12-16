using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class Stack
    {
        private static readonly int Id = 23700;
        private static List<byte> playerIdList = new();
        static AssignOptionItem AssignAddon;

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Stack);
            AddOnsAssignData.Create(Id + 10, CustomRoles.Stack, true, true, true, true);
            AssignAddon = (AssignOptionItem)AssignOptionItem.Create(Id + 20, "AssignAddon", 0, TabGroup.Addons, false, addon: true,
            notassing: [CustomRoles.LastImpostor, CustomRoles.LastNeutral, CustomRoles.Workhorse, CustomRoles.Amanojaku])
            .SetParent(CustomRoleSpawnChances[CustomRoles.Stack]).SetParentRole(CustomRoles.Stack); ;
        }
        public static void Init()
        {
            playerIdList = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);

            if (AmongUsClient.Instance.AmHost)
            {
                var pc = PlayerCatch.GetPlayerById(playerId);
                foreach (var addon in AssignAddon.GetNowRoleValue())
                {
                    if (pc.Is(addon) is false)
                    {
                        PlayerState.GetByPlayerId(playerId).SetSubRole(addon);
                    }
                }
            }
        }
    }
}