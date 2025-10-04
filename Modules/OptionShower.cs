using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using HarmonyLib;

using TownOfHost.Roles;
using TownOfHost.Roles.Core;
using static TownOfHost.Translator;

namespace TownOfHost
{
    public static class OptionShower
    {
        public static int currentPage = 0;
        public static bool Update = false;
        public static List<string> pages;
        public static string GetText()
        {
            if (pages == null || Update) UpdateText();

            if (currentPage >= pages.Count) currentPage = pages.Count - 1; //現在のページが最大ページ数を超えていれば最後のページに修正
            return $"{pages[currentPage]}{GetString("PressTabToNextPage")}({currentPage + 1}/{pages.Count})";
        }
        public static void UpdateText()
        {
            Update = false;
            if (AmongUsClient.Instance == null) return;

            //初期化
            var flug = false;
            StringBuilder sb = new();
            pages = new();
            {
                //1ページに基本ゲーム設定を格納
                StringBuilder gameoptionsb = new();
                GameOptionsManager.Instance.CurrentGameOptions.ToHudString(GameData.Instance ? GameData.Instance.PlayerCount : 10).Split("\r\n").SkipLast(10)
                .Do(option => gameoptionsb.Append(option + "\r\n"));
                pages.Add(gameoptionsb.ToString() + "\r\n");
            }
            //ゲームモードの表示
            sb.Append($"{Options.GameMode.GetName()}: {Options.GameMode.GetString()}\n\n");
            //sb.AppendFormat("{0}: {1}\n\n", RoleAssignManager.OptionAssignMode.GetName(), RoleAssignManager.OptionAssignMode.GetString());
            if (Options.HideGameSettings.GetBool() && !AmongUsClient.Instance.AmHost)
            {
                sb.Append($"<#ff0000>{GetString("Message.HideGameSettings")}</color>");
            }
            else
            {
                //Standardの時のみ実行
                if (Options.CurrentGameMode == CustomGameMode.Standard)
                {
                    var roleType = CustomRoleTypes.Impostor;
                    var farst = true;
                    var (imp, mad, crew, neu, addon, lover, gorst) = UtilsShowOption.GetRoleTypesCountInt();
                    //有効な役職一覧
                    sb.Append($"<{UtilsRoleText.GetRoleColorCode(CustomRoles.GM)}>{UtilsRoleText.GetRoleName(CustomRoles.GM)}:</color> {Options.EnableGM.GetString()}\n\n");
                    sb.Append(GetString("ActiveRolesList")).Append("<size=90%>");
                    var count = -1;
                    var lines = 0;
                    var lasttab = "";
                    var impostorrole = Options.CustomRoleSpawnChances.Where(r => r.Key.IsImpostor())?.ToArray();
                    var madmaterole = Options.CustomRoleSpawnChances.Where(r => r.Key.IsMadmate())?.ToArray();
                    var crewmaterole = Options.CustomRoleSpawnChances.Where(r => r.Key.IsCrewmate())?.ToArray();
                    var neutralrole = Options.CustomRoleSpawnChances.Where(r => r.Key.IsNeutral())?.ToArray();
                    var otherrole = Options.CustomRoleSpawnChances.Where(r => !r.Key.IsImpostor() && !r.Key.IsCrewmate() && !r.Key.IsMadmate() && !r.Key.IsNeutral()).ToArray();
                    var addoncheck = false;
                    foreach (var kvp in impostorrole.AddRangeToArray(madmaterole).AddRangeToArray(crewmaterole).AddRangeToArray(neutralrole).AddRangeToArray(otherrole))
                        if (kvp.Value.GameMode is CustomGameMode.Standard or CustomGameMode.All && kvp.Value.GetBool()) //スタンダードか全てのゲームモードで表示する役職
                        {
                            var role = kvp.Key;
                            if (farst && role.IsImpostor())
                            {
                                var maxtext = $"({imp})";
                                var (che, max, min) = RoleAssignManager.CheckRoleTypeCount(role.GetCustomRoleTypes());
                                if (che)
                                {
                                    maxtext += $"　[Min : {min}|Max : {max} ]";
                                }
                                lasttab = Utils.ColorString(Palette.ImpostorRed, "\n<u>☆Impostors☆" + maxtext + "</u>\n");
                                sb.Append(Utils.ColorString(Palette.ImpostorRed, "\n<u>☆Impostors☆" + maxtext + "</u>\n"));
                            }
                            farst = false;
                            if ((!addoncheck && roleType == CustomRoleTypes.Crewmate && role.IsSubRole()) || (role.GetCustomRoleTypes() != roleType && role.GetCustomRoleTypes() != CustomRoleTypes.Impostor))
                            {
                                var NowTabText = "";
                                var rolecount = 0;
                                var color = Color.white;
                                if (role.IsSubRole())
                                {
                                    NowTabText = "☆Add-ons☆";
                                    rolecount = addon + lover + gorst;
                                    color = ModColors.AddonsColor;
                                    count = -1;
                                    addoncheck = true;
                                }
                                else
                                    switch (role.GetCustomRoleTypes())
                                    {
                                        case CustomRoleTypes.Crewmate: count = -1; NowTabText = "☆CrewMates☆"; rolecount = crew; color = ModColors.CrewMateBlue; break;
                                        case CustomRoleTypes.Madmate: count = -1; NowTabText = "☆MadMates☆"; rolecount = mad; color = StringHelper.CodeColor("#ff7f50"); break;
                                        case CustomRoleTypes.Neutral: count = -1; NowTabText = "☆Neutrals☆"; rolecount = neu; color = ModColors.NeutralGray; break;
                                    }
                                var maxtext = $"({rolecount})";
                                var (che, max, min) = RoleAssignManager.CheckRoleTypeCount(role.GetCustomRoleTypes());
                                if (che && !role.IsSubRole())
                                {
                                    maxtext += $"　[Min : {min}|Max : {max} ]";
                                }
                                lasttab = Utils.ColorString(color, $"\n<u>{NowTabText + maxtext}</u>\n");
                                sb.Append(Utils.ColorString(color, $"\n<u>{NowTabText + maxtext}</u>\n"));
                                roleType = role.GetCustomRoleTypes();
                            }
                            var mark = role.IsImpostor() ? Utils.ColorString(Palette.ImpostorRed, "Ⓘ") : (role.IsCrewmate() ? Utils.ColorString(Palette.CrewmateBlue, "Ⓒ") : (role.IsMadmate() ? "<#ff7f50>Ⓜ</color>" : (role.IsNeutral() ? Utils.ColorString(ModColors.NeutralGray, "Ⓝ") : "<#cccccc>⦿</color>")));

                            if (role.IsBuffAddon()) mark = Utils.AdditionalWinnerMark;
                            if (role.IsLovers()) mark = Utils.ColorString(UtilsRoleText.GetRoleColor(CustomRoles.Lovers), "♥");
                            if (role.IsDebuffAddon()) mark = Utils.ColorString(Palette.DisabledGrey, "☆");
                            if (role.IsGhostRole()) mark = "<#8989d9>■</color>";

                            if (count == 0) sb.Append($"\n{mark}{UtilsRoleText.GetCombinationName(kvp.Key)}: {kvp.Value.GetString()}×{kvp.Key.GetCount()}");
                            else if (count == -1) sb.Append($"{mark}{UtilsRoleText.GetCombinationName(kvp.Key)}: {kvp.Value.GetString()}×{kvp.Key.GetCount()}");
                            else sb.Append($"<pos=39%>{mark}{UtilsRoleText.GetCombinationName(kvp.Key)}: {kvp.Value.GetString()}×{kvp.Key.GetCount()}</pos>");

                            if (count == 0) lines++;
                            count = count is 0 or -1 ? 1 : 0;

                            if (lines >= 27)
                            {
                                lines = 0;
                                count = -1;
                                pages.Add(sb.ToString() + "\n\n");
                                sb.Clear();
                                sb.Append("<size=90%>" + lasttab);
                            }
                        }
                    pages.Add(sb.ToString() + "\n\n</size>");
                    sb.Clear();
                }
                //有効な役職と詳細設定一覧
                pages.Add("");
                nameAndValue(Options.EnableGM);
                //いっくらなんでもこれが重すぎる！
                //30役職を上回ったらこの処理をスキップ
                if (Options.CustomRoleSpawnChances.Where(op => op.Value.GetBool()).Count() < 30)
                {
                    foreach (var kvp in Options.CustomRoleSpawnChances)
                    {
                        if (!kvp.Key.IsEnable() || kvp.Value.IsHiddenOn(Options.CurrentGameMode)) continue;
                        sb.Append('\n');
                        sb.Append($"</size><size=100%>{UtilsRoleText.GetCombinationName(kvp.Key)}: {kvp.Value.GetString()}×{kvp.Key.GetCount()}</size>\n<size=80%>");
                        ShowChildren(kvp.Value, ref sb, UtilsRoleText.GetRoleColor(kvp.Key).ShadeColor(-0.5f), 1);
                        string rule = Utils.ColorString(Palette.ImpostorRed.ShadeColor(-0.5f), "┣ ");
                        string ruleFooter = Utils.ColorString(Palette.ImpostorRed.ShadeColor(-0.5f), "┗ ");

                        if (kvp.Key.CanMakeMadmate()) //シェイプシフター役職の時に追加する詳細設定
                        {
                            sb.Append($"{ruleFooter}{Options.CanMakeMadmateCount.GetName()}: {Options.CanMakeMadmateCount.GetString()}\n");
                        }
                    }
                }
                else flug = true;
                sb.Append("</size><size=90%>");
                foreach (var opt in OptionItem.AllOptions.Where(x => x.Id >= 90000 && !x.IsHiddenOn(Options.CurrentGameMode) && x.Parent == null))
                {
                    if (opt.IsHeader) sb.Append('\n');
                    sb.Append($"{opt.GetName()}: {opt.GetString().RemoveSN()}\n");
                    if (opt.GetBool())
                        ShowChildren(opt, ref sb, Color.white, 1);
                }
                //Onの時に子要素まで表示するメソッド
                void nameAndValue(OptionItem o) => sb.Append($"{o.GetName()}: {o.GetString().RemoveSN()}\n");
            }
            if (flug) sb.Append($"\n{GetString("Warning.OverRole")}");
            //1ページにつき35行までにする処理
            List<string> tmp = new(sb.ToString().Split("\n\n"));
            for (var i = 0; i < tmp.Count; i++)
            {
                if (pages[^1].Count(c => c == '\n') + 1 + tmp[i].Count(c => c == '\n') + 1 > 35)
                    pages.Add(tmp[i] + "\n\n");
                else pages[^1] += tmp[i] + "\n\n";
            }
        }

