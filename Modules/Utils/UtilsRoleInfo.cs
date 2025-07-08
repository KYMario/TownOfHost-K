using static TownOfHost.Utils;
using static TownOfHost.Translator;
using static TownOfHost.ChatCommands;
using static TownOfHost.UtilsRoleText;
using static TownOfHost.UtilsShowOption;
using System.Collections.Generic;
using TownOfHost.Roles.Core;
using System.Linq;
using UnityEngine;
using System.Text.RegularExpressions;
using System;

namespace TownOfHost
{
    class UtilsRoleInfo
    {
        public static string LastL = "N";
        public static void GetRolesInfo(string role, byte player = 255)
        {
            if ((Options.HideGameSettings.GetBool() || (Options.HideSettingsDuringGame.GetBool() && GameStates.IsInGame)) && player != byte.MaxValue)
            {
                SendMessage(GetString("Message.HideGameSettings"), player);
                return;
            }
            // 初回のみ処理
            if (roleCommands == null)
            {
#pragma warning disable IDE0028  // Dictionary初期化の簡素化をしない
                roleCommands = new Dictionary<CustomRoles, string>();

                // GM
                roleCommands.Add(CustomRoles.GM, "gm");

                // Impostor役職
                roleCommands.Add((CustomRoles)(-1), $"【=== {GetString("Impostor")} ===】");  // 区切り用
                roleCommands.Add(CustomRoles.Shapeshifter, "She");
                //roleCommands.Add(CustomRoles.Phantom, "Pha");//アプデ対応用 仮
                ConcatCommands(CustomRoleTypes.Impostor);

                // Madmate役職
                roleCommands.Add((CustomRoles)(-2), $"【=== {GetString("Madmate")} ===】");  // 区切り用
                ConcatCommands(CustomRoleTypes.Madmate);
                roleCommands.Add(CustomRoles.SKMadmate, "sm");

                // Crewmate役職
                roleCommands.Add((CustomRoles)(-3), $"【=== {GetString("Crewmate")} ===】");  // 区切り用
                roleCommands.Add(CustomRoles.Engineer, "Eng");
                roleCommands.Add(CustomRoles.Scientist, "Sci");
                roleCommands.Add(CustomRoles.Tracker, "Trac");
                roleCommands.Add(CustomRoles.Noisemaker, "Nem");//アプデ対応用 仮

                ConcatCommands(CustomRoleTypes.Crewmate);

                // Neutral役職
                roleCommands.Add((CustomRoles)(-4), $"【=== {GetString("Neutral")} ===】");  // 区切り用
                ConcatCommands(CustomRoleTypes.Neutral);
                // 属性
                roleCommands.Add((CustomRoles)(-5), $"【=== {GetString("Addons")} ===】");  // 区切り用
                                                                                          //ラスト
                roleCommands.Add(CustomRoles.Workhorse, "wh");
                roleCommands.Add(CustomRoles.LastNeutral, "ln");
                roleCommands.Add(CustomRoles.LastImpostor, "li");
                //バフ
                roleCommands.Add(CustomRoles.watching, "wat");
                roleCommands.Add(CustomRoles.Speeding, "sd");
                roleCommands.Add(CustomRoles.Guarding, "gi");
                roleCommands.Add(CustomRoles.Guesser, "Gr");
                roleCommands.Add(CustomRoles.Moon, "Mo");
                roleCommands.Add(CustomRoles.Lighting, "Li");
                roleCommands.Add(CustomRoles.Management, "Dr");
                roleCommands.Add(CustomRoles.Connecting, "Cn");
                roleCommands.Add(CustomRoles.Serial, "Se");
                roleCommands.Add(CustomRoles.PlusVote, "Pv");
                roleCommands.Add(CustomRoles.Opener, "Oe");
                //roleCommands.Add(CustomRoles.AntiTeleporter, "At");
                roleCommands.Add(CustomRoles.Revenger, "Re");
                roleCommands.Add(CustomRoles.seeing, "Se");
                roleCommands.Add(CustomRoles.Autopsy, "Au");
                roleCommands.Add(CustomRoles.Tiebreaker, "tb");
                roleCommands.Add(CustomRoles.MagicHand, "MaH");

                //デバフ
                roleCommands.Add(CustomRoles.SlowStarter, "sl");
                roleCommands.Add(CustomRoles.NonReport, "Nr");
                roleCommands.Add(CustomRoles.Notvoter, "nv");
                roleCommands.Add(CustomRoles.Water, "wt");
                roleCommands.Add(CustomRoles.Transparent, "tr");
                roleCommands.Add(CustomRoles.Slacker, "sl");
                roleCommands.Add(CustomRoles.Clumsy, "lb");
                roleCommands.Add(CustomRoles.Elector, "El");
                roleCommands.Add(CustomRoles.Amnesia, "am");
                roleCommands.Add(CustomRoles.InfoPoor, "IP");
                //第三
                roleCommands.Add(CustomRoles.Lovers, "lo");
                roleCommands.Add(CustomRoles.OneLove, "ol");
                roleCommands.Add(CustomRoles.MadonnaLovers, "Ml");
                roleCommands.Add(CustomRoles.Amanojaku, "Ama");

                roleCommands.Add((CustomRoles)(-7), $"== {GetString("GhostRole")} ==");  // 区切り用
                                                                                         //幽霊
                roleCommands.Add(CustomRoles.Ghostbuttoner, "Bbu");
                roleCommands.Add(CustomRoles.GhostNoiseSender, "NiS");
                roleCommands.Add(CustomRoles.GhostReseter, "Res");
                roleCommands.Add(CustomRoles.GhostRumour, "Rum");
                roleCommands.Add(CustomRoles.GuardianAngel, "Gan");
                roleCommands.Add(CustomRoles.DemonicCrusher, "DCr");
                roleCommands.Add(CustomRoles.DemonicTracker, "DTr");
                roleCommands.Add(CustomRoles.DemonicVenter, "Dve");
                roleCommands.Add(CustomRoles.AsistingAngel, "AsA");

                // HAS
                roleCommands.Add((CustomRoles)(-6), $"== {GetString("HideAndSeek")} ==");  // 区切り用
                roleCommands.Add(CustomRoles.HASFox, "hfo");
                roleCommands.Add(CustomRoles.HASTroll, "htr");
                LastL = "N";
#pragma warning restore IDE0028
            }

            var msg = "";
            var rolemsg = $"{GetString("Command.h_args")}";
            switch (role)
            {
                case "i":
                case "I":
                case "imp":
                case "インポスター":
                case "impostor":
                case "impostors":
                case "インポス":
                    rolemsg = $"{GetString("h_r_impostor").Color(Palette.ImpostorRed)}</u><size=50%>";
                    foreach (var im in roleCommands.Where(r => r.Key.IsImpostor()))
                    {
                        if (im.Key.IsE() || !Event.CheckRole(im.Key) || im.Key is CustomRoles.Assassin) continue;
                        rolemsg += $"\n{GetString($"{im.Key}")}({im.Value})";
                    }
                    if (player == byte.MaxValue) player = 0;
                    SendMessage(rolemsg, player);
                    return;
                case "c":
                case "C":
                case "crew":
                case "crewmate":
                case "クルー":
                case "クルーメイト":
                    rolemsg = $"{GetString("h_r_crew").Color(Color.blue)}</u><size=50%>";
                    foreach (var im in roleCommands.Where(r => r.Key.IsCrewmate()))
                    {
                        if (im.Key.IsE() || !Event.CheckRole(im.Key) || im.Key is CustomRoles.Assassin) continue;
                        rolemsg += $"\n{GetString($"{im.Key}")}({im.Value})";
                    }
                    if (player == byte.MaxValue) player = 0;
                    SendMessage(rolemsg, player);
                    return;
                case "n":
                case "N":
                case "Neu":
                case "Neutral":
                case "第三":
                case "第三陣営":
                case "ニュートラル":
                    rolemsg = $"{GetString("h_r_Neutral").Color(Palette.DisabledGrey)}</u><size=50%>";
                    foreach (var im in roleCommands.Where(r => r.Key.IsNeutral()))
                    {
                        if (im.Key.IsE() || !Event.CheckRole(im.Key)) continue;
                        rolemsg += $"\n{GetString($"{im.Key}")}({im.Value})";
                    }
                    if (player == byte.MaxValue) player = 0;
                    SendMessage(rolemsg, player);
                    return;
                case "m":
                case "Mad":
                case "マッド":
                case "狂人":
                case "M":
                    rolemsg = $"{GetString("h_r_MadMate").Color(ModColors.MadMateOrenge)}</u><size=50%>";
                    foreach (var im in roleCommands.Where(r => r.Key.IsMadmate()))
                    {
                        if (im.Key.IsE() || !Event.CheckRole(im.Key)) continue;
                        rolemsg += $"\n{GetString($"{im.Key}")}({im.Value})";
                    }
                    if (player == byte.MaxValue) player = 0;
                    SendMessage(rolemsg, player);
                    return;
                case "a":
                case "A":
                case "Addon":
                case "アドオン":
                case "属性":
                case "モディフィア":
                case "重複役職":
                    rolemsg = $"{GetString("h_r_Addon").Color(ModColors.AddonsColor)}</u><size=50%>";
                    foreach (var im in roleCommands.Where(r => r.Key.IsAddOn() || r.Key is CustomRoles.Amanojaku))
                    {
                        if (im.Key.IsE() || !Event.CheckRole(im.Key)) continue;
                        rolemsg += $"\n{GetString($"{im.Key}")}({im.Value})";
                    }
                    if (player == byte.MaxValue) player = 0;
                    SendMessage(rolemsg, player);
                    return;
                case "g":
                case "G":
                case "Ghost":
                case "幽霊":
                case "幽霊役職":
                    rolemsg = $"{GetString("h_r_GhostRole").Color(ModColors.GhostRoleColor)}</u><size=50%>";
                    foreach (var im in roleCommands.Where(r => r.Key.IsGhostRole()))
                    {
                        if (im.Key.IsE() || !Event.CheckRole(im.Key)) continue;
                        rolemsg += $"\n{GetString($"{im.Key}")}({im.Value})";
                    }
                    if (player == byte.MaxValue) player = 0;
                    SendMessage(rolemsg, player);
                    return;
            }
            foreach (var r in roleCommands)
            {
                var roleName = r.Key.ToString();
                var roleShort = r.Value;

                if (string.Compare(role, roleName, true) == 0 || string.Compare(role, roleShort, true) == 0)
                {
                    if (r.Key is CustomRoles.Assassin or CustomRoles.Merlin)
                    {
                        goto infosend;
                    }
                    if ((r.Key.IsE() && !Event.Special) || !Event.CheckRole(r.Key)) goto infosend;
                    var roleInfo = r.Key.GetRoleInfo();
                    if (roleInfo != null && roleInfo.Description != null)
                    {
                        SendMessage(roleInfo.Description.FullFormatHelp, sendTo: player);
                    }
                    // RoleInfoがない役職は従来の処理
                    else
                    {
                        if (r.Key.IsAddOn() || r.Key.IsRiaju() || r.Key == CustomRoles.Amanojaku || r.Key.IsGhostRole()) SendMessage(GetAddonsHelp(r.Key), sendTo: player);
                        else SendMessage(ColorString(GetRoleColor(r.Key), "<b><line-height=2.0pic><size=150%>" + GetString(roleName) + "\n<line-height=1.8pic><size=90%>" + GetString($"{roleName}Info")) + "\n<line-height=1.3pic></b><size=60%>\n" + GetString($"{roleName}InfoLong"), sendTo: player);
                    }
                    return;
                }
            }

            if (GetRoleByInputName(role, out var hr, true))
            {
                if (hr is CustomRoles.Assassin or CustomRoles.Merlin)
                {
                    goto infosend;
                }
                if (hr is CustomRoles.Crewmate or CustomRoles.Impostor) SendMessage(msg, player);
                var roleInfo = hr.GetRoleInfo();
                if (roleInfo != null && roleInfo.Description != null)
                {
                    SendMessage(roleInfo.Description.FullFormatHelp, sendTo: player);
                }
                // RoleInfoがない役職は従来の処理
                else
                {
                    if (hr.IsAddOn() || hr.IsRiaju() || hr == CustomRoles.Amanojaku || hr.IsGhostRole()) SendMessage(GetAddonsHelp(hr), sendTo: player);
                    else SendMessage(ColorString(GetRoleColor(hr), "<b><line-height=2.0pic><size=150%>" + GetString($"{hr}") + "\n<line-height=1.8pic><size=90%>" + GetString($"{hr}Info")) + "\n<line-height=1.3pic></b><size=60%>\n" + GetString($"{hr}InfoLong"), sendTo: player);
                }
                return;
            }
            goto infosend;

        infosend:
            msg += rolemsg;
            if (player == byte.MaxValue) player = 0;
            SendMessage(msg, player);
        }
        /// <summary>
        /// 複数登録するor特別な奴以外はしなくてよい。
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string FixRoleNameInput(string text)
        {
            return text.RemoveHtmlTags() switch
            {
                "GM" or "gm" or "ゲームマスター" => GetString("GM"),

                //インポスター
                "ボマー" or "爆弾魔" => GetString("Bomber"),
                "大狼" or "たいろう" or "大老" => GetString("Tairou"),
                "吸血鬼" or "ヴァンパイア" => GetString("Vampire"),
                "魔女" or "ウィッチ" => GetString("Witch"),
                "プロボウラー" or "プロボーラー" => GetString("ProBowler"),
                "一途な狼" or "一途" or "いちず" => GetString("EarnestWolf"),

                //マッドメイト
                "サイドキックマッドメイト" => GetString("SKMadmate"),

                //クルーメイト
                "ぽんこつ占い師" or "ポンコツ占い師" => GetString("PonkotuTeller"),
                "エンジニア" => GetString("Engineer"),
                "科学者" => GetString("Scientist"),
                "トラッカー" => GetString("Tracker"),
                "ノイズメーカー" => GetString("Noisemaker"),
                "巫女" or "みこ" or "ふじょ" => GetString("ShrineMaiden"),
                "クルー" or "クルーメイト" => GetString("Crewmate"),
                "狼少年" or "オオカミ少年" or "おおかみ少年" => GetString("WolfBoy"),
                "ギャスプ" or "ギャプス" or "ギャスフ" => GetString("Gasp"),//これを許していいのか。

                //第3陣営
                "ラバーズ" or "リア充" or "恋人" => GetString("Lovers"),
                "片思い" or "片想い" => GetString("OneLove"),
                "シュレディンガーの猫" or "シュレ猫" => GetString("SchrodingerCat"),
                "ジャッカルドール" => GetString("Jackaldoll"),
                "きつね" or "ようこ" or "妖狐" => GetString("Fox"),
                "ヴァルチャー" or "バルチャー" => GetString("Vulture"),

                "ドライバーとブレイド" => GetString("Driver"),

                "織姫と彦星" => GetString("Vega"),
                _ => text,
            };
        }
        public static bool GetRoleByInputName(string input, out CustomRoles output, bool includeVanilla = false)
        {
            output = new();
            input = Regex.Replace(input, @"[0-9]+", string.Empty);
            input = Regex.Replace(input, @"\s", string.Empty);
            input = Regex.Replace(input, @"[\x01-\x1F,\x7F]", string.Empty);
            input = input.ToLower().Trim().Replace("是", string.Empty);
            if (input == "" || input == string.Empty) return false;
            input = FixRoleNameInput(input).ToLower();
            foreach (CustomRoles role in Enum.GetValues(typeof(CustomRoles)))
            {
                if (!includeVanilla && role.IsVanilla() && role != CustomRoles.GuardianAngel) continue;
                if (input == GuessManager.ChangeNormal2Vanilla(role))
                {
                    output = role;
                    return true;
                }
            }
            return false;
        }
        private static void ConcatCommands(CustomRoleTypes roleType)
        {
            var roles = CustomRoleManager.AllRolesInfo.Values.Where(role => role.CustomRoleType == roleType);
            foreach (var role in roles)
            {
                if (role.ChatCommand is null) continue;
                roleCommands[role.RoleName] = role.ChatCommand;
            }
        }
    }
}