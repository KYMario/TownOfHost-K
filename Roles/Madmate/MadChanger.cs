using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Madmate;

public sealed class MadChanger : RoleBase, IKiller, IKillFlashSeeable, IDeathReasonSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(MadChanger),
            player => new MadChanger(player),
            CustomRoles.MadChanger,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Madmate,
            7900,
            SetupOptionItem,
            "Mc",
            OptionSort: (2, 3),
            isDesyncImpostor: true,
            introSound: () => GetIntroSound(RoleTypes.Crewmate),
            from: From.None
        );
    public MadChanger(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillTarget = null;
    }
    static OptionItem CanUseVent;
    static OptionItem PlayAnimation;
    static OptionItem KillCoolDown;
    static OptionItem AbilityCoolDown;
    enum OptionName
    {
        MadChangerKillTargetCooldown
    }
    PlayerControl KillTarget;
    public bool? CheckKillFlash(MurderInfo info) => Options.MadmateCanSeeKillFlash.GetBool();
    public bool? CheckSeeDeathReason(PlayerControl seen) => Options.MadmateCanSeeDeathReason.GetBool();
    public override CustomRoles TellResults(PlayerControl player) => Options.MadTellOpt();
    private static void SetupOptionItem()
    {
        CanUseVent = BooleanOptionItem.Create(RoleInfo, 10, GeneralOption.CanVent, true, false);
        PlayAnimation = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.PlayShapeAnimate, true, false);
        KillCoolDown = FloatOptionItem.Create(RoleInfo, 12, OptionName.MadChangerKillTargetCooldown, new(0f, 180f, 0.5f), 10f, false).SetValueFormat(OptionFormat.Seconds);
        AbilityCoolDown = FloatOptionItem.Create(RoleInfo, 13, GeneralOption.Cooldown, new(0f, 180f, 0.5f), 15f, false).SetValueFormat(OptionFormat.Seconds);
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (Is(killer))
        {
            KillTarget = target;
            Player.SetKillCooldown();
            Player.RpcResetAbilityCooldown();
            info.DoKill = false;
        }
    }

    public override bool CheckShapeshift(PlayerControl target, ref bool animate)
    {
        if (Player.shapeshifting) return false;
        if (Is(target))
        {
            animate = false;
            return PlayAnimation.GetBool();
        }
        //死亡 or Null or KillTargetなら自身が対象
        var pc = KillTarget;
        if (pc == null || !pc.IsAlive() || pc == target) pc = Player;
        if (!target.IsAlive()) target = Player;//ターゲットが死んでるなら

        //ここまできたらリセット入れる。
        KillTarget = null;
        Player.SetKillCooldown();
        if (!PlayAnimation.GetBool()) Player.RpcResetAbilityCooldown();

        if (target.inVent || target.MyPhysics.Animations.IsPlayingEnterVentAnimation() || pc.inVent || pc.MyPhysics.Animations.IsPlayingEnterVentAnimation()
        || target.MyPhysics.Animations.IsPlayingAnyLadderAnimation() || pc.MyPhysics.Animations.IsPlayingAnyLadderAnimation()
        || target.inMovingPlat || pc.inMovingPlat
        ) return PlayAnimation.GetBool();//ベント等テレポしたらだめなやーつならここで止める。

        var Ptf = pc.transform.position;
        var Ttf = target.transform.position;

        pc.RpcSnapToForced(Ttf);
        target.RpcSnapToForced(Ptf);

        animate = PlayAnimation.GetBool();
        return PlayAnimation.GetBool();
    }
    public bool CanUseImpostorVentButton() => CanUseVent.GetBool();
    public bool CanUseSabotageButton() => false;
    public float CalculateKillCooldown() => KillCoolDown.GetFloat();
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterCooldown = AbilityCoolDown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 1f;
    }
    public bool OverrideKillButtonText(out string text)
    {
        text = GetString("MadChanger_Targetset");
        return true;
    }
    bool IKiller.OverrideKillButton(out string text)
    {
        text = "MadChanger_Kill";
        return true;
    }
    public override string GetAbilityButtonText() => GetString("MadChanger_Change");
    public override bool OverrideAbilityButton(out string text)
    {
        text = "MadChanger_Ability";
        return true;
    }
}