using System.Collections.Generic;
using System.Linq;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.AddOns.Neutral;

class Faction
{
    public static Dictionary<CustomRoles, OptionItem> OptionRole = new();
    static OptionItem CanSeeFactionMate;
    public static OptionItem CantKillFaction;
    public static void SetUpOption()
    {
        Options.SetupRoleOptions(19600, TabGroup.Addons, CustomRoles.Faction, new(1, 1, 1));
        CanSeeFactionMate = BooleanOptionItem.Create(19611, "CanSeeFactionMate", false, TabGroup.Addons, false).SetSubRoleOptionItem(CustomRoles.Faction);
        CantKillFaction = BooleanOptionItem.Create(19613, "CantKillFaction", false, TabGroup.Addons, false).SetSubRoleOptionItem(CustomRoles.Faction);
        ObjectOptionitem.Create(19612, "AddonOption", true, "", TabGroup.Addons).SetOptionName(() => "Assign Option").SetSubRoleOptionItem(CustomRoles.Faction);

        var id = 19620;
        List<CustomRoles> AddWinners = [CustomRoles.Amanojaku];
        foreach (var role in EnumHelper.GetAllValues<CustomRoles>().Where(role => role.IsNeutral()).OrderBy(x => x.GetRoleInfo()?.ConfigId ?? 100000))
        {
            if (role is CustomRoles.Madonna) continue;
            if (SoloWinOption.AllData.ContainsKey(role) is false)
            {
                if (role is CustomRoles.Jackaldoll or CustomRoles.JackalAlien or CustomRoles.JackalMafia) continue;

                AddWinners.Add(role);
                continue;
            }
            var option = BooleanOptionItem.Create(id++, "AssingroleType", true, TabGroup.Addons, false).SetSubRoleOptionItem(CustomRoles.Faction).SetEnabled(() =>
            {
                if (role is not CustomRoles.Jackal) return role.IsEnable();

                return CustomRoles.Jackal.IsEnable() || CustomRoles.JackalAlien.IsEnable() || CustomRoles.JackalMafia.IsEnable() || CustomRoles.Jackaldoll.IsEnable();
            });
            option.ReplacementDictionary = new Dictionary<string, string> { { "%roletype%", UtilsRoleText.GetRoleColorAndtext(role) } };

            if (!OptionRole.TryAdd(role, option))
            {
                Logger.Error($"{role}重複したよ!!!", "Faction");
            }
        }

        foreach (var role in AddWinners)
        {
            var option = BooleanOptionItem.Create(id++, "AssingroleType", true, TabGroup.Addons, false).SetSubRoleOptionItem(CustomRoles.Faction).SetEnabled(() => role.IsEnable());
            option.ReplacementDictionary = new Dictionary<string, string> { { "%roletype%", UtilsRoleText.GetRoleColorAndtext(role) } };

            if (!OptionRole.TryAdd(role, option))
            {
                Logger.Error($"{role}重複したよ!!!", "Faction");
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
                    if (player.IsLovers() || player.GetCustomRole() is CustomRoles.Emptiness)
                    {
                        CustomWinnerHolder.CantWinPlayerIds.Add(player.PlayerId);
                        continue;
                    }
                    CustomWinnerHolder.CantWinPlayerIds.Remove(player.PlayerId);
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
        var chance = Options.GetRoleChance(CustomRoles.Faction);
        if (chance is 100 || IRandom.Instance.Next(1, 100) <= chance)
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
                        continue;
                    }
                }
                if (player.Is(CustomRoles.Amanojaku) && OptionRole.TryGetValue(CustomRoles.Amanojaku, out var amaopt))
                {
                    if (amaopt.GetBool())
                    {
                        PlayerState.GetByPlayerId(player.PlayerId).SetSubRole(CustomRoles.Faction);
                        Logger.Info($"役職設定:{player.Data.GetLogPlayerName()} + Faction", "Faction");
                        continue;
                    }
                }
            }
        }

        var allfaction = PlayerCatch.AllPlayerControls.Where(player => player.Is(CustomRoles.Faction));

        if (allfaction.Count() is 1)
        {
            foreach (var player in allfaction)
            {
                player.GetPlayerState().RemoveSubRole(CustomRoles.Faction);
            }
            Logger.Info("徒党が1人だから削除", "Faction");
        }

        // 徒党が無事始動した場合、色を付ける
        foreach (var seer in allfaction)
        {
            foreach (var seen in allfaction)
            {
                if (seer == seen) continue;
                NameColorManager.Add(seer.PlayerId, seen.PlayerId, UtilsRoleText.GetRoleColorCode(CustomRoles.Faction));
            }
        }
    }
}