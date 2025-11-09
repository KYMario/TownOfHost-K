using AmongUs.GameOptions;
using HarmonyLib;
using UnityEngine;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.AddOns.Common;
using TownOfHost.Roles.Ghost;

namespace TownOfHost
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Start))]
    class PlayerStartPatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            var roleText = Object.Instantiate(__instance.cosmetics.nameText);
            roleText.transform.SetParent(__instance.cosmetics.nameText.transform);
            roleText.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            roleText.transform.localScale = new(1f, 1f, 1f);
            roleText.fontSize = Main.RoleTextSize;
            roleText.text = "RoleText";
            roleText.gameObject.name = "RoleText";
            roleText.enabled = false;
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetColor))]
    class SetColorPatch
    {
        public static bool IsAntiGlitchDisabled = false;
        public static bool Prefix(PlayerControl __instance, int bodyColor)
        {
            //色変更バグ対策
            if (!AmongUsClient.Instance.AmHost || __instance.CurrentOutfit.ColorId == bodyColor || IsAntiGlitchDisabled) return true;
            if (bodyColor < 0 && __instance.name is not "Player(Clone)")
            {
                Logger.Warn($"{__instance.Data.GetLogPlayerName()} => ColorError", "SetColor");
                Logger.seeingame($"{__instance.name}のColorが存在しません!");
                return false;
            }
            if (AmongUsClient.Instance.IsGameStarted && Options.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                //ゲーム中に色を変えた場合
                __instance.RpcMurderPlayer(__instance, true);
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetRole))]
    class PlayerControlSetRolePatch
    {
        public static bool Prefix(PlayerControl __instance, ref RoleTypes roleType, ref bool canOverrideRole)
        {
            var target = __instance;
            var targetName = __instance.GetNameWithRole().RemoveHtmlTags();
            canOverrideRole = true;
            Logger.Info($"{targetName} => {roleType}", "PlayerControl.RpcSetRole");
            if (AmongUsClient.Instance.AmHost is false) return true;
            if (GameStates.IsFreePlay && Main.EditMode)
            {
                roleType = RoleTypes.Shapeshifter;
                return true;
            }
            if (ShipStatus.Instance == null || !ShipStatus.Instance.enabled) return true;
            if (AntiBlackout.IsSet)
            {
                Logger.Info($"AntiBlackoutが動作中だからキャンセル！", "RpcSetRole");
                return false;
            }

            Main.NowTypes[__instance.PlayerId] = roleType;
            return true;
        }
        public static void Postfix(PlayerControl __instance) => __instance.Data.Role.NameColor = Palette.White;
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Die))]
    public static class PlayerControlDiePatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                // 死者の最終位置にペットが残るバグ対応
                __instance.RpcSetPet("");

                if (__instance.Is(CustomRoles.Amnesia))//アムネシア削除
                {
                    Amnesia.RemoveAmnesia(__instance.PlayerId, true);
                }

                if (CustomWinnerHolder.WinnerTeam == CustomWinner.Default)
                {
                    if (!SuddenDeathMode.NowSuddenDeathMode && __instance.PlayerId != PlayerControl.LocalPlayer.PlayerId)//サボ可能役職のみインポスターゴーストにする
                    {
                        if (__instance.GetCustomRole().IsImpostor() || ((__instance.GetRoleClass() as IKiller)?.CanUseSabotageButton() ?? false))
                            _ = new LateTask(() =>
                            {
                                if (!GameStates.CalledMeeting)
                                {
                                    __instance.RpcSetRole(RoleTypes.ImpostorGhost, true);
                                }
                            }, 1.4f, "Fix sabotage", true);
                    }
                    _ = new LateTask(() => GhostRoleAssingData.AssignAddOnsFromList(), 1.4f, "Fix sabotage", true);
                }
            }
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MixUpOutfit))]
    public static class PlayerControlMixupOutfitPatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            if (!__instance.IsAlive())
            {
                return;
            }
            // 自分がDesyncインポスターで，バニラ判定ではインポスターの場合，バニラ処理で名前が非表示にならないため，相手の名前を非表示にする
            if (
                PlayerControl.LocalPlayer.Data.Role.IsImpostor &&  // バニラ判定でインポスター
                !PlayerControl.LocalPlayer.Is(CustomRoleTypes.Impostor) &&  // Mod判定でインポスターではない
                PlayerControl.LocalPlayer.GetCustomRole().GetRoleInfo()?.IsDesyncImpostor == true)  // Desyncインポスター
            {
                // 名前を隠す
                __instance.cosmetics.ToggleNameVisible(false);
            }
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckSporeTrigger))]
    public static class PlayerControlCheckSporeTriggerPatch
    {
        public static bool Prefix()
        {
            if (Options.DisableFungleSporeTrigger.GetBool())
            {
                return false;
            }
            return true;
        }
    }

}