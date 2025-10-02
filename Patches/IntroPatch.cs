using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using AmongUs.GameOptions;
using HarmonyLib;
using UnityEngine;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.AddOns.Common;
using TownOfHost.Roles.Impostor;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.CoBegin))]
    class SetUpRoleTextCoBeginPatch
    {
        public static void Postfix(IntroCutscene __instance, ref Il2CppSystem.Collections.IEnumerator __result)
        {
            //ShowRoleに直接パッチあて出来ないためCoBegin中にパッチを当てる
            var patcher = new CoroutinPatcher(__result);
            //ShowRoleはステートマシンクラスになっているためその実行前にパッチを当てる
            //元々Postfixだが、タイミング的にはPrefixの方が適切なのでPrefixに当てる
            //androidは_ShowRole_d__41なんてないよとエラーを吐く。
            patcher.AddPrefix(typeof(IntroCutscene), nameof(IntroCutscene.ShowRole), () => SetUpRoleTextPatch.Postfix(__instance));
            __result = patcher.EnumerateWithPatch();
        }
    }
    class SetUpRoleTextPatch
    {
        public static void Postfix(IntroCutscene __instance)
        {
            if (!GameStates.IsModHost) return;
            _ = new LateTask(() =>
            {
                CustomRoles role = PlayerControl.LocalPlayer.GetCustomRole();
                if (PlayerControl.LocalPlayer.GetMisidentify(out var missrole)) role = missrole;

                if (!role.IsVanilla() && !PlayerControl.LocalPlayer.Is(CustomRoles.Amnesia))
                {
                    __instance.YouAreText.color = UtilsRoleText.GetRoleColor(role);
                    __instance.RoleText.text = UtilsRoleText.GetRoleName(role);
                    __instance.RoleText.color = UtilsRoleText.GetRoleColor(role);
                    __instance.RoleBlurbText.color = UtilsRoleText.GetRoleColor(role);

                    __instance.RoleBlurbText.text = PlayerControl.LocalPlayer.GetRoleDesc();

                    //Amnesiacだった場合シェリフと表示させる
                    if (role == CustomRoles.Amnesiac)
                    {
                        __instance.RoleText.text = Amnesiac.IsWolf ? UtilsRoleText.GetRoleName(CustomRoles.WolfBoy) : UtilsRoleText.GetRoleName(CustomRoles.Sheriff);
                        __instance.YouAreText.color = UtilsRoleText.GetRoleColor(role);
                        __instance.RoleText.color = UtilsRoleText.GetRoleColor(role);
                        __instance.RoleBlurbText.color = UtilsRoleText.GetRoleColor(role);
                    }
                }
                else
                if (role.IsVanilla())
                {
                    __instance.YouAreText.color = UtilsRoleText.GetRoleColor(role);
                    __instance.RoleText.color = UtilsRoleText.GetRoleColor(role);
                    __instance.RoleBlurbText.color = UtilsRoleText.GetRoleColor(role);
                }

                foreach (var subRole in PlayerState.GetByPlayerId(PlayerControl.LocalPlayer.PlayerId).SubRoles)
                {
                    if (subRole == CustomRoles.Amnesia) continue;
                    __instance.RoleBlurbText.text += "<size=75%>\n" + Utils.ColorString(UtilsRoleText.GetRoleColor(subRole), GetString($"{subRole}Info"));
                }
                __instance.RoleText.text += UtilsRoleText.GetSubRolesText(PlayerControl.LocalPlayer.PlayerId, amkesu: true);

            }, 0.01f, "Override Role Text", null);

            /* wiki用
                            if (PlayerControl.LocalPlayer.IsGhostRole())
                    role = PlayerState.GetByPlayerId(PlayerControl.LocalPlayer.PlayerId).GhostRole;

                if (!role.IsVanilla() && !PlayerControl.LocalPlayer.Is(CustomRoles.Amnesia))
                {
                    __instance.YouAreText.color = UtilsRoleText.GetRoleColor(role);
                    __instance.RoleText.text = UtilsRoleText.GetRoleName(role);
                    __instance.RoleText.color = UtilsRoleText.GetRoleColor(role);
                    __instance.RoleBlurbText.color = UtilsRoleText.GetRoleColor(role);

                    __instance.RoleBlurbText.text = PlayerControl.LocalPlayer.GetRoleInfo();

                    //Amnesiacだった場合シェリフと表示させる
                    if (role == CustomRoles.Amnesiac)
                    {
                        __instance.RoleText.text = Amnesiac.IsWolf ? UtilsRoleText.GetRoleName(CustomRoles.WolfBoy) : UtilsRoleText.GetRoleName(CustomRoles.Sheriff);
                        __instance.YouAreText.color = UtilsRoleText.GetRoleColor(role);
                        __instance.RoleText.color = UtilsRoleText.GetRoleColor(role);
                        __instance.RoleBlurbText.color = UtilsRoleText.GetRoleColor(role);
                    }
                }
                else
                if (role.IsVanilla())
                {
                    __instance.YouAreText.color = UtilsRoleText.GetRoleColor(role);
                    __instance.RoleText.color = UtilsRoleText.GetRoleColor(role);
                    __instance.RoleBlurbText.color = UtilsRoleText.GetRoleColor(role);
                }

                foreach (var subRole in PlayerState.GetByPlayerId(PlayerControl.LocalPlayer.PlayerId).SubRoles)
                {
                    __instance.RoleText.text = UtilsRoleText.GetRoleName(subRole);
                    __instance.YouAreText.color = UtilsRoleText.GetRoleColor(subRole);
                    __instance.RoleText.color = UtilsRoleText.GetRoleColor(subRole);
                    __instance.RoleBlurbText.color = UtilsRoleText.GetRoleColor(subRole);
                    __instance.RoleBlurbText.text = GetString($"{subRole}Info");
                }
                //__instance.RoleText.text += UtilsRoleText.GetSubRolesText(PlayerControl.LocalPlayer.PlayerId, amkesu: true);
            */
            if (PlayerControl.LocalPlayer.GetCustomRole() is CustomRoles.VentHunter)
                SoundManager.Instance.PlaySound(DestroyableSingleton<PhantomRole>.Instance.IntroSound, false);
        }
    }
    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.CoBegin))]
    class CoBeginPatch
    {
        public static void Prefix(IntroCutscene __instance, ref Il2CppSystem.Collections.IEnumerator __result)
        {
            var logger = Logger.Handler("Info");
            logger.Info("------------名前表示------------");
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                logger.Info($"{(pc.AmOwner ? "[*]" : ""),-3}{pc.PlayerId,-2}:{pc.name.PadRightV2(20)}:{pc.cosmetics.nameText.text}({Palette.ColorNames[pc.Data.DefaultOutfit.ColorId].ToString().Replace("Color", "")})");
                pc.cosmetics.nameText.text = pc.name;
            }
            logger.Info("----------役職割り当て----------");
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                logger.Info($"{(pc.AmOwner ? "[*]" : ""),-3}{pc.PlayerId,-2}:{pc?.Data?.GetLogPlayerName()?.PadRightV2(20)}:{pc.GetAllRoleName().RemoveHtmlTags()}");
            }
            logger.Info("--------------環境--------------");
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                try
                {
                    var text = pc.AmOwner ? "[*]" : "   ";
                    text += $"{pc.PlayerId,-2}:{pc.Data?.GetLogPlayerName()?.PadRightV2(20)}:{pc.GetClient()?.PlatformData?.Platform.ToString()?.Replace("Standalone", ""),-11}";
                    if (Main.playerVersion.TryGetValue(pc.PlayerId, out PlayerVersion pv))
                        text += $":Mod({pv.forkId}/{pv.version}:{pv.tag})";
                    else text += ":Vanilla";
                    logger.Info(text);
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex, "Platform");
                }
            }
            logger.Info("------------基本設定------------");
            var tmp = GameOptionsManager.Instance.CurrentGameOptions.ToHudString(GameData.Instance ? GameData.Instance.PlayerCount : 10).Split("\r\n").Skip(1).SkipLast(10);
            foreach (var t in tmp) logger.Info(t);
            logger.Info("------------詳細設定------------");
            foreach (var o in OptionItem.AllOptions)
                if (!o.IsHiddenOn(Options.CurrentGameMode) && (o.Parent == null ? !o.GetString().Equals("0%") : o.Parent.InfoGetBool()) && o.IsEnabled.Invoke())
                    logger.Info($"{(o.Parent == null ? o.Name.PadRightV2(40) : $"┗ {o.Name}".PadRightV2(41))}:{o.GetTextString().RemoveSN().RemoveHtmlTags()}");
            logger.Info("-------------その他-------------");
            logger.Info($"プレイヤー数: {PlayerCatch.AllPlayerControls.Count()}人");
            GameStates.InGame = true;

            if (Options.CurrentGameMode is not CustomGameMode.Standard)
            {
                PlayerCatch.AllPlayerControls.Do(x => PlayerState.GetByPlayerId(x.PlayerId).InitTask(x));
                GameData.Instance.RecomputeTaskCounts();
                TaskState.InitialTotalTasks = GameData.Instance.TotalTasks;
            }
        }
    }
    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginCrewmate))]
    class BeginCrewmatePatch
    {
        public static void Prefix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> teamToDisplay)
        {
            if (PlayerControl.LocalPlayer.Is(CustomRoleTypes.Neutral) && !PlayerControl.LocalPlayer.Is(CustomRoles.BakeCat) && !PlayerControl.LocalPlayer.Is(CustomRoles.Amnesia))
            {
                //ぼっち役職
                var soloTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
                soloTeam.Add(PlayerControl.LocalPlayer);
                teamToDisplay = soloTeam;
            }
        }
        public static void Postfix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> teamToDisplay)
        {
            //チーム表示変更
            CustomRoles role = PlayerControl.LocalPlayer.GetCustomRole();
            var pc = PlayerControl.LocalPlayer;
            var IsMisidentify = pc.GetMisidentify(out var missrole);
            if (IsMisidentify) role = missrole;

            if (role.GetRoleInfo()?.IntroSound is AudioClip introSound)
            {
                PlayerControl.LocalPlayer.Data.Role.IntroSound = introSound;
            }

            switch (role.GetCustomRoleTypes())
            {
                case CustomRoleTypes.Neutral:
                    __instance.TeamTitle.text = GetString("Neutral");
                    __instance.TeamTitle.color = Palette.DisabledGrey;
                    __instance.ImpostorText.gameObject.SetActive(true);
                    __instance.ImpostorText.text = GetString("NeutralInfo");
                    if (pc.Is(CountTypes.MilkyWay)) StartFadeIntro(__instance, Palette.DisabledGrey, StringHelper.CodeColor(Roles.Neutral.Vega.TeamColor), 2000);
                    else if (!pc.Is(CustomRoles.Amnesia)) StartFadeIntro(__instance, Palette.DisabledGrey, UtilsRoleText.GetRoleColor(role), 2000);
                    else __instance.BackgroundBar.material.color = Palette.DisabledGrey;
                    break;
                case CustomRoleTypes.Madmate:
                    __instance.TeamTitle.text = pc.Is(CustomRoles.Amnesia) ? GetString("Neutral") : GetString("Madmate");
                    __instance.TeamTitle.color = pc.Is(CustomRoles.Amnesia) ? Palette.DisabledGrey : UtilsRoleText.GetRoleColor(CustomRoles.Madmate);
                    __instance.ImpostorText.gameObject.SetActive(true);
                    __instance.ImpostorText.text = pc.Is(CustomRoles.Amnesia) ? GetString("NeutralInfo") : GetString("MadmateInfo");
                    if (!pc.Is(CustomRoles.Amnesia)) StartFadeIntro(__instance, Palette.CrewmateBlue, Palette.ImpostorRed, 2000);
                    else __instance.BackgroundBar.material.color = Palette.DisabledGrey;
                    break;
            }
            if (role is CustomRoles.Amnesiac) role = Amnesiac.IsWolf ? CustomRoles.WolfBoy : CustomRoles.Sheriff;
            switch (role)
            {
                case CustomRoles.Sheriff:
                    __instance.BackgroundBar.material.color = Palette.CrewmateBlue;
                    __instance.ImpostorText.gameObject.SetActive(true);
                    var numImpostors = Main.NormalOptions.NumImpostors;
                    var text = numImpostors == 1
                        ? GetString(StringNames.NumImpostorsS)
                        : string.Format(GetString(StringNames.NumImpostorsP), numImpostors);
                    __instance.ImpostorText.text = text.Replace("[FF1919FF]", "<#FF1919FF>").Replace("[]", "</color>");
                    break;
                case CustomRoles.WolfBoy:
                    __instance.BackgroundBar.material.color = Palette.CrewmateBlue;
                    __instance.ImpostorText.gameObject.SetActive(true);
                    var WnumImpostors = Main.NormalOptions.NumImpostors;
                    var Wtext = WnumImpostors == 1
                        ? GetString(StringNames.NumImpostorsS)
                        : string.Format(GetString(StringNames.NumImpostorsP), WnumImpostors);
                    __instance.ImpostorText.text = Wtext.Replace("[808080]", "<#808080>").Replace("[]", "</color>");
                    break;

                case CustomRoles.GM:
                    __instance.TeamTitle.text = UtilsRoleText.GetRoleName(role);
                    __instance.TeamTitle.color = UtilsRoleText.GetRoleColor(role);
                    __instance.BackgroundBar.material.color = UtilsRoleText.GetRoleColor(role);
                    __instance.ImpostorText.gameObject.SetActive(false);
                    break;

                case CustomRoles.TaskPlayerB:
                    __instance.TeamTitle.text = GetString("TaskBattle");
                    __instance.TeamTitle.color = UtilsRoleText.GetRoleColor(role);
                    __instance.BackgroundBar.material.color = UtilsRoleText.GetRoleColor(role);
                    __instance.ImpostorText.gameObject.SetActive(PlayerCatch.AllPlayerControls.Count() == 1);
                    if (PlayerCatch.AllPlayerControls.Count() == 1) __instance.ImpostorText.text = GetString("TaskRTAInfo");
                    break;
            }
            if (SuddenDeathMode.NowSuddenDeathMode)
            {
                __instance.TeamTitle.text = GetString("SuddenDeath");
                __instance.TeamTitle.color = StringHelper.CodeColor("#db5837");
                __instance.BackgroundBar.material.color = StringHelper.CodeColor("#db5837");
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = GetString("SuddenDeathIntro");
            }

            /*if (Input.GetKey(KeyCode.RightShift))
            {
                __instance.TeamTitle.text = "<size=13>" + Main.ModName;
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = "";
                __instance.TeamTitle.color = Color.cyan;
                StartFadeIntro(__instance, Color.blue, Color.cyan);
            }
            if (Input.GetKey(KeyCode.RightControl))
            {
                __instance.TeamTitle.text = "Discord Server";
                __instance.ImpostorText.gameObject.SetActive(true);
                __instance.ImpostorText.text = "https://discord.gg/v8SFfdebpz";
                __instance.TeamTitle.color = Color.magenta;
                StartFadeIntro(__instance, Color.magenta, Color.magenta);
            }*/

            if (IsMisidentify)
            {
                role = missrole;
                if (role.GetRoleInfo()?.IntroSound is AudioClip intro)
                {
                    PlayerControl.LocalPlayer.Data.Role.IntroSound = intro;
                }
            }
            if (pc.Is(CustomRoles.Amnesia))
            {
                PlayerControl.LocalPlayer.Data.Role.IntroSound = Amnesia.dontcanUseability ?
                (role.IsImpostor() ? RoleBase.GetIntrosound(RoleTypes.Impostor) : RoleBase.GetIntrosound(RoleTypes.Crewmate)) : RoleBase.GetIntrosound(role.GetRoleInfo().BaseRoleType.Invoke());
            }
        }
        private static async void StartFadeIntro(IntroCutscene __instance, Color start, Color end, int t = 1000)
        {
            __instance.BackgroundBar.material.color = start;
            await Task.Delay(t);
            int milliseconds = 0;
            while (true)
            {
                await Task.Delay(20);
                milliseconds += 20;
                float time = (float)milliseconds / (float)500;
                Color LerpingColor = Color.Lerp(start, end, time);
                if (__instance == null || milliseconds > 500)
                {
                    Logger.Info("ループを終了します", "StartFadeIntro");
                    break;
                }
                __instance.BackgroundBar.material.color = LerpingColor;
            }
        }
    }
    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.BeginImpostor))]
    class BeginImpostorPatch
    {
        public static bool Prefix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
        {
            if (PlayerControl.LocalPlayer.Is(CustomRoles.Sheriff) || PlayerControl.LocalPlayer.Is(CustomRoles.WolfBoy) || PlayerControl.LocalPlayer.Is(CustomRoles.BakeCat) || PlayerControl.LocalPlayer.Is(CustomRoles.Amnesiac) || (PlayerControl.LocalPlayer.GetCustomRole().GetRoleInfo()?.IsDesyncImpostor == true) && PlayerControl.LocalPlayer.Is(CustomRoles.Amnesia))
            {
                //シェリフの場合はキャンセルしてBeginCrewmateに繋ぐ
                yourTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
                yourTeam.Add(PlayerControl.LocalPlayer);
                foreach (var pc in PlayerCatch.AllPlayerControls)
                {
                    if (!pc.AmOwner) yourTeam.Add(pc);
                }
                __instance.BeginCrewmate(yourTeam);
                __instance.overlayHandle.color = Palette.CrewmateBlue;
                return false;
            }
            BeginCrewmatePatch.Prefix(__instance, ref yourTeam);
            if (PlayerControl.LocalPlayer.GetCustomRole().IsImpostor())
            {
                foreach (var pc in PlayerCatch.AllPlayerControls)
                {
                    if (pc == null) continue;
                    if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
                    if (pc.Is(CustomRoles.Amnesiac))
                    {
                        RoleManager.Instance.SetRole(pc, RoleTypes.Impostor);
                        yourTeam.Add(pc);
                    }
                    if (pc.Is(CustomRoles.OneWolf))
                    {
                        yourTeam.Remove(pc);
                    }
                }
                if (PlayerControl.LocalPlayer.Is(CustomRoles.OneWolf))
                {
                    yourTeam = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
                    yourTeam.Add(PlayerControl.LocalPlayer);
                }
            }
            return true;
        }
        public static void Postfix(IntroCutscene __instance, ref Il2CppSystem.Collections.Generic.List<PlayerControl> yourTeam)
        {
            BeginCrewmatePatch.Postfix(__instance, ref yourTeam);
        }
    }
    [HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.OnDestroy))]//android、このPatch適応されないっぽい。
    class IntroCutsceneDestroyPatch
    {
        public static void Postfix(IntroCutscene __instance)
        {
            if (!GameStates.IsInGame) return;

            GameStates.introDestroyed = true;
            var mapId = Main.NormalOptions.MapId;
            // エアシップではまだ湧かない
            if ((MapNames)mapId != MapNames.Airship)
            {
                foreach (var state in PlayerState.AllPlayerStates.Values)
                {
                    state.HasSpawned = true;
                }
            }
            if (AmongUsClient.Instance.AmHost)
            {
                if (mapId != 4)
                {
                    if (SuddenDeathMode.SuddenKillcooltime.GetBool() && SuddenDeathMode.NowSuddenDeathMode)
                    {
                        _ = new LateTask(() =>
                        {
                            PlayerCatch.AllPlayerControls.Do(pc => pc.SetKillCooldown(SuddenDeathMode.SuddenKillcooltime.GetFloat() - 0.7f, delay: true));
                        }, 0.7f, "FixKillCooldownTask", null);
                    }
                    else if (Options.FixFirstKillCooldown.GetBool())
                        _ = new LateTask(() =>
                        {
                            PlayerCatch.AllPlayerControls.Do(pc => pc.SetKillCooldown((Main.AllPlayerKillCooldown.TryGetValue(pc.PlayerId, out var time) ? time : Main.LastKillCooldown.Value) - 0.7f, force: true, delay: true));
                        }, 0.7f, "FixKillCooldownTask", null);
                    else _ = new LateTask(() =>
                        {
                            PlayerCatch.AllPlayerControls.Do(pc => pc.SetKillCooldown(10f, force: true, delay: true));
                        }, 0.7f, "FixKillCooldownTask", null);
                    GameStates.Intro = false;
                    GameStates.AfterIntro = true;
                }
                if (!(Options.CurrentGameMode is CustomGameMode.Standard && Main.SetRoleOverride))
                    _ = new LateTask(() => PlayerCatch.AllPlayerControls.Do(pc => pc.RpcSetRoleDesync(RoleTypes.Shapeshifter, -3)), 2f, "SetImpostorForServer");
                if (PlayerControl.LocalPlayer.Is(CustomRoles.GM))
                {
                    PlayerControl.LocalPlayer.RpcExile();
                    PlayerState.GetByPlayerId(PlayerControl.LocalPlayer.PlayerId).SetDead();
                }

                if (RandomSpawn.IsRandomSpawn())
                {
                    RandomSpawn.SpawnMap map;
                    switch (mapId)
                    {
                        case 0:
                            map = new RandomSpawn.SkeldSpawnMap();
                            PlayerCatch.AllPlayerControls.Do(map.RandomTeleport);
                            break;
                        case 1:
                            map = new RandomSpawn.MiraHQSpawnMap();
                            PlayerCatch.AllPlayerControls.Do(map.RandomTeleport);
                            break;
                        case 2:
                            map = new RandomSpawn.PolusSpawnMap();
                            PlayerCatch.AllPlayerControls.Do(map.RandomTeleport);
                            break;
                        case 5:
                            map = new RandomSpawn.FungleSpawnMap();
                            PlayerCatch.AllPlayerControls.Do(map.RandomTeleport);
                            break;
                    }
                }

                foreach (var kvp in PlayerState.AllPlayerStates)
                {
                    kvp.Value.IsBlackOut = false;
                }
                UtilsOption.MarkEveryoneDirtySettings();
                //役職選定後に処理する奴。
                foreach (var pc in PlayerCatch.AllPlayerControls)
                {
                    var role = pc.GetCustomRole();
                    if (SuddenDeathMode.NowSuddenDeathMode && !SuddenDeathMode.NowSuddenDeathTemeMode)
                    {
                        NameColorManager.RemoveAll(pc.PlayerId);
                        PlayerCatch.AllPlayerControls.DoIf(pl => pl != pc, pl => NameColorManager.Add(pc.PlayerId, pl.PlayerId, Main.PlayerColors[pl.PlayerId].ColorCode()));
                    }
                    //マッドメイトの最初からの内通
                    if (role.IsMadmate() && Options.MadCanSeeImpostor.GetBool())
                    {
                        if (PlayerCatch.AllPlayerFirstTypes.Where(x => x.Value is CustomRoleTypes.Impostor).Any())
                            foreach (var imp in PlayerCatch.AllPlayerFirstTypes.Where(x => x.Value is CustomRoleTypes.Impostor))
                            {
                                var iste = PlayerState.GetByPlayerId(imp.Key);
                                if (iste.TargetColorData.ContainsKey(pc.PlayerId)) NameColorManager.Remove(pc.PlayerId, imp.Key);
                                NameColorManager.Add(pc.PlayerId, imp.Key, "ff1919");
                            }
                    }
                }

                // そのままだとホストのみDesyncImpostorの暗室内での視界がクルー仕様になってしまう
                var roleInfo = PlayerControl.LocalPlayer.GetCustomRole().GetRoleInfo();
                var amDesyncImpostor = roleInfo?.IsDesyncImpostor == true;
                if (amDesyncImpostor && PlayerControl.LocalPlayer.GetCustomRole() is not CustomRoles.BakeCat)
                {
                    PlayerControl.LocalPlayer.Data.Role.AffectedByLightAffectors = false;
                }
                GameStates.task = true;
                Logger.Info("タスクフェイズ開始", "Phase");

                //desyneインポかつ置き換えがimp以外ならそれにする。
                if ((roleInfo?.IsDesyncImpostor == true || SuddenDeathMode.NowSuddenDeathMode) && roleInfo.BaseRoleType.Invoke() != RoleTypes.Impostor)
                    RoleManager.Instance.SetRole(PlayerControl.LocalPlayer, roleInfo.BaseRoleType.Invoke());

                if (!Options.EnableGM.GetBool() && Options.CurrentGameMode == CustomGameMode.TaskBattle && TaskBattle.TaskBattleCanVent.GetBool())
                    RoleManager.Instance.SetRole(PlayerControl.LocalPlayer, RoleTypes.Engineer);

                RemoveDisableDevicesPatch.UpdateDisableDevices();

                _ = new LateTask(() =>
                {
                    CustomRoleManager.AllActiveRoles.Values.Do(role => role.StartGameTasks());
                    foreach (var pl in PlayerCatch.AllPlayerControls)
                    {
                        if (pl.Is(CustomRoles.Amnesiac))
                            pl.Data.Role.NameColor = Palette.White;
                        if (pl.Is(CustomRoles.OneWolf))
                            pl.Data.Role.NameColor = Palette.White;
                        List<uint> TaskList = new();
                        if (pl.Data.Tasks != null)
                            foreach (var task in pl.Data.Tasks) TaskList.Add(task.Id);
                        Main.AllPlayerTask.TryAdd(pl.PlayerId, TaskList);
                        if (pl.isDummy && Main.NormalOptions.MapId is 4)
                        {
                            new RandomSpawn.AirshipSpawnMap().RandomTeleport(pl);
                        }
                    }
                    ExtendedRpc.RpcResetAbilityCooldownAllPlayer();
                    CustomButtonHud.BottonHud(true);
                }, 0.3f, "setnames", true);

                _ = new LateTask(() =>
                {
                    CustomRoleManager.AllActiveRoles.Values.Do(role => role.ChangeColor());
                    UtilsNotifyRoles.NotifyRoles(ForceLoop: true);
                    SuddenDeathMode.NotTeamKill();
                    ExtendedRpc.AllPlayerOnlySeeMePet();
                }, 1.25f, "setcolorandpet", true);

                if (Options.firstturnmeeting)
                {
                    _ = new LateTask(() =>
                        ReportDeadBodyPatch.ExReportDeadBody(PlayerControl.LocalPlayer, PlayerControl.LocalPlayer.Data, false, GetString("Firstmeetinginfo"), "#23dbc0"), 0.7f + Main.LagTime, "", true);
                }
                else
                {
                    _ = new LateTask(() =>
                    {
                        Main.ShowRoleIntro = false;
                        if (GameStates.InGame && !GameStates.CalledMeeting)
                            UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);
                    }, 15f, "Intro", true);
                }
            }
            _ = new LateTask(() => Main.showkillbutton = true, 0.5f, "", true);
            Logger.Info("OnDestroy", "IntroCutscene");
        }
    }
}