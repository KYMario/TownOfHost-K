using TownOfHost.Roles.AddOns.Common;
using TownOfHost.Roles.AddOns.Crewmate;
using TownOfHost.Roles.AddOns.Impostor;
using TownOfHost.Roles.AddOns.Neutral;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Ghost;

namespace TownOfHost;

class GhostRoleCore
{
    [Attributes.GameModuleInitializer]
    public static void Init()
    {
        AsistingAngel.Init();
        DemonicCrusher.Init();
        DemonicTracker.Init();
        DemonicVenter.Init();
        DemonicSupporter.Init();
        Ghostbuttoner.Init();
        GhostNoiseSender.Init();
        GhostReseter.Init();
        GhostRumour.Init();
        GuardianAngel.Init();

        //アドオンもここ置かせて( ᐛ )
        LastImpostor.Init();
        LastNeutral.Init();
        Watching.Init();
        Serial.Init();
        Management.Init();
        Speeding.Init();
        Guarding.Init();
        Connecting.Init();
        Opener.Init();
        //AntiTeleporter.Init();
        Moon.Init();
        Tiebreaker.Init();
        MagicHand.Init();
        Amnesia.Init();
        Lighting.Init();
        Seeing.Init();
        Revenger.Init();
        Amanojaku.Init();
        Guesser.Init();
        Autopsy.Init();
        Workhorse.Init();
        NonReport.Init();
        Notvoter.Init();
        PlusVote.Init();
        Elector.Init();
        News.Init();
        InfoPoor.Init();
        Water.Init();
        SlowStarter.Init();
        Slacker.Init();
        Transparent.Init();
        Clumsy.Init();
        OneWolf.Init();
        Twins.Init();
        Stack.Init();
    }
    public static void SetupCustomOptionAddonAndIsGhostRole()
    {
        // Add-Ons
        Amanojaku.SetupCustomOption();
        LastImpostor.SetupCustomOption();
        LastNeutral.SetupCustomOption();
        Workhorse.SetupCustomOption();

        Stack.SetupCustomOption();
        ObjectOptionitem.Create(1_000_116, "Buff-Addon", true, null, TabGroup.Addons)
            .SetOptionName(() => "Buff Add-on").SetColor(UtilsRoleText.GetRoleColor(CustomRoles.Moon)).SetTag(CustomOptionTags.Role);
        //バフ(ゲッサー→特定陣営→会議効果→タスクターン)
        Guesser.SetupCustomOption();
        Serial.SetupCustomOption();
        MagicHand.SetupCustomOption();
        Connecting.SetupCustomOption();
        Watching.SetupCustomOption();
        PlusVote.SetupCustomOption();
        Tiebreaker.SetupCustomOption();
        Autopsy.SetupCustomOption();
        Revenger.SetupCustomOption();
        Speeding.SetupCustomOption();
        Guarding.SetupCustomOption();
        Management.SetupCustomOption();
        Seeing.SetupCustomOption();
        Opener.SetupCustomOption();
        //AntiTeleporter.SetupCustomOption();
        Lighting.SetupCustomOption();
        Moon.SetupCustomOption();
        //デバフ達
        ObjectOptionitem.Create(1_000_117, "Debuff-Addon", true, null, TabGroup.Addons)
            .SetOptionName(() => "Debuff Add-on").SetColor(UtilsRoleText.GetRoleColor(CustomRoles.SlowStarter)).SetTag(CustomOptionTags.Role);
        Amnesia.SetupCustomOption();
        News.SetupCustomOption();
        OneWolf.SetupCustomOption();
        SlowStarter.SetupCustomOption();
        Notvoter.SetupCustomOption();
        Elector.SetupCustomOption();
        InfoPoor.SetupCustomOption();
        NonReport.SetupCustomOption();
        Transparent.SetupCustomOption();
        Water.SetupCustomOption();
        Clumsy.SetupCustomOption();
        Slacker.SetupCustomOption();
        //ゆーれーやくしょく
        ObjectOptionitem.Create(1_000_118, "Madmate", true, null, TabGroup.GhostRoles)
            .SetOptionName(() => "Mad Ghost").SetColor(ModColors.MadMateOrenge).SetTag(CustomOptionTags.Role);
        DemonicTracker.SetupCustomOption();
        DemonicCrusher.SetupCustomOption();
        DemonicVenter.SetupCustomOption();
        DemonicSupporter.SetupCustomOption();
        ObjectOptionitem.Create(1_000_119, "Madmate", true, null, TabGroup.GhostRoles)
            .SetOptionName(() => "Neutral Ghost").SetColor(ModColors.NeutralGray).SetTag(CustomOptionTags.Role);
        AsistingAngel.SetupCustomOption();
        ObjectOptionitem.Create(1_000_120, "Madmate", true, null, TabGroup.GhostRoles)
            .SetOptionName(() => "Crew Ghost").SetColor(ModColors.CrewMateBlue).SetTag(CustomOptionTags.Role);
        Ghostbuttoner.SetupCustomOption();
        GhostNoiseSender.SetupCustomOption();
        GhostReseter.SetupCustomOption();
        GhostRumour.SetupCustomOption();
        GuardianAngel.SetupCustomOption();
    }
}