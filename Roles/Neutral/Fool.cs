using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Roles.Neutral.Nue;

namespace TownOfHost.Roles.Neutral;
//設定、こんびのしょりへん
public sealed class Fool : RoleBase, IKiller, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Fool),
            player => new Fool(player),
            CustomRoles.Fool,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            23400,
            null,
            "Fool",
            "#8b6d61",
            (3, 0),
            true,
            tab: TabGroup.Combinations,
            assignInfo: new RoleAssignInfo(CustomRoles.Fool, CustomRoleTypes.Neutral)
            {
                AssignUnitRoles = [CustomRoles.Fool, CustomRoles.Nue]
            },
            combination: CombinationRoles.FoolandNue
        );
    public Fool(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        IsKillerKilled = false;

        canvent = OptionCanVent.GetBool();
        hasimpostorvision = OptionHasImpostorVision.GetBool();
        killcool = OptionKillcooldown.GetFloat();
        Isresultimposotr = OptionIsTellResultImposotr.GetBool();
        winalive = OptionWinAlive.GetBool();
    }
    //愚か者の設定
    static OptionItem OptionCanVent; static bool canvent;
    static OptionItem OptionHasImpostorVision; static bool hasimpostorvision;
    static OptionItem OptionKillcooldown; static float killcool;
    static OptionItem OptionIsTellResultImposotr; static bool Isresultimposotr;
    static OptionItem OptionWinAlive; static bool winalive;
    bool IsKillerKilled;
    enum OptionName
    {
        FoolIsTellResultImpostor,
        FoolAlivetoWin
    }
    public static void FoolSetupOptionItem(SimpleRoleInfo info)
    {
        OptionKillcooldown = FloatOptionItem.Create(info, 10, GeneralOption.KillCooldown, OptionBaseCoolTime, 32.5f, false).SetInfo(UtilsRoleText.GetRoleColorAndtext(CustomRoles.Fool)).SetValueFormat(OptionFormat.Seconds);
        OptionCanVent = BooleanOptionItem.Create(info, 11, GeneralOption.CanVent, true, false).SetInfo(UtilsRoleText.GetRoleColorAndtext(CustomRoles.Fool));
        OptionHasImpostorVision = BooleanOptionItem.Create(info, 12, GeneralOption.ImpostorVision, true, false).SetInfo(UtilsRoleText.GetRoleColorAndtext(CustomRoles.Fool));
        OptionIsTellResultImposotr = BooleanOptionItem.Create(info, 13, OptionName.FoolIsTellResultImpostor, true, false).SetInfo(UtilsRoleText.GetRoleColorAndtext(CustomRoles.Fool));
        OptionWinAlive = BooleanOptionItem.Create(info, 14, OptionName.FoolAlivetoWin, false, false).SetInfo(UtilsRoleText.GetRoleColorAndtext(CustomRoles.Fool));
    }
    float IKiller.CalculateKillCooldown() => killcool;
    bool IKiller.CanUseSabotageButton() => false;
    bool IKiller.CanUseImpostorVentButton() => canvent;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(hasimpostorvision);
    public override CustomRoles TellResults(PlayerControl player) => Isresultimposotr ? CustomRoles.Impostor : CustomRoles.NotAssigned;


    void IKiller.OnMurderPlayerAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (Is(killer) && (target.IsNeutralKiller() || target.GetCustomRole().IsImpostor()))
        {
            IsKillerKilled = true;
            Logger.Info($"{target.Data.GetLogPlayerName()}はニュートラルキラー", "Fool");
        }
    }
    public bool CheckWin(ref CustomRoles winnerRole)
    {
        if (CustomWinnerHolder.WinnerTeam is CustomWinner.Crewmate)
        {
            return (Player.IsAlive() && winalive) || IsKillerKilled;
        }
        return false;
    }
}