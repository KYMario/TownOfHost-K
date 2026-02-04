using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class PlusVote
    {
        private static readonly int Id = 17500;
        private static Color RoleColor = UtilsRoleText.GetRoleColor(CustomRoles.PlusVote);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "р");
        public static List<byte> playerIdList = new();
        public static OptionItem AdditionalVote;

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.PlusVote, fromtext: UtilsOption.GetFrom(From.TownOfHost_Y));
            AddOnsAssignData.Create(Id + 10, CustomRoles.PlusVote, true, true, true, true);
            ObjectOptionitem.Create(Id + 51, "AddonOption", true, "", TabGroup.Addons).SetOptionName(() => "Role Option").SetSubRoleOptionItem(CustomRoles.PlusVote);
            AdditionalVote = IntegerOptionItem.Create(Id + 50, "MayorAdditionalVote", new(1, 99, 1), 1, TabGroup.Addons, false).SetValueFormat(OptionFormat.Votes).SetSubRoleOptionItem(CustomRoles.PlusVote);
        }

        public static void Init()
        {
            playerIdList = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
    }
}