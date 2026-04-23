using System.Collections.Generic;

using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.Ghost
{
    public class GhostNoiseSender
    {
        private static readonly int Id = 16300;
        public static List<byte> playerIdList = new();
        public static OptionItem CoolDown;
        public static OptionItem Time;
        public static Dictionary<byte, byte> Nois;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.GhostRoles, CustomRoles.GhostNoiseSender, fromtext: UtilsOption.GetFrom(From.TownOfHost_K));
            GhostRoleAssingData.Create(Id + 1, CustomRoles.GhostNoiseSender, CustomRoleTypes.Crewmate);
            CoolDown = FloatOptionItem.Create(Id + 2, "Cooldown", new(0f, 180f, 0.5f), 25f, TabGroup.GhostRoles, false)
                .SetValueFormat(OptionFormat.Seconds).SetParent(CustomRoleSpawnChances[CustomRoles.GhostNoiseSender]).SetParentRole(CustomRoles.GhostNoiseSender);
            Time = FloatOptionItem.Create(Id + 3, "GhostNoiseSenderTime", new(1f, 180, 1), 7, TabGroup.GhostRoles, false)
                .SetValueFormat(OptionFormat.Seconds).SetParent(CustomRoleSpawnChances[CustomRoles.GhostNoiseSender]).SetParentRole(CustomRoles.GhostNoiseSender);
        }
        public static void Init()
        {
            playerIdList = new();
            Nois = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static void UseAbility(PlayerControl pc, PlayerControl target)
        {
            if (pc.Is(CustomRoles.GhostNoiseSender))
            {
                if (!Nois.ContainsKey(pc.PlayerId))
                {
                    Nois[pc.PlayerId] = target.PlayerId;
                    _ = new LateTask(() => Nois.Remove(pc.PlayerId), Time.GetFloat(), "GhostNoiseSender");
                    pc.RpcResetAbilityCooldown();
                }
            }
        }
        public static Dictionary<int, Achievement> achievements = new();
        [Attributes.PluginModuleInitializer]
        public static void Load()
        {
            var n1 = new Achievement(CustomRoles.GhostNoiseSender, Id + 0, 1, 0, 0);
            achievements.Add(0, n1);
        }
    }
}