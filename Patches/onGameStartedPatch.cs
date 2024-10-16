using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using BepInEx.Unity.IL2CPP.Utils.Collections;

using TownOfHost.Modules;
using TownOfHost.Roles;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.AddOns.Common;
using TownOfHost.Roles.AddOns.Impostor;
using TownOfHost.Roles.AddOns.Crewmate;
using TownOfHost.Roles.AddOns.Neutral;
using static TownOfHost.Translator;
using TownOfHost.Roles.Neutral;
using TownOfHost.Modules.ChatManager;
using TownOfHost.Roles.Ghost;
using TownOfHost.Roles.Crewmate;
//using AmongUs.Data.Settings;

namespace TownOfHost
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CoStartGame))]
    class ChangeRoleSettings
    {
        public static void Postfix(AmongUsClient __instance)
        {
            //注:この時点では役職は設定されていません。
            Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.GuardianAngel, 0, 0);

            var op = Main.NormalOptions;
            if (op.NumCommonTasks + op.NumLongTasks + op.NumShortTasks >= 255)
            {
                Main.NormalOptions.SetInt(Int32OptionNames.NumCommonTasks, 85);
                Main.NormalOptions.SetInt(Int32OptionNames.NumLongTasks, 84);
                Main.NormalOptions.SetInt(Int32OptionNames.NumShortTasks, 84);
                Logger.Error($"全体のタスクが255を超えています", "CoStartGame ChTask");
            }

            PlayerState.Clear();

            Main.AllPlayerKillCooldown = new Dictionary<byte, float>();
            Main.AllPlayerFirstTypes = new Dictionary<byte, CustomRoleTypes>();
            Main.AllPlayerSpeed = new Dictionary<byte, float>();
            Main.LastLog = new Dictionary<byte, string>();
            Main.LastLogRole = new Dictionary<byte, string>();
            Main.LastLogPro = new Dictionary<byte, string>();
            Main.LastLogSubRole = new Dictionary<byte, string>();
            Main.KillCount = new Dictionary<byte, int>();
            Main.Guard = new Dictionary<byte, int>();
            Main.AllPlayerTask = new Dictionary<byte, List<uint>>();
            GhostRoleAssingData.GhostAssingCount = new Dictionary<CustomRoles, int>();

            Main.SKMadmateNowCount = 0;

            Main.AfterMeetingDeathPlayers = new();
            Main.clientIdList = new();

            Main.CheckShapeshift = new();
            Main.ShapeshiftTarget = new();

            ReportDeadBodyPatch.CanReport = new();
            ReportDeadBodyPatch.Musisuruoniku = new();

            Options.UsedButtonCount = 0;
            Main.RealOptionsData = new OptionBackupData(GameOptionsManager.Instance.CurrentGameOptions);

            Main.introDestroyed = false;

            MeetingTimeManager.Init();
            Main.DefaultCrewmateVision = Main.RealOptionsData.GetFloat(FloatOptionNames.CrewLightMod);
            Main.DefaultImpostorVision = Main.RealOptionsData.GetFloat(FloatOptionNames.ImpostorLightMod);

            Main.LastNotifyNames = new();

            Main.PlayerColors = new();
            //名前の記録
            Main.AllPlayerNames = new();

            SelectRolesPatch.roleAssigned = false;
            SelectRolesPatch.senders2 = new();
            HudManagerCoShowIntroPatch.Cancel = true;
            RpcSetTasksPatch.taskIds.Clear();

            Camouflage.Init();
            var invalidColor = Main.AllPlayerControls.Where(p => p.Data.DefaultOutfit.ColorId < 0 || Palette.PlayerColors.Length <= p.Data.DefaultOutfit.ColorId);
            if (invalidColor.Any())
            {
                var msg = Translator.GetString("Error.InvalidColor");
                Logger.seeingame(msg);
                msg += "\n" + string.Join(",", invalidColor.Select(p => $"{p.name}({p.Data.DefaultOutfit.ColorId})"));
                Utils.SendMessage(msg);
                Logger.Error(msg, "CoStartGame");
            }

            foreach (var target in Main.AllPlayerControls)
            {
                foreach (var seer in Main.AllPlayerControls)
                {
                    var pair = (target.PlayerId, seer.PlayerId);
                    Main.LastNotifyNames[pair] = target.name;
                }
            }
            foreach (var pc in Main.AllPlayerControls)
            {
                var colorId = pc.Data.DefaultOutfit.ColorId;
                if (AmongUsClient.Instance.AmHost && Options.ColorNameMode.GetBool()) pc.RpcSetName(Palette.GetColorName(colorId));
                PlayerState.Create(pc.PlayerId);
                Main.AllPlayerNames[pc.PlayerId] = pc?.Data?.PlayerName;
                Main.PlayerColors[pc.PlayerId] = Palette.PlayerColors[colorId];
                Main.AllPlayerSpeed[pc.PlayerId] = Main.RealOptionsData.GetFloat(FloatOptionNames.PlayerSpeedMod); //移動速度をデフォルトの移動速度に変更
                ReportDeadBodyPatch.CanReport[pc.PlayerId] = true;
                ReportDeadBodyPatch.WaitReport[pc.PlayerId] = new();
                ReportDeadBodyPatch.Musisuruoniku[pc.PlayerId] = true;
                Main.KillCount.Add(pc.PlayerId, 0);
                pc.cosmetics.nameText.text = pc.name;

                var outfit = pc.Data.DefaultOutfit;
                Camouflage.PlayerSkins[pc.PlayerId] = new NetworkedPlayerInfo.PlayerOutfit().Set(outfit.PlayerName, outfit.ColorId, outfit.HatId, outfit.SkinId, outfit.VisorId, outfit.PetId);
                Main.clientIdList.Add(pc.GetClientId());
            }
            Main.VisibleTasksCount = true;
            if (__instance.AmHost)
            {
                RPC.SyncCustomSettingsRPC();
                if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
                {
                    if (!Main.HnSFlag)
                        Options.HideAndSeekKillDelayTimer = Options.KillDelay.GetFloat();
                }
                if (Options.IsStandardHAS)
                {
                    Options.HideAndSeekKillDelayTimer = Options.StandardHASWaitingTime.GetFloat();
                }
            }

            SelectRolesPatch.Disconnected.Clear();
            ExileControllerWrapUpPatch.AllSpawned = true;
            RpcSetTasksPatch.HostFin = false;
            Main.DontGameSet = Options.NoGameEnd.GetBool();
            CustomRoleManager.Initialize();
            DisableDevice.Reset();
            FallFromLadder.Reset();
            LastImpostor.Init();
            LastNeutral.Init();
            TargetArrow.Init();
            GetArrow.Init();
            DoubleTrigger.Init();
            watching.Init();
            Serial.Init();
            Management.Init();
            Speeding.Init();
            Guarding.Init();
            Connecting.Init();
            Opener.Init();
            Moon.Init();
            Tiebreaker.Init();
            Amnesia.Init();
            Lighting.Init();
            seeing.Init();
            Revenger.Init();
            Amanojaku.Init();
            Guesser.Init();
            Autopsy.Init();
            Workhorse.Init();
            Ghostbuttoner.Init();
            GhostNoiseSender.Init();
            GhostReseter.Init();
            DemonicTracker.Init();
            DemonicCrusher.Init();
            DemonicVenter.Init();
            AsistingAngel.Init();
            PlayerSkinPatch.RemoveAll();
            NonReport.Init();
            Notvoter.Init();
            PlusVote.Init();
            Elector.Init();
            InfoPoor.Init();
            Water.Init();
            SlowStarter.Init();
            Slacker.Init();
            Transparent.Init();
            Clumsy.Init();
            CustomWinnerHolder.Reset();
            AntiBlackout.Reset();
            GuessManager.Guessreset();
            Madonna.Mareset();
            SelfVoteManager.Init();
            IRandom.SetInstanceById(Options.RoleAssigningAlgorithm.GetValue());
            ChatManager.ResetChat();
            SuddenDeathMode.Reset();
            RandomSpawn.SpawnMap.NextSporn.Clear();
            RandomSpawn.SpawnMap.NextSpornName.Clear();
            Roles.Madmate.MadAvenger.Skill = false;
            MeetingStates.MeetingCalled = false;
            MeetingStates.FirstMeeting = true;
            GameStates.AlreadyDied = false;
            GameStates.Intro = true;
            GameStates.task = false;
            HudManagerPatch.ch = null;
            GameStates.AfterIntro = false;
            GameStates.Meeting = false;
            GameStates.Tuihou = false;
            GameStates.canmusic = false;
            MeetingVoteManager.Voteresult = "";
            MeetingHudPatch.Oniku = "";
            MeetingHudPatch.Send = "";
            Utils.MeetingMoji = "";
            Main.day = 1;
            Main.FixTaskNoPlayer.Clear();
            Main.IntroHyoji = true;
            Utils.TaskCh = true;
            Main.NowSabotage = false;
            JackalDoll.side = 0;
            Balancer.Id = 255;

            Main.FeColl = 0;
            Main.GameCount++;
            Logger.Info($"================{Main.GameCount}試合目================", "OnGamStarted");
            Main.Time = (Main.NormalOptions?.DiscussionTime ?? 0, Main.NormalOptions?.VotingTime ?? 180);
            var c = string.Format(GetString("log.Start"), Main.GameCount);
            Main.gamelog = $"<size=60%>{DateTime.Now:HH.mm.ss} [Start]{c}\n</size><size=80%>" + string.Format(GetString("Message.Day"), Main.day).Color(Palette.Orange) + "</size><size=60%>";
            if (Options.CuseVent.GetBool() && (Options.CuseVentCount.GetFloat() >= Main.AllAlivePlayerControls.Count())) Utils.CanVent = true;
            else Utils.CanVent = false;

            if (GameStates.IsOnlineGame)
            {
                var sn = ServerManager.Instance.CurrentRegion.TranslateName;
                if (sn is StringNames.ServerNA or StringNames.ServerEU or StringNames.ServerSA)
                    Main.LagTime = 0.43f;
                else Main.LagTime = 0.23f;
            }
            else Main.LagTime = 0.23f;
            Logger.Info($"LagTime : {Main.LagTime} PlayerCount : {Main.AllPlayerControls.Count()}", "OnGamStarted Fin");
        }
    }
    [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
    class SelectRolesPatch
    {
        public static List<byte> Disconnected = new();
        public static bool roleAssigned = false;
        public static Dictionary<byte, CustomRpcSender> senders2 = new();
        public static void Prefix()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            //CustomRpcSenderとRpcSetRoleReplacerの初期化
            Dictionary<byte, CustomRpcSender> senders = new();
            foreach (var pc in Main.AllPlayerControls)
            {
                senders[pc.PlayerId] = new CustomRpcSender($"{pc.name}'s SetRole Sender", SendOption.Reliable, false)
                        .StartMessage(pc.GetClientId());
            }
            RpcSetRoleReplacer.StartReplace(senders);

            RoleAssignManager.SelectAssignRoles();

            if (Options.CurrentGameMode != CustomGameMode.HideAndSeek)
            {
                if (Options.CurrentGameMode == CustomGameMode.TaskBattle)
                {
                    Main.TaskBattleTeams.Clear();
                    if (Options.TaskBattleTeamMode.GetBool())
                    {
                        var rand = new Random();
                        var AllPlayerCount = Options.EnableGM.GetBool() ? Main.AllPlayerControls.Count() - 1 : Main.AllPlayerControls.Count();
                        var teamc = Math.Min(Options.TaskBattleTeamC.GetFloat(), AllPlayerCount);
                        var c = AllPlayerCount / teamc;//1チームのプレイヤー数 ↑チーム数
                        List<PlayerControl> ap = new();
                        List<byte> playerlist = new();
                        foreach (var pc in Main.AllPlayerControls)
                            ap.Add(pc);
                        if (Options.EnableGM.GetBool())
                            ap.RemoveAll(x => x == PlayerControl.LocalPlayer);
                        Logger.Info($"{teamc},{c}", "TB");
                        for (var i = 0; teamc > i; i++)
                        {
                            Logger.Info($"team{i}", "TB");
                            playerlist.Clear();
                            for (var i2 = 0; c > i2; i2++)
                            {
                                if (ap.Count == 0) continue;
                                var player = ap[rand.Next(0, ap.Count)];
                                playerlist.Add(player.PlayerId);
                                Logger.Info($"{player.PlayerId}", "TB");
                                ap.Remove(player);
                            }
                            Main.TaskBattleTeams.Add(new List<byte>(playerlist));
                        }
                    }
                    foreach (var pc in Main.AllPlayerControls)
                    {
                        if (Options.EnableGM.GetBool() && pc.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                        {
                            PlayerControl.LocalPlayer.RpcSetCustomRole(CustomRoles.GM);
                            PlayerControl.LocalPlayer.RpcSetRole(RoleTypes.Crewmate, false);
                            PlayerControl.LocalPlayer.Data.IsDead = true;
                        }
                        else
                        {
                            pc.RpcSetCustomRole(CustomRoles.TaskPlayerB);
                            pc.RpcSetRole(Options.TaskBattleCanVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate, false);
                        }
                    }
                }
                else
                {
                    RoleTypes[] RoleTypesList = { RoleTypes.Scientist, RoleTypes.Engineer, RoleTypes.Tracker, RoleTypes.Noisemaker, RoleTypes.Shapeshifter, RoleTypes.Phantom };
                    foreach (var roleTypes in RoleTypesList)
                    {
                        var roleOpt = Main.NormalOptions.roleOptions;
                        int numRoleTypes = GetRoleTypesCount(roleTypes);
                        roleOpt.SetRoleRate(roleTypes, numRoleTypes, numRoleTypes > 0 ? 100 : 0);
                    }

                    List<PlayerControl> AllPlayers = new();
                    foreach (var pc in Main.AllPlayerControls)
                    {
                        AllPlayers.Add(pc);
                    }

                    if (Options.EnableGM.GetBool())
                    {
                        AllPlayers.RemoveAll(x => x == PlayerControl.LocalPlayer);
                        PlayerControl.LocalPlayer.RpcSetCustomRole(CustomRoles.GM);
                        PlayerControl.LocalPlayer.RpcSetRole(RoleTypes.Crewmate, Main.SetRoleOverride && Options.CurrentGameMode == CustomGameMode.Standard);
                        PlayerControl.LocalPlayer.Data.IsDead = true;
                    }
                    if (DebugModeManager.EnableTOHkDebugMode.GetBool())
                    {
                        if (Main.HostRole != CustomRoles.NotAssigned)
                        {
                            AllPlayers.RemoveAll(x => x == PlayerControl.LocalPlayer);
                            PlayerControl.LocalPlayer.RpcSetCustomRole(Main.HostRole, true);
                            PlayerControl.LocalPlayer.RpcSetRole(Main.HostRole.GetRoleInfo()?.BaseRoleType.Invoke() ?? RoleTypes.Crewmate, Main.SetRoleOverride && Options.CurrentGameMode == CustomGameMode.Standard);
                            PlayerControl.LocalPlayer.Data.IsDead = true;
                        }
                    }
                    Dictionary<(byte, byte), RoleTypes> rolesMap = new();
                    foreach (var (role, info) in CustomRoleManager.AllRolesInfo)
                    {
                        if (info.IsDesyncImpostor || role.IsMadmate() || Options.SuddenDeathMode.GetBool())
                        {
                            AssignDesyncRole(role, AllPlayers, senders, rolesMap, BaseRole: info.BaseRoleType.Invoke());
                        }
                    }
                    MakeDesyncSender(senders, rolesMap);
                }
            }
            //以下、バニラ側の役職割り当てが入る
        }
        public static void Postfix()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            RpcSetRoleReplacer.Release(); //保存していたSetRoleRpcを一気に書く
            RpcSetRoleReplacer.senders.Do(kvp => kvp.Value.SendMessage());

            // 不要なオブジェクトの削除
            RpcSetRoleReplacer.senders = null;
            RpcSetRoleReplacer.OverriddenSenderList = null;
            RpcSetRoleReplacer.StoragedData = null;

            //Utils.ApplySuffix();

            var rand = IRandom.Instance;

            List<PlayerControl> Crewmates = new();
            List<PlayerControl> Impostors = new();
            List<PlayerControl> Scientists = new();
            List<PlayerControl> Engineers = new();
            List<PlayerControl> Trackers = new();
            List<PlayerControl> Noisemakers = new();
            List<PlayerControl> GuardianAngels = new();
            List<PlayerControl> Shapeshifters = new();
            List<PlayerControl> Phantoms = new();

            foreach (var pc in Main.AllPlayerControls)
            {
                pc.Data.IsDead = false; //プレイヤーの死を解除する
                var state = PlayerState.GetByPlayerId(pc.PlayerId);
                if (state.MainRole != CustomRoles.NotAssigned) continue; //既にカスタム役職が割り当てられていればスキップ
                var role = CustomRoles.NotAssigned;
                switch (pc.Data.Role.Role)
                {
                    case RoleTypes.Crewmate:
                        Crewmates.Add(pc);
                        role = CustomRoles.Crewmate;
                        break;
                    case RoleTypes.Impostor:
                        Impostors.Add(pc);
                        role = CustomRoles.Impostor;
                        break;
                    case RoleTypes.Scientist:
                        Scientists.Add(pc);
                        role = CustomRoles.Scientist;
                        break;
                    case RoleTypes.Engineer:
                        Engineers.Add(pc);
                        role = CustomRoles.Engineer;
                        break;
                    case RoleTypes.Tracker:
                        Trackers.Add(pc);
                        role = CustomRoles.Tracker;
                        break;
                    case RoleTypes.Noisemaker:
                        Noisemakers.Add(pc);
                        role = CustomRoles.Noisemaker;
                        break;
                    case RoleTypes.GuardianAngel:
                        GuardianAngels.Add(pc);
                        role = CustomRoles.GuardianAngel;
                        break;
                    case RoleTypes.Shapeshifter:
                        Shapeshifters.Add(pc);
                        role = CustomRoles.Shapeshifter;
                        break;
                    case RoleTypes.Phantom:
                        Phantoms.Add(pc);
                        role = CustomRoles.Phantom;
                        break;
                    default:
                        Logger.seeingame(string.Format(GetString("Error.InvalidRoleAssignment"), pc?.Data?.PlayerName));
                        break;
                }
                state.SetMainRole(role);
            }

            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                SetColorPatch.IsAntiGlitchDisabled = true;
                if (!Main.HnSFlag)
                    foreach (var pc in Main.AllPlayerControls)
                    {
                        if (pc.Is(CustomRoleTypes.Impostor))
                            pc.RpcSetColor(0);
                        else if (pc.Is(CustomRoleTypes.Crewmate))
                            pc.RpcSetColor(1);
                    }

                //役職設定処理
                AssignCustomRolesFromList(CustomRoles.HASFox, Crewmates);
                AssignCustomRolesFromList(CustomRoles.HASTroll, Crewmates);
                foreach (var pair in PlayerState.AllPlayerStates)
                {
                    //RPCによる同期
                    ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value.MainRole);
                }
                //色設定処理
                SetColorPatch.IsAntiGlitchDisabled = true;
                GameEndChecker.SetPredicateToHideAndSeek();
            }
            else if (Options.CurrentGameMode == CustomGameMode.TaskBattle)
            {
                AssignCustomRolesFromList(CustomRoles.TaskPlayerB, Crewmates);
                foreach (var pair in PlayerState.AllPlayerStates)
                {
                    //RPCによる同期
                    ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value.MainRole);
                }

                if (Options.TaskBattleTeamMode.GetBool())
                {
                    foreach (var pc in Main.AllPlayerControls)
                        foreach (var t in Main.TaskBattleTeams)
                        {
                            if (!t.Contains(pc.PlayerId)) continue;
                            foreach (var id in t.Where(id => id != pc.PlayerId))
                                NameColorManager.Add(pc.PlayerId, id);
                        }
                }

                GameEndChecker.SetPredicateToTaskBattle();
            }
            else
            {
                foreach (var role in CustomRolesHelper.AllStandardRoles)
                {
                    if (role.IsVanilla()) continue;
                    if (CustomRoleManager.GetRoleInfo(role)?.IsDesyncImpostor == true) continue;
                    if (role.IsMadmate()) continue;
                    if (Options.SuddenDeathMode.GetBool()) continue;
                    var baseRoleTypes = role.GetRoleTypes() switch
                    {
                        RoleTypes.Impostor => Impostors,
                        RoleTypes.Shapeshifter => Shapeshifters,
                        RoleTypes.Phantom => Phantoms,
                        RoleTypes.Scientist => Scientists,
                        RoleTypes.Engineer => Engineers,
                        RoleTypes.Tracker => Trackers,
                        RoleTypes.Noisemaker => Noisemakers,
                        RoleTypes.GuardianAngel => GuardianAngels,
                        _ => Crewmates,
                    };
                    AssignCustomRolesFromList(role, baseRoleTypes);
                }
                Lovers.AssignLoversRoles();
                AddOnsAssignDataOnlyKiller.AssignAddOnsFromList();
                AddOnsAssignDataTeamImp.AssignAddOnsFromList();
                AddOnsAssignData.AssignAddOnsFromList();

                foreach (var pair in PlayerState.AllPlayerStates)
                {
                    ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value.MainRole);

                    foreach (var subRole in pair.Value.SubRoles)
                        ExtendedPlayerControl.RpcSetCustomRole(pair.Key, subRole);
                }

                CustomRoleManager.CreateInstance();
                foreach (var pc in Main.AllPlayerControls)
                {
                    HudManager.Instance.SetHudActive(true);
                    pc.ResetKillCooldown();

                    //通常モードでかくれんぼをする人用
                    if (Options.IsStandardHAS)
                    {
                        foreach (var seer in Main.AllPlayerControls)
                        {
                            if (seer == pc) continue;
                            if (pc.GetCustomRole().IsImpostor() || pc.IsNeutralKiller()) //変更対象がインポスター陣営orキル可能な第三陣営
                                NameColorManager.Add(seer.PlayerId, pc.PlayerId);
                        }
                    }
                }

                RoleTypes[] RoleTypesList = { RoleTypes.Scientist, RoleTypes.Engineer, RoleTypes.Tracker, RoleTypes.Noisemaker, RoleTypes.Shapeshifter, RoleTypes.Phantom };
                foreach (var roleTypes in RoleTypesList)
                {
                    var roleOpt = Main.NormalOptions.roleOptions;
                    roleOpt.SetRoleRate(roleTypes, 0, 0);
                }
                if (!Options.SuddenDeathMode.GetBool()) GameEndChecker.SetPredicateToNormal();
                else GameEndChecker.SetPredicateToSadness();
            }
            GameOptionsSender.AllSenders.Clear();
            foreach (var pc in Main.AllPlayerControls)
            {
                GameOptionsSender.AllSenders.Add(
                    new PlayerGameOptionsSender(pc)
                );
            }

            /*
            //インポスターのゴーストロールがクルーになるバグ対策
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc.Data.Role.IsImpostor || Main.ResetCamPlayerList.Contains(pc.PlayerId))
                {
                    pc.Data.Role.DefaultGhostRole = RoleTypes.ImpostorGhost;
                }
            }
            */

            //コネクティングが1ならコネクティングを削除
            if (Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Connecting)).Count() == 1)
            {
                Main.AllPlayerControls.Where(x => x.Is(CustomRoles.Connecting)).ToArray().Do(
                            p => PlayerState.GetByPlayerId(p.PlayerId).RemoveSubRole(CustomRoles.Connecting));
            }

            //役職選定後に処理する奴。
            foreach (var pc in Main.AllPlayerControls)
            {
                //Log
                var color = Palette.CrewmateBlue;
                if (pc.Is(CustomRoleTypes.Impostor) || pc.Is(CustomRoleTypes.Madmate)) color = Palette.ImpostorRed;
                if (pc.Is(CustomRoleTypes.Neutral)) color = Utils.GetRoleColor(pc.GetCustomRole());
                Main.LastLog[pc.PlayerId] = ("<b>" + Utils.ColorString(Main.PlayerColors[pc.PlayerId], Main.AllPlayerNames[pc.PlayerId] + "</b>")).Mark(color, false);
                Main.LastLogRole[pc.PlayerId] = "<b>" + Utils.ColorString(Utils.GetRoleColor(pc.GetCustomRole()), GetString($"{pc.GetCustomRole()}")) + "</b>";
                Main.AllPlayerFirstTypes.Add(pc.PlayerId, pc.GetCustomRole().GetCustomRoleTypes());
                //FixTask
                var roleClass = CustomRoleManager.GetByPlayerId(pc.PlayerId);
                if (roleClass != null)
                    if (roleClass.HasTasks == HasTask.False)
                        Main.FixTaskNoPlayer.Add(pc);
                //Addons
                Main.Guard.Add(pc.PlayerId, 0);
                if (pc.Is(CustomRoles.Guarding)) Main.Guard[pc.PlayerId] += Guarding.Guard;
                //RoleAddons
                if (RoleAddAddons.AllData.TryGetValue(pc.GetCustomRole(), out var d) && d.GiveAddons.GetBool())
                {
                    if (d.GiveGuarding.GetBool()) Main.Guard[pc.PlayerId] += d.Guard.GetInt();
                    if (d.GiveSpeeding.GetBool()) Main.AllPlayerSpeed[pc.PlayerId] = d.Speed.GetFloat();
                }
                if (!Main.AllPlayerKillCooldown.ContainsKey(pc.PlayerId)) Main.AllPlayerKillCooldown.Add(pc.PlayerId, Options.DefaultKillCooldown);
            }
            if (Options.CurrentGameMode is CustomGameMode.Standard && Main.SetRoleOverride)
                AmongUsClient.Instance.StartCoroutine(CoReSetRole(AmongUsClient.Instance).WrapToIl2Cpp());

            Utils.CountAlivePlayers(true);
            Utils.SyncAllSettings();
            SetColorPatch.IsAntiGlitchDisabled = false;
        }
        private static System.Collections.IEnumerator CoReSetRole(AmongUsClient self)
        {
            yield return new UnityEngine.WaitForSeconds(Main.LagTime + 0.3f);//MakeDesyncSenderが送られるまで待つ
            foreach (var info in GameData.Instance.AllPlayers)
            {
                if (info.Disconnected)
                    Disconnected.Add(info.PlayerId);
                info.Disconnected = true;
                info.SetDirtyBit(0b_1u << info.PlayerId);
            }
            RPC.RpcSyncAllNetworkedPlayer();
            Logger.Info("Disconnected", "RSetRole");
            new LateTask(() =>
            {
                foreach (var pc in Main.AllPlayerControls)
                {
                    if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
                    var hostRole = PlayerControl.LocalPlayer.GetCustomRole();
                    if (Options.EnableGM.GetBool())//こうしないとGMが動かない
                        PlayerControl.LocalPlayer.RpcSetRoleDesync(RoleTypes.Crewmate, pc.GetClientId());
                    else
                        PlayerControl.LocalPlayer.RpcSetRoleDesync(
                            Options.SuddenDeathMode.GetBool() || pc.GetCustomRole().GetRoleInfo().IsDesyncImpostor || hostRole.GetRoleInfo().IsDesyncImpostor ? RoleTypes.Crewmate : hostRole.GetRoleTypes(), pc.GetClientId());
                }
            }, Main.LagTime, "SetHostRole");
            new LateTask(() =>
            {
                foreach (var info in GameData.Instance.AllPlayers)
                {
                    if (Disconnected.Contains(info.PlayerId))
                        continue;
                    info.Disconnected = false;
                    info.SetDirtyBit(0b_1u << info.PlayerId);
                }
                foreach (var pc in Main.AllPlayerControls)
                {
                    if (Disconnected.Contains(pc.PlayerId))
                        continue;
                    pc.Data.Disconnected = false;
                    pc.Data.SetDirtyBit(0b_1u << pc.Data.PlayerId);
                }
                RPC.RpcSyncAllNetworkedPlayer();
            }, Main.LagTime * 2.5f, "UnDisconnected");

            yield return new UnityEngine.WaitForSeconds(Main.LagTime);
            PlayerControl.AllPlayerControls.ForEach((Action<PlayerControl>)(pc => PlayerNameColor.Set(pc)));
            PlayerControl.LocalPlayer.StopAllCoroutines();
            HudManagerCoShowIntroPatch.Cancel = false;
            DestroyableSingleton<HudManager>.Instance.StartCoroutine(DestroyableSingleton<HudManager>.Instance.CoShowIntro());
            DestroyableSingleton<HudManager>.Instance.HideGameLoader();

            yield return new UnityEngine.WaitForSeconds(0.2f);

            if (!Main.IsroleAssigned)
            {
                roleAssigned = true;
                Main.AllPlayerControls.DoIf(x => RpcSetTasksPatch.taskIds.ContainsKey(x.PlayerId), pc => pc.Data.RpcSetTasks(RpcSetTasksPatch.taskIds[pc.PlayerId]));
            }

            Utils.NotifyRoles(ForceLoop: true);
            yield return new UnityEngine.WaitForSeconds(1.5f);//イントロが表示された後に本来の役職に変更
            if (senders2 != null)
                senders2.Do(kvp => kvp.Value.SendMessage());
            senders2 = null;
        }
        private static void AssignDesyncRole(CustomRoles role, List<PlayerControl> AllPlayers, Dictionary<byte, CustomRpcSender> senders, Dictionary<(byte, byte), RoleTypes> rolesMap, RoleTypes BaseRole, RoleTypes hostBaseRole = RoleTypes.Crewmate)
        {
            if (!role.IsPresent()) return;

            var hostId = PlayerControl.LocalPlayer.PlayerId;
            var rand = IRandom.Instance;

            for (var i = 0; i < role.GetRealCount(); i++)
            {
                if (AllPlayers.Count <= 0) break;
                var player = AllPlayers[rand.Next(0, AllPlayers.Count)];
                AllPlayers.Remove(player);
                PlayerState.GetByPlayerId(player.PlayerId).SetMainRole(role);

                var selfRole = player.PlayerId == hostId ? hostBaseRole : (role.IsCrewmate() ? RoleTypes.Crewmate : (role.IsMadmate() ? RoleTypes.Phantom : BaseRole));
                var othersRole = player.PlayerId == hostId ? RoleTypes.Crewmate : RoleTypes.Scientist;

                //Desync役職視点
                foreach (var target in Main.AllPlayerControls)
                {
                    if (player.PlayerId != target.PlayerId)
                    {
                        rolesMap[(player.PlayerId, target.PlayerId)] = othersRole;
                    }
                    else
                    {
                        rolesMap[(player.PlayerId, target.PlayerId)] = selfRole;
                    }
                }

                //他者視点
                foreach (var seer in Main.AllPlayerControls)
                {
                    if (player.PlayerId != seer.PlayerId)
                    {
                        rolesMap[(seer.PlayerId, player.PlayerId)] = othersRole;
                    }
                }
                RpcSetRoleReplacer.OverriddenSenderList.Add(senders[player.PlayerId]);
                //ホスト視点はロール決定
                player.StartCoroutine(player.CoSetRole(othersRole, Main.SetRoleOverride && Options.CurrentGameMode == CustomGameMode.Standard));
                player.Data.IsDead = true;
            }
        }
        public static void MakeDesyncSender(Dictionary<byte, CustomRpcSender> senders, Dictionary<(byte, byte), RoleTypes> rolesMap)
        {
            var hostId = PlayerControl.LocalPlayer.PlayerId;
            foreach (var seer in Main.AllPlayerControls)
            {
                var sender = senders[seer.PlayerId];
                foreach (var target in Main.AllPlayerControls)
                {
                    if (rolesMap.TryGetValue((seer.PlayerId, target.PlayerId), out var role))
                    {
                        //本人はイントロ表示後に再度SetRoleする
                        if (seer == PlayerControl.LocalPlayer || seer != target)
                            sender.RpcSetRole(seer, role, target.GetClientId());
                        else
                        {
                            //teamはまだMergeしない(てかできない)
                            var sender2 = new CustomRpcSender($"", SendOption.Reliable, false).StartMessage(seer.GetClientId());
                            sender2.RpcSetRole(seer, role, seer.GetClientId());
                            sender2.EndMessage();
                            senders2[seer.PlayerId] = sender2;
                        }
                    }
                }
            }
        }

        private static List<PlayerControl> AssignCustomRolesFromList(CustomRoles role, List<PlayerControl> players, int RawCount = -1)
        {
            if (players == null || players.Count <= 0) return null;
            var rand = IRandom.Instance;
            var count = Math.Clamp(RawCount, 0, players.Count);
            if (RawCount == -1) count = Math.Clamp(role.GetRealCount(), 0, players.Count);
            if (count <= 0) return null;
            List<PlayerControl> AssignedPlayers = new();
            SetColorPatch.IsAntiGlitchDisabled = true;
            for (var i = 0; i < count; i++)
            {
                var player = players[rand.Next(0, players.Count)];
                AssignedPlayers.Add(player);
                players.Remove(player);
                PlayerState.GetByPlayerId(player.PlayerId).SetMainRole(role);
                Logger.Info("役職設定:" + player?.Data?.PlayerName + " = " + role.ToString(), "AssignRoles");

                if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
                {
                    if (player.Is(CustomRoles.HASTroll))
                        player.RpcSetColor(2);
                    else if (player.Is(CustomRoles.HASFox))
                        player.RpcSetColor(3);
                }
            }
            SetColorPatch.IsAntiGlitchDisabled = false;
            return AssignedPlayers;
        }
        public static int GetRoleTypesCount(RoleTypes roleTypes)
        {
            int count = 0;
            foreach (var role in CustomRolesHelper.AllRoles)
            {
                if (CustomRoleManager.GetRoleInfo(role)?.IsDesyncImpostor == true) continue;
                if (Options.SuddenDeathMode.GetBool()) continue;
                if (role.IsMadmate()) continue;
                if (role == CustomRoles.Egoist && Main.NormalOptions.GetInt(Int32OptionNames.NumImpostors) <= 1) continue;
                if (role.GetRoleTypes() == roleTypes)
                    count += role.GetRealCount();
            }
            return count;
        }
        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetRole))]
        class RpcSetRoleReplacer
        {
            public static bool doReplace = false;
            public static Dictionary<byte, CustomRpcSender> senders;
            public static List<(PlayerControl, RoleTypes)> StoragedData = new();
            // 役職Desyncなど別の処理でSetRoleRpcを書き込み済みなため、追加の書き込みが不要なSenderのリスト
            public static List<CustomRpcSender> OverriddenSenderList;
            public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] RoleTypes roleType)
            {
                if (doReplace && senders != null)
                {
                    StoragedData.Add((__instance, roleType));
                    return false;
                }
                else return true;
            }
            public static void Release()
            {

                foreach (var pc in Main.AllPlayerControls)
                {
                    var playerInfo = GameData.Instance.GetPlayerById(pc.PlayerId);
                    if (playerInfo.Disconnected) Disconnected.Add(pc.PlayerId);
                }

                foreach (var sender in senders)
                {
                    if (OverriddenSenderList.Contains(sender.Value)) continue;
                    if (sender.Value.CurrentState != CustomRpcSender.State.InRootMessage)
                        throw new InvalidOperationException("A CustomRpcSender had Invalid State.");

                    foreach (var pair in StoragedData)
                    {
                        pair.Item1.StartCoroutine(pair.Item1.CoSetRole(pair.Item2, Main.SetRoleOverride && Options.CurrentGameMode == CustomGameMode.Standard));
                        sender.Value.AutoStartRpc(pair.Item1.NetId, (byte)RpcCalls.SetRole, Utils.GetPlayerById(sender.Key).GetClientId())
                            .Write((ushort)pair.Item2)
                            .Write(Main.SetRoleOverride && Options.CurrentGameMode == CustomGameMode.Standard)
                            .EndRpc();
                    }
                    sender.Value.EndMessage();
                }
                doReplace = false;
            }
            public static void StartReplace(Dictionary<byte, CustomRpcSender> senders)
            {
                RpcSetRoleReplacer.senders = senders;
                StoragedData = new();
                OverriddenSenderList = new();
                doReplace = true;
            }
        }
    }
}