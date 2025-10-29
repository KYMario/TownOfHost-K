using AmongUs.GameOptions;
using UnityEngine;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using Hazel;

/// <Memo>
/// 転がる設定ONの時、転がりながらひき殺しも考えた。

namespace TownOfHost.Roles.Impostor;

public sealed class ProBowler : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(ProBowler),
            player => new ProBowler(player),
            CustomRoles.ProBowler,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            4200,
            SetupOptionItem,
            "Pb",
            OptionSort: (3, 4),
            Desc: () => string.Format(GetString("ProBowlerDesc"), OptionBowling.GetBool() ? GetString("ProBowlerDescBowl") : GetString("ProBowlerDescTeleport"), OptionDeathReasonIsFall.GetBool() ? GetString("DeathReason.Fall") : GetString("DeathReason.Kill"), OptionMaxUseCount.GetInt())

        );
    public ProBowler(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCooldown = OptionKillCoolDown.GetFloat();
        Bowling = OptionBowling.GetBool();
        AbilityCooldown = OptionAbilityCoolDown.GetFloat();
        MaxUseCount = OptionMaxUseCount.GetInt();
        DeaathreasonIsFall = OptionDeathReasonIsFall.GetBool();

        targetpos = new Vector2(999f, 999f);
        NowKilling = false;
        Rollscount = 0;
        NowUseCount = 0;
        Bowl = null;
        Bowltarget = null;
    }
    static OptionItem OptionKillCoolDown; static float KillCooldown;
    static OptionItem OptionAbilityCoolDown; static float AbilityCooldown;
    static OptionItem OptionMaxUseCount; static int MaxUseCount;
    static OptionItem OptionBowling; static bool Bowling;
    static OptionItem OptionDeathReasonIsFall; static bool DeaathreasonIsFall;
    enum OptionName
    {
        ProBowlerMaxUseCount,
        ProBowlerBowling,
        ProBowlerDeathReasonIsFall
    }
    bool NowKilling;
    Vector2? Bowl;
    int NowUseCount;
    PlayerControl Bowltarget;
    Vector2 BowlTp;
    Vector2 targetpos;
    int Rollscount;

    private static void SetupOptionItem()
    {
        OptionKillCoolDown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, OptionBaseCoolTime, 20f, false).SetValueFormat(OptionFormat.Seconds);
        OptionAbilityCoolDown = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.Cooldown, OptionBaseCoolTime, 25f, false).SetValueFormat(OptionFormat.Seconds);
        OptionMaxUseCount = IntegerOptionItem.Create(RoleInfo, 12, OptionName.ProBowlerMaxUseCount, new(1, 99, 1), 4, false);
        OptionBowling = BooleanOptionItem.Create(RoleInfo, 13, OptionName.ProBowlerBowling, true, false);
        OptionDeathReasonIsFall = BooleanOptionItem.Create(RoleInfo, 14, OptionName.ProBowlerDeathReasonIsFall, false, false);
    }
    public override bool CheckShapeshift(PlayerControl target, ref bool shouldAnimate)
    {
        if (Is(target) || Bowl is not null)
        {
            shouldAnimate = false;
            return true;
        }
        NowUseCount++;
        Bowl = Player.transform.position;
        UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player);
        shouldAnimate = true;
        SendRPC();
        return true;
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterCooldown = MaxUseCount <= NowUseCount ? 200 : AbilityCooldown;
        AURoleOptions.ShapeshifterDuration = 1f;
        AURoleOptions.ShapeshifterLeaveSkin = false;
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;

        // キル中ではない、　キルができる状態でBowlがnullじゃない
        if (!NowKilling && info.IsCanKilling && Bowl != null)
        {
            NowKilling = true;
            info.DoKill = false;
            killer.SetKillCooldown();
            Rollscount = 0;
            Bowltarget = target;
            if (Bowling)
            {
                var tagepos = target.transform.position;
                BowlTp = new Vector2((Bowl.Value.x - tagepos.x) * 0.1f, (Bowl.Value.y - tagepos.y) * 0.1f);
                Bowl = null;
                targetpos = tagepos;
                return;
            }
            target.RpcSnapToForced(Bowl.Value);
            Bowl = null;
            _ = new LateTask(() =>
            {
                NowKilling = false;
                target.SetRealKiller(killer);
                if (DeaathreasonIsFall)
                    PlayerState.GetByPlayerId(target.PlayerId).DeathReason = CustomDeathReason.Fall;
                target.RpcMurderPlayerV2(target);
                RPC.PlaySoundRPC(killer.PlayerId, Sounds.KillSound);
            }, Main.LagTime, "ProBowlerKill", null);
        }
    }
    float timer;
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost || !Bowling || !Bowltarget.IsAlive() || GameStates.CalledMeeting) return;

        timer += Time.fixedDeltaTime;

        if (timer > 0.1)
        {
            timer = 0;
            Rollscount++;

            Bowltarget.RpcSnapToForced(new Vector2(targetpos.x + BowlTp.x * Rollscount, targetpos.y + BowlTp.y * Rollscount));

            if (10 <= Rollscount)
            {
                NowKilling = false;

                if (Bowltarget.IsAlive())
                {
                    Bowltarget.RpcMurderPlayerV2(Bowltarget);
                    if (DeaathreasonIsFall)
                        PlayerState.GetByPlayerId(Bowltarget.PlayerId).DeathReason = CustomDeathReason.Fall;
                }
            }
        }
    }
    public override void OnStartMeeting()
    {
        timer = 0;
        Rollscount = 0;
        NowKilling = false;
        Bowl = null;
        if (Bowltarget.IsAlive())
        {
            if (DeaathreasonIsFall)
                PlayerState.GetByPlayerId(Bowltarget.PlayerId).DeathReason = CustomDeathReason.Fall;
            Bowltarget.RpcMurderPlayerV2(Bowltarget);
        }
    }
    public float CalculateKillCooldown() => KillCooldown;
    public override string GetProgressText(bool comms = false, bool GameLog = false)
        => $"<color=#{(MaxUseCount <= NowUseCount ? "cccccc" : "ff1919")}>({MaxUseCount - NowUseCount})</color>";

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (isForMeeting) return "";

        if (seen.PlayerId == seer.PlayerId && seer.IsAlive() && MaxUseCount > NowUseCount)
        {
            return Bowl == null ? GetString("ProBowlerInfoTextSet") : GetString("ProBowlerInfoTextKill");
        }

        return "";
    }
    public override string GetAbilityButtonText() => GetString("ProBowler_Ability");
    public override bool OverrideAbilityButton(out string text)
    {
        text = "ProBowler_Ability";
        return true;
    }

    public void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(NowUseCount);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        NowUseCount = reader.ReadInt32();
    }
}
