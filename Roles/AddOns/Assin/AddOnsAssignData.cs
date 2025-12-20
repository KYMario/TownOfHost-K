using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;

using TownOfHost.Roles.Core;
using static TownOfHost.Options;
using static TownOfHost.Translator;
using TownOfHost.Modules;

namespace TownOfHost.Roles.AddOns.Common
{
    /// <summary>
    /// 全陣営が付与される属性。
    /// </summary>
    public class AddOnsAssignData
    {
        public static Dictionary<CustomRoles, AddOnsAssignData> AllData = new();
        public CustomRoles Role { get; private set; }
        public int IdStart { get; private set; }
        public OptionItem CrewmateMaximum;
        AssignOptionItem CrewmateAssignTarget;
        public OptionItem ImpostorMaximum;
        AssignOptionItem ImpostorAssignTarget;
        public OptionItem MadmateMaximum;
        AssignOptionItem MadmateAssignTarget;
        public OptionItem NeutralMaximum;
        AssignOptionItem NeutralAssignTarget;
        static readonly CustomRoles[] InvalidRoles =
        {
            CustomRoles.Emptiness,
            CustomRoles.Phantom,
            CustomRoles.GuardianAngel,
            CustomRoles.SKMadmate,
            CustomRoles.Jackaldoll,
            CustomRoles.HASFox,
            CustomRoles.HASTroll,
            CustomRoles.GM,
            CustomRoles.TaskPlayerB,
        };
        static readonly IEnumerable<CustomRoles> ValidRoles = CustomRolesHelper.AllRoles.Where(role => !InvalidRoles.Contains(role));

        public AddOnsAssignData(int idStart, CustomRoles role, bool assignCrewmate, bool assignMadmate, bool assignImpostor, bool assignNeutral)
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

            if (assignImpostor)
            {
                ImpostorMaximum = IntegerOptionItem.Create(idStart++, "%roleTypes%Maximum", new(0, 3, 1), 3, TabGroup.Addons, false)
                    .SetParent(CustomRoleSpawnChances[role])
                    .SetValueFormat(OptionFormat.Players).SetParentRole(role);
                ImpostorMaximum.ReplacementDictionary = new Dictionary<string, string> { { "%roleTypes%", Utils.ColorString(Palette.ImpostorRed, GetString("TeamImpostor")) } };
                ImpostorAssignTarget = (AssignOptionItem)AssignOptionItem.Create(idStart++, "FixedRole", 0, TabGroup.Addons, false, imp: true, notassing: InvalidRoles)
                    .SetParent(ImpostorMaximum).SetParentRole(role);
            }
            if (assignMadmate)
            {
                MadmateMaximum = IntegerOptionItem.Create(idStart++, "%roleTypes%Maximum", new(0, 15, 1), 15, TabGroup.Addons, false)
                    .SetParent(CustomRoleSpawnChances[role]).SetParentRole(role)
                    .SetValueFormat(OptionFormat.Players);
                MadmateMaximum.ReplacementDictionary = new Dictionary<string, string> { { "%roleTypes%", Utils.ColorString(Palette.ImpostorRed, GetString("Madmate")) } };
                MadmateAssignTarget = (AssignOptionItem)AssignOptionItem.Create(idStart++, "FixedRole", 0, TabGroup.Addons, false, mad: true, notassing: InvalidRoles)
                    .SetParent(MadmateMaximum).SetParentRole(role);
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
            else Logger.Warn("重複したCustomRolesを対象とするAddOnsAssignDataが作成されました", "AddOnsAssignData");
        }
        public static AddOnsAssignData Create(int idStart, CustomRoles role, bool assignCrewmate, bool assignMadmate, bool assignImpostor, bool assignNeutral)
            => new(idStart, role, assignCrewmate, assignMadmate, assignImpostor, assignNeutral);
        ///<summary>
        ///AddOnsAssignDataが存在する属性を一括で割り当て
        ///</summary>
        public static void AssignAddOnsFromList()
        {
            foreach (var kvp in AllData)
            {
                var (role, data) = kvp;
                if (!role.IsPresent()) continue;
                var assignTargetList = AssignTargetList(data);

                if (Options.CurrentGameMode is CustomGameMode.SuddenDeath && SuddenDeathMode.SuddenSharingRoles.GetBool() && assignTargetList.Count != 0)
                {
                    assignTargetList.Clear();
                    PlayerCatch.AllPlayerControls.Do(p => assignTargetList.Add(p));
                }
                foreach (var pc in assignTargetList)
                {
                    PlayerState.GetByPlayerId(pc.PlayerId).SetSubRole(role);
                    Logger.Info("役職設定:" + pc?.Data?.GetLogPlayerName() + " = " + pc.GetCustomRole().ToString() + " + " + role.ToString(), "AssignCustomSubRoles");
                }
            }
        }
        ///<summary>
        ///アサインするプレイヤーのList
        ///</summary>
        private static List<PlayerControl> AssignTargetList(AddOnsAssignData data)
        {
            var rnd = IRandom.Instance;
            var candidates = new List<PlayerControl>();
            var validPlayers = PlayerCatch.AllPlayerControls.Where(pc => ValidRoles.Contains(pc.GetCustomRole()));

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
                        if (data.Role is CustomRoles.Amnesia && selectedCrewmate.Is(CustomRoles.King))
                        {
                            crewmates.Remove(selectedCrewmate);
                            continue;
                        }
                        candidates.Add(selectedCrewmate);
                        crewmates.Remove(selectedCrewmate);
                    }
                }
            }

            if (data.ImpostorMaximum != null)
            {
                var impostorMaximum = data.ImpostorMaximum.GetInt();
                if (impostorMaximum > 0)
                {
                    var impostors = validPlayers.Where(pc
                        => data.ImpostorAssignTarget.GetBool() ? data.ImpostorAssignTarget.RoleValues[AssignOptionItem.Getpresetid()].Contains(pc.GetCustomRole()) :
                        pc.Is(CustomRoleTypes.Impostor)).ToList();
                    for (var i = 0; i < impostorMaximum; i++)
                    {
                        if (impostors.Count == 0) break;
                        var selectedImpostor = impostors[rnd.Next(impostors.Count)];
                        candidates.Add(selectedImpostor);
                        impostors.Remove(selectedImpostor);
                    }
                }
            }

            if (data.MadmateMaximum != null)
            {
                var MadmateMaximum = data.MadmateMaximum.GetInt();
                if (MadmateMaximum > 0)
                {
                    var Madmates = validPlayers.Where(pc
                        => data.MadmateAssignTarget.GetBool() ? data.MadmateAssignTarget.RoleValues[AssignOptionItem.Getpresetid()].Contains(pc.GetCustomRole()) :
                        pc.Is(CustomRoleTypes.Madmate)).ToList();
                    for (var i = 0; i < MadmateMaximum; i++)
                    {
                        if (Madmates.Count == 0) break;
                        var selectedMadmate = Madmates[rnd.Next(Madmates.Count)];
                        candidates.Add(selectedMadmate);
                        Madmates.Remove(selectedMadmate);
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