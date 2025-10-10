using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Il2CppInterop.Runtime.InteropTypes;
using HarmonyLib;
using UnityEngine;
using AmongUs.Data;
using AmongUs.GameOptions;

using TownOfHost.Modules;
using TownOfHost.Roles;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.Crewmate;
using TownOfHost.Roles.AddOns.Common;
using TownOfHost.Roles.AddOns.Impostor;
using TownOfHost.Roles.AddOns.Neutral;
using TownOfHost.Roles.Ghost;
using static TownOfHost.Translator;
using static TownOfHost.UtilsRoleText;
using TownOfHost.Patches;
using TownOfHost.Attributes;

namespace TownOfHost
{
    public static class Utils
    {
        public static bool IsActive(SystemTypes type)
        {
            if (GameStates.IsFreePlay && Main.EditMode) return false;
            // ないものはfalse
            if (!ShipStatus.Instance.Systems.ContainsKey(type))
            {
                return false;
            }
            int mapId = Main.NormalOptions.MapId;
            switch (type)
            {
                case SystemTypes.Electrical:
                    {
                        var SwitchSystem = ShipStatus.Instance.Systems[type].Cast<SwitchSystem>();
                        return SwitchSystem != null && SwitchSystem.IsActive;
                    }
                case SystemTypes.Reactor:
                    {
                        if (mapId == 2) return false;
                        else
                        {
                            var ReactorSystemType = ShipStatus.Instance.Systems[type].Cast<ReactorSystemType>();
                            return ReactorSystemType != null && ReactorSystemType.IsActive;
                        }
                    }
                case SystemTypes.Laboratory:
                    {
                        if (mapId != 2) return false;
                        var ReactorSystemType = ShipStatus.Instance.Systems[type].Cast<ReactorSystemType>();
                        return ReactorSystemType != null && ReactorSystemType.IsActive;
                    }
                case SystemTypes.LifeSupp:
                    {
                        if (mapId is 2 or 4) return false;
                        var LifeSuppSystemType = ShipStatus.Instance.Systems[type].Cast<LifeSuppSystemType>();
                        return LifeSuppSystemType != null && LifeSuppSystemType.IsActive;
                    }
                case SystemTypes.Comms:
                    {
                        if (mapId is 1 or 5)
                        {
                            var HqHudSystemType = ShipStatus.Instance.Systems[type].Cast<HqHudSystemType>();
                            return HqHudSystemType != null && HqHudSystemType.IsActive;
                        }
                        else
                        {
                            var HudOverrideSystemType = ShipStatus.Instance.Systems[type].Cast<HudOverrideSystemType>();
                            return HudOverrideSystemType != null && HudOverrideSystemType.IsActive;
                        }
                    }
                case SystemTypes.HeliSabotage:
                    {
                        var HeliSabotageSystem = ShipStatus.Instance.Systems[type].Cast<HeliSabotageSystem>();
                        return HeliSabotageSystem != null && HeliSabotageSystem.IsActive;
                    }
                case SystemTypes.MushroomMixupSabotage:
                    {
                        var mushroomMixupSabotageSystem = ShipStatus.Instance.Systems[type].TryCast<MushroomMixupSabotageSystem>();
                        return mushroomMixupSabotageSystem != null && mushroomMixupSabotageSystem.IsActive;
                    }
                default:
                    return false;
            }
        }

