using AmongUs.GameOptions;
using System;
using System.Linq;
using System.Collections.Generic;

using TownOfHost.Roles.Core;

using static TownOfHost.Modules.SelfVoteManager;
using static TownOfHost.Modules.MeetingVoteManager;
using static TownOfHost.Modules.MeetingTimeManager;
using Hazel;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Crewmate;

public sealed class Balancer : RoleBase, ISelfVoter
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Balancer),
            player => new Balancer(player),
            CustomRoles.Balancer,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            10300,
            SetupOptionItem,
            "bal",
            "#cff100",
            (3, 8),
            introSound: () => GetIntroSound(RoleTypes.Crewmate),
            from: From.SuperNewRoles
        );
    public Balancer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        meetingtime = OptionMeetingTime.GetInt();
        CanUseAllAlive = OptionCanUseAllAlive.GetBool();
        target1 = 255;
        target2 = 255;
        Target1 = 255;
        Target2 = 255;
        used = false;
        Id = 255;
        nickname = null;
        CustomRoleManager.MarkOthers.Add(OtherMark);
    }

    static OptionItem OptionMeetingTime;
    static OptionItem OptionCanUseAllAlive;
    public static OptionItem OptionCanMeetingAbility;
    //共有用
    public static byte target1 = 255, target2 = 255;
    public static byte Id = 255;
    public static int meetingtime;
    static string nickname;
    static bool CanUseAllAlive; //誰かが死亡するまで、能力を使えない
    //プレイヤーによって操作できる
    byte Target1, Target2;
    bool used;

    enum Option
    {
        BalancerMeetingTime,
        BalancerCanUseAllAlive,
        BalancerCanUseMeetingAbility
    }

    private static void SetupOptionItem()
    {
        OptionMeetingTime = IntegerOptionItem.Create(RoleInfo, 10, Option.BalancerMeetingTime, new(15, 120, 1), 30, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionCanUseAllAlive = BooleanOptionItem.Create(RoleInfo, 11, Option.BalancerCanUseAllAlive, false, false);
        OptionCanMeetingAbility = BooleanOptionItem.Create(RoleInfo, 12, Option.BalancerCanUseMeetingAbility, false, false);
    }

    public override void OnDestroy()
    {
        Id = byte.MaxValue;
        target1 = byte.MaxValue;
        target2 = byte.MaxValue;

        if (AmongUsClient.Instance.AmHost)
        {
            if (Target1 != byte.MaxValue && Target2 != byte.MaxValue)
            {
                PlayerCatch.GetPlayerById(Target1).RpcSetName(Main.AllPlayerNames[Target1]);
                PlayerCatch.GetPlayerById(Target2).RpcSetName(Main.AllPlayerNames[Target2]);
            }
            if (nickname != null)
                Main.nickName = nickname;
            nickname = null;
        }
    }
    bool ISelfVoter.CanUseVoted() => Canuseability() && !used && Id is not 255 && (CanUseAllAlive || GameStates.AlreadyDied);
    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (!Canuseability()) return true;
        //誰かが天秤を発動していて、自分ではないなら実行しない
        if (Id is not 255 && Id != Player.PlayerId) return true;
        //発動してるなら～
        if (Id is not 255)
        {
            //投票先が天秤のターゲットではないなら投票しない
            if (votedForId == Target1 || votedForId == Target2)
                return true;
            return false;
        }

        //通常会議の処理 投票した人が自分ではない or 能力使用済みならここから先は実行しない
        if (voter.PlayerId != Player.PlayerId || used || (CanUseAllAlive && !GameStates.AlreadyDied))
            return true;

        //天秤モードかチェック
        if (CheckSelfVoteMode(Player, votedForId, out var status))
        {
            if (status is VoteStatus.Self)
            {
                //ターゲットの情報をリセット
                Target1 = 255;
                Target2 = 255;
                Utils.SendMessage(string.Format(GetString("SkillMode"), GetString("Mode.Balancer"), GetString("Vote.Balancer")) + GetString("VoteSkillMode"), Player.PlayerId);
                //正直前のメッセージの方が好きby ky
                //↑ﾏｴﾉｯﾃﾅﾆ..._(:3 」∠)_ byʕ⓿ᴥ⓿ʔ
            }
            if (status is VoteStatus.Skip)
            {
                SetMode(Player, false);
                Utils.SendMessage(GetString("VoteSkillFin"), Player.PlayerId);
            }
            //選ぶ処理
            if (status is VoteStatus.Vote)
            {
                Vote();
            }
            return false;
        }
        else
        {
            if (votedForId == Player.PlayerId && ((Target1 != 255 && Target2 == 255) || (Target1 == 255 && Target2 != 255)))
            {
                Vote();
                return false;
            }
        }
        return true;

        void Vote()
        {
            //1一目が決まってないなら一人目を決める
            if (Target1 == 255)
                Target1 = votedForId;
            //二人目が決まってないなら二人目を決める
            else if (Target2 == 255)
                Target2 = votedForId;

            //同じ人なら二人目をリセット
            if (Target1 == Target2)
                Target2 = 255;

            //プレイヤーの状態を取得
            var p1 = PlayerCatch.GetPlayerById(Target1);
            var p2 = PlayerCatch.GetPlayerById(Target2);

            //切断or死んでいるならリセット
            if (!p1.IsAlive())
                Target1 = 255;
            if (!p2.IsAlive())
                Target2 = 255;

            //どちらかの情報があるならチャットで伝える
            if (Target1 != 255 || Target2 != 255)
            {
                //どちらかが決まっていなかったら一人目
                var Nowtargetcount = (Target1 != 255 && Target2 != 255) ? GetString("TowPlayer") : GetString("OnePlayer");
                var sendtext = string.Format(GetString("Skill.Balancer"), Nowtargetcount, UtilsName.GetPlayerColor(PlayerCatch.GetPlayerById(votedForId), true));
                Utils.SendMessage(sendtext, Player.PlayerId);
            }

            //二人決まったなら会議を終了
            if (Target1 != 255 && Target2 != 255)
            {
                byte[] random_target = [Target1, Target2];
                random_target = [.. random_target.OrderBy(x => Guid.NewGuid())];
                Voteresult = "<color=#cff100>☆" + GetString("BalancerMeeting") + "☆</color>\n" + string.Format(GetString("BalancerMeetingInfo"), UtilsName.GetPlayerColor(PlayerCatch.GetPlayerById(random_target[0]), true), UtilsName.GetPlayerColor(PlayerCatch.GetPlayerById(random_target[1]), true));
                used = true;
                target1 = Target1;
                target2 = Target2;

                using var sender = CreateSender();
                sender.Writer.Write(target1);
                sender.Writer.Write(target2);

                ExileControllerWrapUpPatch.AntiBlackout_LastExiled = null;
                Modules.MeetingVoteManager.Instance.ClearAndEndMeeting();
                MeetingHud.Instance.RpcClose();
                GameStates.ExiledAnimate = true;
            }
        }
    }

    public override bool VotingResults(ref NetworkedPlayerInfo Exiled, ref bool IsTie, Dictionary<byte, int> vote, byte[] mostVotedPlayers, bool ClearAndExile)
    {
        //天秤会議開始時は処理スキップ
        if (Id == 255 && Target1 != 255 && Target2 != 255) return true;

        //天秤モードじゃないor自分の天秤じゃないなら実行しない
        if (Id != Player.PlayerId) return false;

        //ディクテーターなどの強制的に会議を終わらせるものなら生存確認の処理スキップ
        if (!ClearAndExile)
        {
            var d1 = PlayerCatch.GetPlayerById(Target1);
            var d2 = PlayerCatch.GetPlayerById(Target2);

            //二人とも切断or死んでいるなら同数
            if (!d1.IsAlive() && !d2.IsAlive())
            {
                IsTie = true;
                Exiled = null;
                return true;
            }
            IsTie = false;
            //チェック
            if (!d1.IsAlive())
            {
                Exiled = d2.Data;
                vote[d2.PlayerId] = PlayerCatch.AllAlivePlayersCount;
                return true;
            }
            if (!d2.IsAlive())
            {
                Exiled = d1.Data;
                vote[d1.PlayerId] = PlayerCatch.AllAlivePlayersCount;
                return true;
            }
        }

        var rand = new Random();
        Dictionary<byte, int> data = new(2)
        {
            //セット
            { Target1, 0 },
            { Target2, 0 }
        };

        //投票をカウント、投票してない場合はどちらかに投票させる
        foreach (var voteData in Instance.AllVotes)
        {
            var voted = voteData.Value;

            //死んでたらスキップ
            if (!PlayerCatch.GetPlayerById(voted.Voter).IsAlive()) continue;

            //ディクテーターなどの強制的に会議を終わらせるものではないならランダム投票
            if (!voted.HasVoted && !ClearAndExile)
            {
                var id = rand.Next(0, 2) is 0 ? Target1 : Target2;
                Instance.SetVote(voted.Voter, id, isIntentional: false);
                data[id] += 1;
            }
            else if (voted.VotedFor is not NoVote) //投票なし(死亡時、会議強制終了時など)の人はスキップ
            {
                data[voted.VotedFor] += voted.NumVotes;
            }
        }
        //暗転対策の追放リセット
        ExileControllerWrapUpPatch.AntiBlackout_LastExiled = null;

        //ランダムで追放者を決める 同数ならどちらも追放
        var exileId = data.Where(kv => kv.Value == data.Values.Max())
                        .Select(kv => kv.Key)
                        .OrderBy(x => Guid.NewGuid())
                        .FirstOrDefault();
        Exiled = GameData.Instance.GetPlayerById(exileId);
        var ex = PlayerCatch.GetPlayerById(exileId);
        //どちらも追放の時?
        if (data.Values.Distinct().Count() == 1)
        {
            Exiled = null;
            //追放画面が出てくるちょい前に名前を変える
            _ = new LateTask(() =>
            {
                //ホストなら別の処理
                if (exileId is 0)
                {
                    nickname = Main.nickName;
                    Main.nickName = GetString("Balancer.Executad") + "<size=0>";
                }
                else
                    ex.RpcSetName(GetString("Balancer.Executad") + "<size=0>");
            }, 4f, "dotiramotuihou☆");
            var toExile = data.Keys.ToArray();
            bool winnoyatu = false;
            foreach (var playerId in toExile)
            {
                ex?.SetRealKiller(null);
            }
            MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.Vote, toExile);
            Voteresult = GetString("Balancer.Executad");
            UtilsGameLog.AddGameLog($"Vote", GetString("Balancer.Executad"));

            foreach (var playerId in toExile)
            {
                CustomRoleManager.GetByPlayerId(playerId)?.OnExileWrapUp(PlayerCatch.GetPlayerInfoById(playerId), ref winnoyatu);
            }
        }
        return true;
    }

    public void BalancerAfterMeetingTasks()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        //天秤会議になってない状態なら
        if (Id == 255 && Target1 != 255 && Target2 != 255)
        {
            //天秤会議にする
            Id = Player.PlayerId;
            var ran = IRandom.Instance.Next(0, 2);
            var oniku = PlayerCatch.GetPlayerById(ran == 0 ? Target1 : Target2) ?? PlayerControl.LocalPlayer;
            var reporter = PlayerCatch.GetPlayerById(ran == 0 ? Target2 : Target1) ?? PlayerControl.LocalPlayer;

            foreach (var pc in PlayerCatch.AllPlayerControls.Where(pc => pc.PlayerId == Target1 || pc.PlayerId == Target2))
                pc.RpcSetName("<#ff1919>Ω<i><u>" + Utils.ColorString(RoleInfo.RoleColor, Main.AllPlayerNames[pc.PlayerId]) + "</u>");
            _ = new LateTask(() =>
            {
                _ = new LateTask(() => Utils.AllPlayerKillFlash(), 1f, "BMkillf", true);
                ReportDeadBodyPatch.ExReportDeadBody(reporter, oniku.Data, false, "Balancer.meeting", UtilsRoleText.GetRoleColorCode(CustomRoles.Balancer));
            }, 2f, "BalanerMeeting");

            //対象の名前を天秤の色に
            Balancer(meetingtime);

            _ = new LateTask(() =>
            {
                //名前を戻す
                UtilsGameLog.AddGameLog("Meeting", string.Format(GetString("BalancerMeetingInfo"), UtilsName.GetPlayerColor(PlayerCatch.GetPlayerById(Target1), true), UtilsName.GetPlayerColor(PlayerCatch.GetPlayerById(Target2), true)).Split("\n")[0] + "</color>");
                PlayerCatch.GetPlayerById(Target1)?.RpcSetName(Main.AllPlayerNames[Target1]);
                PlayerCatch.GetPlayerById(Target2)?.RpcSetName(Main.AllPlayerNames[Target2]);
            }, 2.8f);

            return;
        }
    }
    public static string OtherMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        seen ??= seer;
        if (!isForMeeting) return "";
        if (Id == byte.MaxValue) return "";
        if (seen.PlayerId == target1 || seen.PlayerId == target2)
        {
            return "<color=red>Ω</color>";
        }
        return "";
    }
    public override void AfterMeetingTasks()
    {
        //ホストのみ実行
        if (AmongUsClient.Instance.AmHost)
        {
            //自分の天秤会議じゃないなら実行しない
            if (Id != Player.PlayerId)
                return;

            //名前を戻す
            PlayerCatch.GetPlayerById(Target1)?.RpcSetName(Main.AllPlayerNames[Target1]);
            PlayerCatch.GetPlayerById(Target2)?.RpcSetName(Main.AllPlayerNames[Target2]);

            if (nickname != null)
                Main.nickName = nickname;
            nickname = null;

            //名前にロールとかのを適用
            _ = new LateTask(() => UtilsNotifyRoles.NotifyRoles(ForceLoop: true, NoCache: true), Main.LagTime);
        }

        //リセット
        Id = 255;
        Target1 = 255;
        Target2 = 255;
        target1 = 255;
        target2 = 255;
        return;
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (isForMeeting && Player.IsAlive() && (!GameStates.AlreadyDied || CanUseAllAlive) && seer.PlayerId == seen.PlayerId && Canuseability() && !used)
        {
            var mes = $"<color={RoleInfo.RoleColorCode}>{GetString("SelfVoteRoleInfoMeg")}</color>";
            return isForHud ? mes : $"<size=40%>{mes}</size>";
        }
        return "";
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        used = true;
        Id = Player.PlayerId;
        target1 = reader.ReadByte();
        target2 = reader.ReadByte();
    }
}