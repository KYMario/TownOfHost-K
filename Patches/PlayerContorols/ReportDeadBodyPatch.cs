using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.AddOns.Common;
using TownOfHost.Roles.Impostor;
using TownOfHost.Roles.Neutral;
using static TownOfHost.Roles.Core.RoleBase;
using TownOfHost.Roles;

namespace TownOfHost
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
    class ReportDeadBodyPatch
    {
        public static Dictionary<byte, bool> CanReport;
        public static Dictionary<byte, bool> IgnoreBodyids;
        public static Dictionary<byte, string> ChengeMeetingInfo;
        public static Dictionary<byte, List<NetworkedPlayerInfo>> WaitReport = new();
        static bool Wait;
        public static uint reporternetid;
        public static byte targetid;
        //public static Dictionary<byte, Vector2> Pos = new();
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] NetworkedPlayerInfo target)
        {
            if (GameStates.IsMeeting || GameStates.IsLobby || Wait) return false;
            if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Default) return false;
            if (SuddenDeathMode.NowSuddenDeathMode)
            {
                CheckMeeting(__instance, target);
                return false;
            }
            Logger.Info($"{__instance.GetNameWithRole().RemoveHtmlTags()} => {target?.Object?.GetNameWithRole()?.RemoveHtmlTags() ?? "null"}", "ReportDeadBody");

            var State = PlayerState.GetByPlayerId(__instance.PlayerId);
            if (State.NumberOfRemainingButtons <= 0 && target is null)
            {
                Logger.Info($"{__instance.name}君はもうボタン使ったでしょ!", "ReportDeadBody");
                return false;
            }

            GameStates.CalledMeeting = true;
            if (Options.IsStandardHAS && target != null && __instance == target.Object) return true; //[StandardHAS] ボタンでなく、通報者と死体が同じなら許可

            if (Options.CurrentGameMode is CustomGameMode.HideAndSeek or CustomGameMode.TaskBattle || Options.IsStandardHAS)
            {
                GameStates.CalledMeeting = false;
                return false;
            }

            if (!CanReport[__instance.PlayerId])
            {
                GameStates.CalledMeeting = false;
                WaitReport[__instance.PlayerId].Add(target);
                Logger.Warn($"{__instance.GetNameWithRole().RemoveHtmlTags()}:通報禁止中のため可能になるまで待機します", "ReportDeadBody");

                if (DontReport.TryGetValue(__instance.PlayerId, out var check))
                {
                    if (check.reason == DontReportreson.wait) return false;
                }
                if (!DontReport.TryAdd(__instance.PlayerId, (0, DontReportreson.wait))) DontReport[__instance.PlayerId] = (0, DontReportreson.wait);
                RpcCancelMeeting(__instance);
                _ = new LateTask(() =>
                {
                    if (!GameStates.CalledMeeting) UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: __instance);
                }, 0.2f, "", true);
                return false;
            }

            //ホスト以外はこの先処理しない
            if (!AmongUsClient.Instance.AmHost)
            {
                GameStates.CalledMeeting = false;
                return true;
            }

            if (!CheckMeeting(__instance, target)) return false;

            PlayerControlRpcUseZiplinePatch.OnMeeting(__instance, target);

            //=============================================
            //以下、ボタンが押されることが確定したものとする。
            //=============================================

            reporternetid = __instance.NetId;
            targetid = target?.PlayerId ?? byte.MaxValue;
            Wait = true;
            GameStates.task = false;

            DisableDevice.StartMeeting();
            foreach (var kvp in PlayerState.AllPlayerStates)
            {
                var pc = PlayerCatch.GetPlayerById(kvp.Key);
                kvp.Value.LastRoom = pc?.GetPlainShipRoom();
                if (Options.ExCallMeetingBlackout.GetBool() || CustomRoles.Monochromer.IsEnable())
                    kvp.Value.IsBlackOut = true;
            }
            UtilsOption.MarkEveryoneDirtySettings();

            AdminProvider.CalculateAdmin(true);

            // シェイプキラー：通報者書き換え
            ShapeKiller.SetDummyReport(ref __instance, target);

            if (target != null)
            {
                UtilsGameLog.AddGameLog("Meeting", UtilsName.GetPlayerColor(target, true) + Translator.GetString("Meeting.Report") + "\n\t\t┗  " + string.Format(Translator.GetString("Meeting.Shoushu"), UtilsName.GetPlayerColor(__instance.PlayerId, true)));
                var colorid = Camouflage.PlayerSkins[target.PlayerId].ColorId;
                var DieName = Palette.GetColorName(colorid);
                var check = false;
                var color = Palette.PlayerColors[colorid];
                if (ChengeMeetingInfo.TryGetValue(target.PlayerId, out var output))
                {
                    color = ModColors.NeutralGray;
                    check = true;
                    DieName = output;
                }
                MeetingHudPatch.Oniku = (check ? DieName : UtilsName.GetPlayerColor(target, true)) + Translator.GetString("Meeting.Report") + "\n　" + string.Format(Translator.GetString("Meeting.Shoushu"), UtilsName.GetPlayerColor(__instance.PlayerId, true));
                UtilsNotifyRoles.ExtendedMeetingText = "<u>★".Color(Palette.PlayerColors[Camouflage.PlayerSkins[__instance.PlayerId].ColorId]) + "<#ffffff>" + string.Format(Translator.GetString("MI.die"), DieName.Color(color)) + "</u></color>";
                RpcSyncMeetingInfo(__instance, target, check ? DieName : null);
            }
            else
            {
                UtilsGameLog.AddGameLog("Meeting", Translator.GetString("Meeting.Button") + "\n\t\t┗  " + string.Format(Translator.GetString("Meeting.Shoushu"), UtilsName.GetPlayerColor(__instance.PlayerId, true)));
                MeetingHudPatch.Oniku = Translator.GetString("Meeting.Button") + "\n　" + string.Format(Translator.GetString("Meeting.Shoushu"), UtilsName.GetPlayerColor(__instance.PlayerId, true));
                UtilsNotifyRoles.ExtendedMeetingText = "<u>★".Color(Palette.PlayerColors[Camouflage.PlayerSkins[__instance.PlayerId].ColorId]) + "<#ffffff>" + Translator.GetString("MI.Bot") + "</u></color>";
                RpcSyncMeetingInfo(__instance, target);
            }

            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                var roleClass = pc.GetRoleClass();
                roleClass?.OnReportDeadBody(__instance, target);
            }

            _ = new LateTask(() =>
            {
                foreach (var pc in PlayerCatch.AllPlayerControls)
                {
                    if (!pc) continue;
                    Camouflage.RpcSetSkin(pc, RevertToDefault: true, force: true);
                }
            }, 1f, "SetSkin", false);

            // var State = PlayerState.GetByPlayerId(__instance.PlayerId);
            if (State.NumberOfRemainingButtons > 0 && target is null)
                State.NumberOfRemainingButtons--;

            //if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Default) return false;
            //サボ関係多分なしに～
            //押したのなら強制で始める
            _ = new LateTask(() =>
            {
                UtilsNotifyRoles.NotifyMeetingRoles();

                MeetingTimeManager.OnReportDeadBody();

                UtilsOption.SyncAllSettings();

                InnerNetClientPatch.DontTouch = true;
                MeetingRoomManager.Instance.AssignSelf(__instance, target);
                DestroyableSingleton<HudManager>.Instance.OpenMeetingRoom(__instance);
                __instance.StartMeeting(target);
                _ = new LateTask(() =>
                {
                    CustomRpcSender.Create("StartMeeting")
                    .AutoStartRpc(__instance.NetId, RpcCalls.StartMeeting)
                    .Write(target?.PlayerId ?? byte.MaxValue)
                    .EndRpc()
                    .SendMessage();
                }, 0.2f, "Startmeeting", true);
                Wait = false;
            }, Options.ExCallMeetingBlackout.GetBool() || CustomRoles.Monochromer.IsEnable() ? 0.12f : 0, "StartMeeting", true);

            return false;
        }
        public static async void ChangeLocalNameAndRevert(string name, int time)
        {
            //async Taskじゃ警告出るから仕方ないよね。
            var revertName = PlayerControl.LocalPlayer.name;
            PlayerControl.LocalPlayer.RpcSetNameEx(name);
            await Task.Delay(time);
            PlayerControl.LocalPlayer.RpcSetNameEx(revertName);
        }
        /// <summary>
        /// 死者でもReportさせるやーつ
        /// </summary>
        /// <param name="reporter">通報者</param>
        /// <param name="target">死体(null=button)</param>
        /// <param name="Cancelcheck">属性等のチェック入れるか</param>
        public static void ExReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target = null, bool? Cancelcheck = true, string Meetinginfo = "", string colorcode = "#000000")
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Default) return;

            Logger.Info($"{reporter.GetNameWithRole().RemoveHtmlTags()} => {target?.Object?.GetNameWithRole()?.RemoveHtmlTags() ?? "null"}", "ExReportDeadBody");

            if (reporter == null)
            {
                Logger.Error($"{reporter?.Data?.GetLogPlayerName() ?? "???"} がnull!", "ExReportDeadBody");
            }
            if (GameStates.IsMeeting || GameStates.IsLobby || Wait) return;

            var State = PlayerState.GetByPlayerId(reporter.PlayerId);
            if (State.NumberOfRemainingButtons <= 0 && target is null && Cancelcheck is not false) return;

            if (Cancelcheck is null or true)
                if (!CheckMeeting(reporter, target, checkdie: Cancelcheck is true)) return;

            PlayerControlRpcUseZiplinePatch.OnMeeting(reporter, target);

            GameStates.CalledMeeting = true;
            GameStates.task = false;// Pos.Clear();
            Wait = true;

            DisableDevice.StartMeeting();
            foreach (var kvp in PlayerState.AllPlayerStates)
            {
                var pc = PlayerCatch.GetPlayerById(kvp.Key);
                kvp.Value.LastRoom = pc?.GetPlainShipRoom();
                if (Options.ExCallMeetingBlackout.GetBool() || CustomRoles.Monochromer.IsEnable())
                    kvp.Value.IsBlackOut = true;
            }

            UtilsOption.MarkEveryoneDirtySettings();
            AdminProvider.CalculateAdmin(true);
            // シェイプキラー：通報者書き換え
            ShapeKiller.SetDummyReport(ref reporter, target);

            if (Meetinginfo == "")
            {
                if (target != null)
                {
                    UtilsGameLog.AddGameLog("Meeting", UtilsName.GetPlayerColor(target, true) + Translator.GetString("Meeting.Report") + "\n\t\t┗  " + string.Format(Translator.GetString("Meeting.Shoushu"), UtilsName.GetPlayerColor(reporter.PlayerId, true)));
                    var colorid = Camouflage.PlayerSkins[target.PlayerId].ColorId;
                    var DieName = Palette.GetColorName(colorid);
                    var check = false;
                    var color = Palette.PlayerColors[colorid];
                    if (ChengeMeetingInfo.TryGetValue(target.PlayerId, out var output))
                    {
                        color = ModColors.NeutralGray;
                        check = true;
                        DieName = output;
                    }
                    MeetingHudPatch.Oniku = (check ? DieName : UtilsName.GetPlayerColor(target, true)) + Translator.GetString("Meeting.Report") + "\n　" + string.Format(Translator.GetString("Meeting.Shoushu"), UtilsName.GetPlayerColor(reporter.PlayerId, true));
                    UtilsNotifyRoles.ExtendedMeetingText = "<u>★".Color(Palette.PlayerColors[Camouflage.PlayerSkins[reporter.PlayerId].ColorId]) + "<#ffffff>" + string.Format(Translator.GetString("MI.die"), DieName.Color(color)) + "</u></color>";
                    RpcSyncMeetingInfo(reporter, target, check ? DieName : null);
                }
                else
                {
                    UtilsGameLog.AddGameLog("Meeting", Translator.GetString("Meeting.Button") + "\n\t\t┗  " + string.Format(Translator.GetString("Meeting.Shoushu"), UtilsName.GetPlayerColor(reporter.PlayerId, true)));
                    MeetingHudPatch.Oniku = Translator.GetString("Meeting.Button") + "\n　" + string.Format(Translator.GetString("Meeting.Shoushu"), UtilsName.GetPlayerColor(reporter.PlayerId, true));
                    UtilsNotifyRoles.ExtendedMeetingText = "<u>★".Color(Palette.PlayerColors[Camouflage.PlayerSkins[reporter.PlayerId].ColorId]) + "<#ffffff>" + Translator.GetString("MI.Bot") + "</u></color>";
                    RpcSyncMeetingInfo(reporter, target);
                }
            }
            else
            {
                var info = GetString(Meetinginfo);
                MeetingHudPatch.Oniku = info;
                UtilsNotifyRoles.ExtendedMeetingText = $"<color={colorcode}><u>★" + info + "</u></color>";
                RpcSyncMeetingInfo(reporter, target, meetingInfo: Meetinginfo, infoColor: colorcode);
            }
            reporternetid = reporter.NetId;
            targetid = target?.PlayerId ?? byte.MaxValue;

            if (!Options.firstturnmeeting || !MeetingStates.FirstMeeting)
                foreach (var pc in PlayerCatch.AllPlayerControls)
                {
                    var roleClass = pc.GetRoleClass();
                    roleClass?.OnReportDeadBody(reporter, target);
                }
            _ = new LateTask(() =>
            {
                foreach (var pc in PlayerCatch.AllPlayerControls)
                {
                    if (!pc) continue;
                    Camouflage.RpcSetSkin(pc, RevertToDefault: true, force: true);
                }
            }, 1f, "SetSkin", false);

            //if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Default) return;
            _ = new LateTask(() =>
            {
                UtilsNotifyRoles.NotifyMeetingRoles();

                MeetingTimeManager.OnReportDeadBody();

                UtilsOption.SyncAllSettings();

                InnerNetClientPatch.DontTouch = true;
                MeetingRoomManager.Instance.AssignSelf(reporter, target);
                DestroyableSingleton<HudManager>.Instance.OpenMeetingRoom(reporter);
                reporter.StartMeeting(target);

                _ = new LateTask(() =>
                {
                    CustomRpcSender.Create("StartMeeting")
                    .AutoStartRpc(reporter.NetId, RpcCalls.StartMeeting)
                    .Write(target?.PlayerId ?? byte.MaxValue)
                    .EndRpc()
                    .SendMessage();
                }, 0.2f, "Startmeeting", true);
                Wait = false;
            }, Options.ExCallMeetingBlackout.GetBool() || CustomRoles.Monochromer.IsEnable() ? 0.12f : 0, "StartMeeting", true);
        }
        public static Dictionary<byte, (float time, DontReportreson reason)> DontReport = new();
        public static string GetDontReportMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
        {
            seen ??= seer;
            if (isForMeeting) return "";

            if (seer == seen)
                if (DontReport.TryGetValue(seer.PlayerId, out var data))
                {
                    switch (data.reason)
                    {
                        case DontReportreson.wait: return "<size=120%><#91abbd>...</color></size>";
                        case DontReportreson.NonReport: return "<size=120%><#006666>×</color></size>";
                        case DontReportreson.Transparent: return "<size=120%><#7b7c7d>×</color></size>";
                        case DontReportreson.CantUseButton: return "<size=120%><#bdb091>×</color></size>";
                        case DontReportreson.Other: return "<size=120%><#bd9391>×</color></size>";
                        case DontReportreson.Eat: return "<size=120%><#6f4204>×</color></size>";
                        case DontReportreson.Impostor: return "<size=120%><#ff1919>×</color></size>";
                    }
                }

            return "";
        }
        public static bool CheckMeeting(PlayerControl repoter, NetworkedPlayerInfo target, bool checkdie = true)
        {
            var DontAddonCheck = false;
            var reason = DontReportreson.None;
            var check = false;
            if (target != null)
            {
                if (repoter.GetRoleClass() is MassMedia massMedia)
                {
                    if (massMedia.Targetid == target.PlayerId)
                        DontAddonCheck = true;
                }
                if (repoter.GetCustomRole() is CustomRoles.Vulture)
                    DontAddonCheck = true;
            }
            if (SuddenDeathMode.NowSuddenDeathMode)
            {
                foreach (var role in CustomRoleManager.AllActiveRoles.Values)
                {
                    if (role.CancelReportDeadBody(repoter, target, ref reason))
                    {
                        Logger.Info($"{role}によって会議はキャンセルされました。{reason}", "ReportDeadBody");
                        GameStates.CalledMeeting = false;
                        AddDontrepo(repoter, reason, false);
                        check = true;
                    }
                }
                RpcCancelMeeting(repoter, null);
                return false;
            }

            //サボタージュ中でボタンの時、キャンセルする
            if ((Utils.IsActive(SystemTypes.Reactor)
                || Utils.IsActive(SystemTypes.Electrical)
                || Utils.IsActive(SystemTypes.Laboratory)
                || Utils.IsActive(SystemTypes.Comms)
                || Utils.IsActive(SystemTypes.LifeSupp)
                || Utils.IsActive(SystemTypes.HeliSabotage)) && target == null)
            {
                GameStates.CalledMeeting = false;
                Logger.Info($"サボ発生中！キャンセルする！", "ReportDeadBody");
                return false;
            }
            RoleAddAddons.GetRoleAddon(repoter.GetCustomRole(), out var da, repoter, subrole: CustomRoles.NonReport);
            var GiveNonReport = da.GiveNonReport.GetBool();
            var val = da.mode;
            if (target == null)
            {
                if (GiveNonReport && val is RoleAddAddons.NonReportMode.NonReportModeAll or RoleAddAddons.NonReportMode.NotButton)
                {
                    Logger.Info($"役職でノンレポ(Mode: {val})だからキャンセル。", "ReportDeadBody");
                    GameStates.CalledMeeting = false;
                    AddDontrepo(repoter, DontReportreson.NonReport);
                    return false;
                }
                else
                {
                    if (repoter.Is(CustomRoles.NonReport) && NonReport.Mode is NonReport.NonReportMode.NonReportModeAll or NonReport.NonReportMode.NotButton)
                    {
                        Logger.Info($"属性でノンレポ(Mode: {val})だからキャンセル。", "ReportDeadBody");
                        GameStates.CalledMeeting = false;
                        AddDontrepo(repoter, DontReportreson.NonReport);
                        return false;
                    }
                }
            }
            else if (!DontAddonCheck)
            {
                if (GiveNonReport && val is RoleAddAddons.NonReportMode.NonReportModeAll or RoleAddAddons.NonReportMode.NotReport)
                {
                    Logger.Info($"NonReportの設定が{val}だからキャンセル。", "ReportDeadBody");
                    GameStates.CalledMeeting = false;
                    AddDontrepo(repoter, DontReportreson.NonReport);
                    return false;
                }
                else
                if (repoter.Is(CustomRoles.NonReport) && NonReport.Mode is NonReport.NonReportMode.NonReportModeAll or NonReport.NonReportMode.NotReport)
                {
                    Logger.Info($"属性でノンレポ(Mode: {val})だからキャンセル。", "ReportDeadBody");
                    GameStates.CalledMeeting = false;
                    AddDontrepo(repoter, DontReportreson.NonReport);
                    return false;
                }
                else
                if (RoleAddAddons.GetRoleAddon(target?.Object.GetCustomRole() ?? CustomRoles.NotAssigned, out var d, target?.Object, subrole: CustomRoles.Transparent) && d.GiveTransparent.GetBool())
                {
                    GameStates.CalledMeeting = false;
                    Logger.Info($"ターゲットが属性トランスパレントだから通報をキャンセルする。", "ReportDeadBody");
                    AddDontrepo(repoter, DontReportreson.Transparent);
                    return false;
                }
                else
                if (target?.Object.Is(CustomRoles.Transparent) ?? false || Transparent.playerIdList.Contains(target.PlayerId))
                {
                    GameStates.CalledMeeting = false;
                    Logger.Info($"ターゲットが属性トランスパレントだから通報をキャンセルする。", "ReportDeadBody");
                    AddDontrepo(repoter, DontReportreson.Transparent);
                    return false;
                }
            }

            if (!AmongUsClient.Instance.AmHost) return true;

            //通報者が死んでいる場合、本処理で会議がキャンセルされるのでここで止める
            if (repoter?.Data?.IsDead ?? false && checkdie)
            {
                GameStates.CalledMeeting = false;
                Logger.Info($"通報者が死んでいるのでキャンセルする", "ReportDeadBody");
                return false;
            }

            foreach (var role in CustomRoleManager.AllActiveRoles.Values)
            {
                if (role.CancelReportDeadBody(repoter, target, ref reason))
                {
                    Logger.Info($"{role}によって会議はキャンセルされました。{reason}", "ReportDeadBody");
                    GameStates.CalledMeeting = false;
                    AddDontrepo(repoter, reason, false);
                    check = true;
                }
            }
            RpcCancelMeeting(repoter, null);

            if (check) return false;

            if (Options.SyncButtonMode.GetBool() && target == null)
            {
                Logger.Info("最大:" + Options.SyncedButtonCount.GetInt() + ", 現在:" + Options.UsedButtonCount, "ReportDeadBody");
                if (Options.SyncedButtonCount.GetFloat() <= Options.UsedButtonCount)
                {
                    GameStates.CalledMeeting = false;
                    Logger.Info("使用可能ボタン回数が最大数を超えているため、ボタンはキャンセルされました。", "ReportDeadBody");
                    AddDontrepo(repoter, DontReportreson.CantUseButton);
                    return false;
                }
                else Options.UsedButtonCount++;
            }
            return true;

            void AddDontrepo(PlayerControl pc, DontReportreson repo, bool sync = true)
            {
                if (!DontReport.TryAdd(pc.PlayerId, (0, repo))) DontReport[pc.PlayerId] = (0, repo);
                if (sync) RpcCancelMeeting(pc, repo);

                _ = new LateTask(() =>
                {
                    if (!GameStates.CalledMeeting) UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: pc);
                }, 0.2f, "", true);
            }
        }

        public static void RpcCancelMeeting(PlayerControl target, DontReportreson? reason = null)
        {
            if (!AmongUsClient.Instance.AmHost || !PlayerCatch.AnyModClient()) return;

            reason ??= DontReport.TryGetValue(target.PlayerId, out var value) ? value.reason : null;
            if (reason == null) return;

            var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.MeetingInfo, Hazel.SendOption.Reliable);
            writer.Write(true);
            writer.Write(target.PlayerId);
            writer.WritePacked((int)reason.Value);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        public static void RpcSyncMeetingInfo(PlayerControl reporter, NetworkedPlayerInfo target, string dieName = null, string meetingInfo = null, string infoColor = "")
        {
            if (!AmongUsClient.Instance.AmHost || !PlayerCatch.AnyModClient() || reporter == null) return;

            var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.MeetingInfo, Hazel.SendOption.Reliable);
            writer.Write(false);
            writer.Write(reporter.PlayerId);
            writer.Write(target?.PlayerId ?? byte.MaxValue);

            bool flag1 = dieName != null;
            writer.Write(flag1);
            if (flag1) writer.Write(dieName);

            bool flag2 = meetingInfo != null;
            writer.Write(flag2);
            if (flag2)
            {
                writer.Write(meetingInfo);
                writer.Write(infoColor);
            }

            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        public static void CancelMeeting(Hazel.MessageReader reader)
        {
            if (AmongUsClient.Instance.AmHost) return;

            var target = PlayerCatch.GetPlayerById(reader.ReadByte());
            var reason = (DontReportreson)reader.ReadPackedInt32();

            if (target == null) return;

            if (!DontReport.TryAdd(target.PlayerId, (0, reason))) DontReport[target.PlayerId] = (0, reason);
            Logger.Info($"ホストが会議をキャンセルしました: {reason}", "CancelMeeting");
        }

        public static void SetMeetingInfo(Hazel.MessageReader reader)
        {
            if (AmongUsClient.Instance.AmHost) return;

            var reporterId = reader.ReadByte();
            var targetId = reader.ReadByte();

            var reporter = reporterId == byte.MaxValue ? null : PlayerCatch.GetPlayerById(reporterId);
            var target = targetId == byte.MaxValue ? null : PlayerCatch.GetPlayerById(targetId);

            var overrideDieName = reader.ReadBoolean();
            var output = overrideDieName ? reader.ReadString() : string.Empty;

            var overrideInfo = reader.ReadBoolean();
            var meetingInfo = overrideInfo ? reader.ReadString() : string.Empty;
            var infoColorCode = overrideInfo ? reader.ReadString() : string.Empty;

            GameStates.task = false;

            DisableDevice.StartMeeting();
            AdminProvider.CalculateAdmin(true);

            foreach (var kvp in PlayerState.AllPlayerStates)
            {
                var pc = PlayerCatch.GetPlayerById(kvp.Key);
                kvp.Value.LastRoom = pc?.GetPlainShipRoom();
            }

            if (overrideInfo)
            {
                MeetingHudPatch.Oniku = GetString(meetingInfo);
                UtilsNotifyRoles.ExtendedMeetingText = $"<color={infoColorCode}><u>★" + GetString(meetingInfo) + "</u></color>";
                return;
            }

            //レポート
            if (target != null)
            {
                var check = overrideDieName;
                var colorid = Camouflage.PlayerSkins[target.PlayerId].ColorId;
                var DieName = overrideDieName ? output : Palette.GetColorName(colorid);
                var color = overrideDieName ? ModColors.NeutralGray : Palette.PlayerColors[colorid];

                MeetingHudPatch.Oniku = (check ? DieName : UtilsName.GetPlayerColor(target, true)) + Translator.GetString("Meeting.Report") + "\n　" + string.Format(Translator.GetString("Meeting.Shoushu"), UtilsName.GetPlayerColor(reporter.PlayerId, true));
                UtilsNotifyRoles.ExtendedMeetingText = "<u>★".Color(Palette.PlayerColors[Camouflage.PlayerSkins[reporter.PlayerId].ColorId]) + "<#ffffff>" + string.Format(Translator.GetString("MI.die"), DieName.Color(color)) + "</u></color>";
                return;
            }

            //ボタン
            MeetingHudPatch.Oniku = Translator.GetString("Meeting.Button") + "\n　" + string.Format(Translator.GetString("Meeting.Shoushu"), UtilsName.GetPlayerColor(reporter.PlayerId, true));
            UtilsNotifyRoles.ExtendedMeetingText = "<u>★".Color(Palette.PlayerColors[Camouflage.PlayerSkins[reporter.PlayerId].ColorId]) + "<#ffffff>" + Translator.GetString("MI.Bot") + "</u></color>";
        }
    }
}