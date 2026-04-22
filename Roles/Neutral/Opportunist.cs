using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using UnityEngine;

namespace TownOfHost.Roles.Neutral;

public sealed class Opportunist : RoleBase, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Opportunist),
            player => new Opportunist(player),
            CustomRoles.Opportunist,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            15200,
            null,
            "op",
            "#00ff00",
            (6, 2),
            from: From.TOR_GM_Edition
        );
    public Opportunist(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        timer = 0;
        pos = new(0, 0);
    }
    float timer; Vector2 pos;
    public bool CheckWin(ref CustomRoles winnerRole)
    {
        if (Player.IsAlive())
        {
            Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[0]);
            if (PlayerCatch.AllAlivePlayersCount <= 4) Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[1]);
            if (timer > 100) Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[2]);
            if (timer < 10) Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[3]);
            return true;
        }
        return false;
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            var nowpos = player.GetTruePosition();
            if (nowpos == pos)
            {
                timer += Time.fixedDeltaTime;
            }
            pos = nowpos;
        }
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if ((seen == seer) && Player.IsAlive()) return Utils.AdditionalAliveWinnerMark;
        return "";
    }
    public static System.Collections.Generic.Dictionary<int, Achievement> achievements = new();
    [Attributes.PluginModuleInitializer]
    public static void Load()
    {
        var n1 = new Achievement(RoleInfo, 0, 1, 0, 0);
        var l1 = new Achievement(RoleInfo, 1, 1, 0, 1);
        var l2 = new Achievement(RoleInfo, 2, 1, 0, 1, true);
        var sp1 = new Achievement(RoleInfo, 3, 1, 0, 2, true);
        achievements.Add(0, n1);
        achievements.Add(1, l1);
        achievements.Add(2, l2);
        achievements.Add(3, sp1);
    }
}
