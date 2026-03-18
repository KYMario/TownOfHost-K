using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Vanilla;

public sealed class Noisemaker : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForVanilla(
            typeof(Noisemaker),
            player => new Noisemaker(player),
            RoleTypes.Noisemaker,
            SetUpCustomOption,
            "#287031"
            , from: From.AmongUs
        );
    public Noisemaker(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
    public static OptionItem NoisemakerAlertDuration;
    public static OptionItem NoisemakerImpostorAlert;
    public static void SetUpCustomOption()
    {
        NoisemakerAlertDuration = FloatOptionItem.Create(RoleInfo, 353, "NoisemakerAlertDuration", new(0f, 180f, 1f), 15f, false)
        .SetValueFormat(OptionFormat.Seconds);
        NoisemakerImpostorAlert = BooleanOptionItem.Create(RoleInfo, 354, "NoisemakerImpostorAlert", false, false);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.NoisemakerAlertDuration = NoisemakerAlertDuration.GetFloat();
        AURoleOptions.NoisemakerImpostorAlert = NoisemakerImpostorAlert.GetBool();
    }
    public static System.Collections.Generic.Dictionary<int, Achievement> achievements = new();
    [Attributes.PluginModuleInitializer]
    public static void Load()
    {
        var n1 = new Achievement(RoleInfo, 0, 1, 0, 0);
        achievements.Add(0, n1);
    }
}