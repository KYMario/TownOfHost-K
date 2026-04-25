using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using Hazel;
using TMPro;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Templates;
using UnityEngine;
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

            var meg = GetString($"{(CustomRoles)CustomWinnerHolder.WinnerTeam}") + GetString("Team") + GetString("Win");
            var winnerColor = ((CustomRoles)CustomWinnerHolder.WinnerTeam).GetRoleInfo()?.RoleColor ?? UtilsRoleText.GetRoleColor((CustomRoles)CustomWinnerHolder.WinnerTeam);
            if (UtilsGameLog.IsPavlovWinnerTeam())
            {
                meg = GetString("TeamPavlov") + GetString("Win");
                winnerColor = UtilsRoleText.GetRoleColor(CustomRoles.PavlovDog);
            }

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
            outputLog = AmongUsClient.Instance.AmHost ? "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~\n" + UtilsGameLog.gamelog + "\n\n<b>" + star + meg.Mark(winnerColor, false) + "</b>" + star
            : "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~\n" + star + meg.Mark(winnerColor, false) + "/b" + star;

            LastGameSave.CreateIfNotExists();
            Main.Alltask = UtilsTask.AllTaskstext(false, false, false, false, false).RemoveHtmlTags();

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
                        winner = new() { pc };
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
            }
            CustomRoleManager.Dispose();
            Camouflage.PlayerSkins.Clear();
            Statistics.Update();
            CheckGetNomalAchievement.OnGameEnd();
            Achievements.UpdateAchievement();

            // ★ バニラクライアントに勝利結果を送信
            if (AmongUsClient.Instance.AmHost)
            {
                string vanillaResult = meg;
                SetEverythingUpPatch.RpcSendResultToVanilla(vanillaResult);
            }
        }
    }

    [HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.SetEverythingUp))]
    class SetEverythingUpPatch
    {
        public static string LastWinsText = "";
        // ★ バニラ向け表示用にstaticで保持
        public static string VanillaResultText = "";

        // ★ Prefix: バニラクライアントが自分で名前を書き換えて結果を表示
        public static void Prefix(EndGameManager __instance)
        {
            if (Main.playerVersion.ContainsKey(0)) return; // MODホストがいる場合はPostfixで処理
            if (string.IsNullOrEmpty(VanillaResultText)) return;

            // バニラクライアントは自分自身の名前に結果を乗っけて表示
            var localPc = PlayerControl.LocalPlayer;
            if (localPc == null) return;

            string origName = localPc.Data?.PlayerName ?? "";
            string nameWithResult = $"{origName}\n<size=60%>{VanillaResultText}</size>";

            try { localPc.cosmetics.nameText.text = nameWithResult; }
            catch { }
        }
        private static TextMeshPro roleSummary;
        private static SimpleButton showHideButton;
        public static SimpleButton ScreenShotbutton;
        public static StringBuilder sb = new();

        public static void Postfix(EndGameManager __instance)
        {
            // ★ バニラクライアント向け: MODなしでも試合結果を左上に表示
            // EndGameManager.SetEverythingUpは全クライアントで動くのでここに書く
            // ★ バニラクライアント: MODがない場合はShowVanillaResultのみ実行してreturn
            if (!Main.playerVersion.ContainsKey(0))
            {
                ShowVanillaResult(__instance);
                return;
            }

            // ★ MODクライアントでもバニラ表示を追加（左上のroleSummaryが見えない場合の補助）
            if (!AmongUsClient.Instance.AmHost && !string.IsNullOrEmpty(VanillaResultText))
            {
                ShowVanillaResult(__instance);
            }

            //#######################################
            //          ==勝利陣営表示==
            //#######################################

            var WinnerTextObject = UnityEngine.Object.Instantiate(__instance.WinText.gameObject);
            WinnerTextObject.transform.position = new(__instance.WinText.transform.position.x, __instance.WinText.transform.position.y - 0.5f, __instance.WinText.transform.position.z);
            WinnerTextObject.transform.localScale = new(0.6f, 0.6f, 0.6f);
            var WinnerText = WinnerTextObject.GetComponent<TMPro.TextMeshPro>();
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
            __instance.transform.SetLocalZ(20);

            //#######################################
            //           ==最終結果表示==
            //#######################################

            var parent = TMPTemplate.Create("parent");
            parent.alignment = TextAlignmentOptions.TopRight;
            parent.rectTransform.pivot = new(2.1f, -10.5f);
            var parentAspectPos = parent.gameObject.AddComponent<AspectPosition>();
            parentAspectPos.Alignment = AspectPosition.EdgeAlignments.LeftTop;
            parentAspectPos.DistanceFromEdge = new(5.3f, 3, 0);
            parent.gameObject.SetActive(true);

            var showInitially = Main.ShowResults.Value;
            showHideButton = new SimpleButton(
                parent.transform,
                "ShowHideResultsButton",
                new(-4.5f, 2.6f, -14f),
                new(0, 136, 209, byte.MaxValue),
                new(0, 196, byte.MaxValue, byte.MaxValue),
                () =>
                {
                    var setToActive = !roleSummary.gameObject.activeSelf;
                    if (setToActive is false)
                    {
                        if (roleSummary.text.Contains("<size=0>★</size>"))
                        {
                            roleSummary.text = Achievements.GetAllAchievement();
                            showHideButton.Label.text = GetString("HideResults");
                            return;
                        }
                    }
                    roleSummary.text = sb.ToString() + "<size=0>★</size>";
                    roleSummary.gameObject.SetActive(setToActive);
                    Main.ShowResults.Value = setToActive;
                    showHideButton.Label.text = GetString(setToActive ? "ShowAward" : "ShowResults");
                },
                GetString(showInitially ? "ShowAward" : "ShowResults"))
            {
                Scale = new(1.5f, 0.5f),
                FontSize = 2f,
            };

            ScreenShotbutton = new SimpleButton(
                parent.transform,
                "ScreenShotButton",
                new(-3.5f, 2.6f, -14f),
                new(0, 245, 185, byte.MaxValue),
                new(66, 245, 185, byte.MaxValue),
                () => { LastGameSave.SeveImage(); },
                Main.UseingJapanese ? "保存" : "Save")
            {
                Scale = new(0.5f, 0.5f),
                FontSize = 2f,
            };

            sb = new();
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
                    sb.Append($"\n<{CustomWinnerColor}>★</color> ").Append(EndGamePatch.SummaryText.TryGetValue(id, out var name) ? name : "???");
                    cloneRoles.Remove(id);
                }
                foreach (var id in cloneRoles)
                {
                    sb.Append($"\n　 ").Append(EndGamePatch.SummaryText.TryGetValue(id, out var name) ? name : "???");
                }
            }
            roleSummary = TMPTemplate.Create(
                "RoleSummaryText",
                sb.ToString() + "<size=0>★</size>",
                Color.white,
                1.25f,
                TextAlignmentOptions.TopLeft,
                setActive: showInitially,
                parent: showHideButton.Button.transform);
            roleSummary.transform.localPosition = new(1.7f, -0.4f, 0f);
            roleSummary.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
            roleSummary.gameObject.SetActive(!Main.AssignSameRoles);

            var modtext = TMPTemplate.Create(
                "ModText",
                $"<b><{Main.ModColor}>{Main.ModName}</color><size=80%>v{Main.PluginShowVersion}</b>",
                Color.white,
                1.25f,
                TextAlignmentOptions.TopLeft,
                setActive: false);
            modtext.transform.localScale = new Vector3(1.7f, 1.7f, 1f);
            modtext.alignment = TextAlignmentOptions.TopRight;
            modtext.rectTransform.pivot = new(0.3f, -6f);
            var modtextAspectPos = modtext.gameObject.AddComponent<AspectPosition>();
            modtextAspectPos.Alignment = AspectPosition.EdgeAlignments.RightTop;
            modtextAspectPos.DistanceFromEdge = new(5f, 3f);
            modtext.gameObject.SetActive(true);

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
        }

        // ★ バニラクライアント（MODなし）向けに試合結果をUIに直接追加
        private static void ShowVanillaResult(EndGameManager instance)
        {
            try
            {
                // ★ WinTextの下に結果テキストを追加表示
                string displayText = string.IsNullOrEmpty(VanillaResultText)
                    ? instance.WinText.text
                    : VanillaResultText;

                var resultObj = UnityEngine.Object.Instantiate(instance.WinText.gameObject);
                resultObj.transform.position = new(
                    instance.WinText.transform.position.x,
                    instance.WinText.transform.position.y - 1.2f,
                    instance.WinText.transform.position.z);
                resultObj.transform.localScale = new(0.45f, 0.45f, 0.45f);

                var resultText = resultObj.GetComponent<TMPro.TextMeshPro>();
                resultText.text = displayText;
                resultText.color = Color.white;
                resultText.fontSizeMin = 2f;
                resultText.alignment = TextAlignmentOptions.Center;
            }
            catch { }
        }

        // ★ ホストからバニラクライアントへRPCで結果テキストを送信
        // EndGamePatchのPostfixから呼ぶ
        public static void RpcSendResultToVanilla(string resultText)
        {
            if (!AmongUsClient.Instance.AmHost) return;

            VanillaResultText = resultText;

            // ★ バニラクライアント（MODなし）にカスタムRPCで送信
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                if (pc.AmOwner) continue;
                if (pc.IsModClient()) continue; // MODクライアントは自分でUI表示できる

                // ★ 名前改行方式でバニラに無理やり表示
                // WinTextの下に結果が見える
                string nameWithResult =
                    $"{Main.AllPlayerNames.GetValueOrDefault(pc.PlayerId, pc.Data.PlayerName)}\n" +
                    $"<size=55%>{resultText}</size>";

                var writer = AmongUsClient.Instance.StartRpcImmediately(
                    pc.NetId, (byte)RpcCalls.SetName, SendOption.Reliable);
                writer.Write(pc.Data.NetId);
                writer.Write(nameWithResult);
                writer.Write(false);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
        }
    }

    [HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.GetStingerVol))]
    class EndGameManagerGetStingerVolPatch
    {
        public static void Postfix(EndGameManager __instance, ref AudioSource source)
        {
            if (CustomWinnerHolder.WinnerTeam is CustomWinner.Crewmate or CustomWinner.TaskPlayerB)
            {
                source.clip = __instance.CrewStinger;
            }
        }
    }
}