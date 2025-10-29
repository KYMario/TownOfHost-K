using System.Collections.Generic;
using Hazel;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.Ghost
{
    public class Ghostbuttoner
    {
        static GhostRoleAssingData Data;
        private static readonly int Id = 16200;
        public static List<byte> playerIdList = new();
        public static OptionItem CoolDown;
        public static OptionItem Count;
        static OptionItem AssingMadmate;
        public static Dictionary<byte, int> count;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.GhostRoles, CustomRoles.Ghostbuttoner);
            Data = GhostRoleAssingData.Create(Id + 1, CustomRoles.Ghostbuttoner, CustomRoleTypes.Crewmate);
            CoolDown = FloatOptionItem.Create(Id + 2, "Cooldown", new(0f, 180f, 0.5f), 25f, TabGroup.GhostRoles, false)
                .SetValueFormat(OptionFormat.Seconds).SetParent(CustomRoleSpawnChances[CustomRoles.Ghostbuttoner]).SetParentRole(CustomRoles.Ghostbuttoner);
            Count = IntegerOptionItem.Create(Id + 3, "GhostButtonerCount", new(1, 9, 1), 1, TabGroup.GhostRoles, false)
                .SetValueFormat(OptionFormat.Times).SetParent(CustomRoleSpawnChances[CustomRoles.Ghostbuttoner]).SetParentRole(CustomRoles.Ghostbuttoner);
            AssingMadmate = BooleanOptionItem.Create(Id + 4, "AssgingMadmate", false, TabGroup.GhostRoles, false)
                                .SetParent(CustomRoleSpawnChances[CustomRoles.Ghostbuttoner]).SetParentRole(CustomRoles.Ghostbuttoner);
        }
        public static void Init()
        {
            playerIdList = new();
            count = new Dictionary<byte, int>();
            CustomRoleManager.MarkOthers.Add(OtherMark);
            SubRoleRPCSender.AddHandler(CustomRoles.Ghostbuttoner, ReceiveRPC);
            Data.SubRoleType = AssingMadmate.GetBool() ? CustomRoleTypes.Madmate : CustomRoleTypes.Crewmate;
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
                || Utils.IsActive(SystemTypes.HeliSabotage))
                {
                    Logger.Info("サボ中なう", "Ghostbuttoner");
                    return;
                }
                if (!count.TryGetValue(pc.PlayerId, out var nowcount))
                {
                    count[pc.PlayerId] = Count.GetInt();
                    nowcount = Count.GetInt();
                }
                if (nowcount > 0)
                {
                    count[pc.PlayerId]--;
                    ReportDeadBodyPatch.ExReportDeadBody(pc, null, false);
                }
                SendRPC(pc.PlayerId);
            }
        }
        public static string OtherMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
        {
            seen ??= seer;

            if (seer == seen && seer.Is(CustomRoles.Ghostbuttoner))
            {
                var LimitCount = 0;
                if (count.ContainsKey(seer.PlayerId)) LimitCount = count[seer.PlayerId];
                else LimitCount = Count.GetInt();
                return Utils.ColorString(UtilsRoleText.GetRoleColor(CustomRoles.Ghostbuttoner).ShadeColor(-0.25f), $" ({LimitCount}/{Count.GetInt()})");
            }
            return "";
        }

        public static void SendRPC(byte playerId)
        {
            using var sender = new SubRoleRPCSender(CustomRoles.Ghostbuttoner, playerId);
            sender.Writer.Write(count[playerId]);
        }

        public static void ReceiveRPC(MessageReader reader, byte playerId)
        {
            count[playerId] = reader.ReadInt32();
        }
    }
}