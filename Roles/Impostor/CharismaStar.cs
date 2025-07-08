using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;
public sealed class CharismaStar : RoleBase, IImpostor, IUsePhantomButton, IDoubleTrigger
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(CharismaStar),
            player => new CharismaStar(player),
            CustomRoles.CharismaStar,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Impostor,
            25400,
            SetUpOptionItem,
            "chs",
            from: From.TownOfHost_Y
        );

    public CharismaStar(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        killCooldown = optionKillCooldown.GetFloat();
        gatherCooldown = optionGatherCooldown.GetFloat();
        // 集合クールがキルクールより短い時、集合クールはキルクールと同じにする
        if (gatherCooldown < killCooldown)
        {
            gatherCooldown = killCooldown;
        }

        gatherMaxCount = optionGatherMaxCount.GetInt();
        notGatherPlayerKill = optionNotGatherPlayerKill.GetBool();
        canAllPlayerGather = optionCanAllPlayerGather.GetBool();
    }

    private static OptionItem optionKillCooldown;
    private static OptionItem optionGatherCooldown;
    private static OptionItem optionGatherMaxCount;
    private static OptionItem optionNotGatherPlayerKill;
    private static OptionItem optionCanAllPlayerGather;

    enum OptionName
    {
        CharismaStarGatherCooldown,
        CharismaStarGatherMaxCount,
        CharismaStarNotGatherPlayerKill,
        CharismaStarCanAllPlayerGather,
    }

    private static float killCooldown;
    private static float gatherCooldown;
    private static int gatherMaxCount;
    private static bool notGatherPlayerKill;
    private static bool canAllPlayerGather;

    private HashSet<byte> gatherChoosePlayers;
    private int gatherLimitCount;

    private static Vector2 LIFT_POSITION = new(7.76f, 8.56f); //昇降機の座標

    private static void SetUpOptionItem()
    {
        optionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
        optionGatherCooldown = FloatOptionItem.Create(RoleInfo, 11, OptionName.CharismaStarGatherCooldown, new(2.5f, 180f, 2.5f), 30f, false)
        .SetValueFormat(OptionFormat.Seconds);
        optionGatherMaxCount = IntegerOptionItem.Create(RoleInfo, 12, OptionName.CharismaStarGatherMaxCount, new(1, 10, 1), 3, false)
            .SetValueFormat(OptionFormat.Pieces);
        optionNotGatherPlayerKill = BooleanOptionItem.Create(RoleInfo, 13, OptionName.CharismaStarNotGatherPlayerKill, true, false);
        optionCanAllPlayerGather = BooleanOptionItem.Create(RoleInfo, 14, OptionName.CharismaStarCanAllPlayerGather, true, false);
    }

    public float CalculateKillCooldown() => killCooldown;
    public override void ApplyGameOptions(IGameOptions opt) => AURoleOptions.PhantomCooldown = gatherCooldown;

    public override void Add()
    {
        Player.AddDoubleTrigger();

        gatherLimitCount = gatherMaxCount;
        gatherChoosePlayers = new();
    }

    public bool CheckAction => gatherLimitCount > 0;

    public bool SingleAction(PlayerControl killer, PlayerControl target)
    {
        // 集めるターゲット登録
        gatherChoosePlayers.Add(target.PlayerId);
        Logger.Info($"{killer.GetNameWithRole()} → {target.GetNameWithRole()}：ターゲット選択", "CharismaStar");
        // 表示更新
        UtilsNotifyRoles.NotifyRoles(SpecifySeer: killer);

        return false;
    }

    public bool DoubleAction(PlayerControl killer, PlayerControl target)
    {
        // アビリティクールリセット
        Player.RpcResetAbilityCooldown();

        // ホストの集合後のキルクールが0.1のままになってしまう為ここでもリセット
        if (Player == PlayerControl.LocalPlayer)
        {
            _ = new LateTask(() =>
            {
                Player.SetKillCooldown(killCooldown);
            }, 0.2f, "CharismaStar_HostSetKillCooldown");
        }

        return true;
    }

    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        if (!info.CanKill)
        {
            return;
        }

        // キルする時のみクールリセット処理
        if (info.DoKill)
        {
            // アビリティクールリセット
            Player.RpcResetAbilityCooldown();

            // ホストの集合後のキルクールが0.1のままになってしまう為ここでもリセット
            if (Player == PlayerControl.LocalPlayer)
            {
                _ = new LateTask(() =>
                {
                    Player.SetKillCooldown(killCooldown);
                }, 0.2f, "CharismaStar_HostSetKillCooldown");
            }
        }
    }

    public bool UseOneclickButton => gatherLimitCount > 0;

    public void OnClick(ref bool resetkillcooldown, ref bool? fall)
    {
        // クールダウン設定(使用時は既にキルクールがない想定)
        resetkillcooldown = false;
        Main.AllPlayerKillCooldown[Player.PlayerId] = 0.1f;
        fall = false;

        // リストに誰も登録されていない
        if (gatherChoosePlayers.Count == 0)
        {
            // 全員集合できるモードでないなら能力は使わない
            if (!canAllPlayerGather) return;

            // 全員をリストに登録する
            PlayerCatch.AllAlivePlayerControls.Do(target => gatherChoosePlayers.Add(target.PlayerId));
        }
        else
        {
            // 集まる直前に自身を登録する
            gatherChoosePlayers.Add(Player.PlayerId);
        }

        // リストに登録されている人たち
        foreach (var targetId in gatherChoosePlayers)
        {
            var target = PlayerCatch.GetPlayerById(targetId);
            // 死亡していたら関係ない
            if (!target.IsAlive()) continue;

            // ターゲットが梯子またはヌーンを使っている
            if ((target.MyPhysics.Animations.IsPlayingAnyLadderAnimation()
                || ((MapNames)Main.NormalOptions.MapId == MapNames.Airship && Vector2.Distance(target.GetTruePosition(), LIFT_POSITION) <= 1.9f))
                && !target.Is(CustomRoleTypes.Impostor))
            {
                // 集まらないプレイヤーをキルするがONの時
                if (notGatherPlayerKill)
                {
                    target.SetRealKiller(Player);
                    target.RpcMurderPlayer(target);
                    PlayerState.GetByPlayerId(targetId).DeathReason = CustomDeathReason.NotGather;
                    // キルフラッシュを自視点に鳴らす
                    Player.KillFlash();
                }
                Logger.Info($"{target.GetNameWithRole()} : ワープできませんでした。", "CharismaStar");
                continue;
            }

            // ベントに集合
            target.MyPhysics.RpcExitVent(GetNearestVent().Id);
        }

        // 能力使用後のリセット
        gatherChoosePlayers.Clear();
        gatherLimitCount--;
        Logger.Info($"{Player.GetNameWithRole()} : 残り{gatherLimitCount}回", "CharismaStar");
        // 表示更新
        UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player);
    }

    Vent GetNearestVent()
    {
        var vents = ShipStatus.Instance.AllVents.OrderBy(v => (Player.transform.position - v.transform.position).magnitude);
        return vents.First();
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        // seenが省略の場合seer
        seen ??= seer;

        if (gatherChoosePlayers.Contains(seen.PlayerId))
        {
            return Utils.ColorString(RoleInfo.RoleColor, "◎");
        }

        return string.Empty;
    }
    public override string GetProgressText(bool comms = false, bool GameLog = false)
        => Utils.ColorString(gatherLimitCount > 0 ? RoleInfo.RoleColor : Color.gray, $"[{gatherLimitCount}]");
    public override string GetAbilityButtonText() => Translator.GetString("CharismaStarGatherButtonText");
}