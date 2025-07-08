using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Neutral;

public sealed class Monochromer : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Monochromer),
            player => new Monochromer(player),
            CustomRoles.Monochromer,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            35400,
            SetupOptionItem,
            "Mc",
            "#808080",
            assignInfo: new RoleAssignInfo(CustomRoles.Monochromer, CustomRoleTypes.Neutral)
            {
                AssignCountRule = new(0, 15, 1)
            }
        );
    public Monochromer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CanseeKiller = OpCanseeKiller.GetBool();
        color = OpCanseeRoleColor.GetBool();
    }
    private static OptionItem Kurosiro;
    private static OptionItem HasImpostorVision;
    private static OptionItem OpCanseeKiller;
    private static OptionItem OpCanseeRoleColor;
    bool CanseeKiller;
    bool color;
    enum Option
    {
        MonochromerMonochro,
        MonochromerCanseeKiller,
        MonochromerMarkColor
    }
    private static void SetupOptionItem()
    {
        SoloWinOption.Create(RoleInfo, 4, defo: 1);
        Kurosiro = BooleanOptionItem.Create(RoleInfo, 5, Option.MonochromerMonochro, false, false);
        HasImpostorVision = BooleanOptionItem.Create(RoleInfo, 6, GeneralOption.ImpostorVision, false, false);
        OpCanseeKiller = BooleanOptionItem.Create(RoleInfo, 7, Option.MonochromerCanseeKiller, true, false);
        OpCanseeRoleColor = BooleanOptionItem.Create(RoleInfo, 8, Option.MonochromerMarkColor, false, false, OpCanseeKiller);
    }
    public override bool NotifyRolesCheckOtherName => true;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpostorVision.GetBool());
    public override bool GetTemporaryName(ref string name, ref bool NoMarker, PlayerControl seer, PlayerControl seen = null)
    {
        seen ??= seer;
        if (GameStates.Meeting || !GameStates.IsInTask) return false;
        if (seer == seen) return false;
        if (!Is(seer)) return false;
        if (!Player.IsAlive()) return false;
        name = "<size=0></size>";
        NoMarker = false;
        return true;
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        if (Options.firstturnmeeting && MeetingStates.FirstMeeting) return "";
        if (!CanseeKiller || GameStates.Meeting) return "";
        if (seer.Is(CustomRoles.Monochromer) &&
        (seen.GetCustomRole().IsImpostor() || seen.IsNeutralKiller() || seen.Is(CustomRoles.WolfBoy) || seen.Is(CustomRoles.Sheriff) || seen.Is(CustomRoles.GrimReaper)))
        {
            var c = seen.GetRoleColor();
            if (seen.Is(CustomRoles.WolfBoy))
            {
                c = UtilsRoleText.GetRoleColor(CustomRoles.Impostor);
            }
            return Utils.ColorString(color ? c : Palette.DisabledGrey, "★");
        }
        else return "";
    }
    public override void Colorchnge()
    {
        if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;
        if (!Player.IsAlive()) return;
        foreach (var pc in PlayerCatch.AllPlayerControls)
        {
            if (pc == Player) continue;
            if (pc == null) continue;
            if (pc.Is(CustomRoles.UltraStar)) continue;
            var id = Camouflage.PlayerSkins[pc.PlayerId].ColorId;
            if (Kurosiro.GetBool())
            {
                if (id is 0 or 1 or 2 or 6 or 8 or 9 or 12 or 15 or 16)
                {
                    pc.RpcChColor(Player, 6, true);
                }
                else pc.RpcChColor(Player, 7, true);
            }
            else
                pc.RpcChColor(Player, 15, true);
        }
        UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player);
    }
    public override void OnReportDeadBody(PlayerControl _, NetworkedPlayerInfo __)
    {
        foreach (var pc in PlayerCatch.AllPlayerControls)
        {
            var id = Camouflage.PlayerSkins[pc.PlayerId].ColorId;
            pc.SetColor(id);
            Camouflage.RpcSetSkin(pc, RevertToDefault: true, kyousei: true);
        }
    }
    public static bool CheckWin(GameOverReason reason)
    {
        foreach (var pc in PlayerCatch.AllAlivePlayerControls)
        {
            if (pc.Is(CustomRoles.Monochromer))
            {
                if (pc.IsAlive())
                {
                    return Win(pc, reason);
                }
            }
        }
        return false;
    }
    private static bool Win(PlayerControl pc, GameOverReason reason)
    {
        if (reason == GameOverReason.CrewmatesByTask)
        {
            CustomWinnerHolder.WinnerIds.Add(pc.PlayerId);
            CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.Monochromer);
            return false;
        }
        CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Monochromer, pc.PlayerId, true);
        return true;
    }

    public override void ChengeRoleAdd() => Colorchnge();
}