using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class News
    {
        private static readonly int Id = 23900;
        private static Color RoleColor = UtilsRoleText.GetRoleColor(CustomRoles.News);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "Ｎ");
        public static List<byte> playerIdList = new();
        static OptionItem OptionSendDay; static int sendday;
        static OptionItem OptionSendDayEveryturn; static bool senddayeveryturn;
        static OptionItem OptionSendPlayerCount; static int sendplayercount;
        static OptionItem OptionSendPlayerEveryturn; static bool sendplayercounteveryturn;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.News);
            AddOnsAssignData.Create(Id + 10, CustomRoles.News, true, true, true, true);
            //OptionSidekickToremove = BooleanOptionItem.Create(Id + 19, "NewsSidekickToremove", false, TabGroup.Addons, false)
            //.SetParentRole(CustomRoles.News).SetParent(CustomRoleSpawnChances[CustomRoles.News]);
            OptionSendDay = IntegerOptionItem.Create(Id + 20, "NewsSendDay", new(0, 30, 1), 4, TabGroup.Addons, false)
            .SetParentRole(CustomRoles.News).SetParent(CustomRoleSpawnChances[CustomRoles.News])
            .SetValueFormat(OptionFormat.day).SetZeroNotation(OptionZeroNotation.Off);
            OptionSendDayEveryturn = BooleanOptionItem.Create(Id + 21, "Everyturn", false, TabGroup.Addons, false)
            .SetParentRole(CustomRoles.News).SetParent(OptionSendDay);
            OptionSendPlayerCount = IntegerOptionItem.Create(Id + 22, "NewsSendPlayerCount", new(0, 15, 1), 6, TabGroup.Addons, false)
            .SetParentRole(CustomRoles.News).SetParent(CustomRoleSpawnChances[CustomRoles.News])
            .SetValueFormat(OptionFormat.Players).SetZeroNotation(OptionZeroNotation.Off);
            OptionSendPlayerEveryturn = BooleanOptionItem.Create(Id + 23, "Everyturn", false, TabGroup.Addons, false)
            .SetParentRole(CustomRoles.News).SetParent(OptionSendPlayerCount);
        }
        public static void Init()
        {
            playerIdList = new();
            sendday = OptionSendDay.GetInt();
            senddayeveryturn = OptionSendDayEveryturn.GetBool();
            sendplayercount = OptionSendPlayerCount.GetInt();
            sendplayercounteveryturn = OptionSendPlayerEveryturn.GetBool();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }

        public static string SendMessage()
        {
            var playercount = PlayerCatch.AllAlivePlayersCount;
            if ((((senddayeveryturn && sendday <= UtilsGameLog.day) || (sendday == UtilsGameLog.day)) && sendday is not 0)
            || ((sendplayercounteveryturn && playercount <= sendplayercount) || (playercount == sendplayercount)) && playercount is not 0)
            { }//どの条件にも当てはまらない場合は処理を行わない
            else return "";

            var sendmessage = "";
            List<string> SendRole = new();
            foreach (var playerid in playerIdList)
            {
                var player = playerid.GetPlayerControl();
                if (player.IsAlive())
                {
                    SendRole.Add(UtilsRoleText.GetRoleColorAndtext(player.GetCustomRole()));
                }
            }
            sendmessage = string.Format(Translator.GetString($"News_MeetingNews{IRandom.Instance.Next(3)}"), string.Join('、', SendRole));
            return sendmessage;
        }
    }
}