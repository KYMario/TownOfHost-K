using System.Linq;
using System.Text;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using static TownOfHost.Modules.SelfVoteManager;

namespace TownOfHost.Roles.Crewmate;

public sealed class Inspector : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Inspector),
            player => new Inspector(player),
            CustomRoles.Inspector,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            9700,
            SetupOptionItem,
            "Is",
            "#977b48",
            (3, 5)
        );
    public Inspector(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Awakened = !OptAwakening.GetBool();
        Max = OptionMaximum.GetInt();
        Votemode = (AbilityVoteMode)OptionVoteMode.GetValue();
        IsSetRect = OptionSetRect.GetBool();
        rectdeathreason = OptionDeathReason.GetFloat();
        rectkillercolor = OptionKillerColor.GetFloat();
        recttargetteam = OptionTargetTeam.GetFloat();
        rectkillerrole = OptionKillerRole.GetFloat();
        rectdeathtimer = OptionDeathTimer.GetFloat();
        recttargetroom = OptionTargetroom.GetFloat();
        count = 0;
        TargetPlayerId = byte.MaxValue;
        Isdie = false;
        deadtimer = 0;
    }
    bool Awakened;
    static OptionItem OptionMaximum;
    static OptionItem OptAwakening;
    static OptionItem OptAwakeningTaskcount;
    static OptionItem OptionVoteMode;
    static OptionItem OptionSetRect;/*確率を設定する*/ static bool IsSetRect;
    static OptionItem OptionDeathReason; static float rectdeathreason;
    static OptionItem OptionKillerColor; static float rectkillercolor;
    static OptionItem OptionTargetTeam; static float recttargetteam;
    static OptionItem OptionKillerRole; static float rectkillerrole;
    static OptionItem OptionDeathTimer; static float rectdeathtimer;
    static OptionItem OptionTargetroom; static float recttargetroom;
    public AbilityVoteMode Votemode;
    static int Max;
    int count;
    byte TargetPlayerId;
    float deadtimer;
    bool Isdie;

    enum OptionName
    {
        InspectVoteMode,
        InspectSetRect,
        InspectDeathReason,
        InspectColor,
        InspectTargetTeam,
        InspectKillerRole,
        InspectDeathTimer,
        InspectTargetRoom
    }

    enum Infom
    {
        DeathReason,
        Color,
        TargetTeam,
        KillerRole,
        DeathTimer,
        TargetRoom,
    }
    public override void Add() => AddSelfVotes(Player);
    private static void SetupOptionItem()
    {
        OptionMaximum = IntegerOptionItem.Create(RoleInfo, 10, GeneralOption.OptionCount, new(1, 99, 1), 1, false)
            .SetValueFormat(OptionFormat.Times);
        OptionVoteMode = StringOptionItem.Create(RoleInfo, 11, OptionName.InspectVoteMode, EnumHelper.GetAllNames<AbilityVoteMode>(), 1, false);
        OptionSetRect = BooleanOptionItem.Create(RoleInfo, 14, OptionName.InspectSetRect, false, false);
        OptionDeathReason = FloatOptionItem.Create(RoleInfo, 15, OptionName.InspectDeathReason, new(0, 100, 5), 100, false, OptionSetRect).SetValueFormat(OptionFormat.Percent);
        OptionKillerColor = FloatOptionItem.Create(RoleInfo, 16, OptionName.InspectColor, new(0, 100, 5), 100, false, OptionSetRect).SetValueFormat(OptionFormat.Percent);
        OptionTargetTeam = FloatOptionItem.Create(RoleInfo, 17, OptionName.InspectTargetTeam, new(0, 100, 5), 100, false, OptionSetRect).SetValueFormat(OptionFormat.Percent);
        OptionKillerRole = FloatOptionItem.Create(RoleInfo, 18, OptionName.InspectKillerRole, new(0, 100, 5), 100, false, OptionSetRect).SetValueFormat(OptionFormat.Percent);
        OptionDeathTimer = FloatOptionItem.Create(RoleInfo, 19, OptionName.InspectDeathTimer, new(0, 100, 5), 100, false, OptionSetRect).SetValueFormat(OptionFormat.Percent);
        OptionTargetroom = FloatOptionItem.Create(RoleInfo, 20, OptionName.InspectTargetRoom, new(0, 100, 5), 100, false, OptionSetRect).SetValueFormat(OptionFormat.Percent);
        OptAwakening = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.TaskAwakening, false, false);
        OptAwakeningTaskcount = IntegerOptionItem.Create(RoleInfo, 13, GeneralOption.AwakeningTaskcount, new(1, 255, 1), 5, false, OptAwakening);
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (Isdie)
        {
            deadtimer += Time.fixedDeltaTime;
            return;
        }
        if (!player.IsAlive() || !AmongUsClient.Instance.AmHost || TargetPlayerId is byte.MaxValue) return;

        var target = PlayerCatch.GetPlayerById(TargetPlayerId);
        if (target.IsAlive()) return;

        if (PlayerState.GetByPlayerId(TargetPlayerId).DeathReason is CustomDeathReason.Disconnected)
        {
            TargetPlayerId = byte.MaxValue;
            return;
        }

        Isdie = true;
    }
    public override CustomRoles Misidentify() => Awakened ? CustomRoles.NotAssigned : CustomRoles.Crewmate;
    public override bool OnCompleteTask(uint taskid)
    {
        if (MyTaskState.HasCompletedEnoughCountOfTasks(OptAwakeningTaskcount.GetValue()))
        {
            if (Awakened == false)
                if (!Utils.RoleSendList.Contains(Player.PlayerId))
                    Utils.RoleSendList.Add(Player.PlayerId);
            Awakened = true;
        }
        return true;
    }
    public override string GetProgressText(bool comms = false, bool gamelog = false) => Utils.ColorString(Max <= count ? Color.gray : Color.cyan, $"({Max - count})");
    public override void OnStartMeeting()
    {
        if (Isdie)
        {
            PlayerState targetstate = TargetPlayerId.GetPlayerState();
            StringBuilder sb = new();
            //for (var i = 0; i == 1; i++) 
            {
                var type = (Infom)IRandom.Instance.Next(EnumHelper.GetAllValues<Infom>().Count());
                if (IsSetRect)
                {
                    var all = rectdeathreason + rectkillercolor + recttargetteam + rectkillerrole + rectdeathtimer + recttargetroom;
                    var chance = IRandom.Instance.Next((int)all);

                    if (chance <= rectdeathreason) { type = Infom.DeathReason; }
                    else if (chance <= rectdeathreason + rectkillercolor) { type = Infom.Color; }
                    else if (chance <= rectdeathreason + rectkillercolor + recttargetteam) { type = Infom.TargetTeam; }
                    else if (chance <= rectdeathreason + rectkillercolor + recttargetteam + rectkillerrole) { type = Infom.KillerRole; }
                    else if (chance <= rectdeathreason + rectkillercolor + recttargetteam + rectkillerrole + rectdeathtimer) { type = Infom.DeathTimer; }
                    else { type = Infom.TargetRoom; }
                }

                switch (type)
                {
                    case Infom.Color:
                        if (Camouflage.PlayerSkins.TryGetValue(targetstate.RealKiller.killerid, out var cos))
                        {
                            var lightName = "";
                            if (cos.ColorId is 0 or 1 or 2 or 6 or 8 or 9 or 12 or 15 or 16)
                            {
                                lightName = Palette.GetColorName((int)ModColors.PlayerColor.Black);
                            }
                            else lightName = Palette.GetColorName((int)ModColors.PlayerColor.white);

                            sb.AppendFormat(GetString("Inspector.InfoColor"), UtilsName.GetPlayerColor(TargetPlayerId), lightName);
                        }
                        break;
                    case Infom.DeathReason:
                        sb.AppendFormat(GetString("Inspector.InfoDeathReason"), UtilsName.GetPlayerColor(TargetPlayerId), GetString($"DeathReason.{targetstate.DeathReason}"));
                        break;
                    case Infom.DeathTimer:
                        sb.AppendFormat(GetString("Inspector.InfoTimer"), UtilsName.GetPlayerColor(TargetPlayerId), (int)deadtimer);
                        break;
                    case Infom.KillerRole:
                        sb.AppendFormat(GetString("Inspector.InfoRole"), UtilsName.GetPlayerColor(TargetPlayerId), UtilsRoleText.GetTrueRoleName(targetstate.RealKiller.killerid, false));
                        break;
                    case Infom.TargetTeam:
                        string str = "";
                        switch (targetstate.MainRole.GetCustomRoleTypes())
                        {
                            case CustomRoleTypes.Crewmate:
                                str = UtilsRoleText.GetRoleColorAndtext(CustomRoles.Crewmate);
                                break;
                            case CustomRoleTypes.Impostor:
                                str = UtilsRoleText.GetRoleColorAndtext(CustomRoles.Impostor);
                                break;
                            case CustomRoleTypes.Madmate:
                                str = UtilsRoleText.GetRoleColorAndtext(CustomRoles.Madmate);
                                break;
                            case CustomRoleTypes.Neutral:
                                str = Utils.ColorString(ModColors.Gray, GetString("Neutral"));
                                break;
                        }
                        sb.AppendFormat(GetString("Inspector.InfoTeam"), UtilsName.GetPlayerColor(TargetPlayerId), str);
                        break;
                    case Infom.TargetRoom:
                        if (targetstate.KillRoom is "")
                        {
                            sb.AppendFormat(GetString("Inspector.InfoDeathReason"), UtilsName.GetPlayerColor(TargetPlayerId), GetString($"DeathReason.{targetstate.DeathReason}"));
                            break;
                        }
                        sb.AppendFormat(GetString("Inspector.InfoRoom"), UtilsName.GetPlayerColor(TargetPlayerId), targetstate.KillRoom);
                        break;
                    default:
                        sb.Append("???");
                        break;
                }
                Logger.Info($"{Player.Data.GetLogPlayerName()} => {type}", "Inspector");
                _ = new LateTask(() =>
                    Utils.SendMessage(sb.ToString(), Player.PlayerId, $"<{RoleInfo.RoleColorCode}>{GetString("Inspector.Title")}</color>")
                    , 3, "InspectorSend", true);
            }
        }
        deadtimer = 0;
        Isdie = false;
        TargetPlayerId = byte.MaxValue;
    }
    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (!Canuseability()) return true;
        if (Max > count && Is(voter) && TargetPlayerId is byte.MaxValue && Awakened)
        {
            var target = PlayerCatch.GetPlayerById(votedForId);
            if (Votemode == AbilityVoteMode.NomalVote)
            {
                if (Player.PlayerId == votedForId || votedForId == SkipId) return true;
                Inspect(votedForId);
                return false;
            }
            else
            {
                if (CheckSelfVoteMode(Player, votedForId, out var status))
                {
                    if (status is VoteStatus.Self)
                        Utils.SendMessage(string.Format(GetString("SkillMode"), GetString("Mode.Inspect"), GetString("Vote.Inspect")) + GetString("VoteSkillMode"), Player.PlayerId);
                    if (status is VoteStatus.Skip)
                        Utils.SendMessage(GetString("VoteSkillFin"), Player.PlayerId);
                    if (status is VoteStatus.Vote)
                        Inspect(votedForId);
                    SetMode(Player, status is VoteStatus.Self);
                    return false;
                }
            }
        }
        return true;
    }

    public void Inspect(byte votedForId)
    {
        var target = PlayerCatch.GetPlayerById(votedForId);
        if (!target.IsAlive()) return;//死んでるならここで処理を止める。
        count++;//全体のカウント
        TargetPlayerId = votedForId;
        Utils.SendMessage(string.Format(GetString("Skill.Inspector"), UtilsName.GetPlayerColor(votedForId)), Player.PlayerId);
        Logger.Info($"Player: {Player.name},Target: {target.name}, count: {count}", "Inspector");
    }
}
