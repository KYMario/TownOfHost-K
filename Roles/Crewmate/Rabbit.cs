using System.Collections.Generic;
using AmongUs.GameOptions;
using Hazel;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.PlayerCatch;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Crewmate;

public sealed class Rabbit : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Rabbit),
            player => new Rabbit(player),
            CustomRoles.Rabbit,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            67000,
            SetupOptionItem,
            "rb",
            "#f9c0c0",
            (6, 1),
            from: From.TownOfHost_Y
        );

    public Rabbit(PlayerControl player)
        : base(RoleInfo, player)
    {
        TriggerTaskCount = OptTriggerTaskCount.GetInt();
        ExtraLongTasks = OptExtraLongTasks.GetInt();
        ExtraShortTasks = OptExtraShortTasks.GetInt();

        arrowPositions = new();
        arrowTimers = new();
        abilityUnlocked = false;
        extraTasksAdded = false;
    }

    static OptionItem OptTriggerTaskCount;
    static OptionItem OptExtraLongTasks;
    static OptionItem OptExtraShortTasks;

    static int TriggerTaskCount;
    static int ExtraLongTasks;
    static int ExtraShortTasks;

    Dictionary<byte, Vector2> arrowPositions;
    Dictionary<byte, float> arrowTimers;
    bool abilityUnlocked;
    bool extraTasksAdded;

    enum OptionName
    {
        RabbitTriggerTaskCount,
        RabbitExtraLongTasks,
        RabbitExtraShortTasks,
    }

    private static void SetupOptionItem()
    {
        OptTriggerTaskCount = IntegerOptionItem.Create(RoleInfo, 10, OptionName.RabbitTriggerTaskCount, new(0, 20, 1), 10, false)
            .SetValueFormat(OptionFormat.Times);
        OptExtraLongTasks = IntegerOptionItem.Create(RoleInfo, 11, OptionName.RabbitExtraLongTasks, new(0, 15, 1), 1, false)
            .SetValueFormat(OptionFormat.Times);
        OptExtraShortTasks = IntegerOptionItem.Create(RoleInfo, 12, OptionName.RabbitExtraShortTasks, new(0, 15, 1), 1, false)
            .SetValueFormat(OptionFormat.Times);
        OverrideTasksData.Create(RoleInfo, 20);
    }

    public override bool OnCompleteTask(uint taskid)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        if (!Player.IsAlive()) return true;

        // ★ タスク完了数が設定値以上になったら能力解放
        if (!abilityUnlocked)
        {
            if (MyTaskState.HasCompletedEnoughCountOfTasks(TriggerTaskCount))
                abilityUnlocked = true;
            else
                return true;
        }

        // ★ 通常タスク全完了時に追加タスクを付与
        if (IsTaskFinished && !extraTasksAdded)
        {
            extraTasksAdded = true;
            AddExtraTasks();
            return true;
        }

        // ★ 能力発動：ランダムなインポスターの位置を5秒間表示
        TriggerAbility();
        return true;
    }

    void AddExtraTasks()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        var tasks = new List<byte>();
        var shipStatus = ShipStatus.Instance;

        // ロングタスクを収集
        int longAdded = 0;
        foreach (var task in shipStatus.LongTasks)
        {
            if (longAdded >= ExtraLongTasks) break;
            tasks.Add((byte)task.Index);
            longAdded++;
        }

        // ショートタスクを収集
        int shortAdded = 0;
        foreach (var task in shipStatus.ShortTasks)
        {
            if (shortAdded >= ExtraShortTasks) break;
            tasks.Add((byte)task.Index);
            shortAdded++;
        }

        if (tasks.Count == 0) return;

        Player.Data.RpcSetTasks(tasks.ToArray());

        Logger.Info($"{Player.GetRealName()} に追加タスク付与: Long×{longAdded} Short×{shortAdded}", "Rabbit");
        UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player);
        Utils.SendMessage(GetString("RabbitExtraTaskAdded"), Player.PlayerId);
    }

    void TriggerAbility()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        // ★ 生存しているインポスターをリストアップ
        var impostors = new List<PlayerControl>();
        foreach (var pc in AllAlivePlayerControls)
        {
            if (pc.GetCustomRole().IsImpostor())
                impostors.Add(pc);
        }
        if (impostors.Count == 0) return;

        // ★ ランダムに1名選択
        var rand = IRandom.Instance;
        var target = impostors[rand.Next(impostors.Count)];
        var pos = target.GetTruePosition();

        // ★ 既存の矢印を削除して新しい矢印を追加
        if (arrowPositions.ContainsKey(target.PlayerId))
            GetArrow.Remove(Player.PlayerId, arrowPositions[target.PlayerId]);

        arrowPositions[target.PlayerId] = pos;
        arrowTimers[target.PlayerId] = 5f;
        GetArrow.Add(Player.PlayerId, pos);

        SendRpc();
        UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player);
        Logger.Info($"{Player.GetRealName()} の能力発動: {target.GetRealName()} の位置を探知", "Rabbit");
    }

    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!player.IsAlive()) return;
        if (arrowTimers.Count == 0) return;

        bool updated = false;
        var toRemove = new List<byte>();

        foreach (var kvp in arrowTimers)
        {
            arrowTimers[kvp.Key] -= Time.fixedDeltaTime;
            if (arrowTimers[kvp.Key] <= 0f)
                toRemove.Add(kvp.Key);
        }

        foreach (var id in toRemove)
        {
            if (arrowPositions.TryGetValue(id, out var pos))
                GetArrow.Remove(Player.PlayerId, pos);
            arrowPositions.Remove(id);
            arrowTimers.Remove(id);
            updated = true;
        }

        if (updated)
        {
            SendRpc();
            UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player);
        }
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        // ★ 会議前に矢印をリセット
        foreach (var pos in arrowPositions.Values)
            GetArrow.Remove(Player.PlayerId, pos);
        arrowPositions.Clear();
        arrowTimers.Clear();
        SendRpc();
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (!Is(seer) || isForMeeting || !Player.IsAlive()) return "";
        if (seer.PlayerId != seen.PlayerId) return "";
        if (arrowPositions.Count == 0) return "";

        var result = "";
        foreach (var kvp in arrowPositions)
        {
            result += GetArrow.GetArrows(seer, kvp.Value);
        }
        return result == "" ? "" : $"<color=#f9c0c0>{result}</color>";
    }

    public override string GetProgressText(bool comms = false, bool GameLog = false)
    {
        if (!abilityUnlocked)
        {
            var remaining = TriggerTaskCount - MyTaskState.CompletedTasksCount;
            if (remaining <= 0) remaining = 0;
            return $"<color=#5e5e5e>(あと{remaining})</color>";
        }
        return $"<color=#f9c0c0>◎</color>";
    }

    void SendRpc()
    {
        using var sender = CreateSender();
        sender.Writer.Write(abilityUnlocked);
        sender.Writer.Write(extraTasksAdded);
        sender.Writer.Write(arrowPositions.Count);
        foreach (var kvp in arrowPositions)
        {
            sender.Writer.Write(kvp.Key);
            NetHelpers.WriteVector2(kvp.Value, sender.Writer);
        }
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        abilityUnlocked = reader.ReadBoolean();
        extraTasksAdded = reader.ReadBoolean();

        foreach (var pos in arrowPositions.Values)
            GetArrow.Remove(Player.PlayerId, pos);
        arrowPositions.Clear();
        arrowTimers.Clear();

        int count = reader.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            var id = reader.ReadByte();
            var pos = NetHelpers.ReadVector2(reader);
            arrowPositions[id] = pos;
        }
    }
}