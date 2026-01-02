
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Hazel;
using TownOfHost.Roles;
using TownOfHost.Roles.Core;
using UnityEngine;

namespace TownOfHost;

class MurderMystery
{
    // MurderMystery用
    public class MurderMysteryGameEndPredicate : GameEndPredicate
    {
        public override bool CheckForEndGame(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorsByKill;
            if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) return false;

            if (CheckGameEndByLivingPlayers(out reason)) return true;
            if (CheckGameEndBySabotage(out reason)) return true;

            return false;
        }

        public bool CheckGameEndByLivingPlayers(out GameOverReason reason)
        {
            reason = GameOverReason.ImpostorsByKill;

            int Imp = PlayerCatch.AlivePlayersCount(CountTypes.Impostor);
            int Crew = PlayerCatch.AlivePlayersCount(CountTypes.Crew);

            if (Imp == 0 && Crew == 0) //全滅
            {
                reason = GameOverReason.ImpostorsByKill;
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.None);
            }
            else if (Crew <= 0) //インポスター勝利
            {
                reason = GameOverReason.ImpostorsByKill;
                CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Impostor, byte.MaxValue);
            }
            else if (Imp == 0) //クルー勝利(インポスター切断など)
            {
                reason = GameOverReason.CrewmatesByVote;
                CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Crewmate, byte.MaxValue);
            }
            else return false; //勝利条件未達成

            return true;
        }
    }
    public static List<byte> TaskArchers = new();
    public static OptionItem OptionTimeLimit; public static float timer;
    public static OptionItem OptionSeeImpostorArrow; public static bool IsImpostorArrow;
    public static OptionItem OptionCrewvision; public static float Crewvision;
    public static OptionItem OptionImpostorvision; public static float ImpostorVision;
    public static int? DeadArcherCount; public static bool sabotage;
    public static void SetUpMurderMysteryOption()
    {
        ObjectOptionitem.Create(115804, "MurderMystery", true, null, TabGroup.MainSettings).SetOptionName(() => "Murder Mystery").SetColor(Color.blue).SetTag(CustomOptionTags.MurderMystery);
        OptionTimeLimit = FloatOptionItem.Create(115800, "MMTimeLimit", new(70, 600, 5), 360, TabGroup.MainSettings, false)
        .SetTag(CustomOptionTags.MurderMystery).SetHeader(true).SetColor(Color.blue);
        OptionSeeImpostorArrow = BooleanOptionItem.Create(115801, "MMSeeImpostorArrow", true, TabGroup.MainSettings, false).SetTag(CustomOptionTags.MurderMystery).SetColor(Color.blue);
        OptionCrewvision = FloatOptionItem.Create(115802, "MMCrewvision", new(0.1f, 1.5f, 0.05f), 0.4f, TabGroup.MainSettings, false).SetTag(CustomOptionTags.MurderMystery).SetColor(Color.blue);
        OptionImpostorvision = FloatOptionItem.Create(115803, "MMImpostorvision", new(0.1f, 3f, 0.05f), 2f, TabGroup.MainSettings, false).SetTag(CustomOptionTags.MurderMystery).SetColor(Color.blue);
    }
    [Attributes.GameModuleInitializer]
    public static void Init()
    {
        timer = OptionTimeLimit.GetFloat() + 5;
        IsImpostorArrow = OptionSeeImpostorArrow.GetBool();
        DeadArcherCount = null;
        sabotage = false;
        TaskArchers = new();
        ImpostorVision = OptionImpostorvision.GetFloat();
        Crewvision = OptionCrewvision.GetFloat();
        if (Options.CurrentGameMode is not CustomGameMode.MurderMystery) return;
        CustomRoleManager.LowerOthers.Add(GetLowerTextOthers);
        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
    }
    public static void CheckArcher()
    {
        if (Options.CurrentGameMode is not CustomGameMode.MurderMystery) return;
        DeadArcherCount = PlayerCatch.AllPlayerControls.Count(pc => pc.GetCustomRole() is CustomRoles.MMArcher);
        if (DeadArcherCount is 0) DeadArcherCount = null;
        else DeadArcherCount = 0;
    }
    public static void OnFixedUpdate()
    {
        if (!GameStates.AfterIntro) return;
        if (Options.CurrentGameMode is not CustomGameMode.MurderMystery) return;
        if (sabotage) return;

        timer -= Time.fixedDeltaTime;
        if (timer <= 60)
        {
            sabotage = true;
            if (AmongUsClient.Instance.AmHost)
            {
                var systemtypes = Utils.GetCriticalSabotageSystemType();
                ShipStatus.Instance.RpcUpdateSystem(systemtypes, 128);
                if (IsImpostorArrow)
                {
                    foreach (var impostor in PlayerCatch.AllAlivePlayerControls.Where(pc => pc.GetCustomRole().IsImpostor()))
                    {
                        foreach (var target in PlayerCatch.AllAlivePlayerControls.Where(pc => pc.GetCustomRole().IsCrewmate()))
                        {
                            TargetArrow.Add(impostor.PlayerId, target.PlayerId);
                        }
                    }
                }
                UtilsOption.MarkEveryoneDirtySettings();
                UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);
            }
        }
    }
    //通報時、保安官だったプレイヤーの引継ぎの確認
    public static void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (reporter.IsAlive() is false || target is null) return;
        if (reporter.GetCustomRole().IsCrewmate() is false) return;
        if (reporter.GetCustomRole() is CustomRoles.MMArcher && !TaskArchers.Contains(reporter.PlayerId)) return;

        var roleclass = CustomRoleManager.GetByPlayerId(target.PlayerId);
        if (roleclass is MMArcher MMArcher && MMArcher?.IsPromotioned is false)
        {
            if (reporter.GetRoleClass() is MMArcher mm)
            {
                mm.Arrowtime = MMArcher.OptionArrowTime.GetInt() is 0 ? null : MMArcher.OptionArrowTime.GetInt();
                mm.IsPromotioned = false;
                UtilsGameLog.LastLogRole[reporter.PlayerId] = $"<size=40%>{UtilsGameLog.LastLogRole[reporter.PlayerId].RemoveSizeTags()}</size><b>=> " + UtilsRoleText.GetRoleColorAndtext(CustomRoles.MMArcher) + "</b>";
            }
            else
            {
                reporter.RpcSetCustomRole(CustomRoles.MMArcher, log: null);
            }
            MMArcher.IsPromotioned = true;
            TaskArchers.Remove(reporter.PlayerId);
            DeadArcherCount--;
            SendRpc();
            UtilsNotifyRoles.NotifyRoles();
        }
    }
    //タスク完了時、ミニ保安官を付与
    public static void OnCompleteTask(PlayerControl player)
    {
        if (AmongUsClient.Instance.AmHost is false) return;
        if (player.IsAlive() is false) return;

        if (player.GetPlayerTaskState().IsTaskFinished)
        {
            TaskArchers.Add(player.PlayerId);
            player.RpcSetCustomRole(CustomRoles.MMArcher);
        }
    }
    public static void CheckDeath(PlayerControl target)
    {
        if (DeadArcherCount is null) return;
        if ((target.GetRoleClass() as MMArcher)?.IsPromotioned is false)
        {
            DeadArcherCount++;
            PlayerCatch.AllPlayerControls.Do(pc => pc.KillFlash());
            SendRpc();
        }
    }

    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (seer != seen) return "";
        if (seer.GetCustomRole().IsImpostor() && sabotage && IsImpostorArrow)
        {
            List<byte> list = new();
            PlayerCatch.AllAlivePlayerControls.Where(pc => pc.GetCustomRole().IsCrewmate()).Do(pc => list.Add(pc.PlayerId));
            return TargetArrow.GetArrows(seer, list.ToArray()) + $"({PlayerCatch.AliveImpostorCount}|{PlayerCatch.AlivePlayersCount(CountTypes.Crew)})";
        }
        return $"({PlayerCatch.AliveImpostorCount}|{PlayerCatch.AlivePlayersCount(CountTypes.Crew)})";
    }
    public static string GetLowerTextOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (seen != seer) return "";
        if (DeadArcherCount is null) return "";
        var crewtext = "";
        if ((seer.GetRoleClass() as MMArcher)?.IsPromotioned is true or null
        && seer.GetCustomRole().IsCrewmate()) crewtext += Translator.GetString("MM_ArceherIsDead_Crew");

        return DeadArcherCount.Value > 0 ? $"<#ff1919>{Translator.GetString("MM_ArceherIsDead")}{crewtext}</color>"
        : $"<#30b6ef>{Translator.GetString("MM_ArceherIsAlive")}</color>";
    }
    public static void SendRpc()
    {
        var sender = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncModSystem, SendOption.None, -1);
        sender.Write((int)RPC.ModSystem.SyncMuderMystery);
        sender.Write(DeadArcherCount is null ? -5 : DeadArcherCount.Value);
        AmongUsClient.Instance.FinishRpcImmediately(sender);
    }
}