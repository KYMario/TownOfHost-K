using UnityEngine;
using AmongUs.GameOptions;
using Hazel;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Madmate;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Crewmate;

public sealed class WhiteHacker : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(WhiteHacker),
            player => new WhiteHacker(player),
            CustomRoles.WhiteHacker,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            34100,
            SetupOptionItem,
            "WH",
            "#efefef"
        );

    public WhiteHacker(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        cantaskcount = Optioncantaskcount.GetFloat();
        P = 225;
        Max = OptionMaximum.GetFloat();
        cont = 0;
        use = false;
        kakusei = !Kakusei.GetBool();
    }

    private static OptionItem Optioncantaskcount;
    private static OptionItem OptionMaximum;

    static OptionItem Kakusei;
    bool kakusei;
    private static float cantaskcount;
    private int P;
    static float Max;
    float cont;
    bool use;
    enum Option
    {
        WhiteHackerTrackTimes,
    }

    private static void SetupOptionItem()
    {
        Optioncantaskcount = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.cantaskcount, new(0, 99, 1), 5, false);
        OptionMaximum = FloatOptionItem.Create(RoleInfo, 11, Option.WhiteHackerTrackTimes, new(1f, 99f, 1f), 1f, false)
            .SetValueFormat(OptionFormat.Times);
        Kakusei = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.UKakusei, true, false);
    }
    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(cont);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        cont = reader.ReadInt32();
    }
    private bool IsTrackTarget(PlayerControl target)
    => (Player.IsAlive() && target.IsAlive() && !Is(target)) || P == target.PlayerId;

    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (MadAvenger.Skill) return true;
        if (Is(voter) && (MyTaskState.CompletedTasksCount >= cantaskcount || IsTaskFinished) && Max > cont)
        {
            if (Player.PlayerId == votedForId || votedForId == 253)
            {
                P = 225;
                return true;
            }
            else
            {
                cont++;
                P = votedForId;
                use = true;
                Utils.SendMessage(string.Format(GetString("Skill.WhiteHacker"), Utils.GetPlayerColor(Utils.GetPlayerById(votedForId), true), Max - cont), Player.PlayerId);
                SendRPC();
                return true;
            }
        }
        return true;
    }
    public override void AfterMeetingTasks()
    {
        if (!use)
            P = 255;
    }
    public override void OnReportDeadBody(PlayerControl _, NetworkedPlayerInfo __) => use = false;
    // 表示系の関数群
    public override string GetSuffix(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        if (P == 225) return "";
        seen ??= seer;
        if (GameStates.Meeting && P == seen.PlayerId && MyTaskState.CompletedTasksCount >= cantaskcount)
        {
            var roomName = GetLastRoom(seen);
            // 空のときにタグを付けると，suffixが空ではない判定となりなにもない3行目が表示される
            return roomName.Length == 0 ? "" : $"<size=1.5>{roomName}</size>";
        }
        return "";
    }
    public override string GetProgressText(bool comms = false) => Utils.ColorString(MyTaskState.CompletedTasksCount < cantaskcount && !IsTaskFinished ? Color.gray : Max <= cont ? Color.gray : Color.cyan, $"({Max - cont})");

    public string GetLastRoom(PlayerControl seen)
    {
        if (P == 225 || !Player.IsAlive()) return "";
        if (!IsTrackTarget(seen) && P == seen.PlayerId) return "";
        var text = "";
        var room = PlayerState.GetByPlayerId(seen.PlayerId).LastRoom;
        if (room == null) text += Utils.ColorString(Color.gray, "@" + GetString("FailToTrack"));
        else
        {
            text += Utils.ColorString(Palette.LightBlue, "@" + GetString(room.RoomId.ToString()));
        }

        return text;
    }
    public override CustomRoles Jikaku() => kakusei ? CustomRoles.NotAssigned : CustomRoles.Crewmate;
    public override bool OnCompleteTask(uint taskid)
    {
        if (IsTaskFinished || MyTaskState.CompletedTasksCount >= cantaskcount) kakusei = true;
        return true;
    }
}