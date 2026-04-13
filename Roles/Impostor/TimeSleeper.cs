using System.Collections.Generic;
using AmongUs.GameOptions;
using Hazel;
using UnityEngine;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;

public sealed class TimeSleeper : RoleBase, IImpostor, IUsePhantomButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(TimeSleeper),
            player => new TimeSleeper(player),
            CustomRoles.TimeSleeper,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Impostor,
            25000,
            SetUpOptionItem,
            "ts",
            OptionSort: (3, 14)
        );

    public TimeSleeper(PlayerControl player)
        : base(RoleInfo, player)
    {
        RecordDuration = OptionRecordDuration.GetFloat();
        RewindSpeed = OptionRewindSpeed.GetFloat();
        PhantomCooldown = OptionPhantomCooldown.GetFloat();

        isRecording = false;
        isRewinding = false;
        recordTimer = 0f;
        rewindTimer = 0f;
        rewindIndex = 0;
        positionHistory = new();
        rewindSkipPlayers = new();
    }

    static OptionItem OptionPhantomCooldown;
    static float PhantomCooldown;
    static OptionItem OptionRecordDuration;
    static float RecordDuration;
    static OptionItem OptionRewindSpeed;
    static float RewindSpeed;

    bool isRecording;
    bool isRewinding;
    float recordTimer;
    float rewindTimer;
    int rewindIndex;

    Dictionary<byte, List<Vector2>> positionHistory;
    // ★ REC開始時に梯子・ぬーん・ジップライン使用中だったプレイヤー
    HashSet<byte> rewindSkipPlayers;

    enum OptionName
    {
        TimeSleeperRecordDuration,
        TimeSleeperRewindSpeed,
    }

    static void SetUpOptionItem()
    {
        OptionPhantomCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.Cooldown, OptionBaseCoolTime, 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionRecordDuration = FloatOptionItem.Create(RoleInfo, 11, OptionName.TimeSleeperRecordDuration, new(1f, 20f, 0.5f), 5f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionRewindSpeed = FloatOptionItem.Create(RoleInfo, 12, OptionName.TimeSleeperRewindSpeed, new(0.5f, 5f, 0.5f), 1f, false)
            .SetValueFormat(OptionFormat.Multiplier);
    }

    public float CalculateKillCooldown() => 30f;
    public bool CanUseSabotageButton() => true;
    public bool CanUseImpostorVentButton() => true;
    bool IUsePhantomButton.IsresetAfterKill => false;

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.PhantomCooldown = PhantomCooldown;
    }

    bool IUsePhantomButton.IsPhantomRole => true;

    void IUsePhantomButton.OnClick(ref bool AdjustKillCooldown, ref bool? ResetCooldown)
    {
        AdjustKillCooldown = false;
        ResetCooldown = false;

        if (isRecording || isRewinding) return;
        if (!Player.IsAlive()) return;

        isRecording = true;
        recordTimer = 0f;
        positionHistory.Clear();
        rewindSkipPlayers.Clear();

        foreach (var pc in PlayerCatch.AllAlivePlayerControls)
        {
            positionHistory[pc.PlayerId] = new List<Vector2>();

            // ★ REC開始時点で梯子・ぬーん・ジップライン使用中のプレイヤーをスキップリストに追加
            if (pc.MyPhysics.Animations.IsPlayingAnyLadderAnimation())
            {
                rewindSkipPlayers.Add(pc.PlayerId);
                continue;
            }
            if ((MapNames)Main.NormalOptions.MapId == MapNames.Airship
                && Vector2.Distance(pc.GetTruePosition(), new Vector2(7.76f, 8.56f)) <= 1.9f)
            {
                rewindSkipPlayers.Add(pc.PlayerId);
            }
        }

        UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);
        SendRpc();
    }

    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!Player.IsAlive()) return;

        // ★ 記録フェーズ
        if (isRecording)
        {
            recordTimer += Time.fixedDeltaTime;

            if (recordTimer % 0.1f < Time.fixedDeltaTime)
            {
                foreach (var pc in PlayerCatch.AllAlivePlayerControls)
                {
                    if (!positionHistory.ContainsKey(pc.PlayerId))
                        positionHistory[pc.PlayerId] = new List<Vector2>();

                    var pos = pc.GetTruePosition();
                    pos.y += 0.47f;
                    positionHistory[pc.PlayerId].Add(pos);
                }
            }

            if (recordTimer >= RecordDuration)
            {
                // 記録終了 → 巻き戻し開始
                isRecording = false;
                isRewinding = true;
                rewindTimer = 0f;
                rewindIndex = positionHistory.ContainsKey(Player.PlayerId)
                    ? positionHistory[Player.PlayerId].Count - 1
                    : 0;
                UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);
                SendRpc();
            }
            return;
        }

        // ★ 巻き戻しフェーズ
        if (isRewinding)
        {
            rewindTimer += Time.fixedDeltaTime * RewindSpeed;

            if (rewindTimer >= 0.1f)
            {
                rewindTimer = 0f;

                if (rewindIndex < 0)
                {
                    // 巻き戻し完了
                    isRewinding = false;
                    positionHistory.Clear();
                    rewindSkipPlayers.Clear();
                    Player.RpcResetAbilityCooldown();
                    UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);
                    SendRpc();
                    return;
                }

                foreach (var kvp in positionHistory)
                {
                    var pc = PlayerCatch.GetPlayerById(kvp.Key);
                    if (pc == null || !pc.IsAlive()) continue;
                    if (rewindIndex >= kvp.Value.Count) continue;

                    // ★ REC開始時にスキップ登録されていたプレイヤーはワープしない
                    if (rewindSkipPlayers.Contains(pc.PlayerId)) continue;

                    // ★ 巻き戻し中に梯子・ジップライン使用中もスキップ
                    if (pc.MyPhysics.Animations.IsPlayingAnyLadderAnimation()) continue;
                    if ((MapNames)Main.NormalOptions.MapId == MapNames.Airship
                        && Vector2.Distance(pc.GetTruePosition(), new Vector2(7.76f, 8.56f)) <= 1.9f) continue;

                    var targetPos = kvp.Value[rewindIndex];
                    pc.RpcSnapToForced(targetPos, SendOption.None);
                }

                rewindIndex--;
            }
        }
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        isRecording = false;
        isRewinding = false;
        recordTimer = 0f;
        rewindTimer = 0f;
        rewindIndex = 0;
        positionHistory.Clear();
        rewindSkipPlayers.Clear();
        SendRpc();
    }

    public override void OnStartMeeting()
    {
        isRecording = false;
        isRewinding = false;
        recordTimer = 0f;
        rewindTimer = 0f;
        rewindIndex = 0;
        positionHistory.Clear();
        rewindSkipPlayers.Clear();
    }

    public override void AfterMeetingTasks()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!Player.IsAlive()) return;
        AURoleOptions.PhantomCooldown = PhantomCooldown;
        Player.RpcResetAbilityCooldown();
    }

    void SendRpc()
    {
        using var sender = CreateSender();
        sender.Writer.Write(isRecording);
        sender.Writer.Write(isRewinding);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        isRecording = reader.ReadBoolean();
        isRewinding = reader.ReadBoolean();
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (seen.PlayerId != seer.PlayerId || isForMeeting || !Player.IsAlive()) return "";
        if (isRecording)
        {
            var remaining = RecordDuration - recordTimer;
            return $"{(isForHud ? "" : "<size=60%>")}<color=#a0c4ff>記録中... {remaining:F1}s</color>";
        }
        if (isRewinding)
            return $"{(isForHud ? "" : "<size=60%>")}<color=#a0c4ff>巻き戻し中...</color>";
        return $"{(isForHud ? "" : "<size=60%>")}<color=#a0c4ff>ファントムボタン → タイムスリープ</color>";
    }

    public override string GetProgressText(bool comms = false, bool GameLog = false)
    {
        if (isRecording) return $"<color=#a0c4ff>●REC</color>";
        if (isRewinding) return $"<color=#a0c4ff>◀◀</color>";
        return "";
    }

    public override string GetAbilityButtonText() => "タイムスリープ";
}