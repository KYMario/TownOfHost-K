using System.Collections.Generic;
using System.Linq;

using Hazel;
using HarmonyLib;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.Ghost;
using TownOfHost.Roles.AddOns.Common;
using TownOfHost.Patches.ISystemType;

namespace TownOfHost

{
    #region  ShapeShift
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckShapeshift))]
    public static class PlayerControlCheckShapeshiftPatch
    {
        private static readonly LogHandler logger = Logger.Handler(nameof(PlayerControl.CheckShapeshift));

        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target, [HarmonyArgument(1)] bool shouldAnimate)
        {
            if (Main.EditMode && GameStates.IsFreePlay)
            {
                CustomSpawnEditor.CheckShapeshift(__instance, target);
                return false;
            }

            if (AmongUsClient.Instance.IsGameOver || !AmongUsClient.Instance.AmHost)
            {
                return false;
            }

            var roleclass = __instance.GetRoleClass();

            // 無効な変身を弾く．これより前に役職等の処理をしてはいけない
            if (!CheckInvalidShapeshifting(__instance, target, shouldAnimate))
            {
                __instance.RpcRejectShapeshift();
                return false;
            }

            var shapeshifter = __instance;
            var shapeshifting = shapeshifter.PlayerId != target.PlayerId;
            // 変身したとき一番近い人をマッドメイトにする処理
            if (shapeshifter.CanMakeMadmate() && shapeshifting)
            {
                var sidekickable = roleclass as ISidekickable;
                var targetRole = sidekickable?.SidekickTargetRole ?? CustomRoles.SKMadmate;

                //var targetm = shapeshifter.GetKillTarget();
                Vector2 shapeshifterPosition = shapeshifter.transform.position;//変身者の位置
                Dictionary<PlayerControl, float> mpdistance = new();
                float dis;
                foreach (var pc in PlayerCatch.AllAlivePlayerControls)
                {
                    if ((pc.Data.Role.Role != RoleTypes.Shapeshifter || pc.GetCustomRole().GetRoleInfo()?.BaseRoleType.Invoke() != RoleTypes.Shapeshifter) && !pc.Is(CustomRoleTypes.Impostor) && !pc.Is(targetRole))
                    {
                        dis = Vector2.Distance(shapeshifterPosition, pc.transform.position);
                        mpdistance.Add(pc, dis);
                    }
                }
                if (mpdistance.Count != 0)

                //if (targetm != null && !targetm.Is(targetRole) && !targetm.Is(CustomRoleTypes.Impostor))
                {
                    //shapeshifter.RpcShapeshift(shapeshifter, false);
                    var min = mpdistance.OrderBy(c => c.Value).FirstOrDefault();//一番値が小さい
                    PlayerControl targetm = min.Key;
                    if (!targetm.Is(CustomRoles.King) && !targetm.Is(CustomRoles.Merlin))
                    {
                        if (SuddenDeathMode.NowSuddenDeathTemeMode)
                        {
                            targetm.SideKickChangeTeam(shapeshifter);
                        }
                        UtilsGameLog.AddGameLog("SideKick", string.Format(Translator.GetString("log.Sidekick"), UtilsName.GetPlayerColor(targetm, true) + $"({UtilsRoleText.GetTrueRoleName(targetm.PlayerId)})", UtilsName.GetPlayerColor(shapeshifter, true)));
                        targetm.RpcSetCustomRole(targetRole);
                        Logger.Info($"Make SKMadmate:{targetm.name}", "Shapeshift");
                        PlayerCatch.SKMadmateNowCount++;
                        shapeshifter.RpcProtectedMurderPlayer(targetm);
                        targetm.RpcProtectedMurderPlayer(shapeshifter);
                        targetm.RpcProtectedMurderPlayer(targetm);

                        target.RpcSetRole(Options.SkMadCanUseVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate);
                        NameColorManager.Add(targetm.PlayerId, shapeshifter.PlayerId, "#ff1919");
                        if (!Utils.RoleSendList.Contains(targetm.PlayerId)) Utils.RoleSendList.Add(targetm.PlayerId);
                        PlayerState.GetByPlayerId(targetm.PlayerId).SetCountType(CountTypes.Crew);
                        UtilsGameLog.LastLogRole[targetm.PlayerId] += "<b>⇒" + Utils.ColorString(UtilsRoleText.GetRoleColor(targetm.GetCustomRole()), Translator.GetString($"{targetm.GetCustomRole()}")) + "</b>" + UtilsRoleText.GetSubRolesText(targetm.PlayerId);
                        UtilsOption.MarkEveryoneDirtySettings();
                        UtilsNotifyRoles.NotifyRoles();
                        //shapeshifter.RpcRejectShapeshift();
                        //return false;
                    }
                }
            }
            // 役職の処理
            if (Amnesia.CheckAbility(shapeshifter))
                if (roleclass?.CheckShapeshift(target, ref shouldAnimate) == false && !MeetingHud.Instance)
                {
                    if (roleclass.CanDesyncShapeshift)
                    {
                        shouldAnimate &= !MeetingHud.Instance;
                        shapeshifter.RpcSpecificRejectShapeshift(target, shouldAnimate);
                    }
                    else
                    {
                        shapeshifter.RpcRejectShapeshift();
                    }
                    return false;
                }
            shouldAnimate &= !MeetingHud.Instance;

            shapeshifter.RpcShapeshift(target, shouldAnimate);
            return false;
        }
        private static bool CheckInvalidShapeshifting(PlayerControl instance, PlayerControl target, bool animate)
        {
            logger.Info($"Checking shapeshift {instance.GetNameWithRole().RemoveHtmlTags()} -> {(target == null || target.Data == null ? "(null)" : target.GetNameWithRole().RemoveHtmlTags())} (animate:{animate})");

            if (!target || target.Data == null)
            {
                logger.Info("targetがnullのため変身をキャンセルします");
                return false;
            }
            if (!instance.IsAlive())
            {
                logger.Info("変身者が死亡しているため変身をキャンセルします");
                return false;
            }
            if (instance.Is(CustomRoles.SKMadmate) || instance.Is(CustomRoles.Jackaldoll))
            {
                logger.Info("変身者がサイドキックされてるため変身をキャンセルします");
                return false;
            }
            // RoleInfoによるdesyncシェイプシフター用の判定を追加
            if (instance.Data.Role.Role != RoleTypes.Shapeshifter && instance.GetCustomRole().GetRoleInfo()?.BaseRoleType?.Invoke() != RoleTypes.Shapeshifter)
            {
                logger.Info("変身者がシェイプシフターではないため変身をキャンセルします");
                return false;
            }
            if (instance.Data.Disconnected)
            {
                logger.Info("変身者が切断済のため変身をキャンセルします");
                return false;
            }
            if (target.IsMushroomMixupActive() && animate)
            {
                logger.Info("キノコカオス中のため変身をキャンセルします");
                return false;
            }
            if (MeetingHud.Instance && animate)
            {
                logger.Info("会議中のため変身をキャンセルします");
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Shapeshift))]
    class ShapeshiftPatch
    {
        public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target, [HarmonyArgument(1)] bool animate)
        {
            Logger.Info($"{__instance?.GetNameWithRole().RemoveHtmlTags()} => {target?.GetNameWithRole().RemoveHtmlTags()}", "Shapeshift");

            var shapeshifter = __instance;
            var shapeshifting = shapeshifter.PlayerId != target.PlayerId;

            if (Main.CheckShapeshift.TryGetValue(shapeshifter.PlayerId, out var last) && last == shapeshifting)
            {
                Logger.Info($"{__instance?.GetNameWithRole().RemoveHtmlTags()}:Cancel Shapeshift.Prefix", "Shapeshift");
                return;
            }

            Main.CheckShapeshift[shapeshifter.PlayerId] = shapeshifting;
            Main.ShapeshiftTarget[shapeshifter.PlayerId] = target.PlayerId;

            shapeshifter.GetRoleClass()?.OnShapeshift(target);
            if (GameStates.IsMeeting) return;

            if (!AmongUsClient.Instance.AmHost) return;

            _ = new LateTask(() =>
            {
                foreach (var role in CustomRoleManager.AllActiveRoles.Values)
                {
                    role.ChangeColor();
                }
            }, 1.2f, "", true);

            if (!shapeshifting)
            {
                Camouflage.RpcSetSkin(shapeshifter);
            }
            _ = new LateTask(() =>
                shapeshifter.OnlySeeMyPet(target.Data.DefaultOutfit.PetId), animate ? 1.2f : 0.1f, "ShapeSetPet", null);

            //変身解除のタイミングがずれて名前が直せなかった時のために強制書き換え
            if (!shapeshifting)
            {
                _ = new LateTask(() =>
                {
                    UtilsNotifyRoles.NotifyRoles(NoCache: true);
                },
                1.2f, "ShapeShiftNotify");
            }
        }
    }
    #endregion
    #region Phantom
    //[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckVanish))]
    class PlayerControlPhantomPatch
    {
        public static bool CheckVanish(PlayerControl __instance)
        {
            if (__instance.PlayerId == PlayerControl.LocalPlayer.PlayerId) return false;
            var AdjustKillCooldown = true;
            bool? ResetCooldown = true;

            if (__instance.GetRoleClass() is IUsePhantomButton iusephantombutton && Main.CanUseAbility)
                iusephantombutton.CheckOnClick(ref AdjustKillCooldown, ref ResetCooldown);

            float TurnTimer = 0;
            IUsePhantomButton.IPPlayerKillCooldown.TryGetValue(__instance.PlayerId, out TurnTimer);
            Main.AllPlayerKillCooldown.TryGetValue(__instance.PlayerId, out var killcool);
            /* 初手で、キルク修正がオフでキルクが10s以上で、10sのまま*/
            if (MeetingStates.FirstMeeting && !Options.FixFirstKillCooldown.GetBool() && killcool > 10 &&
            (PlayerState.GetByPlayerId(__instance.PlayerId)?.Is10secKillButton == true))
                killcool = 10;
            float cooldown = killcool - TurnTimer;
            if (cooldown <= 1) cooldown = 0.005f;

            if (AdjustKillCooldown)
            {
                Main.AllPlayerKillCooldown[__instance.PlayerId] = cooldown < 10 ? cooldown : cooldown * 2;
                __instance.SyncSettings();
            }
            var writer = CustomRpcSender.Create("Phantom OneClick", SendOption.None);
            writer.StartMessage(__instance.GetClientId());

            /*モーションのせいで1.5秒位ベント、キル不可になるのが...
            writer.StartRpc(__instance.NetId, (byte)RpcCalls.StartVanish)
                .EndRpc();
            writer.StartRpc(__instance.NetId, (byte)RpcCalls.StartAppear)
                .Write(true)//falseにしたら動かない。多分ベント用
                .EndRpc();*/
            if (ResetCooldown is not null)
            {
                writer.StartRpc(__instance.NetId, (byte)RpcCalls.SetRole)
                    .Write((ushort)RoleTypes.Impostor)
                    .Write(true)
                    .EndRpc();
                if ((__instance.GetRoleClass() as IUsePhantomButton)?.IsPhantomRole is true)
                {
                    writer.StartRpc(__instance.NetId, (byte)RpcCalls.SetRole)
                        .Write((ushort)RoleTypes.Phantom)
                        .Write(true)
                        .EndRpc();
                }
            }
            if (AdjustKillCooldown && 10 < cooldown)
            {
                writer.StartRpc(__instance.NetId, (byte)RpcCalls.MurderPlayer)
                    .WriteNetObject(__instance)
                    .Write((int)MurderResultFlags.FailedProtected)
                    .EndRpc();
            }
            if (ResetCooldown is true && (__instance.GetRoleClass() as IUsePhantomButton)?.IsPhantomRole is true)
            {
                writer.StartRpc(__instance.NetId, (byte)RpcCalls.ProtectPlayer)
                    .WriteNetObject(__instance)
                    .Write(0)
                    .EndRpc();
            }
            writer.EndMessage();
            writer.SendMessage();
            __instance.ResetKillCooldown();

            return false;
        }
    }
    #endregion
    #region  Vent

    [HarmonyPatch(typeof(Vent), nameof(Vent.Use))]
    class OcCoVentUsePatch
    {
        public static bool Prefix(Vent __instance)
        {
            var user = PlayerControl.LocalPlayer;
            var id = __instance.Id;
            if (CoEnterVentPatch.VentPlayers.ContainsKey(user.PlayerId)) return true;

            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek && Options.IgnoreVent.GetBool())
                return false;

            if (user.Is(CustomRoles.DemonicVenter)) return true;

            var roleClass = user.GetRoleClass();
            var pos = __instance.transform.position;
            if (Amnesia.CheckAbilityreturn(user)) roleClass = null;
            if ((!roleClass?.OnEnterVent(user.MyPhysics, id) ?? false) || !CoEnterVentPatch.CanUse(user.MyPhysics, id))
            {
                if (Options.CurrentGameMode is not CustomGameMode.TaskBattle)
                {
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(user.MyPhysics.NetId, (byte)RpcCalls.BootFromVent, SendOption.None, user.GetClientId());
                    writer.Write(id);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    return false;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerPhysics._CoEnterVent_d__47), nameof(PlayerPhysics._CoEnterVent_d__47.MoveNext))]
    class OcCoEnterVentPatch
    {
        public static void Prefix(PlayerPhysics._CoEnterVent_d__47 __instance, ref bool __result)
        {
            if (__instance.__1__state is not 1) return;
            if (CoEnterVentPatch.VentPlayers.ContainsKey(__instance.__4__this.myPlayer.PlayerId)) return;
            if (__instance.__4__this.myPlayer.PlayerId == PlayerControl.LocalPlayer.PlayerId)
            {
                CoEnterVentPatch.Prefix(__instance.__4__this, __instance.id);
            }
        }
    }
    [HarmonyPatch(typeof(PlayerPhysics._CoExitVent_d__48), nameof(PlayerPhysics._CoExitVent_d__48.MoveNext))]
    class OcCoExitVentPath
    {
        public static void Prefix(PlayerPhysics._CoExitVent_d__48 __instance)
        {
            if (__instance.__1__state is not 1) return;

            if (__instance.__4__this.myPlayer.PlayerId == PlayerControl.LocalPlayer.PlayerId)
            {
                ExitVentPatch.Prefix(__instance.__4__this);
            }
        }
    }

    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.CoEnterVent))]
    class CoEnterVentPatch
    {
        public static Dictionary<byte, float> VentPlayers = new();
        public static Dictionary<byte, bool> OldOnEnterVent = new();
        static bool MadBool = false;
        public static bool Prefix(PlayerPhysics __instance, [HarmonyArgument(0)] int id)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                var user = __instance.myPlayer;

                if (MadBool)
                {
                    MadBool = false;
                    if (!OldOnEnterVent.TryAdd(user.PlayerId, true))
                    {
                        OldOnEnterVent[user.PlayerId] = true;
                    }
                    return true;
                }

                if (Options.CurrentGameMode == CustomGameMode.HideAndSeek && Options.IgnoreVent.GetBool())
                    __instance.RpcBootFromVent(id);

                if (user.Is(CustomRoles.DemonicVenter))
                {
                    if (!OldOnEnterVent.TryAdd(user.PlayerId, true))
                    {
                        OldOnEnterVent[user.PlayerId] = true;
                    }
                    return true;
                }
                var roleClass = user.GetRoleClass();
                var pos = __instance.transform.position;
                if (Amnesia.CheckAbilityreturn(user)) roleClass = null;

                if (!VentilationSystemUpdateSystemPatch.NowVentId.TryAdd(user.PlayerId, (byte)id))
                {
                    VentilationSystemUpdateSystemPatch.NowVentId[user.PlayerId] = (byte)id;
                }

                if (user.IsModClient() is false)
                {
                    if ((!roleClass?.OnEnterVent(__instance, id) ?? false) || !CanUse(__instance, id))
                    {
                        if (Options.CurrentGameMode == CustomGameMode.TaskBattle) return true;
                        //一番遠いベントに追い出す
                        var canuse = CanUse(__instance, id, false);
                        if (!OldOnEnterVent.TryAdd(user.PlayerId, canuse))
                        {
                            OldOnEnterVent[user.PlayerId] = canuse;
                        }

                        foreach (var pc in PlayerCatch.AllPlayerControls)
                        {
                            if (pc == user || pc.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue; //本人とホストは別の処理
                            Dictionary<int, float> Distance = new();
                            Vector2 position = pc.transform.position;
                            //一番遠いベントを調べて送る
                            foreach (var vent in ShipStatus.Instance.AllVents)
                                Distance.Add(vent.Id, Vector2.Distance(position, vent.transform.position));
                            var ventid = Distance.OrderByDescending(x => x.Value).First().Key;
                            var sender = CustomRpcSender.Create("Farthest Vent", SendOption.None)
                                .StartMessage(pc.GetClientId())
                                .AutoStartRpc(__instance.NetId, (byte)RpcCalls.BootFromVent, pc.GetClientId())
                                .Write(ventid)
                                .EndRpc()
                                .EndMessage();
                            sender.SendMessage();
                            __instance.myPlayer.RpcSnapToForced(pos);
                        }
                        //多分負荷あれだし、テープで無理やり戻した感じだから参考にしない方がいい、

                        /*MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.BootFromVent, SendOption.None, -1);
                        writer.WritePacked(127);
                        AmongUsClient.Instance.FinishRpcImmediately(writer);*/

                        __instance.myPlayer.inVent = false;
                        _ = new LateTask(() =>
                        {
                            int clientId = user.GetClientId();
                            MessageWriter writer2 = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.BootFromVent, SendOption.None, clientId);
                            writer2.Write(id);
                            AmongUsClient.Instance.FinishRpcImmediately(writer2);
                            __instance.myPlayer.RpcSnapToForced(pos);
                            __instance.myPlayer.inVent = false;
                        }, 0.8f, "Fix DesyncImpostor Stuck", null);
                        return false;
                    }
                }
                if (OldOnEnterVent.TryAdd(__instance.myPlayer.PlayerId, true)) OldOnEnterVent[__instance.myPlayer.PlayerId] = true;

                //マッドでベント移動できない設定なら矢印を消す
                if ((!roleClass?.CanVentMoving(__instance, id) ?? false) ||
                    (user.GetCustomRole().IsMadmate() && !Options.MadmateCanMovedByVent.GetBool()))
                {
                    if (!MadBool && user.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                        MadBool = true;
                    int clientId = user.GetClientId();
                    _ = new LateTask(() =>
                    {
                        MessageWriter writer2 = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.BootFromVent, SendOption.None, clientId);
                        writer2.Write(id);
                        AmongUsClient.Instance.FinishRpcImmediately(writer2);
                    }, 0.1f, "Vent- BootFromVent", true);
                    _ = new LateTask(() =>
                    {
                        VentPlayers.TryAdd(__instance.myPlayer.PlayerId, 0);
                        MessageWriter writer2 = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.EnterVent, SendOption.None, clientId);
                        writer2.Write(id);
                        AmongUsClient.Instance.FinishRpcImmediately(writer2);
                    }, 0.25f, "Vent- EnterVent", null);
                    //BootFromVentで255とかしてもできるが、タイミングがよくわかんないので上ので今のとこはおｋ
                }
                CustomRoleManager.OnEnterVent(__instance, id);
            }
            //if (Options.MaxInVentMode.GetBool())
            VentPlayers.TryAdd(__instance.myPlayer.PlayerId, 0);
            return true;
        }
        public static bool CanUse(PlayerPhysics pp, int id, bool log = true)
        {
            //役職処理はここで行ってしまうと色々とめんどくさくなるので上で。
            var user = pp.myPlayer;

            if (!(user.Data.Role.Role == RoleTypes.Engineer || user.GetCustomRole().GetRoleInfo()?.BaseRoleType.Invoke() == RoleTypes.Engineer))//エンジニアでなく
            {
                if (!user.CanUseImpostorVentButton()) //インポスターベントも使えない
                {
                    if (log) Logger.Info($"{pp.name}はエンジニアでもインポスターベントも使えないため弾きます。", "OnenterVent");
                    return false;
                }
            }
            if (Utils.CantUseVent)
            {
                if (log) Logger.Info($"{pp.name}がベントに入ろうとしましたがベントが無効化されているので弾きます。", "OnenterVent");
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.CoExitVent))]
    class ExitVentPatch
    {
        public static bool Prefix(PlayerPhysics __instance)
        {
            CoEnterVentPatch.VentPlayers.Remove(__instance.myPlayer.PlayerId);

            var player = __instance.myPlayer;
            if (CoEnterVentPatch.OldOnEnterVent.TryGetValue(player.PlayerId, out var canuse))
            {
                if (canuse is false)
                {
                    ReMove();
                    return false;
                }
            }
            if (!(player.Data.Role.Role == RoleTypes.Engineer || player.GetCustomRole().GetRoleInfo()?.BaseRoleType.Invoke() == RoleTypes.Engineer))//エンジニアでなく
            {
                if (!player.CanUseImpostorVentButton()) //インポスターベントも使えない
                {
                    ReMove();
                    return false;
                }
            }
            if (Utils.CantUseVent)
            {
                ReMove();
                return false;
            }

            if (Camouflage.IsCamouflage)
            {
                var sender = CustomRpcSender.Create(name: $"Camouflage.RpcSetSkin({player.Data.GetLogPlayerName()})");
                byte color = (byte)ModColors.PlayerColor.Gray;

                player.SetColor(color);
                sender.AutoStartRpc(player.NetId, (byte)RpcCalls.SetColor)
                    .Write(player.Data.NetId)
                    .Write(color)
                    .EndRpc();

                player.SetHat("", color);
                sender.AutoStartRpc(player.NetId, (byte)RpcCalls.SetHatStr)
                    .Write("")
                    .Write(player.GetNextRpcSequenceId(RpcCalls.SetHatStr))
                    .EndRpc();

                player.SetSkin("", color);
                sender.AutoStartRpc(player.NetId, (byte)RpcCalls.SetSkinStr)
                    .Write("")
                    .Write(player.GetNextRpcSequenceId(RpcCalls.SetSkinStr))
                    .EndRpc();

                player.SetVisor("", color);
                sender.AutoStartRpc(player.NetId, (byte)RpcCalls.SetVisorStr)
                    .Write("")
                    .Write(player.GetNextRpcSequenceId(RpcCalls.SetVisorStr))
                    .EndRpc();
                sender.SendMessage();
            }
            else Camouflage.RpcSetSkin(player);

            return true;

            void ReMove()
            {
                if (VentilationSystemUpdateSystemPatch.NowVentId.TryGetValue(player.PlayerId, out var id))
                {
                    var vent = ShipStatus.Instance.AllVents.Where(x => x.Id == id).FirstOrDefault();
                    if (vent is null)
                    {
                        player.RpcSnapToForced(new UnityEngine.Vector2(100f, 100f));
                        Logger.Error($"無効なベントid{id}。", "Vent");
                        return;
                    }
                    player.RpcSnapToForced(vent.transform.position + new UnityEngine.Vector3(0f, 0.1f));
                }
            }
        }
    }
    #endregion
    #region CheckProtect
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckProtect))]
    class CheckProtectPatch
    {
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            if (!AmongUsClient.Instance.AmHost || !Main.CanUseAbility) return false;

            Logger.Info("CheckProtect発生: " + __instance.GetNameWithRole().RemoveHtmlTags() + "=>" + target.GetNameWithRole().RemoveHtmlTags(), "CheckProtect");

            if (__instance.IsGhostRole())
            {
                Ghostbuttoner.UseAbility(__instance);
                GhostNoiseSender.UseAbility(__instance, target);
                GhostReseter.UseAbility(__instance, target);
                GhostRumour.UseAbility(__instance, target);
                GuardianAngel.UseAbility(__instance, target);
                DemonicTracker.UseAbility(__instance, target);
                DemonicCrusher.UseAbility(__instance);
                DemonicVenter.UseAbility(__instance, target);
                AsistingAngel.UseAbility(__instance, target);
                return false;
            }

            if (__instance.GetCustomRole() is CustomRoles.Sheriff or CustomRoles.WolfBoy or CustomRoles.SwitchSheriff)
            {
                if (__instance.Data.IsDead)
                {
                    Logger.Info("守護をブロックしました。", "CheckProtect");
                    return false;
                }
            }
            //全部falseしてもいい気はする
            return true;
        }
    }
    #endregion
}