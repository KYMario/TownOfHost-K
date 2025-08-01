using AmongUs.GameOptions;
using HarmonyLib;
using UnityEngine;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;

namespace TownOfHost
{
    [HarmonyPatch(typeof(Console), nameof(Console.CanUse))]
    class CanUsePatch
    {
        public static bool Prefix(ref float __result, Console __instance, [HarmonyArgument(0)] NetworkedPlayerInfo pc, [HarmonyArgument(1)] out bool canUse, [HarmonyArgument(2)] out bool couldUse)
        {
            if (GameStates.IsFreePlay)
            {
                canUse = couldUse = false;
                return true;
            }
            canUse = couldUse = false;
            var role = PlayerControl.LocalPlayer.GetCustomRole();
            var hastask = UtilsTask.HasTasks(PlayerControl.LocalPlayer.Data, false);
            var isMotogaCrew = PlayerControl.LocalPlayer.IsAlive() && !hastask && PlayerControl.LocalPlayer.Data.RoleType.IsCrewmate() && !PlayerControl.LocalPlayer.CanUseKillButton() && !role.IsImpostor();
            var Rolecanuse = isMotogaCrew || (hastask && (PlayerControl.LocalPlayer.GetRoleClass()?.CanTask() ?? true));
            var isAmn = PlayerControl.LocalPlayer.Is(CustomRoles.Amnesia) && !PlayerControl.LocalPlayer.Is(CustomRoleTypes.Impostor);

            //こいつをfalseでreturnしても、タスク(サボ含む)以外の使用可能な物は使えるまま(ボタンなど)
            if (!GameStates.InGame)
                return __instance.AllowImpostor || hastask;
            else
                return __instance.AllowImpostor || Rolecanuse || isAmn;
        }
    }
    [HarmonyPatch(typeof(EmergencyMinigame), nameof(EmergencyMinigame.Update))]
    class EmergencyMinigamePatch
    {
        public static void Postfix(EmergencyMinigame __instance)
        {
            if (Options.CurrentGameMode is CustomGameMode.HideAndSeek or CustomGameMode.TaskBattle || SuddenDeathMode.NowSuddenDeathMode)
                __instance.Close();
        }
    }
    [HarmonyPatch(typeof(Vent), nameof(Vent.CanUse))]
    class CanUseVentPatch
    {
        public static bool Prefix(Vent __instance, [HarmonyArgument(0)] NetworkedPlayerInfo pc,
            [HarmonyArgument(1)] ref bool canUse,
            [HarmonyArgument(2)] ref bool couldUse,
            ref float __result)
        {
            PlayerControl playerControl = pc.Object;

            // 前半，Mod独自の処理

            // カスタムロールを元にベントを使えるか判定
            // エンジニアベースの役職は常にtrue
            couldUse = playerControl.CanUseImpostorVentButton() || pc.Role.Role == RoleTypes.Engineer;
            if (playerControl.GetRoleClass()?.CanClickUseVentButton == false) couldUse = false;

            canUse = couldUse;
            // カスタムロールが使えなかったら使用不可
            if (!canUse)
            {
                return false;
            }
            //エンジニア置き換えでもコミュだったらだめだよね。
            if (pc.Role.Role == RoleTypes.Engineer && Utils.IsActive(SystemTypes.Comms))
            {
                canUse = couldUse = false;
                return false;
            }

            // ここまでMod独自の処理
            // ここからバニラ処理の置き換え

            IUsable usableVent = __instance.Cast<IUsable>();
            // ベントとプレイヤーの間の距離
            float actualDistance = float.MaxValue;

            couldUse =
                // クラシックではtrue 多分バニラHnS用
                GameManager.Instance.LogicUsables.CanUse(usableVent, playerControl) &&
                // pc.Role.CanUse(usableVent) &&  バニラロールではなくカスタムロールを元に判定するので無視
                // 対象のベントにベントタスクがない もしくは今自分が対象のベントに入っている
                (!playerControl.MustCleanVent(__instance.Id) || (playerControl.inVent && Vent.currentVent == __instance)) &&
                playerControl.IsAlive() &&
                (playerControl.CanMove || playerControl.inVent);

            // ベント掃除のチェック
            if (ShipStatus.Instance.Systems.TryGetValue(SystemTypes.Ventilation, out var systemType))
            {
                VentilationSystem ventilationSystem = systemType.TryCast<VentilationSystem>();
                // 誰かがベント掃除をしていたらそのベントには入れない
                if (ventilationSystem != null && ventilationSystem.IsVentCurrentlyBeingCleaned(__instance.Id))
                {
                    couldUse = false;
                }
            }

            canUse = couldUse;
            if (canUse)
            {
                Vector3 center = playerControl.Collider.bounds.center;
                Vector3 ventPosition = __instance.transform.position;
                actualDistance = Vector2.Distance(center, ventPosition);
                canUse &= actualDistance <= __instance.UsableDistance && !PhysicsHelpers.AnythingBetween(playerControl.Collider, center, ventPosition, Constants.ShipOnlyMask, false);
            }
            __result = actualDistance;
            return false;
        }
    }
}