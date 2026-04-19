using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class Sunglasses
    {
        private static readonly int Id = 24200;
        private static Color RoleColor = UtilsRoleText.GetRoleColor(CustomRoles.Sunglasses);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "Ｓ");
        public static List<byte> playerIdList = new();
        public static OptionItem SunglassesVisionmagnification;

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Sunglasses, fromtext: UtilsOption.GetFrom(From.TheOtherRoles));
            AddOnsAssignData.Create(Id + 10, CustomRoles.Sunglasses, true, true, true, true);
            ObjectOptionitem.Create(Id + 51, "AddonOption", true, "", TabGroup.Addons).SetOptionName(() => "Role Option").SetSubRoleOptionItem(CustomRoles.Sunglasses);
            SunglassesVisionmagnification = FloatOptionItem.Create(Id + 50, "SunglassesVisionmagnification", new(1f, 100f, 1f), 75, TabGroup.Addons, false).SetSubRoleOptionItem(CustomRoles.Sunglasses).SetValueFormat(OptionFormat.Percent)
            .SetTooltip(() => string.Format(Translator.GetString("SunglassesVisionmagnification_Info"), Main.NormalOptions.CrewLightMod, Main.NormalOptions.CrewLightMod * SunglassesVisionmagnification.GetFloat() * 0.01f, Main.NormalOptions.ImpostorLightMod, Main.NormalOptions.ImpostorLightMod * SunglassesVisionmagnification.GetFloat() * 0.01f));
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