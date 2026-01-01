
using AmongUs.GameOptions;
using Hazel;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using UnityEngine;

namespace TownOfHost.Roles.Impostor;

public sealed class Ballooner : RoleBase, IImpostor, IUsePhantomButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Ballooner),
            player => new Ballooner(player),
            CustomRoles.Ballooner,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Impostor,
            3900,
            SetupOptionItem,
            "Ba",
            OptionSort: (3, 0),
            Desc: () => string.Format(GetString("BalloonerDesc"), OptionChargeWalk.GetFloat(), OptionChargeStep.GetFloat(), OptionMaxBoomDis.GetFloat(),
            OptionAfterMeetingRemoveCharge.GetFloat(), OptionSuicide.GetBool() ? GetString("BalloonerDescSuicide") : "")
        );
    public Ballooner(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        MinBoomDis = OptionMinBoomDis.GetFloat();
        MaxBoomDis = OptionMaxBoomDis.GetFloat();
        chargewakl = OptionChargeWalk.GetFloat();
        chargestep = OptionChargeStep.GetFloat();
        ResetBalloon();
    }
    static OptionItem OptionKillCoolDown;
    static OptionItem OptionAbilityCoolDown;
    static OptionItem OptionMinBoomDis;//最小の爆破範囲
    static float MinBoomDis;
    static OptionItem OptionMaxBoomDis;//最大の爆破範囲
    static float MaxBoomDis;
    static OptionItem OptionChargeWalk;//どれくらい歩けば1たまるか
    static float chargewakl;
    static OptionItem OptionChargeStep;//どれくらいチャージされるか
    static float chargestep;
    static OptionItem OptionAfterMeetingRemoveCharge;//会議後どれだけ空気が抜けるか
    static OptionItem OptionSuicide;//爆破時、自殺するか
    static OptionItem OptionTargetImpostor;//爆破時、味方を巻き込むか

    float NowBoomDis;//現在の爆破範囲 0以下なら使用不可
    float NowWalkCount;//歩数計
    Vector2 OldPosition;//前チェックの位置

    enum OptionName
    {
        BalloonerMinBoomDis, BalloonerMaxBoomDis,
        BalloonerChargeWalk, BalloonerChargeStep,
        BalloonerSuicide, BalloonerAfterMeetingRemoveCharge,
        BalloonerTargetImpostor
    }

    private static void SetupOptionItem()
    {
        OptionKillCoolDown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, OptionBaseCoolTime, 30f, false).SetValueFormat(OptionFormat.Seconds);
        OptionAbilityCoolDown = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.Cooldown, OptionBaseCoolTime, 30f, false).SetValueFormat(OptionFormat.Seconds);
        OptionMinBoomDis = FloatOptionItem.Create(RoleInfo, 12, OptionName.BalloonerMinBoomDis, new(-5, 10, 0.25f), 0, false).SetValueFormat(OptionFormat.Multiplier);
        OptionMaxBoomDis = FloatOptionItem.Create(RoleInfo, 13, OptionName.BalloonerMaxBoomDis, new(0.25f, 15, 0.25f), 3, false).SetValueFormat(OptionFormat.Multiplier);
        OptionChargeWalk = FloatOptionItem.Create(RoleInfo, 14, OptionName.BalloonerChargeWalk, new(1, 300, 1), 60, false);
        OptionChargeStep = FloatOptionItem.Create(RoleInfo, 15, OptionName.BalloonerChargeStep, new(0.1f, 5, 0.05f), 0.5f, false).SetValueFormat(OptionFormat.Multiplier);
        OptionAfterMeetingRemoveCharge = FloatOptionItem.Create(RoleInfo, 16, OptionName.BalloonerAfterMeetingRemoveCharge, new(0, 30, 0.5f), 0.5f, false).SetValueFormat(OptionFormat.Multiplier);
        OptionSuicide = BooleanOptionItem.Create(RoleInfo, 17, OptionName.BalloonerSuicide, false, false);
        OptionTargetImpostor = BooleanOptionItem.Create(RoleInfo, 18, OptionName.BalloonerTargetImpostor, true, false);
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (player.IsAlive() is false) return;

        if (OldPosition == new Vector2(50f, 50f) || MaxBoomDis <= NowBoomDis
        || Player.inVent || Player.MyPhysics.Animations.IsPlayingEnterVentAnimation()
        || Player.MyPhysics.Animations.IsPlayingAnyLadderAnimation() || Player.inMovingPlat
        || !Player.CanMove)//歩行以外は除外
        {
            OldPosition = player.GetTruePosition();
            return;
        }

        var distance = Vector2.Distance(OldPosition, player.GetTruePosition());
        OldPosition = player.GetTruePosition();
        //歩数計の確認
        NowWalkCount += distance;
        if (chargewakl <= NowWalkCount)
        {
            NowWalkCount = 0;
            NowBoomDis = Mathf.Clamp(NowBoomDis + chargestep, MinBoomDis, MaxBoomDis);
            NowBoomDis = Mathf.Round(NowBoomDis * 100) / 100;
            UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: player);
        }
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.PhantomCooldown = OptionAbilityCoolDown.GetFloat();
    }
    public float CalculateKillCooldown() => OptionKillCoolDown.GetFloat();

    public override string GetProgressText(bool comms = false, bool GameLog = false)
        => Utils.ColorString(NowBoomDis <= 0 ? ModColors.Gray : (MaxBoomDis <= NowBoomDis ? ModColors.Red : ModColors.Orange), $" ({NowBoomDis})");

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (isForMeeting) return "";
        string text = "";

        if (MaxBoomDis > NowBoomDis)
            text = GetString("BalloonerLowerText1");
        if (NowBoomDis > 0)
        {
            if (text is not "") text += "\n";
            text += GetString("BalloonerLowerText2");
        }
        if (isForHud is false) text = $"<size=60%>{text}</size>";

        return Utils.ColorString(ModColors.MadMateOrenge, text);
    }
    void ResetBalloon()
    {
        NowBoomDis = OptionMinBoomDis.GetFloat();
        NowWalkCount = 0;
    }

    void IUsePhantomButton.OnClick(ref bool AdjustKillCooldown, ref bool? ResetCooldown)
    {
        if (NowBoomDis <= 0)
        {
            AdjustKillCooldown = true;
            ResetCooldown = false;
            return;
        }
        AdjustKillCooldown = false;
        ResetCooldown = true;

        foreach (var target in PlayerCatch.AllAlivePlayerControls)
        {
            if (target.PlayerId == Player.PlayerId && !OptionSuicide.GetBool()) continue;//自殺が無効なら除外
            if (target.PlayerId != Player.PlayerId && target.GetCustomRole().GetCustomRoleTypes() is CustomRoleTypes.Impostor && !OptionTargetImpostor.GetBool()) continue;

            var dis = Vector2.Distance(target.GetTruePosition(), Player.GetTruePosition());
            if (dis <= NowBoomDis)
            {
                CustomRoleManager.OnCheckMurder(Player, target, target, target, true, false, 2, CustomDeathReason.Bombed);
            }
        }
        if (OptionSuicide.GetBool() is false)
        {
            ResetBalloon();
            SendRPC();
            UtilsNotifyRoles.NotifyRoles();
        }
        Player.SetKillCooldown(delay: true);
    }

    public override void AfterMeetingTasks()
    {
        NowBoomDis = Mathf.Clamp(NowBoomDis - OptionAfterMeetingRemoveCharge.GetFloat(), MinBoomDis, MaxBoomDis);
        NowBoomDis = Mathf.Round(NowBoomDis * 100) / 100;
        UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player);
    }

    public override string GetAbilityButtonText() => GetString("BalloonerAbility");
    public override bool OverrideAbilityButton(out string text)
    {
        text = "Ballooner_Ability";
        return true;
    }

    public void SendRPC()
    {
        using var sender = CreateSender();
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        ResetBalloon();
    }
}
