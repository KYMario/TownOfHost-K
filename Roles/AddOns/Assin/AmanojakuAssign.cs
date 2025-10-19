using System;
using System.Linq;
using System.Collections.Generic;

using TownOfHost.Roles.Core;
using static TownOfHost.Options;
using static TownOfHost.Translator;


namespace TownOfHost.Roles.AddOns.Common
{
    /// <summary>
    /// 天邪鬼専用assign
    /// </summary>
    public class AmanojakuAssing
    {
        static Dictionary<CustomRoles, AmanojakuAssing> AllData = new();
        public CustomRoles Role { get; private set; }
        public int IdStart { get; private set; }
        OptionItem CrewmateMaximum;
        AssignOptionItem CrewmateAssignTarget;
        OptionItem NeutralMaximum;
        AssignOptionItem NeutralAssignTarget;
        static readonly CustomRoles[] InvalidRoles =
        {
            CustomRoles.Phantom,
            CustomRoles.Emptiness,
            CustomRoles.GuardianAngel,
            CustomRoles.SKMadmate,
            CustomRoles.Jackaldoll,
            CustomRoles.HASFox,
            CustomRoles.HASTroll,
            CustomRoles.GM,
            CustomRoles.TaskPlayerB,
            CustomRoles.Fox,
            CustomRoles.King
        };
        static readonly IEnumerable<CustomRoles> ValidRoles = CustomRolesHelper.AllRoles.Where(role => !InvalidRoles.Contains(role));

        public AmanojakuAssing(int idStart, CustomRoles role, bool assignCrewmate, bool assignNeutral)
        {
            this.IdStart = idStart;
            this.Role = role;
            if (assignCrewmate)
            {
                CrewmateMaximum = IntegerOptionItem.Create(idStart++, "%roleTypes%Maximum", new(0, 15, 1), 15, TabGroup.Addons, false)
                    .SetParent(CustomRoleSpawnChances[role]).SetParentRole(role)
                    .SetValueFormat(OptionFormat.Players);
                CrewmateMaximum.ReplacementDictionary = new Dictionary<string, string> { { "%roleTypes%", Utils.ColorString(Palette.CrewmateBlue, GetString("TeamCrewmate")) } };
                CrewmateAssignTarget = (AssignOptionItem)AssignOptionItem.Create(idStart++, "FixedRole", 0, TabGroup.Addons, false, crew: true, notassing: InvalidRoles)
                    .SetParent(CrewmateMaximum).SetParentRole(role);
            }

            if (assignNeutral)
            {
                NeutralMaximum = IntegerOptionItem.Create(idStart++, "%roleTypes%Maximum", new(0, 15, 1), 15, TabGroup.Addons, false)
                    .SetParent(CustomRoleSpawnChances[role]).SetParentRole(role)
                    .SetValueFormat(OptionFormat.Players);
                NeutralMaximum.ReplacementDictionary = new Dictionary<string, string> { { "%roleTypes%", Utils.ColorString(Palette.AcceptedGreen, GetString("Neutral")) } };
                NeutralAssignTarget = (AssignOptionItem)AssignOptionItem.Create(idStart++, "FixedRole", 0, TabGroup.Addons, false, neu: true, notassing: InvalidRoles)
                    .SetParent(NeutralMaximum).SetParentRole(role);
            }

            if (!AllData.ContainsKey(role)) AllData.Add(role, this);
            else Logger.Warn("重複したCustomRolesを対象とするAmanojakuAssingが作成されました", "AmanojakuAssing");
        }
        public static AmanojakuAssing Create(int idStart, CustomRoles role, bool assignCrewmate, bool assignNeutral)
            => new(idStart, role, assignCrewmate, assignNeutral);
        ///<summary>
        ///AmanojakuAssingが存在する属性を一括で割り当て
        ///</summary>
        public static void AssignAddOnsFromList()
        {
            foreach (var kvp in AllData)
            {
                var (role, data) = kvp;
                if (!role.IsPresent()) continue;
                var assignTargetList = AssignTargetList(data);

                foreach (var pc in assignTargetList)
                {
                    UtilsGameLog.AddGameLog($"Amanojaku", string.Format(GetString("Log.Amanojaku"), UtilsName.GetPlayerColor(pc)));
                    PlayerState.GetByPlayerId(pc.PlayerId).SetSubRole(role);
                    Logger.Info("役職設定:" + pc?.Data?.GetLogPlayerName() + " = " + pc.GetCustomRole().ToString() + " + " + role.ToString(), "AssignCustomSubRoles");
                    Amanojaku.Add(pc.PlayerId);
                    UtilsGameLog.LastLogRole[pc.PlayerId] = Utils.ColorString(UtilsRoleText.GetRoleColor(CustomRoles.Amanojaku), GetString("Amanojaku") + GetString($"{pc.GetCustomRole()}"));
                }
            }
        }
        ///<summary>
        ///アサインするプレイヤーのList
        ///</summary>
        private static List<PlayerControl> AssignTargetList(AmanojakuAssing data)
        {
            var rnd = IRandom.Instance;
            var candidates = new List<PlayerControl>();
            var validPlayers = PlayerCatch.AllAlivePlayerControls.Where(pc => ValidRoles.Contains(pc.GetCustomRole()));

            if (data.CrewmateMaximum != null)
            {
                var crewmateMaximum = data.CrewmateMaximum.GetInt();
                if (crewmateMaximum > 0)
                {
                    var crewmates = validPlayers.Where(pc
                        => data.CrewmateAssignTarget.GetBool() ? data.CrewmateAssignTarget.RoleValues[AssignOptionItem.Getpresetid()].Contains(pc.GetCustomRole()) :
                        pc.Is(CustomRoleTypes.Crewmate)).ToList();
                    for (var i = 0; i < crewmateMaximum; i++)
                    {
                        if (crewmates.Count == 0) break;
                        var selectedCrewmate = crewmates[rnd.Next(crewmates.Count)];
                        candidates.Add(selectedCrewmate);
                        crewmates.Remove(selectedCrewmate);
                    }
                }
            }

            if (data.NeutralMaximum != null)
            {
                var neutralMaximum = data.NeutralMaximum.GetInt();
                if (neutralMaximum > 0)
                {
                    var neutrals = validPlayers.Where(pc
                        => data.NeutralAssignTarget.GetBool() ? data.NeutralAssignTarget.RoleValues[AssignOptionItem.Getpresetid()].Contains(pc.GetCustomRole()) :
                        pc.Is(CustomRoleTypes.Neutral)).ToList();
                    for (var i = 0; i < neutralMaximum; i++)
                    {
                        if (neutrals.Count == 0) break;
                        var selectedNeutral = neutrals[rnd.Next(neutrals.Count)];
                        candidates.Add(selectedNeutral);
                        neutrals.Remove(selectedNeutral);
                    }
                }
            }

            while (candidates.Count > data.Role.GetRealCount())
                candidates.RemoveAt(rnd.Next(candidates.Count));

            return candidates;
        }
    }
}