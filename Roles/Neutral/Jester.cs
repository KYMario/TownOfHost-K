using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Neutral;

public sealed class Jester : RoleBase, IKiller
{
    //Memo
    //エンジニア置き換えのベントをいつかする。
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Jester),
            player => new Jester(player),
            CustomRoles.Jester,
            () => CanUseShape.GetBool() ? RoleTypes.Shapeshifter : (CanUseVent.GetBool() ? RoleTypes.Impostor : RoleTypes.Crewmate),
            CustomRoleTypes.Neutral,
            14200,
            SetupOptionItem,
            "je",
            "#ec62a5",
            (4, 0),
            true,
            assignInfo: new RoleAssignInfo(CustomRoles.Jester, CustomRoleTypes.Neutral)
            {
                AssignCountRule = new(0, 15, 1)
            },
            from: From.Jester
        );
    public Jester(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
    }
    static OptionItem CanUseShape;
    static OptionItem CanUseVent;
    static OptionItem Cooldown;
    static OptionItem Duration;
    static OptionItem CanVentMove;
    enum Option
    {
        JesterCanUseShapeshift, MadmateCanMovedByVent
    }
    private static void SetupOptionItem()
    {
        SoloWinOption.Create(RoleInfo, 8, defo: 1);
        CanUseShape = BooleanOptionItem.Create(RoleInfo, 3, Option.JesterCanUseShapeshift, false, false);
        Cooldown = FloatOptionItem.Create(RoleInfo, 4, GeneralOption.Cooldown, new(0f, 180f, 0.5f), 30f, false, CanUseShape).SetValueFormat(OptionFormat.Seconds);
        Duration = FloatOptionItem.Create(RoleInfo, 5, GeneralOption.Duration, new(0f, 180f, 0.5f), 5f, false, CanUseShape).SetZeroNotation(OptionZeroNotation.Infinity).SetValueFormat(OptionFormat.Seconds);
        CanUseVent = BooleanOptionItem.Create(RoleInfo, 6, GeneralOption.CanVent, false, false);
        CanVentMove = BooleanOptionItem.Create(RoleInfo, 7, Option.MadmateCanMovedByVent, false, false, CanUseVent);
    }
    public bool CanUseImpostorVentButton() => CanUseVent.GetBool();
    public override bool CanUseAbilityButton() => CanUseShape.GetBool();
    public bool CanUseSabotageButton() => false;
    public override bool OnInvokeSabotage(SystemTypes systemType) => false;
    public bool CanKill { get; private set; } = false;
    public bool CanUseKillButton() => false;
    float IKiller.CalculateKillCooldown() => 0f;
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterCooldown = Cooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = Duration.GetFloat();
        AURoleOptions.EngineerCooldown = 0f;
        AURoleOptions.EngineerInVentMaxTime = 0f;
        opt.SetVision(false);
    }
    public override bool CanVentMoving(PlayerPhysics physics, int ventId) => CanVentMove.GetBool();
    public override void OnExileWrapUp(NetworkedPlayerInfo exiled, ref bool DecidedWinner)
    {
        if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;
        if (!AmongUsClient.Instance.AmHost || Player.PlayerId != exiled.PlayerId) return;

        if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Jester, Player.PlayerId))
        {
            CustomWinnerHolder.NeutralWinnerIds.Add(Player.PlayerId);
        }
        Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[0]);
        if (10 <= UtilsGameLog.LastLogRole.Count && PlayerCatch.AllAlivePlayersCount <= 3)
            DecidedWinner = true;
    }
    public static System.Collections.Generic.Dictionary<int, Achievement> achievements = new();
    [Attributes.PluginModuleInitializer]
    public static void Load()
    {
        var n1 = new Achievement(RoleInfo, 0, 1, 0, 0);
        var sp1 = new Achievement(RoleInfo, 1, 1, 0, 2, true);
        achievements.Add(0, n1);
        achievements.Add(1, sp1);
    }
}