using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using TMPro;
using UnityEngine;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Templates;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
    class EndGamePatch
    {
        public static Dictionary<byte, string> SummaryText = new();
        public static string KillLog = "";
        public static string outputLog = "";
        public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ref EndGameResult endGameResult)
        {
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            GameStates.canmusic = true;
            GameStates.InGame =
            GameStates.task =
            GameStates.CalledMeeting =
            GameStates.ExiledAnimate = false;
            UtilsGameLog.day++;
            UtilsGameLog.WriteGameLog();
            if (TaskBattle.IsAllMapMode)
            {
                TaskBattle.allmapmodetimer += HudManagerPatch.TaskBattleTimer;
                TaskBattle.Maptimer.Add(Main.NormalOptions.MapId, HudManagerPatch.TaskBattleTimer);
            }
            if (CustomWinnerHolder.WinnerTeam is CustomWinner.Draw || (TaskBattle.IsAllMapMode && Main.NormalOptions.MapId is 5))
            {
                TaskBattle.IsAllMapMode = false;
            }

            Logger.Info("-----------ゲーム終了-----------", "Phase");
            if (!GameStates.IsModHost) return;

            SummaryText = new();
            foreach (var id in PlayerState.AllPlayerStates.Keys)
                SummaryText[id] = UtilsGameLog.SummaryTexts(id);
            //if (!AmongUsClient.Instance.AmHost) return;

            var meg = GetString($"{(CustomRoles)CustomWinnerHolder.WinnerTeam}") + GetString("Team") + GetString("Win");
            var winnerColor = ((CustomRoles)CustomWinnerHolder.WinnerTeam).GetRoleInfo()?.RoleColor ?? UtilsRoleText.GetRoleColor((CustomRoles)CustomWinnerHolder.WinnerTeam);

            switch (CustomWinnerHolder.WinnerTeam)
            {
                case CustomWinner.Draw: meg = GetString("ForceEnd"); break;
                case CustomWinner.None: meg = GetString("EveryoneDied"); break;
                case CustomWinner.SuddenDeathRed: meg = GetString("SuddenDeathRed"); winnerColor = ModColors.Red; break;
                case CustomWinner.SuddenDeathBlue: meg = GetString("SuddenDeathBlue"); winnerColor = ModColors.Blue; break;
                case CustomWinner.SuddenDeathYellow: meg = GetString("SuddenDeathYellow"); winnerColor = ModColors.Yellow; break;
                case CustomWinner.SuddenDeathGreen: meg = GetString("SuddenDeathGreen"); winnerColor = ModColors.Green; break;
                case CustomWinner.SuddenDeathPurple: meg = GetString("SuddenDeathPurple"); winnerColor = ModColors.Purple; break;
            }

            var star = "★".Color(winnerColor);
            KillLog = $"{GetString("GameLog")}\n" + UtilsGameLog.gamelog + "\n\n<b>" + star + meg.Mark(winnerColor, false) + "</b>" + star;
            outputLog = "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~\n" + UtilsGameLog.gamelog + "\n\n<b>" + star + meg.Mark(winnerColor, false) + "</b>" + star;

            LastGameSave.CreateIfNotExists();
            Main.Alltask = UtilsTask.AllTaskstext(false, false, false, false, false).RemoveHtmlTags();

            //winnerListリセット
            EndGameResult.CachedWinners = new Il2CppSystem.Collections.Generic.List<CachedPlayerData>();
            var winner = new List<PlayerControl>();
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                if (CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId)) winner.Add(pc);
            }
            foreach (var team in CustomWinnerHolder.WinnerRoles)
            {
                winner.AddRange(PlayerCatch.AllPlayerControls.Where(p => p.Is(team) && !winner.Contains(p)));
            }
            foreach (var id in CustomWinnerHolder.CantWinPlayerIds)
            {
                var pc = PlayerCatch.GetPlayerById(id);
                if (pc == null) continue;
                winner.Remove(pc);
            }

            //HideAndSeek専用
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek &&
                CustomWinnerHolder.WinnerTeam != CustomWinner.Draw && CustomWinnerHolder.WinnerTeam != CustomWinner.None)
            {
                winner = new();
                foreach (var pc in PlayerCatch.AllPlayerControls)
                {
                    var role = PlayerState.GetByPlayerId(pc.PlayerId).MainRole;
                    if (role.GetCustomRoleTypes() == CustomRoleTypes.Impostor)
                    {
                        if (CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor)
                            winner.Add(pc);
                    }
                    else if (role.GetCustomRoleTypes() == CustomRoleTypes.Crewmate)
                    {
                        if (CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate)
                            winner.Add(pc);
                    }
                    else if (role == CustomRoles.HASTroll && pc.Data.IsDead)
                    {
                        //トロールが殺されていれば単独勝ち
                        winner = new()
                        {
                            pc
                        };
                        break;
                    }
                    else if (role == CustomRoles.HASFox && CustomWinnerHolder.WinnerTeam != CustomWinner.HASTroll && !pc.Data.IsDead)
                    {
                        winner.Add(pc);
                        CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.HASFox);
                    }
                }
            }
            Main.winnerList = new();
            foreach (var pc in winner)
            {
                if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Draw && pc.Is(CustomRoles.GM)) continue;

                if (CustomWinnerHolder.WinnerIds.Contains(pc.PlayerId) && Main.winnerList.Contains(pc.PlayerId)) continue;
                if (CustomWinnerHolder.CantWinPlayerIds.Contains(pc.PlayerId)) continue;

                EndGameResult.CachedWinners.Add(new CachedPlayerData(pc.Data));
                Main.winnerList.Add(pc.PlayerId);
            }

            Main.VisibleTasksCount = false;
            if (AmongUsClient.Instance.AmHost)
            {
                Main.NormalOptions.KillCooldown = Options.DefaultKillCooldown;
                Main.RealOptionsData.Restore(GameOptionsManager.Instance.CurrentGameOptions);
                GameOptionsSender.AllSenders.Clear();
                GameOptionsSender.AllSenders.Add(new NormalGameOptionsSender());
                /* Send SyncSettings RPC */
            }
            //オブジェクト破棄
            CustomRoleManager.Dispose();

            Camouflage.PlayerSkins.Clear();
            Statistics.Update();
        }
    }
    [HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.SetEverythingUp))]
    class SetEverythingUpPatch
    {
        public static string LastWinsText = "";
        private static TextMeshPro roleSummary;
        private static SimpleButton showHideButton;
        public static SimpleButton ScreenShotbutton;

        public static void Postfix(EndGameManager __instance)
        {
            if (!Main.playerVersion.ContainsKey(0)) return;
            //#######################################
            //          ==勝利陣営表示==
            //#######################################

            var WinnerTextObject = UnityEngine.Object.Instantiate(__instance.WinText.gameObject);
            WinnerTextObject.transform.position = new(__instance.WinText.transform.position.x, __instance.WinText.transform.position.y - 0.5f, __instance.WinText.transform.position.z);
            WinnerTextObject.transform.localScale = new(0.6f, 0.6f, 0.6f);
            var WinnerText = WinnerTextObject.GetComponent<TMPro.TextMeshPro>(); //WinTextと同じ型のコンポーネントを取得
            WinnerText.fontSizeMin = 3f;

            string CustomWinnerText;
            string CustomWinnerColor;
            string WinText = __instance.WinText.text;
            Color BackgroundBar = __instance.BackgroundBar.material.color;
            Color WinColor = __instance.WinText.color;

            (CustomWinnerText, CustomWinnerColor, WinText, BackgroundBar, WinColor) = UtilsGameLog.GetWinnerText(WinText, BackgroundBar, WinColor);

            WinnerText.text = CustomWinnerText;
            __instance.WinText.text = WinText;
            __instance.WinText.color = WinColor;
            __instance.BackgroundBar.material.color = BackgroundBar;

            LastWinsText = WinnerText.text;

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            //#######################################
            //           ==最終結果表示==
            //#######################################

            var showInitially = Main.ShowResults.Value;
            showHideButton = new SimpleButton(
                __instance.transform,
                "ShowHideResultsButton",
                new(-4.5f, 2.6f, -14f),  // BackgroundLayer(z=-13)より手前
                new(0, 136, 209, byte.MaxValue),
                new(0, 196, byte.MaxValue, byte.MaxValue),
                () =>
                {
                    var setToActive = !roleSummary.gameObject.activeSelf;
                    roleSummary.gameObject.SetActive(setToActive);
                    Main.ShowResults.Value = setToActive;
                    showHideButton.Label.text = GetString(setToActive ? "HideResults" : "ShowResults");
                },
                GetString(showInitially ? "HideResults" : "ShowResults"))
            {
                Scale = new(1.5f, 0.5f),
                FontSize = 2f,
            };

            ScreenShotbutton = new SimpleButton(
                __instance.transform,
                "ScreenShotButton",
                new(-3.5f, 2.6f, -14f),
                new(0, 245, 185, byte.MaxValue),
                new(66, 245, 185, byte.MaxValue),
                () =>
                {
                    LastGameSave.SeveImage();
                },
                "保存")
            {
                Scale = new(0.5f, 0.5f),
                FontSize = 2f,
            };

            StringBuilder sb = new();
            if (TaskBattle.IsRTAMode && Options.CurrentGameMode == CustomGameMode.TaskBattle)
            {
                sb.Append(UtilsGameLog.GetRTAText());
                EndGamePatch.KillLog += $"<#D4AF37>~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~</color>{"★".Color(Palette.DisabledGrey)}\n" + sb.ToString().Replace("\n", "\n　") + $"\n{"★".Color(Palette.DisabledGrey)}<#D4AF37>~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~</color>{"★".Color(Palette.DisabledGrey)}";
            }
            else
            {
                sb.Append(GetString("RoleSummaryText"));
                List<byte> cloneRoles = new(PlayerState.AllPlayerStates.Keys);
                foreach (var id in Main.winnerList)
                {
                    sb.Append($"\n<{CustomWinnerColor}>★</color> ").Append(EndGamePatch.SummaryText[id]);
                    cloneRoles.Remove(id);
                }
                foreach (var id in cloneRoles)
                {
                    sb.Append($"\n　 ").Append(EndGamePatch.SummaryText[id]);
                }
            }
            roleSummary = TMPTemplate.Create(
                "RoleSummaryText",
                sb.ToString(),
                Color.white,
                1.25f,
                TextAlignmentOptions.TopLeft,
                setActive: showInitially,
                parent: showHideButton.Button.transform);
            roleSummary.transform.localPosition = new(1.7f, -0.4f, 0f);
            roleSummary.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
            roleSummary.gameObject.SetActive(!Main.AssignSameRoles);

            //if (Main.UseWebHook.Value) UtilsWebHook.WH_ShowLastResult();
            if (Main.IsAndroid()) return;
            if (Main.AutoSaveScreenShot.Value || Main.UseWebHook.Value)
            {
                var endGameNavigation = GameObject.Find("EndGameNavigation");
                endGameNavigation.SetActive(false);
                ScreenShotbutton.Button.transform.SetLocalY(-50);
                _ = new LateTask(() =>
                {
                    LastGameSave.SeveImage(true);
                    Webhook.SendResult(ScreenCapture.CaptureScreenshotAsTexture().EncodeToPNG());
                }, 3f, "", true);
                _ = new LateTask(() =>
                {
                    endGameNavigation.SetActive(true);
                    ScreenShotbutton.Button.transform.SetLocalY(2.6f);
                }, 5f, "", true);
            }
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            //Utils.ApplySuffix();
        }
    }

    [HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.GetStingerVol))]
    class EndGameManagerGetStingerVolPatch
    {
        public static void Postfix(EndGameManager __instance, ref AudioSource source)
        {
            //非クライアントの勝利の時アウトロ表示のため、クルー勝利の時もインポスター勝利音なってるから修正
            if (CustomWinnerHolder.WinnerTeam is CustomWinner.Crewmate or CustomWinner.TaskPlayerB)
            {
                source.clip = __instance.CrewStinger;
            }
        }
    }
}