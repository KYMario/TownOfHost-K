using System;
using System.Linq;
using System.Text;
using HarmonyLib;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.AddOns.Common;
using TownOfHost.Roles.Impostor;
using TownOfHost.Roles.Neutral;
using static TownOfHost.Translator;

namespace TownOfHost
{
    public static class CustomButton
    {
        public static Sprite Get(string name) => UtilsSprite.LoadSprite($"TownOfHost.Resources.Button.{name}.png", 115f);
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    class HudManagerPatch
    {
        public static int NowCallNotifyRolesCount = 0;
        public static int LastSetNameDesyncCount = 0;
        public static float TaskBattleTimer = 0.0f;
        public static TMPro.TextMeshPro LowerInfoText;
        public static TMPro.TextMeshPro GameSettings;
        private static readonly int Desat = Shader.PropertyToID("_Desat");
        public static void Postfix(HudManager __instance)
        {
            if (!GameStates.IsModHost) return;
            var player = PlayerControl.LocalPlayer;
            if (player == null) return;
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
            GameObject.Find("Main Camera/Hud/TaskDisplay/ProgressTracker")?.SetActive(false);
#if DEBUG
            if (Main.DebugChatopen.Value && DebugModeManager.EnableDebugMode.GetBool())
                if (__instance.Chat)
                {
                    if (!__instance.Chat?.gameObject?.active ?? false)
                    {
                        __instance.Chat?.gameObject?.SetActive(true);
                    }
                }
#endif

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

                GameSettings.text = Main.ShowGameSettingsTMP.Value ? OptionShower.GetText() : "";
                GameSettings.SetOutlineColor(Color.black);
                GameSettings.SetOutlineThickness(0.13f);
                GameSettings.transform.localPosition = new(-3.325f * GameSettingMenuStartPatch.w, 2.78f);
                GameSettings.fontSizeMin =
                GameSettings.fontSizeMax = (TranslationController.Instance.currentLanguage.languageID == SupportedLangs.Japanese || Main.ForceJapanese.Value) ? 1.05f : 1.2f;

                var b = GameStates.IsLobby && !GameStates.IsCountDown && !GameStates.InGame && (GameSettingMenuStartPatch.ModSettingsButton?.selected ?? false);// && GameSettingMenuStartPatch.NowRoleTab is not CustomRoles.NotAssigned;
                GameObject.Find("Main Camera/Hud/TaskDisplay")?.SetActive(b);
                GameObject.Find("Main Camera/Hud/TaskDisplay")?.transform.SetLocalZ(b ? -500 : 5);

                if (b)
                {
                    __instance.TaskPanel.SetTaskText("");
                }
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
                return;
            }
            //ゲーム中でなければ以下は実行されない
            if (!AmongUsClient.Instance.IsGameStarted) return;
            PlayerCatch.CountAlivePlayers();

            if (SetHudActivePatch.IsActive)
            {
                if (player.IsAlive())
                {
                    __instance.AdminButton.Hide();
                    var roleClass = player.GetRoleClass();
                    if (roleClass is not null)
                    {
                        if (roleClass.HasAbility)
                        {
                            bool Visible = roleClass.CanUseAbilityButton() && GameStates.IsInTask;
                            if ((roleClass as IUsePhantomButton)?.IsPhantomRole is false) Visible = false;
                            __instance.AbilityButton.ToggleVisible(Visible);
                        }
                    }
                    if (Main.CustomSprite.Value && CustomButtonHud.CantJikakuIsPresent is not true)
                    {
                        if (roleClass != null)
                        {
                            if (Amnesia.CheckAbility(player))
                            {
                                var killLabel = (roleClass as IKiller)?.OverrideKillButtonText(out string text) == true ? text : GetString(StringNames.KillLabel);
                                __instance.KillButton.OverrideText(killLabel);

                                if (roleClass.HasAbility)
                                {
                                    __instance.AbilityButton.OverrideText(roleClass.GetAbilityButtonText());
                                    if (roleClass.AllEnabledColor)
                                    {
                                        __instance.AbilityButton.graphic.color = __instance.AbilityButton.buttonLabelText.color = Palette.EnabledColor;
                                        __instance.AbilityButton.graphic.material.SetFloat(Desat, 0f);
                                    }
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

                    LowerInfoText.text = roleClass?.GetLowerText(player, isForMeeting: GameStates.IsMeeting, isForHud: true) ?? "";
                    if (player.Is(CustomRoles.Amnesia)) LowerInfoText.text = "";
                    if (roleClass?.Jikaku() != CustomRoles.NotAssigned) LowerInfoText.text = "";

#if DEBUG
                    if (Main.ShowDistance.Value)
                    {
                        foreach (var pc in PlayerCatch.AllPlayerControls.Where(pc => pc.PlayerId != 0))
                            LowerInfoText.text += Utils.ColorString(Palette.PlayerColors[pc.cosmetics.ColorId], $"{Vector2.Distance(PlayerControl.LocalPlayer.transform.position, pc.transform.position)}");
                    }
#endif

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
                        __instance.KillButton.ToggleVisible(/*player.IsAlive() && */GameStates.IsInTask);
                        player.Data.Role.CanUseKillButton = true;
                    }
                    else
                    {
                        __instance.KillButton.SetDisabled();
                        __instance.KillButton.ToggleVisible(false);
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
                    if (GameStates.IsMeeting)
                    {
                        __instance.AbilityButton.Hide();
                    }
                    else
                    {
                        __instance.AbilityButton.Show();
                        __instance.AbilityButton.OverrideText(GetString(StringNames.HauntAbilityName));
                    }
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

            if (AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame && Main.DebugSendAmout.Value)
            {
                if (Input.GetKeyDown(KeyCode.RightShift) && DebugModeManager.IsDebugMode)
                {
                    RepairSender.enabled = !RepairSender.enabled;
                    RepairSender.Reset();
                }
                if (RepairSender.enabled)
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
            else RepairSender.enabled = false;
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
    }
    class CustomButtonHud
    {
        //カスタムぼたーん。
        public static bool? CantJikakuIsPresent;
        public static Sprite MotoKillButton = null;
        public static Sprite ImpVentButton = null;
        public static Sprite ShapeShiftButton = null;
        public static Sprite EngButton = null;
        public static Sprite PhantomButton = null;
        static bool? OldValue = null;
        public static void BottonHud(bool reset = false)
        {
            OldValue ??= Main.CustomSprite.Value;
            if (!AmongUsClient.Instance) return;
            if (!AmongUsClient.Instance.IsGameStarted) return;
            //if (!GameStates.AfterIntro) return;
            try
            {
                if (SetHudActivePatch.IsActive)
                {
                    if (!GameStates.IsModHost) return;
                    var player = PlayerControl.LocalPlayer;
                    if (player == null) return;
                    var roleClass = player.GetRoleClass();
                    var __instance = DestroyableSingleton<HudManager>.Instance;
                    var customrole = player.GetCustomRole();
                    var isalive = player.IsAlive();
                    if (!__instance) return;
                    if (roleClass.HasAbility) player.Data.Role.InitializeAbilityButton();
                    //定義
                    if (MotoKillButton == null && __instance.KillButton.graphic.sprite) MotoKillButton = __instance.KillButton.graphic.sprite;
                    if (ImpVentButton == null && __instance.ImpostorVentButton.graphic.sprite) ImpVentButton = __instance.ImpostorVentButton.graphic.sprite;

                    if (player == !GameStates.IsModHost) return;
                    //リセット
                    if (__instance.KillButton.graphic.sprite && MotoKillButton) __instance.KillButton.graphic.sprite = MotoKillButton;
                    if (__instance.ImpostorVentButton.graphic.sprite && ImpVentButton) __instance.ImpostorVentButton.graphic.sprite = ImpVentButton;
                    if (roleClass.HasAbility)
                    {
                        Sprite abilitybutton = null;
                        switch (player.Data.Role.Role)
                        {
                            case RoleTypes.Engineer:
                                if (EngButton == null)
                                    EngButton = player.Data.Role.Ability.Image;
                                abilitybutton = EngButton;
                                break;
                            case RoleTypes.Shapeshifter:
                                if (ShapeShiftButton == null)
                                    ShapeShiftButton = player.Data.Role.Ability.Image;
                                abilitybutton = ShapeShiftButton;
                                break;
                            case RoleTypes.Phantom:
                                if (PhantomButton == null)
                                    PhantomButton = player.Data.Role.Ability.Image;
                                abilitybutton = PhantomButton;
                                break;
                            default:
                                abilitybutton = null;
                                break;
                        }
                        if (abilitybutton is not null)
                        {
                            player.Data.Role.Ability.Image = abilitybutton;
                            player.Data.Role.InitializeAbilityButton();
                        }
                    }
                    if (CustomRoles.Amnesia.IsPresent()) return;
                    if (CantJikakuIsPresent == null)
                        foreach (var pc in PlayerCatch.AllPlayerControls)
                        {
                            if (pc.GetRoleClass()?.Jikaku() is not null and not CustomRoles.NotAssigned and not CustomRoles.Crewmate) CantJikakuIsPresent = true;
                        }
                    if (CantJikakuIsPresent == true) return;
                    if (customrole.IsVanilla()) return;
                    if (roleClass == null) return;
                    if (Main.CustomSprite.Value)
                    {
                        if (roleClass != null)
                        {
                            if (isalive)
                            {
                                if (roleClass.OverrideAbilityButton(out string abname) == true && Main.CustomSprite.Value)
                                {
                                    player.Data.Role.Ability.Image = CustomButton.Get(abname);

                                    player.Data.Role.Ability.name = "ModRoleAbilityButton";
                                    player.Data.Role.InitializeAbilityButton();
                                    if (reset && OldValue == Main.CustomSprite.Value)
                                    {
                                        var role = customrole.GetRoleTypes();
                                        if (customrole.GetRoleInfo().IsDesyncImpostor && role is RoleTypes.Impostor) role = RoleTypes.Crewmate;
                                        RoleManager.Instance.SetRole(PlayerControl.LocalPlayer, role);
                                        player.RpcResetAbilityCooldown();
                                    }
                                }
                            }

                            // Memo
                            // キルボタン非表示してから当てれば大丈夫
                            // キルボタンがバグってもなんかスイッチシェリフのキルボだけ大丈夫なことが多い。
                            if ((roleClass as IKiller)?.OverrideKillButton(out string name) == true && Main.CustomSprite.Value)
                            {
                                __instance.KillButton.graphic.sprite = CustomButton.Get(name);
                            }
                            else if (MotoKillButton)
                            {
                                __instance.KillButton.graphic.sprite = MotoKillButton;
                            }

                            if ((roleClass as IKiller)?.OverrideImpVentButton(out string name2) == true && Main.CustomSprite.Value)
                            {
                                __instance.ImpostorVentButton.graphic.sprite = CustomButton.Get(name2);
                            }
                            else if (ImpVentButton)
                            {
                                __instance.ImpostorVentButton.graphic.sprite = ImpVentButton;
                            }
                        }
                    }
                    OldValue = Main.CustomSprite.Value;//アビリティボタンのあれのせいでクールがあれなのでリセット入る時だけしっかり反映させる。
                }
            }
            catch (Exception ec) { Logger.Error($"{ec}", "ButtonHud"); }
        }
    }
    [HarmonyPatch(typeof(ShapeshifterPanel), nameof(ShapeshifterPanel.SetPlayer))]
    class ShapeShifterNamePatch
    {
        private static StringBuilder Mark = new(20);
        private static StringBuilder Suffix = new(120);
        public static void Postfix(ShapeshifterPanel __instance, [HarmonyArgument(0)] int index, [HarmonyArgument(1)] NetworkedPlayerInfo pl)
        {
            if (Main.EditMode && GameStates.IsFreePlay) return;

            var seer = PlayerControl.LocalPlayer;
            var seerRole = seer.GetRoleClass();
            var target = PlayerCatch.GetPlayerById(pl.PlayerId);

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

            var targetlover = target.GetRiaju();
            //ハートマークを付ける(会議中MOD視点)
            if ((targetlover == seer.GetRiaju() && targetlover is not CustomRoles.OneLove and not CustomRoles.NotAssigned)
            || (seer.Data.IsDead && target.IsRiaju() && targetlover != CustomRoles.OneLove))
            {
                Mark.Append(Utils.ColorString(UtilsRoleText.GetRoleColor(targetlover), "♥"));
            }
            else
            if ((Lovers.OneLovePlayer.Ltarget == target.PlayerId && target.PlayerId != seer.PlayerId && seer.Is(CustomRoles.OneLove))
            || (target.Is(CustomRoles.OneLove) && target.PlayerId != seer.PlayerId && seer.Is(CustomRoles.OneLove))
            || (seer.Data.IsDead && target.Is(CustomRoles.OneLove) && !seer.Is(CustomRoles.OneLove)))
            {
                Mark.Append(Utils.ColorString(UtilsRoleText.GetRoleColor(CustomRoles.OneLove), "♡"));
            }

            if (target.Is(CustomRoles.Connecting) && PlayerControl.LocalPlayer.Is(CustomRoles.Connecting)
            && !target.Is(CustomRoles.WolfBoy) && !PlayerControl.LocalPlayer.Is(CustomRoles.WolfBoy))
            {
                Mark.Append($"<{UtilsRoleText.GetRoleColorCode(CustomRoles.Connecting)}>Ψ</color>");
            }
            else if (target.Is(CustomRoles.Connecting) && PlayerControl.LocalPlayer.Data.IsDead)
            {
                Mark.Append($"<{UtilsRoleText.GetRoleColorCode(CustomRoles.Connecting)}>Ψ</color>");
            }
            //seerに関わらず発動するLowerText
            Suffix.Append(CustomRoleManager.GetLowerTextOthers(seer, target));
            if (Amnesia.CheckAbility(seer))
            {
                //プログレスキラー
                if (seer.Is(CustomRoles.ProgressKiller) && target.Is(CustomRoles.Workhorse) && ProgressKiller.ProgressWorkhorseseen)
                {
                    Mark.Append($"<color=blue>♦</color>");
                }
                //エーリアン
                if ((seerRole as Alien)?.modeProgresskiller == true && Alien.ProgressWorkhorseseen)
                    if (target.Is(CustomRoles.Workhorse))
                    {
                        Mark.Append($"<color=blue>♦</color>");
                    }
                if ((seerRole as JackalAlien)?.modeProgresskiller == true && JackalAlien.ProgressWorkhorseseen)
                    if (target.Is(CustomRoles.Workhorse))
                    {
                        Mark.Append($"<color=blue>♦</color>");
                    }
                if ((seerRole as AlienHijack)?.modeProgresskiller == true && Alien.ProgressWorkhorseseen)
                    if (target.Is(CustomRoles.Workhorse))
                    {
                        Mark.Append($"<color=blue>♦</color>");
                    }
                //seer役職が対象のSuffix
                Suffix.Append(seerRole?.GetSuffix(seer, target));
            }

            //seerに関わらず発動するSuffix
            Suffix.Append(CustomRoleManager.GetSuffixOthers(seer, target));

            if (Utils.IsActive(SystemTypes.Comms) && Options.CommsCamouflage.GetBool())
                RealName = $"<size=0>{RealName}</size> ";

            bool? canseedeathreasoncolor = seer.PlayerId.CanDeathReasonKillerColor() == true ? true : null;
            string DeathReason = seer.Data.IsDead && seer.KnowDeathReason(target) ? $"<size=75%>({Utils.GetVitalText(target.PlayerId, canseedeathreasoncolor)})</size>" : "";//Mark・Suffixの適用
            if (!seer.GetCustomRole().GetRoleInfo()?.IsDesyncImpostor ?? false)
                __instance.NameText.text = $"{RealName}{((TemporaryName && nomarker) ? "" : DeathReason + Mark)}";
            else
                __instance.NameText.text = $"<#ffffff>{RealName}{((TemporaryName && nomarker) ? "" : DeathReason + Mark)}</color>";

            if (Suffix.ToString() != "" && (!TemporaryName || (TemporaryName && !nomarker)))
            {
                __instance.NameText.text += "\r\n" + Suffix.ToString();
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
                if (PlayerControl.LocalPlayer.Is(CustomRoles.Amnesia)) color = PlayerControl.LocalPlayer.Is(CustomRoleTypes.Crewmate) ? UtilsRoleText.GetRoleColor(CustomRoles.Crewmate) : (PlayerControl.LocalPlayer.Is(CustomRoleTypes.Impostor) ?
                    UtilsRoleText.GetRoleColor(CustomRoles.Impostor) : UtilsRoleText.GetRoleColor(CustomRoles.SchrodingerCat));
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
            var roleclass = player.GetRoleClass();
            Color color = PlayerControl.LocalPlayer.GetRoleColor();
            if (PlayerControl.LocalPlayer.Is(CustomRoles.Amnesia)) color = PlayerControl.LocalPlayer.Is(CustomRoleTypes.Crewmate) ? UtilsRoleText.GetRoleColor(CustomRoles.Crewmate) : (PlayerControl.LocalPlayer.Is(CustomRoleTypes.Impostor) ?
                UtilsRoleText.GetRoleColor(CustomRoles.Impostor) : UtilsRoleText.GetRoleColor(CustomRoles.SchrodingerCat));
            if (roleclass?.Jikaku() != CustomRoles.NotAssigned && roleclass != null)
            {
                color = UtilsRoleText.GetRoleColor(roleclass.Jikaku());
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
                if (DisableDevice.optTimeLimitAdmin != 0 && DisableDevice.GameAdminTimer > DisableDevice.optTimeLimitAdmin) return false;
                if (DisableDevice.optTimeLimitAdmin != 0 && DisableDevice.TurnAdminTimer > DisableDevice.optTimeLimitAdmin) return false;
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
            if (GameStates.IsLobby && (GameSettingMenuStartPatch.NowRoleTab is not CustomRoles.NotAssigned || GameSettingMenuStartPatch.Nowinfo is not CustomRoles.NotAssigned))
            {
                var text = "";
                var desc = "";
                var inforole = GameSettingMenuStartPatch.NowRoleTab is CustomRoles.NotAssigned ? GameSettingMenuStartPatch.Nowinfo : GameSettingMenuStartPatch.NowRoleTab;
                var inforoleinfo = inforole.GetRoleInfo();

                text = $"<size=150%><{UtilsRoleText.GetRoleColorCode(inforole)}>{UtilsRoleText.GetRoleName(inforole)}\n</size>";

                if (inforoleinfo?.Desc is not null)
                {
                    text += $"<size=100%>{GetString($"{inforole}Info")}" + "\n\n</size>";
                    desc += $"<size=70%>{inforoleinfo?.Desc()}";
                }
                else
                if (inforole.IsVanilla() && inforole is not CustomRoles.GuardianAngel)
                {
                    text += $"<size=100%>{inforoleinfo.Description.Blurb}" + "\n\n</size>";
                    desc += $"<size=70%>{inforoleinfo.Description.Description}";
                }
                else
                {
                    text += "<size=100%>" + GetString($"{inforole}Info") + "\n\n</size>";
                    desc += "<size=70%>" + GetString($"{inforole}InfoLong");
                }
                desc = desc.RemoveDeltext("、", "、\n");
                desc = desc.RemoveDeltext("。", "。\n");

                __instance.taskText.text = $"{text}</color>{desc}";
                __instance.background.color = new Color32(15, 15, 15, 220);
                return;
            }
            if (!GameStates.IsModHost || GameStates.IsLobby) return;
            __instance.background.color = new Color(0.5188679f, 0.5188679f, 0.5188679f, 0.5176471f);
            PlayerControl player = PlayerControl.LocalPlayer;
            var role = player.GetCustomRole();
            var roleClass = player.GetRoleClass();
            if (player.Is(CustomRoles.Amnesia)) role = player.Is(CustomRoleTypes.Crewmate) ? CustomRoles.Crewmate : CustomRoles.Impostor;
            if (roleClass?.Jikaku() != CustomRoles.NotAssigned && roleClass != null) role = roleClass.Jikaku();

            if (role is CustomRoles.Amnesiac)
            {
                if (roleClass is Amnesiac amnesiac && !amnesiac.omoidasita)
                    role = Amnesiac.iamwolf ? CustomRoles.WolfBoy : CustomRoles.Sheriff;
            }
            // 役職説明表示
            if (!role.IsVanilla() || player.IsGhostRole())
            {
                var RoleWithInfo = $"{UtilsRoleText.GetTrueRoleName(player.PlayerId)}:\r\n";
                RoleWithInfo += player.GetRoleInfo();
                var task = __instance.taskText.text;
                if (role.IsCrewmate())
                {
                    task = task.RemoveDeltext(GetString(StringNames.FakeTasks));
                    task = task.RemoveDeltext(GetString(StringNames.ImpostorTask));
                    task = task.RemoveDeltext("<#FF0000FF>\r\n<#FF1919FF></color></color>\r\n");
                }
                __instance.taskText.text = Utils.ColorString(player.GetRoleColor(), RoleWithInfo) + "\n" + task;
            }

            // RepairSenderの表示
            if (RepairSender.enabled && AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
            {
                __instance.taskText.text = RepairSender.GetText();
            }

            if (Main.DebugTours.Value && AmongUsClient.Instance.NetworkMode is not NetworkModes.OnlineGame)
            {
                __instance.taskText.text = "";
                var a = "";
                foreach (var pl in PlayerCatch.AllPlayerControls)
                {
                    a += $"{Utils.GetPlayerColor(pl)}({(pl.IsAlive() ? "<#3cff63>●</color>" : $"{Utils.GetVitalText(pl.PlayerId, true)}")}) : "
                    + $"{UtilsRoleText.GetTrueRoleName(pl.PlayerId)} (pos:{pl.GetTruePosition()}) ({(pl.inVent ? "Vent" : "Walk")}) ({pl.Data.RoleType})\n";
                }
                __instance.taskText.text = a;
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