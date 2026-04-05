using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using System.Reflection;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Crewmate;

public sealed class Santa : RoleBase, IKiller
{
    // ★ 追加：内部的にキラー扱いにする（TOH-P のキルキャンセルを防ぐ）
    bool IKiller.IsKiller => true;
    bool IKiller.CanKill => true;

    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Santa),
            player => new Santa(player),
            CustomRoles.Santa,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            66100,
            SetupOptionItem,
            "st",
            "#f29c9f",
            (6, 0),
            from: From.SuperNewRoles,
            isDesyncImpostor: true

        );

    public Santa(PlayerControl player)
        : base(RoleInfo, player)
    {
        KillCooldown = OptKillCooldown.GetFloat();
        taskCompleted = false;
    }

    static OptionItem OptKillCooldown;
    static float KillCooldown;

    static OptionItem OptBalancerRate;
    static OptionItem OptSheriffRate;
    static OptionItem OptLighterRate;
    static OptionItem OptUltraStarRate;
    static OptionItem OptExpressRate;

    bool taskCompleted;
    private enum OptionName
    {
        SantaGiftRateBalancer,
        SantaGiftRateSheriff,
        SantaGiftRateLighter,
        SantaGiftRateUltraStar,
        SantaGiftRateExpress
    }

    // UltraStar化前の元色を保持
    private static readonly Dictionary<byte, int> RememberedColorByPlayerId = new();

    private static void SetupOptionItem()
    {
        OptBalancerRate = IntegerOptionItem.Create(
            RoleInfo, 11, OptionName.SantaGiftRateBalancer,
            new(0, 100, 5), 20, false
        ).SetValueFormat(OptionFormat.Percent);

        OptSheriffRate = IntegerOptionItem.Create(
            RoleInfo, 12, OptionName.SantaGiftRateSheriff,
            new(0, 100, 5), 20, false
        ).SetValueFormat(OptionFormat.Percent);

        OptLighterRate = IntegerOptionItem.Create(
            RoleInfo, 13, OptionName.SantaGiftRateLighter,
            new(0, 100, 5), 20, false
        ).SetValueFormat(OptionFormat.Percent);

        OptUltraStarRate = IntegerOptionItem.Create(
            RoleInfo, 14, OptionName.SantaGiftRateUltraStar,
            new(0, 100, 5), 20, false
        ).SetValueFormat(OptionFormat.Percent);

        OptExpressRate = IntegerOptionItem.Create(
            RoleInfo, 15, OptionName.SantaGiftRateExpress,
            new(0, 100, 5), 20, false
        ).SetValueFormat(OptionFormat.Percent);

        OptKillCooldown = FloatOptionItem.Create(
            RoleInfo, 10, "SantaKillCooldown",
            new(0.5f, 60f, 0.5f), 25f, false
        ).SetValueFormat(OptionFormat.Seconds);

        OverrideTasksData.Create(RoleInfo, 200);
    }
    public float CalculateKillCooldown() => KillCooldown;
    private static int GetGiftRate(CustomRoles role) => role switch
    {
        CustomRoles.Balancer => OptBalancerRate?.GetInt() ?? 0,
        CustomRoles.Sheriff => OptSheriffRate?.GetInt() ?? 0,
        CustomRoles.Lighter => OptLighterRate?.GetInt() ?? 0,
        CustomRoles.UltraStar => OptUltraStarRate?.GetInt() ?? 0,
        CustomRoles.Express => OptExpressRate?.GetInt() ?? 0,
        _ => 0
    };

    private static CustomRoles RollGiftRole(CustomRoles[] giftRoles)
    {
        var weightedRoles = giftRoles
            .Select(role =>
            {
                var weight = GetGiftRate(role);
                if (weight < 0) weight = 0;
                if (weight > 100) weight = 100;
                return (Role: role, Weight: weight);
            })
            .Where(x => x.Weight > 0)
            .ToArray();

        if (weightedRoles.Length == 0)
        {
            // 全部0%の場合は従来どおり等確率にフォールバック
            return giftRoles[IRandom.Instance.Next(giftRoles.Length)];
        }

        var totalWeight = weightedRoles.Sum(x => x.Weight);
        var roll = IRandom.Instance.Next(totalWeight);
        var acc = 0;

        foreach (var entry in weightedRoles)
        {
            acc += entry.Weight;
            if (roll < acc) return entry.Role;
        }

        return weightedRoles[weightedRoles.Length - 1].Role;
    }
    // ★ タスク完了後のみキルボタンを使える
    public bool CanUseKillButton() => Player.IsAlive() && taskCompleted;
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => false;

    public override RoleTypes? AfterMeetingRole => taskCompleted ? RoleTypes.Impostor : RoleTypes.Crewmate;

    // ★ タスク完了時にデシンクでインポスターにする
    public override bool OnCompleteTask(uint taskid)
    {
        // ★ 死んでいたら絶対にタスク完了扱いにしない
        if (!Player.IsAlive())
            return true;

        if (IsTaskFinished && !taskCompleted)
        {
            taskCompleted = true;

            if (!AmongUsClient.Instance.AmHost) return true;

            // ★ 生存中のみデシンクでインポスター化
            Player.RpcSetRoleDesync(RoleTypes.Impostor, Player.GetClientId());

            Player.ResetKillCooldown();
            Player.SetKillCooldown();

            UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player);
        }
        return true;
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(false);
    }

    // ★ 会議後も内部ロールは変えない
    public override void ChengeRoleAdd()
    {
        base.ChengeRoleAdd();

        // ★ ホストだけ会議後に内部ロールが戻るので再デシンク
        if (taskCompleted && Player.IsAlive() && AmongUsClient.Instance.AmHost)
        {
            Player.RpcSetRoleDesync(RoleTypes.Impostor, Player.GetClientId());
        }
    }

    // ★ キルボタン → プレゼント処理
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        info.DoKill = false;

        if (target.PlayerId == killer.PlayerId) return;

        // ★ 追加：クルー以外にプレゼントしようとしたら自爆
        if (target.GetCustomRole().GetCustomRoleTypes() != CustomRoleTypes.Crewmate)
        {
            PlayerState.GetByPlayerId(Player.PlayerId).DeathReason = CustomDeathReason.Suicide;
            killer.RpcMurderPlayerV2(killer);
            return;
        }

        // ランダム付与候補
        CustomRoles[] giftRoles =
        {
            CustomRoles.Balancer,
            CustomRoles.Sheriff,
            CustomRoles.Lighter,
            CustomRoles.UltraStar,
            CustomRoles.Express
        };

        var rand = IRandom.Instance;
        var role = RollGiftRole(giftRoles);

        var beforeRole = target.GetCustomRole();

        // UltraStar化する直前に元色を記録（初回のみ）
        if (role == CustomRoles.UltraStar && beforeRole != CustomRoles.UltraStar)
        {
            RememberedColorByPlayerId[target.PlayerId] = target.Data.DefaultOutfit.ColorId;
        }

        // Express -> 非Express になる場合は速度をバニラへ戻す
        bool resetExpressSpeed = beforeRole == CustomRoles.Express && role != CustomRoles.Express;
        if (resetExpressSpeed)
        {
            Main.AllPlayerSpeed[target.PlayerId] = Main.NormalOptions.PlayerSpeedMod;
        }

        if (!Utils.RoleSendList.Contains(target.PlayerId))
            Utils.RoleSendList.Add(target.PlayerId);

        // 役職付与
        target.RpcSetCustomRole(role, log: null);

        // UltraStar -> 非UltraStar になったら元色へ戻す
        if (beforeRole == CustomRoles.UltraStar &&
            role != CustomRoles.UltraStar &&
            RememberedColorByPlayerId.TryGetValue(target.PlayerId, out var originalColorId))
        {
            target.RpcSetColor((byte)originalColorId);
            RememberedColorByPlayerId.Remove(target.PlayerId);
        }

        // ★ UltraStar の RoleBase を生成（全員に UltraStar 表記を見せるため）
        if (role == CustomRoles.UltraStar)
        {
            var field = typeof(UltraStar).GetField("CanseeAllplayer", BindingFlags.NonPublic | BindingFlags.Static);
            field?.SetValue(null, true);
        }

        if (resetExpressSpeed)
        {
            UtilsOption.MarkEveryoneDirtySettings();
        }

        killer.ResetKillCooldown();
        killer.SetKillCooldown();

        _ = new LateTask(() => UtilsNotifyRoles.NotifyRoles(ForceLoop: true), 0.2f, "Santa Gift");
    }

    public bool OverrideKillButtonText(out string text)
    {
        text = "プレゼント";
        return true;
    }

    public bool OverrideKillButton(out string text)
    {
        text = "Santa_Gift";
        return true;
    }
}
