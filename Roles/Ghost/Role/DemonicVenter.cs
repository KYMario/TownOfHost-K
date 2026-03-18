using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.Ghost
{
    public class DemonicVenter
    {
        private static readonly int Id = 21600;
        public static List<byte> playerIdList = new();
        public static OptionItem CoolDown;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.GhostRoles, CustomRoles.DemonicVenter);
            GhostRoleAssingData.Create(Id + 1, CustomRoles.DemonicVenter, CustomRoleTypes.Madmate);
            CoolDown = FloatOptionItem.Create(Id + 2, "Cooldown", new(0f, 180f, 0.5f), 25f, TabGroup.GhostRoles, false)
                .SetValueFormat(OptionFormat.Seconds).SetParent(CustomRoleSpawnChances[CustomRoles.DemonicVenter]).SetParentRole(CustomRoles.DemonicVenter);
        }
        public static void Init()
        {
            playerIdList = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static void UseAbility(PlayerControl pc, PlayerControl target)
        {
            if (pc.Is(CustomRoles.DemonicVenter))
            {
                pc.RpcResetAbilityCooldown();

                Dictionary<Vent, float> Distance = new();
                Vector2 position = target.transform.position;
                var pos = pc.transform.position;
                //一番近いベントを調べる
                foreach (var vent in ShipStatus.Instance.AllVents)
                    Distance.Add(vent, Vector2.Distance(position, vent.transform.position));
                var ve = Distance.OrderByDescending(x => x.Value).Last().Key;
                foreach (var pl in PlayerCatch.AllPlayerControls)
                {
                    pc.RpcSnapToForced(ve.transform.position);
                    _ = new LateTask(() => pc.MyPhysics.RpcExitVent(ve.Id), 0.2f, "DemonicVenter3");
                    _ = new LateTask(() => pc.RpcSnapToForced(pos), 0.6f, "DemonicVenter3");
                }
                Achievements.RpcCompleteAchievement(pc.PlayerId, 1, achievements[0]);
                Achievements.RpcCompleteAchievement(pc.PlayerId, 1, achievements[1]);
            }
        }
        public static Dictionary<int, Achievement> achievements = new();
        [Attributes.PluginModuleInitializer]
        public static void Load()
        {
            var n1 = new Achievement(CustomRoles.DemonicVenter, Id + 0, 5, 0, 0);
            var l1 = new Achievement(CustomRoles.DemonicVenter, Id + 1, 50, 0, 1);
            achievements.Add(0, n1);
            achievements.Add(1, l1);
        }
    }
}
