using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.AddOns.Common;
using TownOfHost.Roles.Neutral;
using static TownOfHost.Roles.Core.RoleBase;

namespace TownOfHost
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
    class ReportDeadBodyPatch
    {
        public static Dictionary<byte, bool> CanReport;
        public static Dictionary<byte, bool> Musisuruoniku;
        public static Dictionary<byte, string> ChengeMeetingInfo;
        public static Dictionary<byte, List<NetworkedPlayerInfo>> WaitReport = new();
        //public static Dictionary<byte, Vector2> Pos = new();
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] NetworkedPlayerInfo target)
        {
            if (GameStates.IsMeeting || GameStates.IsLobby) return false;
            if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Default) return false;

            Logger.Info($"{__instance.GetNameWithRole().RemoveHtmlTags()} => {target?.Object?.GetNameWithRole()?.RemoveHtmlTags() ?? "null"}", "ReportDeadBody");

            var State = PlayerState.GetByPlayerId(__instance.PlayerId);
            if (State.NumberOfRemainingButtons <= 0 && target is null)
            {
                Logger.Info($"{__instance.name}君はもうボタン使ったでしょ!", "ReportDeadBody");
                return false;
            }

            GameStates.Meeting = true;
            if (Options.IsStandardHAS && target != null && __instance == target.Object) return true; //[StandardHAS] ボタンでなく、通報者と死体が同じなら許可
            if (Options.CurrentGameMode is CustomGameMode.HideAndSeek or CustomGameMode.TaskBattle || Options.IsStandardHAS) return false;
            if (!CanReport[__instance.PlayerId])
            {
                GameStates.Meeting = false;
                WaitReport[__instance.PlayerId].Add(target);
                Logger.Warn($"{__instance.GetNameWithRole().RemoveHtmlTags()}:通報禁止中のため可能になるまで待機します", "ReportDeadBody");

                if (DontReport.TryGetValue(__instance.PlayerId, out var check))
                {
                    if (check.reason == DontReportreson.wait) return false;
                }
                if (!DontReport.TryAdd(__instance.PlayerId, (0, DontReportreson.wait))) DontReport[__instance.PlayerId] = (0, DontReportreson.wait);
                _ = new LateTask(() =>
                {
                    if (!GameStates.Meeting) UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: __instance);
                }, 0.2f, "", true);
                return false;
            }

            //ホスト以外はこの先処理しない
            if (!AmongUsClient.Instance.AmHost) return true;

            if (!CheckMeeting(__instance, target)) return false;

            PlayerControlRpcUseZiplinePatch.OnMeeting(__instance, target);

            //=============================================
            //以下、ボタンが押されることが確定したものとする。
            //=============================================

            GameStates.task = false;

            DisableDevice.StartMeeting();
            foreach (var kvp in PlayerState.AllPlayerStates)
            {
                var pc = PlayerCatch.GetPlayerById(kvp.Key);
                kvp.Value.LastRoom = pc?.GetPlainShipRoom();
            }

            UtilsOption.MarkEveryoneDirtySettings();

            AdminProvider.CalculateAdmin(true);

            if (target != null)
            {
                UtilsGameLog.AddGameLog("Meeting", Utils.GetPlayerColor(target, true) + Translator.GetString("Meeting.Report") + "\n\t\t┗  " + string.Format(Translator.GetString("Meeting.Shoushu"), Utils.GetPlayerColor(__instance.PlayerId, true)));
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
                MeetingHudPatch.Oniku = (check ? DieName : Utils.GetPlayerColor(target, true)) + Translator.GetString("Meeting.Report") + "\n　" + string.Format(Translator.GetString("Meeting.Shoushu"), Utils.GetPlayerColor(__instance.PlayerId, true));
                UtilsNotifyRoles.MeetingMoji = "<u>★".Color(Palette.PlayerColors[Camouflage.PlayerSkins[__instance.PlayerId].ColorId]) + "<#ffffff>" + string.Format(Translator.GetString("MI.die"), DieName.Color(color)) + "</u></color>";
            }
            else
            {
                UtilsGameLog.AddGameLog("Meeting", Translator.GetString("Meeting.Button") + "\n\t\t┗  " + string.Format(Translator.GetString("Meeting.Shoushu"), Utils.GetPlayerColor(__instance.PlayerId, true)));
                MeetingHudPatch.Oniku = Translator.GetString("Meeting.Button") + "\n　" + string.Format(Translator.GetString("Meeting.Shoushu"), Utils.GetPlayerColor(__instance.PlayerId, true));
                UtilsNotifyRoles.MeetingMoji = "<u>★".Color(Palette.PlayerColors[Camouflage.PlayerSkins[__instance.PlayerId].ColorId]) + "<#ffffff>" + Translator.GetString("MI.Bot") + "</u></color>";
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
                    Camouflage.RpcSetSkin(pc, RevertToDefault: true, kyousei: true);
                }
            }, 1f, "SetSkin", false);

            // var State = PlayerState.GetByPlayerId(__instance.PlayerId);
            if (State.NumberOfRemainingButtons > 0 && target is null)
                State.NumberOfRemainingButtons--;

            MeetingTimeManager.OnReportDeadBody();

            UtilsNotifyRoles.NotifyMeetingRoles();

            UtilsOption.SyncAllSettings();

            //if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Default) return false;
            //サボ関係多分なしに～
            //押したのなら強制で始める
            MeetingRoomManager.Instance.AssignSelf(__instance, target);
            DestroyableSingleton<HudManager>.Instance.OpenMeetingRoom(__instance);
            __instance.RpcStartMeeting(target);

            return true;
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
        /// <param name="repo">通報者</param>
        /// <param name="target">死体(null=button)</param>
        /// <param name="ch">属性等のチェック入れるか</param>
        public static void DieCheckReport(PlayerControl repo, NetworkedPlayerInfo target = null, bool? ch = true, string Meetinginfo = "", string colorcode = "#000000")
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Default) return;

            Logger.Info($"{repo.GetNameWithRole().RemoveHtmlTags()} => {target?.Object?.GetNameWithRole()?.RemoveHtmlTags() ?? "null"}", "DieCheckReport");

            if (repo == null)
            {
                Logger.Error($"{repo?.Data?.GetLogPlayerName() ?? "???"} がnull!", "DieCheckReport");
            }
            if (GameStates.IsMeeting || GameStates.IsLobby) return;

            var State = PlayerState.GetByPlayerId(repo.PlayerId);
            if (State.NumberOfRemainingButtons <= 0 && target is null && ch is not false) return;

            if (ch is null or true)
                if (!CheckMeeting(repo, target, checkdie: ch is true)) return;

            PlayerControlRpcUseZiplinePatch.OnMeeting(repo, target);

            GameStates.Meeting = true;
            GameStates.task = false;// Pos.Clear();

            DisableDevice.StartMeeting();
            foreach (var kvp in PlayerState.AllPlayerStates)
            {
                var pc = PlayerCatch.GetPlayerById(kvp.Key);
                kvp.Value.LastRoom = pc?.GetPlainShipRoom();
            }

            UtilsOption.MarkEveryoneDirtySettings();
            AdminProvider.CalculateAdmin(true);

            if (Meetinginfo == "")
            {
                if (target != null)
                {
                    UtilsGameLog.AddGameLog("Meeting", Utils.GetPlayerColor(target, true) + Translator.GetString("Meeting.Report") + "\n\t\t┗  " + string.Format(Translator.GetString("Meeting.Shoushu"), Utils.GetPlayerColor(repo.PlayerId, true)));
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
                    MeetingHudPatch.Oniku = (check ? DieName : Utils.GetPlayerColor(target, true)) + Translator.GetString("Meeting.Report") + "\n　" + string.Format(Translator.GetString("Meeting.Shoushu"), Utils.GetPlayerColor(repo.PlayerId, true));
                    UtilsNotifyRoles.MeetingMoji = "<u>★".Color(Palette.PlayerColors[Camouflage.PlayerSkins[repo.PlayerId].ColorId]) + "<#ffffff>" + string.Format(Translator.GetString("MI.die"), DieName.Color(color)) + "</u></color>";
                }
                else
                {
                    UtilsGameLog.AddGameLog("Meeting", Translator.GetString("Meeting.Button") + "\n\t\t┗  " + string.Format(Translator.GetString("Meeting.Shoushu"), Utils.GetPlayerColor(repo.PlayerId, true)));
                    MeetingHudPatch.Oniku = Translator.GetString("Meeting.Button") + "\n　" + string.Format(Translator.GetString("Meeting.Shoushu"), Utils.GetPlayerColor(repo.PlayerId, true));
                    UtilsNotifyRoles.MeetingMoji = "<u>★".Color(Palette.PlayerColors[Camouflage.PlayerSkins[repo.PlayerId].ColorId]) + "<#ffffff>" + Translator.GetString("MI.Bot") + "</u></color>";
                }
            }
            else
            {
                MeetingHudPatch.Oniku = Meetinginfo;
                UtilsNotifyRoles.MeetingMoji = $"<color={colorcode}><u>★" + Meetinginfo + "</u></color>";
            }

            if (!Options.firstturnmeeting || !MeetingStates.FirstMeeting)
                foreach (var pc in PlayerCatch.AllPlayerControls)
                {
                    var roleClass = pc.GetRoleClass();
                    roleClass?.OnReportDeadBody(repo, target);
                }
            _ = new LateTask(() =>
            {
                foreach (var pc in PlayerCatch.AllPlayerControls)
                {
                    if (!pc) continue;
                    Camouflage.RpcSetSkin(pc, RevertToDefault: true, kyousei: true);
                }
            }, 1f, "SetSkin", false);

            //if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Default) return;
            UtilsNotifyRoles.NotifyMeetingRoles();

            MeetingTimeManager.OnReportDeadBody();

            UtilsOption.SyncAllSettings();

            MeetingRoomManager.Instance.AssignSelf(repo, target);
            DestroyableSingleton<HudManager>.Instance.OpenMeetingRoom(repo);
            repo.RpcStartMeeting(target);
        }
        public static Dictionary<byte, (float time, DontReportreson reason)> DontReport = new();
        public static string Dontrepomark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
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
                    }
                }

            return "";
        }
        public static bool CheckMeeting(PlayerControl repoter, NetworkedPlayerInfo target, bool checkdie = true)
        {
            var DontAddonCheck = false;
            var r = DontReportreson.None;
            var check = false;
            if (target != null)
            {
                if (repoter.GetRoleClass() is MassMedia massMedia)
                {
                    if (massMedia.Target == target.PlayerId)
                        DontAddonCheck = true;
                }
                if (repoter.GetCustomRole() is CustomRoles.Vulture)
                    DontAddonCheck = true;
            }
            if (SuddenDeathMode.NowSuddenDeathMode)
            {
                foreach (var role in CustomRoleManager.AllActiveRoles.Values)
                {
                    if (role.CancelReportDeadBody(repoter, target, ref r))
                    {
                        Logger.Info($"{role}によって会議はキャンセルされました。{r}", "ReportDeadBody");
                        GameStates.Meeting = false;
                        AddDontrepo(repoter, r);
                        check = true;
                    }
                }
            }

            if (SuddenDeathMode.NowSuddenDeathMode)
            {
                Logger.Info($"サドンデスモードなのにボタンを使おうと?", "ReportDeadBody");
                AddDontrepo(repoter, DontReportreson.CantUseButton);
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
                Logger.Info($"サボ発生中！キャンセルする！", "ReportDeadBody");
                return false;
            }
            RoleAddAddons.GetRoleAddon(repoter.GetCustomRole(), out var da, repoter, subrole: CustomRoles.NonReport);
            var GiveNonReport = da.GiveNonReport.GetBool();
            var val = da.mode;
            if (target == null)
            {
                if (GiveNonReport && val is RoleAddAddons.Convener.ConvenerAll or RoleAddAddons.Convener.NotButton)
                {
                    Logger.Info($"役職でノンレポ(Mode: {val})だからキャンセル。", "ReportDeadBody");
                    GameStates.Meeting = false;
                    AddDontrepo(repoter, DontReportreson.NonReport);
                    return false;
                }
                else
                {
                    if (repoter.Is(CustomRoles.NonReport) && NonReport.Mode is NonReport.Convener.ConvenerAll or NonReport.Convener.NotButton)
                    {
                        Logger.Info($"属性でノンレポ(Mode: {val})だからキャンセル。", "ReportDeadBody");
                        GameStates.Meeting = false;
                        AddDontrepo(repoter, DontReportreson.NonReport);
                        return false;
                    }
                }
            }
            else if (!DontAddonCheck)
            {
                if (GiveNonReport && val is RoleAddAddons.Convener.ConvenerAll or RoleAddAddons.Convener.NotReport)
                {
                    Logger.Info($"NonReportの設定が{val}だからキャンセル。", "ReportDeadBody");
                    GameStates.Meeting = false;
                    AddDontrepo(repoter, DontReportreson.NonReport);
                    return false;
                }
                else
                if (repoter.Is(CustomRoles.NonReport) && NonReport.Mode is NonReport.Convener.ConvenerAll or NonReport.Convener.NotReport)
                {
                    Logger.Info($"属性でノンレポ(Mode: {val})だからキャンセル。", "ReportDeadBody");
                    GameStates.Meeting = false;
                    AddDontrepo(repoter, DontReportreson.NonReport);
                    return false;
                }
                else
                if (RoleAddAddons.GetRoleAddon(target?.Object.GetCustomRole() ?? CustomRoles.NotAssigned, out var d, target?.Object, subrole: CustomRoles.Transparent) && d.GiveTransparent.GetBool())
                {
                    GameStates.Meeting = false;
                    Logger.Info($"ターゲットが属性トランスパレントだから通報をキャンセルする。", "ReportDeadBody");
                    AddDontrepo(repoter, DontReportreson.Transparent);
                    return false;
                }
                else
                if (target?.Object.Is(CustomRoles.Transparent) ?? false || Transparent.playerIdList.Contains(target.PlayerId))
                {
                    GameStates.Meeting = false;
                    Logger.Info($"ターゲットが属性トランスパレントだから通報をキャンセルする。", "ReportDeadBody");
                    AddDontrepo(repoter, DontReportreson.Transparent);
                    return false;
                }
            }

            if (!AmongUsClient.Instance.AmHost) return true;

            //通報者が死んでいる場合、本処理で会議がキャンセルされるのでここで止める
            if (repoter?.Data?.IsDead ?? false && checkdie)
            {
                GameStates.Meeting = false;
                Logger.Info($"通報者が死んでいるのでキャンセルする", "ReportDeadBody");
                return false;
            }

            foreach (var role in CustomRoleManager.AllActiveRoles.Values)
            {
                if (role.CancelReportDeadBody(repoter, target, ref r))
                {
                    Logger.Info($"{role}によって会議はキャンセルされました。{r}", "ReportDeadBody");
                    GameStates.Meeting = false;
                    AddDontrepo(repoter, r);
                    check = true;
                }
            }
            if (check) return false;

            if (Options.SyncButtonMode.GetBool() && target == null)
            {
                Logger.Info("最大:" + Options.SyncedButtonCount.GetInt() + ", 現在:" + Options.UsedButtonCount, "ReportDeadBody");
                if (Options.SyncedButtonCount.GetFloat() <= Options.UsedButtonCount)
                {
                    GameStates.Meeting = false;
                    Logger.Info("使用可能ボタン回数が最大数を超えているため、ボタンはキャンセルされました。", "ReportDeadBody");
                    AddDontrepo(repoter, DontReportreson.CantUseButton);
                    return false;
                }
                else Options.UsedButtonCount++;
            }
            return true;

            void AddDontrepo(PlayerControl pc, DontReportreson repo)
            {
                if (!DontReport.TryAdd(pc.PlayerId, (0, repo))) DontReport[pc.PlayerId] = (0, repo);
                _ = new LateTask(() =>
                {
                    if (!GameStates.Meeting) UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: pc);
                }, 0.2f, "", true);
            }
        }
    }
}