        public static SystemTypes GetCriticalSabotageSystemType() => (MapNames)Main.NormalOptions.MapId switch
        {
            MapNames.Polus => SystemTypes.Laboratory,
            MapNames.Airship => SystemTypes.HeliSabotage,
            _ => SystemTypes.Reactor,
        };
        //誰かが死亡したときのメソッド
        public static void TargetDies(MurderInfo info)
        {
            PlayerControl killer = info.AppearanceKiller, target = info.AttemptTarget;

            if (!target.Data.IsDead || GameStates.IsMeeting) return;

            List<PlayerControl> Players = new();
            foreach (var seer in PlayerCatch.AllPlayerControls)
            {
                if (KillFlashCheck(info, seer))
                {
                    seer.KillFlash();
                }
            }
        }
        public static bool KillFlashCheck(MurderInfo info, PlayerControl seer)
        {
            PlayerControl killer = info.AppearanceKiller, target = info.AttemptTarget;

            if (seer.Is(CustomRoles.GM)) return true;

            if (seer.Data.IsDead && (Options.GhostCanSeeKillflash.GetBool() || !Options.GhostOptions.GetBool()) && !seer.Is(CustomRoles.AsistingAngel) && (!seer.IsGhostRole() || Options.GhostRoleCanSeeKillflash.GetBool()) && target != seer) return true;
            if (seer.Data.IsDead || killer == seer || target == seer) return false;

            //ラスポスで付いてるのに！とかが一応ありえる。
            var check = false;

            if (seer.GetRoleClass() is IKillFlashSeeable killFlashSeeable)
            {
                if (Amnesia.CheckAbility(seer))
                {
                    var role = killFlashSeeable.CheckKillFlash(info);
                    if (role is null) return false;
                    check |= role is true;
                }
            }


            if (seer.Is(CustomRoles.LastImpostor) && LastImpostor.GiveSeeing.GetBool()) check |= !IsActive(SystemTypes.Comms) || LastImpostor.SeeingCanSeeComms.GetBool();
            if (seer.Is(CustomRoles.LastNeutral) && LastNeutral.GiveSeeing.GetBool()) check |= !IsActive(SystemTypes.Comms) || LastNeutral.SeeingCanSeeComms.GetBool();

            if (RoleAddAddons.GetRoleAddon(seer.GetCustomRole(), out var data, seer, subrole: CustomRoles.Seeing))
                if (data.GiveSeeing.GetBool()) check |= !IsActive(SystemTypes.Comms) || data.SeeingCanSeeComms.GetBool();

            if (SuddenDeathMode.SuddenCanSeeKillflash.GetBool()) return true;

            return check || seer.GetCustomRole() switch
            {
                // IKillFlashSeeable未適用役職はここに書く
                _ => (seer.Is(CustomRoleTypes.Madmate) && Options.MadmateCanSeeKillFlash.GetBool())
                || (seer.Is(CustomRoles.Seeing) && (!IsActive(SystemTypes.Comms) || Seeing.OptionCanSeeActiveComms.GetBool()))
            };
        }
        public static bool NowKillFlash = false;
        public static void KillFlash(this PlayerControl player, bool force = false)
        {
            //キルフラッシュ(ブラックアウト+リアクターフラッシュ)の処理
            bool ReactorCheck = IsActive(GetCriticalSabotageSystemType());

            var Duration = Options.KillFlashDuration.GetFloat();
            if (ReactorCheck) Duration += 0.2f; //リアクター中はブラックアウトを長くする

            //実行
            var state = PlayerState.GetByPlayerId(player.PlayerId);
            if (!force) state.IsBlackOut = true; //ブラックアウト
            if (player.PlayerId == 0 && !force)
            {
                FlashColor(new(1f, 0f, 0f, 0.5f));
                if (Constants.ShouldPlaySfx()) RPC.PlaySound(player.PlayerId, Sounds.KillSound);
            }
            else if (!ReactorCheck) player.ReactorFlash(0f); //リアクターフラッシュ
            player.MarkDirtySettings();
            _ = new LateTask(() =>
            {
                state.IsBlackOut = false; //ブラックアウト解除
                player.MarkDirtySettings();
            }, Options.KillFlashDuration.GetFloat(), "RemoveKillFlash");
        }
        public static void AllPlayerKillFlash()
        {
            if (SuddenDeathMode.NowSuddenDeathMode) return;

            if (IsActive(SystemTypes.Reactor) || IsActive(SystemTypes.HeliSabotage))
            {
                foreach (var pc in PlayerCatch.AllPlayerControls)
                {
                    pc.KillFlash(true);
                }
                return;
            }
            var systemtypes = GetCriticalSabotageSystemType();
            ShipStatus.Instance.RpcUpdateSystem(systemtypes, 128);

            NowKillFlash = true;
            _ = new LateTask(() =>
            {
                ShipStatus.Instance.RpcUpdateSystem(systemtypes, 16);

                if (Main.NormalOptions.MapId == 4) //Airship用
                    ShipStatus.Instance.RpcUpdateSystem(systemtypes, 17);
            }, Options.KillFlashDuration.GetFloat(), "Fix Reactor");
            _ = new LateTask(() => NowKillFlash = false, Options.KillFlashDuration.GetFloat() * 2, "", true);
        }
        public static void BlackOut(this IGameOptions opt, bool IsBlackOut)
        {
            opt.SetFloat(FloatOptionNames.ImpostorLightMod, Main.DefaultImpostorVision);
            opt.SetFloat(FloatOptionNames.CrewLightMod, Main.DefaultCrewmateVision);
            if (IsBlackOut)
            {
                opt.SetFloat(FloatOptionNames.ImpostorLightMod, 0);
                opt.SetFloat(FloatOptionNames.CrewLightMod, 0);
            }
            return;
        }
        public static bool CanDeathReasonKillerColor(this byte playerId)
        {
            var pc = PlayerCatch.GetPlayerById(playerId);
            if (pc == null) return false;
            var isAlive = pc.IsAlive();
            var GhostRole = pc.IsGhostRole();
            if (!isAlive && GhostRole) return Options.GhostRoleCanSeeKillerColor.GetBool();
            if (!isAlive && !GhostRole) return Options.GhostCanSeeKillerColor.GetBool() || !Options.GhostOptions.GetBool();
            return false;
        }
        public static string GetVitalText(byte playerId, bool? RealKillerColor = false)
        {
            var state = PlayerState.GetByPlayerId(playerId);

            if (state == null) return GetString("DeathReason.Disconnected");

            string deathReason = state.IsDead ? GetString("DeathReason." + state.DeathReason) : GetString("Alive");
            switch (RealKillerColor)
            {
                case true:
                    {
                        //rgb(97, 128, 163)
                        var KillerId = state.GetRealKiller();
                        Color color = KillerId != byte.MaxValue ? Main.PlayerColors[KillerId] : (state.DeathReason == CustomDeathReason.etc ? new Color32(97, 128, 163, 255) : new Color32(120, 120, 120, 255));
                        deathReason = ColorString(color, deathReason);
                    }
                    break;
                case null:
                    deathReason = $"<#80ffdd>{deathReason}</color>";
                    break;
            }
            return deathReason;
        }
        public static string GetDeathReason(CustomDeathReason status)
        {
            return GetString("DeathReason." + Enum.GetName(typeof(CustomDeathReason), status));
        }
        public static void ShowTimer(byte PlayerId = byte.MaxValue) => SendMessage(GetTimer(), PlayerId);
        public static string GetTimer()
        {
            var sb = new StringBuilder();
            float timerValue = GameStartManagerPatch.GetTimer();
            int minutes = (int)timerValue / 60;
            int seconds = (int)timerValue % 60;
            return $"{minutes:00}:{seconds:00}";
        }
        public static void ShowHelp(byte to = 255)
        {
            var tpinfo = "";
            var text = "";
            if (GameStates.IsLobby && Main.IsCs())
            {
                tpinfo += $"\n/tp o - {GetString("Command.tpo")}";
                tpinfo += $"\n/tp i - {GetString("Command.tpi")}";
                tpinfo += $"\n/allplayertp(/apt) - {GetString("Command.apt")}";
            }
            text = GetString("CommandList")
            + "<size=60%><line-height=1.3pic>";
            if (to == 0)
            {
                //ホスト限定
                text += $"<size=80%></line-height>\n【~~~~~~~{GetString("OnlyHost")}~~~~~~~】</size><line-height=1.3pic>"
                + $"\n/rename(/r) - {GetString("Command.rename")}"
                + $"\n/dis - {GetString("Command.dis")}"
                + $"\n/sw - {GetString("Command.sw")}"
                + $"\n/forceend(/fe) - {GetString("Command.forceend")}"
                + $"\n/mw - {GetString("Command.mw")}"
                + $"\n/kf - {GetString("Command.kf")}"
                + $"\n/addwhite(/aw) - {GetString("Command.addwhite")}";
                //導入者
                text += $"<size=80%></line-height>\n【~~~~~~~{GetString("OnlyClient")}~~~~~~~】</size><line-height=1.3pic>"
                + $"\n/dump - {GetString("Command.dump")}";
            }
            text
            //全員
            += $"<size=80%></line-height>\n【~~~~~~~{GetString("Allplayer")}~~~~~~~】</size><line-height=1.3pic>"
            + $"\n/now(/n) - {GetString("Command.now")}"
            + $"\n/now role(/n r) - {GetString("Command.nowrole")}"
            + $"\n/now set(/n s) - {GetString("Command.nowset")}"
            + $"\n/now w(/n w) - {GetString("Command.nowwin")}"
            + $"\n/h now(/h n) - {GetString("Command.h_now")}"
            + $"\n/h roles(/h r ) {GetString("Command.h_roles")}"
            + $"\n/myrole(/m) - {GetString("Command.m")}"
            + $"\n/meetinginfo(/mi,/day) - {GetString("Command.mi")}";
            if (CustomRolesHelper.CheckGuesser() || CustomRoles.Guesser.IsPresent()) text += $"\n/bt - {GetString("Command.bt")}";
            if (Options.ImpostorHideChat.GetBool()) text += $"\n/ic - {GetString("Command.impchat")}";
            if (Options.JackalHideChat.GetBool()) text += $"\n/jc - {GetString("Command.jacchat")}";
            if (Options.LoversHideChat.GetBool()) text += $"\n/lc - {GetString("Command.LoverChat")}";
            if (Options.ConnectingHideChat.GetBool()) text += $"\n/cc - {GetString("Command.ConnectingChat")}";
            if (Options.TwinsHideChat.GetBool()) text += $"\n/tc - {GetString("Command.TwinsChat")}";
            if (GameStates.IsLobby)
            {
                text += $"\n/lastresult(/l) - {GetString("Command.lastresult")}"
                    + $"\n/killlog(/kl) - {GetString("Command.killlog")}"
                    + $"\n/timer - {GetString("Command.timer")}";
            }
            if (Main.UseYomiage.Value) text += $"\n/voice - {GetString("Command.voice")}";

            SendMessage(text + tpinfo, to);
        }
        public static void SendMessage(string text, byte sendTo = byte.MaxValue, string title = "", bool rob = false, bool checkl = false)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (text.RemoveHtmlTags() == "") return;
            if (title == "") title = $"<{Main.ModColor}>" + GetString($"DefaultSystemMessageTitle");// + "</color>";
                                                                                                    //すぐ</align>すると最終行もあれなので。
            var towsend = "";
            if (checkl && text.Length > 500 && sendTo != PlayerControl.LocalPlayer.PlayerId)
            {
                var sendtext = "";
                var alltext = text.Split("\n");
                (string size, string color, string hi, string b) tag = ("", "", "", "");
                var oldtext = text;
                var i = 0;
                for (i = 0; sendtext.Length < 400; i++)
                {
                    if (sendtext.Length + alltext[i].Length > 400 && i > 0)
                    {
                        i--;
                        break;
                    }
                    sendtext += $"{alltext[i]}\n";
                    var tagtex = alltext[i].RemoveText().RemoveDeltext("　").RemoveDeltext(" ").Split("<");
                    foreach (var tagtext in tagtex)
                    {
                        if (tagtext == "") continue;
                        switch (tagtext.Substring(0, 1))
                        {
                            case "s": tag.size = $"<{tagtext.Split(">")[0]}>"; break;
                            case "c": case "#": tag.color = $"<{tagtext.Split(">")[0]}>"; break;
                            case "l": tag.hi = $"<{tagtext.Split(">")[0]}>"; break;
                            case "b": tag.b = $"<{tagtext.Split(">")[0]}>"; break;
                            case "/":
                                switch (tagtext.Substring(1, 1))
                                {
                                    case "s": tag.size = ""; break;
                                    case "c": tag.color = ""; break;
                                    case "l": tag.hi = ""; break;
                                    case "b": tag.b = ""; break;
                                }
                                break;
                        }
                    }
                }
                var send = "";
                for (var ii = i; ii < alltext.Count(); ii++)
                {
                    send += $"{alltext[ii]}\n";
                }
                towsend = $"{tag.b}{tag.hi}{tag.color}{tag.size}{send}";
                text = sendtext;
            }
            if (text.RemoveHtmlTags().StartsWith("\n") || text.RemoveHtmlTags().StartsWith("\r"))
            {
                var alltext = text.Split("\n");
                bool first = true;
                text = "";
                alltext.Do(t =>
                {
                    if (first)
                    {
                        text += t.RemoveDeltext("\n").RemoveDeltext("\r");
                        first = false;
                        return;
                    }
                    text += t + "\n";
                });
            }
            var fir = rob ? "" : "<align=\"left\">";
            text = text.RemoveDeltext("color=#", "#").RemoveDeltext("FF>", ">");
            title = title.RemoveDeltext("color=#", "#").RemoveDeltext("FF>", ">");
            Main.MessagesToSend.Add(($"{fir}{text}", sendTo, $"{fir}{title}"));
            if (towsend is not "")
            {
                SendMessage(towsend, sendTo, title, rob, true);
            }
        }
        /// <param name="pc">seer</param>
        /// <param name="force">強制かつ全員に送信</param>
        public static void ApplySuffix(PlayerControl pc, bool force = false, bool countdown = false)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (GameStates.IsOutro) return;
            if (GameStates.IsFreePlay) return;
            if (PlayerControl.LocalPlayer == null) return;

