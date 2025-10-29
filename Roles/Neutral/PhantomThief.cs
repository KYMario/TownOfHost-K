using AmongUs.GameOptions;
using UnityEngine;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using Hazel;

namespace TownOfHost.Roles.Neutral;

public sealed class PhantomThief : RoleBase, IKiller, IKillFlashSeeable
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
        target = null;
        targetId = byte.MaxValue;
        targetrole = CustomRoles.NotAssigned;
        MeetingNotice = false;
    }
    static OptionItem OptionKillCoolDown;
    static OptionItem OptionCantSetCount;
    static OptionItem OptionSoloWin;
    static OptionItem OptionNotice;
    static OptionItem OptionNoticetype;
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
        PhantomThiefNoticeType
    }
    enum Notice
    {
        NoticeNone, //なし
        NoticeTeam,//陣営のみ
        NoticePlayer,//個人
    }
    private static void SetupOptionItem()
    {
        SoloWinOption.Create(RoleInfo, 9, defo: 15);
        OptionKillCoolDown = FloatOptionItem.Create(RoleInfo, 10, OptionName.PhantomThiefFarstCoolDown, new(0f, 180f, 0.5f), 20f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionCantSetCount = IntegerOptionItem.Create(RoleInfo, 11, OptionName.PhantomThiefCantSetCount, new(1, 15, 1), 8, false).SetValueFormat(OptionFormat.Players);
        OptionNotice = BooleanOptionItem.Create(RoleInfo, 13, OptionName.PhantomThiefNotice, true, false);
        OptionNoticetype = StringOptionItem.Create(RoleInfo, 14, OptionName.PhantomThiefNoticeType, EnumHelper.GetAllNames<Notice>(), 1, false, OptionNotice);
        OptionSoloWin = BooleanOptionItem.Create(RoleInfo, 12, OptionName.PhantomThiefSoloWin, false, false);
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
    public override void OnMurderPlayerAsTarget(MurderInfo info)
    {
        target = null;
        targetId = byte.MaxValue;
        targetrole = CustomRoles.NotAssigned;
        MeetingNotice = false;
        SendRPC();
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
        SendRPC();
        _ = new LateTask(() => UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player), 0.2f, "PhantomThief Target");
        killer.SetKillCooldown(target: target, delay: true);
        return;
    }
    public bool CanUseKillButton() => !(OptionCantSetCount.GetFloat() > PlayerCatch.AllAlivePlayersCount);
    public bool CanUseImpostorVentButton() => false;
    public bool CanUseSabotageButton() => false;
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

            return isForHud ? hehhehhe.RemoveSizeTags() : hehhehhe;
        }
        return "";
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(false);
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
        }
        sendmeg += $"<size=40%>\n{tumari}</size>";
        if (sendmeg.RemoveHtmlTags() != "") _ = new LateTask(() => Utils.SendMessage(sendmeg, title: GetString("PhantomThiefTitle").Color(UtilsRoleText.GetRoleColor(CustomRoles.PhantomThief))), 5f, "SendPhantom", true);
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
    public float CalculateKillCooldown() => OptionKillCoolDown.GetFloat();
    public bool? CheckKillFlash(MurderInfo info)
    {
        var (killer, target) = info.AppearanceTuple;
        return target.PlayerId == targetId;
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (!Player.IsAlive() || !target.IsAlive()) return "";

        if (seen.PlayerId == targetId) return $"<color={RoleInfo.RoleColorCode}>◆</color>";

        return "";
    }
    public bool CheckWin()
    {
        if (targetId == byte.MaxValue || !Player.IsAlive()) return false;
        if (CustomWinnerHolder.WinnerIds.Contains(targetId))
        {
            var Targetrole = target.GetCustomRole();
            if (Targetrole != targetrole && Targetrole != CustomRoles.NotAssigned) targetrole = Targetrole;
            if (OptionSoloWin.GetBool())
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

    public void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(targetId);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        targetId = reader.ReadByte();
    }
}