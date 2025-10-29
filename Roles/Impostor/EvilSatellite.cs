using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Hazel;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

using static TownOfHost.Modules.SelfVoteManager;

namespace TownOfHost.Roles.Impostor;

public sealed class EvilSatellite : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(EvilSatellite),
            player => new EvilSatellite(player),
            CustomRoles.EvilSatellite,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            3400,
            SetupOptionItem,
            "Es",
            OptionSort: (2, 3)
        );
    public EvilSatellite(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CustomRoleManager.OnFixedUpdateOthers.Add(OnFixedUpdateOthers);
        AllAlivePlayerRoute.Clear();
        SentPlayerId.Clear();
        AllAlivePlayerLastRoom.Clear();
        foreach (var pc in PlayerCatch.AllPlayerControls)
        {
            List<SystemTypes> Route = new();
            if (!AllAlivePlayerRoute.TryAdd(pc.PlayerId, Route)) AllAlivePlayerRoute[pc.PlayerId] = Route;
        }
        usecount = OptionMax.GetInt();
    }
    static OptionItem OptionKillCoolDown;
    static OptionItem OptionRandom;
    static OptionItem OptionMax;
    static Dictionary<byte, List<SystemTypes>> AllAlivePlayerRoute = new();
    static Dictionary<byte, SystemTypes> AllAlivePlayerLastRoom = new();
    static Dictionary<byte, List<SystemTypes>> SentPlayerId = new();
    static HashSet<EvilSatellite> EvilSatellites = new();
    public override bool CanUseAbilityButton() => GameStates.IsMeeting;
    int usecount;
    public override void Add()
    {
        EvilSatellites.Add(this);
    }
    enum OptionName
    {
        EvilSatelliteRandom
    }
    static void SetupOptionItem()
    {
        OptionKillCoolDown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, OptionBaseCoolTime, 30f, false).SetValueFormat(OptionFormat.Seconds);
        OptionRandom = BooleanOptionItem.Create(RoleInfo, 11, OptionName.EvilSatelliteRandom, true, false);
        OptionMax = IntegerOptionItem.Create(RoleInfo, 12, GeneralOption.OptionCount, (1, 99, 1), 5, false);
    }
    public override void AfterMeetingTasks()
    {
        AllAlivePlayerRoute.Clear();
        SentPlayerId.Clear();
        AllAlivePlayerLastRoom.Clear();
    }
    public static void OnFixedUpdateOthers(PlayerControl player)
    {
        if (player == null) return;
        if (!player.IsAlive()) return;
        //全員死亡なら終了
        if (PlayerCatch.AllAlivePlayerControls.All(pc => !pc.Is(CustomRoles.EvilSatellite))) return;
        if (!PlayerState.GetByPlayerId(player.PlayerId).HasSpawned) return;
        var nowroom = player.GetPlainShipRoom();
        if (nowroom == null) return;//ぬるぽならｶﾞｯ
        if (AllAlivePlayerLastRoom.TryGetValue(player.PlayerId, out var lastroom))
        {
            //位置が変わってないならreturn
            if (lastroom == nowroom.RoomId) return;
        }
        //最後の追加が被らないように
        if (!AllAlivePlayerLastRoom.TryAdd(player.PlayerId, nowroom.RoomId)) AllAlivePlayerLastRoom[player.PlayerId] = nowroom.RoomId;
        //経路リストに追加
        if (AllAlivePlayerRoute.TryGetValue(player.PlayerId, out var keiro))
        {
            AllAlivePlayerRoute[player.PlayerId].Add(nowroom.RoomId);
        }
        else
        {
            List<SystemTypes> Route = [nowroom.RoomId];
            if (AllAlivePlayerRoute.TryAdd(player.PlayerId, Route))
                return;
            Logger.Error($"{player.name} : AllAlivePlayerRouteがぬる!", "EvilSatellite");
        }
    }
    public float CalculateKillCooldown() => OptionKillCoolDown.GetFloat();
    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (!Canuseability()) return true;
        if (Is(voter))
        {
            if (CheckSelfVoteMode(Player, votedForId, out var status))
            {
                if (status is VoteStatus.Self)
                    Utils.SendMessage(string.Format(GetString("SkillMode"), GetString("Mode.Satellite"), GetString("Vote.Satellite")) + GetString("VoteSkillMode"), Player.PlayerId);
                if (status is VoteStatus.Skip)
                    Utils.SendMessage(GetString("VoteSkillFin"), Player.PlayerId);
                if (status is VoteStatus.Vote)
                    SendPlayerRoute(votedForId);
                SetMode(Player, status is VoteStatus.Self);
                return false;
            }
        }
        return true;
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (isForMeeting && Player.IsAlive() && seer.PlayerId == seen.PlayerId && Canuseability())
        {
            var voteinfo = $"<color={RoleInfo.RoleColorCode}>{GetString("SelfVoteRoleInfoMeg")}</color>";
            return isForHud ? voteinfo : $"<size=40%>{voteinfo}</size>";
        }
        return "";
    }
    public void SendPlayerRoute(byte playerid)
    {
        if (AllAlivePlayerRoute.TryGetValue(playerid, out var Routelist))
        {
            //送信済みではない時
            if (!SentPlayerId.TryGetValue(playerid, out var sentlist))
            {
                //設定回数と同じor多いともう使えない
                if (usecount <= 0) return;
                usecount--;
                SendRPC();
                List<SystemTypes> spk = new(!OptionRandom.GetBool() ? Routelist.ToArray() : Routelist.OrderBy(x => Guid.NewGuid()).ToArray());
                if (!SentPlayerId.TryAdd(playerid, spk)) SentPlayerId[playerid] = spk;
                sentlist = spk;
            }
            var sendtext = "<size=60%><line-height=80%>";
            var index = 0;
            var count = 0;
            foreach (var send in sentlist)
            {
                sendtext += GetString($"{send}") + (sentlist.Count == (count + 1) ? "" : (OptionRandom.GetBool() ? "・" : " → ")) + (count > 3 ? "\n" : "");
                if (index > 3) index = 0;
                index++;
                count++;
            }
            sendtext += string.Format(GetString("EvilSateliteSkillInfo"), UtilsName.GetPlayerColor(playerid));
            if (OptionRandom.GetBool()) sendtext += GetString("EvilSateliteSkillInfo2");
            sendtext += string.Format(GetString("EvilSateliteSkillInfo3"), usecount);
            Utils.SendMessage(sendtext, Player.PlayerId, string.Format($"<color=#ff1919>{GetString("EvilSateliteSkillInfoTitle")}</color>", UtilsName.GetPlayerColor(playerid)));
        }
    }
    public override string GetProgressText(bool comms = false, bool GameLog = false) => $"<color=#ff1919>({usecount})</color>";

    public void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(usecount);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        usecount = reader.ReadInt32();
    }
}