using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;

using TownOfHost.Modules;
using TownOfHost.Roles;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Ghost;
using TownOfHost.Roles.AddOns.Common;

using static TownOfHost.Translator;
using TownOfHost.Roles.AddOns.Neutral;

namespace TownOfHost
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CoStartGame))]
    class ChangeRoleSettings
    {
        public static void Postfix(AmongUsClient __instance)
        {
            //注:この時点では役職は設定されていません。
            Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.GuardianAngel, 0, 0);

            if (GameStates.IsOnlineGame && !Main.IsCs())
            {
                var op = Main.NormalOptions;
                if (op.NumCommonTasks + op.NumLongTasks + op.NumShortTasks > 255)
                {
                    Main.NormalOptions.SetInt(Int32OptionNames.NumCommonTasks, 85);
                    Main.NormalOptions.SetInt(Int32OptionNames.NumLongTasks, 85);
                    Main.NormalOptions.SetInt(Int32OptionNames.NumShortTasks, 85);
                    Logger.Error($"全体のタスクが255を超えています", "CoStartGame ChTask");
                }
            }
            StandardIntro.CoGameIntroWeight();

            UtilsGameLog.Reset();
            PlayerState.Clear();

            Main.AllPlayerKillCooldown = new Dictionary<byte, float>();
            PlayerCatch.AllPlayerFirstTypes = new Dictionary<byte, CustomRoleTypes>();
            Main.AllPlayerSpeed = new Dictionary<byte, float>();
            UtilsGameLog.LastLog = new Dictionary<byte, string>();
            UtilsGameLog.LastLogRole = new Dictionary<byte, string>();
            UtilsGameLog.LastLogPro = new Dictionary<byte, string>();
            UtilsGameLog.LastLogSubRole = new Dictionary<byte, string>();
            Main.KillCount = new Dictionary<byte, int>();
            Main.Guard = new Dictionary<byte, int>();
            Main.AllPlayerTask = new Dictionary<byte, List<uint>>();
            GhostRoleAssingData.GhostAssingCount = new Dictionary<CustomRoles, int>();

            Main.HostKill = new();
            PlayerCatch.SKMadmateNowCount = 0;

            Main.AfterMeetingDeathPlayers = new();
            Main.clientIdList = new();

            Main.CheckShapeshift = new();
            Main.ShapeshiftTarget = new();

            ReportDeadBodyPatch.CanReport = new();
            ReportDeadBodyPatch.Musisuruoniku = new();
            ReportDeadBodyPatch.ChengeMeetingInfo = new();

            Options.UsedButtonCount = 0;
            Main.RealOptionsData = new OptionBackupData(GameOptionsManager.Instance.CurrentGameOptions);

            GameStates.introDestroyed = false;

            MeetingTimeManager.Init();
            Main.DefaultCrewmateVision = Main.RealOptionsData.GetFloat(FloatOptionNames.CrewLightMod);
            Main.DefaultImpostorVision = Main.RealOptionsData.GetFloat(FloatOptionNames.ImpostorLightMod);

            Main.LastNotifyNames = new();

            Main.PlayerColors = new();
            Main.AllPlayerLastkillpos = new();
            //名前の記録
            Main.AllPlayerNames = new();

            //ホストの名前を戻す
            string name = AmongUs.Data.DataManager.player.Customization.Name;
            if (Main.nickName != "") name = Main.nickName;
            PlayerControl.LocalPlayer.Data.PlayerName = name;

            SelectRolesPatch.roleAssigned = false;
            SelectRolesPatch.senders2 = new();
            HudManagerCoShowIntroPatch.Cancel = true;
            RpcSetTasksPatch.taskIds.Clear();

            Camouflage.Init();
            var invalidColor = PlayerCatch.AllPlayerControls.Where(p => p.Data.DefaultOutfit.ColorId < 0 || Palette.PlayerColors.Length <= p.Data.DefaultOutfit.ColorId);
            if (invalidColor.Any())
            {
                var msg = Translator.GetString("Error.InvalidColor");
                Logger.seeingame(msg);
                msg += "\n" + string.Join(",", invalidColor.Select(p => $"{p.name}({p.Data.DefaultOutfit.ColorId})"));
                Utils.SendMessage(msg);
                Logger.Error(msg, "CoStartGame");
            }

            foreach (var target in PlayerCatch.AllPlayerControls)
            {
                foreach (var seer in PlayerCatch.AllPlayerControls)
                {
                    var pair = (target.PlayerId, seer.PlayerId);
                    Main.LastNotifyNames[pair] = target.name;
                }
            }
            List<PlayerControl> players = new();
            PlayerCatch.AllPlayerControls.Do(x => players.Add(x));
            PlayerCatch.AllPlayerNetId = new();
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                PlayerCatch.AllPlayerNetId.TryAdd(pc.PlayerId, pc.NetId);
                PlayerState.Create(pc.PlayerId);
                Main.AllPlayerSpeed[pc.PlayerId] = Main.RealOptionsData.GetFloat(FloatOptionNames.PlayerSpeedMod); //移動速度をデフォルトの移動速度に変更
                ReportDeadBodyPatch.CanReport[pc.PlayerId] = true;
                ReportDeadBodyPatch.WaitReport[pc.PlayerId] = new();
                ReportDeadBodyPatch.Musisuruoniku[pc.PlayerId] = true;
                Main.clientIdList.Add(pc.GetClientId());
                pc.RemoveProtection();
                Main.KillCount.Add(pc.PlayerId, 0);

                if (Options.AllPlayerSkinShuffle.GetBool() && (Event.April || Event.Special))
                {
                    var tageId = IRandom.Instance.Next(players.Count);
                    var pl = players.OrderBy(x => Guid.NewGuid()).ToArray()[tageId];
                    Logger.Info($"{pc?.Data?.PlayerName} => {pl?.Data?.PlayerName}", "Shuffle");
                    UtilsGameLog.AddGameLogsub($"{pc?.Data?.PlayerName}のシャッフル先 : {pl?.Data?.PlayerName}");

                    var colorId = pl.Data.DefaultOutfit.ColorId;

                    Main.AllPlayerNames[pc.PlayerId] = pl?.Data?.PlayerName;
                    Main.PlayerColors[pc.PlayerId] = Palette.PlayerColors[colorId];
                    pc.cosmetics.nameText.text = pl.name;

                    var outfit = pl.Data.DefaultOutfit;
                    Camouflage.PlayerSkins[pc.PlayerId] = new NetworkedPlayerInfo.PlayerOutfit().Set(outfit.PlayerName, outfit.ColorId, outfit.HatId, outfit.SkinId, outfit.VisorId, outfit.PetId);

                    players.Remove(pl);
                }
                else
                {
                    var colorId = pc.Data.DefaultOutfit.ColorId;
                    if (AmongUsClient.Instance.AmHost && Options.ColorNameMode.GetBool()) pc.RpcSetName(Palette.GetColorName(colorId));

                    Main.AllPlayerNames[pc.PlayerId] = pc?.Data?.PlayerName;
                    Main.PlayerColors[pc.PlayerId] = Palette.PlayerColors[colorId];
                    pc.cosmetics.nameText.text = pc.name;

                    var outfit = pc.Data.DefaultOutfit;
                    Camouflage.PlayerSkins[pc.PlayerId] = new NetworkedPlayerInfo.PlayerOutfit().Set(outfit.PlayerName, outfit.ColorId, outfit.HatId, outfit.SkinId, outfit.VisorId, outfit.PetId);
                }
            }
            if (Options.AllPlayerSkinShuffle.GetBool() && (Event.April || Event.Special))
            {
                PlayerCatch.AllPlayerControls.Do(pc =>
                {
                    Camouflage.RpcSetSkin(pc);
                    if (!Camouflage.PlayerSkins.TryGetValue(pc.PlayerId, out var outfit)) return;

                    if (AmongUsClient.Instance.AmHost && Options.ColorNameMode.GetBool()) pc.RpcSetName(Palette.GetColorName(outfit.ColorId));
                    else if (AmongUsClient.Instance.AmHost) pc.RpcSetName(outfit.PlayerName);
                });
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

            Attributes.GameModuleInitializerAttribute.InitializeAll();

            SelectRolesPatch.Disconnected.Clear();
            RpcSetTasksPatch.HostFin = false;
            Main.DontGameSet = Options.NoGameEnd.GetBool();
            IRandom.SetInstanceById(Options.RoleAssigningAlgorithm.GetValue());
            CustomRoleManager.MarkOthers.Add(ReportDeadBodyPatch.Dontrepomark);

            Logger.Info($"==============　{Main.GameCount}試合目　==============", "OnGamStarted");
            Main.Time = (Main.NormalOptions?.DiscussionTime ?? 0, Main.NormalOptions?.VotingTime ?? 180);

            if (GameStates.IsOnlineGame)
            {
                var sn = ServerManager.Instance.CurrentRegion.TranslateName;
                if (sn is StringNames.ServerNA or StringNames.ServerEU or StringNames.ServerSA)
                    Main.LagTime = 0.43f;
                else Main.LagTime = 0.23f;
            }
            else Main.LagTime = 0.23f;
            Logger.Info($"LagTime : {Main.LagTime} PlayerCount : {PlayerCatch.AllPlayerControls.Count()}", "OnGamStarted Fin");
        }
    }
    [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
    class SelectRolesPatch
    {
        public static List<byte> Disconnected = new();
        public static bool roleAssigned = false;
        public static Dictionary<byte, CustomRpcSender> senders2 = new();
        public static bool Prefix()
        {
            //Logger.Warn("!!!", "SR");
            if (!AmongUsClient.Instance.AmHost) return true;
            //CustomRpcSenderとRpcSetRoleReplacerの初期化
            Dictionary<byte, CustomRpcSender> senders = new();
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                senders[pc.PlayerId] = new CustomRpcSender($"{pc.name}'s SetRole Sender", SendOption.None, false)
                        .StartMessage(pc.GetClientId());

            }
            RpcSetRoleReplacer.StartReplace(senders);

            RoleAssignManager.SelectAssignRoles();

            if (Options.CurrentGameMode != CustomGameMode.HideAndSeek)
            {
                if (Options.CurrentGameMode == CustomGameMode.TaskBattle)
                {
                    TaskBattle.ResetAndSetTeam();
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
                    foreach (var pc in PlayerCatch.AllPlayerControls)
                    {
                        AllPlayers.Add(pc);
                    }

                    if (Options.EnableGM.GetBool())
                    {
                        AllPlayers.RemoveAll(x => x.PlayerId == PlayerControl.LocalPlayer.PlayerId);
                        PlayerControl.LocalPlayer.RpcSetCustomRole(CustomRoles.GM);
                        PlayerControl.LocalPlayer.RpcSetRole(RoleTypes.Crewmate, Main.SetRoleOverride && Options.CurrentGameMode == CustomGameMode.Standard);
                        PlayerControl.LocalPlayer.Data.IsDead = true;
                    }
                    if (DebugModeManager.EnableTOHkDebugMode.GetBool())
                    {
                        if (Main.HostRole != CustomRoles.NotAssigned)
                        {
                            AllPlayers.RemoveAll(x => x.PlayerId == PlayerControl.LocalPlayer.PlayerId);
                            PlayerControl.LocalPlayer.RpcSetCustomRole(Main.HostRole, true);
                            PlayerControl.LocalPlayer.RpcSetRole(Main.HostRole.GetRoleInfo()?.BaseRoleType.Invoke() ?? RoleTypes.Crewmate, Main.SetRoleOverride && Options.CurrentGameMode == CustomGameMode.Standard);
                            PlayerControl.LocalPlayer.Data.IsDead = true;
                        }
                    }
                    if (SuddenDeathMode.NowSuddenDeathMode)
                    {
                        if (Options.SuddenTeamRole.GetBool()) AllPlayers.Clear();
                        SuddenDeathMode.TeamSet();
                    }
                    Dictionary<(byte, byte), RoleTypes> rolesMap = new();
                    foreach (var (role, info) in CustomRoleManager.AllRolesInfo)
                    {
                        if (info.IsDesyncImpostor || role is CustomRoles.Amnesiac || role.IsMadmate() || (role.IsNeutral() && role is not CustomRoles.Egoist) || Options.SuddenDeathMode.GetBool())
                        {
                            AssignDesyncRole(role, AllPlayers, senders, rolesMap, BaseRole: info.BaseRoleType.Invoke());
                        }
                    }
                    MakeDesyncSender(senders, rolesMap);
                }
            }
            //以下、バニラ側の役職割り当てが入る

            {
                //動かないのでTOH対応くるまでにこれで...
                Il2CppSystem.Collections.Generic.List<NetworkedPlayerInfo> playerInfos = new();
                foreach (NetworkedPlayerInfo data in GameData.Instance.AllPlayers)
                {
                    if (data.Object != null && !data.IsDead && !data.Disconnected)
                        playerInfos.Add(data);
                }
                IGameOptions currentGameOptions = GameOptionsManager.Instance.CurrentGameOptions;
                int adjustedNumImpostors = GameOptionsManager.Instance.CurrentGameOptions.GetAdjustedNumImpostors(playerInfos.Count);
                //一応呼ぶ....呼ぶ..
                RoleManager.Instance.DebugRoleAssignments(playerInfos, ref adjustedNumImpostors);

                GameManager.Instance.LogicRoleSelection.AssignRolesForTeam(playerInfos, currentGameOptions, RoleTeamTypes.Impostor, adjustedNumImpostors, new Il2CppSystem.Nullable<RoleTypes>(RoleTypes.Impostor));
                GameManager.Instance.LogicRoleSelection.AssignRolesForTeam(playerInfos, currentGameOptions, RoleTeamTypes.Crewmate, int.MaxValue, new Il2CppSystem.Nullable<RoleTypes>(RoleTypes.Crewmate));
            }
            return false;
        }
        public static void Postfix()
        {
            //Logger.Warn("!!!2", "SR");
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

            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                if (!pc.Is(CustomRoles.GM)) PlayerCatch.OldAlivePlayerControles.Add(pc);
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
                        Logger.seeingame(string.Format(GetString("Error.InvalidRoleAssignment"), pc?.Data?.GetLogPlayerName()));
                        break;
                }
                state.SetMainRole(role);
            }

            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                SetColorPatch.IsAntiGlitchDisabled = true;
                if (!Main.HnSFlag)
                    foreach (var pc in PlayerCatch.AllPlayerControls)
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

                if (TaskBattle.IsTaskBattleTeamMode)
                {
                    foreach (var pc in PlayerCatch.AllPlayerControls)
                        foreach (var (team, players) in TaskBattle.TaskBattleTeams)
                        {
                            if (!players.Contains(pc.PlayerId)) continue;
                            foreach (var id in players.Where(id => id != pc.PlayerId))
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
                    if (CustomRoleManager.GetRoleInfo(role)?.IsDesyncImpostor is true) continue;
                    if (role.IsMadmate()) continue;
                    if (role.IsNeutral() && role is not CustomRoles.Egoist) continue;
                    if (role is CustomRoles.Amnesiac) continue;
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
                Twins.AssingAndReset();
                Faction.AssingFaction();

                foreach (var pair in PlayerState.AllPlayerStates)
                {
                    ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value.MainRole);

                    foreach (var subRole in pair.Value.SubRoles)
                        ExtendedPlayerControl.RpcSetCustomRole(pair.Key, subRole);
                }

                CustomRoleManager.CreateInstance();
                foreach (var pc in PlayerCatch.AllPlayerControls)
                {
                    var role = pc.GetCustomRole();
                    HudManager.Instance.SetHudActive(true);
                    pc.ResetKillCooldown();

                    //通常モードでかくれんぼをする人用
                    if (Options.IsStandardHAS)
                    {
                        foreach (var seer in PlayerCatch.AllPlayerControls)
                        {
                            if (seer == pc) continue;
                            if (role.IsImpostor() || (seer.IsNeutralKiller() && role is not CustomRoles.Egoist)) //変更対象がインポスター陣営orキル可能な第三陣営
                                NameColorManager.Add(seer.PlayerId, pc.PlayerId);
                        }
                    }

                    if (role.GetRoleInfo()?.IsCantSeeTeammates == true && role.IsImpostor() && !SuddenDeathMode.NowSuddenDeathMode)
                    {
                        var clientId = pc.GetClientId();
                        foreach (var killer in PlayerCatch.AllPlayerControls)
                        {
                            if (!killer.GetCustomRole().IsImpostor()) continue;
                            //Amnesiac視点インポスターをクルーにする
                            killer.RpcSetRoleDesync(RoleTypes.Scientist, clientId);
                        }
                    }
                    if (pc.Is(CustomRoles.Amnesiac) && !SuddenDeathMode.NowSuddenDeathMode)
                    {
                        foreach (var killer in PlayerCatch.AllPlayerControls)
                        {
                            if (killer == null) continue;
                            if (pc.PlayerId == killer.PlayerId) continue;
                            if (!killer.GetCustomRole().IsImpostor()) continue;
                            var clientId = killer.GetClientId();
                            //他者視点Amnesiacをインポスターにする
                            pc.RpcSetRoleDesync(RoleTypes.Impostor, clientId);
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
            foreach (var pc in PlayerCatch.AllPlayerControls)
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
            if (PlayerCatch.AllPlayerControls.Where(x => x.Is(CustomRoles.Connecting)).Count() == 1)
            {
                PlayerCatch.AllPlayerControls.Where(x => x.Is(CustomRoles.Connecting)).ToArray().Do(
                            p => PlayerState.GetByPlayerId(p.PlayerId).RemoveSubRole(CustomRoles.Connecting));
            }

            //役職選定後に処理する奴。
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                var role = pc.GetCustomRole();
                var roletype = role.GetCustomRoleTypes();
                var lov = pc.Is(CustomRoles.OneLove) ? Utils.ColorString(UtilsRoleText.GetRoleColor(CustomRoles.OneLove), GetString("OneLove") + " ") : "";
                var color = Palette.CrewmateBlue;
                var roleClass = CustomRoleManager.GetByPlayerId(pc.PlayerId);
                switch (roletype)
                {
                    case CustomRoleTypes.Impostor or CustomRoleTypes.Madmate:
                        color = Palette.ImpostorRed;
                        break;
                    case CustomRoleTypes.Neutral:
                        color = UtilsRoleText.GetRoleColor(role);
                        break;
                }
                UtilsGameLog.LastLog[pc.PlayerId] = "<b>" + Utils.ColorString(Main.PlayerColors[pc.PlayerId], Main.AllPlayerNames[pc.PlayerId] + "</b>");
                UtilsGameLog.LastLogRole[pc.PlayerId] = $"<b>{lov}" + Utils.ColorString(UtilsRoleText.GetRoleColor(role), GetString($"{role}")) + "</b>";
                PlayerCatch.AllPlayerFirstTypes.Add(pc.PlayerId, roletype);

                //Addons
                Main.Guard.Add(pc.PlayerId, 0);
                if (pc.Is(CustomRoles.Guarding)) Main.Guard[pc.PlayerId] += Guarding.Guard;
                //RoleAddons
                if (RoleAddAddons.GetRoleAddon(role, out var d, pc, subrole: [CustomRoles.Speeding, CustomRoles.Guarding]))
                {
                    if (d.GiveGuarding.GetBool()) Main.Guard[pc.PlayerId] += d.Guard.GetInt();
                    if (d.GiveSpeeding.GetBool()) Main.AllPlayerSpeed[pc.PlayerId] = d.Speed.GetFloat();
                }
                if (!Main.AllPlayerKillCooldown.ContainsKey(pc.PlayerId))
                    Main.AllPlayerKillCooldown.Add(pc.PlayerId, Options.DefaultKillCooldown);
            }

            if (Lovers.OneLovePlayer.Ltarget != byte.MaxValue && Options.CurrentGameMode == CustomGameMode.Standard)
            {
                UtilsGameLog.LastLogRole[Lovers.OneLovePlayer.Ltarget] += Utils.ColorString(UtilsRoleText.GetRoleColor(CustomRoles.OneLove), "♡");
                if (Lovers.OneLovePlayer.doublelove) UtilsGameLog.LastLogRole[Lovers.OneLovePlayer.OneLove] += Utils.ColorString(UtilsRoleText.GetRoleColor(CustomRoles.OneLove), "♡");
            }

            if (Options.CurrentGameMode == CustomGameMode.Standard)
                StandardIntro.CoResetRoleY();
            RPC.RpcSyncAllNetworkedPlayer();

            PlayerCatch.CountAlivePlayers(true);
            UtilsOption.SyncAllSettings();
            SetColorPatch.IsAntiGlitchDisabled = false;
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
                Logger.Info("役職設定:" + player?.Data?.GetLogPlayerName() + " = " + role.ToString(), "AssignRolesDesync");

                var selfRole = player.PlayerId == hostId ? hostBaseRole : (role.IsCrewmate() ? RoleTypes.Crewmate : (role.IsMadmate() ? RoleTypes.Phantom : (role.IsNeutral() && role is not CustomRoles.Egoist && !BaseRole.IsCrewmate() ? RoleTypes.Crewmate : BaseRole)));
                var othersRole = player.PlayerId == hostId ? RoleTypes.Crewmate : RoleTypes.Scientist;

                if (role is CustomRoles.Amnesiac)
                {
                    selfRole = RoleTypes.Crewmate;
                    Main.NormalOptions.SetInt(Int32OptionNames.NumImpostors, Main.NormalOptions.GetInt(Int32OptionNames.NumImpostors) - 1);
                }
                //Desync役職視点
                foreach (var target in PlayerCatch.AllPlayerControls)
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
                foreach (var seer in PlayerCatch.AllPlayerControls)
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
            if (Options.ExIntroWeight.GetBool()) return;
            var hostId = PlayerControl.LocalPlayer.PlayerId;
            foreach (var seer in PlayerCatch.AllPlayerControls)
            {
                foreach (var target in PlayerCatch.AllPlayerControls)
                {
                    var sender = senders[target.PlayerId];
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
                Logger.Info("役職設定:" + player?.Data?.GetLogPlayerName() + " = " + role.ToString(), "AssignRoles");

                if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
                {
                    if (player.Is(CustomRoles.HASTroll))
                        player.RpcSetColor(2);
                    else if (player.Is(CustomRoles.HASFox))
                        player.RpcSetColor(3);
                }

                if (role.GetRoleInfo().IsCantSeeTeammates && player != PlayerControl.LocalPlayer && !SuddenDeathMode.NowSuddenDeathMode)
                {
                    player.RpcSetRoleDesync(RoleTypes.Scientist, player.GetClientId());

                    var sender2 = new CustomRpcSender($"", SendOption.Reliable, false).StartMessage(player.GetClientId());
                    sender2.RpcSetRole(player, role.GetRoleTypes(), player.GetClientId());
                    sender2.EndMessage();
                    senders2[player.PlayerId] = sender2;
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
                if (SuddenDeathMode.NowSuddenDeathMode) continue;
                if (role.IsMadmate()) continue;
                if (role.IsNeutral() && role is not CustomRoles.Egoist) continue;
                if (role is CustomRoles.Amnesiac) continue;
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
                if (Options.ExIntroWeight.GetBool() && Options.CurrentGameMode is CustomGameMode.Standard)
                {
                    foreach (var pair in StoragedData)
                    {
                        pair.Item1.StartCoroutine(pair.Item1.CoSetRole(pair.Item2, Main.SetRoleOverride && Options.CurrentGameMode is CustomGameMode.Standard));
                    }
                    return;
                }
                foreach (var pc in PlayerCatch.AllPlayerControls)
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
                        sender.Value.AutoStartRpc(pair.Item1.NetId, (byte)RpcCalls.SetRole, PlayerCatch.GetPlayerById(sender.Key).GetClientId())
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