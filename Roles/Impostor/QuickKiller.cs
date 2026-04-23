using AmongUs.GameOptions;
using UnityEngine;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;

public sealed class QuickKiller : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(QuickKiller),
            player => new QuickKiller(player),
            CustomRoles.QuickKiller,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            7000,
            SetupOptionItem,
            "qk",
            OptionSort: (7, 5),
            Desc: () => string.Format(GetString("QuickKillerDesc"), OptionAbiltyCanUsePlayercount.GetInt(), OptionQuickKillTimer.GetInt()),
            from: From.TownOfHost_K
        );
    public QuickKiller(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        timer = null;
    }
    static OptionItem OptionKillCoolDown;
    static OptionItem OptionAbiltyCanUsePlayercount;
    static OptionItem OptionQuickKillTimer;

    //クイック可能の時間。null → 未キル
    float? timer;
    enum OptionName
    {
        QuickKillerCanuseplayercount,
        QuickKillerTimer
    }
    public override void ApplyGameOptions(IGameOptions opt) => AURoleOptions.ShapeshifterCooldown = timer.HasValue ? OptionQuickKillTimer.GetFloat() + Main.LagTime : 200;
    public override bool CheckShapeshift(PlayerControl target, ref bool shouldAnimate)
    {
        shouldAnimate = false;
        return false;
    }
    private static void SetupOptionItem()
    {
        OptionKillCoolDown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, OptionBaseCoolTime, 20f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionQuickKillTimer = FloatOptionItem.Create(RoleInfo, 11, OptionName.QuickKillerTimer, new(0.1f, 10f, 0.1f), 3f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionAbiltyCanUsePlayercount = IntegerOptionItem.Create(RoleInfo, 12, OptionName.QuickKillerCanuseplayercount, new(0, 15, 1), 6, false)
            .SetValueFormat(OptionFormat.Players).SetZeroNotation(OptionZeroNotation.Off);
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost || !player.IsAlive() || timer == null) return;
        if (GameStates.IsMeeting) return;

        timer -= Time.fixedDeltaTime;

        if (timer < 0)
        {
            timer = null;
            player.ResetKillCooldown();
            player.SetKillCooldown(force: true);
            player.RpcResetAbilityCooldown();
            quickmodekillcount = 0;
        }
    }
    void IKiller.OnMurderPlayerAsKiller(MurderInfo info)
    {
        //必要人数未満だったらさいなら
        if (OptionAbiltyCanUsePlayercount.GetInt() > PlayerCatch.AllAlivePlayersCount) return;
        var (killer, target) = info.AttemptTuple;
        if (!info.IsCanKilling || info.IsFakeSuicide || info.IsSuicide) return;

        //タイマー進行中なら止める
        Main.AllPlayerKillCooldown[killer.PlayerId] = 0.0001f;

        if (timer.HasValue)
        {
            quickmodekillcount++;
            switch (quickmodekillcount)
            {
                case 1: Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[0]); break;
                case 2: Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[1]); break;
                case 4: Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[2]); break;
            }
            killer.SyncSettings();
            return;
        }
        quickmodekillcount = 0;
        timer = OptionQuickKillTimer.GetFloat();
        killer.SyncSettings();
        killer.RpcResetAbilityCooldown();
    }
    public float CalculateKillCooldown() => OptionKillCoolDown.GetFloat();
    public override void OnStartMeeting() => timer = null;
    public override string GetAbilityButtonText() => GetString("QuickKiller_Timer");
    public override bool CanUseAbilityButton() => timer is not null;
    public override bool OverrideAbilityButton(out string text)
    {
        text = "QuickKiller_Ability";
        return true;
    }
    int quickmodekillcount;
    public static System.Collections.Generic.Dictionary<int, Achievement> achievements = new();
    [Attributes.PluginModuleInitializer]
    public static void Load()
    {
        var n1 = new Achievement(RoleInfo, 0, 1, 0, 0);
        var l1 = new Achievement(RoleInfo, 1, 1, 0, 1);
        var sp1 = new Achievement(RoleInfo, 2, 1, 0, 2, true);
        achievements.Add(0, n1);
        achievements.Add(1, l1);
        achievements.Add(2, sp1);
    }
}