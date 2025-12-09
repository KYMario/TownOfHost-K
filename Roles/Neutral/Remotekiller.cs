using AmongUs.GameOptions;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Neutral
{
    public sealed class Remotekiller : RoleBase, ILNKiller, ISchrodingerCatOwner
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Remotekiller),
                player => new Remotekiller(player),
                CustomRoles.Remotekiller,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Neutral,
                13600,
                SetupOptionItem,
                "rk",
                "#8f00ce",
                (2, 3),
                true,
                introSound: () => GetIntroSound(RoleTypes.Impostor),
                countType: CountTypes.Remotekiller,
                assignInfo: new RoleAssignInfo(CustomRoles.Remotekiller, CustomRoleTypes.Neutral)
                {
                    AssignCountRule = new(1, 1, 1)
                }
            );
        public Remotekiller(PlayerControl player)
        : base(
            RoleInfo,
            player,
            () => HasTask.False
        )
        {
            KillCooldown = OptionKillCooldown.GetFloat();
            ReamoteTargetId = byte.MaxValue;
        }

        private static OptionItem OptionKillCooldown;
        private static OptionItem OptionKillAnimation;
        private byte ReamoteTargetId;
        private static float KillCooldown;

        enum OptionName
        {
            KillAnimation
        }

        private static void SetupOptionItem()
        {
            SoloWinOption.Create(RoleInfo, 9, defo: 0);
            OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 0.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionKillAnimation = BooleanOptionItem.Create(RoleInfo, 11, OptionName.KillAnimation, true, false);
            RoleAddAddons.Create(RoleInfo, 12, NeutralKiller: true);
        }
        public ISchrodingerCatOwner.TeamType SchrodingerCatChangeTo => ISchrodingerCatOwner.TeamType.Remotekiller;
        public float CalculateKillCooldown() => KillCooldown;
        public override bool OnInvokeSabotage(SystemTypes systemType) => false;

        public void OnCheckMurderAsKiller(MurderInfo info)
        {
            if (!info.CanKill) return; //キル出来ない相手には無効
            var (killer, target) = info.AttemptTuple;

            if (info.IsFakeSuicide) return;
            if (info.CheckHasGuard())
            {
                info.IsGuard = true;
                return;
            }
            //登録
            killer.SetKillCooldown(KillCooldown, target: target);
            ReamoteTargetId = target.PlayerId;
            info.DoKill = false;
        }
        public override void OnReportDeadBody(PlayerControl _, NetworkedPlayerInfo __)
        {
            ReamoteTargetId = byte.MaxValue;
        }
        public bool OverrideKillButtonText(out string text)
        {
            text = GetString("rkTargetButtonText");
            return true;
        }
        public override bool OnEnterVent(PlayerPhysics physics, int ventId)
        {
            var user = physics.myPlayer;
            if (ReamoteTargetId is not byte.MaxValue && Player.PlayerId == user.PlayerId)
            {
                var target = PlayerCatch.GetPlayerById(ReamoteTargetId);
                if (!target.IsAlive()) return true;
                if (OptionKillAnimation.GetBool())
                {
                    _ = new LateTask(() =>
                    {
                        target.SetRealKiller(user);
                        user.RpcMurderPlayer(target, true);
                    }, 1.2f);
                }
                else
                {
                    target.SetRealKiller(user);
                    target.RpcMurderPlayer(target, true);
                }

                RPC.PlaySoundRPC(user.PlayerId, Sounds.KillSound);
                RPC.PlaySoundRPC(user.PlayerId, Sounds.TaskComplete);
                Logger.Info($"Remotekillerのターゲット{target.name}のキルに成功", "Remotekiller.kill");
                ReamoteTargetId = byte.MaxValue;
                return !OptionKillAnimation.GetBool();
            }
            return true;
        }
        public bool CanUseSabotageButton() => false;
        bool IKiller.OverrideKillButton(out string text)
        {
            text = "Remotekiller_Kill";
            return true;
        }
        bool IKiller.OverrideKillButtonText(out string text)
        {
            text = GetString("MadChanger_Targetset");
            return true;
        }
        bool IKiller.OverrideImpVentButton(out string text)
        {
            text = "Remotekiller_Vent";
            return ReamoteTargetId is not byte.MaxValue;
        }
    }
}