            var Iscountdown = countdown || GameStates.IsCountDown;
            string name = DataManager.player.Customization.Name;
            if (Main.nickName != "") name = Main.nickName;
            string n = name;
            bool RpcTimer = false;
            if (AmongUsClient.Instance.IsGameStarted)
            {
                if (!Camouflage.PlayerSkins.TryGetValue(PlayerControl.LocalPlayer.PlayerId, out var color)) return;

                if (Options.ColorNameMode.GetBool() && Main.nickName == "") name = Palette.GetColorName(color.ColorId);
            }
            else if (GameStates.IsLobby)
            {
                if (!Iscountdown)
                {
                    switch (Options.GetSuffixMode())
                    {
                        case SuffixModes.None:
                            break;
                        case SuffixModes.TOH:
                            name += $"<size=75%>(<{Main.ModColor}>TOH-K v{Main.PluginShowVersion})</color></size>";
                            break;
                        case SuffixModes.Streaming:
                            name += $"<size=75%>(<{Main.ModColor}>{GetString("SuffixMode.Streaming")})</color></size>";
                            break;
                        case SuffixModes.Recording:
                            name += $"<size=75%>(<{Main.ModColor}>{GetString("SuffixMode.Recording")})</color></size>";
                            break;
                        case SuffixModes.RoomHost:
                            name += $"<size=75%>(<{Main.ModColor}>{GetString("SuffixMode.RoomHost")})</color></size>";
                            break;
                        case SuffixModes.OriginalName:
                            name += $"<size=75%>(<{Main.ModColor}>{DataManager.player.Customization.Name})</color></size>";
                            break;
                        case SuffixModes.Timer:
                            if (GameStates.IsLocalGame
                            || Iscountdown) break;
                            float timerValue = GameStartManagerPatch.GetTimer();
                            if (timerValue < GameStartManagerPatch.Timer2 - 2 || GameStartManagerPatch.Timer2 < 25)
                                GameStartManagerPatch.Timer2 = timerValue;
                            timerValue = GameStartManagerPatch.Timer2;
                            int minutes = (int)timerValue / 60;
                            int seconds = (int)timerValue % 60;
                            string Color = "<#00ffff>";
                            if (minutes <= 4) Color = "<#9acd32>";//5分切ったら
                            if (minutes <= 2) Color = "<#ffa500>";//3分切ったら。
                            if (minutes <= 0) Color = "<color=red>";//1分切ったら。
                            name += $"<size=75%>({Color}{minutes:00}:{seconds:00}</color>)</size>";
                            RpcTimer = true;
                            break;
                    }
                }
            }
            //Dataのほう変えるのはなぁっておもいました。うん。
            if ((name != PlayerControl.LocalPlayer.name || countdown) && !PlayerControl.LocalPlayer.name.Contains("マーリン") && !PlayerControl.LocalPlayer.name.Contains("どちらも") && !RpcTimer && PlayerControl.LocalPlayer.CurrentOutfitType == PlayerOutfitType.Default)
            {
                PlayerControl.LocalPlayer.RpcSetName(name);
                if (!Iscountdown && GameStates.IsLobby) _ = new LateTask(() => ApplySuffix(null, force: true), 0.2f, "LobySetName", null);
            }

