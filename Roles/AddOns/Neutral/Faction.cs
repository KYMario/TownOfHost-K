using System.Collections.Generic;
using System.Linq;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.AddOns.Neutral;

class Faction
{
    public static Dictionary<CustomRoles, OptionItem> OptionRole = new();
    static OptionItem CanSeeFactionMate;
    public static void SetUpOption()
    {
        Options.SetupRoleOptions(19600, TabGroup.Addons, CustomRoles.Faction, new(1, 1, 1));
        CanSeeFactionMate = BooleanOptionItem.Create(19611, "CanSeeFactionMate", false, TabGroup.Addons, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Faction]).SetParentRole(CustomRoles.Faction);

        var id = 19620;
        foreach (var role in EnumHelper.GetAllValues<CustomRoles>().Where(role => role.IsNeutral()).OrderBy(x => x.GetRoleInfo()?.ConfigId ?? 100000))
        {
            if (SoloWinOption.AllData.ContainsKey(role))
            {
                var option = BooleanOptionItem.Create(id++, "AssingroleType", true, TabGroup.Addons, false).SetParentRole(CustomRoles.Faction).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Faction]).SetCansee(() => role.IsEnable());
                option.ReplacementDictionary = new Dictionary<string, string> { { "%roletype%", UtilsRoleText.GetRoleColorAndtext(role) } };

                if (!OptionRole.TryAdd(role, option))
                {
                    Logger.Error($"{role}重複したよ!!!", "Faction");
                }
            }
        }
    }

    public static void CheckWin()
    {
        if (CustomWinnerHolder.WinnerTeam is CustomWinner.Default or CustomWinner.None or CustomWinner.Draw or CustomWinner.Crewmate or CustomWinner.Impostor) return;

        var role = (CustomRoles)CustomWinnerHolder.WinnerTeam;
        if (OptionRole.TryGetValue(role, out var option))
        {
            if (option.GetBool())
            {
                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Faction);
                foreach (var player in PlayerCatch.AllPlayerControls.Where(pc => pc.Is(CustomRoles.Faction)))
                {
                    if (player.IsRiaju())
                    {
                        CustomWinnerHolder.IdRemoveLovers.Add(player.PlayerId);
                        continue;
                    }
                    CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                }
            }
        }

        return;
    }

    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (CanSeeFactionMate.GetBool())
        {
            if (seer.Is(CustomRoles.Faction) && seen.Is(CustomRoles.Faction))
            {
                return Utils.ColorString(UtilsRoleText.GetRoleColor(CustomRoles.Faction), "δ");
            }
        }
        return "";
    }
    public static void AssingFaction()
    {
        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
        var p = Options.GetRoleChance(CustomRoles.Faction);
        if (p is 100 || IRandom.Instance.Next(1, 100) <= p)
        {
            Logger.Info("徒党のおでまし", "Faction");

            foreach (var player in PlayerCatch.AllPlayerControls)
            {
                var role = player.GetCustomRole();
                role = role is CustomRoles.Jackal or CustomRoles.JackalAlien or CustomRoles.JackalMafia ? CustomRoles.Jackal : role;
                if (OptionRole.TryGetValue(role, out var option))
                {
                    if (option.GetBool())
                    {
                        PlayerState.GetByPlayerId(player.PlayerId).SetSubRole(CustomRoles.Faction);
                        Logger.Info($"役職設定:{player.Data.GetLogPlayerName()} + Faction", "Faction");
                    }
                }
            }

            if (PlayerCatch.AllPlayerControls.Where(pc => pc.Is(CustomRoles.Faction)).Count() is 1)
            {
                foreach (var player in PlayerCatch.AllPlayerControls.Where(player => player.Is(CustomRoles.Faction)))
                {
                    player.GetPlayerState().RemoveSubRole(CustomRoles.Faction);
                }
                Logger.Info("徒党が1人だから削除", "Faction");
            }
        }
    }
}