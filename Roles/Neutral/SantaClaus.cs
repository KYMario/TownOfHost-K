using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using HarmonyLib;

namespace TownOfHost.Roles.Neutral;

public sealed class SantaClaus : RoleBase, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(SantaClaus),
            player => new SantaClaus(player),
            CustomRoles.SantaClaus,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Neutral,
            14800,
            SetupOptionItem,
            "Sac",
            "#e05050",
            (5, 4),
            Desc: () =>
            {
                return string.Format(GetString("SantaClausDesc"), OptWinGivePresentCount.GetInt(), OptAddWin.GetBool() ? GetString("AddWin") : GetString("SoloWin"));
            }
        );
    public SantaClaus(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.ForRecompute
    )
    {
        WinGivePresentCount = OptWinGivePresentCount.GetInt();
        AddWin = OptAddWin.GetBool();

        IWinflag = false;
        MeetingNotify = false;
        MeetingNotifyRoom = new();
        havepresent = 0;
        giftpresent = 0;
        EntotuVentId = null;
        EntotuVentPos = null;
        meetinggift = 0;
        GiftedPlayers.Clear();
        Memo = "";
    }
    static OptionItem OptWinGivePresentCount; static int WinGivePresentCount;
    static OptionItem OptAddWin; static bool AddWin;
    static OptionItem Optpresent;
    enum OptionName
    {
        SantaClausWinGivePresentCount,
        CountKillerAddWin,//追加勝利
        SantaClausGivePresent
    }
    bool IWinflag;
    bool MeetingNotify;
    List<string> MeetingNotifyRoom;
    int havepresent;
    int giftpresent;
    int? EntotuVentId;
    int meetinggift;
    Vector3? EntotuVentPos;
    string Memo;
    static List<byte> GiftedPlayers = new();
    private static void SetupOptionItem()
    {
        OptWinGivePresentCount = IntegerOptionItem.Create(RoleInfo, 10, OptionName.SantaClausWinGivePresentCount, new(1, 30, 1), 4, false);
        OptAddWin = BooleanOptionItem.Create(RoleInfo, 15, OptionName.CountKillerAddWin, false, false);
        SoloWinOption.Create(RoleInfo, 16, show: () => !OptAddWin.GetBool(), defo: 1);
        Optpresent = BooleanOptionItem.Create(RoleInfo, 17, OptionName.SantaClausGivePresent, true, false);
        OverrideTasksData.Create(RoleInfo, 20, tasks: (true, 0, 0, 3));
    }
    public override void Add() => SetPresentVent();
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = 1.1f;
        AURoleOptions.EngineerInVentMaxTime = 1;
    }
    public override bool OnCompleteTask(uint taskid)
    {
        if (AmongUsClient.Instance.AmHost && MyTaskState.IsTaskFinished && Player.IsAlive())
        {
            havepresent++;
            UtilsNotifyRoles.NotifyRoles();
        }
        return true;
    }
    public override string GetProgressText(bool comms = false, bool GameLog = false)
    {
        var win = $"{giftpresent}/{WinGivePresentCount}";

        return $" <color=#e05050>({win})</color>";
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        Memo = "";
        if (!MeetingNotify || !Player.IsAlive() || MeetingNotifyRoom.Count <= 0) return;

        var text = "";
        var count = 0;
        while (meetinggift > 0)
        {
            var room = MeetingNotifyRoom[count++];
            var chance = IRandom.Instance.Next(0, 20);
            var mesnumber = 0;

            if (chance > 18) mesnumber = 2;
            if (chance > 15) mesnumber = 1;

            var msg = string.Format(GetString($"SantaClausMeetingMeg{mesnumber}"), room);

            MeetingNotify = false;
            if (text is not "") text += "\n";
            text += $"<size=60%><color=#e05050>{msg}</color></size>";

            if (Optpresent.GetBool())
            {
                GiftPresent();
            }
            meetinggift--;
        }
        meetinggift = 0;
        MeetingNotifyRoom.Clear();
        Memo = text;
    }
    public override string MeetingAddMessage()
    {
        var send = Memo;
        Memo = "";
        return send;
    }
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (!Player.IsAlive() || ventId != EntotuVentId || havepresent <= 0 || EntotuVentPos == null) return false;

        havepresent--;
        //プレゼントを渡せたって言う処理
        Player.RpcProtectedMurderPlayer();

        Player.Data.RpcSetTasks(Array.Empty<byte>());
        MyTaskState.CompletedTasksCount = 0;
        giftpresent++;
        meetinggift++;
        Player.SyncSettings();
        EntotuVentId = null;
        MeetingNotify = true;

        // 通知の奴
        MeetingNotifyRoom.Add(Player.GetShipRoomName());

        GetArrow.Remove(Player.PlayerId, (Vector3)EntotuVentPos);
        if (WinGivePresentCount <= giftpresent)
        {
            Logger.Info($"{Player?.Data?.GetLogPlayerName() ?? "null"}が勝利条件達成！", "SantaClaus");

            if (!AddWin)//単独勝利設定なら即勝利で処理終わり
            {
                if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.SantaClaus, Player.PlayerId, true))
                {
                    CustomWinnerHolder.NeutralWinnerIds.Add(Player.PlayerId);
                }
                return false;
            }
            else
            {
                IWinflag = true;
            }
        }
        SetPresentVent();
        UtilsNotifyRoles.NotifyRoles();

        return false;
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        //自分だけで完結しないならお帰り！
        if (seen.PlayerId != seer.PlayerId) return "";
        //会議、死亡するとおしまい
        if (isForMeeting || !Player.IsAlive()) return "";

        //配達先が決まっている時
        if (EntotuVentPos != null && EntotuVentId != null && havepresent > 0)
            return $"<color=#e05050>{GetString("SantaClausLower1") + GetArrow.GetArrows(seer, (Vector3)EntotuVentPos)}</color>";

        // プレゼントの用意をするんだぜ
        var pos = "";
        if (EntotuVentPos != null && EntotuVentId != null)
        {
            pos = GetString("SantaClausLower1") + GetArrow.GetArrows(seer, (Vector3)EntotuVentPos);
        }
        return $"<color=#e05050>{GetString("SantaClausLower2")}<size=60%>{pos}</size></color>";
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (IWinflag && seen == seer) return Utils.AdditionalWinnerMark;
        return "";
    }
    public bool CheckWin(ref CustomRoles winnerRole) => IWinflag;
    public override string GetAbilityButtonText() => GetString("ChefButtonText");
    public override bool OverrideAbilityButton(out string text)
    {
        text = "SantaClaus_Ability";
        return true;
    }
    void SetPresentVent()
    {
        // プレゼントの配達先リスト
        List<Vent> AllVents = new(ShipStatus.Instance.AllVents);

        var ev = AllVents[IRandom.Instance.Next(AllVents.Count)];

        EntotuVentId = ev.Id;
        EntotuVentPos = new Vector3(ev.transform.position.x, ev.transform.position.y);
        GetArrow.Add(Player.PlayerId, (Vector3)EntotuVentPos);
    }
    CustomRoles[] giveaddons =
    {
        CustomRoles.Autopsy,
        CustomRoles.Lighting,
        CustomRoles.Moon,
        CustomRoles.Guesser,
        CustomRoles.Tiebreaker,
        CustomRoles.Opener,
        CustomRoles.Management,
        CustomRoles.Speeding,
        CustomRoles.MagicHand,
        CustomRoles.Serial,
        CustomRoles.Transparent,//なんでデバグがあるのかって?悪いサンタもおるやろ。
        CustomRoles.InfoPoor,//   というか天邪鬼付与させたいとても(?)
        CustomRoles.Water,
        CustomRoles.Clumsy
    };
    void GiftPresent()
    {
        List<PlayerControl> GiftTargets = new();
        foreach (var player in PlayerCatch.AllAlivePlayerControls)
        {
            if (!player.IsAlive()) continue;
            if (GiftedPlayers.Contains(player.PlayerId)) continue;
            GiftTargets.Add(player);
        }
        if (GiftTargets.Count < 1)
        {
            Logger.Info($"ギフトのターゲットがいないって伝えなきゃ!", "SantaClaus");
            return;
        }

        var target = GiftTargets[IRandom.Instance.Next(GiftTargets.Count)];
        if (!target)
        {
            return;
        }
        var roles = giveaddons.Where(role => !target.Is(role)).ToList();
        if (roles.Count() < 1)
        {
            Logger.Info($"{target.Data.GetLogPlayerName()}には付与できないって伝えなきゃ！", "SantaClaus");
            GiftedPlayers.Add(target.PlayerId);
            return;
        }

        var giftrole = roles[IRandom.Instance.Next(roles.Count())];
        target.RpcSetCustomRole(giftrole);
        Logger.Info($"{Player.Data.GetLogPlayerName()}:gift=>{target.Data.GetLogPlayerName()}({giftrole})", "SantaClaus");
        _ = new LateTask(() => Utils.SendMessage(string.Format(GetString("SantaGiftAddonMessage"), UtilsRoleText.GetRoleColorAndtext(giftrole)), target.PlayerId), 5f, "SantaGiftMeg", true);
    }
}