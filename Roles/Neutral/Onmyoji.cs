using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Hazel;
using UnityEngine;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.PlayerCatch;
using static TownOfHost.Utils;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Neutral;

public sealed class Onmyoji : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Onmyoji),
            player => new Onmyoji(player),
            CustomRoles.Onmyoji,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Neutral,
            30200,
            SetupOptionItem,
            "oy",
            "#9b59b6",
            (6, 2),
            true,
            countType: CountTypes.OutOfGame, // 生存者カウントなし
            from: From.SuperNewRoles
        );

    public Onmyoji(PlayerControl player)
    : base(RoleInfo, player, () => HasTask.ForRecompute)
    {
        PhantomCooldown = OptPhantomCooldown.GetFloat();
        VentCooldown = OptVentCooldown.GetFloat();
        VentInTime = OptVentInTime.GetFloat();
        KillCooldown = OptKillCooldown.GetFloat();
        NeedTask = OptNeedTask.GetBool();
        MaxShikigami = OptMaxShikigami.GetInt();
        WinTaskCount = OptWinTaskCount.GetInt();
        MyTaskState.NeedTaskCount = WinTaskCount;

        ShikigamiIds = new();
        checktaskwinflag = !NeedTask;
    }

    static OptionItem OptPhantomCooldown;
    static OptionItem OptVentCooldown;
    static OptionItem OptVentInTime;
    static OptionItem OptKillCooldown;
    static OptionItem OptNeedTask;
    static OptionItem OptMaxShikigami;
    static OptionItem OptWinTaskCount;

    static float PhantomCooldown;
    static float VentCooldown;
    static float VentInTime;
    static float KillCooldown;
    static bool NeedTask;
    static int MaxShikigami;
    static int WinTaskCount;

    public List<byte> ShikigamiIds;
    bool checktaskwinflag;

    enum OptionName
    {
        OnmyojiVentCooldown,
        OnmyojiVentInTime,
        OnmyojiKillCooldown,
        OnmyojiNeedTask,
        OnmyojiMaxShikigami,
        OnmyojiWinTaskCount,
    }

    private static void SetupOptionItem()
    {
        SoloWinOption.Create(RoleInfo, 9, defo: 15);
        OptKillCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.OnmyojiKillCooldown, new(0f, 60f, 2.5f), 25f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptMaxShikigami = IntegerOptionItem.Create(RoleInfo, 11, OptionName.OnmyojiMaxShikigami, new(1, 5, 1), 1, false);
        OptVentCooldown = FloatOptionItem.Create(RoleInfo, 12, OptionName.OnmyojiVentCooldown, new(0f, 60f, 2.5f), 15f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptVentInTime = FloatOptionItem.Create(RoleInfo, 13, OptionName.OnmyojiVentInTime, new(0f, 60f, 2.5f), 10f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptNeedTask = BooleanOptionItem.Create(RoleInfo, 14, OptionName.OnmyojiNeedTask, false, false);
        OptWinTaskCount = IntegerOptionItem.Create(RoleInfo, 15, OptionName.OnmyojiWinTaskCount, new(1, 99, 1), 5, false)
            .SetValueFormat(OptionFormat.Times);
        OverrideTasksData.Create(RoleInfo, 20);
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = VentCooldown;
        AURoleOptions.EngineerInVentMaxTime = VentInTime;
    }

    public float CalculateKillCooldown() => KillCooldown;
    public bool CanUseKillButton() => Player.IsAlive() && ShikigamiIds.Count < MaxShikigami;
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => false;

    // 式神指名
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        info.DoKill = false;

        // キル能力なしクルーのみ指名可能
        if (target.GetCustomRole().IsImpostor()) return;
        if (target.GetCustomRole().GetCustomRoleTypes() == CustomRoleTypes.Neutral) return;
        if ((target.GetRoleClass() as IKiller) != null) return;
        if (ShikigamiIds.Count >= MaxShikigami) return;

        ShikigamiIds.Add(target.PlayerId);

        if (!Utils.RoleSendList.Contains(target.PlayerId))
            Utils.RoleSendList.Add(target.PlayerId);
        target.RpcSetCustomRole(CustomRoles.Shikigami, log: null);

        // 式神に陰陽師のIDを記録
        if (target.GetRoleClass() is Shikigami sk)
            sk.SetOwner(Player.PlayerId);

        // 矢印追加
        GetArrow.Add(Player.PlayerId, target.GetTruePosition());

        killer.ResetKillCooldown();
        killer.SetKillCooldown();
        SendRPC();
        _ = new LateTask(() => UtilsNotifyRoles.NotifyRoles(), 0.2f, "Onmyoji Shikigami");
    }

    // ベント：開閉モーションなし
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        // エンジニアとして処理、開閉モーションをスキップ
        return true;
    }

    // タスク完了チェック
    public override bool OnCompleteTask(uint taskid)
    {
        if (NeedTask && MyTaskState.HasCompletedEnoughCountOfTasks(WinTaskCount))
            checktaskwinflag = true;
        return true;
    }

    // 星詠み：キル能力持ちクルーの名前色を役職色で表示
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (!Is(seer)) return "";
        if (seen.PlayerId == seer.PlayerId) return "";
        if (!Player.IsAlive()) return "";

        // キル能力持ちクルー
        if (seen.GetCustomRole().GetCustomRoleTypes() == CustomRoleTypes.Crewmate
            && seen.GetRoleClass() is IKiller)
        {
            var roleColor = UtilsRoleText.GetRoleColorCode(seen.GetCustomRole());
            return $"<color={roleColor}>★</color>";
        }
        return "";
    }

    // 式神探知：矢印
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (!Is(seer) || isForMeeting || !Player.IsAlive()) return "";
        if (seer.PlayerId != seen.PlayerId) return "";
        if (ShikigamiIds.Count == 0) return "";

        var arrows = "";
        foreach (var id in ShikigamiIds)
        {
            var sk = GetPlayerById(id);
            if (sk == null || !sk.IsAlive()) continue;
            arrows += GetArrow.GetArrows(seer, sk.GetTruePosition());
        }
        return arrows == "" ? "" : $"<color=#9b59b6>{arrows}</color>";
    }

    // 単独勝利判定
    public static bool CheckWinStatic(ref GameOverReason reason)
    {
        foreach (var pc in AllPlayerControls)
        {
            if (pc.GetRoleClass() is not Onmyoji onmyoji) continue;
            if (!pc.IsAlive()) continue;
            if (!onmyoji.checktaskwinflag) continue;

            if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Onmyoji, pc.PlayerId, true))
            {
                CustomWinnerHolder.NeutralWinnerIds.Add(pc.PlayerId);
                // 式神も追加勝利
                foreach (var id in onmyoji.ShikigamiIds)
                    CustomWinnerHolder.WinnerIds.Add(id);
                reason = GameOverReason.ImpostorsByKill;
                return true;
            }
        }
        return false;
    }

    public override string GetProgressText(bool comms = false, bool GameLog = false)
    {
        var skCount = ShikigamiIds.Count;
        var color = checktaskwinflag ? "#9b59b6" : "#5e5e5e";
        return $"<color={color}>式:{skCount}/{MaxShikigami}</color>";
    }

    public void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(ShikigamiIds.Count);
        foreach (var id in ShikigamiIds)
            sender.Writer.Write(id);
        sender.Writer.Write(checktaskwinflag);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        int count = reader.ReadInt32();
        ShikigamiIds = new();
        for (int i = 0; i < count; i++)
            ShikigamiIds.Add(reader.ReadByte());
        checktaskwinflag = reader.ReadBoolean();
    }

    public bool OverrideKillButton(out string text)
    {
        text = "Onmyoji_Kill";
        return true;
    }

    public bool OverrideKillButtonText(out string text)
    {
        text = GetString("OnmyojiKillButtonText");
        return true;
    }
}