using System.Collections.Generic;
using AmongUs.GameOptions;
using Hazel;
using UnityEngine;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.PlayerCatch;
using static TownOfHost.Utils;
using static TownOfHost.Translator;
using static TownOfHost.Modules.SelfVoteManager;

namespace TownOfHost.Roles.Neutral;

public sealed class Onmyoji : RoleBase, ISelfVoter
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
            from: From.SuperNewRoles
        );

    public Onmyoji(PlayerControl player)
        : base(RoleInfo, player, () => HasTask.True)
    {
        WinTaskCount = OptWinTaskCount.GetInt();
        MyTaskState.NeedTaskCount = WinTaskCount;

        ShikigamiIds = new();
        checktaskwinflag = false;

        NextShikigamiCandidate = byte.MaxValue;
        nearTimer = 0f;
        hasSpawned = false;

        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
    }

    static OptionItem OptWinTaskCount;

    static int WinTaskCount;

    public List<byte> ShikigamiIds;
    bool checktaskwinflag;

    // ★ スポーン済みかどうか
    bool hasSpawned = false;

    // ★ 式神候補に3秒間近づいている時間
    float nearTimer = 0f;

    // ★ 会議で選ばれた式神候補
    public byte NextShikigamiCandidate;

    enum OptionName
    {
        OnmyojiWinTaskCount,
    }

    private static void SetupOptionItem()
    {
        SoloWinOption.Create(RoleInfo, 9, defo: 15);
        OptWinTaskCount = IntegerOptionItem.Create(RoleInfo, 15, OptionName.OnmyojiWinTaskCount, new(1, 99, 1), 5, false)
            .SetValueFormat(OptionFormat.Times);
        OverrideTasksData.Create(RoleInfo, 20);
    }

    // ★ スポーンした瞬間に呼ばれる（TOH-P 正しいシグネチャ）
    public override void OnSpawn(bool initialState)
    {
        hasSpawned = true;
    }

    // ★ 会議で能力を使えるかどうか
    bool ISelfVoter.CanUseVoted()
        => Player.IsAlive() && ShikigamiIds.Count < 1;

    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (!Is(voter)) return true;
        if (!Player.IsAlive()) return true;
        if (ShikigamiIds.Count >= 1) return true;

        if (CheckSelfVoteMode(Player, votedForId, out var status))
        {
            // ★ 自投票 → モード開始（システムメッセージ）
            if (status is VoteStatus.Self)
            {
                Utils.SendMessage(
                    "<color=#9b59b6>式神選択モードになりました！</color>\n\n" +
                    "誰かに投票 → <color=#9b59b6>式神候補に指定</color>\n" +
                    "自投票 → <color=#9b59b6>自身に投票</color>\n" +
                    "投票スキップ → <color=#9b59b6>式神選択をキャンセル</color>",
                    Player.PlayerId
                );

                SetMode(Player, true);
                return false;
            }

            // ★ 他プレイヤーに投票 → 式神候補に設定
            if (status is VoteStatus.Vote)
            {
                if (votedForId == Player.PlayerId || votedForId == SkipId)
                {
                    Utils.SendMessage("<color=#9b59b6>その相手には式神を付けられません。</color>", Player.PlayerId);
                    SetMode(Player, false);
                    return false;
                }

                NextShikigamiCandidate = votedForId;

                Utils.SendMessage(
                    "<color=#9b59b6>式神候補を設定しました！</color>\n" +
                    "次のターン、この相手に近づくと式神になります。",
                    Player.PlayerId
                );

                SetMode(Player, false);
                return false;
            }

            // ★ スキップ → キャンセル
            if (status is VoteStatus.Skip)
            {
                NextShikigamiCandidate = byte.MaxValue;

                Utils.SendMessage(
                    "<color=#9b59b6>式神選択をキャンセルしました。</color>",
                    Player.PlayerId
                );

                SetMode(Player, false);
                return false;
            }
        }

        return true;
    }

    public override void OnStartMeeting()
    {
        NextShikigamiCandidate = byte.MaxValue;
    }

    public override void AfterMeetingTasks()
    {
        hasSpawned = false; // ★ 会議後はスポーン前扱いに戻す

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Player.IsAlive()) return;

        foreach (var id in ShikigamiIds)
        {
            var sk = GetPlayerById(id);
            if (sk == null || !sk.IsAlive()) continue;
            TargetArrow.Add(Player.PlayerId, id);
        }
    }

    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!GameStates.IsInTask) return;
        if (!Player.IsAlive()) return;

        // ★ スポーンするまで式神指名処理を無効化
        if (!hasSpawned) return;

        if (ShikigamiIds.Count >= 1) return;
        if (NextShikigamiCandidate == byte.MaxValue) return;

        var target = GetPlayerById(NextShikigamiCandidate);
        if (target == null || !target.IsAlive())
        {
            nearTimer = 0f;
            return;
        }

        float dist = Vector2.Distance(Player.GetTruePosition(), target.GetTruePosition());
        if (dist <= 1.0f)
        {
            nearTimer += Time.fixedDeltaTime;

            if (nearTimer >= 3f)
            {
                AddShikigami(target);
                NextShikigamiCandidate = byte.MaxValue;
                nearTimer = 0f;
            }
        }
        else
        {
            nearTimer = 0f;
        }
    }

    void AddShikigami(PlayerControl target)
    {
        if (ShikigamiIds.Count >= 1) return;

        ShikigamiIds.Add(target.PlayerId);

        TargetArrow.Add(Player.PlayerId, target.PlayerId);

        if (!Utils.RoleSendList.Contains(target.PlayerId))
            Utils.RoleSendList.Add(target.PlayerId);

        target.RpcSetCustomRole(CustomRoles.Shikigami, log: null);
        if (target.GetRoleClass() is Shikigami sk)
            sk.SetOwner(Player.PlayerId);

        NameColorManager.Add(Player.PlayerId, target.PlayerId, "#9b59b6");

        Player.RpcSetRoleDesync(RoleTypes.Engineer, Player.GetClientId());

        SendRPC();
        _ = new LateTask(() => UtilsNotifyRoles.NotifyRoles(), 0.2f, "Onmyoji Shikigami");
    }

    public override bool OnCompleteTask(uint taskid)
    {
        if (MyTaskState.HasCompletedEnoughCountOfTasks(WinTaskCount))
            checktaskwinflag = true;
        return true;
    }

    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (seen.PlayerId == seer.PlayerId) return "";

        foreach (var pc in AllPlayerControls)
        {
            if (pc.GetRoleClass() is not Onmyoji onmyoji) continue;
            if (pc.PlayerId != seer.PlayerId) continue;
            if (!pc.IsAlive()) return "";

            if (seen.GetRoleClass() is not IKiller) return "";

            var roleType = seen.GetCustomRole().GetCustomRoleTypes();
            string color = roleType switch
            {
                CustomRoleTypes.Impostor => "#ff0000",
                _ => UtilsRoleText.GetRoleColorCode(seen.GetCustomRole())
            };

            return $"<color={color}>★</color>";
        }
        return "";
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
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
            arrows += TargetArrow.GetArrows(seer, id);
        }
        return arrows == "" ? "" : $"<color=#9b59b6>{arrows}</color>";
    }

    public override void OnMurderPlayerAsTarget(MurderInfo info)
    {
        // ★ 陰陽師が死んでも式神は死なない（しぇとこ仕様）
        // 何もしない
    }

    public static bool CheckWinStatic(ref GameOverReason reason)
    {
        foreach (var pc in AllPlayerControls)
        {
            if (pc.GetRoleClass() is not Onmyoji onmyoji) continue;
            if (!pc.IsAlive()) continue;
            if (!onmyoji.checktaskwinflag) continue;

            if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Onmyoji, pc.PlayerId))
            {
                CustomWinnerHolder.NeutralWinnerIds.Add(pc.PlayerId);
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
        var color = checktaskwinflag ? "#9b59b6" : "#5e5e5e";
        var skCount = ShikigamiIds.Count;
        return $"<color={color}>式:{skCount}/1</color>";
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

    public override string GetAbilityButtonText() => GetString("OnmyojiAbilityButtonText");
}
