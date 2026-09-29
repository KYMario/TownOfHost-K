using System.Collections.Generic;

using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.Ghost
{
    public class SpiritGuide
    {
        static GhostRoleAssingData Data;
        private static readonly int Id = 25200;
        public static List<byte> playerIdList = new();
        public static OptionItem CoolDown;
        static OptionItem AssingMadmate;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.GhostRoles, CustomRoles.SpiritGuide, fromtext: UtilsOption.GetFrom(From.AmongUs));
            Data = GhostRoleAssingData.Create(Id + 1, CustomRoles.SpiritGuide, CustomRoleTypes.Crewmate, CustomRoleTypes.Madmate);
            CoolDown = FloatOptionItem.Create(Id + 2, "Cooldown", new(0f, 180f, 0.5f), 27.5f, TabGroup.GhostRoles, false)
                .SetValueFormat(OptionFormat.Seconds).SetParent(CustomRoleSpawnChances[CustomRoles.SpiritGuide]).SetParentRole(CustomRoles.SpiritGuide);
            AssingMadmate = BooleanOptionItem.Create(Id + 4, "AssgingMadmate", false, TabGroup.GhostRoles, false)
                                .SetParent(CustomRoleSpawnChances[CustomRoles.SpiritGuide]).SetParentRole(CustomRoles.SpiritGuide);
        }

        public static void Init()
        {
            playerIdList = new();
            Data.SubRoleType = AssingMadmate.GetBool() ? CustomRoleTypes.Madmate : CustomRoleTypes.Crewmate;
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
    }
}