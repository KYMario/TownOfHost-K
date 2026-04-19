using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using UnityEngine;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.PlayerCatch;

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
    : base(RoleInfo, player)
    {
        Awakened = !OptAwakening.GetBool() || OptionCanTaskcount.GetInt() < 1;
        ActiveComms = OptionActiveComms.GetBool();
        DelayMode = OptionDelay.GetBool();
        lastMaxdelay = OptionLastMaxdelay.GetFloat();
        lastMindelay = OptionLastMindelay.GetFloat();
        FirstMaxdelay = OptionFirstMaxdelay.GetFloat();
        FirstMindelay = OptionFirstMindelay.GetFloat();
        cantaskcount = OptionCanTaskcount.GetInt();
        ShowSoul = OptionShowSoul.GetBool();

        Receivedcount = 0;
        lateTaskdatas = [];
        SoulObjects = new();
        PendingDeadBodies = new();
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

    // ★ 霊魂オプション・管理
    static OptionItem OptionShowSoul;
    static bool ShowSoul;
    private readonly List<SoulObject> SoulObjects;
    private readonly List<(Vector2 pos, int colorId, string playerName)> PendingDeadBodies;

    enum OptionName
    {
        SeerDelayMode,
        SeerLastMindelay,
        SeerLastMaxdelay,
        SeerFirstMindelay,
        SeerFirstMaxdelay,
        SeerShowSoul,
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
        // ★ 霊魂表示オプション（ID:18）
        OptionShowSoul = BooleanOptionItem.Create(RoleInfo, 18, OptionName.SeerShowSoul, true, false);
    }

    public bool? CheckKillFlash(MurderInfo info)
    {
        var canseekillflash = !Utils.IsActive(SystemTypes.Comms) || ActiveComms;

        if (GetDelay(out var delays) is false) return false;

        if (DelayMode && canseekillflash)
        {
            var addDelay = 0f;
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

        if (MyTaskState.HasCompletedEnoughCountOfTasks(cantaskcount) is false) return false;
        if (MyTaskState.IsTaskFinished) return true;
        if (MyTaskState.CompletedTasksCount <= 0)
        {
            delays = (FirstMaxdelay, FirstMindelay);
            return true;
        }

        float proportion = 100 - (MyTaskState.CompletedTasksCount - cantaskcount) * 100 / (MyTaskState.AllTasksCount - cantaskcount);
        proportion *= 0.01f;

        delays = (lastMaxdelay - ((lastMaxdelay - FirstMaxdelay) * proportion),
        lastMindelay - ((lastMindelay - FirstMindelay) * proportion));

        if (delays.Maxdelay < 0) delays.Maxdelay = 0;
        if (delays.Mindelay < 0) delays.Mindelay = 0;

        return true;
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        // ★ 既存のキルフラッシュ遅延処理
        bool IsCalled = (!Utils.IsActive(SystemTypes.Comms) || ActiveComms) is false || !Player.IsAlive();
        foreach (var data in lateTaskdatas)
        {
            if (data.latetask is null) continue;
            if (!IsCalled && !data.latetask.Isruned && data.latetask.timer > data.mintime)
            {
                IsCalled = true;
                Player.KillFlash();
            }
            data.latetask.CallStop();
        }
        lateTaskdatas.Clear();

        // ★ 霊魂記録処理
        if (!ShowSoul) return;
        if (!Player.IsAlive()) return;
        if (target == null) return; // 緊急会議ボタン

        var deadBody = UnityEngine.Object.FindObjectsOfType<DeadBody>()
            .FirstOrDefault(db => db.ParentId == target.PlayerId);

        Vector2 pos = deadBody != null
            ? (Vector2)deadBody.transform.position
            : (GetPlayerById(target.PlayerId)?.GetTruePosition() ?? Vector2.zero);

        string playerName = target.PlayerName;
        int colorId = target.DefaultOutfit.ColorId;

        if (!PendingDeadBodies.Exists(d => d.playerName == playerName))
            PendingDeadBodies.Add((pos, colorId, playerName));
    }

    // ★ 会議終了後に霊魂を設置
    public override void AfterMeetingTasks()
    {
        if (!ShowSoul) return;
        if (!AmongUsClient.Instance.AmHost) return;
        if (!Player.IsAlive()) return;
        if (PendingDeadBodies.Count == 0) return;

        foreach (var (pos, colorId, playerName) in PendingDeadBodies)
            SoulObjects.Add(new SoulObject(pos, colorId, playerName, Player));

        PendingDeadBodies.Clear();
    }

    // ★ シーアが死んだら霊魂を全部消す
    public override void OnMurderPlayerAsTarget(MurderInfo info)
    {
        foreach (var soul in SoulObjects) soul?.Despawn();
        SoulObjects.Clear();
    }

    public override string GetProgressText(bool comms = false, bool GameLog = false)
    {
        if (Player.IsAlive() is false) return "";

        if (comms) return $" <#cccccc>{(ActiveComms ? "(?~?)" : "(×)")}</color>";

        if (MyTaskState.HasCompletedEnoughCountOfTasks(cantaskcount) is false) return "";

        string soulText = ShowSoul && SoulObjects.Count > 0
            ? $"<color=#c8a2c8> 👻{SoulObjects.Count}</color>"
            : "";

        if (DelayMode)
        {
            var delays = GetDelay(out var _delays) ? _delays : (-1, -1);
            if (delays.Maxdelay < 0) return soulText;
            return $" <{RoleInfo.RoleColorCode}>({Math.Round(delays.Mindelay)}~{Math.Round(delays.Maxdelay + delays.Mindelay)})</color>{soulText}";
        }
        return $" <{RoleInfo.RoleColorCode}>(〇)</color>{soulText}";
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

// ======================================================
// ★ 霊魂ダミーオブジェクト
// ======================================================
public sealed class SoulObject : CustomNetObject
{
    private static readonly string[] ColorCodes =
    {
        "#c51111", // 0  Red
        "#132ed1", // 1  Blue
        "#117f2d", // 2  Green
        "#ed54ba", // 3  Pink
        "#ef7d0d", // 4  Orange
        "#f5f557", // 5  Yellow
        "#3f474e", // 6  Black
        "#d6e0f0", // 7  White
        "#6b2fbb", // 8  Purple
        "#71491e", // 9  Brown
        "#38fedc", // 10 Cyan
        "#50ef39", // 11 Lime
        "#ff0000", // 12 Maroon
        "#ffff00", // 13 Rose
        "#fffebe", // 14 Banana
        "#c8a2c8", // 15 Coral
        "#4d2b6e", // 16 Sunset
        "#00c3fc", // 17 Teal
    };

    public SoulObject(Vector2 position, int colorId, string playerName, PlayerControl seer)
    {
        string color = colorId >= 0 && colorId < ColorCodes.Length
            ? ColorCodes[colorId]
            : "#ffffff";

        // ★ スプライト: 死者のカラーで霊魂を表現
        string sprite = $"<size=150%><color={color}>👻</color></size>";

        CreateNetObject(sprite, position);

        _ = new LateTask(() =>
        {
            // ★ シーア以外を非表示
            foreach (var pc in AllPlayerControls)
            {
                if (pc.PlayerId != seer.PlayerId)
                    Hide(pc);
            }

            // ★ 名前を「霊魂\n(プレイヤー名)」にする
            if (PlayerControl != null && PlayerControl.cosmetics?.nameText != null)
            {
                PlayerControl.cosmetics.nameText.text =
                    $"<color={color}>霊魂\n<size=70%>({playerName})</size></color>";
            }
        }, 0.5f, "SoulObject.Setup", true);
    }

    // ★ 霊魂は会議をまたいでも消えない（累積して残る）
    public override void OnMeeting() { }
}