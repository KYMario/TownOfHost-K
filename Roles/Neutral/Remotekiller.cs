using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static TownOfHost.Translator;
using static TownOfHost.Options;

namespace TownOfHost.Roles.Neutral
{
    public static class Remotekiller
    {
        private static readonly int Id = 50904;
        private static OptionItem KillCooldown;
        private static PlayerControl target;
        private static PlayerControl killer;
        public static void SetupCustomOption()
        {

            SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Remotekiller);
            KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(2.5f, 180f, 2.5f), 30f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Remotekiller])
                .SetValueFormat(OptionFormat.Seconds);

        }
        public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
        public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
        {
            Remotekiller.target = target;
            Remotekiller.killer = killer;
            return true;
        }
        public static bool Checktarget1()
        {
            if (target != null && killer != null)
            {
                return true;
            }
            else return false;
        }

        public static void RemoteKill()
        {
            if (target != null && killer != null)
            {
                killer.RpcMurderPlayer(target);
                RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
                RPC.PlaySoundRPC(killer.PlayerId, Sounds.TaskComplete);
                target = null;
                killer = null;

            }
        }
    }
}
