using System.Linq;
using System.Text;
using UnityEngine;

namespace TownOfHost.Roles.Core.Descriptions;

public abstract class RoleDescription
{
    public RoleDescription(SimpleRoleInfo roleInfo)
    {
        RoleInfo = roleInfo;
    }

    public SimpleRoleInfo RoleInfo { get; }
    /// <summary>イントロなどで表示される短い文</summary>
    public abstract string Blurb { get; }
    /// <summary>
    /// ヘルプコマンドで使用される長い説明文<br/>
    /// AmongUs2023.7.12時点で，Impostor, Crewmateに関してはバニラ側でロング説明文が未実装のため「タスクを行う」と表示される
    /// </summary>
    public abstract string Description { get; }
    public string FullFormatHelp
    {
        get
        {
            var builder = new StringBuilder(256);
            //役職とイントロ
            builder.AppendFormat("<{2}><b><line-height=1.3pic><size={0}>{1}\n\n", FirstHeaderSize, Translator.GetRoleString(RoleInfo.RoleName.ToString()), UtilsRoleText.GetRoleColorCode(RoleInfo.RoleName));
            builder.AppendFormat("<size={0}>{1}</b></color>\n", InfoSize, Blurb);
            // 陣営
            builder.AppendFormat("<size={0}>{1}:", "65%", Translator.GetString("Team"));
            var roleTeam = RoleInfo.CustomRoleType == CustomRoleTypes.Madmate ? CustomRoleTypes.Impostor : RoleInfo.CustomRoleType;
            builder.AppendFormat("{0}    ", Translator.GetString($"CustomRoleTypes.{roleTeam}"));

            //カウント
            var count = RoleInfo.CountType;
            var overrideRoleText = CustomRoles.NotAssigned;
            var countText = Translator.GetString(count.ToString());
            switch (count)
            {
                case CountTypes.Impostor: overrideRoleText = CustomRoles.Impostor; break;
                case CountTypes.Jackal: overrideRoleText = CustomRoles.Jackal; break;
                case CountTypes.Fox: overrideRoleText = CustomRoles.Fox; break;
                case CountTypes.GrimReaper: overrideRoleText = CustomRoles.GrimReaper; break;
                case CountTypes.Remotekiller: overrideRoleText = CustomRoles.Remotekiller; break;
                case CountTypes.MilkyWay: countText = Neutral.Vega.TeamText; break;
                default: overrideRoleText = CustomRoles.Crewmate; break;
            }

            if (overrideRoleText != CustomRoles.NotAssigned) countText = Translator.GetString(overrideRoleText.ToString());

            builder.Append($"{Translator.GetString("Count")}:{countText}    ");

            // バニラ置き換え役職
            builder.Append(Translator.GetString("Basis"));
            builder.AppendFormat(":{0}\n", Translator.GetString(RoleInfo.BaseRoleType.Invoke().ToString()));
            //From
            if (RoleInfo.From != From.None) builder.AppendFormat("{0}\n", UtilsOption.GetFrom(RoleInfo).RemoveSizeTags());

            //説明
            builder.AppendFormat("<size={0}>\n", BlankLineSize);
            builder.AppendFormat("<size={0}>{1}\n", BodySize, Description);
            //設定
            var sb = new StringBuilder();
            if (Options.CustomRoleSpawnChances.TryGetValue(RoleInfo.RoleName, out var op)) UtilsShowOption.ShowChildrenSettings(op, ref sb);
            else if (RoleInfo.RoleName is CustomRoles.Braid) UtilsShowOption.ShowChildrenSettings(Options.CustomRoleSpawnChances[CustomRoles.Driver], ref sb);
            else if (RoleInfo.RoleName is CustomRoles.Altair) UtilsShowOption.ShowChildrenSettings(Options.CustomRoleSpawnChances[CustomRoles.Vega], ref sb);
            else if (RoleInfo.RoleName is CustomRoles.Nue) UtilsShowOption.ShowChildrenSettings(Options.CustomRoleSpawnChances[CustomRoles.Nue], ref sb);
            if (RoleInfo.CustomRoleType == CustomRoleTypes.Madmate)
            {
                string rule = "┣ ";
                string ruleFooter = "┗ ";
                sb.Append($"{Options.MadMateOption.GetName()}: {Options.MadMateOption.GetString().Color(Palette.ImpostorRed)}\n");
                if (Options.MadMateOption.GetBool())
                {
                    sb.Append($"{rule}{Options.MadmateCanFixLightsOut.GetName()}: {Options.MadmateCanFixLightsOut.GetTextString()}\n");
                    sb.Append($"{rule}{Options.MadmateCanFixComms.GetName()}: {Options.MadmateCanFixComms.GetTextString()}\n");
                    sb.Append($"{rule}{Options.MadmateHasLighting.GetName()}: {Options.MadmateHasLighting.GetTextString()}\n");
                    sb.Append($"{rule}{Options.MadmateHasMoon.GetName()}: {Options.MadmateHasMoon.GetTextString()}\n");
                    if (Options.MadmateCanSeeKillFlash.GetBool())
                        sb.Append($"{rule}{Options.MadmateCanSeeKillFlash.GetName()}: {Options.MadmateCanSeeKillFlash.GetTextString()}\n");
                    if (Options.MadmateCanSeeOtherVotes.GetBool())
                        sb.Append($"{rule}{Options.MadmateCanSeeOtherVotes.GetName()}: {Options.MadmateCanSeeOtherVotes.GetTextString()}\n");
                    if (Options.MadmateCanSeeDeathReason.GetBool())
                        sb.Append($"{rule}{Options.MadmateCanSeeDeathReason.GetName()}: {Options.MadmateCanSeeDeathReason.GetTextString()}\n");
                    if (Options.MadmateRevengePlayer.GetBool())
                    {
                        sb.Append($"{rule}{Options.MadmateRevengePlayer.GetName()}: {Options.MadmateRevengePlayer.GetTextString()}\n");
                        sb.Append($"┃ {rule}{Options.MadmateRevengeCanImpostor.GetName()}: {Options.MadmateRevengeCanImpostor.GetTextString()}\n");
                        sb.Append($"┃ {rule}{Options.MadmateRevengeMadmate.GetName()}: {Options.MadmateRevengeMadmate.GetTextString()}\n");
                        sb.Append($"┃ {rule}{Options.MadmateRevengeCrewmate.GetName()}: {Options.MadmateRevengeCrewmate.GetTextString()}\n");
                        sb.Append($"┃ {ruleFooter}{Options.MadmateRevengeNeutral.GetName()}: {Options.MadmateRevengeNeutral.GetTextString()}\n");
                    }
                    if (Options.MadCanSeeImpostor.GetBool())
                        sb.Append($"{rule}{Options.MadCanSeeImpostor.GetName()}: {Options.MadCanSeeImpostor.GetTextString()}\n");
                    sb.Append($"{rule}{Options.MadmateVentCooldown.GetName()}: {Options.MadmateVentCooldown.GetTextString()}\n");
                    sb.Append($"{rule}{Options.MadmateVentMaxTime.GetName()}: {Options.MadmateVentMaxTime.GetTextString()}\n");
                    sb.Append($"{rule}{Options.MadmateCanMovedByVent.GetName()}: {Options.MadmateCanMovedByVent.GetTextString()}\n");
                    sb.Append($"{ruleFooter}{Options.MadmateTell.GetName()}: {Options.MadmateTell.GetTextString()}\n");
                }
            }
            var temp = TemplateManager.GetTemplate($"{RoleInfo.RoleName}");
            if (temp != "")
            {
                builder.Append($"\n{temp}");
            }
            if (sb.ToString() != "")
            {
                builder.AppendFormat("<line-height=0.9pic><size={0}>\n{1}", "45%", sb.ToString().RemoveColorTags());
            }
            return builder.ToString();
        }
    }
    public string WikiText
    {
        get
        {
            var builder = new StringBuilder(256);
            //役職とイントロ
            builder.Append("# ").Append(Translator.GetRoleString(RoleInfo.RoleName.ToString()));
            var roleTeam = RoleInfo.CustomRoleType == CustomRoleTypes.Madmate ? CustomRoleTypes.Impostor : RoleInfo.CustomRoleType;

            if (roleTeam is CustomRoleTypes.Neutral)
            {
                builder.Append($"\n陣営　　：{Translator.GetString($"CustomRoleTypes.{roleTeam}")}<br>\n");
                builder.Append($"判定　　：<br>\n");
                builder.Append($"カウント：<br>\n");
            }
            else
            {
                builder.Append($"\n陣営：{Translator.GetString($"CustomRoleTypes.{roleTeam}")}<br>\n");
                builder.Append($"判定：<br>\n");
            }
            builder.Append($"\n\n\n");
            builder.Append($"## 参考/移植元\n").Append("[]()より<br>\n");
            builder.Append($"\n## 役職概要\n{Description.Changebr(true)}<br>\n");
            builder.Append($"\n## 能力\n ()<br>\n◎→ワンクリ\n△→キル\n★→常時発動\n◇→ベント\n※→自投票\n▽→タスク\n▶→その他のアビリティ\n");

            //設定
            var sb = new StringBuilder();
            if (Options.CustomRoleSpawnChances.TryGetValue(RoleInfo.RoleName, out var op))
                wikiOption(op, ref sb);

            if (sb.ToString().RemoveHtmlTags() is not null and not "")
            {
                builder.Append($"\n## 設定\n").Append("|設定名|(設定値 / デフォルト値)|説明|\n").Append("|-----|----------------------|----|\n");
                builder.Append($"{sb.ToString().RemoveHtmlTags()}\n");
            }

            builder.Append($"\n## 補足説明/仕様\n");
            builder.Append($"\n## 勝利条件\n");

            return builder.ToString().RemoveColorTags();
        }
    }
    public string WikiOpt
    {
        get
        {
            var builder = new StringBuilder(256);
            var sb = new StringBuilder();
            if (Options.CustomRoleSpawnChances.TryGetValue(RoleInfo.RoleName, out var op))
                wikiOption(op, ref sb);

            if (sb.ToString().RemoveHtmlTags() is not null and not "")
            {
                builder.Append($"\n## 設定\n").Append("|設定名|(設定値 / デフォルト値)|説明|\n").Append("|-----|----------------------|----|\n");
                builder.Append($"{sb.ToString().RemoveHtmlTags()}\n");
            }

            return builder.ToString().RemoveColorTags();
        }
    }

