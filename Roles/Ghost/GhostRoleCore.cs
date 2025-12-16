using TownOfHost.Roles.AddOns.Common;
using TownOfHost.Roles.AddOns.Crewmate;
using TownOfHost.Roles.AddOns.Impostor;
using TownOfHost.Roles.AddOns.Neutral;
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
        //バフ(ゲッサー→特定陣営→会議効果→タスクターン)
        Guesser.SetupCustomOption();
        OneWolf.SetupCustomOption();
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
        Amnesia.SetupCustomOption();
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
        DemonicTracker.SetupCustomOption();
        DemonicCrusher.SetupCustomOption();
        DemonicVenter.SetupCustomOption();
        DemonicSupporter.SetupCustomOption();
        AsistingAngel.SetupCustomOption();
        Ghostbuttoner.SetupCustomOption();
        GhostNoiseSender.SetupCustomOption();
        GhostReseter.SetupCustomOption();
        GhostRumour.SetupCustomOption();
        GuardianAngel.SetupCustomOption();
    }
}