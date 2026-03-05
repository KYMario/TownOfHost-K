using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;

public sealed class ToiletFan : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(ToiletFan),
            player => new ToiletFan(player),
            CustomRoles.ToiletFan,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            12500,
            SetupOptionItem,
            "to",
            "#5f5573",
            (9, 3),
            introSound: () => GetIntroSound(RoleTypes.Crewmate),
            from: From.SuperNewRoles
        );
    public ToiletFan(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Cooldown = OptionCooldown.GetFloat();
        flug = 0;
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = Cooldown;
        AURoleOptions.EngineerInVentMaxTime = 1;
    }
    private static OptionItem OptionCooldown;
    enum OptionName
    {
        Cooldown
    }
    private static float Cooldown;
    int flug;
    private static void SetupOptionItem()
    {
        OptionCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.Cooldown, new(0f, 10f, 1f), 5f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        flug = Main.NormalOptions.MapId is 4 ? 1 : 2;
        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, 79);
        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, 80);
        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, 81);
        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, 82);
        return false;
    }
    public override string GetAbilityButtonText() => GetString("ToiletFanAbility");
    public override bool OverrideAbilityButton(out string text)
    {
        text = "ToiletFan_Ability";
        return true;
    }
    public override void CheckWinner(GameOverReason reason)
    {
        switch (flug)
        {
            case 1:
                Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[0]);
                break;
            case 2:
                Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[1]);
                break;
            default: break;
        }
    }
    public static System.Collections.Generic.Dictionary<int, Achievement> achievements = new();
    [Attributes.PluginModuleInitializer]
    public static void Load()
    {
        var n1 = new Achievement(RoleInfo, 0, 1, 0, 0);
        var n2 = new Achievement(RoleInfo, 1, 1, 0, 0);
        achievements.Add(0, n1);
        achievements.Add(1, n2);
    }
}