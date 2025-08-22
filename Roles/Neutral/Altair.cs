using AmongUs.GameOptions;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Neutral;

public sealed class Altair : RoleBase, IKiller, ISchrodingerCatOwner, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Altair),
            player => new Altair(player),
            CustomRoles.Altair,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            22600,
            null,
            "alt",
            "#b0c4de",
            (3, 1),
            true,
            countType: CountTypes.MilkyWay,
            combination: CombinationRoles.VegaandAltair
        );
    public Altair(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    { }

    public Vega Vega = null;

    public float CalculateKillCooldown() => Vega.KillCooldown;
    public bool CanUseImpostorVentButton() => Vega.AltairCanUseVent;
    public bool CanUseSabotageButton() => false;

    public ISchrodingerCatOwner.TeamType SchrodingerCatChangeTo => ISchrodingerCatOwner.TeamType.MilkyWay;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(Vega.HasImpostorVision);

    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        if (!Is(seer) || Vega == null) return "";

        if (seen == Vega.Player)
            return $"<color={Vega.TeamColor}>☆</color>";

        if (Options.firstturnmeeting && MeetingStates.FirstMeeting) return "";
        if (!Vega.CanSeeKiller || GameStates.CalledMeeting) return "";

        if (seen.GetCustomRole().IsImpostor() || seen.IsNeutralKiller() || seen.Is(CustomRoles.WolfBoy) || seen.Is(CustomRoles.Sheriff) || seen.Is(CustomRoles.GrimReaper))
        {
            var impostorColor = UtilsRoleText.GetRoleColor(CustomRoles.Impostor);
            var color = seen.Is(CustomRoles.WolfBoy) ? impostorColor : seen.GetRoleColor();
            return Utils.ColorString(Vega.FactionBasedStarColor ? color : Palette.DisabledGrey, "★");
        }
        else return "";
    }

    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        //なにしようとしてるの?だめだよ?
        if (info.AttemptTarget == Vega?.Player) info.DoKill = false;
    }

    public void SetVega(Vega vega) => Vega = vega;

    public bool CheckWin(ref CustomRoles winnerRole)
    {
        if (Vega?.Player == null || !Vega.AddWin) return false;

        return Check(Vega.Player.PlayerId, CustomRoles.Vega);

        bool Check(byte playerId, CustomRoles role)
            => CustomWinnerHolder.WinnerIds.Contains(playerId) || CustomWinnerHolder.WinnerRoles.Contains(role);
    }

}