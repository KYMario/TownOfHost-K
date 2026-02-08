using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;
using TownOfHost.Roles.AddOns.Neutral;
using System.Linq;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class Amanojaku
    {
        private static readonly int Id = 19100;
        public static List<byte> playerIdList = new();
        public static OptionItem AssingDay;
        public static OptionItem SurvivetoWin;
        public static Dictionary<CustomWinner, OptionItem> OptionRole = new();
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Amanojaku);
            AmanojakuAssing.Create(Id + 10, CustomRoles.Amanojaku, true, true);
            AssingDay = IntegerOptionItem.Create(Id + 20, "AmanojakuAssingDay", new(0, 99, 1), 4, TabGroup.Addons, false).SetSubRoleOptionItem(CustomRoles.Amanojaku).SetValueFormat(OptionFormat.day);
            ObjectOptionitem.Create(Id + 21, "AddonOption", true, "", TabGroup.Addons).SetOptionName(() => "Role Option").SetSubRoleOptionItem(CustomRoles.Amanojaku);
            SurvivetoWin = BooleanOptionItem.Create(Id + 22, "AmanojakuSurvivetoWin", true, TabGroup.Addons, false).SetSubRoleOptionItem(CustomRoles.Amanojaku);

            var id = 52 + Id;
            foreach (var _customwinner in EnumHelper.GetAllValues<CustomWinner>().Where(x => x < CustomWinner.TaskPlayerB && x is not CustomWinner.Crewmate && x > CustomWinner.Draw))
            {
                var customwinner = _customwinner;
                if (SoloWinOption.AllData.ContainsKey((CustomRoles)customwinner) && !OptionRole.ContainsKey(customwinner))
                {
                    var option = BooleanOptionItem.Create(id++, "AmanojakuCanwin", true, TabGroup.Addons, false).SetSubRoleOptionItem(CustomRoles.Amanojaku).SetEnabled(() => ((CustomRoles)customwinner).IsEnable() || customwinner is CustomWinner.Impostor or CustomWinner.Jackal);
                    option.ReplacementDictionary = new Dictionary<string, string> { { "%winner%", UtilsRoleText.GetRoleColorAndtext((CustomRoles)customwinner) } };
                    if (!OptionRole.TryAdd(customwinner, option))
                    {
                        Logger.Error($"{customwinner}が重複", "Amanojaku");
                    }
                }
            }
        }

        public static void Init()
        {
            playerIdList = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static void Assign()
        {
            if (AssingDay.GetInt() == UtilsGameLog.day && AssingDay.GetInt() is not 0)
                AmanojakuAssing.AssignAddOnsFromList();
        }

        public static bool CheckWin(PlayerControl pc, GameOverReason reason)
        {
            if (pc.IsLovers()) return false;

            if (playerIdList.Contains(pc.PlayerId))
            {
                //クルーなら負け。
                if (reason.Equals(GameOverReason.CrewmatesByTask) || reason.Equals(GameOverReason.CrewmatesByVote)) goto remove;
                // 死んでて生きてないと勝利できないなら負け。
                if (!pc.IsAlive() && SurvivetoWin.GetBool()) goto remove;
                //LNかつオポが付与されているなら処理しない
                if (pc.Is(CustomRoles.LastNeutral) && LastNeutral.GiveOpportunist.GetBool()) return false;

                Logger.Info("aaa", "aaa");
                var Canwin = false;
                foreach (var data in OptionRole.Where(d => d.Value.GetBool()))
                {
                    if (CustomWinnerHolder.winners.Contains(data.Key))
                    {
                        Canwin = true;
                        break;
                    }
                }
                if (Canwin is false) return false;
                Logger.Info($"{pc.Data.GetLogPlayerName()}:天邪鬼で追加勝利", "Amanojaku");
                CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
                CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.Amanojaku);
                return true;
            }

            return false;

        remove:
            CustomWinnerHolder.WinnerIds.Remove(pc.PlayerId);
            CustomWinnerHolder.CantWinPlayerIds.Add(pc.PlayerId);
            return false;
        }
    }
}