    public const string FirstHeaderSize = "150%";
    public const string InfoSize = "90%";
    public const string SecondHeaderSize = "80%";
    public const string SecondSize = "70%";
    public const string BodySize = "57%";
    public const string BlankLineSize = "30%";

    public static void wikiOption(OptionItem option, ref StringBuilder sb, int deep = 0)
    {
        foreach (var opt in option.Children.Select((v, i) => new { Value = v, Index = i + 1 }))
        {
            switch (opt.Value.Name)
            {
                case "Maximum": continue;
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

            sb.Append("|");
            if (deep > 0)
            {
                sb.Append(string.Concat(Enumerable.Repeat("┃", Mathf.Max(deep - 1, 0))));
                sb.Append(opt.Index == option.Children.Count ? "┗ " : "┣ ");
            }
            var val = "";
            var tani = "";
            if (opt.Value is BooleanOptionItem boolean)
            {
                val = $"On/Off ({(opt.Value.GetBool() ? "On" : "Off")})";
            }
            if (opt.Value is IntegerOptionItem integer)
            {
                val = $"{integer.Rule.MinValue}/{integer.Rule.MaxValue}/{integer.Rule.Step} ({opt.Value.GetInt()})";
            }
            if (opt.Value is FloatOptionItem floatOption)
            {
                var i = opt.Value.ZeroNotation;
                var min = $"{floatOption.Rule.MinValue}";
                var step = $"{floatOption.Rule.Step}";
                if (i is not OptionZeroNotation.None)
                {
                    if (min == "0") min = i == OptionZeroNotation.Infinity ? "∞" : "ー";
                    if (step == "0") step = i == OptionZeroNotation.Infinity ? "∞" : "ー";
                }

                val = $"{min}/{floatOption.Rule.MaxValue}/{step} ({opt.Value.GetFloat()})";
            }
            if (opt.Value is StringOptionItem stringOption)
            {
                val = $" ({opt.Value.GetString()})";
            }
            if (opt.Value.ValueFormat is not OptionFormat.None)
            {
                //かっこの色がおかしくなるけどご愛敬。とると判定が狂って削除エラー吐く
                tani = $"({string.Format(Translator.GetString("Format." + opt.Value.ValueFormat), "")})";
            }

            sb.Append($"{opt.Value.GetName(true)}{tani}|{val}||\n");
            if (opt.Value is not null) wikiOption(opt.Value, ref sb, deep + 1);
        }
    }
}
