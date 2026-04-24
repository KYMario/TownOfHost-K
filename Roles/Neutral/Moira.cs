using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Hazel;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Modules.SelfVoteManager;
using static TownOfHost.PlayerCatch;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Neutral;

public sealed class Moira : RoleBase, ISelfVoter
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Moira),
            player => new Moira(player),
            CustomRoles.Moira,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            70500,
            SetupOptionItem,
            "mo",
            "#c084fc",
            (6, 3),
            true,
            from: From.SuperNewRoles,
            assignInfo: new RoleAssignInfo(CustomRoles.Moira, CustomRoleTypes.Neutral)
            {
                AssignCountRule = new(1, 1, 1)
            }
        );

    public Moira(PlayerControl player)
        : base(RoleInfo, player, () => HasTask.False)
    {
        MaxSwaps = OptionMaxSwaps.GetInt();
        SwapVotes = OptionSwapVotes.GetBool();
        remainingSwaps = MaxSwaps;
        target1 = byte.MaxValue;
        target2 = byte.MaxValue;
        isRevealed = false;
        usedThisMeeting = false;
        swapHistory = new();
    }

    static OptionItem OptionMaxSwaps;
    static int MaxSwaps;
    static OptionItem OptionSwapVotes;
    static bool SwapVotes;

    int remainingSwaps;
    byte target1;
    byte target2;
    bool isRevealed;
    bool usedThisMeeting;

    // (player1Id, player2Id, role1Before, role2Before)
    List<(byte, byte, CustomRoles, CustomRoles)> swapHistory;

    // ★ 今ターンスワップしたペア（シスメ用）
    (byte id1, byte id2) pendingSwapMsg = (byte.MaxValue, byte.MaxValue);

    enum OptionName
    {
        MoiraMaxSwaps,
        MoiraSwapVotes,
    }

    private static void SetupOptionItem()
    {
        OptionMaxSwaps = IntegerOptionItem.Create(RoleInfo, 10, OptionName.MoiraMaxSwaps, new(1, 10, 1), 3, false)
            .SetValueFormat(OptionFormat.Times);
        OptionSwapVotes = BooleanOptionItem.Create(RoleInfo, 11, OptionName.MoiraSwapVotes, false, false);
    }

    bool ISelfVoter.CanUseVoted() => remainingSwaps > 0 && Player.IsAlive() && !usedThisMeeting;

    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (!Is(voter)) return true;
        if (remainingSwaps <= 0 || usedThisMeeting) return true;

        // ★ モードON中・1人目選択済み・2人目未設定 → 直接2人目登録
        if (CheckVote.TryGetValue(Player.PlayerId, out var inMode) && inMode && target1 != byte.MaxValue && target2 == byte.MaxValue)
        {
            if (votedForId == Player.PlayerId || votedForId == byte.MaxValue)
            {
                target1 = byte.MaxValue;
                target2 = byte.MaxValue;
                SetMode(Player, false);
                Utils.SendMessage(GetString("VoteSkillFin"), Player.PlayerId);
                SendRPC();
                return false;
            }
            RegisterTarget(votedForId);
            SendRPC();
            return false;
        }

        if (CheckSelfVoteMode(Player, votedForId, out var status))
        {
            switch (status)
            {
                case VoteStatus.Self:
                    target1 = byte.MaxValue;
                    target2 = byte.MaxValue;
                    Utils.SendMessage(
                        string.Format(GetString("SkillMode"), GetString("Mode.Moira"), GetString("Vote.Moira"))
                        + GetString("VoteSkillMode"),
                        Player.PlayerId);
                    SetMode(Player, true);
                    break;

                case VoteStatus.Skip:
                    target1 = byte.MaxValue;
                    target2 = byte.MaxValue;
                    Utils.SendMessage(GetString("VoteSkillFin"), Player.PlayerId);
                    SetMode(Player, false);
                    break;

                case VoteStatus.Vote:
                    RegisterTarget(votedForId);
                    break;
            }
            SendRPC();
            return false;
        }
        return true;
    }

    void RegisterTarget(byte id)
    {
        var target = GetPlayerById(id);
        if (target == null || !target.IsAlive()) return;

        if (target1 == byte.MaxValue)
        {
            target1 = id;
            Utils.SendMessage(
                $"<color={RoleInfo.RoleColorCode}>【運命改変】</color>\n" +
                $"1人目: {UtilsName.GetPlayerColor(target, true)} を選択しました。\n" +
                $"次に2人目に投票してください。",
                Player.PlayerId);
        }
        else if (id != target1)
        {
            target2 = id;
            usedThisMeeting = true;
            SetMode(Player, false);
            Utils.SendMessage(
                $"<color={RoleInfo.RoleColorCode}>【運命改変】</color>\n" +
                $"✓ {UtilsName.GetPlayerColor(GetPlayerById(target1), true)} と " +
                $"{UtilsName.GetPlayerColor(target, true)} の運命改変を確定しました！\n" +
                $"会議終了後に役職が入れ替わります。",
                Player.PlayerId);
        }
        SendRPC();
    }

    public override void OnStartMeeting()
    {
        target1 = byte.MaxValue;
        target2 = byte.MaxValue;
        SendRPC();
    }

    public override void AfterMeetingTasks()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        usedThisMeeting = false;

        if (target1 == byte.MaxValue || target2 == byte.MaxValue)
        {
            target1 = byte.MaxValue;
            target2 = byte.MaxValue;
            SendRPC();
            return;
        }

        ExecuteSwap(target1, target2);
        target1 = byte.MaxValue;
        target2 = byte.MaxValue;
        SendRPC();
    }

    void ExecuteSwap(byte id1, byte id2)
    {
        var p1 = GetPlayerById(id1);
        var p2 = GetPlayerById(id2);
        if (p1 == null || p2 == null) return;
        if (!p1.IsAlive() || !p2.IsAlive()) return;

        var role1 = p1.GetCustomRole();
        var role2 = p2.GetCustomRole();

        swapHistory.Add((id1, id2, role1, role2));

        // ★ 役職の入れ替え
        if (!Utils.RoleSendList.Contains(id1)) Utils.RoleSendList.Add(id1);
        if (!Utils.RoleSendList.Contains(id2)) Utils.RoleSendList.Add(id2);
        p1.RpcSetCustomRole(role2, true, log: null);
        p2.RpcSetCustomRole(role1, true, log: null);

        remainingSwaps--;

        // ★ 使い切ったら全員に公開
        if (remainingSwaps <= 0 && !isRevealed)
        {
            isRevealed = true;
            Utils.SendMessage(
                string.Format(GetString("MoiraRevealed"), UtilsName.GetPlayerColor(Player, true)));
        }

        // ★ タスクを再割り当て（スワップ）
        SwapTaskState(p1, p2);

        // ★ シスメはOnExileWrapUpで送るためここでは保留
        pendingSwapMsg = (id1, id2);

        UtilsGameLog.AddGameLog("Moira",
            $"{UtilsName.GetPlayerColor(Player)}が{UtilsName.GetPlayerColor(p1)}と{UtilsName.GetPlayerColor(p2)}の役職を入れ替えた");

        SendRPC();
        UtilsNotifyRoles.NotifyRoles();
    }

    // ★ タスクの完了フラグを交換してスワップ
    static void SwapTaskState(PlayerControl p1, PlayerControl p2)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        var tasks1 = p1.Data.Tasks?.ToArray();
        var tasks2 = p2.Data.Tasks?.ToArray();
        if (tasks1 == null || tasks2 == null) return;

        // ★ 完了フラグを交換
        int minLen = Math.Min(tasks1.Length, tasks2.Length);
        for (int i = 0; i < minLen; i++)
        {
            (tasks1[i].Complete, tasks2[i].Complete) = (tasks2[i].Complete, tasks1[i].Complete);
        }

        p1.MarkDirtySettings();
        p2.MarkDirtySettings();
        GameManager.Instance.CheckTaskCompletion();
    }

    // ★ 投票結果画面（OnExileWrapUp）でシスメを送る → 追放アニメの直後に表示される
    public override void OnExileWrapUp(NetworkedPlayerInfo exiled, ref bool DecidedWinner)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        // ★ スワップのシスメ
        if (pendingSwapMsg.id1 != byte.MaxValue && pendingSwapMsg.id2 != byte.MaxValue)
        {
            var p1 = GetPlayerById(pendingSwapMsg.id1);
            var p2 = GetPlayerById(pendingSwapMsg.id2);
            if (p1 != null && p2 != null)
            {
                string swapMsg =
                    $"<color={RoleInfo.RoleColorCode}>【運命改変】</color>\n" +
                    $"{UtilsName.GetPlayerColor(p1, true)} と " +
                    $"{UtilsName.GetPlayerColor(p2, true)} の運命が入れ替わった！";
                Utils.SendMessage(swapMsg);
            }
            pendingSwapMsg = (byte.MaxValue, byte.MaxValue);
        }

        // ★ モイラ本人が追放されたら全スワップを逆順で元に戻す
        if (exiled != null && exiled.PlayerId == Player.PlayerId)
        {
            foreach (var (id1, id2, role1, role2) in Enumerable.Reverse(swapHistory))
            {
                var p1 = GetPlayerById(id1);
                var p2 = GetPlayerById(id2);
                if (p1 != null)
                {
                    if (!Utils.RoleSendList.Contains(id1)) Utils.RoleSendList.Add(id1);
                    p1.RpcSetCustomRole(role1, true, log: null);
                }
                if (p2 != null)
                {
                    if (!Utils.RoleSendList.Contains(id2)) Utils.RoleSendList.Add(id2);
                    p2.RpcSetCustomRole(role2, true, log: null);
                }
            }
            swapHistory.Clear();

            Utils.SendMessage(GetString("MoiraExiledRevert"));
            UtilsNotifyRoles.NotifyRoles();
        }
    }

    // ★ 単独勝利判定（CheckWinnerから呼ぶ）
    public override void CheckWinner(GameOverReason reason)
    {
        if (!Player.IsAlive()) return;
        if (remainingSwaps > 0) return;

        // ★ 全改変を使い切って生存→単独勝利
        if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Moira, Player.PlayerId))
        {
            CustomWinnerHolder.NeutralWinnerIds.Add(Player.PlayerId);
        }
    }

    public override string GetProgressText(bool comms = false, bool GameLog = false)
    {
        if (isRevealed) return $"<color={RoleInfo.RoleColorCode}>【公開済】</color>";
        return $"<color={RoleInfo.RoleColorCode}>({remainingSwaps})</color>";
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null,
        bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (!Is(seer) || seer.PlayerId != seen.PlayerId || !Player.IsAlive()) return "";
        if (!isForMeeting) return "";

        string size = isForHud ? "" : "<size=60%>";
        string color = RoleInfo.RoleColorCode;

        if (remainingSwaps <= 0)
            return $"{size}<color={color}>運命改変使用済み。生存で勝利！</color>";
        if (usedThisMeeting)
            return $"{size}<color={color}>この会議は使用済み</color>";
        if (target1 != byte.MaxValue)
            return $"{size}<color={color}>2人目に投票して入れ替え確定</color>";
        return $"{size}<color={color}>自投票→運命改変モード | 残り{remainingSwaps}回</color>";
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (!isForMeeting) return "";
        if (!Is(seer) || !Player.IsAlive()) return "";
        if (seen.PlayerId == Player.PlayerId) return "";

        string color = RoleInfo.RoleColorCode;
        if (seen.PlayerId == target1) return $" <color={color}>①</color>";
        if (seen.PlayerId == target2) return $" <color={color}>②</color>";
        return "";
    }

    public override bool GetTemporaryName(ref string name, ref bool NoMarker, bool isForMeeting, PlayerControl seer, PlayerControl seen = null)
    {
        seen ??= seer;
        if (!isRevealed) return false;
        if (!Is(seen)) return false;
        name = $"<color={RoleInfo.RoleColorCode}>【モイラ】</color>{seen.Data.PlayerName}";
        return true;
    }

    void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(remainingSwaps);
        sender.Writer.Write(target1);
        sender.Writer.Write(target2);
        sender.Writer.Write(isRevealed);
        sender.Writer.Write(usedThisMeeting);
        sender.Writer.Write(swapHistory.Count);
        foreach (var (id1, id2, r1, r2) in swapHistory)
        {
            sender.Writer.Write(id1);
            sender.Writer.Write(id2);
            sender.Writer.WritePacked((int)r1);
            sender.Writer.WritePacked((int)r2);
        }
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        remainingSwaps = reader.ReadInt32();
        target1 = reader.ReadByte();
        target2 = reader.ReadByte();
        isRevealed = reader.ReadBoolean();
        usedThisMeeting = reader.ReadBoolean();
        int count = reader.ReadInt32();
        swapHistory.Clear();
        for (int i = 0; i < count; i++)
        {
            var id1 = reader.ReadByte();
            var id2 = reader.ReadByte();
            var r1 = (CustomRoles)reader.ReadPackedInt32();
            var r2 = (CustomRoles)reader.ReadPackedInt32();
            swapHistory.Add((id1, id2, r1, r2));
        }
    }
}