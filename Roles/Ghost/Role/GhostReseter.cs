using System.Collections.Generic;
using Hazel;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Impostor;
using static TownOfHost.Options;

namespace TownOfHost.Roles.Ghost
{
    public class GhostReseter
    {
        static GhostRoleAssingData Data;
        private static readonly int Id = 16400;
        public static List<byte> playerIdList = new();
        public static OptionItem CoolDown;
        public static OptionItem ResetAbilityCool;
        public static OptionItem Count;
        public static Dictionary<byte, int> Counts = new();
        static OptionItem AssingMadmate;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.GhostRoles, CustomRoles.GhostReseter);
            Data = GhostRoleAssingData.Create(Id + 1, CustomRoles.GhostReseter, CustomRoleTypes.Crewmate);
            CoolDown = FloatOptionItem.Create(Id + 2, "Cooldown", new(0f, 180f, 0.5f), 25f, TabGroup.GhostRoles, false)
                .SetValueFormat(OptionFormat.Seconds).SetParent(CustomRoleSpawnChances[CustomRoles.GhostReseter]).SetParentRole(CustomRoles.GhostReseter);
            ResetAbilityCool = BooleanOptionItem.Create(Id + 3, "GhostReseterResetAbilityCool", true, TabGroup.GhostRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.GhostReseter]).SetParentRole(CustomRoles.GhostReseter);
            Count = IntegerOptionItem.Create(Id + 4, "GhostReseterCount", new(1, 99, 1), 2, TabGroup.GhostRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.GhostReseter]).SetParentRole(CustomRoles.GhostReseter);
            AssingMadmate = BooleanOptionItem.Create(Id + 5, "AssgingMadmate", false, TabGroup.GhostRoles, false).SetParentRole(CustomRoles.GhostReseter)
                        .SetParent(CustomRoleSpawnChances[CustomRoles.GhostReseter]);
        }

        public static void Init()
        {
            playerIdList = new();
            Counts.Clear();

            CustomRoleManager.MarkOthers.Add(OtherMark);
            SubRoleRPCSender.AddHandler(CustomRoles.GhostReseter, ReceiveRPC);
            Data.SubRoleType = AssingMadmate.GetBool() ? CustomRoleTypes.Madmate : CustomRoleTypes.Crewmate;
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static void UseAbility(PlayerControl pc, PlayerControl target)
        {
            if (pc.Is(CustomRoles.GhostReseter))
            {
                if (!Counts.ContainsKey(pc.PlayerId))//登録前なら登録する
                    Counts.Add(pc.PlayerId, Count.GetInt());

                if (Counts[pc.PlayerId] <= 0) return;

                Counts[pc.PlayerId]--;
                SendRPC(pc.PlayerId);

                target.SetKillCooldown(force: true);
                if (ResetAbilityCool.GetBool())
                {
                    target.RpcResetAbilityCooldown(Sync: true);
                    var roleclass = target.GetRoleClass();
                    if (roleclass is SerialKiller serialKiller && serialKiller?.SuicideTimer is not null) serialKiller.SuicideTimer = SerialKiller.TimeLimit;
                    if (roleclass is BountyHunter bountyHunter && bountyHunter?.ChangeTimer is not null) bountyHunter.ChangeTimer = BountyHunter.TargetChangeTime;
                }

                UtilsNotifyRoles.NotifyRoles(SpecifySeer: pc);
                pc.RpcResetAbilityCooldown();
            }
        }
        public static string OtherMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
        {
            seen ??= seer;

            if (seer == seen && seer.Is(CustomRoles.GhostReseter))
            {
                var count = 0;
                if (Counts.ContainsKey(seer.PlayerId)) count = Counts[seer.PlayerId];
                return Utils.ColorString(UtilsRoleText.GetRoleColor(CustomRoles.GhostReseter).ShadeColor(-0.25f), $" ({count}/{Count.GetInt()})");
            }
            return "";
        }

        public static void SendRPC(byte playerId)
        {
            using var sender = new SubRoleRPCSender(CustomRoles.GhostReseter, playerId);
            sender.Writer.Write(Counts[playerId]);
        }

        public static void ReceiveRPC(MessageReader reader, byte playerId)
        {
            Counts[playerId] = reader.ReadInt32();
        }
    }
}