            if (GameStates.IsLobby && !Iscountdown && (force || (pc.name != "Player(Clone)" && pc.PlayerId != PlayerControl.LocalPlayer.PlayerId && !pc.IsModClient())))
            {
                /*
                var info = "<size=80%>";
                var at = "";
                if (Options.NoGameEnd.OptionMeGetBool()) info += $"\r\n" + ColorString(Color.red, GetString("NoGameEnd")); else at += "\r\n";
                if (Options.IsStandardHAS) info += $"\r\n" + ColorString(Color.yellow, GetString("StandardHAS")); else at += "\r\n";
                if (Options.CurrentGameMode == CustomGameMode.HideAndSeek) info += $"\r\n" + ColorString(Color.red, GetString("HideAndSeek")); else at += "\r\n";
                if (Options.CurrentGameMode == CustomGameMode.TaskBattle) info += $"\r\n" + ColorString(Color.cyan, GetString("TaskBattle")); else at += "\r\n";
                if (SuddenDeathMode.SuddenDeathModeActive.InfoGetBool()) info += "\r\n" + ColorString(GetRoleColor(CustomRoles.Comebacker), GetString("SuddenDeathMode")); else at += "\r\n";
                if (Options.EnableGM.OptionMeGetBool()) info += $"\r\n" + ColorString(GetRoleColor(CustomRoles.GM), GetString("GM")); else at += "\r\n";
                if (DebugModeManager.IsDebugMode)
                    info += "\r\n" + (DebugModeManager.EnableTOHkDebugMode.OptionMeGetBool() ? "<#0066de>DebugMode</color>" : ColorString(Color.green, "デバッグモード"));
                else at += "\r\n";
                info += "</size>";*/
                //n = "<size=120%><line-height=-1450%>\n\r<b></line-height>" + name + "\n<line-height=-100%>" + info.RemoveText() + at + $"<line-height=-1400%>\r\n{name}<size=120%><{Main.ModColor}>TownOfHost-K <#ffffff>v{Main.PluginShowVersion}<size=120%></line-height>{info}{at}</b><size=0>　";
                n = "<line-height=-100%>\n<b></line-height>" + name + $"\n<line-height=-200%>\n<size=60%><line-height=0%><{Main.ModColor}>TownOfHost-K <#ffffff>v{Main.PluginShowVersion}</size><line-height=100%>\n<size=0> ";
                if (force)
                    PlayerCatch.AllPlayerControls.DoIf(x => x.name != "Player(Clone)" && x.PlayerId != PlayerControl.LocalPlayer.PlayerId && !x.IsModClient(), x => PlayerControl.LocalPlayer.RpcSetNamePrivate(n, true, x, true));
                else if (pc.PlayerId != PlayerControl.LocalPlayer.PlayerId)
                    PlayerControl.LocalPlayer.RpcSetNamePrivate(n, true, pc);
            }
        }
        public static void SendGuardDate(byte playerId = byte.MaxValue)
        {
            StringBuilder sb = new();

            sb.Append($"{GetString("GuardData")}\n<size=60%>{GetString("GuardDataInfo")}");
            sb.Append($"<size=80%>{string.Format(GetString("GuardPower"), 0)}</size>\n{GetString("Normal")}\n");
            sb.Append($"<size=80%>{string.Format(GetString("KillPower"), 1)}</size>\n");

            var i = 0;
            bool Ishedder = false;
            CustomRoles[] killLv1 = [CustomRoles.UltraStar, CustomRoles.NekoKabocha , CustomRoles.Puppeteer , CustomRoles.Sniper, CustomRoles.TeleportKiller
            ,CustomRoles.Bomber , CustomRoles.Vampire,CustomRoles.Remotekiller];
            CustomRoles[] GuardLv1 = [CustomRoles.GuardMaster, CustomRoles.Guarding, CustomRoles.OneWolf, CustomRoles.VentOpener];
            CustomRoles[] killLv2 = [CustomRoles.Ballooner, CustomRoles.FireWorks, CustomRoles.Warlock, CustomRoles.GrimReaper];
            CustomRoles[] GuardLv2 = [CustomRoles.Fox, CustomRoles.MadGuardian];
            CustomRoles[] KillLv3 = [CustomRoles.Jumper];
            CustomRoles[] GuardLv9 = [CustomRoles.King];
            CustomRoles[] KillLv10 = [CustomRoles.ConnectSaver, CustomRoles.Shyboy, CustomRoles.Limiter, CustomRoles.EarnestWolf, CustomRoles.CurseMaker];

            sb.Append($"{GetString("DeathReason.Kill")}{(i is 3 ? "\n" : "　")}");
            i = i + 1 % 4;
            foreach (var role in killLv1)
            {
                string infotext = "";
                if (UltraStar.OptionCheckKill.GetBool() && role is CustomRoles.UltraStar)
                {
                    killLv2.AddItem(role);
                    continue;
                }
                if (role is CustomRoles.NekoKabocha) infotext = $"({GetString("DeathReason.Revenge")})";
                if (role is CustomRoles.Sniper) infotext = $"({GetString("DeathReason.Sniped")})";
                if (role is CustomRoles.TeleportKiller) infotext = $"({GetString("DeathReason.TeleportKill")})";
                if (role is CustomRoles.Bomber) infotext = $"({GetString("DeathReason.Bombed")})";
                if (role.IsEnable())
                {
                    sb.Append($"{GetRoleColorAndtext(role)}{infotext}{(i is 3 ? "\n" : "　")}");
                    i = i + 1 % 4;
                }
            }
            i = 0;
            Ishedder = true;
            foreach (var role in GuardLv1)
            {
                string infotext = "";
                if (role is CustomRoles.VentOpener && !VentOpener.OptionBlockKill.GetBool()) continue;
                if (role is CustomRoles.VentOpener) infotext = $"({GetString("DeathReason.Revenge")})";
                if (role.IsEnable())
                {
                    if (Ishedder)
                    {
                        sb.Append($"\n<size=80%>{string.Format(GetString("GuardPower"), 1)}</size>\n");
                        Ishedder = false;
                    }
                    sb.Append($"{GetRoleColorAndtext(role)}{infotext}{(i is 3 ? "\n" : "　")}");
                    i = i + 1 % 4;
                }
            }
            i = 0;
            Ishedder = true;
            foreach (var role in killLv2)
            {
                string infotext = "";
                if (role is CustomRoles.Bomber or CustomRoles.FireWorks) infotext = $"({GetString("DeathReason.Bombed")})";
                if (role is CustomRoles.GrimReaper) infotext = $"({GetString("DeathReason.Grim")})";
                if (role is CustomRoles.Warlock) infotext = $"({GetString("DeathReason.Spell")})";
                if (role.IsEnable())
                {
                    if (Ishedder)
                    {
                        sb.Append($"\n<size=80%>{string.Format(GetString("KillPower"), 2)}</size>\n");
                        Ishedder = false;
                    }
                    sb.Append($"{GetRoleColorAndtext(role)}{infotext}{(i is 3 ? "\n" : "　")}");
                    i = i + 1 % 4;
                }
            }
            i = 0;
            Ishedder = true;
            foreach (var role in GuardLv2)
            {
                string infotext = "";
                if (role.IsEnable())
                {
                    if (Ishedder)
                    {
                        sb.Append($"\n<size=80%>{string.Format(GetString("GuardPower"), 2)}</size>\n");
                        Ishedder = false;
                    }
                    sb.Append($"{GetRoleColorAndtext(role)}{infotext}{(i is 3 ? "\n" : "　")}");
                    i = i + 1 % 4;
                }
            }
            i = 0;
            Ishedder = true;
            foreach (var role in KillLv3)
            {
                string infotext = "";
                if (role.IsEnable())
                {
                    if (Ishedder)
                    {
                        sb.Append($"\n<size=80%>{string.Format(GetString("KillPower"), 3)}</size>\n");
                        Ishedder = false;
                    }
                    sb.Append($"{GetRoleColorAndtext(role)}{infotext}{(i is 3 ? "\n" : "　")}");
                    i = i + 1 % 4;
                }
            }
            i = 0;
            Ishedder = true;
            foreach (var role in GuardLv9)
            {
                string infotext = "";
                if (role.IsEnable())
                {
                    if (Ishedder)
                    {
                        sb.Append($"\n<size=80%>{string.Format(GetString("GuardPower"), 9)}</size>\n");
                        Ishedder = false;
                    }
                    sb.Append($"{GetRoleColorAndtext(role)}{infotext}{(i is 3 ? "\n" : "　")}");
                    i = i + 1 % 4;
                }
            }
            i = 0;
            Ishedder = true;
            foreach (var role in KillLv10)
            {
                string infotext = "";
                if (role is CustomRoles.Shyboy && Shyboy.OptionShyDieBom.GetBool() is false) continue;
                if (role is CustomRoles.Shyboy or CustomRoles.Limiter) infotext = $"({GetString("DeathReason.Bombed")})";
                if (role.IsEnable())
                {
                    if (Ishedder)
                    {
                        sb.Append($"\n<size=80%>{string.Format(GetString("KillPower"), 10)}</size>\n");
                        Ishedder = false;
                    }
                    sb.Append($"{GetRoleColorAndtext(role)}{infotext}{(i is 3 ? "\n" : "　")}");
                    i = i + 1 % 4;
                }
            }
            SendMessage(sb.ToString(), playerId, checkl: true);
        }
        #region AfterMeetingTasks
        public static bool CantUseVent;
        public static List<byte> RoleSendList = new();
        public static void AfterMeetingTasks()
        {
            MovingPlatformBehaviourPatch.SetPlatfrom();
            GameStates.CalledMeeting = false;
            //天秤会議だと送らない
            if (Balancer.Id == 255 && Balancer.target1 != 255 && Balancer.target2 != 255 && (!Options.firstturnmeeting || !MeetingStates.First))
            {
                foreach (var roleClass in CustomRoleManager.AllActiveRoles.Values)
                {
                    if (roleClass is Balancer balancer) balancer.BalancerAfterMeetingTasks();
                }
            }
            else
            {
                if (!Options.firstturnmeeting || !MeetingStates.First)
                {
                    if (Amanojaku.AssingDay.GetFloat() == UtilsGameLog.day) AmanojakuAssing.AssignAddOnsFromList();
                    if (Amnesia.OptionRealizeDayCount.GetFloat() <= UtilsGameLog.day && Amnesia.OptionCanRealizeDay.GetBool())
                    {
                        foreach (var pc in PlayerCatch.AllPlayerControls)
                        {
                            if (pc.Is(CustomRoles.Amnesia))
                            {
                                if (!RoleSendList.Contains(pc.PlayerId)) RoleSendList.Add(pc.PlayerId);
                                Amnesia.RemoveAmnesia(pc.PlayerId);
                            }
                        }
                    }
                }
                foreach (var pc in PlayerCatch.AllPlayerControls)
                {
                    var roleClass = pc.GetRoleClass();
                    if (!Options.firstturnmeeting || !MeetingStates.First) roleClass?.AfterMeetingTasks();
                    pc.GetRoleClass()?.ChangeColor();//会議後、役職変更されてものやつ。
                }
                if (!Options.firstturnmeeting || !MeetingStates.First)
                {
                    if (AsistingAngel.CanSetAsistTarget())
                    {
                        AsistingAngel.Limit++;
                    }

                    UtilsGameLog.day++;
                    UtilsGameLog.AddGameLogsub("\n" + string.Format(GetString("Message.Day").RemoveDeltext("【").RemoveDeltext("】"), UtilsGameLog.day).Color(Palette.Orange));
                }
            }

            if (AmongUsClient.Instance.AmHost)
            {
                if (Options.AirShipVariableElectrical.GetBool()) AirShipElectricalDoors.Initialize();
                DoorsReset.ResetDoors();
                // 空デデンバグ対応 会議後にベントを空にする
                var ventilationSystem = ShipStatus.Instance.Systems.TryGetValue(SystemTypes.Ventilation, out var systemType) ? systemType.TryCast<VentilationSystem>() : null;
                if (ventilationSystem != null)
                {
                    ventilationSystem.PlayersInsideVents.Clear();
                    ventilationSystem.IsDirty = true;
                }
                GuessManager.Reset();//会議後にリセット入れる
                GameStates.ExiledAnimate = false;
            }
        }
        #endregion
        public static void ChangeInt(ref int ChangeTo, int input, int max)
        {
            var tmp = ChangeTo * 10;
            tmp += input;
            ChangeTo = Math.Clamp(tmp, 0, max);
        }
        public static string PadRightV2(this object text, int num)
        {
            int bc = 0;
            var t = text.ToString();
            foreach (char c in t) bc += Encoding.GetEncoding("UTF-8").GetByteCount(c.ToString()) == 1 ? 1 : 2;
            return t?.PadRight(Mathf.Max(num - (bc - t.Length), 0));
        }
        #region Remove
        public static string RemoveHtmlTags(this string str) => Regex.Replace(str, "<[^>]*?>", "");
        public static string RemoveColorTags(this string str)
        {
            var removecolor = Regex.Replace(str, "</?color(=#[0-9a-fA-F]*)?>", "");
            removecolor = Regex.Replace(removecolor, "<#[^>]*?>", "");
            return removecolor;
        }
        public static string RemoveSizeTags(this string str) => Regex.Replace(str, "</?size[^>]*?>", "");
        public static string RemoveGiveAddon(this string str) => Regex.Replace(str, "を付与する", "");
        public static string RemoveSN(this string str) => Regex.Replace(str, "\n", "");
        public static string Changebr(this string str, bool nokosu) => Regex.Replace(str, "\n", $"{(nokosu ? "<br>\n" : "<br>")}");
        public static string RemoveaAlign(this string str) => Regex.Replace(str, "align", "");
        public static string RemoveDeltext(this string str, string del, string set = "") => Regex.Replace(str, del, set);
        public static string RemoveText(this string str, bool Update = false)
        {
            bool Skip = false;
            string returns = "";
            if (Update)
            {
                for (var i = 0; i < str.Length; i++)
                {
                    string text = "";
                    text = str.Substring(i, 1);
                    if (text == Regex.Replace(text, "[0-9]", "")) continue;
                    returns += text;
                }
                return returns;
            }
            for (var i = 0; i < str.Length; i++)
            {
                string text = "";
                text = str.Substring(i, 1);

                {
                    if (text == "<")
                        Skip = true;
                    if (text == ">")
                    {
                        returns += ">";
                        Skip = false;
                    }

                    if (Skip)
                        returns += text;
                    else
                        if (text != ">")
                    {
                        if (text == "\n")
                            returns += "\n ";
                        else if (text == "\r")
                            returns += "\r";
                        else
                            returns += " ";
                    }
                }
            }
            return returns;
        }
        #endregion
        public static void FlashColor(Color color, float duration = 1f)
        {
            var hud = DestroyableSingleton<HudManager>.Instance;
            if (hud.FullScreen == null) return;
            var obj = hud.transform.FindChild("FlashColor_FullScreen")?.gameObject;
            if (obj == null)
            {
                obj = GameObject.Instantiate(hud.FullScreen.gameObject, hud.transform);
                obj.name = "FlashColor_FullScreen";
            }
            hud.StartCoroutine(Effects.Lerp(duration, new Action<float>((t) =>
            {
                obj.SetActive(t != 1f);
                obj.GetComponent<SpriteRenderer>().color = new(color.r, color.g, color.b, Mathf.Clamp01((-2f * Mathf.Abs(t - 0.5f) + 1) * color.a)); //アルファ値を0→目標→0に変化させる
            })));
        }

        public static string ColorString(Color32 color, string str) => $"<#{color.r:x2}{color.g:x2}{color.b:x2}{color.a:x2}>{str}</color>";
        /// <summary>
        /// Darkness:１の比率で黒色と元の色を混ぜる。マイナスだと白色と混ぜる。
        /// </summary>
        public static Color ShadeColor(this Color color, float Darkness = 0)
        {
            bool IsDarker = Darkness >= 0; //黒と混ぜる
            if (!IsDarker) Darkness = -Darkness;
            float Weight = IsDarker ? 0 : Darkness; //黒/白の比率
            float R = (color.r + Weight) / (Darkness + 1);
            float G = (color.g + Weight) / (Darkness + 1);
            float B = (color.b + Weight) / (Darkness + 1);
            return new Color(R, G, B, color.a);
        }

        /// <summary>
        /// 乱数の簡易的なヒストグラムを取得する関数
        /// <params name="nums">生成した乱数を格納したint配列</params>
        /// <params name="scale">ヒストグラムの倍率 大量の乱数を扱う場合、この値を下げることをお勧めします。</params>
        /// </summary>
        public static string WriteRandomHistgram(int[] nums, float scale = 1.0f)
        {
            int[] countData = new int[nums.Max() + 1];
            foreach (var num in nums)
            {
                if (0 <= num) countData[num]++;
            }
            StringBuilder sb = new();
            for (int i = 0; i < countData.Length; i++)
            {
                // 倍率適用
                countData[i] = (int)(countData[i] * scale);

                // 行タイトル
                sb.AppendFormat("{0:D2}", i).Append(" : ");

                // ヒストグラム部分
                for (int j = 0; j < countData[i]; j++)
                    sb.Append('|');

                // 改行
                sb.Append('\n');
            }

            // その他の情報
            sb.Append("最大数 - 最小数: ").Append(countData.Max() - countData.Min());

            return sb.ToString();
        }

        public static bool TryCast<T>(this Il2CppObjectBase obj, out T casted)
        where T : Il2CppObjectBase
        {
            casted = obj.TryCast<T>();
            return casted != null;
        }
        public static float Round(this float value, float digit)
        {
            var roundvalue = Mathf.Round(value / digit);
            if ((roundvalue * digit).ToString().Contains("."))
            {
                var roundvaluestring = $"{roundvalue * digit}";
                string[] ages = roundvaluestring.Split(".");
                if (ages[1].Count() > digit.ToString().RemoveDeltext("0.", ".").Count())
                {
                    var ages1 = ages[1].ToString().Substring(0, digit.ToString().RemoveDeltext("0.", ".").Count());

                    return float.TryParse(ages[0] + "." + ages1, out var result) ? result : roundvalue * digit;
                }
            }
            return roundvalue * digit;
        }
        public const string AdditionalWinnerMark = "<#dddd00>★</color>";

        public static void SyncAllSettings()
        {
            // 設定を同期するための処理をここに記述
            // 例: 各プレイヤーの設定をサーバーと同期する
            Logger.Info("Syncing all settings...", "Utils");
            // 実際の同期処理をここに実装
        }
        [GameModuleInitializer]
        public static void Init()
        {
            GameDataSerializePatch.DontTouch = false;
            Camouflage.ventplayr.Clear();
            PlayerCatch.OldAlivePlayerControles.Clear();
            ReportDeadBodyPatch.DontReport.Clear();
            RandomSpawn.SpawnMap.NextSporn.Clear();
            RandomSpawn.SpawnMap.NextSpornName.Clear();
            Patches.ISystemType.VentilationSystemUpdateSystemPatch.NowVentId.Clear();
            CoEnterVentPatch.VentPlayers.Clear();
            MeetingHudPatch.Oniku = "";
            MeetingHudPatch.Send = "";
            MeetingHudPatch.Title = "";
            MeetingVoteManager.Voteresult = "";
            IUsePhantomButton.IPPlayerKillCooldown.Clear();
            CustomButtonHud.CantJikakuIsPresent = null;
            RoleSendList.Clear();
            UtilsNotifyRoles.ExtendedMeetingText = "";
            Roles.Madmate.MadAvenger.Skill = false;
            Roles.Neutral.JackalDoll.NowSideKickCount = 0;
            Balancer.Id = 255;
            Main.DisableTaskPlayerList = new();
            Stolener.Killers.Clear();
            Options.firstturnmeeting = Options.FirstTurnMeeting.GetBool() && !SuddenDeathMode.SuddenDeathModeActive.GetBool();
            CoEnterVentPatch.OldOnEnterVent = new();

            if (Options.CantUseVentMode.GetBool() && (Options.CantUseVentTrueCount.GetFloat() >= PlayerCatch.AllAlivePlayerControls.Count())) CantUseVent = true;
            else CantUseVent = false;
        }
    }
}
