using System.Collections.Generic;
using System.Linq;
using Hazel;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.Neutral;
using static TownOfHost.Translator;
using TownOfHost.Roles.Crewmate;

namespace TownOfHost.Roles.Impostor;
public sealed class BountyHunter : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(BountyHunter),
            player => new BountyHunter(player),
            CustomRoles.BountyHunter,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            8100,
            SetupOptionItem,
            "bo",
            from: From.TheOtherRoles
        );
    public BountyHunter(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        TargetChangeTime = OptionTargetChangeTime.GetFloat();
        SuccessKillCooldown = OptionSuccessKillCooldown.GetFloat();
        FailureKillCooldown = OptionFailureKillCooldown.GetFloat();
        ShowTargetArrow = OptionShowTargetArrow.GetBool();

        ChangeTimer = OptionTargetChangeTime.GetFloat();
    }

    private static OptionItem OptionTargetChangeTime;
    private static OptionItem OptionSuccessKillCooldown;
    private static OptionItem OptionFailureKillCooldown;
    private static OptionItem OptionShowTargetArrow;
    enum OptionName
    {
        BountyTargetChangeTime,
        BountySuccessKillCooldown,
        BountyFailureKillCooldown,
        BountyShowTargetArrow,
    }

    private static float TargetChangeTime;
    private static float SuccessKillCooldown;
    private static float FailureKillCooldown;
    private static bool ShowTargetArrow;

    public bool CanBeLastImpostor { get; } = false;
    public PlayerControl Target;
    public float ChangeTimer;

    private static void SetupOptionItem()
    {
        OptionTargetChangeTime = FloatOptionItem.Create(RoleInfo, 10, OptionName.BountyTargetChangeTime, new(5f, 900f, 1f), 60f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionSuccessKillCooldown = FloatOptionItem.Create(RoleInfo, 11, OptionName.BountySuccessKillCooldown, new(0f, 180f, 0.5f), 2.5f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionFailureKillCooldown = FloatOptionItem.Create(RoleInfo, 12, OptionName.BountyFailureKillCooldown, new(0f, 180f, 0.5f), 50f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionShowTargetArrow = BooleanOptionItem.Create(RoleInfo, 13, OptionName.BountyShowTargetArrow, false, false);
    }
    public override void Add()
    {
        if (AmongUsClient.Instance.AmHost)
            ResetTarget();
    }
    private void SendRPC(byte targetId)
    {
        using var sender = CreateSender();
        sender.Writer.Write(targetId);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        byte targetId = reader.ReadByte();

        Target = PlayerCatch.GetPlayerById(targetId);
        if (ShowTargetArrow) TargetArrow.Add(Player.PlayerId, targetId);
        Logger.Info($"{Player.GetNameWithRole().RemoveHtmlTags()}のターゲットを{Target.GetNameWithRole().RemoveHtmlTags()}に変更", "BountyHunter");
    }
    //public static void SetKillCooldown(byte id, float amount) => Main.AllPlayerKillCooldown[id] = amount;
    public override void ApplyGameOptions(IGameOptions opt) => AURoleOptions.ShapeshifterCooldown = TargetChangeTime;

    public void OnCheckMurderDontKill(MurderInfo info)
    {
        if (!info.IsSuicide)
        {
            (var killer, var target) = info.AttemptTuple;

            if (GetTarget() == target)
            {//ターゲットをキルした場合
                Logger.Info($"{killer?.Data?.PlayerName}:ターゲットをキル", "BountyHunter");
                Main.AllPlayerKillCooldown[killer.PlayerId] = SuccessKillCooldown;
                killer.SyncSettings();//キルクール処理を同期
                ResetTarget();
            }
            else
            {
                Logger.Info($"{killer?.Data?.PlayerName}:ターゲット以外をキル", "BountyHunter");
                Main.AllPlayerKillCooldown[killer.PlayerId] = FailureKillCooldown;
                killer.SyncSettings();//キルクール処理を同期
            }
        }
        return;
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        if (!info.IsSuicide)
        {
            (var killer, var target) = info.AttemptTuple;

            if (GetTarget() == target)
            {//ターゲットをキルした場合
                Logger.Info($"{killer?.Data?.PlayerName}:ターゲットをキル", "BountyHunter");
                Main.AllPlayerKillCooldown[killer.PlayerId] = SuccessKillCooldown;
                killer.SyncSettings();//キルクール処理を同期
                ResetTarget();
            }
            else
            {
                Logger.Info($"{killer?.Data?.PlayerName}:ターゲット以外をキル", "BountyHunter");
                Main.AllPlayerKillCooldown[killer.PlayerId] = FailureKillCooldown;
                killer.SyncSettings();//キルクール処理を同期
            }
        }
        return;
    }

    public override void OnFixedUpdate(PlayerControl player)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            if (Player.IsAlive())
            {
                var targetId = GetTarget().PlayerId;
                if (ChangeTimer >= TargetChangeTime)//時間経過でターゲットをリセットする処理
                {
                    ResetTarget();//ターゲットの選びなおし
                    UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player);
                }
                if (ChangeTimer >= 0)
                    ChangeTimer += Time.fixedDeltaTime;

                //BountyHunterのターゲット更新
                if (PlayerState.GetByPlayerId(targetId).IsDead)
                {
                    ResetTarget();
                    Logger.Info($"{Player.GetNameWithRole().RemoveHtmlTags()}のターゲットが無効だったため、ターゲットを更新しました", "BountyHunter");
                    UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player);
                }
            }
        }
    }
    public PlayerControl GetTarget()
    {
        if (Target == null)
            Target = ResetTarget();

        return Target;
    }
    public PlayerControl ResetTarget()
    {
        if (!AmongUsClient.Instance.AmHost) return null;

        var playerId = Player.PlayerId;

        ChangeTimer = 0f;

        Logger.Info($"{Player.GetNameWithRole().RemoveHtmlTags()}:ターゲットリセット", "BountyHunter");
        Player.RpcResetAbilityCooldown(); ;//タイマー（変身クールダウン）のリセットと

        var cTargets = new List<PlayerControl>(PlayerCatch.AllAlivePlayerControls.Where(pc => !pc.Is(CountTypes.Impostor)));

        if (cTargets.Count >= 2)
            cTargets.RemoveAll(x => x == Target); //前回のターゲットは除外

        if (cTargets.Count <= 0)
        {
            Logger.Warn("ターゲットの指定に失敗しました:ターゲット候補が存在しません", "BountyHunter");
            return null;
        }

        var rand = IRandom.Instance;
        var target = cTargets[rand.Next(0, cTargets.Count)];
        var targetId = target.PlayerId;
        Target = target;
        if (ShowTargetArrow) TargetArrow.Add(playerId, targetId);
        Logger.Info($"{Player.GetNameWithRole().RemoveHtmlTags()}のターゲットを{Target.GetNameWithRole().RemoveHtmlTags()}に変更", "BountyHunter");

        //RPCによる同期
        SendRPC(targetId);
        return target;
    }
    public override string GetAbilityButtonText() => GetString("BountyHunterChangeButtonText");
    public override bool OverrideAbilityButton(out string text)
    {
        text = "BountyHunter_Ability";
        return true;
    }
    public override void AfterMeetingTasks()
    {
        if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;
        if (Player.IsAlive())
        {
            Player.RpcResetAbilityCooldown();
            ChangeTimer = 0f;
        }
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (GameStates.Meeting) return "";
        //seenが省略の場合seer
        seen ??= seer;
        //seeおよびseenが自分である場合以外は関係なし
        if (!Is(seer) || !Is(seen)) return "";

        var target = GetTarget();
        return target != null ? $"{(isForHud ? GetString("BountyCurrentTarget") : "Target")}:{Utils.GetPlayerColor(target.PlayerId)}" : "";
    }
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        //seeおよびseenが自分である場合以外は関係なし
        if (!Is(seer) || !Is(seen)) return "";

        if (!ShowTargetArrow || isForMeeting) return "";

        var target = GetTarget();
        if (target == null) return "";
        //seerがtarget自身でBountyHunterのとき、
        //矢印オプションがありミーティング以外で矢印表示
        return TargetArrow.GetArrows(Player, target.PlayerId);
    }
    public void OnSchrodingerCatKill(SchrodingerCat schrodingerCat)
    {
        if (GetTarget() == schrodingerCat.Player)
        {
            ResetTarget();  // ターゲットの選びなおし
        }
    }
    public void OnBakeCatKill(BakeCat bake)
    {
        if (GetTarget() == bake.Player)
        {
            ResetTarget();  // ターゲットの選びなおし
        }
    }
    public void OnKingKill(King king)
    {
        if (GetTarget() == king.Player)
        {
            ResetTarget();
        }
    }
}