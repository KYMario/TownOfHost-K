using System;
using HarmonyLib;
using UnityEngine;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Translator;
using TownOfHost.Roles;
using TownOfHost.Roles.AddOns.Common;
using System.Text;
using TownOfHost.Roles.Impostor;
using AmongUs.GameOptions;
using TownOfHost.Roles.Neutral;

namespace TownOfHost
{
    public static class CustomButton
    {
        public static Sprite Get(string name) => Utils.LoadSprite($"TownOfHost.Resources.Button.{name}.png", 115f);
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    class HudManagerPatch
    {
        public static int NowCallNotifyRolesCount = 0;
        public static int LastSetNameDesyncCount = 0;
        public static float TaskBattleTimer = 0.0f;
        public static Vector2 TaskBattlep;
        public static TMPro.TextMeshPro LowerInfoText;
        public static TMPro.TextMeshPro GameSettings;
        private static readonly int Desat = Shader.PropertyToID("_Desat");
        public static void Postfix(HudManager __instance)
        {
            if (!GameStates.IsModHost) return;
            var player = PlayerControl.LocalPlayer;
            if (player == null) return;
            var TaskTextPrefix = "";
            //壁抜け
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                if ((!AmongUsClient.Instance.IsGameStarted || !GameStates.IsOnlineGame || !Main.EditMode)
                    && Options.CurrentGameMode != CustomGameMode.TaskBattle
                    && player.CanMove)
                {
                    player.Collider.offset = new Vector2(0f, 127f);
                }
            }
            //壁抜け解除
            if (player.Collider.offset.y == 127f)
            {
                if (!Input.GetKey(KeyCode.LeftControl) || (AmongUsClient.Instance.IsGameStarted && GameStates.IsOnlineGame) || Main.EditMode)
                {
                    player.Collider.offset = new Vector2(0f, -0.3636f);
                }
            }
            if (Main.DebugChatopen.Value && DebugModeManager.EnableDebugMode.GetBool())
                if (__instance.Chat)
                {
                    if (!__instance.Chat?.gameObject?.active ?? false)
                    {
                        __instance.Chat?.gameObject?.SetActive(true);
                    }
                }

            if (GameStates.IsLobby && !GameStates.IsFreePlay && !Main.EditMode)
            {
                if (!GameSettings)
                {
                    GameSettings = Templates.TMPTemplate.Create("GameSettings");
                    if (DestroyableSingleton<HudManager>.Instance?.TaskPanel?.taskText?.font != null) GameSettings.font = DestroyableSingleton<HudManager>.Instance?.TaskPanel?.taskText?.font;
                    GameSettings.alignment = TMPro.TextAlignmentOptions.TopLeft;
                    GameSettings.transform.SetParent(__instance.roomTracker.transform.parent);
                    GameSettings.transform.localPosition = new(-3.325f, 2.78f);
                }

                GameSettings.text = OptionShower.GetText();
                GameSettings.SetOutlineColor(Color.black);
                GameSettings.SetOutlineThickness(0.13f);
                GameSettings.fontSizeMin =
                GameSettings.fontSizeMax = (TranslationController.Instance.currentLanguage.languageID == SupportedLangs.Japanese || Main.ForceJapanese.Value) ? 1.05f : 1.2f;
            }
            if (GameSettings)
                GameSettings.gameObject.SetActive(GameStates.IsLobby && Main.ShowGameSettingsTMP.Value);

            //カスタムスポーン位置設定中ならキルボタン等を非表示にする
            if (GameStates.IsFreePlay && Main.EditMode)
            {
                GameObject.Find("Main Camera/ShadowQuad")?.SetActive(false);
                __instance.ReportButton.Hide();
                __instance.ImpostorVentButton.Hide();
                __instance.KillButton.Hide();
                __instance.SabotageButton.Hide();
                __instance.AbilityButton.Show();
                __instance.AbilityButton.OverrideText(GetString("EditCSp"));
            }

            //ゲーム中でなければ以下は実行されない
            if (!AmongUsClient.Instance.IsGameStarted) return;

            Utils.CountAlivePlayers();

            if (SetHudActivePatch.IsActive)
            {
                if (player.IsAlive())
                {
                    __instance.AdminButton.Hide();
                    var roleClass = player.GetRoleClass();
                    if (Main.CustomSprite.Value)
                    {
                        if (roleClass != null)
                        {
                            if (Amnesia.CheckAbility(player))
                            {
                                var killLabel = (roleClass as IKiller)?.OverrideKillButtonText(out string text) == true ? text : GetString(StringNames.KillLabel);
                                __instance.KillButton.OverrideText(killLabel);
                            }
                            if (roleClass.HasAbility)
                            {
                                if (Amnesia.CheckAbility(player))
                                {
                                    __instance.AbilityButton.OverrideText(roleClass.GetAbilityButtonText());
                                    __instance.AbilityButton.ToggleVisible(roleClass.CanUseAbilityButton() && GameStates.IsInTask);
                                }
                            }
                        }
                    }

                    //バウンティハンターのターゲットテキスト
                    if (LowerInfoText == null)
                    {
                        LowerInfoText = UnityEngine.Object.Instantiate(__instance.TaskPanel.taskText);
                        LowerInfoText.transform.parent = __instance.transform;
                        LowerInfoText.transform.localPosition = new Vector3(0, -2f, 0);
                        LowerInfoText.alignment = TMPro.TextAlignmentOptions.Center;
                        LowerInfoText.overflowMode = TMPro.TextOverflowModes.Overflow;
                        LowerInfoText.enableWordWrapping = false;
                        LowerInfoText.color = Palette.EnabledColor;
                        LowerInfoText.fontSizeMin = 2.0f;
                        LowerInfoText.fontSizeMax = 2.0f;
                    }

                    LowerInfoText.text = roleClass?.GetLowerText(player, isForMeeting: GameStates.Meeting, isForHud: true) ?? "";
                    if (player.Is(CustomRoles.Amnesia)) LowerInfoText.text = "";
                    if (player.GetRoleClass()?.Jikaku() != CustomRoles.NotAssigned && player.GetRoleClass() != null) LowerInfoText.text = "";

                    LowerInfoText.enabled = LowerInfoText.text != "";

                    if (Main.RTAMode && GameStates.IsInTask)
                    {
                        LowerInfoText.enabled = true;
                        LowerInfoText.text = GetTaskBattleTimer();
                    }
                    if (!GameStates.IsInTask)
                        TaskBattleTimer = 0f;

                    if (!AmongUsClient.Instance.IsGameStarted && AmongUsClient.Instance.NetworkMode != NetworkModes.FreePlay)
                    {
                        LowerInfoText.enabled = false;
                    }

                    if (player.CanUseKillButton())
                    {
                        __instance.KillButton.ToggleVisible(player.IsAlive() && GameStates.IsInTask);
                        player.Data.Role.CanUseKillButton = true;
                    }
                    else
                    {
                        __instance.KillButton.SetDisabled();
                        __instance.KillButton.ToggleVisible(false);
                    }
                    switch (player.GetCustomRole())
                    {
                        case CustomRoles.Madmate:
                        case CustomRoles.SKMadmate:
                        case CustomRoles.Jackaldoll:
                        case CustomRoles.Jester:
                            TaskTextPrefix += GetString(StringNames.FakeTasks);
                            break;
                    }

                    bool CanUseVent = player.CanUseImpostorVentButton();
                    __instance.ImpostorVentButton.ToggleVisible(CanUseVent);
                    player.Data.Role.CanVent = CanUseVent;
                }
                else
                {
                    __instance.ReportButton.Hide();
                    __instance.ImpostorVentButton.Hide();
                    __instance.KillButton.Hide();
                    __instance.AbilityButton.Show();
                    __instance.AbilityButton.OverrideText(GetString(StringNames.HauntAbilityName));
                }
            }

            if (Input.GetKeyDown(KeyCode.Y) && AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay)
            {
                __instance.ToggleMapVisible(new MapOptions()
                {
                    Mode = MapOptions.Modes.Sabotage,
                    AllowMovementWhileMapOpen = true
                });
                if (player.AmOwner)
                {
                    player.MyPhysics.inputHandler.enabled = true;
                    ConsoleJoystick.SetMode_Task();
                }
            }

            if (AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame || !Main.DebugSendAmout.Value) RepairSender.enabled = false;
            if (Input.GetKeyDown(KeyCode.RightShift) && AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame && DebugModeManager.IsDebugMode && Main.DebugSendAmout.Value)
            {
                RepairSender.enabled = !RepairSender.enabled;
                RepairSender.Reset();
            }
            if (RepairSender.enabled && AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame && Main.DebugSendAmout.Value)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0)) RepairSender.Input(0);
                if (Input.GetKeyDown(KeyCode.Alpha1)) RepairSender.Input(1);
                if (Input.GetKeyDown(KeyCode.Alpha2)) RepairSender.Input(2);
                if (Input.GetKeyDown(KeyCode.Alpha3)) RepairSender.Input(3);
                if (Input.GetKeyDown(KeyCode.Alpha4)) RepairSender.Input(4);
                if (Input.GetKeyDown(KeyCode.Alpha5)) RepairSender.Input(5);
                if (Input.GetKeyDown(KeyCode.Alpha6)) RepairSender.Input(6);
                if (Input.GetKeyDown(KeyCode.Alpha7)) RepairSender.Input(7);
                if (Input.GetKeyDown(KeyCode.Alpha8)) RepairSender.Input(8);
                if (Input.GetKeyDown(KeyCode.Alpha9)) RepairSender.Input(9);
                if (Input.GetKeyDown(KeyCode.Return)) RepairSender.InputEnter();
            }
        }
        public static string GetTaskBattleTimer()
        {
            int hours = (int)TaskBattleTimer / 3600;
            int minutes = (int)TaskBattleTimer % 3600 / 60;
            int seconds = (int)TaskBattleTimer % 60;
            int milliseconds = (int)(TaskBattleTimer % 1 * 1000);
            return hours > 0
                ? string.Format("{0:00} : {1:00} : {2:00} : {3:000}", hours, minutes, seconds, milliseconds)
                : string.Format("{0:00} : {1:00} : {2:000}", minutes, seconds, milliseconds);
        }
        //カスタムぼたーん。
        //参考→https://github.com/0xDrMoe/TownofHost-Enhanced/releases/tag/v1.5.1
        public static bool? ch;
        public static Sprite KillButton = null;
        public static Sprite ShepeButton = null;
        public static Sprite EngButton = null;
        public static Sprite HyoiButton = null;
        public static Sprite ImpVentButton = null;
        public static void BottonHud()
        {
            if (!GameStates.AfterIntro) return;
            if (!AmongUsClient.Instance.IsGameStarted) return;
            if (SetHudActivePatch.IsActive)
            {
                if (!GameStates.IsModHost) return;
                var player = PlayerControl.LocalPlayer;
                if (player == null) return;

                var roleClass = player.GetRoleClass();
                var __instance = DestroyableSingleton<HudManager>.Instance;
                if (!__instance) return;
                //定義
                if (KillButton == null && __instance.KillButton.graphic.sprite) KillButton = __instance.KillButton.graphic.sprite;
                if (ImpVentButton == null && __instance.ImpostorVentButton.graphic.sprite) ImpVentButton = __instance.ImpostorVentButton.graphic.sprite;
                if (roleClass?.HasAbility ?? false || !player.IsAlive())
                {
                    if (player.Data.Role.Ability.Image)
                    {
                        if (EngButton == null && player.Data.Role.Role is RoleTypes.Engineer) EngButton = player.Data.Role.Ability.Image;
                        if (ShepeButton == null && player.Data.Role.Role is RoleTypes.Shapeshifter) ShepeButton = player.Data.Role.Ability.Image;
                        if (HyoiButton == null && player.Data.Role.Role is RoleTypes.CrewmateGhost or RoleTypes.ImpostorGhost) HyoiButton = player.Data.Role.Ability.Image;
                    }
                }
                if (player == !GameStates.IsModHost) return;
                if (!AmongUsClient.Instance.IsGameStarted) return;
                //リセット
                if (__instance.KillButton.graphic.sprite && KillButton) __instance.KillButton.graphic.sprite = KillButton;
                if (__instance.ImpostorVentButton.graphic.sprite && ImpVentButton) __instance.ImpostorVentButton.graphic.sprite = ImpVentButton;
                if (roleClass?.HasAbility ?? false || !player.IsAlive())
                {
                    if (EngButton && player.Data.Role.Role is RoleTypes.Engineer) player.Data.Role.Ability.Image = EngButton;
                    if (ShepeButton && player.Data.Role.Role is RoleTypes.Shapeshifter) player.Data.Role.Ability.Image = ShepeButton;
                    if (HyoiButton && !player.IsAlive() && player.Data.Role.Role is RoleTypes.CrewmateGhost or RoleTypes.ImpostorGhost) player.Data.Role.Ability.Image = HyoiButton;
                }

                if (CustomRoles.Amnesia.IsPresent()) return;
                if (ch == null)
                    foreach (var pc in Main.AllPlayerControls)
                    {
                        if (pc.GetRoleClass()?.Jikaku() != CustomRoles.NotAssigned && pc.GetRoleClass() != null) ch = true;
                    }
                if (ch == true) return;
                if (player.GetCustomRole().IsVanilla()) return;
                if (roleClass == null) return;
                if (Main.CustomSprite.Value)
                {
                    if (roleClass != null)
                    {
                        if (player.IsAlive() && roleClass.HasAbility)
                            if (roleClass.OverrideAbilityButton(out string abname) == true && Main.CustomSprite.Value)
                                player.Data.Role.Ability.Image = CustomButton.Get(abname);

                        if (player.CanUseKillButton() && (roleClass as IKiller)?.OverrideKillButton(out string name) == true && Main.CustomSprite.Value)
                            __instance.KillButton.graphic.sprite = CustomButton.Get(name);

                        if ((roleClass as IKiller)?.OverrideImpVentButton(out string name2) == true && Main.CustomSprite.Value)
                            __instance.ImpostorVentButton.graphic.sprite = CustomButton.Get(name2);
                    }
                }
            }
        }
    }
    [HarmonyPatch(typeof(ShapeshifterPanel), nameof(ShapeshifterPanel.SetPlayer))]
    class ShapeShifterNamePatch
    {
        private static StringBuilder Mark = new(20);
        private static StringBuilder Suffix = new(120);
        public static void Postfix(ShapeshifterPanel __instance, [HarmonyArgument(0)] int index, [HarmonyArgument(1)] NetworkedPlayerInfo pl)
        {
            {
                var seer = PlayerControl.LocalPlayer;
                var seerRole = seer.GetRoleClass();
                var target = Utils.GetPlayerById(pl.PlayerId);

                //変数定義
                string name = "";
                bool nomarker = false;
                string RealName;
                Mark.Clear();
                Suffix.Clear();

                //名前を一時的に上書きするかのチェック
                var TemporaryName = target.GetRoleClass()?.GetTemporaryName(ref name, ref nomarker, seer, target) ?? false;

                //名前変更
                RealName = TemporaryName ? name : target.GetRealName();

                //NameColorManager準拠の処理
                RealName = RealName.ApplyNameColorData(seer, target, false);

                //seer役職が対象のMark
                if (Amnesia.CheckAbility(seer))
                    Mark.Append(seerRole?.GetMark(seer, target, false));
                //seerに関わらず発動するMark
                Mark.Append(CustomRoleManager.GetMarkOthers(seer, target, false));

                //ハートマークを付ける(会議中MOD視点)
                if (target.GetRiaju() == PlayerControl.LocalPlayer.GetRiaju() && target.GetRiaju() != CustomRoles.NotAssigned)
                {
                    Mark.Append(Utils.ColorString(Utils.GetRoleColor(target.GetRiaju()), "♥"));
                }
                else if (PlayerControl.LocalPlayer.Data.IsDead && target.IsRiaju())
                {
                    Mark.Append(Utils.ColorString(Utils.GetRoleColor(target.GetRiaju()), "♥"));
                }

                if (target.Is(CustomRoles.Connecting) && PlayerControl.LocalPlayer.Is(CustomRoles.Connecting)
                && !target.Is(CustomRoles.WolfBoy) && !PlayerControl.LocalPlayer.Is(CustomRoles.WolfBoy))
                {
                    Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Connecting)}>Ψ</color>");
                }
                else if (target.Is(CustomRoles.Connecting) && PlayerControl.LocalPlayer.Data.IsDead)
                {
                    Mark.Append($"<color={Utils.GetRoleColorCode(CustomRoles.Connecting)}>Ψ</color>");
                }
                //プログレスキラー
                if (Amnesia.CheckAbility(seer))
                {
                    if (seer.Is(CustomRoles.ProgressKiller) && target.Is(CustomRoles.Workhorse) && ProgressKiller.ProgressWorkhorseseen)
                    {
                        Mark.Append($"<color=blue>♦</color>");
                    }
                    //エーリアン
                    if (seer.Is(CustomRoles.Alien))
                    {
                        foreach (var al in Alien.Aliens)
                        {
                            if (al.Player == seer)
                                if (target.Is(CustomRoles.Workhorse) && al.modeProgresskiller && Alien.ProgressWorkhorseseen)
                                {
                                    Mark.Append($"<color=blue>♦</color>");
                                }
                        }
                    }
                }

                //seerに関わらず発動するLowerText
                Suffix.Append(CustomRoleManager.GetLowerTextOthers(seer, target));
                //seer役職が対象のSuffix
                if (Amnesia.CheckAbility(seer))
                    Suffix.Append(seerRole?.GetSuffix(seer, target));

                //seerに関わらず発動するSuffix
                Suffix.Append(CustomRoleManager.GetSuffixOthers(seer, target));

                if (Utils.IsActive(SystemTypes.Comms) && Options.CommsCamouflage.GetBool())
                    RealName = $"<size=0>{RealName}</size> ";

                string DeathReason = seer.Data.IsDead && seer.KnowDeathReason(target) ? $"({Utils.ColorString(Utils.GetRoleColor(CustomRoles.Doctor), Utils.GetVitalText(target.PlayerId, seer.PlayerId.CanDeathReasonKillerColor()))})" : "";
                //Mark・Suffixの適用
                if (!seer.GetCustomRole().GetRoleInfo()?.IsDesyncImpostor ?? false)
                    __instance.NameText.text = $"{RealName}{((TemporaryName && nomarker) ? "" : DeathReason + Mark)}";
                else
                    __instance.NameText.text = $"<color=#ffffff>{RealName}{((TemporaryName && nomarker) ? "" : DeathReason + Mark)}</color>";

                if (Suffix.ToString() != "" && (!TemporaryName || (TemporaryName && !nomarker)))
                {
                    __instance.NameText.text += "\r\n" + Suffix.ToString();
                }
            }
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ToggleHighlight))]
    class ToggleHighlightPatch
    {
        public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] bool active, [HarmonyArgument(1)] RoleTeamTypes team)
        {
            var player = PlayerControl.LocalPlayer;
            if (!GameStates.IsInTask) return;

            if (player.CanUseKillButton())
            {
                Color color = PlayerControl.LocalPlayer.GetRoleColor();
                if (PlayerControl.LocalPlayer.Is(CustomRoles.Amnesia)) color = PlayerControl.LocalPlayer.Is(CustomRoleTypes.Crewmate) ? Utils.GetRoleColor(CustomRoles.Crewmate) : (PlayerControl.LocalPlayer.Is(CustomRoleTypes.Impostor) ?
                    Utils.GetRoleColor(CustomRoles.Impostor) : Utils.GetRoleColor(CustomRoles.SchrodingerCat));
                ((Renderer)__instance.cosmetics.currentBodySprite.BodySprite).material.SetColor("_OutlineColor", color);
            }
        }
    }
    [HarmonyPatch(typeof(Vent), nameof(Vent.SetOutline))]
    class SetVentOutlinePatch
    {
        public static void Postfix(Vent __instance, [HarmonyArgument(1)] ref bool mainTarget)
        {
            var player = PlayerControl.LocalPlayer;
            Color color = PlayerControl.LocalPlayer.GetRoleColor();
            if (PlayerControl.LocalPlayer.Is(CustomRoles.Amnesia)) color = PlayerControl.LocalPlayer.Is(CustomRoleTypes.Crewmate) ? Utils.GetRoleColor(CustomRoles.Crewmate) : (PlayerControl.LocalPlayer.Is(CustomRoleTypes.Impostor) ?
                Utils.GetRoleColor(CustomRoles.Impostor) : Utils.GetRoleColor(CustomRoles.SchrodingerCat));
            if (player.GetRoleClass()?.Jikaku() != CustomRoles.NotAssigned && player.GetRoleClass() != null)
            {
                color = Utils.GetRoleColor(player.GetRoleClass().Jikaku());
            }
        ((Renderer)__instance.myRend).material.SetColor("_OutlineColor", color);
            ((Renderer)__instance.myRend).material.SetColor("_AddColor", mainTarget ? color : Color.clear);
        }
    }
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.SetHudActive), new Type[] { typeof(PlayerControl), typeof(RoleBehaviour), typeof(bool) })]
    class SetHudActivePatch
    {
        public static bool IsActive = false;
        public static void Postfix(HudManager __instance, [HarmonyArgument(2)] bool isActive)
        {
            __instance.ReportButton.ToggleVisible(!GameStates.IsLobby && isActive);
            if (!GameStates.IsModHost) return;
            IsActive = isActive;
            if (GameStates.IsLobby) return;
            if (!isActive) return;

            var player = PlayerControl.LocalPlayer;
            __instance.KillButton.ToggleVisible(player.CanUseKillButton());
            __instance.ImpostorVentButton.ToggleVisible(player.CanUseImpostorVentButton());
            __instance.SabotageButton.ToggleVisible(player.CanUseSabotageButton());
        }
    }
    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.Show))]
    class MapBehaviourShowPatch
    {
        public static bool Prefix(MapBehaviour __instance, ref MapOptions opts)
        {
            if (GameStates.IsMeeting) return true;

            if (opts.Mode == MapOptions.Modes.CountOverlay && PlayerControl.LocalPlayer.IsAlive() && MapBehaviour.Instance && __instance)
            {
                if (Options.TimeLimitAdmin.GetFloat() != 0 && DisableDevice.GameAdminTimer > Options.TimeLimitAdmin.GetFloat()) return false;
                if (Options.TarnTimeLimitAdmin.GetFloat() != 0 && DisableDevice.TarnAdminTimer > Options.TarnTimeLimitAdmin.GetFloat()) return false;
            }

            if (opts.Mode is MapOptions.Modes.Normal or MapOptions.Modes.Sabotage)
            {
                var player = PlayerControl.LocalPlayer;
                if (player.GetRoleClass() is IKiller killer && killer.CanUseSabotageButton())
                    opts.Mode = MapOptions.Modes.Sabotage;
                else
                    opts.Mode = MapOptions.Modes.Normal;
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(TaskPanelBehaviour), nameof(TaskPanelBehaviour.SetTaskText))]
    class TaskPanelBehaviourPatch
    {
        // タスク表示の文章が更新・適用された後に実行される
        public static void Postfix(TaskPanelBehaviour __instance)
        {
            if (!GameStates.IsModHost || GameStates.IsLobby) return;
            PlayerControl player = PlayerControl.LocalPlayer;
            var role = player.GetCustomRole();
            if (player.Is(CustomRoles.Amnesia)) role = player.Is(CustomRoleTypes.Crewmate) ? CustomRoles.Crewmate : CustomRoles.Impostor;
            if (player.GetRoleClass()?.Jikaku() != CustomRoles.NotAssigned && player.GetRoleClass() != null) role = player.GetRoleClass().Jikaku();
            // 役職説明表示
            if (!role.IsVanilla() || player.IsGorstRole())
            {
                var RoleWithInfo = $"{player.GetTrueRoleName()}:\r\n";
                RoleWithInfo += player.GetRoleInfo();
                __instance.taskText.text = Utils.ColorString(player.GetRoleColor(), RoleWithInfo) + "\n" + __instance.taskText.text;
            }

            // RepairSenderの表示
            if (RepairSender.enabled && AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
            {
                __instance.taskText.text = RepairSender.GetText();
            }
        }
    }
    [HarmonyPatch(typeof(FriendsListBar), nameof(FriendsListBar.Update))]
    class FriendsListBarUpdatePatch
    {
        public static void Prefix(FriendsListBar __instance)
        {
            if (!Main.HideSomeFriendCodes.Value) return;
            var FriendCodeText = GameObject.Find("FriendCodeText");
            if (FriendCodeText && FriendCodeText.active)
                FriendCodeText.SetActive(false);
        }
    }
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.CoShowIntro))]
    class HudManagerCoShowIntroPatch
    {
        public static bool Cancel = true;
        public static bool Prefix(HudManager __instance)
        {
            if (AmongUsClient.Instance.AmHost && Options.CurrentGameMode == CustomGameMode.Standard && Cancel)
            {
                Logger.Warn("イントロの表示をキャンセルしました", "CoShowIntro");
                return false;
            }
            Cancel = true;
            return true;
        }
    }
    class RepairSender
    {
        public static bool enabled = false;
        public static bool TypingAmount = false;

        public static int SystemType;
        public static int amount;

        public static void Input(int num)
        {
            if (!TypingAmount)
            {
                //SystemType入力中
                SystemType *= 10;
                SystemType += num;
            }
            else
            {
                //Amount入力中
                amount *= 10;
                amount += num;
            }
        }
        public static void InputEnter()
        {
            if (!TypingAmount)
            {
                //SystemType入力中
                TypingAmount = true;
            }
            else
            {
                //Amount入力中
                Send();
            }
        }
        public static void Send()
        {
            ShipStatus.Instance.RpcUpdateSystem((SystemTypes)SystemType, (byte)amount);
            Reset();
        }
        public static void Reset()
        {
            TypingAmount = false;
            SystemType = 0;
            amount = 0;
        }
        public static string GetText()
        {
            return SystemType.ToString() + "(" + ((SystemTypes)SystemType).ToString() + ")\r\n" + amount;
        }
    }
}