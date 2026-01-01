using AmongUs.GameOptions;
using Hazel;
using UnityEngine;
using System.Linq;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Modules.SelfVoteManager;

namespace TownOfHost.Roles.Impostor;

//メモ
//タゲ相手の狙われてる設定orタゲ相手が分かる設定追加する
//↑キルク変動つけたから、これでバランス見たい感じはある。

public sealed class ConnectSaver : RoleBase, IImpostor, ISelfVoter
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(ConnectSaver),
            player => new ConnectSaver(player),
            CustomRoles.ConnectSaver,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            4500,
            SetupOptionItem,
            "Cs",
            OptionSort: (3, 7)
        );
    public ConnectSaver(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        target1 = byte.MaxValue;
        target2 = byte.MaxValue;
        Max = OptionMaximum.GetFloat();
        usedcount = 0;
        IsUseing = false;
    }
    static OptionItem OptionKillCoolDown;

    static OptionItem OptionTageKillCoolDown;
    static OptionItem OptionMaximum;
    static OptionItem OptionMinimumPlayerCount;
    static OptionItem OptionDeathReason;
    byte target1;
    byte target2;
    static float Max;
    int usedcount;
    bool IsUseing;
    public static readonly CustomDeathReason[] deathReasons =
    {
        CustomDeathReason.Kill,CustomDeathReason.Suicide,CustomDeathReason.Revenge,CustomDeathReason.FollowingSuicide
    };
    enum OptionName
    {
        ConnectSaverPlayerCount, ConnectSaverDeathReason, ConnectSaverTageKillCooldown
    }
    private static void SetupOptionItem()
    {
        var cRolesString = deathReasons.Select(x => x.ToString()).ToArray();

        OptionKillCoolDown = FloatOptionItem.Create(RoleInfo, 9, GeneralOption.KillCooldown, new(0f, 180f, 0.5f), 30f, false).SetValueFormat(OptionFormat.Seconds);
        OptionTageKillCoolDown = FloatOptionItem.Create(RoleInfo, 10, OptionName.ConnectSaverTageKillCooldown, new(0f, 180f, 0.5f), 40f, false).SetValueFormat(OptionFormat.Seconds);
        OptionMaximum = IntegerOptionItem.Create(RoleInfo, 11, GeneralOption.OptionCount, new(1, 99, 1), 1, false).SetValueFormat(OptionFormat.Times);
        OptionMinimumPlayerCount = IntegerOptionItem.Create(RoleInfo, 12, OptionName.ConnectSaverPlayerCount, new(0, 15, 1), 6, false).SetValueFormat(OptionFormat.Players);
        OptionDeathReason = StringOptionItem.Create(RoleInfo, 13, OptionName.ConnectSaverDeathReason, cRolesString, 3, false);
    }
    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(usedcount);
        sender.Writer.Write(target1);
        sender.Writer.Write(target2);
        sender.Writer.Write(IsUseing);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        usedcount = reader.ReadInt32();
        target1 = reader.ReadByte();
        target2 = reader.ReadByte();
        IsUseing = reader.ReadBoolean();
    }

    public float CalculateKillCooldown() => IsUseing ? OptionTageKillCoolDown.GetFloat() : OptionKillCoolDown.GetFloat();
    public override void AfterMeetingTasks()
    {
        if (IsUseing) Main.AllPlayerKillCooldown[Player.PlayerId] = OptionTageKillCoolDown.GetFloat();
        else Main.AllPlayerKillCooldown[Player.PlayerId] = OptionKillCoolDown.GetFloat();

        if (!AmongUsClient.Instance.AmHost) return;

        Player.SyncSettings();
    }
    public override string GetProgressText(bool comms = false, bool gamelog = false) => Utils.ColorString(Max <= usedcount ? Color.gray : Palette.ImpostorRed, $"({Max - usedcount})");
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (PlayerCatch.AllAlivePlayersCount < OptionMinimumPlayerCount.GetInt()) return "";
        if (isForMeeting && Player.IsAlive() && seer.PlayerId == seen.PlayerId && Canuseability() && Max > usedcount)
        {
            var mes = $"<color={RoleInfo.RoleColorCode}>{GetString("SelfVoteRoleInfoMeg")}</color>";
            return isForHud ? mes : $"<size=40%>{mes}</size>";
        }
        return "";
    }
    bool ISelfVoter.CanUseVoted() => Canuseability() && !(PlayerCatch.AllAlivePlayersCount < OptionMinimumPlayerCount.GetInt()) && Max > usedcount && (target1 == byte.MaxValue || target2 == byte.MaxValue);
    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (!Canuseability()) return true;
        if (Madmate.MadAvenger.Skill) return true;
        if (PlayerCatch.AllAlivePlayersCount < OptionMinimumPlayerCount.GetInt()) return true;
        if (Is(voter) && Max > usedcount && (target1 == byte.MaxValue || target2 == byte.MaxValue))
        {
            if (CheckSelfVoteMode(Player, votedForId, out var status))
            {
                if (status is VoteStatus.Self)
                    Utils.SendMessage(string.Format(GetString("SkillMode"), GetString("Mode.ConnectSaver"), GetString("Vote.ConnectSaver")) + GetString("VoteSkillMode"), Player.PlayerId);
                if (status is VoteStatus.Skip)
                {
                    target1 = byte.MaxValue;
                    target2 = byte.MaxValue;
                    SetMode(Player, false);
                    Utils.SendMessage(GetString("VoteSkillFin"), Player.PlayerId);
                }
                if (status is VoteStatus.Vote)
                    Tagech();
                return false;
            }
            else
            {
                if (!IsUseing && voter.PlayerId != votedForId && Is(voter) && votedForId != SkipId && ((target1 != 255 && target2 == 255) || (target1 == 255 && target2 != 255)))
                {
                    Tagech();
                    return false;
                }
            }
        }
        return true;

        void Tagech()
        {
            if (votedForId == voter.PlayerId) return;

            if (target1 == byte.MaxValue) target1 = votedForId;
            else if (target2 == byte.MaxValue) target2 = votedForId;

            if (target1 == target2) target2 = byte.MaxValue;

            var targetpc1 = PlayerCatch.GetPlayerById(target1);
            var targetpc2 = PlayerCatch.GetPlayerById(target2);

            if (!targetpc1.IsAlive() || targetpc1 == null) target1 = byte.MaxValue;
            if (!targetpc2.IsAlive() || targetpc2 == null) target2 = byte.MaxValue;

            if (target1 != byte.MaxValue || target2 != byte.MaxValue)
            {
                var Nowtargetcount = (target1 != byte.MaxValue && target2 != byte.MaxValue) ? GetString("TowPlayer") : GetString("OnePlayer");
                var lasttext = string.Format(GetString("Skill.Balancer"), Nowtargetcount, UtilsName.GetPlayerColor(PlayerCatch.GetPlayerById(votedForId), true));
                Utils.SendMessage(lasttext.ToString(), Player.PlayerId);
            }
            if (target1 != byte.MaxValue && target2 != byte.MaxValue)
            {
                Utils.SendMessage(GetString("Skill.ConnectSaver"), Player.PlayerId);
                usedcount++;//成功or失敗にかかわらず発動はした記録。
                IsUseing = true;
                SendRPC();
            }
        }
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        if (!info.CanKill) return;
        var (killer, target) = info.AttemptTuple;

        if (info.IsFakeSuicide) return;
        if (IsUseing && ((target.PlayerId == target1 && target1 != byte.MaxValue) || (target.PlayerId == target2 && target2 != byte.MaxValue)))
        {
            if (Is(killer))
            {
                CheckMurderPatch.TimeSinceLastKill[killer.PlayerId] = 30f;//キル連打とかいう奴を無視する奴
                var targetid = target.PlayerId == target1 ? target2 : target1;
                var connecttarget = PlayerCatch.GetPlayerById(targetid);
                if (CustomRoleManager.OnCheckMurder(killer, connecttarget, connecttarget, connecttarget, true, Killpower: 10, deathReason: deathReasons[OptionDeathReason.GetValue()]))//一応殺した判定は貰うしガードとかいうの知らない。
                {
                    connecttarget.SetRealKiller(killer);
                }
            }
            target1 = byte.MaxValue;
            target2 = byte.MaxValue;
            IsUseing = false;
            SendRPC();
            CheckMurderPatch.TimeSinceLastKill[killer.PlayerId] = 0f;//無視してもキル連打はさせない。
            return;
        }

        if (IsUseing && !(target.PlayerId == target1 || target.PlayerId == target2)) info.DoKill = false;//ターゲット生存中は他のキルは不可
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        seen ??= seer;
        if (IsUseing && seer == Player)
        {
            if ((seen.PlayerId == target1 && target1 != byte.MaxValue) || (seen.PlayerId == target2 && target2 != byte.MaxValue))
            {
                return Utils.ColorString(Palette.Purple, GetString("CS.Target"));
            }
        }
        return "";
    }
    public override void OnReportDeadBody(PlayerControl repo, NetworkedPlayerInfo oniku)
    {
        //会議入ったらリセット
        target1 = byte.MaxValue;
        target2 = byte.MaxValue;
        IsUseing = false;
        SendRPC();
    }
}