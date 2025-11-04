using System;
using System.Collections.Generic;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using TownOfHost.Modules;
using TownOfHost.Patches;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using UnityEngine;

namespace TownOfHost.Roles.Impostor;

public sealed class Assassin : RoleBase, IImpostor, IUsePhantomButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Assassin),
            player => new Assassin(player),
            CustomRoles.Assassin,
            () => OptionHasOtherRole.GetBool() ? OptionHaveRole.GetRole().GetRoleInfo()?.BaseRoleType?.Invoke() ?? RoleTypes.Impostor : RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            15800,
            SetupOptionItem,
            "as",
            OptionSort: (2, 0),
            tab: TabGroup.Combinations,
            introSound: () => GetIntroSound(RoleTypes.Detective),
            assignInfo: new RoleAssignInfo(CustomRoles.Assassin, CustomRoleTypes.Impostor)
            {
                AssignUnitRoles = [CustomRoles.Assassin, CustomRoles.Merlin]
            },
            combination: CombinationRoles.AssassinandMerlin,
            from: From.ExtremeRoles
        );
    public Assassin(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        NowState = AssassinMeeting.WaitMeeting;
        MarlinIds = new();
        isDeadCache = new();
        NowUse = false;
        GuessId = byte.MaxValue;
        cancallmeetingkill = OptionCanCallMeetingKill.GetBool();
    }
    byte GuessId;
    public static bool NowUse;
    public static List<byte> MarlinIds = new();
    public AssassinMeeting NowState;
    static Dictionary<byte, (bool isDead, bool Disconnected)> isDeadCache = new();

    static OptionItem OptionCanCallMeetingKill; static bool cancallmeetingkill;
    static OptionItem OptionHasOtherRole;
    static FilterOptionItem OptionHaveRole; static CustomRoles haverole;

    public static OptionItem OptionMerlinHasTask;
    static OverrideTasksData OptionMerlinNomalTask;
    public static OptionItem OptionMerlinWorkTask;
    public static OptionItem OptionMerlinCanSeeNeutral;
    public static OptionItem OptionMerlinOnlyNeutralKiller;
    public static OptionItem OptionMerlinCantSeeNeutralColor;
    public static OptionItem OptionMerlinCanSeeMadmate;
    RoleBase AddRole;
    public static Assassin assassin;
    public enum AssassinMeeting
    {
        WaitMeeting,
        CallMetting,
        Guessing,
        Collected,
        EndMeeting,
        DieWait,
        Win
    }
    static CustomRoles[] InvalidRoles()
    {
        List<CustomRoles> InvalidRoles = new();
        foreach (var data in CustomRoleManager.AllRolesInfo)
        {
            if (data.Key.IsImpostor() is false && data.Key is not CustomRoles.NotAssigned) continue;
            if (data.Key is CustomRoles.AlienHijack or CustomRoles.EvilSatellite or CustomRoles.ConnectSaver
            or CustomRoles.Limiter or CustomRoles.Assassin)
            {
                InvalidRoles.Add(data.Key);
                continue;
            }
        }
        return InvalidRoles.ToArray();
    }
    enum OptionName
    {
        AssasinCanCallMeetingKill,
        AssasinHasOtherRole,
        AssassinHaveRole,
        WalkerWalkTaskCount,
        MerlinCanSeeNeutral,
        MerlinOnlyNeutralKiller,
        MerlinCantSeeNeutralColor,
        MerlinCanSeeMadmate,
        MerlinHasTask
    }
    public static void SetupOptionItem()
    {
        SoloWinOption.Create(RoleInfo, 11);
        OptionCanCallMeetingKill = BooleanOptionItem.Create(RoleInfo, 10, OptionName.AssasinCanCallMeetingKill, false, false);
        OptionHasOtherRole = BooleanOptionItem.Create(RoleInfo, 13, OptionName.AssasinHasOtherRole, false, false);
        OptionHaveRole = FilterOptionItem.Create(RoleInfo, 12, OptionName.AssassinHaveRole, 0, false, OptionHasOtherRole, true, false, false, false, () => InvalidRoles());

        OptionMerlinHasTask = BooleanOptionItem.Create(RoleInfo, 24, OptionName.MerlinHasTask, false, false);
        OptionMerlinNomalTask = OverrideTasksData.Create(RoleInfo, 16, rolename: CustomRoles.Merlin, tasks: (true, 2, 0, 0), OptionMerlinHasTask);
        OptionMerlinWorkTask = IntegerOptionItem.Create(RoleInfo, 15, OptionName.WalkerWalkTaskCount, (0, 99, 1), 6, false, OptionMerlinWorkTask);
        OptionMerlinCanSeeNeutral = BooleanOptionItem.Create(RoleInfo, 20, OptionName.MerlinCanSeeNeutral, false, false);
        OptionMerlinCanSeeMadmate = BooleanOptionItem.Create(RoleInfo, 21, OptionName.MerlinCanSeeMadmate, true, false, OptionMerlinCanSeeNeutral);
        OptionMerlinOnlyNeutralKiller = BooleanOptionItem.Create(RoleInfo, 22, OptionName.MerlinOnlyNeutralKiller, false, false, OptionMerlinCanSeeNeutral);
        OptionMerlinCantSeeNeutralColor = BooleanOptionItem.Create(RoleInfo, 23, OptionName.MerlinCantSeeNeutralColor, true, false, OptionMerlinCanSeeNeutral);
    }
    public override void Add()
    {
        if (OptionHasOtherRole.GetBool())
        {
            haverole = OptionHaveRole.GetRole();
            if (CustomRoleManager.AllRolesInfo.TryGetValue(haverole, out var roleinfo))
            {
                AddRole = roleinfo.CreateInstance(Player);
                Logger.Info($"Assasin + {haverole}", "Assasin");
            }
        }
        else
        {
            AddRole = null;
            haverole = CustomRoles.NotAssigned;
        }
        AddRole?.Add();
        assassin = this;
    }
    public override void OnDestroy()
    {
        if (AddRole is not null)
        {
            AddRole?.OnDestroy();
        }
        AddRole = null;
        haverole = CustomRoles.NotAssigned;
        NowUse = false;
        NowState = AssassinMeeting.EndMeeting;
        MarlinIds = new();
        isDeadCache = new();
        GuessId = byte.MaxValue;
        assassin = null;
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (NowState is not AssassinMeeting.WaitMeeting || player.IsAlive() || player == null || GameStates.IsMeeting || CustomWinnerHolder.WinnerTeam is not CustomWinner.Default)
        {
            AddRole?.OnFixedUpdate(player);
            return;
        }
        if (!cancallmeetingkill)
        {
            NowState = AssassinMeeting.EndMeeting;
            return;
        }
        if (NowState is AssassinMeeting.WaitMeeting)
        {
            NowState = AssassinMeeting.CallMetting;
            Logger.Info("死んじゃった。", "Assassin");

            //この先ホストのみ実行
            if (!AmongUsClient.Instance.AmHost)
            {
                NowState = AssassinMeeting.Guessing;
                return;
            }

            _ = new LateTask(() =>
            {
                MyState.IsDead = false;
                foreach (var info in GameData.Instance.AllPlayers)
                {
                    if (info.PlayerId == Player.PlayerId)
                    {
                        isDeadCache[info.PlayerId] = (false, false);

                        info.IsDead = false;
                        info.Disconnected = false;
                        continue;
                    }
                    isDeadCache[info.PlayerId] = (info.PlayerId.GetPlayerState().IsDead, info.Disconnected);

                    info.IsDead = false;
                    info.Disconnected = false;
                }
                NowUse = true;
                NowState = AssassinMeeting.Guessing;

                GameDataSerializePatch.SerializeMessageCount++;
                AntiBlackout.SendGameData();
                GameDataSerializePatch.SerializeMessageCount--;
                MyState.IsDead = false;
                _ = new LateTask(() =>
                {
                    GameDataSerializePatch.DontTouch = true;
                }, 1, "DontTuch", true);
                _ = new LateTask(() =>
                {
                    MyState.IsDead = false;
                    ReportDeadBodyPatch.ExReportDeadBody(Player, null, false, "AssassinMeeting", "#ff1919");
                    Utils.AllPlayerKillFlash();

                    GameDataSerializePatch.DontTouch = false;
                }, 3, "", true);
            }, 1, "", true);
        }
    }
    public override void OnSpawn(bool initialState = false)
    {
        AddRole?.OnSpawn(initialState);
        NowUse = false;
        if (NowState is AssassinMeeting.DieWait)
        {
            NowState = AssassinMeeting.EndMeeting;
            SendStateRPC();
            Player.RpcExile();
            MyState.SetDead();
            return;
        }
        if (NowState is AssassinMeeting.Collected or AssassinMeeting.CallMetting)
        {
            //_ = new LateTask(() =>
            {
                if (NowState is AssassinMeeting.Collected)
                {
                    MyState.IsDead = false;
                    CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Impostor, Player.PlayerId, hantrole: CustomRoles.Assassin);
                    Logger.Info("まーりんぱりーん", "Assassin");
                    NowState = AssassinMeeting.Win;
                }
                else if (NowState is AssassinMeeting.CallMetting)
                {
                    MyState.IsDead = false;
                    GameDataSerializePatch.SerializeMessageCount++;
                    foreach (var info in GameData.Instance.AllPlayers)
                    {
                        if (info.PlayerId == Player.PlayerId)
                        {
                            isDeadCache[info.PlayerId] = (false, false);

                            info.IsDead = false;
                            info.Disconnected = false;
                            continue;
                        }
                        isDeadCache[info.PlayerId] = (info.PlayerId.GetPlayerState().IsDead, info.Disconnected);

                        info.IsDead = false;
                        info.Disconnected = false;
                    }
                    NowUse = true;
                    NowState = AssassinMeeting.Guessing;
                    SendStateRPC();

                    AntiBlackout.SendGameData();
                    GameDataSerializePatch.SerializeMessageCount--;
                    _ = new LateTask(() =>
                    {//ホストだけSetRoleして復活させる。
                        PlayerCatch.AllPlayerControls.DoIf(pc => !pc.IsModClient() && pc.PlayerId != Player.PlayerId,
                        pc => PlayerCatch.GetPlayerById(0).RpcSetRoleDesync(RoleTypes.Crewmate, pc.GetClientId()));
                        ReportDeadBodyPatch.ExReportDeadBody(Player, null, false, "AssassinMeeting", "#ff1919");
                    }, 3, "", true);
                }
            }//, 0.5f, "AssassinShori");
        }
    }
    public override void OnStartMeeting()
    {
        if (NowState is AssassinMeeting.Guessing)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            _ = new LateTask(() =>
            {
                foreach (var info in GameData.Instance.AllPlayers)
                {
                    if (info == null) continue;
                    if (isDeadCache.TryGetValue(info.PlayerId, out var val))
                    {
                        info.IsDead = val.isDead;
                        info.Disconnected = val.Disconnected;
                    }
                }
                isDeadCache.Clear();

                GameDataSerializePatch.SerializeMessageCount++;
                AntiBlackout.SendGameData();
                GameDataSerializePatch.SerializeMessageCount--;

                if (Options.ExHideChatCommand.GetBool())
                {
                    _ = new LateTask(() =>
                    {
                        var count = 0;
                        Dictionary<byte, bool> State = new();
                        foreach (var player in PlayerCatch.AllAlivePlayerControls)
                        {
                            State.TryAdd(player.PlayerId, player.PlayerId == Player.PlayerId ? false : player.Data.IsDead);
                        }
                        foreach (var pc in PlayerCatch.AllAlivePlayerControls)
                        {
                            if (!Main.IsCs() && Options.ExRpcWeightR.GetBool()) count++;

                            if (!State.ContainsKey(pc.PlayerId)) continue;
                            if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
                            if (pc.IsModClient()) continue;

                            _ = new LateTask(() =>
                            {
                                foreach (PlayerControl tg in PlayerCatch.AllAlivePlayerControls)
                                {
                                    if (tg.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
                                    if (tg.IsModClient()) continue;
                                    tg.Data.IsDead = true;
                                }
                                pc.Data.IsDead = false;
                                GameDataSerializePatch.SerializeMessageCount++;
                                RPC.RpcSyncAllNetworkedPlayer(pc.GetClientId());
                                GameDataSerializePatch.SerializeMessageCount--;
                            }, count * 0.1f, "SetDienoNaka", true);
                        }
                        _ = new LateTask(() =>
                        {
                            foreach (PlayerControl player in PlayerCatch.AllAlivePlayerControls)
                            {
                                player.Data.IsDead = State.TryGetValue(player.PlayerId, out var data) && data;
                            }
                        }, count * 0.1f, "SetDienoNaka", true);
                    }, 4f, "SetDie");
                }
            }, 3, "Assassin-SetDie", true);
        }
        else AddRole?.OnStartMeeting();
    }
    public override bool VotingResults(ref NetworkedPlayerInfo Exiled, ref bool IsTie, Dictionary<byte, int> vote, byte[] mostVotedPlayers, bool ClearAndExile)
    {
        if (NowState is AssassinMeeting.EndMeeting or AssassinMeeting.CallMetting) return false;

        if (NowState is AssassinMeeting.Guessing)
        {
            var name = Camouflage.PlayerSkins.TryGetValue(Player.PlayerId, out var cos) ? cos.PlayerName : "^a^";
            var tage = Camouflage.PlayerSkins.TryGetValue(GuessId, out var tcos) ? tcos.PlayerName : "彼";

            if (GuessId is not byte.MaxValue)
            {
                NowState = AssassinMeeting.DieWait;
                Player.RpcSetName(string.Format(GetString("AssassinGuessNonCollect"), tage) + "<size=0>");
                MeetingVoteManager.Voteresult = string.Format(GetString("AssassinGuessNonCollect"), tage);
                Logger.Info($"{GuessId}マーリンじゃない!", "Assassin");
            }
            if (GuessId.GetPlayerState()?.MainRole is CustomRoles.Merlin)
            {
                NowState = AssassinMeeting.Collected;
                Player.RpcSetName(string.Format(GetString("AssassinGuessCollect"), tage) + "<size=0>");
                MeetingVoteManager.Voteresult = string.Format(GetString("AssassinGuessCollect"), tage);
                Logger.Info($"{GuessId}マーリンだ!", "Assassin");
            }
            else
            {
                NowState = AssassinMeeting.DieWait;
                Player.RpcSetName(string.Format(GetString("AssassinGuessNonCollect"), tage) + "<size=0>");
                MeetingVoteManager.Voteresult = string.Format(GetString("AssassinGuessNonCollect"), tage);
            }
            SendStateRPC();
            Exiled = Player.Data;
            _ = new LateTask(() => Player.RpcSetName(name), 6f, "AssassinSetName", true);
            return true;
        }
        else
        if (NowState is AssassinMeeting.WaitMeeting)
        {
            if (Exiled?.PlayerId == Player.PlayerId)
            {
                NowState = AssassinMeeting.CallMetting;
                SendStateRPC();
                Logger.Info("追放されちゃった！", "Assassin");
                Exiled = null;
                IsTie = false;
                ClearAndExile = true;
                return true;
            }
        }


        return AddRole?.VotingResults(ref Exiled, ref IsTie, vote, mostVotedPlayers, ClearAndExile) ?? false;
    }
    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (NowState is not AssassinMeeting.Guessing) return true;

        if (!Is(voter)) return false;
        if (votedForId is MeetingVoteManager.Skip
        || votedForId == Player.PlayerId
        || (votedForId == PlayerControl.LocalPlayer.PlayerId && Options.EnableGM.GetBool())) return false;

        GuessId = votedForId;
        Logger.Info($"{votedForId.GetPlayerControl()?.Data?.GetLogPlayerName() ?? "???"} はマーリンかな?", "Assassin");
        MeetingVoteManager.Instance.ClearAndExile(Player.PlayerId, Player.PlayerId);
        return true;
    }
    public override void OverrideTrueRoleName(ref Color roleColor, ref string roleText)
    {
        if (AddRole is null) return;

        roleText += GetString($"{haverole}");
    }

    public override bool OnEnterVent(PlayerPhysics physics, int ventId) => AddRole?.OnEnterVent(physics, ventId) ?? true;
    public override bool CanVentMoving(PlayerPhysics physics, int ventId) => AddRole?.CanVentMoving(physics, ventId) ?? true;
    public override bool CanUseAbilityButton() => AddRole?.CanUseAbilityButton() ?? false;
    public override void ApplyGameOptions(IGameOptions opt) => AddRole?.ApplyGameOptions(opt);
    public override bool OnCheckMurderAsTarget(MurderInfo info) => AddRole?.OnCheckMurderAsTarget(info) ?? true;
    public override void OnMurderPlayerAsTarget(MurderInfo info) => AddRole?.OnMurderPlayerAsTarget(info);
    public override void OnShapeshift(PlayerControl target) => AddRole?.OnShapeshift(target);
    public override bool CheckShapeshift(PlayerControl target, ref bool shouldAnimate) => AddRole?.CheckShapeshift(target, ref shouldAnimate) ?? true;
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target) => AddRole?.OnReportDeadBody(reporter, target);
    public override bool CanClickUseVentButton => AddRole?.CanClickUseVentButton ?? false;
    public override string MeetingAddMessage() => AddRole?.MeetingAddMessage() ?? "";
    public override (byte? votedForId, int? numVotes, bool doVote) ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional) => AddRole?.ModifyVote(voterId, sourceVotedForId, isIntentional) ?? (null, null, true);
    public override void OnExileWrapUp(NetworkedPlayerInfo exiled, ref bool DecidedWinner) => AddRole?.OnExileWrapUp(exiled, ref DecidedWinner);
    public override void AfterMeetingTasks() => AddRole?.AfterMeetingTasks();
    public override void StartGameTasks() => AddRole?.StartGameTasks();
    public override bool OnInvokeSabotage(SystemTypes systemType) => AddRole?.OnInvokeSabotage(systemType) ?? true;
    public override bool OnSabotage(PlayerControl player, SystemTypes systemType) => AddRole?.OnSabotage(player, systemType) ?? true;
    public override void AfterSabotage(SystemTypes systemType) => AddRole?.AfterSabotage(systemType);
    public override bool NotifyRolesCheckOtherName => AddRole?.NotifyRolesCheckOtherName ?? false;
    public override void OverrideDisplayRoleNameAsSeen(PlayerControl seer, ref bool enabled, ref Color roleColor, ref string roleText, ref bool addon)
    => AddRole?.OverrideDisplayRoleNameAsSeen(seer, ref enabled, ref roleColor, ref roleText, ref addon);
    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, ref bool enabled, ref Color roleColor, ref string roleText, ref bool addon)
    {
        if (NowState is AssassinMeeting.WaitMeeting or AssassinMeeting.CallMetting or AssassinMeeting.Guessing && !Is(seen))
            enabled &= seen.GetCustomRole().IsWhiteCrew();

    }
    public override void OverrideProgressTextAsSeer(PlayerControl seen, ref bool enabled, ref string text)
    => AddRole?.OverrideProgressTextAsSeer(seen, ref enabled, ref text);
    public override void OverrideProgressTextAsSeen(PlayerControl seer, ref bool enabled, ref string text)
    => AddRole?.OverrideProgressTextAsSeen(seer, ref enabled, ref text);
    public override string GetProgressText(bool comms = false, bool GameLog = false) => AddRole?.GetProgressText(comms, GameLog);
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false) => AddRole?.GetMark(seer, seen, isForMeeting);
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false) => AddRole?.GetLowerText(seer, seen, isForMeeting, isForHud);
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false) => AddRole?.GetSuffix(seer, seen, isForMeeting);
    public override string GetAbilityButtonText() => AddRole?.GetAbilityButtonText();
    public override bool OverrideAbilityButton(out string text)
    {
        if (AddRole?.OverrideAbilityButton(out var abilitytext) ?? false)
        {
            text = abilitytext;
            return true;
        }
        text = default;
        return false;
    }
    public override bool CancelReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target, ref DontReportreson reason)
    {
        if (NowState is AssassinMeeting.CallMetting)
        {
            reason = DontReportreson.wait;
            return true;
        }
        return AddRole?.CancelReportDeadBody(reporter, target, ref reason) ?? false;
    }
    public override CustomRoles TellResults(PlayerControl player) => AddRole?.TellResults(player) ?? CustomRoles.NotAssigned;
    public override RoleTypes? AfterMeetingRole => AddRole?.AfterMeetingRole ?? null;
    public override void CheckWinner() => AddRole?.CheckWinner();


    bool IImpostor.CanBeLastImpostor => AddRole is IImpostor impostor ? impostor.CanBeLastImpostor : true;
    public bool CanKill => AddRole is IImpostor impostor ? impostor.CanKill : true;
    public bool IsKiller => AddRole is IImpostor impostor ? impostor.IsKiller : true;
    public bool CanUseKillButton() => AddRole is IImpostor impostor ? impostor.CanUseKillButton() : CanKill;
    public float CalculateKillCooldown() => AddRole is IImpostor impostor ? impostor.CalculateKillCooldown() : Options.DefaultKillCooldown;
    public bool CanUseSabotageButton() => AddRole is IImpostor impostor ? impostor.CanUseSabotageButton() : true;
    public bool CanUseImpostorVentButton() => AddRole is IImpostor impostor ? impostor.CanUseImpostorVentButton() : true;
    public void OnCheckMurderAsKiller(MurderInfo info) { if (AddRole is IImpostor impostor) impostor.OnCheckMurderAsKiller(info); }
    public void OnCheckMurderDontKill(MurderInfo info) { if (AddRole is IImpostor impostor) impostor.OnCheckMurderDontKill(info); }
    public void OnMurderPlayerAsKiller(MurderInfo info) { if (AddRole is IImpostor impostor) impostor.OnCheckMurderAsKiller(info); }
    public bool OverrideKillButtonText(out string text)
    {
        if ((AddRole as IImpostor)?.OverrideKillButtonText(out var killbuttontext) is true)
        {
            text = killbuttontext;
            return true;
        }
        text = default;
        return false;
    }
    public bool OverrideKillButton(out string text)
    {
        if ((AddRole as IImpostor)?.OverrideKillButton(out var killbutton) is true)
        {
            text = killbutton;
            return true;
        }
        text = default;
        return false;
    }
    public bool OverrideImpVentButton(out string text)
    {
        if ((AddRole as IImpostor)?.OverrideImpVentButton(out var ventbutton) is true)
        {
            text = ventbutton;
            return true;
        }
        text = default;
        return false;
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        try
        {
            AddRole.ReceiveRPC(reader);
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex}", "Assassin");
        }
    }

    public void ReceiveStateRPC(MessageReader reader)
    {
        NowState = (AssassinMeeting)reader.ReadInt32();
    }

    public void SendStateRPC() //一時用、いつか別の方法で実装する //いい案なんかないかな、
    {
        if (!PlayerCatch.AnyModClient()) return;
        var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncAssassinState, SendOption.Reliable);
        writer.Write(Player.PlayerId);
        writer.Write((int)NowState);
    }

    bool IUsePhantomButton.IsPhantomRole => AddRole is IUsePhantomButton iusephantom && iusephantom?.IsPhantomRole is true;
    bool IUsePhantomButton.UseOneclickButton => AddRole is IUsePhantomButton iusephantom && iusephantom?.UseOneclickButton is true;
    public void OnClick(ref bool AdjustKillCooldown, ref bool? ResetCooldown)
    {
        if (AddRole is IUsePhantomButton iusephantom)
        {
            iusephantom.OnClick(ref AdjustKillCooldown, ref ResetCooldown);
        }
    }
}