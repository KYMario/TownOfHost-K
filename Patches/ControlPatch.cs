using System.Linq;
using HarmonyLib;
using UnityEngine;

using TownOfHost.Modules;
using System.Collections.Generic;

namespace TownOfHost
{
    [HarmonyPatch(typeof(ControllerManager), nameof(ControllerManager.Update))]
    class ControllerManagerUpdatePatch
    {
        static readonly (int, int)[] resolutions = { (480, 270), (640, 360), (800, 450), (1280, 720), (1600, 900), (1920, 1080) };
        static int resolutionIndex = 0;
        public static void Postfix(ControllerManager __instance)
        {
            //カスタム設定切り替え
            if (GameStates.IsLobby && !GameStates.IsFreePlay)
            {
                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    OptionShower.Next();
                }
                for (var i = 0; i < 9; i++)
                {
                    if (ORGetKeysDown(KeyCode.Alpha1 + i, KeyCode.Keypad1 + i) && OptionShower.pages.Count >= i + 1)
                        OptionShower.currentPage = i;
                }
                // 現在の設定を文字列形式のデータに変換してコピー
                if (GetKeysDown(KeyCode.O, KeyCode.LeftAlt))
                {
                    OptionSerializer.SaveToClipboard();
                }
                // 現在の設定を文字列形式のデータに変換してファイルに出力
                if (GetKeysDown(KeyCode.L, KeyCode.LeftAlt))
                {
                    OptionSerializer.SaveToFile();
                }
                // クリップボードから文字列形式の設定データを読み込む
                if (GetKeysDown(KeyCode.P, KeyCode.LeftAlt))
                {
                    OptionSerializer.LoadFromClipboard();
                }
            }
            //解像度変更
            if (Input.GetKeyDown(KeyCode.F11))
            {
                resolutionIndex++;
                if (resolutionIndex >= resolutions.Length) resolutionIndex = 0;
                ResolutionManager.SetResolution(resolutions[resolutionIndex].Item1, resolutions[resolutionIndex].Item2, false);
            }
            //カスタム翻訳のリロード
            if (GetKeysDown(KeyCode.F5, KeyCode.T))
            {
                Logger.Info("Reload Custom Translation File", "KeyCommand");
                Translator.LoadLangs();
                Logger.seeingame("Reloaded Custom Translation File");
            }
            if (GetKeysDown(KeyCode.F5, KeyCode.X))
            {
                Logger.Info("Export Custom Translation File", "KeyCommand");
                Translator.ExportCustomTranslation();
                Logger.seeingame("Exported Custom Translation File");
            }
            if (GetKeysDown(KeyCode.Y, KeyCode.R) && !Event.Special)
            {
                Logger.Info($"YRWoKentisitaze", "KeyCommand");
                Event.Special = GameStates.IsNotJoined;
                if (Event.Special)
                {
                    if (CredentialsPatch.TohkLogo)
                    {
                        CredentialsPatch.TohkLogo.sprite = UtilsSprite.LoadSprite("TownOfHost.Resources.TownOfHost-K_A.png", 300f);
                    }
                }
            }
            //ログファイルのダンプ
            if (GetKeysDown(KeyCode.F1, KeyCode.LeftControl))
            {
                Logger.Info("Dump Logs", "KeyCommand");
                UtilsOutputLog.DumpLog();
            }
            //現在の設定をテキストとしてコピー
            if (GetKeysDown(KeyCode.LeftAlt, KeyCode.C) && !Input.GetKey(KeyCode.LeftShift) && !GameStates.IsNotJoined)
            {
                UtilsShowOption.CopyCurrentSettings();
            }
            //実行ファイルのフォルダを開く
            if (GetKeysDown(KeyCode.F10))
            {
                UtilsOutputLog.OpenDirectory(System.Environment.CurrentDirectory);
            }
            /*if (GetKeysDown(KeyCode.T, KeyCode.B) && !Main.TaskBattleOptionv)
            {
                Main.TaskBattleOptionv = true; //隠しゲームモード 気づけた方おめ！ 全然使っていいよ！いつか普通にできるようにするから、いまのうちに友達に自慢しｙ((((
            }*/

