using System.Collections.Generic;

using TownOfHost.Roles.Core;
using UnityEngine;
using static TownOfHost.Options;

namespace TownOfHost.Roles.Ghost
{
    public class GuardianAngel
    {
        private static readonly int Id = 60800;
        public static List<byte> playerIdList = new();
        public static OptionItem CoolDown;
        public static OptionItem GuardTime;
        public static bool MeetingNotify;
        public static Dictionary<byte, float> Guarng = new();
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.GhostRoles, CustomRoles.GuardianAngel, fromtext: UtilsOption.GetFrom(From.AmongUs));
            GhostRoleAssingData.Create(Id + 1, CustomRoles.GuardianAngel, CustomRoleTypes.Crewmate);
            CoolDown = FloatOptionItem.Create(Id + 2, "GhostButtonerCoolDown", new(0f, 180f, 0.5f), 27.5f, TabGroup.GhostRoles, false)
                .SetValueFormat(OptionFormat.Seconds).SetParent(CustomRoleSpawnChances[CustomRoles.GuardianAngel]);
            GuardTime = FloatOptionItem.Create(Id + 3, "GuardianAngelGuardTime", new(0.5f, 180, 0.5f), 5f, TabGroup.GhostRoles, false)
            .SetValueFormat(OptionFormat.Seconds).SetParent(CustomRoleSpawnChances[CustomRoles.GuardianAngel]);
        }

        public static void Init()
        {
            playerIdList = new();
            MeetingNotify = false;
            Guarng.Clear();
            CustomRoleManager.OnFixedUpdateOthers.Add(FixUpdata);
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static void FixUpdata(PlayerControl player)
        {
            if (player.PlayerId != 0) return;//ホストだけに処理させる
            if (Guarng.Count == 0) return;
            List<byte> dellist = new();
            foreach (var guardingpc in Guarng)
            {
                if (GuardTime.GetFloat() < guardingpc.Value)
                {
                    dellist.Add(guardingpc.Key);
                    continue;
                }
                Guarng[guardingpc.Key] += Time.fixedDeltaTime;
            }
            dellist.ForEach(task => Guarng.Remove(task));
        }
        public static void UseAbility(PlayerControl pc, PlayerControl target)
        {
            if (pc.Is(CustomRoles.GuardianAngel))
            {
                if (!target.IsAlive()) return;

                if (!Guarng.TryAdd(target.PlayerId, 0)) Guarng[target.PlayerId] = 0;
                pc.RpcResetAbilityCooldown();
            }
        }
    }
}