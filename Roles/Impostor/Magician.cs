using AmongUs.GameOptions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using Hazel;

namespace TownOfHost.Roles.Impostor;

public sealed class Magician : RoleBase, IImpostor, IUsePhantomButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Magician),
            player => new Magician(player),
            CustomRoles.Magician,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Impostor,
            4000,
            SetupOptionItem,
            "mc",
            OptionSort: (3, 2)
        );
    public Magician(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        DefaultCooldown = OptionMagicCooldown.GetFloat();
        Maximum = OptionMaximum.GetFloat();
        Radius = OptionRadius.GetFloat();
        ShowDeadbody = OptionShowDeadbody.GetBool();
        MagicUseKillCount = OptionMagicUseKillCount.GetInt();
        ResetKillCount = OptionResetKillCount.GetBool();
        ResetMagicTarget = OptionResetMagicTarget.GetBool();
        MagicCooldown = DefaultCooldown;
        MagicCount = 0;
        HaveKillCount = 0;
        MagicTarget = new();
    }

    static OptionItem OptionMagicCooldown;
    static OptionItem OptionMaximum;
    static OptionItem OptionRadius;
    static OptionItem OptionShowDeadbody;
    static OptionItem OptionMagicUseKillCount;
    static OptionItem OptionResetKillCount;
    static OptionItem OptionResetMagicTarget;

    static float DefaultCooldown;
    static float Maximum;
    static float Radius;
    static bool ShowDeadbody; //会議で死亡してるかを表示するか
    static int MagicUseKillCount; //必要なキル数
    static bool ResetKillCount; //←↓会議後リセットするかの設定 | キル数

    float MagicCooldown;
    bool ResetMagicTarget;// マジックで消す予定の人

    int MagicCount;
    int HaveKillCount;
    List<byte> MagicTarget;

    enum Option
    {
        MagicianMaximum,
        MagicianRadius,
        MagicianShowDeadbody,
        MagicianMagicUseKillCount,
        MagicianResetKillCount,
        MagicianResetMagicTarget,
    }

    public static void SetupOptionItem()
    {
        OptionMagicCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.Cooldown, new FloatValueRule(0, 120, 1), 15, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionMaximum = IntegerOptionItem.Create(RoleInfo, 11, Option.MagicianMaximum, new(0, 99, 1), 3, false)
            .SetValueFormat(OptionFormat.Times).SetZeroNotation(OptionZeroNotation.Infinity);
        OptionRadius = FloatOptionItem.Create(RoleInfo, 12, Option.MagicianRadius, new(0.5f, 3f, 0.5f), 1.5f, false)
            .SetValueFormat(OptionFormat.Multiplier);
        OptionMagicUseKillCount = IntegerOptionItem.Create(RoleInfo, 13, Option.MagicianMagicUseKillCount, new(0, 99, 1), 1, false)
        .SetValueFormat(OptionFormat.Players);
        OptionShowDeadbody = BooleanOptionItem.Create(RoleInfo, 14, Option.MagicianShowDeadbody, false, false);
        OptionResetKillCount = BooleanOptionItem.Create(RoleInfo, 15, Option.MagicianResetKillCount, true, false);
        OptionResetMagicTarget = BooleanOptionItem.Create(RoleInfo, 16, Option.MagicianResetMagicTarget, false, false);
    }

    public override void AfterMeetingTasks()
    {
        if (ResetKillCount) HaveKillCount = 0;
        if (ResetMagicTarget) MagicTarget.Clear();
        //クールダウンリセット
        if (Maximum <= MagicCount && Maximum > 0)
            MagicCooldown = 999;
        else
            MagicCooldown = DefaultCooldown;
    }

    public void OnMurderPlayerAsKiller(MurderInfo info) => HaveKillCount++;
    bool IUsePhantomButton.IsPhantomRole => MagicCount < Maximum || Maximum is 0;
    public void OnClick(ref bool AdjustKillCooldown, ref bool? ResetCooldown)
    {
        AdjustKillCooldown = true;
        ResetCooldown = false;
        if (Maximum <= MagicCount && Maximum > 0) return;
        ResetCooldown = true;
        Dictionary<PlayerControl, float> distance = new();
        float dis;
        bool check = false;
        foreach (var pc in PlayerCatch.AllAlivePlayerControls)
        {
            if (pc.Is(CustomRoles.King)) continue;
            dis = Vector2.Distance(Player.transform.position, pc.transform.position);
            //↑プレイヤーとの距離 ↓自分以外、で近くにいて 既にターゲットにされていないか
            if (pc.PlayerId != Player.PlayerId && dis < Radius && !MagicTarget.Contains(pc.PlayerId) && !pc.GetCustomRole().IsImpostor())
            {
                distance.Add(pc, dis);
            }
        }
        if (distance.Count != 0)
        {
            var min = distance.OrderBy(c => c.Value).FirstOrDefault();
            var target = min.Key;

            MagicCount++;
            SendRPC();
            check = (Maximum <= MagicCount && Maximum > 0) || MagicCooldown != DefaultCooldown;
            MagicTarget.Add(target.PlayerId);
            Player.RpcProtectedMurderPlayer(target);
            MagicCooldown = (Maximum <= MagicCount && Maximum > 0) ? 999 : DefaultCooldown;
            _ = new LateTask(() => UtilsNotifyRoles.NotifyRoles(), 0.1f);
        }
        else
        {
            check = MagicCooldown != 0;
            MagicCooldown = 0;
        }
        if (check)
        {
            Player.MarkDirtySettings();
            Player.RpcResetAbilityCooldown();
        }
    }

    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (MagicTarget.Count == 0 || HaveKillCount < MagicUseKillCount) return;
        foreach (var id in MagicTarget)
        {
            var target = PlayerCatch.GetPlayerById(id);
            if (!target.IsAlive()) continue;
            var state = PlayerState.GetByPlayerId(target.PlayerId);
            Player.RpcProtectedMurderPlayer(target);
            if (ShowDeadbody)
            {
                var position = target.transform.position;
                target.RpcSnapToForced(new Vector2(9999f, 9999f));
                target.RpcMurderPlayer(target, true);
                _ = new LateTask(() => target.RpcSnapToForced(position), 0.5f);
            }
            else
            {
                target.RpcExileV2();
            }
            state.DeathReason = CustomDeathReason.Magic;
            state.SetDead();
        }
        MagicTarget.Clear();
        HaveKillCount -= MagicUseKillCount;
        SendRPC();
        Player.SetKillCooldown();
        _ = new LateTask(() => UtilsNotifyRoles.NotifyRoles(), 0.1f);
    }

    public override string GetAbilityButtonText() => GetString("MagicButtonText");
    public override bool OverrideAbilityButton(out string text)
    {
        text = "Magician_Ability";
        return true;
    }

    public override string GetProgressText(bool comms = false, bool gamelog = false)
    {
        if (Maximum == 0 && MagicUseKillCount == 0) return "";
        var text = "(";
        if (Maximum != 0) text += Maximum - MagicCount;
        if (MagicUseKillCount > 0) text += (Maximum > 0 ? " | " : "") + $"{HaveKillCount}/{MagicUseKillCount}";
        var color = HaveKillCount < MagicUseKillCount ? Color.yellow : MagicCount < Maximum && Maximum > 0 ? Color.red : Color.gray;
        return Utils.ColorString(color, text + ")");
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.PhantomCooldown = MagicCooldown;
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (seen.PlayerId != seer.PlayerId || isForMeeting || !Player.IsAlive()) return "";

        if (isForHud) return GetString("PhantomButtonLowertext");
        return $"<size=50%>{GetString("PhantomButtonLowertext")}</size>";
    }

    public void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(MagicCount);
        sender.Writer.Write(HaveKillCount);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        MagicCount = reader.ReadInt32();
        HaveKillCount = reader.ReadInt32();
    }
}