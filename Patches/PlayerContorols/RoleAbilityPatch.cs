using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using System.Linq;
using TownOfHost.Roles.AddOns.Common;
using AmongUs.GameOptions;
using Hazel;
using TownOfHost.Roles.Ghost;

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

            var button = (roleclass as IUseTheShButton)?.CheckShapeshift(__instance, target);
            if (button.HasValue)
            {
                shouldAnimate = false;
                return button.Value;
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
                foreach (var p in PlayerCatch.AllAlivePlayerControls)
                {
                    if ((p.Data.Role.Role != RoleTypes.Shapeshifter || p.GetCustomRole().GetRoleInfo()?.BaseRoleType.Invoke() != RoleTypes.Shapeshifter) && !p.Is(CustomRoleTypes.Impostor) && !p.Is(targetRole))
                    {
                        dis = Vector2.Distance(shapeshifterPosition, p.transform.position);
                        mpdistance.Add(p, dis);
                    }
                }
                if (mpdistance.Count != 0)

                //if (targetm != null && !targetm.Is(targetRole) && !targetm.Is(CustomRoleTypes.Impostor))
                {
                    //shapeshifter.RpcShapeshift(shapeshifter, false);
                    var min = mpdistance.OrderBy(c => c.Value).FirstOrDefault();//一番値が小さい
                    PlayerControl targetm = min.Key;
                    if (!targetm.Is(CustomRoles.King))
                    {
                        if (SuddenDeathMode.NowSuddenDeathTemeMode)
                        {
                            if (SuddenDeathMode.TeamRed.Contains(targetm.PlayerId))
                            {
                                SuddenDeathMode.TeamRed.Add(target.PlayerId);
                                SuddenDeathMode.TeamBlue.Remove(target.PlayerId);
                                SuddenDeathMode.TeamYellow.Remove(target.PlayerId);
                                SuddenDeathMode.TeamGreen.Remove(target.PlayerId);
                                SuddenDeathMode.TeamPurple.Remove(target.PlayerId);
                            }
                            if (SuddenDeathMode.TeamBlue.Contains(targetm.PlayerId))
                            {
                                SuddenDeathMode.TeamRed.Remove(target.PlayerId);
                                SuddenDeathMode.TeamBlue.Add(target.PlayerId);
                                SuddenDeathMode.TeamYellow.Remove(target.PlayerId);
                                SuddenDeathMode.TeamGreen.Remove(target.PlayerId);
                                SuddenDeathMode.TeamPurple.Remove(target.PlayerId);
                            }
                            if (SuddenDeathMode.TeamYellow.Contains(targetm.PlayerId))
                            {
                                SuddenDeathMode.TeamRed.Remove(target.PlayerId);
                                SuddenDeathMode.TeamBlue.Remove(target.PlayerId);
                                SuddenDeathMode.TeamYellow.Add(target.PlayerId);
                                SuddenDeathMode.TeamGreen.Remove(target.PlayerId);
                                SuddenDeathMode.TeamPurple.Remove(target.PlayerId);
                            }
                            if (SuddenDeathMode.TeamGreen.Contains(targetm.PlayerId))
                            {
                                SuddenDeathMode.TeamRed.Remove(target.PlayerId);
                                SuddenDeathMode.TeamBlue.Remove(target.PlayerId);
                                SuddenDeathMode.TeamYellow.Remove(target.PlayerId);
                                SuddenDeathMode.TeamGreen.Add(target.PlayerId);
                                SuddenDeathMode.TeamPurple.Remove(target.PlayerId);
                            }
                            if (SuddenDeathMode.TeamPurple.Contains(targetm.PlayerId))
                            {
                                SuddenDeathMode.TeamRed.Remove(target.PlayerId);
                                SuddenDeathMode.TeamBlue.Remove(target.PlayerId);
                                SuddenDeathMode.TeamYellow.Remove(target.PlayerId);
                                SuddenDeathMode.TeamGreen.Remove(target.PlayerId);
                                SuddenDeathMode.TeamPurple.Add(target.PlayerId);
                            }
                        }
                        UtilsGameLog.AddGameLog("SideKick", string.Format(Translator.GetString("log.Sidekick"), Utils.GetPlayerColor(targetm, true) + $"({UtilsRoleText.GetTrueRoleName(targetm.PlayerId)})", Utils.GetPlayerColor(shapeshifter, true) + $"({UtilsRoleText.GetTrueRoleName(shapeshifter.PlayerId)})"));
                        targetm.RpcSetCustomRole(targetRole);
                        Logger.Info($"Make SKMadmate:{targetm.name}", "Shapeshift");
                        PlayerCatch.SKMadmateNowCount++;
                        shapeshifter.RpcProtectedMurderPlayer(targetm);
                        targetm.RpcProtectedMurderPlayer(shapeshifter);
                        targetm.RpcProtectedMurderPlayer(targetm);

                        foreach (var pl in PlayerCatch.AllPlayerControls)
                        {
                            if (pl == PlayerControl.LocalPlayer)
                                targetm.StartCoroutine(targetm.CoSetRole(Options.SkMadCanUseVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate, Main.SetRoleOverride));
                            else
                                targetm.RpcSetRoleDesync(Options.SkMadCanUseVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate, pl.GetClientId());
                        }
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
                if (roleclass?.CheckShapeshift(target, ref shouldAnimate) == false)
                {
                    if (roleclass.CanDesyncShapeshift)
                    {
                        shapeshifter.RpcSpecificRejectShapeshift(target, shouldAnimate);
                    }
                    else
                    {
                        shapeshifter.RpcRejectShapeshift();
                    }
                    return false;
                }

            shapeshifter.RpcShapeshift(target, shouldAnimate);
            return false;
        }
        private static bool CheckInvalidShapeshifting(PlayerControl instance, PlayerControl target, bool animate)
        {
            logger.Info($"Checking shapeshift {instance.GetNameWithRole().RemoveHtmlTags()} -> {(target == null || target.Data == null ? "(null)" : target.GetNameWithRole().RemoveHtmlTags())}");

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
        public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
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

            if (!AmongUsClient.Instance.AmHost) return;

            _ = new LateTask(() =>
            {
                foreach (var role in CustomRoleManager.AllActiveRoles.Values)
                {
                    role.Colorchnge();
                }
            }, 1.2f, "", true);

            if (!shapeshifting)
            {
                Camouflage.RpcSetSkin(shapeshifter);
                if (Options.Onlyseepet.GetBool())
                {
                    shapeshifter.OnlySeeMePet(shapeshifter.Data.DefaultOutfit.PetId);
                }
            }
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
    [HarmonyPatch(typeof(PlayerControl))]
    class PlayerControlPhantomPatch
    {
        [HarmonyPatch(nameof(PlayerControl.CheckVanish)), HarmonyPrefix]
        public static bool Prefix(PlayerControl __instance)
        {
            if (__instance.PlayerId == PlayerControl.LocalPlayer.PlayerId) return false;
            //Logger.Info($"{__instance?.Data?.PlayerName}", "CheckVanish and Appear");
            var resetkillcooldown = false;
            bool? fall = false;

            if (__instance.GetRoleClass() is IUsePhantomButton iusephantombutton && Main.CanUseAbility)
                iusephantombutton.CheckOnClick(ref resetkillcooldown, ref fall);

            float k = 0;
            IUsePhantomButton.IPPlayerKillCooldown.TryGetValue(__instance.PlayerId, out k);
            Main.AllPlayerKillCooldown.TryGetValue(__instance.PlayerId, out var killcool);
            /* 初手で、キルク修正がオフでキルクが10s以上で、キルが0回*/
            if (MeetingStates.FirstMeeting && !Options.FixFirstKillCooldown.GetBool() && killcool > 10 && PlayerState.GetByPlayerId(__instance.PlayerId)?.GetKillCount() is 0 or null)
                killcool = 10;
            float cooldown = killcool - k;
            if (cooldown <= 1) cooldown = 0.005f;

            if (!resetkillcooldown)
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
            if (fall is not null)
            {
                writer.StartRpc(__instance.NetId, (byte)RpcCalls.SetRole)
                    .Write((ushort)RoleTypes.Impostor)
                    .Write(true)
                    .EndRpc();
                writer.StartRpc(__instance.NetId, (byte)RpcCalls.SetRole)
                    .Write((ushort)RoleTypes.Phantom)
                    .Write(true)
                    .EndRpc();
            }
            if (!resetkillcooldown && !(cooldown < 10))
            {
                writer.StartRpc(__instance.NetId, (byte)RpcCalls.MurderPlayer)
                    .WriteNetObject(__instance)
                    .Write((int)MurderResultFlags.FailedProtected)
                    .EndRpc();
            }
            if (fall == false)
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
    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.CoEnterVent))]
    class CoEnterVentPatch
    {
        public static Dictionary<byte, float> VentPlayers = new();
        static bool MadBool = false;
        public static bool Prefix(PlayerPhysics __instance, [HarmonyArgument(0)] int id)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                var user = __instance.myPlayer;

                if (MadBool)
                {
                    MadBool = false;
                    return true;
                }

                if (Options.CurrentGameMode == CustomGameMode.HideAndSeek && Options.IgnoreVent.GetBool())
                    __instance.RpcBootFromVent(id);

                if (user.Is(CustomRoles.DemonicVenter)) return true;

                var roleClass = user.GetRoleClass();
                var pos = __instance.transform.position;
                if (Amnesia.CheckAbilityreturn(user)) roleClass = null;

                if ((!roleClass?.OnEnterVent(__instance, id) ?? false) || !CanUse(__instance, id))
                {
                    if (Options.CurrentGameMode == CustomGameMode.TaskBattle) return true;
                    //一番遠いベントに追い出す
                    var sender = CustomRpcSender.Create("Farthest Vent")
                        .StartMessage();
                    foreach (var pc in PlayerCatch.AllPlayerControls)
                    {
                        if (pc == user || pc.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue; //本人とホストは別の処理
                        Dictionary<int, float> Distance = new();
                        Vector2 position = pc.transform.position;
                        //一番遠いベントを調べて送る
                        foreach (var vent in ShipStatus.Instance.AllVents)
                            Distance.Add(vent.Id, Vector2.Distance(position, vent.transform.position));
                        var ventid = Distance.OrderByDescending(x => x.Value).First().Key;
                        sender.AutoStartRpc(__instance.NetId, (byte)RpcCalls.BootFromVent, pc.GetClientId())
                            .Write(ventid)
                            .EndRpc();
                        __instance.myPlayer.RpcSnapToForced(pos);
                    }
                    sender.EndMessage();
                    sender.SendMessage(); //多分負荷あれだし、テープで無理やり戻した感じだから参考にしない方がいい、

                    /*MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.BootFromVent, SendOption.Reliable, -1);
                    writer.WritePacked(127);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);*/

                    _ = new LateTask(() =>
                    {
                        int clientId = user.GetClientId();
                        MessageWriter writer2 = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.BootFromVent, SendOption.Reliable, clientId);
                        writer2.Write(id);
                        AmongUsClient.Instance.FinishRpcImmediately(writer2);
                        __instance.myPlayer.RpcSnapToForced(pos);
                    }, __instance.myPlayer != PlayerControl.LocalPlayer ? 0.8f : 0.3f, "Fix DesyncImpostor Stuck", null);
                    return false;
                }

                //マッドでベント移動できない設定なら矢印を消す
                if ((!roleClass?.CantVentIdo(__instance, id) ?? false) ||
                    (user.GetCustomRole().IsMadmate() && !Options.MadmateCanMovedByVent.GetBool()))
                {
                    if (!MadBool && user.PlayerId == PlayerControl.LocalPlayer.PlayerId)
                        MadBool = true;
                    int clientId = user.GetClientId();
                    _ = new LateTask(() =>
                    {
                        MessageWriter writer2 = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.BootFromVent, SendOption.Reliable, clientId);
                        writer2.Write(id);
                        AmongUsClient.Instance.FinishRpcImmediately(writer2);
                    }, 0.1f, "Vent- BootFromVent", true);
                    _ = new LateTask(() =>
                    {
                        MessageWriter writer2 = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.EnterVent, SendOption.Reliable, clientId);
                        writer2.Write(id);
                        AmongUsClient.Instance.FinishRpcImmediately(writer2);
                    }, 0.25f, "Vent- EnterVent", null);
                    //BootFromVentで255とかしてもできるが、タイミングがよくわかんないので上ので今のとこはおｋ
                }
                CustomRoleManager.OnEnterVent(__instance, id);
            }
            if (Options.MaxInVentMode.GetBool())
                VentPlayers.TryAdd(__instance.myPlayer.PlayerId, 0);
            return true;
        }
        static bool CanUse(PlayerPhysics pp, int id)
        {
            //役職処理はここで行ってしまうと色々とめんどくさくなるので上で。
            var user = pp.myPlayer;

            if (!(user.Data.Role.Role == RoleTypes.Engineer || user.GetCustomRole().GetRoleInfo()?.BaseRoleType.Invoke() == RoleTypes.Engineer))//エンジニアでなく
            {
                if (!user.CanUseImpostorVentButton()) //インポスターベントも使えない
                {
                    Logger.Info($"{pp.name}はエンジニアでもインポスターベントも使えないため弾きます。", "OnenterVent");
                    return false;
                }
            }
            if (Utils.CanVent)
            {
                Logger.Info($"{pp.name}がベントに入ろうとしましたがベントが無効化されているので弾きます。", "OnenterVent");
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.CoExitVent))]
    class ExitVentPatch
    {
        public static void Prefix(PlayerPhysics __instance)
        {
            CoEnterVentPatch.VentPlayers.Remove(__instance.myPlayer.PlayerId);
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

            if (__instance.IsGorstRole())
            {
                Ghostbuttoner.UseAbility(__instance);
                GhostNoiseSender.UseAbility(__instance, target);
                GhostReseter.UseAbility(__instance, target);
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