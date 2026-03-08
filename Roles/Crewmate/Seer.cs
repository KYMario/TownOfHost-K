using System;
using System.Collections.Generic;
using AmongUs.GameOptions;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Crewmate;

public sealed class Seer : RoleBase, IKillFlashSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Seer),
            player => new Seer(player),
            CustomRoles.Seer,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            11000,
            SetupOptionItem,
            "se",
            "#61b26c",
            (6, 2),
            from: From.TheOtherRoles
        );
    public Seer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Awakened = !OptAwakening.GetBool() || OptionCanTaskcount.GetInt() < 1;
        ActiveComms = OptionActiveComms.GetBool();
        DelayMode = OptionDelay.GetBool();
        lastMaxdelay = OptionLastMaxdelay.GetFloat();
        lastMindelay = OptionLastMindelay.GetFloat();
        FirstMaxdelay = OptionFirstMaxdelay.GetFloat();
        FirstMindelay = OptionFirstMindelay.GetFloat();
        cantaskcount = OptionCanTaskcount.GetInt();

        Receivedcount = 0;
        lateTaskdatas = [];
    }
    ICollection<(LateTask latetask, float mintime)> lateTaskdatas;
    private static bool ActiveComms;
    private static OptionItem OptionActiveComms;
    static OptionItem OptionDelay; static bool DelayMode;
    static OptionItem OptionLastMindelay; static float lastMindelay;
    static OptionItem OptionLastMaxdelay; static float lastMaxdelay;
    static OptionItem OptionFirstMindelay; static float FirstMindelay;
    static OptionItem OptionFirstMaxdelay; static float FirstMaxdelay;
    static OptionItem OptAwakening;
    static OptionItem OptionCanTaskcount;
    static int cantaskcount;
    bool Awakened;
    int Receivedcount;

    enum OptionName
    {
        SeerDelayMode,
        SeerLastMindelay,
        SeerLastMaxdelay,
        SeerFirstMindelay,
        SeerFirstMaxdelay,
    }
    private static void SetupOptionItem()
    {
        OptionActiveComms = BooleanOptionItem.Create(RoleInfo, 10, GeneralOption.CanUseActiveComms, true, false);
        OptAwakening = BooleanOptionItem.Create(RoleInfo, 16, GeneralOption.AbilityAwakening, false, false);
        OptionCanTaskcount = IntegerOptionItem.Create(RoleInfo, 17, GeneralOption.cantaskcount, new(0, 255, 1), 0, false);
        OptionDelay = BooleanOptionItem.Create(RoleInfo, 11, OptionName.SeerDelayMode, false, false);
        OptionFirstMindelay = FloatOptionItem.Create(RoleInfo, 12, OptionName.SeerFirstMindelay, new(0, 60, 0.5f), 5f, false, OptionDelay).SetValueFormat(OptionFormat.Seconds);
        OptionFirstMaxdelay = FloatOptionItem.Create(RoleInfo, 13, OptionName.SeerFirstMaxdelay, new(0, 60, 0.5f), 7f, false, OptionDelay).SetValueFormat(OptionFormat.Seconds);
        OptionLastMindelay = FloatOptionItem.Create(RoleInfo, 14, OptionName.SeerLastMindelay, new(0, 60, 0.5f), 0f, false, OptionDelay).SetValueFormat(OptionFormat.Seconds);
        OptionLastMaxdelay = FloatOptionItem.Create(RoleInfo, 15, OptionName.SeerLastMaxdelay, new(0, 60, 0.5f), 5f, false, OptionDelay).SetValueFormat(OptionFormat.Seconds);
    }
    public bool? CheckKillFlash(MurderInfo info) // IKillFlashSeeable
    {
        var canseekillflash = !Utils.IsActive(SystemTypes.Comms) || ActiveComms;

        if (GetDelay(out var delays) is false) return false;

        if (DelayMode && canseekillflash)
        {
            var addDelay = 0f;
            //小数対応
            if (delays.Maxdelay > 0)
            {
                int chance = IRandom.Instance.Next(0, (int)delays.Maxdelay * 10);
                addDelay = chance * 0.1f;
                Logger.Info($"{Player?.Data?.GetLogPlayerName()} => {addDelay}sの追加遅延発生!!", "Seer");
            }
            var lateTask = new LateTask(() =>
            {
                if ((!Utils.IsActive(SystemTypes.Comms) || ActiveComms) is false)
                {
                    Logger.Info($"通信妨害中だからキャンセル!", "Seer");
                    return;
                }
                if (GameStates.CalledMeeting || !Player.IsAlive())
                {
                    Logger.Info($"{info?.AppearanceTarget?.Data?.GetLogPlayerName() ?? "???"}のフラッシュを受け取ろうとしたけどなんかし防いだぜ", "seer");
                    return;
                }
                if (Player.IsAlive()) Receivedcount++;
                Player.KillFlash();
            }, addDelay + delays.Mindelay, "SeerDelayKillFlash", null);
            lateTaskdatas.Add((lateTask, delays.Mindelay));
            return null;
        }
        if (Player.IsAlive()) Receivedcount++;
        return canseekillflash;
    }
    public bool GetDelay(out (float Maxdelay, float Mindelay) delays)
    {
        delays = (lastMaxdelay, lastMindelay);

        //タスク数に届いてない場合は無効
        if (MyTaskState.HasCompletedEnoughCountOfTasks(cantaskcount) is false) return false;
        //終わってる場合は最終のものを使う
        if (MyTaskState.IsTaskFinished) return true;
        //一個も終わってない場合
        if (MyTaskState.CompletedTasksCount <= 0)
        {
            delays = (FirstMaxdelay, FirstMindelay);
            return true;
        }

        float proportion = 100 - (MyTaskState.CompletedTasksCount - cantaskcount) * 100 / (MyTaskState.AllTasksCount - cantaskcount);
        proportion *= 0.01f;

        delays = (lastMaxdelay - ((lastMaxdelay - FirstMaxdelay) * proportion),
        lastMindelay - ((lastMindelay - FirstMindelay) * proportion));
        /*delays = (Mathf.Max(lastMaxdelay, FirstMaxdelay) - (Mathf.Abs((lastMaxdelay - FirstMaxdelay) * 10) * proportion * 0.001f),
        Mathf.Max(lastMindelay, FirstMindelay) - Mathf.Abs((lastMindelay - FirstMindelay) * 10) * proportion * 0.001f);*/

        if (delays.Maxdelay < 0) delays.Maxdelay = 0;
        if (delays.Mindelay < 0) delays.Mindelay = 0;

        return true;
    }
    public override string GetProgressText(bool comms = false, bool GameLog = false)
    {
        if (Player.IsAlive() is false) return "";

        if (comms) return $" <#cccccc>{(ActiveComms ? "(?~?)" : "(×)")}</color>";

        if (MyTaskState.HasCompletedEnoughCountOfTasks(cantaskcount) is false) return "";

        if (DelayMode)
        {
            var delays = GetDelay(out var _delays) ? _delays : (-1, -1);
            if (delays.Maxdelay < 0) return "";
            return $" <{RoleInfo.RoleColorCode}>({Math.Round(delays.Mindelay)}~{Math.Round(delays.Maxdelay + delays.Mindelay)})</color>";
        }
        return $" <{RoleInfo.RoleColorCode}>(〇)</color>";
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        bool IsCalled = (!Utils.IsActive(SystemTypes.Comms) || ActiveComms) is false || !Player.IsAlive();
        foreach (var data in lateTaskdatas)
        {
            if (data.latetask is null) continue;
            //条件を満たしていて、経過時間が最小時間を超えていたらキルフラ
            if (!IsCalled && !data.latetask.Isruned && data.latetask.timer > data.mintime)
            {
                IsCalled = true;
                Player.KillFlash();
            }
            //処理を止める
            data.latetask.CallStop();
        }
        lateTaskdatas.Clear();
    }
    public override CustomRoles Misidentify() => Awakened ? CustomRoles.NotAssigned : CustomRoles.Crewmate;
    public override void CheckWinner(GameOverReason reason)
    {
        Achievements.RpcCompleteAchievement(Player.PlayerId, 1, achievements[0], Receivedcount);
        Achievements.RpcCompleteAchievement(Player.PlayerId, 1, achievements[1], Receivedcount);
        Achievements.RpcCompleteAchievement(Player.PlayerId, 1, achievements[2], Receivedcount);
    }
    public static System.Collections.Generic.Dictionary<int, Achievement> achievements = new();
    [Attributes.PluginModuleInitializer]
    public static void Load()
    {
        var n1 = new Achievement(RoleInfo, 0, 5, 0, 0);
        var l1 = new Achievement(RoleInfo, 1, 15, 0, 1);
        var sp1 = new Achievement(RoleInfo, 2, 50, 0, 2, true);
        achievements.Add(0, n1);
        achievements.Add(1, l1);
        achievements.Add(2, sp1);
    }
}