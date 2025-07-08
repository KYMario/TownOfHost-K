using System;
using System.Linq;

using AmongUs.GameOptions;

using TownOfHost.Modules;
using TownOfHost.Modules.ChatManager;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.Impostor;
using TownOfHost.Roles.Neutral;
using UnityEngine;
using static TownOfHost.Modules.SelfVoteManager;

namespace TownOfHost.Roles.Crewmate;

public sealed class AllArounder : RoleBase, ISystemTypeUpdateHook, IKillFlashSeeable, IMeetingTimeAlterable, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(AllArounder),
            player => new AllArounder(player),
            CustomRoles.AllArounder,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            15200,
            SetupOptionItem,
            "AA",
            "#599afb"
        );
    public AllArounder(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        RBait = RandomBait.GetInt();
        RInsender = RandomInsender.GetInt();
        RBakery = RandomBakery.GetInt();
        RDicator = RandomDicator.GetInt();
        RLighter = RandomLighter.GetInt();
        RMeetingSheriff = RandomMeetingSheriff.GetInt();
        RSabotageMaster = RandomSabotageMaster.GetInt();
        RSeer = RandomSeer.GetInt();
        RTimeManager = RandomTimeManager.GetInt();
        RTarapper = RandomTrapper.GetInt();
        RNone = RandomNone.GetInt();
        ROpportunist = RandomOpportunist.GetInt();
        RMadmate = RandomMadmate.GetInt();

        SkillLimit = OptionSkillLimit.GetInt();
    }
    #region Kill
    public override void OnMurderPlayerAsTarget(MurderInfo info)
    {
        if (!CanUseAbility()) return;
        var (killer, target) = info.AttemptTuple;
        if (NowRole is NowMode.Bait or NowMode.Insender)
        {
            var tien = 0f;
            //小数対応
            if (OptSaiaichien.GetFloat() > 0)
            {
                int ti = IRandom.Instance.Next(0, (int)OptSaiaichien.GetFloat() * 10);
                tien = ti * 0.1f;
                Logger.Info($"{tien}sの追加遅延発生!!", "AllArounder");
            }
            if (!info.IsSuicide && !info.IsFakeSuicide)
                _ = new LateTask(() => ReportDeadBodyPatch.DieCheckReport(NowRole is NowMode.Bait ? killer : target, target.Data, NowRole is NowMode.Bait), 0.15f + OptChien.GetFloat() + tien, "AllArounder Self Report");
        }
        if (NowRole is NowMode.Trapper)
        {
            if (info.IsSuicide) return;

            var tmpSpeed = Main.AllPlayerSpeed[killer.PlayerId];
            Main.AllPlayerSpeed[killer.PlayerId] = Main.MinSpeed;
            ReportDeadBodyPatch.CanReport[killer.PlayerId] = false;
            killer.MarkDirtySettings();
            _ = new LateTask(() =>
            {
                Main.AllPlayerSpeed[killer.PlayerId] = tmpSpeed;
                ReportDeadBodyPatch.CanReport[killer.PlayerId] = true;
                killer.MarkDirtySettings();
                RPC.PlaySoundRPC(killer.PlayerId, Sounds.TaskComplete);
            }, OptionBlockMoveTime.GetFloat(), "Trapper BlockMove");
        }
    }
    #endregion
    #region Meeting
    public override string MeetingMeg()
    {
        if (!CanUseAbility() || NowRole is not NowMode.Bakery) return "";
        if (Player.IsAlive())
        {
            string BakeryTitle = $"<size=90%><color=#8f6121>{GetString("Message.BakeryTitle")}</size></color>";
            return BakeryTitle + $"\n<size=70%>{GetString("Message.Bakery")}</size>\n";
        }
        return "";
    }
    public override void OnStartMeeting()
    {
        MeetingCount = 0;
    }
    public override void OnSpawn(bool initialState = false)
    {
        if (initialState || !Player.IsAlive()) return;
        if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Default) return;

        _ = new LateTask(() =>
        {
            SetRole();
            UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: [Player]);
        }, 3, "AllArounderSet");
    }
    #endregion
    #region Vote
    public override (byte? votedForId, int? numVotes, bool doVote) ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional)
    {
        var (votedForId, numVotes, doVote) = base.ModifyVote(voterId, sourceVotedForId, isIntentional);
        var baseVote = (votedForId, numVotes, doVote);
        if (!isIntentional || !CanUseAbility() || !SelfVoteManager.Canuseability() || voterId != Player.PlayerId || sourceVotedForId == Player.PlayerId || sourceVotedForId >= 253 || !Player.IsAlive())
        {
            return baseVote;
        }
        if (NowRole is NowMode.Dictator)
        {
            MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.Suicide, Player.PlayerId);
            PlayerCatch.GetPlayerById(sourceVotedForId).SetRealKiller(Player);
            MeetingVoteManager.Instance.ClearAndExile(Player.PlayerId, sourceVotedForId);
            UtilsGameLog.AddGameLog($"Dictator", string.Format(GetString("Dictator.log"), Utils.GetPlayerColor(Player)));
        }
        return (votedForId, numVotes, false);
    }
    #endregion

    #region Name

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (isForMeeting && Player.IsAlive() && seer.PlayerId == seen.PlayerId && CanUseAbility() && SelfVoteManager.Canuseability())
        {
            if (NowRole is NowMode.Dictator)
            {
                var mes = $"<color={RoleInfo.RoleColorCode}>{GetString("NomalVoteRoleInfoMeg")}</color>";
                return isForHud ? mes : $"<size=40%>{mes}</size>";
            }
            if (NowRole is NowMode.MeetingSheriff)
            {
                var mes = $"<color={RoleInfo.RoleColorCode}>{GetString("SelfVoteRoleInfoMeg")}</color>";
                return isForHud ? mes : $"<size=40%>{mes}</size>";
            }
        }
        return "";
    }

    public override string GetProgressText(bool comms = false, bool GameLog = false)
    {
        var text = UtilsRoleText.GetRoleColorAndtext((CustomRoles)NowRole);

        if (comms)
        {
            text = text.RemoveColorTags();
            text = Utils.ColorString(ModColors.Gray, text);
        }
        return $" <size=40%>{text}</size>";
    }
    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, ref bool enabled, ref UnityEngine.Color roleColor, ref string roleText, ref bool addon)
    {
        roleText = $"<size=60%>{roleText}</size>";
    }
    #endregion
    #region RoleOpt
    public override void ApplyGameOptions(IGameOptions opt)
    {
        var vision = Player.Is(CustomRoles.Lighting) ? Main.DefaultImpostorVision : Main.DefaultCrewmateVision;

        if (NowRole is not NowMode.Lighter || !CanUseAbility())
        {
            opt.SetFloat(FloatOptionNames.CrewLightMod, vision);
            return;
        }

        {
            float wariai = (float)MyTaskState.CompletedTasksCount / (float)MyTaskState.AllTasksCount;
            wariai += 0.35f;

            wariai = Mathf.Clamp(wariai, 0.35f, 1.35f);
            opt.SetFloat(FloatOptionNames.CrewLightMod, vision * wariai);
        }
    }
    #endregion
    #region Task
    public override bool OnCompleteTask(uint taskid)
    {
        if (!CanUseAbility() || NowRole is NowMode.Lighter)
        {
            Player.MarkDirtySettings();
            return true;
        }
        if (!CanUseAbility() || NowRole is not NowMode.TimeManager) return true;

        TMTask++;
        return true;
    }
    #endregion
    #region MeetingTimer
    public bool RevertOnDie => true;

    public int CalculateMeetingTimeDelta()
    {
        if (!CanUseAbility()) return 0;
        var sec = OptionIncreaseMeetingTime.GetInt() * TMTask;
        return sec;
    }

    #endregion
    #region Sheriff
    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (!Canuseability()) return true;
        if (OptionSheriffShotLimit.GetInt() > NowCount && Is(voter) && CanUseAbility() && NowRole is NowMode.MeetingSheriff && (MeetingCount < Option1MeetingMaximum.GetInt() || Option1MeetingMaximum.GetInt() == 0))
        {
            if (CheckSelfVoteMode(Player, votedForId, out var status))
            {
                if (status is VoteStatus.Self)
                    Utils.SendMessage(string.Format(GetString("SkillMode"), GetString("Mode.MeetingSheriff"), GetString("Vote.MeetingSheriff")) + GetString("VoteSkillMode"), Player.PlayerId);
                if (status is VoteStatus.Skip)
                    Utils.SendMessage(GetString("VoteSkillFin"), Player.PlayerId);
                if (status is VoteStatus.Vote)
                    Sheriff(votedForId);
                SetMode(Player, status is VoteStatus.Self);
                return false;
            }
        }
        return true;
    }
    public void Sheriff(byte votedForId)
    {
        PlayerState state;
        var target = PlayerCatch.GetPlayerById(votedForId);
        if (!target.IsAlive()) return;
        if (!AmongUsClient.Instance.AmHost) return;
        var meetingHud = MeetingHud.Instance;
        var hudManager = DestroyableSingleton<HudManager>.Instance.KillOverlay;
        NowCount++;
        MeetingCount++;//1会議のカウント

        //ゲッサーがいるなら～
        if ((PlayerCatch.AllPlayerControls.Any(pc => pc.Is(CustomRoles.Guesser)) || CustomRolesHelper.CheckGuesser()) && !Options.ExHideChatCommand.GetBool())
            ChatManager.SendPreviousMessagesToAll();

        var AlienTairo = false;
        var targetroleclass = target.GetRoleClass();
        if ((targetroleclass as Alien)?.CheckSheriffKill(target) == true) AlienTairo = true;
        if ((targetroleclass as JackalAlien)?.CheckSheriffKill(target) == true) AlienTairo = true;
        if ((targetroleclass as AlienHijack)?.CheckSheriffKill(target) == true) AlienTairo = true;

        if ((CanBeKilledBy(target.GetCustomRole()) && !AlienTairo) || (target.IsRiaju() && OptionMeetingSheriffCanKillLovers.GetBool()) || (target.Is(CustomRoles.Amanojaku) && OptionMeetingSheriffCanKillNeutrals.GetBool()))
        {
            state = PlayerState.GetByPlayerId(target.PlayerId);
            target.RpcExileV2();
            state.DeathReason = CustomDeathReason.Kill;
            state.SetDead();

            UtilsGameLog.AddGameLog($"MeetingSheriff", $"{Utils.GetPlayerColor(target, true)}(<b>{UtilsRoleText.GetTrueRoleName(target.PlayerId, false)}</b>) [{Utils.GetVitalText(target.PlayerId, true)}]");
            UtilsGameLog.AddGameLogsub($"\n\t⇐ {Utils.GetPlayerColor(Player, true)}(<b>{UtilsRoleText.GetTrueRoleName(Player.PlayerId, false)}</b>)");

            if (Options.ExHideChatCommand.GetBool())
            {
                MeetingHudPatch.StartPatch.Serialize = true;
                foreach (var pc in PlayerCatch.AllAlivePlayerControls)
                {
                    if (pc == target) continue;
                    pc.Data.IsDead = false;
                }
                RPC.RpcSyncAllNetworkedPlayer(target.GetClientId());
                MeetingHudPatch.StartPatch.Serialize = false;
            }
            Logger.Info($"{Player.GetNameWithRole().RemoveHtmlTags()}がシェリフ成功({target.GetNameWithRole().RemoveHtmlTags()}) 残り{OptionSheriffShotLimit.GetInt() - NowCount}", "AllArounder");
            Utils.SendMessage(Utils.GetPlayerColor(target, true) + GetString("Meetingkill"), title: GetString("MSKillTitle"));
            foreach (var go in PlayerCatch.AllPlayerControls.Where(pc => pc != null && !pc.IsAlive()))
            {
                Utils.SendMessage(string.Format(GetString("MMeetingKill"), Utils.GetPlayerColor(Player, true), Utils.GetPlayerColor(target, true)), go.PlayerId, GetString("RMSKillTitle"));
            }

            MeetingVoteManager.ResetVoteManager(target.PlayerId);
            return;
        }
        Player.RpcExileV2();
        MyState.DeathReason = target.Is(CustomRoles.Tairou) && Tairou.TairoDeathReason ? CustomDeathReason.Revenge1 :
                            target.Is(CustomRoles.Alien) && Alien.TairoDeathReason ? CustomDeathReason.Revenge1 :
                            (target.Is(CustomRoles.JackalAlien) && JackalAlien.TairoDeathReason ? CustomDeathReason.Revenge1 :
                            (target.Is(CustomRoles.AlienHijack) && Alien.TairoDeathReason ? CustomDeathReason.Revenge1 : CustomDeathReason.Misfire));
        MyState.SetDead();

        UtilsGameLog.AddGameLog($"MeetingSheriff", $"{Utils.GetPlayerColor(Player, true)}(<b>{UtilsRoleText.GetTrueRoleName(Player.PlayerId, false)}</b>) [{Utils.GetVitalText(Player.PlayerId, true)}]");
        UtilsGameLog.AddGameLogsub($"\n\t┗ {GetString("Skillplayer")}{Utils.GetPlayerColor(target, true)}(<b>{UtilsRoleText.GetTrueRoleName(target.PlayerId, false)}</b>)");

        if (Options.ExHideChatCommand.GetBool())
        {
            MeetingHudPatch.StartPatch.Serialize = true;
            foreach (var pc in PlayerCatch.AllAlivePlayerControls)
            {
                if (pc == Player) continue;
                pc.Data.IsDead = false;
            }
            RPC.RpcSyncAllNetworkedPlayer(Player.GetClientId());
            MeetingHudPatch.StartPatch.Serialize = false;
        }
        Logger.Info($"{Player.GetNameWithRole().RemoveHtmlTags()}がシェリフ失敗({target.GetNameWithRole().RemoveHtmlTags()}) 残り{OptionSheriffShotLimit.GetInt() - NowCount}", "AllArounder");
        Utils.SendMessage(Utils.GetPlayerColor(Player, true) + GetString("Meetingkill"), title: GetString("MSKillTitle"));
        foreach (var go in PlayerCatch.AllPlayerControls.Where(pc => pc != null && !pc.IsAlive()))
        {
            Utils.SendMessage(string.Format(GetString("MMeetingKillfall"), Utils.GetPlayerColor(Player, true), Utils.GetPlayerColor(target, true)), go.PlayerId, GetString("RMSKillTitle"));
        }

        MeetingVoteManager.ResetVoteManager(Player.PlayerId);
    }
    bool CanBeKilledBy(CustomRoles role)
    {
        if (role == CustomRoles.SKMadmate) return OptionMeetingSheriffCanKillMadMate.GetBool();
        if (role == CustomRoles.Jackaldoll) return OptionMeetingSheriffCanKillNeutrals.GetBool();

        return role.GetCustomRoleTypes() switch
        {
            CustomRoleTypes.Impostor => role is not CustomRoles.Tairou,
            CustomRoleTypes.Madmate => OptionMeetingSheriffCanKillMadMate.GetBool(),
            CustomRoleTypes.Neutral => OptionMeetingSheriffCanKillNeutrals.GetBool(),
            CustomRoleTypes.Crewmate => role is CustomRoles.WolfBoy,
            _ => false
        };
    }
    #endregion
    #region Sabotage

    bool ISystemTypeUpdateHook.UpdateReactorSystem(ReactorSystemType reactorSystem, byte amount)
    {
        if (!IsSkillAvailable()) return true;
        if (amount.HasAnyBit(ReactorSystemType.AddUserOp))
        {
            //片方を直したタイミング
            ShipStatus.Instance.UpdateSystem((MapNames)Main.NormalOptions.MapId == MapNames.Polus ? SystemTypes.Laboratory : SystemTypes.Reactor, Player, ReactorSystemType.ClearCountdown);
            UsedSkillCount++;
        }
        return true;
    }
    bool ISystemTypeUpdateHook.UpdateHeliSabotageSystem(HeliSabotageSystem heliSabotageSystem, byte amount)
    {
        if (!IsSkillAvailable()) return true;
        var tags = (HeliSabotageSystem.Tags)(amount & HeliSabotageSystem.TagMask);
        if (tags == HeliSabotageSystem.Tags.ActiveBit)
        {
            //パネル開いたタイミング
            fixedSabotage = false;
        }
        if (!fixedSabotage && tags == HeliSabotageSystem.Tags.FixBit)
        {
            //片方の入力が正解したタイミング
            fixedSabotage = true;
            //ヘリサボは16,17がそろったとき完了。
            var consoleId = amount & HeliSabotageSystem.IdMask;
            var otherConsoleId = (consoleId + 1) % 2;
            //もう一方のパネルの完了報告
            ShipStatus.Instance.UpdateSystem(SystemTypes.HeliSabotage, Player, (byte)(otherConsoleId | (int)HeliSabotageSystem.Tags.FixBit));
            UsedSkillCount++;
        }
        return true;
    }
    bool ISystemTypeUpdateHook.UpdateLifeSuppSystem(LifeSuppSystemType lifeSuppSystem, byte amount)
    {
        if (!IsSkillAvailable()) return true;
        if (amount.HasAnyBit(LifeSuppSystemType.AddUserOp))
        {
            //片方の入力が正解したタイミング
            ShipStatus.Instance.UpdateSystem(SystemTypes.LifeSupp, Player, LifeSuppSystemType.ClearCountdown);
            UsedSkillCount++;
        }
        return true;
    }
    bool ISystemTypeUpdateHook.UpdateHqHudSystem(HqHudSystemType hqHudSystemType, byte amount)
    {
        if (!IsSkillAvailable()) return true;
        var tags = (HqHudSystemType.Tags)(amount & HqHudSystemType.TagMask);
        if (tags == HqHudSystemType.Tags.ActiveBit)
        {
            //パネル開いたタイミング
            fixedSabotage = false;
        }
        if (!fixedSabotage && tags == HqHudSystemType.Tags.FixBit)
        {
            //片方の入力が正解したタイミング
            fixedSabotage = true;
            //MiraHQのコミュは16,17がそろったとき完了。
            var consoleId = amount & HqHudSystemType.IdMask;
            var otherConsoleId = (consoleId + 1) % 2;
            //もう一方のパネルの完了報告
            ShipStatus.Instance.UpdateSystem(SystemTypes.Comms, Player, (byte)(otherConsoleId | (int)HqHudSystemType.Tags.FixBit));
            UsedSkillCount++;
        }
        return true;
    }
    bool ISystemTypeUpdateHook.UpdateSwitchSystem(SwitchSystem switchSystem, byte amount)
    {
        if (!IsSkillAvailable()) return true;
        if (amount.HasBit(SwitchSystem.DamageSystem)) return true;
        //いずれかのスイッチが変更されたタイミング
        //現在のスイッチ状態を今から動かすスイッチ以外を正解にする

        var fixbit = 1 << amount;
        switchSystem.ActualSwitches = (byte)(switchSystem.ExpectedSwitches ^ fixbit);
        UsedSkillCount++;
        return true;
    }
    private bool IsSkillAvailable() => (SkillLimit <= 0 || UsedSkillCount < SkillLimit) && NowRole == NowMode.SabotageMaster;
    #endregion
    #region Seer
    public bool? CheckKillFlash(MurderInfo info) // IKillFlashSeeable
    {
        var canseekillflash = CanUseAbility() && NowRole is NowMode.Seer;

        if (canseekillflash)
        {
            var tien = 0f;
            //小数対応
            if (OptionMaxdelay.GetFloat() > 0)
            {
                int ti = IRandom.Instance.Next(0, (int)OptionMaxdelay.GetFloat() * 10);
                tien = ti * 0.1f;
                Logger.Info($"{Player?.Data?.GetLogPlayerName()} => {tien}sの追加遅延発生!!", "AllArounder");
            }
            _ = new LateTask(() =>
            {
                if (GameStates.Meeting || !Player.IsAlive())
                {
                    Logger.Info($"{info?.AppearanceTarget?.Data?.GetLogPlayerName() ?? "???"}のフラッシュを受け取ろうとしたけどなんかし防いだぜ", "AllArounder");
                    return;
                }
                Player.KillFlash();
            }, tien + OptionMindelay.GetFloat(), "SeerDelayKillFlash", null);
            return null;
        }
        return false;
    }

    #endregion
    #region Win
    public bool CheckWin(ref CustomRoles winnerRole)
    {
        if (NowRole is NowMode.Madmate)
        {
            if (CustomWinnerHolder.WinnerTeam is CustomWinner.Impostor)
            {
                return true;
            }
            else if (CustomWinnerHolder.WinnerTeam is CustomWinner.Crewmate)
            {
                CustomWinnerHolder.WinnerIds.Remove(Player.PlayerId);
                return false;
            }
        }
        if (NowRole is NowMode.Opportunist)
        {
            if (Player.IsAlive())
            {
                return true;
            }
            CustomWinnerHolder.WinnerIds.Remove(Player.PlayerId);
            return false;
        }

        return false;
    }
    #endregion
    #region Add/Set
    public override void Add()
    {
        SetRole();
        MeetingCount = 0;
        NowCount = 0;
        UsedSkillCount = 0;
        AddS(Player);
    }
    void SetRole()
    {
        if (!Player.IsAlive()) return;

        var Max = RBait + RInsender + RBakery + RDicator + RLighter + RMeetingSheriff
        + RSabotageMaster + RSeer + RTimeManager + RTarapper + ROpportunist + RMadmate + RNone;

        var chance = IRandom.Instance.Next(1, Max);

        if (chance <= RBait)
        {
            Logger.Info($"{Player.Data.GetLogPlayerName()}はベイト", "AllArounder");
            NowRole = NowMode.Bait;
        }
        else if (chance <= RBait + RInsender)
        {
            Logger.Info($"{Player.Data.GetLogPlayerName()}はインセンダー", "AllArounder");
            NowRole = NowMode.Insender;
        }
        else if (chance <= RBait + RInsender + RBakery)
        {
            Logger.Info($"{Player.Data.GetLogPlayerName()}はパン屋", "AllArounder");
            NowRole = NowMode.Bakery;
        }
        else if (chance <= RBait + RInsender + RBakery + RDicator)
        {
            Logger.Info($"{Player.Data.GetLogPlayerName()}はディク", "AllArounder");
            NowRole = NowMode.Dictator;
        }
        else if (chance <= RBait + RInsender + RBakery + RDicator + RLighter)
        {
            Logger.Info($"{Player.Data.GetLogPlayerName()}はライター", "AllArounder");
            NowRole = NowMode.Lighter;
        }
        else if (chance <= RBait + RInsender + RBakery + RDicator + RLighter + RMeetingSheriff)
        {
            Logger.Info($"{Player.Data.GetLogPlayerName()}はミーシェ", "AllArounder");
            NowRole = NowMode.MeetingSheriff;
        }
        else if (chance <= RBait + RInsender + RBakery + RDicator + RLighter + RMeetingSheriff
        + RSabotageMaster)
        {
            Logger.Info($"{Player.Data.GetLogPlayerName()}はサボマス", "AllArounder");
            NowRole = NowMode.SabotageMaster;
        }
        else if (chance <= RBait + RInsender + RBakery + RDicator + RLighter + RMeetingSheriff
        + RSabotageMaster + RSeer)
        {
            Logger.Info($"{Player.Data.GetLogPlayerName()}はシーア", "AllArounder");
            NowRole = NowMode.Seer;
        }
        else if (chance <= RBait + RInsender + RBakery + RDicator + RLighter + RMeetingSheriff
        + RSabotageMaster + RSeer + RTimeManager)
        {
            Logger.Info($"{Player.Data.GetLogPlayerName()}はタイマネ！", "AllArounder");
            NowRole = NowMode.TimeManager;
        }
        else if (chance <= RBait + RInsender + RBakery + RDicator + RLighter + RMeetingSheriff
        + RSabotageMaster + RSeer + RTimeManager + RTarapper)
        {
            Logger.Info($"{Player.Data.GetLogPlayerName()}はトラッパー！", "AllArounder");
            NowRole = NowMode.Trapper;
        }
        else if (chance <= RBait + RInsender + RBakery + RDicator + RLighter + RMeetingSheriff
        + RSabotageMaster + RSeer + RTimeManager + RTarapper + ROpportunist)
        {
            Logger.Info($"{Player.Data.GetLogPlayerName()}はオポチュニスト", "AllArounder");
            NowRole = NowMode.Opportunist;
        }
        else if (chance <= RBait + RInsender + RBakery + RDicator + RLighter + RMeetingSheriff
        + RSabotageMaster + RSeer + RTimeManager + RTarapper + ROpportunist + RMadmate)
        {
            Logger.Info($"{Player.Data.GetLogPlayerName()}はマッドメイト！", "AllArounder");
            NowRole = NowMode.Madmate;
        }
        else
        {
            Logger.Info($"{Player.Data.GetLogPlayerName()}はむーしょく！", "AllArounder");
            NowRole = NowMode.None;
        }

        hasTasks = () => (NowRole is NowMode.Madmate or NowMode.Opportunist) ? HasTask.ForRecompute : HasTask.True;
    }

    public bool CanUseAbility()
    {
        if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return false;
        if (NowRole is NowMode.SabotageMaster) return MyTaskState.HasCompletedEnoughCountOfTasks(AbilitycanuseTaskCount.GetInt());
        return MyTaskState.HasCompletedEnoughCountOfTasks(AbilitycanuseTaskCount.GetInt()) && !Utils.IsActive(SystemTypes.Comms);
    }

    #endregion
    public enum NowMode
    {
        None = CustomRoles.Crewmate,
        Bait = CustomRoles.Bait,
        Bakery = CustomRoles.Bakery,
        Dictator = CustomRoles.Dictator,
        Insender = CustomRoles.InSender,
        Lighter = CustomRoles.Lighter,
        MeetingSheriff = CustomRoles.MeetingSheriff,
        SabotageMaster = CustomRoles.SabotageMaster,
        Seer = CustomRoles.Seer,
        TimeManager = CustomRoles.TimeManager,
        Trapper = CustomRoles.Trapper,
        Opportunist = CustomRoles.Opportunist,
        Madmate = CustomRoles.Madmate
    }
    #region Option
    static OptionItem RandomNone; int RNone;
    static OptionItem RandomBait; int RBait;
    static OptionItem RandomInsender; int RInsender;
    static OptionItem OptChien; static OptionItem OptSaiaichien;
    static OptionItem RandomBakery; int RBakery;
    static OptionItem RandomDicator; int RDicator;
    static OptionItem RandomLighter; int RLighter;
    static OptionItem RandomMeetingSheriff; int RMeetingSheriff;
    static OptionItem OptionSheriffShotLimit; static OptionItem Option1MeetingMaximum; static OptionItem OptionMeetingSheriffCanKillMadMate; static OptionItem OptionMeetingSheriffCanKillNeutrals; static OptionItem OptionMeetingSheriffCanKillLovers;
    static OptionItem RandomSabotageMaster; int RSabotageMaster;
    static OptionItem OptionSkillLimit;
    static OptionItem RandomSeer; int RSeer;
    static OptionItem OptionMindelay;
    static OptionItem OptionMaxdelay;
    static OptionItem RandomTimeManager; int RTimeManager;
    static OptionItem OptionIncreaseMeetingTime;
    static OptionItem RandomTrapper; int RTarapper;
    static OptionItem OptionBlockMoveTime;
    static OptionItem RandomOpportunist; int ROpportunist;
    static OptionItem RandomMadmate; int RMadmate;
    static OptionItem AbilitycanuseTaskCount;

    public NowMode NowRole;
    int MeetingCount; int NowCount;
    private bool fixedSabotage;
    private int SkillLimit;
    public int UsedSkillCount;
    int TMTask;
    enum OptionName
    {
        AllArounderRandomBait, BaitChien, Baitsaidaichien,
        AllArounderRandomInsender,
        AllArounderRandomBakery,
        AllArounderRandomDicator,
        AllArounderRandomLighter,
        AllArounderRandomMeetingSheriff, SheriffShotLimit, MeetingSheriffCanKillMadMate, MeetingSheriffCanKillNeutrals, meetingmc, SheriffCanKillLovers,
        AllArounderRandomsabotageMaster, SabotageMasterSkillLimit,
        AllArounderRandomSeer, SeerMindelay, SeerMaxdelay,
        AllArounderRandomTimeManager, TimeManagerIncreaseMeetingTime,
        AllArounderRandomTrapper, TrapperBlockMoveTime,
        AllArounderRandomMadmate,
        AllArounderRandomOpportunist,
        AllArounderRandomNone
    }
    private static void SetupOptionItem()
    {
        OverrideTasksData.Create(RoleInfo, 5, tasks: (false, 2, 3, 5));
        AbilitycanuseTaskCount = IntegerOptionItem.Create(RoleInfo, 10, GeneralOption.TaskTrigger, new(0, 255, 1), 4, false);
        RandomBait = IntegerOptionItem.Create(RoleInfo, 12, OptionName.AllArounderRandomBait, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        RandomInsender = IntegerOptionItem.Create(RoleInfo, 13, OptionName.AllArounderRandomInsender, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        {
            OptChien = FloatOptionItem.Create(RoleInfo, 14, OptionName.BaitChien, new(0f, 180f, 0.5f), 3f, false).SetValueFormat(OptionFormat.Seconds)
            .SetCansee(() => RandomBait.GetBool() || RandomInsender.GetBool());
            OptSaiaichien = FloatOptionItem.Create(RoleInfo, 15, OptionName.Baitsaidaichien, new(0f, 180f, 0.5f), 3f, false).SetValueFormat(OptionFormat.Seconds)
            .SetCansee(() => RandomBait.GetBool() || RandomInsender.GetBool());
        }
        RandomBakery = IntegerOptionItem.Create(RoleInfo, 16, OptionName.AllArounderRandomBakery, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        RandomDicator = IntegerOptionItem.Create(RoleInfo, 17, OptionName.AllArounderRandomDicator, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        RandomLighter = IntegerOptionItem.Create(RoleInfo, 18, OptionName.AllArounderRandomLighter, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        RandomMeetingSheriff = IntegerOptionItem.Create(RoleInfo, 19, OptionName.AllArounderRandomMeetingSheriff, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        {
            OptionSheriffShotLimit = FloatOptionItem.Create(RoleInfo, 20, OptionName.SheriffShotLimit, new(1f, 15f, 1f), 1f, false, RandomMeetingSheriff)
                .SetValueFormat(OptionFormat.Times);
            OptionMeetingSheriffCanKillMadMate = BooleanOptionItem.Create(RoleInfo, 21, OptionName.MeetingSheriffCanKillMadMate, true, false, RandomMeetingSheriff);
            OptionMeetingSheriffCanKillNeutrals = BooleanOptionItem.Create(RoleInfo, 22, OptionName.MeetingSheriffCanKillNeutrals, true, false, RandomMeetingSheriff);
            OptionMeetingSheriffCanKillLovers = BooleanOptionItem.Create(RoleInfo, 23, OptionName.SheriffCanKillLovers, true, false, RandomMeetingSheriff);
            Option1MeetingMaximum = FloatOptionItem.Create(RoleInfo, 24, OptionName.meetingmc, new(0f, 99f, 1f), 0f, false, RandomMeetingSheriff, infinity: true)
                .SetValueFormat(OptionFormat.Times);
        }
        RandomSabotageMaster = IntegerOptionItem.Create(RoleInfo, 25, OptionName.AllArounderRandomsabotageMaster, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        {
            OptionSkillLimit = FloatOptionItem.Create(RoleInfo, 26, OptionName.SabotageMasterSkillLimit, new(0, 99, 1), 1, false, RandomSabotageMaster, infinity: true)
            .SetValueFormat(OptionFormat.Times);
        }
        RandomSeer = IntegerOptionItem.Create(RoleInfo, 27, OptionName.AllArounderRandomSeer, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        {
            OptionMindelay = FloatOptionItem.Create(RoleInfo, 35, OptionName.SeerMindelay, new(0, 60, 0.5f), 3f, false, RandomSeer).SetValueFormat(OptionFormat.Seconds);
            OptionMaxdelay = FloatOptionItem.Create(RoleInfo, 36, OptionName.SeerMaxdelay, new(0, 60, 0.5f), 3f, false, RandomSeer).SetValueFormat(OptionFormat.Seconds);
        }
        RandomTimeManager = IntegerOptionItem.Create(RoleInfo, 28, OptionName.AllArounderRandomTimeManager, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        {
            OptionIncreaseMeetingTime = IntegerOptionItem.Create(RoleInfo, 29, OptionName.TimeManagerIncreaseMeetingTime, new(5, 30, 1), 15, false, RandomTimeManager)
                .SetValueFormat(OptionFormat.Seconds);
        }
        RandomTrapper = IntegerOptionItem.Create(RoleInfo, 30, OptionName.AllArounderRandomTrapper, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        {
            OptionBlockMoveTime = FloatOptionItem.Create(RoleInfo, 31, OptionName.TrapperBlockMoveTime, new(1f, 180f, 1f), 5f, false, RandomTrapper)
            .SetValueFormat(OptionFormat.Seconds);
        }
        RandomOpportunist = IntegerOptionItem.Create(RoleInfo, 32, OptionName.AllArounderRandomOpportunist, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        RandomMadmate = IntegerOptionItem.Create(RoleInfo, 33, OptionName.AllArounderRandomMadmate, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
        RandomNone = IntegerOptionItem.Create(RoleInfo, 34, OptionName.AllArounderRandomNone, new(0, 100, 5), 100, false).SetValueFormat(OptionFormat.Percent);
    }
    #endregion
}