            //--以下ホスト専用コマンド--//
            if (!AmongUsClient.Instance.AmHost) return;
            //廃村
            if (GetKeysDown(KeyCode.Return, KeyCode.L, KeyCode.LeftShift))
            {
                if (Main.ForcedGameEndColl != 0 && !GameStates.IsLobby)
                {
                    GameManager.Instance.enabled = false;
                    CustomWinnerHolder.WinnerTeam = CustomWinner.Draw;
                    GameManager.Instance.RpcEndGame(GameOverReason.ImpostorDisconnect, false);
                    return;
                }
                if (GameStates.IsInGame)
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Draw);
                    GameManager.Instance.LogicFlow.CheckEndCriteria();
                }
                if (!GameStates.IsLobby) Main.ForcedGameEndColl++;
                Logger.Info($"廃村コール{Main.ForcedGameEndColl}回目", "fe");
            }
            //ミーティングを強制終了
            if (GetKeysDown(KeyCode.Return, KeyCode.M, KeyCode.LeftShift) && GameStates.IsMeeting)
            {
                Main.CanUseAbility = false;

                var Dummy = new Dictionary<byte, int>();
                AntiBlackout.SetRole();
                AntiBlackout.voteresult = null;
                MeetingVoteManager.Voteresult = Translator.GetString("voteskip") + "※Host";
                UtilsGameLog.AddGameLog("Vote", Translator.GetString("voteskip") + "※Host");
                GameStates.CalledMeeting = false;
                ExileControllerWrapUpPatch.AntiBlackout_LastExiled = null;
                MeetingHud.Instance.RpcClose();
                GameStates.ExiledAnimate = true;
            }
            //ミーティングを終了
            if (GetKeysDown(KeyCode.Return, KeyCode.N, KeyCode.LeftShift) && GameStates.IsMeeting)
            {
                try
                {
                    MeetingVoteManager.Instance.EndMeeting(true);
                }
                catch
                {
                    try
                    {
                        var ex = MeetingVoteManager.Instance.CountVotes(true, false);
                        Logger.seeingame($"本来の追放者:{ex.Exiled?.GetLogPlayerName() ?? $"{(ex.IsTie ? "同数" : "スキップ")}"}");
                    }
                    catch
                    {
                        Logger.seeingame("集計でエラーが...!");
                    }
                    Logger.seeingame("なんかエラーが起こってるよ！");
                }
            }
            //即スタート
            if (Input.GetKeyDown(KeyCode.LeftShift) && GameStates.IsCountDown)
            {
                if (!PlayerCatch.AllPlayerControls.Where(p => p.Data.DefaultOutfit.ColorId < 0 || Palette.PlayerColors.Length <= p.Data.DefaultOutfit.ColorId).Any())
                {
                    Logger.Info("CountDownTimer set to 0", "KeyCommand");
                    GameStartManager.Instance.countDownTimer = 0;
                }
                else
                {
                    //ホスト以外開始判定になるのを防ぐ
                    _ = new LateTask(() =>
                    {
                        GameStartManager.Instance.countDownTimer = 0;
                    }, 0.5f, "CountDownTimer set to 0");
                }
            }
            //カウントダウンキャンセル
            if (Input.GetKeyDown(KeyCode.C) && GameStates.IsCountDown)
            {
                Logger.Info("Reset CountDownTimer", "KeyCommand");
                GameStartManager.Instance.ResetStartState();
            }
            //現在の有効な設定の説明を表示
            if (GetKeysDown(KeyCode.N, KeyCode.LeftShift, KeyCode.LeftControl))
            {
                UtilsShowOption.ShowActiveSettingsHelp();
            }
            //現在の有効な設定を表示
            if (GetKeysDown(KeyCode.N, KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftShift))
            {
                UtilsShowOption.ShowActiveSettings();
            }
            //キルフラッシュ
            if (GetKeysDown(KeyCode.K, KeyCode.L, KeyCode.LeftControl) && GameStates.InGame)
            {
                Utils.AllPlayerKillFlash();
            }
            //TOH-Kオプションをデフォルトに設定
            if (GetKeysDown(KeyCode.Delete, KeyCode.LeftControl))
            {
                OptionItem.AllOptions.ToArray().Where(x => x.Id > 0).Do(x => x.SetValue(x.DefaultValue));
            }
            //自分自身の死体をレポート
            if (GetKeysDown(KeyCode.Return, KeyCode.M, KeyCode.RightShift) && GameStates.IsInGame && ((!GameStates.CalledMeeting && !GameStates.Intro) || DebugModeManager.IsDebugMode))
            {
                ReportDeadBodyPatch.ExReportDeadBody(PlayerControl.LocalPlayer, PlayerControl.LocalPlayer.Data, false, "MI.force", Main.ModColor);
            }
            if (GameStates.IsLobby && !GameStates.InGame)
            {
                if (GameSettingMenu.Instance && (GameSettingMenuStartPatch.search?.gameObject?.active ?? false))
                {
                    if (GetKeysDown(KeyCode.Return))
                    {
                        if (GameSettingMenuStartPatch.search?.textArea?.text is not "")
                        {
                            GameSettingMenuStartPatch.search?.submitButton?.OnPressed?.Invoke();
                        }
                        else
                        if (GameSettingMenuStartPatch.priset?.textArea?.text is not "")
                        {
                            var pr = OptionItem.AllOptions.Where(op => op.Id == 0).FirstOrDefault();
                            switch (pr.CurrentValue)
                            {
                                case 0: Main.Preset1.Value = GameSettingMenuStartPatch.priset.textArea.text; break;
                                case 1: Main.Preset2.Value = GameSettingMenuStartPatch.priset.textArea.text; break;
                                case 2: Main.Preset3.Value = GameSettingMenuStartPatch.priset.textArea.text; break;
                                case 3: Main.Preset4.Value = GameSettingMenuStartPatch.priset.textArea.text; break;
                                case 4: Main.Preset5.Value = GameSettingMenuStartPatch.priset.textArea.text; break;
                                case 5: Main.Preset6.Value = GameSettingMenuStartPatch.priset.textArea.text; break;
                                case 6: Main.Preset7.Value = GameSettingMenuStartPatch.priset.textArea.text; break;
                            }
                            GameSettingMenuStartPatch.priset.textArea.Clear();
                        }
                    }
                }
            }
            //--以下デバッグモード用コマンド--//
            if (!DebugModeManager.IsDebugMode) return;

            //設定の同期
            if (Input.GetKeyDown(KeyCode.Y))
            {
                RPC.SyncCustomSettingsRPC();
            }
            //投票をクリア
            if (Input.GetKeyDown(KeyCode.V) && GameStates.IsMeeting && !GameStates.IsOnlineGame)
            {
                MeetingHud.Instance.RpcClearVote(AmongUsClient.Instance.ClientId);
            }
            //自分自身を追放
            if (GetKeysDown(KeyCode.Return, KeyCode.E, KeyCode.LeftShift) && GameStates.IsInGame && PlayerControl.LocalPlayer.IsAlive())
            {
                PlayerControl.LocalPlayer.RpcExile();
                PlayerControl.LocalPlayer.Data.IsDead = true;
                PlayerControl.LocalPlayer.RpcExileV2();
                var state = PlayerState.GetByPlayerId(PlayerControl.LocalPlayer.PlayerId);
                state.DeathReason = CustomDeathReason.etc;
                state.SetDead();
            }
            //ログをゲーム内にも出力するかトグル
            if (GetKeysDown(KeyCode.F2, KeyCode.LeftControl))
            {
                Logger.isAlsoInGame = !Logger.isAlsoInGame;
                Logger.seeingame($"ログのゲーム内出力: {Logger.isAlsoInGame}");
            }
            if (Input.GetKeyDown(KeyCode.R) && GameStates.IsCountDown && DebugModeManager.EnableTOHkDebugMode.GetBool())
            {
                Logger.Info("Impostor set to 0", "KeyCommand");
                Main.NormalOptions.NumImpostors = 0;
            }

            //現在の座標を取得
            if (GetKeysDown(KeyCode.I, KeyCode.LeftShift))
            {
                Logger.seeingame(PlayerControl.LocalPlayer.GetTruePosition().ToString());
                Logger.Info(PlayerControl.LocalPlayer.GetTruePosition().ToString(), "GetLocalPlayerPos");
            }

            //--以下フリープレイ用コマンド--//
            if (!GameStates.IsFreePlay || Main.EditMode) return;

            //キルクールを0秒に設定
            if (Input.GetKeyDown(KeyCode.X))
            {
                PlayerControl.LocalPlayer.Data.Object.SetKillTimer(0f);
            }
            //自身のタスクをすべて完了
            if (Input.GetKeyDown(KeyCode.O))
            {
                foreach (var task in PlayerControl.LocalPlayer.myTasks)
                    PlayerControl.LocalPlayer.RpcCompleteTask(task.Id);
            }
            //イントロテスト
            if (Input.GetKeyDown(KeyCode.G))
            {
                HudManager.Instance.StartCoroutine(HudManager.Instance.CoFadeFullScreen(Color.clear, Color.black));
                HudManager.Instance.StartCoroutine(DestroyableSingleton<HudManager>.Instance.CoShowIntro());
            }
            //エアシップのトイレのドアを全て開ける
            if (Input.GetKeyDown(KeyCode.P))
            {
                ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, 79);
                ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, 80);
                ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, 81);
                ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, 82);
            }
            //マスゲーム用コード
            /*if (Input.GetKeyDown(KeyCode.C))
            {
                foreach(var pc in PlayerControl.AllPlayerControls) {
                    if(!pc.AmOwner) pc.MyPhysics.RpcEnterVent(2);
                }
            }
            if (Input.GetKeyDown(KeyCode.V))
            {
                Vector2 pos = PlayerControl.LocalPlayer.NetTransform.transform.position;
                foreach(var pc in PlayerControl.AllPlayerControls) {
                    if(!pc.AmOwner) {
                        pc.NetTransform.RpcSnapToForced(pos);
                        pos.x += 0.5f;
                    }
                }
            }
            if (Input.GetKeyDown(KeyCode.B))
            {
                foreach(var pc in PlayerControl.AllPlayerControls) {
                    if(!pc.AmOwner) pc.MyPhysics.RpcExitVent(2);
                }
            }
            if (Input.GetKeyDown(KeyCode.N))
            {
                VentilationSystem.Update(VentilationSystem.Operation.StartCleaning, 0);
            }*/
            //マスゲーム用コード終わり
        }
        static bool GetKeysDown(params KeyCode[] keys)
        {
            if (keys.Any(k => Input.GetKeyDown(k)) && keys.All(k => Input.GetKey(k)))
            {
                Logger.Info($"KeyDown:{keys.Where(k => Input.GetKeyDown(k)).First()} in [{string.Join(",", keys)}]", "GetKeysDown");
                return true;
            }
            return false;
        }
        static bool ORGetKeysDown(params KeyCode[] keys) => keys.Any(k => Input.GetKeyDown(k));
    }

    [HarmonyPatch(typeof(ConsoleJoystick), nameof(ConsoleJoystick.HandleHUD))]
    class ConsoleJoystickHandleHUDPatch
    {
        public static void Postfix()
        {
            HandleHUDPatch.Postfix(ConsoleJoystick.player);
        }
    }
    [HarmonyPatch(typeof(KeyboardJoystick), nameof(KeyboardJoystick.HandleHud))]
    class KeyboardJoystickHandleHUDPatch
    {
        public static void Postfix()
        {
            HandleHUDPatch.Postfix(KeyboardJoystick.player);
        }
    }
    class HandleHUDPatch
    {
        public static void Postfix(Rewired.Player player)
        {
            if (GameStates.IsLobby) return;

            if (player.GetButtonDown(8) && // 8:キルボタンのactionId
            PlayerControl.LocalPlayer.Data?.Role?.IsImpostor == false &&
            PlayerControl.LocalPlayer.CanUseKillButton())
            {
                DestroyableSingleton<HudManager>.Instance.KillButton.DoClick();
            }
            if (player.GetButtonDown(50) && // 50:インポスターのベントボタンのactionId
            PlayerControl.LocalPlayer.Data?.Role?.IsImpostor == false &&
            PlayerControl.LocalPlayer.CanUseImpostorVentButton())
            {
                try { DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.DoClick(); }
                catch { }
            }
        }
    }
}