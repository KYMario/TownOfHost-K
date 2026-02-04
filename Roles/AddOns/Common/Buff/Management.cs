using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class Management
    {
        private static readonly int Id = 17200;
        public static Color RoleColor = UtilsRoleText.GetRoleColor(CustomRoles.Management);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "θ");
        public static List<byte> playerIdList = new();
        private static OptionItem OptionPercentGage;
        private static OptionItem OptionCanSeeActivecomms;
        public static bool CanSeeActiveComms;
        public static bool PercentGage;
        public static OptionItem Meeting;
        public static OptionItem OptionRoughPercentage;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Management, fromtext: UtilsOption.GetFrom(From.TownOfHost_Y));
            AddOnsAssignData.Create(Id + 10, CustomRoles.Management, true, true, true, true);
            ObjectOptionitem.Create(Id + 57, "AddonOption", true, "", TabGroup.Addons).SetOptionName(() => "Role Option").SetSubRoleOptionItem(CustomRoles.Management);
            OptionPercentGage = BooleanOptionItem.Create(Id + 50, "PercentGage", true, TabGroup.Addons, false).SetSubRoleOptionItem(CustomRoles.Management);
            OptionRoughPercentage = BooleanOptionItem.Create(Id + 51, "RoughPercentage", true, TabGroup.Addons, false).SetParent(OptionPercentGage).SetSubRoleOptionItem(CustomRoles.Management);
            OptionCanSeeActivecomms = BooleanOptionItem.Create(Id + 55, "CanUseActiveComms", true, TabGroup.Addons, false).SetSubRoleOptionItem(CustomRoles.Management);
            Meeting = BooleanOptionItem.Create(Id + 56, "CanseeMeeting", true, TabGroup.Addons, false).SetSubRoleOptionItem(CustomRoles.Management);
        }
        public static void Init()
        {
            playerIdList = new();
            PercentGage = OptionPercentGage.GetBool();
            CanSeeActiveComms = OptionCanSeeActivecomms.GetBool();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
    }
}