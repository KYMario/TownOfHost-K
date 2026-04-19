using System.Collections.Generic;
using UnityEngine;

using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.Ghost
{
    public class GuardianAngel
    {
        static GhostRoleAssingData Data;
        private static readonly int Id = 21500;
        public static List<byte> playerIdList = new();
        public static OptionItem CoolDown;
        public static OptionItem GuardTime;
        public static bool MeetingNotify;
        public static Dictionary<byte, (float timer, byte owner)> GuardianAngelGuarding = new();
        static OptionItem AssingMadmate;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.GhostRoles, CustomRoles.GuardianAngel, fromtext: UtilsOption.GetFrom(From.AmongUs));
            Data = GhostRoleAssingData.Create(Id + 1, CustomRoles.GuardianAngel, CustomRoleTypes.Crewmate);
            CoolDown = FloatOptionItem.Create(Id + 2, "Cooldown", new(0f, 180f, 0.5f), 27.5f, TabGroup.GhostRoles, false)
                .SetValueFormat(OptionFormat.Seconds).SetParent(CustomRoleSpawnChances[CustomRoles.GuardianAngel]).SetParentRole(CustomRoles.GuardianAngel);
            GuardTime = FloatOptionItem.Create(Id + 3, "GuardianAngelGuardTime", new(0.5f, 180, 0.5f), 5f, TabGroup.GhostRoles, false)
            .SetValueFormat(OptionFormat.Seconds).SetParent(CustomRoleSpawnChances[CustomRoles.GuardianAngel]).SetParentRole(CustomRoles.GuardianAngel);
            AssingMadmate = BooleanOptionItem.Create(Id + 4, "AssgingMadmate", false, TabGroup.GhostRoles, false)
                                .SetParent(CustomRoleSpawnChances[CustomRoles.GuardianAngel]).SetParentRole(CustomRoles.GuardianAngel);
        }

        public static void Init()
        {
            playerIdList = new();
            MeetingNotify = false;
            GuardianAngelGuarding.Clear();
            CustomRoleManager.OnFixedUpdateOthers.Add(FixUpdata);
            Data.SubRoleType = AssingMadmate.GetBool() ? CustomRoleTypes.Madmate : CustomRoleTypes.Crewmate;
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static void FixUpdata(PlayerControl player)
        {
            if (player.PlayerId != 0) return;//ホストだけに処理させる
            if (GuardianAngelGuarding.Count == 0) return;
            List<byte> dellist = new();
            foreach (var guardingdata in GuardianAngelGuarding)
            {
                if (GuardTime.GetFloat() < guardingdata.Value.timer)
                {
                    Logger.Info($"{guardingdata.Key}ガードの削除", "GuardianAngel");
                    dellist.Add(guardingdata.Key);
                    continue;
                }
                var timer = guardingdata.Value.timer;
                timer += Time.fixedDeltaTime;
                GuardianAngelGuarding[guardingdata.Key] = (timer, guardingdata.Value.owner);
            }
            dellist.ForEach(task => GuardianAngelGuarding.Remove(task));
        }
        public static void UseAbility(PlayerControl pc, PlayerControl target)
        {
            if (pc.Is(CustomRoles.GuardianAngel))
            {
                if (!target.IsAlive()) return;

                if (!GuardianAngelGuarding.TryAdd(target.PlayerId, (0, pc.PlayerId))) GuardianAngelGuarding[target.PlayerId] = (0, pc.PlayerId);
                pc.RpcResetAbilityCooldown();
            }
        }
    }
}