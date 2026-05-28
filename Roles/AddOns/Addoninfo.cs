
using System.Collections.Generic;
using TownOfHost.Roles.Core;

namespace TownOfHost;

class AddondataInfo
{
    public static string GetAddonMark(CustomRoles addon)
    {
        if (CustomRoles.NotAssigned < addon)
        {
            return addon switch
            {
                CustomRoles.Guesser => "∮",
                CustomRoles.Serial => "∂",
                CustomRoles.MagicHand => "ж",
                CustomRoles.Powerful => "∠",
                CustomRoles.Connecting => "Ψ",
                CustomRoles.Watching => "∑",
                CustomRoles.PlusVote => "р",
                CustomRoles.Tiebreaker => "т",
                CustomRoles.Autopsy => "Å",
                CustomRoles.Revenger => "Я",
                CustomRoles.Speeding => "∈",
                CustomRoles.Guarding => "ζ",
                CustomRoles.Management => "θ",
                CustomRoles.Opener => "п",
                CustomRoles.Seeing => "☯",
                CustomRoles.Lighting => "＊",
                CustomRoles.Moon => "э",
                //デバフ
                CustomRoles.SlowStarter => "Ｓs",
                CustomRoles.Notvoter => "Ｖ",
                CustomRoles.Elector => "Ｅ",
                CustomRoles.InfoPoor => "Ｉ",
                CustomRoles.NonReport => "Ｒ",
                CustomRoles.Transparent => "Ｔ",
                CustomRoles.Water => "Ｗ",
                CustomRoles.Clumsy => "Ｃ",
                CustomRoles.Slacker => "ＳＬ",
                CustomRoles.News => "Ｎ",
                CustomRoles.Sunglasses => "Ｓ",
                _ => ""
            };
        }
        return "";
    }

    public static void SetRoleColor()
    {
        Main.roleColors = new Dictionary<CustomRoles, string>()
                {
                    // マッドメイト役職
                    {CustomRoles.SKMadmate, "#ff1919"},
                    //特殊クルー役職
                    //ニュートラル役職
                    {CustomRoles.Emptiness ,"#221d26"},
                    //HideAndSeek
                    {CustomRoles.HASFox, "#e478ff"},
                    {CustomRoles.HASTroll, "#00ff00"},
                    //TaskBattle
                    {CustomRoles.TaskPlayerB, "#9adfff"},
                    // GM
                    {CustomRoles.GM, "#ff5b70"},

                    //属性
                    {CustomRoles.LastImpostor, "#ff1919"},
                    {CustomRoles.LastNeutral,"#cccccc"},
                    {CustomRoles.Workhorse, "#00ffff"},
                    {CustomRoles.Twins, "#80cf48"},
                    {CustomRoles.OneWolf , "#ff1919"},

                    { CustomRoles.Watching, "#800080"},
                    {CustomRoles.Speeding, "#33ccff"},
                    {CustomRoles.Moon,"#ffff33"},
                    {CustomRoles.Guesser,"#999900"},
                    {CustomRoles.Lighting,"#ec6800"},
                    {CustomRoles.Management,"#cee4ae"},
                    {CustomRoles.Connecting,"#96514d"},
                    {CustomRoles.Serial,"#ff1919"},
                    {CustomRoles.PlusVote,"#93ca76"},
                    {CustomRoles.Opener,"#007bbb"},
                    {CustomRoles.Revenger,"#ffcc99"},
                    {CustomRoles.Seeing,"#61b26c"},
                    {CustomRoles.Autopsy,"#80ffdd"},
                    {CustomRoles.Tiebreaker,"#00552e"},
                    {CustomRoles.Guarding, "#7b68ee"},
                    {CustomRoles.MagicHand , "#dea785"},
                    {CustomRoles.Powerful , "#f08e39"},
                    {CustomRoles.Stack , "#b55f47"},
                    //{CustomRoles.AntiTeleporter,"#dea785"},
                    //デバフ
                    { CustomRoles.NonReport,"#006666"},
                    {CustomRoles.Notvoter,"#6c848d"},
                    {CustomRoles.Water,"#003f8e"},
                    {CustomRoles.Clumsy,"#942343"},
                    {CustomRoles.Slacker,"#980098"},
                    {CustomRoles.Elector,"#544a47"},
                    {CustomRoles.Transparent,"#7b7c7d"},
                    {CustomRoles.Amnesia,"#4682b4"},
                    {CustomRoles.SlowStarter,"#ff00ff"},
                    {CustomRoles.InfoPoor ,"#555647"},
                    {CustomRoles.News, "#828282"},
                    {CustomRoles.Sunglasses , "#2e103d"},

                    //第三属性
                    { CustomRoles.Amanojaku,"#005243"},
                    {CustomRoles.Faction,"#c6a768"},
                    {CustomRoles.Lovers, "#ff6be4"},
                    {CustomRoles.RedLovers, "#d70035"},
                    {CustomRoles.YellowLovers, "#fac559"},
                    {CustomRoles.BlueLovers, "#6c9bd2"},
                    {CustomRoles.GreenLovers, "#00885a"},
                    {CustomRoles.WhiteLovers, "#fdede4"},
                    {CustomRoles.PurpleLovers, "#af0082"},
                    {CustomRoles.MadonnaLovers, "#f09199"},
                    {CustomRoles.OneLove , "#ff7961"},

                    // 幽霊役職
                    {CustomRoles.Ghostbuttoner,"#d0af4c"},
                    {CustomRoles.GhostNoiseSender, "#5aa698"},
                    {CustomRoles.GhostReseter , "#a87a71"},
                    {CustomRoles.GhostRumour , "#707cab"},
                    {CustomRoles.GuardianAngel,"#7cc0fc"},
                    {CustomRoles.DemonicTracker,"#824880"},
                    {CustomRoles.DemonicCrusher,"#522886"},
                    {CustomRoles.DemonicSupporter , "#351f1f"},
                    { CustomRoles.DemonicVenter ,"#635963"},
                    {CustomRoles.AsistingAngel,"#8da0b6"},

                    {CustomRoles.NotAssigned, "#ffffff"}
                };
    }
}