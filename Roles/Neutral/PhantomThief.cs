using AmongUs.GameOptions;
using UnityEngine;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using Hazel;

namespace TownOfHost.Roles.Neutral;

public sealed class PhantomThief : RoleBase, IKiller, IKillFlashSeeable, IRoomTasker
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(PhantomThief),
            player => new PhantomThief(player),
            CustomRoles.PhantomThief,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            15300,
            SetupOptionItem,
            "PT",
            "#3c1f56",
            (6, 3),
            true,
            introSound: () => GetIntroSound(RoleTypes.Phantom),
            Desc: () =>
            {
                var Preview = "";
                var win = "";

                if (OptionSoloWin.GetBool()) win = GetString("SoloWin");
                else win = GetString("AddWin");

                if (OptionNotice.GetBool())
                    Preview = string.Format(GetString("PhantomThiefDescInfo"), GetString($"PhantomThiefDescY{OptionNoticetype.GetValue()}"));

                return string.Format(GetString("PhantomThiefDesc"), Preview, OptionCantSetCount.GetInt(), win);
            }
        );
    public PhantomThief(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        IsAssignRoomtask = OptionRoomTask.GetBool();

        target = null;
        targetId = byte.MaxValue;
        targetrole = CustomRoles.NotAssigned;
        MeetingNotice = false;
        IsStolen = false;
    }
    static OptionItem OptionKillCoolDown;
    static OptionItem OptionCantSetCount;
    static OptionItem OptionSoloWin;
    static OptionItem OptionNotice;
    static OptionItem OptionNoticetype;
    static OptionItem OptionRoomTask; static bool IsAssignRoomtask;
    bool IsStolen;
    byte targetId;
    CustomRoles targetrole;
    PlayerControl target;
    bool MeetingNotice;
    public bool CanKill { get; private set; } = false;
    enum OptionName
    {
        PhantomThiefFarstCoolDown,
        PhantomThiefCantSetCount,
        PhantomThiefSoloWin,
        PhantomThiefNotice,
        PhantomThiefNoticeType,
        PhantomThiefRoomTask
    }
    enum Notice
    {
        NoticeNone, //なし
        NoticeTeam,//陣営のみ
        NoticePlayer,//個人
        NoticeRole//対象の役職
    }
    private static void SetupOptionItem()
    {
        SoloWinOption.Create(RoleInfo, 9, defo: 15);
        OptionKillCoolDown = FloatOptionItem.Create(RoleInfo, 10, OptionName.PhantomThiefFarstCoolDown, new(0f, 180f, 0.5f), 20f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionCantSetCount = IntegerOptionItem.Create(RoleInfo, 11, OptionName.PhantomThiefCantSetCount, new(1, 15, 1), 8, false).SetValueFormat(OptionFormat.Players);
        OptionNotice = BooleanOptionItem.Create(RoleInfo, 13, OptionName.PhantomThiefNotice, true, false);
        OptionNoticetype = StringOptionItem.Create(RoleInfo, 14, OptionName.PhantomThiefNoticeType, EnumHelper.GetAllNames<Notice>(), 1, false, OptionNotice);
        OptionRoomTask = BooleanOptionItem.Create(RoleInfo, 15, OptionName.PhantomThiefRoomTask, true, false);
        OptionSoloWin = BooleanOptionItem.Create(RoleInfo, 12, OptionName.PhantomThiefSoloWin, false, false);
    }
    public float CalculateKillCooldown() => OptionKillCoolDown.GetFloat();
    public bool? CheckKillFlash(MurderInfo info)
    {
        var (killer, target) = info.AppearanceTuple;
        return target.PlayerId == targetId;
    }
    public bool CanUseKillButton() => !(OptionCantSetCount.GetFloat() > PlayerCatch.AllAlivePlayersCount);
    public bool CanUseImpostorVentButton() => false;
    public bool CanUseSabotageButton() => false;
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(false);
    }
    public override void OnMurderPlayerAsTarget(MurderInfo info)
    {
        target = null;
        targetId = byte.MaxValue;
        targetrole = CustomRoles.NotAssigned;
        MeetingNotice = false;
        SendRPC_SetTarget();
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;

        info.DoKill = false;

        if (OptionCantSetCount.GetFloat() > PlayerCatch.AllAlivePlayersCount) return;
        if (targetId != byte.MaxValue) return;

        killer.ResetKillCooldown();
        targetId = target.PlayerId;
        this.target = target;
        targetrole = target.GetCustomRole();
        MeetingNotice = true;
        SendRPC_SetTarget();
        _ = new LateTask(() => UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player), 0.2f, "PhantomThief Target");
        killer.SetKillCooldown(target: target, delay: true);
        return;
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (targetId == byte.MaxValue) return;

        if ((PlayerCatch.GetPlayerById(targetId)?.IsAlive() ?? false) && player != target) return;

        targetId = byte.MaxValue;

        if (!AmongUsClient.Instance.AmHost) return;

        Player.KillFlash();
        MeetingNotice = false;
        Player.SetKillCooldown();
        _ = new LateTask(() => UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player), 0.2f, "PhantomThief Target");
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (seen == seer && seer == Player)
        {
            if (!seer.IsAlive()) return "";
            var notage = "<size=60%>" + GetString("PhantomThieftarget") + "</size>";
            var akiramero = "<size=60%>" + GetString("PhantomThiefakiarmero") + "</size>";
            if (targetId == byte.MaxValue)
                if (OptionCantSetCount.GetFloat() > PlayerCatch.AllAlivePlayersCount)
                {
                    return isForHud ? akiramero.RemoveSizeTags() : akiramero;
                }
                else return isForHud ? notage.RemoveSizeTags() : notage;

            var hehhehhe = "<size=60%>" + UtilsName.GetPlayerColor(targetId) + GetString("PhantomThiefwoitadakuze") + "</size>";
            if (isForMeeting is false) hehhehhe += "\n" + (seer.GetRoleClass() as IRoomTasker)?.GetLowerText(seer, RoleInfo.RoleColorCode) ?? "";

            return isForHud ? hehhehhe.RemoveSizeTags() : hehhehhe;
        }
        return "";
    }
    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, ref bool enabled, ref Color roleColor, ref string roleText, ref bool addon)
    {
        seen ??= Player;
        if (seen.PlayerId == targetId)
        {
            enabled = true;
            addon = true;
        }
    }
    public override void OnStartMeeting()
    {
        if (!MeetingNotice) return;
        MeetingNotice = false;
        if (!OptionNotice.GetBool()) return;
        if (!target.IsAlive()) return;
        if (!Player.IsAlive()) return;
        var sendmeg = "";
        var tumari = "";

        switch ((Notice)OptionNoticetype.GetValue())
        {
            case Notice.NoticeNone:
                sendmeg = GetString("PhantomThiefNoticeemail0");
                break;
            case Notice.NoticeTeam:
                var team = target.GetCustomRole().GetCustomRoleTypes();
                if (target.Is(CustomRoles.Amanojaku) || target.IsLovers()) team = CustomRoleTypes.Neutral;
                if (team == CustomRoleTypes.Madmate) team = CustomRoleTypes.Impostor;
                Color color = team is CustomRoleTypes.Crewmate ? Palette.Blue : (team is CustomRoleTypes.Impostor ? ModColors.ImpostorRed : ModColors.NeutralGray);

                sendmeg = string.Format(GetString("PhantomThiefNoticeTeam0"), Utils.ColorString(color, $"<u>{GetString($"PT.{team}")}</u>"));
                tumari = string.Format(GetString("PhantomThiefmegInfo"), Utils.ColorString(color, GetString(team.ToString())));
                break;
            case Notice.NoticePlayer:
                var colorid = target.Data.DefaultOutfit.ColorId;
                var playername = Utils.ColorString(Palette.PlayerColors[colorid], $"<u>{GetString($"PhantomThiefmeg{colorid}")}</u>");
                sendmeg = string.Format(GetString("PhantomThiefNoticePlayer0"), playername);
                tumari = string.Format(GetString("PhantomThiefmegInfo"), UtilsName.GetPlayerColor(target.Data));
                break;
            case Notice.NoticeRole:
                var role = target.GetCustomRole().GetCustomRoleTypes().ToString();
                if (target.GetCustomRole().GetCustomRoleTypes() == CustomRoleTypes.Madmate) role = "Impostor";
                if (target.IsLovers()) role = "Lovers";

                sendmeg = string.Format(GetString("PhantomThiefNoticeRole"), GetString($"PhantomThiefRole.{role}"), UtilsRoleText.GetRoleColorAndtext(role is "Lovers" ? target.GetLoverRole() : target.GetCustomRole()));
                tumari = "";
                break;
        }
        sendmeg += tumari is not "" ? $"<size=40%>\n{tumari}</size>" : "";
        if (sendmeg.RemoveHtmlTags() != "") _ = new LateTask(() => Utils.SendMessage(sendmeg, title: GetString("PhantomThiefTitle").Color(UtilsRoleText.GetRoleColor(CustomRoles.PhantomThief))), 5f, "SendPhantom", true);
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (!Player.IsAlive() || !target.IsAlive()) return "";

        if (seen.PlayerId == targetId) return $"<color={RoleInfo.RoleColorCode}>◆</color>";

        return "";
    }
    // 部屋タスク↓
    int? IRoomTasker.GetMaxTaskCount() => null;
    bool IRoomTasker.IsAssignRoomTask() => IsStolen is false && targetId is not byte.MaxValue && IsAssignRoomtask && Player.IsAlive();
    void IRoomTasker.ChangeRoom(PlainShipRoom TaskRoom) => SendRPC_ChengeRoom(TaskRoom);
    void IRoomTasker.OnComplete(int completeroom)
    {
        SendRPC_CompleteRoom(completeroom);
        IsStolen = true;
    }
    public override string MeetingAddMessage()
    {
        var addmeg = "";
        if (targetId is not byte.MaxValue && IsAssignRoomtask && Player.IsAlive() && IsStolen is false)
        {
            addmeg = $"<size=90%><{RoleInfo.RoleColorCode}>{GetString("PhantomThief_Title")}</size></color>";
            addmeg += $"\n<size=70%>{GetString($"PhantomThiefStolenMessage_{IRandom.Instance.Next(3)}")}</size>\n";
        }
        IsStolen = false;
        return addmeg;
    }
    public bool CheckWin()
    {
        if (targetId == byte.MaxValue || !Player.IsAlive()) return false;
        if (CustomWinnerHolder.WinnerIds.Contains(targetId))
        {
            var Targetrole = target.GetCustomRole();
            var winner = (CustomWinner)Targetrole;
            if (Targetrole.IsImpostor())
            {
                winner = CustomWinner.Impostor;//マッドメイトは追加勝利的な判定?
            }
            else
            if (Targetrole.IsCrewmate())
            {
                winner = CustomWinner.Crewmate;
            }
            if (Targetrole is CustomRoles.JackalAlien or CustomRoles.Jackaldoll or CustomRoles.JackalMafia)
            {
                winner = CustomWinner.Jackal;
            }

            if (Targetrole != targetrole && Targetrole != CustomRoles.NotAssigned) targetrole = Targetrole;
            if (OptionSoloWin.GetBool() && CustomWinnerHolder.WinnerTeam == winner)//単独勝利かつ、相手が単独勝利
            {
                //ラバーズ等、自身も勝利していたら除外
                if (CustomWinnerHolder.WinnerIds.Contains(Player.PlayerId)) return false;
                if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.PhantomThief, Player.PlayerId, true))
                {
                    CustomWinnerHolder.NeutralWinnerIds.Add(Player.PlayerId);
                    CustomWinnerHolder.WinnerIds.Add(Player.PlayerId);
                    Player.RpcSetCustomRole(targetrole, log: null);
                    target.RpcSetCustomRole(CustomRoles.Emptiness, log: null);
                    CustomWinnerHolder.CantWinPlayerIds.Add(targetId);
                    UtilsGameLog.AddGameLog($"PhantomThief", string.Format(GetString("Log.PhantomThief"), UtilsName.GetPlayerColor(Player, true), UtilsName.GetPlayerColor(targetId, true)));
                }
                return false;
            }
            CustomWinnerHolder.NeutralWinnerIds.Add(Player.PlayerId);
            CustomWinnerHolder.WinnerIds.Add(Player.PlayerId);
            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.PhantomThief);
            Player.RpcSetCustomRole(targetrole, log: null);
            target.RpcSetCustomRole(CustomRoles.Emptiness, log: null);
            CustomWinnerHolder.CantWinPlayerIds.Add(targetId);
            UtilsGameLog.AddGameLog($"PhantomThief", string.Format(GetString("Log.PhantomThief"), UtilsName.GetPlayerColor(Player, true), UtilsName.GetPlayerColor(targetId, true)));
            return true;
        }
        return false;
    }

    public void SendRPC_SetTarget()
    {
        using var sender = CreateSender();
        sender.Writer.WritePacked((int)RPC_Types.SetTarget);
        sender.Writer.Write(targetId);
    }
    public void SendRPC_CompleteRoom(int completeroom)
    {
        using var sender = CreateSender();
        sender.Writer.WritePacked((int)RPC_Types.CompleteRoom);
    }
    public void SendRPC_ChengeRoom(PlainShipRoom TaskPSR)
    {
        using var sender = CreateSender();
        sender.Writer.WritePacked((int)RPC_Types.ChengeRoom);
        sender.Writer.Write((byte)TaskPSR.RoomId);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        var iroomtasker = Player.GetRoleClass() is IRoomTasker roomTasker ? roomTasker : null;
        switch ((RPC_Types)reader.ReadPackedInt32())
        {
            case RPC_Types.SetTarget:
                targetId = reader.ReadByte();
                break;
            case RPC_Types.ChengeRoom:
                iroomtasker?.ReceiveRoom(Player.PlayerId, reader);
                break;
            case RPC_Types.CompleteRoom:
                iroomtasker?.ReceiveCompleteRoom(Player.PlayerId, reader);
                IsStolen = true;
                break;
        }
    }

    enum RPC_Types
    {
        SetTarget,
        ChengeRoom,
        CompleteRoom
    }
    public bool OverrideKillButtonText(out string text)
    {
        text = GetString("PhantomThiefButtonText");
        return true;
    }
    public bool OverrideKillButton(out string text)
    {
        text = "PhantomThief_Kill";
        return true;
    }
}