using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Vanilla;

public sealed class Viper : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForVanilla(
            typeof(Viper),
            player => new Viper(player),
            RoleTypes.Viper,
            SetUpCustomOption
            , from: From.AmongUs
        );
    public Viper(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        ViperDissolveTime = OptViperDissolveTime.GetFloat();
        killcool = OptKillcool.GetFloat();
    }
    static OptionItem OptKillcool; static float killcool;
    public static OptionItem OptViperDissolveTime; public static float ViperDissolveTime;
    public static void SetUpCustomOption()
    {
        OptKillcool = FloatOptionItem.Create(RoleInfo, 4, GeneralOption.KillCooldown, OptionBaseCoolTime, 30, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptViperDissolveTime = FloatOptionItem.Create(RoleInfo, 3, StringNames.ViperDissolveTime, new(0, 180, 1), 15, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ViperDissolveTime = ViperDissolveTime;
    }
    float IKiller.CalculateKillCooldown() => killcool;
    bool IKiller.OverrideKillButtonText(out string text)
    {
        text = GetString(StringNames.ViperAbility);
        return true;
    }
}