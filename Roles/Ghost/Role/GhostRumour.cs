using System.Collections.Generic;

using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.Ghost
{
    public class GhostRumour
    {
        static GhostRoleAssingData Data;
        private static readonly int Id = 16500;
        public static List<byte> playerIdList = new();
        public static OptionItem CoolDown;
        static OptionItem AssingMadmate;
        static List<byte> UsedPlayer = new();
        static Dictionary<byte, byte> SendList = new();
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.GhostRoles, CustomRoles.GhostRumour, fromtext: UtilsOption.GetFrom(From.TownOfHost_K));
            Data = GhostRoleAssingData.Create(Id + 1, CustomRoles.GhostRumour, CustomRoleTypes.Crewmate);
            CoolDown = FloatOptionItem.Create(Id + 2, "Cooldown", new(0f, 180f, 0.5f), 25f, TabGroup.GhostRoles, false)
                .SetValueFormat(OptionFormat.Seconds).SetParent(CustomRoleSpawnChances[CustomRoles.GhostRumour]).SetParentRole(CustomRoles.GhostRumour);
            AssingMadmate = BooleanOptionItem.Create(Id + 5, "AssgingMadmate", false, TabGroup.GhostRoles, false).SetParentRole(CustomRoles.GhostRumour)
                        .SetParent(CustomRoleSpawnChances[CustomRoles.GhostRumour]);
        }

        public static void Init()
        {
            playerIdList = new();
            UsedPlayer = new();
            SendList = new();
            Data.SubRoleType = AssingMadmate.GetBool() ? CustomRoleTypes.Madmate : CustomRoleTypes.Crewmate;
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static void UseAbility(PlayerControl pc, PlayerControl target)
        {
            if (pc.Is(CustomRoles.GhostRumour))
            {
                pc.RpcResetAbilityCooldown();
                if (UsedPlayer.Contains(pc.PlayerId)) return;
                if (SendList.ContainsKey(pc.PlayerId)) return;

                SendList.Add(pc.PlayerId, target.PlayerId);
                Logger.Info($"{pc?.Data?.GetLogPlayerName() ?? "???"} => {target?.Data?.GetLogPlayerName() ?? "???"}", "GhostRumour");
            }
        }
        public static string SendMes()
        {
            List<byte> Sended = new();
            var send = "";
            foreach (var senddata in SendList)
            {
                var target = senddata.Value.GetPlayerControl();
                if (!target.IsAlive()) continue;
                Logger.Info($"Send : {senddata.Key} => {senddata.Value}", "GhostRumour");
                UsedPlayer.Add(senddata.Key);

                if (Sended.Contains(senddata.Value)) continue;
                Sended.Add(senddata.Value);

                send += string.Format(Translator.GetString("GhostRumourAbilityMeg"), UtilsRoleText.GetRoleColorAndtext(target.GetCustomRole()));
            }
            if (send is not "")
            {
                send = $"<size=90%><color=#707cab>{Translator.GetString("GhostRumourAbilityTitle")}</color></size>\n"
                /* */+ $"<size=70%>{send}</size>";
            }
            SendList.Clear();
            return send;
        }
    }
}