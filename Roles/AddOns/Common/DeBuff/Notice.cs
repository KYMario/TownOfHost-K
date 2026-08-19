using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class Notice
    {
        private static readonly int Id = 25600;
        private static readonly Color RoleColor = UtilsRoleText.GetRoleColor(CustomRoles.Notice);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "Ｎo");

        private static OptionItem OptionTellTeam;
        private static OptionItem OptionTellEveryone;

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Notice);
            AddOnsAssignData.Create(Id + 10, CustomRoles.Notice, true, true, true, true);
            ObjectOptionitem.Create(Id + 53, "AddonOption", true, "", TabGroup.Addons)
                .SetOptionName(() => "Role Option")
                .SetSubRoleOptionItem(CustomRoles.Notice);
            OptionTellTeam = BooleanOptionItem.Create(Id + 50, "NoticeTellTeam", false, TabGroup.Addons, false)
                .SetSubRoleOptionItem(CustomRoles.Notice);
            OptionTellEveryone = BooleanOptionItem.Create(Id + 51, "NoticeTellEveryone", false, TabGroup.Addons, false)
                .SetSubRoleOptionItem(CustomRoles.Notice);
        }

        public static void Init() { }

        public static void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (reporter == null || target?.Object == null) return;

            var deadPlayer = target.Object;
            if (deadPlayer.IsAlive()) return;
            if (!deadPlayer.Is(CustomRoles.Notice)) return;

            var revealTeam = OptionTellTeam.GetBool();
            var sendToAll = OptionTellEveryone.GetBool();
            var subject = revealTeam ? GetTeamText(deadPlayer) : UtilsRoleText.GetRoleColorAndtext(deadPlayer.GetCustomRole());
            var message = string.Format(GetString(revealTeam ? "NoticeRevealTeamMessage" : "NoticeRevealRoleMessage"), UtilsName.GetPlayerColor(deadPlayer.PlayerId, true), subject);
            var title = Utils.ColorString(RoleColor, GetString("Notice"));

            if (sendToAll)
            {
                Utils.SendMessage(message, title: title);
                MeetingHudPatch.StartPatch.meetingsends.Add((byte.MaxValue, title, message));
            }
            else
            {
                Utils.SendMessage(message, reporter.PlayerId, title);
                MeetingHudPatch.StartPatch.meetingsends.Add((reporter.PlayerId, title, message));
            }
        }

        private static string GetTeamText(PlayerControl player)
        {
            var team = player.GetCustomRole().GetCustomRoleTypes();
            if (player.Is(CustomRoles.Amanojaku) || player.IsLovers()) team = CustomRoleTypes.Neutral;
            var color = UtilsRoleText.GetRoleColor(team switch
            {
                CustomRoleTypes.Crewmate => CustomRoles.Crewmate,
                CustomRoleTypes.Impostor => CustomRoles.Impostor,
                CustomRoleTypes.Madmate => CustomRoles.Madmate,
                _ => CustomRoles.Jester
            });
            return Utils.ColorString(color, GetString(team.ToString()));
        }
    }
}