        public static void Next()
        {
            currentPage++;
            if (currentPage >= pages.Count) currentPage = 0; //現在のページが最大ページを超えていれば最初のページに
        }
        private static void ShowChildren(OptionItem option, ref StringBuilder sb, Color color, int deep = 0)
        {
            foreach (var opt in option.Children.Select((v, i) => new { Value = v, Index = i + 1 }))
            {
                if (opt.Value.Name is "Maximum" or "FixedRole") continue;
                if (opt.Value.Name == "ResetDoorsEveryTurns" && !(Options.IsActiveFungle || Options.IsActiveAirship || Options.IsActivePolus)) continue;

                if (!opt.Value.GetBool())
                {
                    switch (opt.Value.Name)
                    {
                        case "GiveGuesser": continue;
                        case "GiveWatching": continue;
                        case "GiveManagement": continue;
                        case "GiveSeeing": continue;
                        case "GiveAutopsy": continue;
                        case "GiveTiebreaker": continue;
                        case "GiveMagicHand": continue;
                        case "GivePlusVote": continue;
                        case "GiveRevenger": continue;
                        case "GiveOpener": continue;
                        case "GiveAntiTeleporter": continue;
                        case "GiveLighting": continue;
                        case "GiveMoon": continue;
                        case "GiveElector": continue;
                        case "GiveInfoPoor": continue;
                        case "GiveNonReport": continue;
                        case "GiveTransparent": continue;
                        case "GiveNotvoter": continue;
                        case "GiveWater": continue;
                        case "GiveSpeeding": continue;
                        case "GiveGuarding": continue;
                        case "GiveClumsy": continue;
                        case "GiveSlacker": continue;
                    }
                }
                if (!Options.IsActiveSkeld)
                {
                    switch (opt.Value.Name)
                    {
                        case "DisableSkeldDevices": continue;
                        case "SkeldReactorTimeLimit": continue;
                        case "SkeldO2TimeLimit": continue;
                    }
                }
                if (!Options.IsActiveMiraHQ)
                {
                    switch (opt.Value.Name)
                    {
                        case "MiraReactorTimeLimit": continue;
                        case "MiraO2TimeLimit": continue;
                        case "DisableMiraHQDevices": continue;
                    }
                }
                if (!Options.IsActivePolus)
                {
                    switch (opt.Value.Name)
                    {
                        case "DisablePolusDevices": continue;
                        case "PolusReactorTimeLimit": continue;
                    }
                }
                if (!Options.IsActiveAirship)
                {
                    switch (opt.Value.Name)
                    {
                        case "DisableAirshipDevices": continue;
                        case "AirshipReactorTimeLimit": continue;
                        case "AirShipVariableElectrical": continue;
                        case "DisableAirshipMovingPlatform": continue;
                        case "DisableAirshipViewingDeckLightsPanel": continue;
                        case "DisableAirshipCargoLightsPanel": continue;
                        case "DisableAirshipGapRoomLightsPanel": continue;
                    }
                }
                if (!Options.IsActiveFungle)
                {
                    switch (opt.Value.Name)
                    {
                        case "DisableFungleDevices": continue;
                        case "FungleReactorTimeLimit": continue;
                        case "FungleMushroomMixupDuration": continue;
                        case "DisableFungleSporeTrigger": continue;
                        case "CantUseZipLineTotop": continue;
                        case "CantUseZipLineTodown": continue;
                    }
                }

                sb.Append("<line-height=80%><size=70%>");
                if (deep > 0)
                {
                    sb.Append(string.Concat(Enumerable.Repeat(Utils.ColorString(color, "┃"), deep - 1)));
                    sb.Append(Utils.ColorString(color, opt.Index == option.Children.Count ? "┗ " : "┣ "));
                }
                sb.Append($"{opt.Value.GetName()}: {opt.Value.GetString().RemoveSN()}</size></line-height>\n");
                if (opt.Value.GetBool()) ShowChildren(opt.Value, ref sb, color, deep + 1);
            }
        }
        public static bool? Checkenabled(OptionItem opt)
        {
            if (!opt.GetBool())
            {
                switch (opt.Name)
                {
                    case "GiveGuesser": return false;
                    case "GiveWatching": return false;
                    case "GiveManagement": return false;
                    case "GiveSeeing": return false;
                    case "GiveAutopsy": return false;
                    case "GiveTiebreaker": return false;
                    case "GiveMagicHand": return false;
                    case "GivePlusVote": return false;
                    case "GiveRevenger": return false;
                    case "GiveOpener": return false;
                    case "GiveAntiTeleporter": return false;
                    case "GiveLighting": return false;
                    case "GiveMoon": return false;
                    case "GiveElector": return false;
                    case "GiveInfoPoor": return false;
                    case "GiveNonReport": return false;
                    case "GiveTransparent": return false;
                    case "GiveNotvoter": return false;
                    case "GiveWater": return false;
                    case "GiveSpeeding": return false;
                    case "GiveGuarding": return false;
                    case "GiveClumsy": return false;
                    case "GiveSlacker": return false;
                }
            }
            if (!Options.IsActiveSkeld)
            {
                switch (opt.Name)
                {
                    case "DisableSkeldDevices": return null;
                    case "SkeldReactorTimeLimit": return null;
                    case "SkeldO2TimeLimit": return null;
                }
            }
            if (!Options.IsActiveMiraHQ)
            {
                switch (opt.Name)
                {
                    case "MiraReactorTimeLimit": return null;
                    case "MiraO2TimeLimit": return null;
                    case "DisableMiraHQDevices": return null;
                }
            }
            if (!Options.IsActivePolus)
            {
                switch (opt.Name)
                {
                    case "DisablePolusDevices": return null;
                    case "PolusReactorTimeLimit": return null;
                }
            }
            if (!Options.IsActiveAirship)
            {
                switch (opt.Name)
                {
                    case "DisableAirshipDevices": return null;
                    case "AirshipReactorTimeLimit": return null;
                    case "AirShipVariableElectrical": return null;
                    case "DisableAirshipMovingPlatform": return null;
                    case "DisableAirshipViewingDeckLightsPanel": return null;
                    case "DisableAirshipCargoLightsPanel": return null;
                    case "DisableAirshipGapRoomLightsPanel": return null;
                }
            }
            if (!Options.IsActiveFungle)
            {
                switch (opt.Name)
                {
                    case "DisableFungleDevices": return null;
                    case "FungleReactorTimeLimit": return null;
                    case "FungleMushroomMixupDuration": return null;
                    case "DisableFungleSporeTrigger": return null;
                    case "CantUseZipLineTotop": return null;
                    case "CantUseZipLineTodown": return null;
                }
            }
            if (opt.Name == "ResetDoorsEveryTurns" && !(Options.IsActiveFungle || Options.IsActiveAirship || Options.IsActivePolus)) return null;
            if (opt.Name == "ResetDoorsEveryTurns" && !(Options.IsActiveSkeld || Options.IsActiveMiraHQ || Options.IsActiveAirship || Options.IsActivePolus)) return null;
            return true;
        }
    }
}