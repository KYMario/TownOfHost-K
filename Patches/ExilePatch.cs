using AmongUs.Data;
using AmongUs.GameOptions;
using HarmonyLib;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Impostor;
using TownOfHost.Roles.Neutral;

namespace TownOfHost
{
    class ExileControllerWrapUpPatch
    {
        public static NetworkedPlayerInfo AntiBlackout_LastExiled;

        [HarmonyPatch(typeof(ExileController), nameof(ExileController.WrapUp))]
        class BaseExileControllerPatch
        {
            public static void Postfix(ExileController __instance)
            {
                try
                {
                    WrapUpPostfix(__instance.initData.networkedPlayer);
                }
                finally
                {
                    WrapUpFinalizer(__instance.initData.networkedPlayer);
                }
            }
        }
        [HarmonyPatch(typeof(AirshipExileController), nameof(AirshipExileController.Animate))]
        class AirshipStatusPatch
        {
            public static void Postfix(AirshipExileController __instance, ref Il2CppSystem.Collections.IEnumerator __result)
            {
                //WrapUpAndSpawnに直接パッチが当たらないのでAnimateメソッド中にパッチを当てる
                var pathcer = new CoroutinPatcher(__result);
                //WrapUpAndSpawnはステートマシンとしてクラス化されているためそのクラス実行前にパッチを当てる
                //元々Postfixだが、タイミング的にはPrefixの方が適切なのでPrefixに当てる
                pathcer.AddPrefix(typeof(AirshipExileController), nameof(AirshipExileController.WrapUpAndSpawn), () =>
                    AirshipExileControllerPatch.Postfix(__instance)
                );
                __result = pathcer.EnumerateWithPatch();
            }
        }
        public class AirshipExileControllerPatch
        {
            public static void Postfix(AirshipExileController __instance)
            {
                try
                {
                    WrapUpPostfix(__instance.initData.networkedPlayer);
                }
                finally
                {
                    WrapUpFinalizer(__instance.initData.networkedPlayer);
                }
            }
        }
        static void WrapUpPostfix(NetworkedPlayerInfo exiled)
        {
            if (AntiBlackout.OverrideExiledPlayer())
            {
                exiled = AntiBlackout_LastExiled;
            }

            var mapId = Main.NormalOptions.MapId;

            // エアシップではまだ湧かない
            if ((MapNames)mapId != MapNames.Airship)
            {
                foreach (var state in PlayerState.AllPlayerStates.Values)
                {
                    state.HasSpawned = true;
                }
            }

            bool DecidedWinner = false;
            if (!AmongUsClient.Instance.AmHost) return; //ホスト以外はこれ以降の処理を実行しません
            AntiBlackout.RestoreIsDead(doSend: false);
            if (exiled != null)
            {
                var role = exiled.GetCustomRole();
                var info = role.GetRoleInfo();

                exiled.IsDead = true;
                PlayerState.GetByPlayerId(exiled.PlayerId).DeathReason = CustomDeathReason.Vote;

                foreach (var roleClass in CustomRoleManager.AllActiveRoles.Values)
                {
                    roleClass.OnExileWrapUp(exiled, ref DecidedWinner);
                }

                if (CustomWinnerHolder.WinnerTeam != CustomWinner.Terrorist) PlayerState.GetByPlayerId(exiled.PlayerId).SetDead();
            }
            AfterMeetingTasks();
        }
        public static void AfterMeetingTasks()
        {
            //霊界用暗転バグ処置(移設)
            if (AmongUsClient.Instance.AmHost)
            {
                if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Default) return;

                foreach (var pc in PlayerCatch.AllPlayerControls)
                {
                    pc.ResetKillCooldown();
                }
                if (RandomSpawn.IsRandomSpawn())
                {
                    RandomSpawn.SpawnMap map;
                    switch (Main.NormalOptions.MapId)
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
                FallFromLadder.Reset();
                PlayerCatch.CountAlivePlayers(true);
                Utils.AfterMeetingTasks();
                if (Main.NormalOptions.MapId != 4)
                {
                    foreach (var pc in PlayerCatch.AllPlayerControls)
                    {
                        pc.GetRoleClass()?.OnSpawn();
                    }
                }
            }
        }
        static void WrapUpFinalizer(NetworkedPlayerInfo exiled)
        {
            //WrapUpPostfixで例外が発生しても、この部分だけは確実に実行されます。
            if (AmongUsClient.Instance.AmHost)
            {
                _ = new LateTask(() =>
                {
                    exiled = AntiBlackout_LastExiled;
                    //AntiBlackout.SendGameData();
                }, 0.5f, "Restore IsDead Task");
                _ = new LateTask(() =>
                {
                    if (AntiBlackout.OverrideExiledPlayer() && // 追放対象が上書きされる状態 (上書きされない状態なら実行不要)
                        exiled != null && //exiledがnullでない
                        exiled.Object != null) //exiled.Objectがnullでない
                    {
                        exiled.Object.RpcExileV2();
                    }

                    if (Options.ExAftermeetingflash.GetBool())
                        Utils.AllPlayerKillFlash();

                    if (Main.NormalOptions.MapId is not 4 || AntiBlackout.OverrideExiledPlayer())
                    {
                        PlayerCatch.AllPlayerControls.Do(pc =>
                        {
                            AntiBlackout.ResetSetRole(pc);
                        });
                    }

                    GameStates.task = true;
                    Logger.Info("タスクフェイズ開始", "Phase");
                }, 0.52f, "AfterMeetingDeathPlayers Task");
                _ = new LateTask(() =>
                {
                    Main.AfterMeetingDeathPlayers.Do(x =>
                    {
                        var player = PlayerCatch.GetPlayerById(x.Key);
                        var roleClass = CustomRoleManager.GetByPlayerId(x.Key);
                        var requireResetCam = player?.GetCustomRole().GetRoleInfo()?.IsDesyncImpostor == true;
                        var state = PlayerState.GetByPlayerId(x.Key);
                        Logger.Info($"{player.GetNameWithRole().RemoveHtmlTags()}を{x.Value}で死亡させました", "AfterMeetingDeath");
                        state.DeathReason = x.Value;
                        state.SetDead();
                        player?.RpcExileV2();
                        if (x.Value == CustomDeathReason.Suicide)
                            player?.SetRealKiller(player, true);
                        if (requireResetCam)
                            player?.ResetPlayerCam(1f);
                        if (roleClass is Executioner executioner && executioner.TargetId == x.Key)
                            Executioner.ChangeRoleByTarget(x.Key);
                    });
                    Main.AfterMeetingDeathPlayers.Clear();
                }, 0.6f, "AfterMeetingDeathPlayer", null);
            }
            if (Main.NormalOptions.MapId is 4 && !AntiBlackout.OverrideExiledPlayer())
            {
                _ = new LateTask(() =>
                {
                    if (CustomWinnerHolder.WinnerTeam is CustomWinner.Default)
                        AntiBlackout.isRoleCache.Do(id =>
                        {
                            var pc = PlayerCatch.GetPlayerById(id);
                            if (pc == null) return;
                            AntiBlackout.ResetSetRole(pc);
                            pc.KillFlash(true);
                        });
                }, 11.5f, "BlackoutPlayerSetRole", null);
            }

            UtilsGameLog.WriteGameLog();
            GameStates.AlreadyDied |= !PlayerCatch.IsAllAlive;
            RemoveDisableDevicesPatch.UpdateDisableDevices();
            SoundManager.Instance.ChangeAmbienceVolume(DataManager.Settings.Audio.AmbienceVolume);
            //ExtendedPlayerControl.RpcResetAbilityCooldownAllPlayer();
            Main.CanUseAbility = true;
            MeetingStates.First = false;

            var roleInfo = PlayerControl.LocalPlayer.GetCustomRole().GetRoleInfo();
            var role = (roleInfo?.IsDesyncImpostor == true) && roleInfo.BaseRoleType.Invoke() is RoleTypes.Impostor ? RoleTypes.Crewmate : (roleInfo?.BaseRoleType?.Invoke() ?? RoleTypes.Crewmate);

            if (!PlayerControl.LocalPlayer.IsAlive())
            {
                role = role.IsCrewmate() ? RoleTypes.CrewmateGhost : RoleTypes.ImpostorGhost;
            }
            RoleManager.Instance.SetRole(PlayerControl.LocalPlayer, role);
            //ここで処刑処理を入れると暗転が起こる?

            _ = new LateTask(() => GameStates.ExiledAnimate = false, 3f + Main.LagTime, "Tuihoufin");
        }
    }

