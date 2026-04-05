using AmongUs.GameOptions;
using Il2CppSystem.Runtime.Remoting.Messaging;
using TownOfHost.Roles.Core;
using UnityEngine;

namespace TownOfHost.Roles.Crewmate
{
    public sealed class Pukupuku : RoleBase
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Pukupuku),
                player => new Pukupuku(player),
                CustomRoles.Pukupuku,
                () => RoleTypes.Crewmate,
                CustomRoleTypes.Crewmate,
                65000,
                SetupOptionItem,
                "Pukupuku",
                "#55ccff",
                isDesyncImpostor: false
            );

        private static OptionItem ModeOption;

        private bool tasksCompleted = false;
        private bool guardUsed = false;
        private bool revengeDone = false;
        private bool completedWhileAlive = false;
        private PlayerControl killerRef = null;

        public Pukupuku(PlayerControl player)
            : base(RoleInfo, player)
        {
        }

        private static void SetupOptionItem()
        {
            ModeOption = StringOptionItem.Create(
                RoleInfo,
                65011,
                "PukupukuAliveMode",
                new string[] { "Off", "Guard" },
                0,
                false
            );
        }

        private bool IsGuardMode() => ModeOption.GetValue() == 1;

        public static bool IsPukupuku(PlayerControl pc)
        {
            return pc != null && pc.GetCustomRole() == CustomRoles.Pukupuku;
        }

        public override bool OnCompleteTask(uint taskid)
        {
            if (!IsPukupuku(Player)) return true;
            if (tasksCompleted) return true;

            if (MyTaskState.IsTaskFinished)
            {
                if (!Player.Data.IsDead)
                {
                    // ★ 生存時にタスク完了したときだけ能力解放
                    tasksCompleted = true;
                    completedWhileAlive = true;
                }
                else
                {
                    // ★ 死後タスク完了 → tasksCompleted を true にしない
                    // ★ completedWhileAlive が false のときだけ道連れ
                    if (!completedWhileAlive)
                        TryRevenge();
                }
            }

            return true;
        }

        public override bool OnCheckMurderAsTarget(MurderInfo info)
        {
            if (!IsPukupuku(Player)) return true;
            if (!tasksCompleted) return true;

            var (killer, target) = info.AttemptTuple;

            if (IsGuardMode() && !guardUsed)
            {
                guardUsed = true;

                // ★ ガード時でもキラーのキルクールは消費する
                killer.SetKillCooldown(target: target, force: true);
                info.GuardPower = 2;
                killer.RpcProtectedMurderPlayer(target);

                return true;
            }

            return true;
        }

        public override void OnMurderPlayerAsTarget(MurderInfo info)
        {
            if (!IsPukupuku(Player)) return;
            if (revengeDone) return;

            killerRef = info.AttemptKiller;

            // ★ 生存時タスク完了していた場合は道連れ無効
            if (tasksCompleted && !completedWhileAlive)
            {
                TryRevenge();
            }
        }

        private void TryRevenge()
        {
            if (revengeDone) return;

            if (killerRef != null && !killerRef.Data.IsDead)
            {
                // ★ 道連れ死因を設定
                PlayerState.GetByPlayerId(killerRef.PlayerId).DeathReason = CustomDeathReason.Revenge;

                // ★ キル実行
                killerRef.MurderPlayer(killerRef);

                revengeDone = true;
            }
        }
    }
}