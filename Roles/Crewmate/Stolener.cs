using System.Collections.Generic;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;

public sealed class Stolener : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Stolener),
            player => new Stolener(player),
            CustomRoles.Stolener,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            10500,
            SetupOptionItem,
            "slt",
            "#605eb7",
            (5, 0),
            from: From.TownOfHost_K
        );
    public Stolener(PlayerControl player)
    : base(
        RoleInfo,
        player)
    {
        Killer = byte.MaxValue;
        CanUseaddon = OptionCanUseaddon.GetBool();
        CanUseAddonfinish = OptionCanUseaddonOnfinish.GetBool();
    }
    enum OptionName
    {
        StolenerCanuseaddon,
        StolenerCanUseaddonOnfinish,
    }
    static OptionItem OptionCanUseaddon; static bool CanUseaddon;
    static OptionItem OptionCanUseaddonOnfinish; static bool CanUseAddonfinish;
    //自身がアドオンを使えるか
    public bool ICanUseaddon => CanUseaddon && (!CanUseAddonfinish || MyTaskState.IsTaskFinished);
    byte Killer;//処理キャンセル用のキラー関数
    public static List<byte> Killers = new();//付与する奴
    static void SetupOptionItem()
    {
        OptionCanUseaddon = BooleanOptionItem.Create(RoleInfo, 10, OptionName.StolenerCanuseaddon, true, false);
        OptionCanUseaddonOnfinish = BooleanOptionItem.Create(RoleInfo, 11, OptionName.StolenerCanUseaddonOnfinish, true, false, OptionCanUseaddon);
        RoleAddAddons.Create(RoleInfo, 20, DefaaultOn: true);
    }
    ///マジシャンとかのキルでも受け渡したい
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost || player.IsAlive() || Killer != byte.MaxValue) return;
        var realkiller = player?.GetRealKiller();

        Killer = realkiller?.PlayerId ?? (byte.MaxValue - 1);
        Killers.Add(Killer);
        Logger.Info($"キラー設定：{realkiller?.Data?.name ?? "無し"}", "Stolener");
        realkiller?.SetKillCooldown(force: true);
        UtilsNotifyRoles.NotifyRoles();

        if (realkiller is not null)
            if (RoleAddAddons.GetRoleAddon(CustomRoles.Stolener, out var d, null, subrole: [CustomRoles.Guarding]))
            {
                if (d.GiveGuarding.GetBool()) realkiller.GetPlayerState().HaveGuard[1] += d.Guard.GetInt();
            }
        UtilsOption.SyncAllSettings();
    }
    public override void CheckWinner(GameOverReason reason)
    {
        if (Killer == byte.MaxValue)
        {
            Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[0]);
            return;
        }
        var role = Killer.GetPlayerControl()?.GetCustomRole() ?? CustomRoles.Crewmate;
        if (role.IsCrewmate() || role.IsMadmate()) return;
        if (role.IsImpostor()) role = CustomRoles.Impostor;
        if (role is CustomRoles.JackalAlien or CustomRoles.JackalMafia) role = CustomRoles.Jackal;
        if (CustomWinnerHolder.winners.Contains((CustomWinner)role)) Achievements.RpcCompleteAchievement(Killer, 0, achievements[1]);
    }
    public static Dictionary<int, Achievement> achievements = new();
    [Attributes.PluginModuleInitializer]
    public static void Load()
    {
        var n1 = new Achievement(RoleInfo, 0, 1, 0, 0);
        var l1 = new Achievement(RoleInfo, 1, 1, 0, 1);
        achievements.Add(0, n1);
        achievements.Add(1, l1);
    }
}