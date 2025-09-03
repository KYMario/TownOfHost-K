using System.Globalization;
using System.Text;
using HarmonyLib;
using TMPro;
using UnityEngine;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Templates;
using static TownOfHost.Translator;
using System.Linq;
using System.Collections.Generic;

namespace TownOfHost
{
    [HarmonyPatch]
    public static class CredentialsPatch
    {
        public static SpriteRenderer TohkLogo { get; private set; }
        public static TextMeshPro credentialsText;
        private static float deltaTime = 0.0f;
        public static bool a = false;
        public static List<float> fpss = new();

        [HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
        class PingTrackerUpdatePatch
        {
            static StringBuilder sb = new();
            static StringBuilder exSb = new();
            static void Postfix(PingTracker __instance)
            {
                if (!credentialsText)
                {
                    credentialsText = Object.Instantiate(__instance.text, __instance.transform.parent);
                    credentialsText.name = "credentialsText";
                    credentialsText.transform.parent = __instance.transform.parent;
                    Object.Destroy(credentialsText.GetComponent<PingTracker>());
                    Object.Destroy(credentialsText.GetComponent<AspectPosition>());
                }

                credentialsText.alignment = TextAlignmentOptions.TopRight;

                if ((FixedUpdatePatch.timer is 1 && GameStates.InGame) || GameStates.IsLobby)
                {
                    sb.Clear();

                    sb.Append("\r\n").Append($"<{Main.ModColor}>{Main.ModName}</color> v{Main.PluginShowVersion}");
                    if (Main.DebugVersion) sb.Append($"<{Main.ModColor}>☆Debug☆</color>");

                    if ((Options.NoGameEnd.OptionMeGetBool() && GameStates.IsLobby) || (Main.DontGameSet && !GameStates.IsLobby)) sb.Append($"\r\n").Append(Utils.ColorString(Color.red, GetString("NoGameEnd")));
                    if (Options.IsStandardHAS) sb.Append($"\r\n").Append(Utils.ColorString(Color.yellow, GetString("StandardHAS")));
                    if (Options.CurrentGameMode == CustomGameMode.HideAndSeek) sb.Append($"\r\n").Append(Utils.ColorString(Color.red, GetString("HideAndSeek")));
                    if (Options.CurrentGameMode == CustomGameMode.TaskBattle) sb.Append($"\r\n").Append(Utils.ColorString(Color.cyan, GetString("TaskBattle")));
                    if (SuddenDeathMode.SuddenDeathModeActive.InfoGetBool()) sb.Append("\r\n").Append(Utils.ColorString(UtilsRoleText.GetRoleColor(CustomRoles.Comebacker), GetString("SuddenDeathMode")));
                    if (Options.EnableGM.OptionMeGetBool()) sb.Append($"\r\n").Append(Utils.ColorString(UtilsRoleText.GetRoleColor(CustomRoles.GM), GetString("GM")));
                    if (!GameStates.IsModHost) sb.Append($"\r\n").Append(Utils.ColorString(Color.red, GetString("Warning.NoModHost")));
                    if (DebugModeManager.IsDebugMode)
                    {
                        sb.Append("\r\n");
                        sb.Append(DebugModeManager.EnableTOHkDebugMode.OptionMeGetBool() ? "<#0066de>DebugMode</color>" : Utils.ColorString(Color.green, "デバッグモード"));
                    }

                    exSb.Clear();

                    // #ffef39
                    if (Options.ExHideChatCommand.GetBool())
                        exSb.Append($"<#ffdfaf>Ⓗ</color> ");
                    if (Options.ExAftermeetingflash.GetBool())
                        exSb.Append($"<#d62c12>Ⓚ</color> ");
                    if (Options.FixSpawnPacketSize.GetBool())
                        exSb.Append($"<#ffef39>Ⓟ</color> ");
                    if (Options.ExIntroWeight.GetBool())
                        exSb.Append($"<#8839ff>Ⓘ</color> ");
                    if (Options.ExRpcWeightR.GetBool())
                        exSb.Append($"<#3d83c5>Ⓡ</color> ");

                    if (exSb.Length > 0)
                    {
                        sb.Append("\r\n<size=50%>").Append(exSb).Append("</size>");
                    }

                    var offset_x = 2.5f; //右端からのオフセット
                    if (HudManager.InstanceExists && HudManager._instance.Chat.gameObject.active) offset_x += 0.6f; //チャットがある場合の追加オフセット
                    credentialsText.transform.localPosition = new Vector3((5.6779f * GameSettingMenuStartPatch.Heightratio) - offset_x, 3.0745f, 0f);

                    if (GameStates.IsLobby)
                    {
                        if (Options.IsStandardHAS && !CustomRoles.Sheriff.IsEnable() && !CustomRoles.SerialKiller.IsEnable() && CustomRoles.Egoist.IsEnable())
                            sb.Append($"\r\n").Append(Utils.ColorString(Color.red, GetString("Warning.EgoistCannotWin")));
                    }

                    credentialsText.text = sb.ToString();
                }
#if DEBUG
                if (Main.ViewPingDetails.Value)
                {
                    var serverName = GameStates.IsOnlineGame ? (Main.IsCs() ? ServerManager.Instance.CurrentRegion.Name : GetString(ServerManager.Instance.CurrentRegion.TranslateName)) : "ローカル";
                    deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
                    float fps = 1.0f / deltaTime;
                    __instance.text.text += $"({AmongUsClient.Instance.Ping / 1000f}秒/{serverName}/FPS: {Mathf.Ceil(fps)})\n";
                    __instance.text.alignment = TextAlignmentOptions.Top;
                    if (a) fpss.Add(fps);
                }
                else __instance.text.alignment = TextAlignmentOptions.TopLeft;
#endif
            }
        }
        public static string Subver;
        [HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
        class VersionShowerStartPatch
        {
            static TextMeshPro SpecialEventText;
            static void Postfix(VersionShower __instance)
            {
                if (!__instance) return;

                TMPTemplate.SetBase(__instance.text);

                CreateText();

                ErrorText.Create(__instance.text);
                if (Main.hasArgumentException && ErrorText.Instance != null)
                {
                    ErrorText.Instance.AddError(ErrorCode.Main_DictionaryError);
                }

                VersionChecker.Check();
#if DEBUG
                if (OptionItem.IdDuplicated)
                {
                    ErrorText.Instance.AddError(ErrorCode.OptionIDDuplicate);
                }
#endif

                if (SpecialEventText == null && TohkLogo != null)
                {
                    SpecialEventText = TMPTemplate.Create(
                        "SpecialEventText",
                        "",
                        Color.white,
                        alignment: TextAlignmentOptions.Center,
                        parent: TohkLogo.transform);
                    SpecialEventText.name = "SpecialEventText";
                    SpecialEventText.fontSizeMin = 3f;
                    SpecialEventText.transform.localPosition = new Vector3(0f, 0.8f, 0f);
                    SpecialEventText?.gameObject?.SetActive(true);
                }
                if (!SpecialEventText) return;
                SpecialEventText.enabled = TitleLogoPatch.amongUsLogo != null;
                if (Event.IsInitialRelease)
                {
                    SpecialEventText.text = $"Happy Birthday to {Main.ModName}!";
                    if (ColorUtility.TryParseHtmlString(Main.ModColor, out var col))
                    {
                        SpecialEventText.color = col;
                    }
                }
                if (Event.IsChristmas && CultureInfo.CurrentCulture.Name == "ja-JP")
                {
                    //このソースコ―ドを見た人へ。口外しないでもらえると嬉しいです...
                    //To anyone who has seen this source code. I would appreciate it if you would keep your mouth shut...
                    SpecialEventText.text = $"何とは言いませんが、特別な日ですね。\n<size=15%>\n\n末永く爆発しろ</size>";
                    SpecialEventText.color = UtilsRoleText.GetRoleColor(CustomRoles.Lovers);
                }
                MainMenuManagerPatch.Statistisc = TMPTemplate.Create(
                "Statistisc",
                "",
                Color.white,
                3f,
                TextAlignmentOptions.TopLeft,
                false,
                null
                );
                {
                    MainMenuManagerPatch.Statistisc.transform.localPosition = new Vector3(0.8f, 1.7f);
                }
            }
        }

        public static TextMeshPro CreateText()
        {
            var Debugver = "";
            if (Main.DebugVersion) Debugver = $"<{Main.ModColor}>☆Debug☆</color>";
            Subver = "";
            Main.credentialsText = $"<{Main.ModColor}>{Main.ModName}</color> v{Main.PluginShowVersion}" + Debugver;
#if DEBUG
            if (!GameStates.InGame) Main.credentialsText += $"\n<{Main.ModColor}>{ThisAssembly.Git.Branch}({ThisAssembly.Git.Commit})</color>";
#endif
            var credentials = TMPTemplate.Create(
                "TOHCredentialsText",
                Main.credentialsText,
                fontSize: 2f,
                alignment: TextAlignmentOptions.Right,
                setActive: true);
            credentials.transform.position = new Vector3(2.3419f, 2.29f, -5f);
#if DEBUG
            if (!GameStates.InGame) credentials.transform.position -= new Vector3(0f, 0.1218f, 0f);
#endif
            if (FindAGameManager._instance)
            {
                credentials.transform.position = new Vector3(2.5f, -2.858f, 5f);
#if DEBUG
                credentials.transform.position += new Vector3(0, 0.185f);
#endif
            }
            return credentials;
        }

        [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
        class TitleLogoPatch
        {
            public static GameObject amongUsLogo;

            [HarmonyPriority(Priority.VeryHigh)]
            static void Postfix(MainMenuManager __instance)
            {
                amongUsLogo = GameObject.Find("LOGO-AU");

                var rightpanel = __instance.gameModeButtons.transform.parent;
                var logoObject = new GameObject("titleLogo_TOHk");
                var logoTransform = logoObject.transform;
                TohkLogo = logoObject.AddComponent<SpriteRenderer>();
                logoTransform.parent = rightpanel;
                logoTransform.localPosition = new(0f, 0.15f, 1f);
                logoTransform.localScale *= 1.0f;
                TohkLogo.sprite = UtilsSprite.LoadSprite(Event.April || Event.Special ? "TownOfHost.Resources.TownOfHost-K_A.png" : "TownOfHost.Resources.TownOfHost-K.png", 300f);
            }
        }
        [HarmonyPatch(typeof(ModManager), nameof(ModManager.LateUpdate))]
        class ModManagerLateUpdatePatch
        {
            static int oldcount;
            static float olddeltimer;
            static float timer = 0;
            public static void Prefix(ModManager __instance)
            {
                __instance.ShowModStamp();

                LateTask.Update(Time.deltaTime);
                CheckMurderPatch.Update();

                if (Main.MegCount > 49)
                {
                    timer += Time.deltaTime;

                    if (timer > 1)
                    {
                        timer = 0;
                        olddeltimer = 0;
                        Main.MegCount = 0;
                    }
                }
                else
                if (Main.MegCount == oldcount)
                {
                    olddeltimer += Time.deltaTime;

                    if (olddeltimer > 1.3f)
                    {
                        timer = 0;
                        olddeltimer = 0;
                        Main.MegCount = 0;
                    }
                }

                oldcount = Main.MegCount;
            }
            public static void Postfix(ModManager __instance)
            {
                var offset_y = HudManager.InstanceExists ? 1.6f : 0.9f;
                __instance.ModStamp.transform.position = AspectPosition.ComputeWorldPosition(
                    __instance.localCamera, AspectPosition.EdgeAlignments.RightTop,
                    new Vector3(0.4f, offset_y, __instance.localCamera.nearClipPlane + 0.1f));
            }
        }
    }
}
