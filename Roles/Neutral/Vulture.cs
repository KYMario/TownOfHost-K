using System.Linq;
using System.Collections.Generic;
using AmongUs.GameOptions;
using HarmonyLib;
using UnityEngine;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using Hazel;
using TownOfHost.Roles.Vanilla;

namespace TownOfHost.Roles.Neutral;

public sealed class Vulture : RoleBase, IKillFlashSeeable, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Vulture),
            player => new Vulture(player),
            CustomRoles.Vulture,
            () => OptionCanUseVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            14600,
            SetupOptionItem,
            "Vu",
            "#6f4204",
            (5, 2),
            false,
            from: From.TheOtherRoles,
            Desc: () =>
            {
                var solo = string.Format(GetString("VultrueDescSoloWin"), OptionWinEatCount.GetInt());

                var add = "";
                if (OptionAddWinEatCount.GetInt() is not 0)
                    add = string.Format(GetString("VultrueDescAddWin"), OptionAddWinEatCount.GetInt());

                var shape = "";
                if (OptionEatShape.GetBool())
                    shape = GetString("VultrueDescEatShape");

                return string.Format(GetString("VultureDesc"), solo, add, shape);
            }
        );
    public Vulture(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.ForRecompute
    )
    {
        EatCount = 0;
        staticEatedPlayers.Clear();
        Viperkilledplayers = new();

        OptAddWinEatcount = OptionAddWinEatCount.GetInt();
        OptWinEatcount = OptionWinEatCount.GetInt();
        OptEatShape = OptionEatShape.GetBool();
        OptKillflashTaskcount = OptionKillflashtaskcount.GetInt();
        OptOnikuArrowtaskcount = OptionOnikuArrowtskcount.GetInt();
        OptVentInTime = OptionVentIntime.GetFloat();
        OptVentCooldown = OptionVentCooldown.GetFloat();

        CustomRoleManager.MarkOthers.Add(GetMarkOthers);

        MyTaskState.NeedTaskCount = OptKillflashTaskcount < OptOnikuArrowtaskcount ? OptOnikuArrowtaskcount : OptKillflashTaskcount;
    }
    static OptionItem OptionAddWinEatCount; static int OptAddWinEatcount;//追加勝利に必要なつまみ数
    static OptionItem OptionWinEatCount; static int OptWinEatcount; //勝利に必要なつまみ食い数
    static OptionItem OptionEatShape; static bool OptEatShape;//つまみぐいしたらシェイプ出る
    static OptionItem OptionKillflashtaskcount; static int OptKillflashTaskcount;//キルフラ見えるタスク数
    static OptionItem OptionOnikuArrowtskcount; static int OptOnikuArrowtaskcount;//矢印
    static OptionItem OptionCanUseVent;//ベント使えるか
    static OptionItem OptionVentCooldown; static float OptVentCooldown;//ベントクール
    static OptionItem OptionVentIntime; static float OptVentInTime;//ベント最大

    int EatCount;//食べたかず
    Dictionary<byte, Vector2> DiePlayerPos = new();//死体の矢印
    Dictionary<byte, float> Viperkilledplayers = new();//とける予定の死体
    static List<byte> staticEatedPlayers = new();//食べられたおにく
    enum OptionName
    {
        VultrueCanSeeKillFlushTaskCount,
        VultrueCanSeeOnikuArrowTaskCount,
        VultrueWinEatcount,
        VultrueAddWinEatcount,
        VultrueEatShape
    }

    private static void SetupOptionItem()
    {
        SoloWinOption.Create(RoleInfo, 8, defo: 1);
        OptionAddWinEatCount = IntegerOptionItem.Create(RoleInfo, 9, OptionName.VultrueAddWinEatcount, new(0, 14, 1), 2, false).SetZeroNotation(OptionZeroNotation.Off);
        OptionWinEatCount = IntegerOptionItem.Create(RoleInfo, 10, OptionName.VultrueWinEatcount, new(1, 14, 1), 3, false);
        OptionEatShape = BooleanOptionItem.Create(RoleInfo, 11, OptionName.VultrueEatShape, true, false);
        OptionCanUseVent = BooleanOptionItem.Create(RoleInfo, 14, GeneralOption.CanVent, true, false);
        OptionVentCooldown = FloatOptionItem.Create(RoleInfo, 15, StringNames.EngineerCooldown, OptionBaseCoolTime, 15, false, OptionCanUseVent).SetValueFormat(OptionFormat.Seconds);
        OptionVentIntime = FloatOptionItem.Create(RoleInfo, 16, StringNames.EngineerInVentCooldown, OptionBaseCoolTime, 5, false, OptionCanUseVent).SetZeroNotation(OptionZeroNotation.Infinity).SetValueFormat(OptionFormat.Seconds);
        OptionKillflashtaskcount = IntegerOptionItem.Create(RoleInfo, 12, OptionName.VultrueCanSeeKillFlushTaskCount, new(0, 255, 1), 3, false);
        OptionOnikuArrowtskcount = IntegerOptionItem.Create(RoleInfo, 13, OptionName.VultrueCanSeeOnikuArrowTaskCount, new(0, 255, 1), 5, false);

        OverrideTasksData.Create(RoleInfo, 20);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = OptVentCooldown;
        AURoleOptions.EngineerInVentMaxTime = OptVentInTime;
    }
    public override bool CancelReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target, ref DontReportreson reason)
    {
        if (reporter.PlayerId == Player.PlayerId && target != null && !staticEatedPlayers.Contains(target?.PlayerId ?? 250))
        {
            UtilsGameLog.AddGameLog("Vultrue", $"{UtilsName.GetPlayerColor(Player)}: {UtilsName.GetPlayerColor(target)}をつまみぐい！");
            Logger.Info($"{EatCount + 1}個目のお食事", "Vulture");
            reason = DontReportreson.Eat;
            EatCount++;
            staticEatedPlayers.Add(target.PlayerId);
            DiePlayerPos.Where(poss => poss.Key == target.PlayerId).Do(poss => GetArrow.Remove(Player.PlayerId, poss.Value));

            //勝利の確認
            if (OptWinEatcount <= EatCount)
            {
                Logger.Info($"ごちそうさまでした！", "Vulture");
                if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Vulture, Player.PlayerId, true))
                {
                    CustomWinnerHolder.NeutralWinnerIds.Add(Player.PlayerId);
                }
            }

            //食べたときにシェイプを
            if (OptEatShape)
            {
                var dummyshapetarget = PlayerCatch.AllPlayerControls.Where(pc => pc.PlayerId != Player.PlayerId).FirstOrDefault(PlayerControl.LocalPlayer);
                Player.RpcShapeshift(dummyshapetarget, true);
                Player.RpcShapeshift(Player, true);
                var sender = CustomRpcSender.Create("VultureEatShape");
                sender.AutoStartRpc(Player.NetId, RpcCalls.Shapeshift)
                    .Write(dummyshapetarget)
                    .Write(true)
                    .EndRpc();
                sender.AutoStartRpc(Player.NetId, RpcCalls.Shapeshift)
                    .Write(Player)
                    .Write(true)
                    .EndRpc();
                sender.EndMessage();
                sender.SendMessage();
            }

            return true;
        }
        if (reporter?.PlayerId != target?.PlayerId && target != null && staticEatedPlayers.Contains(target?.PlayerId ?? 250))
        {
            reason = DontReportreson.Eat;
            Logger.Info($"{target?.PlayerName ?? "???"}は食事済みだからキャンセル", "Vulture");
            return true;
        }

        return false;
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        //矢印の削除
        DiePlayerPos.Do(oniku => GetArrow.Remove(Player.PlayerId, oniku.Value));
        //保存データの削除
        DiePlayerPos.Clear();
    }

    public bool? CheckKillFlash(MurderInfo info)
    {
        //矢印の保存。万が一GetTruePositionがずれたことを考えてposで統一。
        var pos = info.AppearanceTarget.GetTruePosition();
        DiePlayerPos.Add(info.AppearanceTarget.PlayerId, pos);
        GetArrow.Add(Player.PlayerId, pos);
        //Viperの死体なら削除検知を
        if (info.AppearanceKiller.GetCustomRole() is CustomRoles.Viper) Viperkilledplayers.Add(info.AppearanceTarget.PlayerId, Viper.ViperDissolveTime);
        RpcAddDiePlayerPos(info.AppearanceTarget.PlayerId, pos);

        //キルフラッシュが見えるタスク数なら～
        return OptKillflashTaskcount <= MyTaskState.CompletedTasksCount;
    }
    //現在の食事状況を
    public override string GetProgressText(bool comms = false, bool GameLog = false) => $" <color={RoleInfo.RoleColorCode}>({EatCount}/{OptWinEatcount})</color>";
    //死体位置の矢印表示
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        //死亡済み or 会議中 or 他人にマークの場合は空
        if (!Player.IsAlive() || isForMeeting || seer.PlayerId != seen.PlayerId) return "";

        //タスク数に達しているか
        if (OptOnikuArrowtaskcount <= MyTaskState.CompletedTasksCount)
        {
            var str = $" <color={RoleInfo.RoleColorCode}>";

            //事前に保存しておいた奴を出す
            foreach (var arrow in DiePlayerPos)
            {
                str += GetArrow.GetArrows(seer, arrow.Value);
            }
            return $"{str}</color>";
        }
        return "";
    }
    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (isForMeeting)
        {
            if (staticEatedPlayers.Contains(seen.PlayerId)) return $"<color={RoleInfo.RoleColorCode}>×</color>";
        }
        return "";
    }
    public override void AfterMeetingTasks()
    {
        Viperkilledplayers.Clear();
        staticEatedPlayers.Clear();
    }
    public bool CheckWin(ref CustomRoles winnerRole)
    {
        if (OptAddWinEatcount is 0) return false;
        //君勝ってるやないか
        if (CustomWinnerHolder.WinnerTeam is CustomWinner.Vulture && CustomWinnerHolder.WinnerIds.Contains(Player.PlayerId)) return false;

        //生きてて小腹が満たせれば勝
        return OptAddWinEatcount <= EatCount && Player.IsAlive();
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (AmongUsClient.Instance.AmHost is false || Viperkilledplayers.Count is 0) return;

        for (byte i = 0; i < Viperkilledplayers.Count; i++)
        {
            if (Viperkilledplayers.TryGetValue(i, out var timer))
            {
                timer -= Time.fixedDeltaTime;
                Viperkilledplayers[i] = timer;

                if (timer <= 0)
                {
                    Viperkilledplayers.Remove(i);
                    DiePlayerPos.Where(poss => poss.Key == i).Do(poss => GetArrow.Remove(Player.PlayerId, poss.Value));
                    RpcEatPlayer(i);//Eatcountも送られるけど多分。。。大丈夫。
                }
            }
        }
    }

    public void RpcEatPlayer(byte targetId)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        using var sender = CreateSender();
        sender.Writer.WritePacked((int)RPC_Types.EatPlayer);
        sender.Writer.Write(EatCount);
        sender.Writer.Write(targetId);
    }

    public void RpcAddDiePlayerPos(byte targetId, Vector2 pos)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        using var sender = CreateSender();
        sender.Writer.WritePacked((int)RPC_Types.AddDiePlayerPos);
        sender.Writer.Write(targetId);
        NetHelpers.WriteVector2(pos, sender.Writer);
    }

    public void RpcClearDiePlayerPos()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        using var sender = CreateSender();
        sender.Writer.WritePacked((int)RPC_Types.ClearDiePlayerPos);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        switch ((RPC_Types)reader.ReadPackedInt32())
        {
            case RPC_Types.EatPlayer:
                EatCount = reader.ReadInt32();
                var targetId = reader.ReadByte();
                staticEatedPlayers.Add(targetId);
                DiePlayerPos.Where(poss => poss.Key == targetId).Do(poss => GetArrow.Remove(Player.PlayerId, poss.Value));
                break;
            case RPC_Types.AddDiePlayerPos:
                var playerPos = NetHelpers.ReadVector2(reader);
                DiePlayerPos.Add(reader.ReadByte(), playerPos);
                GetArrow.Add(Player.PlayerId, playerPos);
                break;
            case RPC_Types.ClearDiePlayerPos:
                DiePlayerPos.Do(oniku => GetArrow.Remove(Player.PlayerId, oniku.Value));
                DiePlayerPos.Clear();
                break;
        }
    }

    enum RPC_Types
    {
        EatPlayer,
        AddDiePlayerPos,
        ClearDiePlayerPos
    }
}