    [HarmonyPatch(typeof(PbExileController), nameof(PbExileController.PlayerSpin))]
    class PolusExileHatFixPatch
    {
        public static void Prefix(PbExileController __instance)
        {
            __instance.Player.cosmetics.hat.transform.localPosition = new(-0.2f, 0.6f, 1.1f);
        }
    }
    [HarmonyPatch(typeof(ExileController), nameof(ExileController.Begin))]
    class ExileControllerBeginPatch
    {
        // オートミュートの関係でMeetingHudの追放者を偽装しているので
        // ここで導入者の追放者を戻す
        public static bool SecondBegin = false;
        public static bool Prefix(ExileController __instance, ExileController.InitProperties init)
        {
            if (PlayerCatch.AllPlayersCount < 4)
            {
                __instance.completeString = Translator.GetString(StringNames.NoExileTie);
                return true;
            }
            var result = AntiBlackout.voteresult;
            var modinit = init;

            if (result.HasValue)
            {
                if (result.Value.Exiled is not null)
                {
                    if (SecondBegin)
                    {
                        __instance.completeString = string.Format(Translator.GetString(StringNames.ExileTextNonConfirm), result.Value.Exiled.GetLogPlayerName());
                        SecondBegin = false;
                        return true;
                    }
                    modinit.networkedPlayer = result.Value.Exiled;
                    modinit.outfit = Camouflage.PlayerSkins.TryGetValue(result.Value.Exiled.PlayerId, out var skin) ? skin : result.Value.Exiled.DefaultOutfit;
                    modinit.voteTie = false;
                    SecondBegin = true;
                    __instance.Begin(modinit);
                    return false;
                }
            }
            return true;
        }
        public static void Postfix(ExileController __instance)
        {
            var result = AntiBlackout.voteresult;

            if (result.HasValue)
            {
                if (result.Value.Exiled is null)
                {
                    if (result.Value.IsTie)
                    {
                        __instance.completeString = Translator.GetString(StringNames.NoExileTie);
                    }
                    else
                    {
                        __instance.completeString = Translator.GetString(StringNames.NoExileSkip);
                    }
                }
                if (result.Value.Exiled.Object?.GetRoleClass() is Assassin && Assassin.NowUse)
                {
                    __instance.completeString = MeetingVoteManager.Voteresult + "<size=0>";
                }
            }
            SecondBegin = false;
        }
    }
}
