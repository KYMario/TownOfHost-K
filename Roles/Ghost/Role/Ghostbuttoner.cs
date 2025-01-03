using System.Collections.Generic;

using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.Ghost
{
    public static class Ghostbuttoner
    {
        private static readonly int Id = 60000;
        public static List<byte> playerIdList = new();
        public static OptionItem CoolDown;
        public static OptionItem Count;
        public static Dictionary<byte, int> count;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.GhostRoles, CustomRoles.Ghostbuttoner);
            GhostRoleAssingData.Create(Id + 1, CustomRoles.Ghostbuttoner, CustomRoleTypes.Crewmate);
            CoolDown = FloatOptionItem.Create(Id + 2, "GhostButtonerCoolDown", new(0f, 180f, 0.5f), 25f, TabGroup.GhostRoles, false)
                .SetValueFormat(OptionFormat.Seconds).SetParent(CustomRoleSpawnChances[CustomRoles.Ghostbuttoner]);
            Count = IntegerOptionItem.Create(Id + 3, "GhostButtonerCount", new(1, 9, 1), 1, TabGroup.GhostRoles, false)
                .SetValueFormat(OptionFormat.Times).SetParent(CustomRoleSpawnChances[CustomRoles.Ghostbuttoner]);
        }
        public static void Init()
        {
            playerIdList = new();
            count = new Dictionary<byte, int>();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static void UseAbility(PlayerControl pc)
        {
            if (pc.Is(CustomRoles.Ghostbuttoner))
            {
                if (Utils.IsActive(SystemTypes.Reactor)
                || Utils.IsActive(SystemTypes.Electrical)
                || Utils.IsActive(SystemTypes.Laboratory)
                || Utils.IsActive(SystemTypes.Comms)
                || Utils.IsActive(SystemTypes.LifeSupp)
                || Utils.IsActive(SystemTypes.HeliSabotage)) return;

                if (!count.ContainsKey(pc.PlayerId))
                {
                    count[pc.PlayerId] = Count.GetInt();
                }
                if (count.ContainsKey(pc.PlayerId))
                    if (count[pc.PlayerId] > 0)
                    {
                        count[pc.PlayerId]--;
                        ReportDeadBodyPatch.DieCheckReport(pc, null, false);
                    }
            }
        }
    }
}