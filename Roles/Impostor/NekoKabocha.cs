using AmongUs.GameOptions;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;

public sealed class NekoKabocha : RoleBase, IImpostor, INekomata
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(NekoKabocha),
            player => new NekoKabocha(player),
            CustomRoles.NekoKabocha,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            6300,
            SetupOptionItems,
            "nk",
            OptionSort: (6, 7),
            introSound: () => PlayerControl.LocalPlayer.KillSfx,
            from: From.TOR_GM_Edition
        );
    public NekoKabocha(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        impostorsGetRevenged = optionImpostorsGetRevenged.GetBool();
        madmatesGetRevenged = optionMadmatesGetRevenged.GetBool();
        NeutralsGetRevenged = optionNeutralsGetRevenged.GetBool();
        revengeOnExile = optionRevengeOnExile.GetBool();

        IsDead = false;
    }

    #region カスタムオプション
    /// <summary>インポスターに仕返し/道連れするかどうか</summary>
    private static BooleanOptionItem optionImpostorsGetRevenged;
    /// <summary>マッドに仕返し/道連れするかどうか</summary>
    private static BooleanOptionItem optionMadmatesGetRevenged;
    /// <summary>ニュートラルに仕返し/道連れするかどうか</summary>
    private static BooleanOptionItem optionNeutralsGetRevenged;
    private static BooleanOptionItem optionRevengeOnExile;
    private static void SetupOptionItems()
    {
        optionImpostorsGetRevenged = BooleanOptionItem.Create(RoleInfo, 10, OptionName.NekoKabochaImpostorsGetRevenged, false, false);
        optionMadmatesGetRevenged = BooleanOptionItem.Create(RoleInfo, 20, OptionName.NekoKabochaMadmatesGetRevenged, false, false);
        optionNeutralsGetRevenged = BooleanOptionItem.Create(RoleInfo, 30, OptionName.NekoKabochaNeutralsGetRevenged, false, false);
        optionRevengeOnExile = BooleanOptionItem.Create(RoleInfo, 40, OptionName.NekoKabochaRevengeOnExile, false, false);
    }
    private enum OptionName { NekoKabochaImpostorsGetRevenged, NekoKabochaMadmatesGetRevenged, NekoKabochaNeutralsGetRevenged, NekoKabochaRevengeOnExile, }
    #endregion

    private static bool impostorsGetRevenged;
    private static bool madmatesGetRevenged;
    private static bool NeutralsGetRevenged;
    private static bool revengeOnExile;
    private static readonly LogHandler logger = Logger.Handler(nameof(NekoKabocha));
    bool IsDead;

    public override void OnMurderPlayerAsTarget(MurderInfo info)
    {
        // 普通のキルじゃない．もしくはキルを行わない時はreturn
        if (GameStates.IsMeeting || info.IsAccident || info.IsSuicide || !info.CanKill || !info.DoKill || IsDead)
        {
            return;
        }
        // 殺してきた人を殺し返す
        logger.Info("ネコカボチャの仕返し");
        var killer = info.AttemptKiller;
        if (!GameStates.CalledMeeting && MyState.DeathReason is CustomDeathReason.Revenge) return;
        IsDead = true;
        if (!IsCandidate(killer))
        {
            logger.Info("キラーは仕返し対象ではないので仕返しされません");
            return;
        }
        CustomRoleManager.OnCheckMurder(Player, killer, Player, killer, true, false, deathReason: CustomDeathReason.Revenge);
    }
    public bool DoRevenge(CustomDeathReason deathReason) => revengeOnExile && deathReason == CustomDeathReason.Vote;
    public bool IsCandidate(PlayerControl player)
    {
        return player.GetCustomRole().GetCustomRoleTypes() switch
        {
            CustomRoleTypes.Impostor => impostorsGetRevenged,
            CustomRoleTypes.Madmate => madmatesGetRevenged,
            CustomRoleTypes.Neutral => NeutralsGetRevenged,
            _ => true,
        };
    }
}