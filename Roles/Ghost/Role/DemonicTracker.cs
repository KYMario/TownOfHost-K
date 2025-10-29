using System.Linq;
using System.Collections.Generic;

using TownOfHost.Roles.Core;
using static TownOfHost.Options;
using Hazel;

namespace TownOfHost.Roles.Ghost
{
    public class DemonicTracker
    {
        private static readonly int Id = 21900;
        public static List<byte> playerIdList = new();
        public static OptionItem CoolDown;
        /// <summary>
        /// Key => 能力者
        /// Value => 対象者
        /// </summary>
        public static Dictionary<PlayerControl, byte> Mark;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.GhostRoles, CustomRoles.DemonicTracker);
            GhostRoleAssingData.Create(Id + 1, CustomRoles.DemonicTracker, CustomRoleTypes.Madmate);
            CoolDown = FloatOptionItem.Create(Id + 2, "Cooldown", new(0f, 180f, 0.5f), 25f, TabGroup.GhostRoles, false)
                .SetValueFormat(OptionFormat.Seconds).SetParent(CustomRoleSpawnChances[CustomRoles.DemonicTracker]).SetParentRole(CustomRoles.DemonicTracker);
        }
        public static void Init()
        {
            playerIdList = new();
            Mark = new Dictionary<PlayerControl, byte>();
            CustomRoleManager.MarkOthers.Add(ImpostorMark);
            SubRoleRPCSender.AddHandler(CustomRoles.DemonicTracker, ReceiveRPC);
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static void UseAbility(PlayerControl pc, PlayerControl target)
        {
            if (pc.Is(CustomRoles.DemonicTracker))
            {
                if (Mark.ContainsKey(pc))
                {
                    foreach (var imp in PlayerCatch.AllPlayerControls)
                    {
                        if (imp.GetCustomRole().IsImpostor())
                        {
                            TargetArrow.Remove(imp.PlayerId, Mark[pc]);
                        }
                    }
                }

                Mark[pc] = target.PlayerId;
                pc.RpcResetAbilityCooldown();

                foreach (var imp in PlayerCatch.AllPlayerControls)
                {
                    if (imp.GetCustomRole().IsImpostor())
                    {
                        TargetArrow.Add(imp.PlayerId, target.PlayerId);
                    }
                }

                RpcUseAbility(pc.PlayerId, target.PlayerId);
            }
        }
        public static string ImpostorMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
        {
            seen ??= seer;
            if (GameStates.CalledMeeting) return "";
            if (Mark.Values.ToArray() == null) return "";

            if (seer == seen)
                if (seer.GetCustomRole().IsImpostor()) return "<color=#824880>" + TargetArrow.GetArrows(seer, Mark.Values.ToArray()) + "</color>";

            return "";
        }

        public static void RpcUseAbility(byte playerId, byte targetId)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            using var sender = new SubRoleRPCSender(CustomRoles.DemonicTracker, playerId);
            sender.Writer.Write(targetId);
        }

        public static void ReceiveRPC(MessageReader reader, byte playerId)
        {
            var targetId = reader.ReadByte();
            UseAbility(PlayerCatch.GetPlayerById(playerId), PlayerCatch.GetPlayerById(targetId));
        }
    }
}
