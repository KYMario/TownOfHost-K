using AmongUs.GameOptions;
using UnityEngine;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Neutral
{
    public sealed class JackalMafia : RoleBase, ILNKiller, ISchrodingerCatOwner, IUsePhantomButton
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(JackalMafia),
                player => new JackalMafia(player),
                CustomRoles.JackalMafia,
                () => OptionCanMakeSidekick.GetBool() ? RoleTypes.Phantom : RoleTypes.Impostor,
                CustomRoleTypes.Neutral,
                21400,
                SetupOptionItem,
                "jm",
                "#00b4eb",
                (1, 2),
                true,
                countType: CountTypes.Jackal,
                assignInfo: new RoleAssignInfo(CustomRoles.JackalMafia, CustomRoleTypes.Neutral)
                {
                    AssignCountRule = new(1, 1, 1)
                },
                Desc: () =>
                {
                    return GetString("JackalMafiaInfoLong") + (OptionCanMakeSidekick.GetBool() ? string.Format(GetString("JackalDescSidekick"), !OptionImpostorCanSidekick.GetBool() ? GetString("JackalDescImpostorSideKick") : "") : "");
                }
            );
        public JackalMafia(PlayerControl player)
        : base(
            RoleInfo,
            player,
            () => HasTask.False
        )
        {
            KillCooldown = OptionKillCooldown.GetFloat();
            Cooldown = OptionCooldown.GetFloat();
            CanVent = OptionCanVent.GetBool();
            CanUseSabotage = OptionCanUseSabotage.GetBool();
            JackalCanAlsoBeExposedToJMafia = OptionJackalCanAlsoBeExposedToJMafia.GetBool();
            JackalMafiaCanAlsoBeExposedToJackal = OptionJJackalMafiaCanAlsoBeExposedToJackal.GetBool();
            CanSideKick = OptionCanMakeSidekick.GetBool();
        }

        public static OptionItem OptionKillCooldown;
        private static OptionItem OptionCooldown;
        public static OptionItem OptionCanVent;
        public static OptionItem OptionCanUseSabotage;
        static OptionItem OptionHasImpostorVision;
        private static OptionItem OptionJackalCanAlsoBeExposedToJMafia;
        private static OptionItem OptionJJackalMafiaCanAlsoBeExposedToJackal;
        private static OptionItem OptionJJackalCanKillMafia;
        static OptionItem OptionImpostorCanSidekick;
        //サイドキックが元仲間の色を見える
        public static OptionItem OptionSidekickCanSeeOldImpostorTeammates;
        //元仲間impがサイドキック相手の名前の色を見える
        public static OptionItem OptionImpostorCanSeeNameColor;
        public static OptionItem OptionCanMakeSidekick;
        public static OptionItem OptionSidekickPromotion;
        private static float KillCooldown;
        private static float Cooldown;
        public static bool CanVent;
        public static bool CanUseSabotage;
        private static bool JackalCanAlsoBeExposedToJMafia;
        private static bool JackalMafiaCanAlsoBeExposedToJackal;
        bool CanSideKick;
        public enum JackalOption
        {
            JackalCanAlsoBeExposedToJMafia,
            JackalMafiaCanAlsoBeExposedToJackal,
            JackalCanKillMafia,
            JackalImpostorCanSidekick,
            JackalbeforeImpCanSeeImp,
            Jackaldollimpgaimpnimieru,
            JackalSidekickPromotion
        }
        private static void SetupOptionItem()
        {
            OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 0.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionCanVent = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.CanVent, true, false);
            OptionCanUseSabotage = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.CanUseSabotage, false, false);
            OptionHasImpostorVision = BooleanOptionItem.Create(RoleInfo, 13, GeneralOption.ImpostorVision, true, false);
            OptionJJackalCanKillMafia = BooleanOptionItem.Create(RoleInfo, 14, JackalOption.JackalCanKillMafia, false, false);
            OptionJJackalMafiaCanAlsoBeExposedToJackal = BooleanOptionItem.Create(RoleInfo, 16, JackalOption.JackalMafiaCanAlsoBeExposedToJackal, false, false);
            OptionJackalCanAlsoBeExposedToJMafia = BooleanOptionItem.Create(RoleInfo, 17, JackalOption.JackalCanAlsoBeExposedToJMafia, true, false);
            OptionCanMakeSidekick = BooleanOptionItem.Create(RoleInfo, 18, GeneralOption.CanCreateSideKick, true, false);
            OptionImpostorCanSidekick = BooleanOptionItem.Create(RoleInfo, 19, JackalOption.JackalImpostorCanSidekick, false, false, OptionCanMakeSidekick);
            OptionSidekickCanSeeOldImpostorTeammates = BooleanOptionItem.Create(RoleInfo, 20, JackalOption.JackalbeforeImpCanSeeImp, false, false, OptionImpostorCanSidekick);
            OptionImpostorCanSeeNameColor = BooleanOptionItem.Create(RoleInfo, 22, JackalOption.Jackaldollimpgaimpnimieru, false, false, OptionImpostorCanSidekick);
            OptionCooldown = FloatOptionItem.Create(RoleInfo, 23, GeneralOption.Cooldown, new(0f, 180f, 0.5f), 30f, false, OptionCanMakeSidekick)
            .SetValueFormat(OptionFormat.Seconds);
            OptionSidekickPromotion = BooleanOptionItem.Create(RoleInfo, 24, JackalOption.JackalSidekickPromotion, false, false, OptionCanMakeSidekick);
            RoleAddAddons.Create(RoleInfo, 25, NeutralKiller: true);
        }   //↑あってるかは知らない、
        public ISchrodingerCatOwner.TeamType SchrodingerCatChangeTo => ISchrodingerCatOwner.TeamType.Jackal;
        public float CalculateKillCooldown() => KillCooldown;
        public bool CanUseSabotageButton() => CanUseSabotage;
        public bool CanUseImpostorVentButton() => CanVent;
        public override bool OnInvokeSabotage(SystemTypes systemType) => CanUseSabotage;
        public override void ApplyGameOptions(IGameOptions opt)
        {
            opt.SetVision(OptionHasImpostorVision.GetBool());
            AURoleOptions.PhantomCooldown = JackalDoll.GetSideKickCount() <= JackalDoll.NowSideKickCount ? 200f : Cooldown;
        }
        public void ApplySchrodingerCatOptions(IGameOptions option) => ApplyGameOptions(option);
        public bool UseOneclickButton => CanSideKick;
        public override bool CanUseAbilityButton() => CanSideKick;
        bool IUsePhantomButton.IsPhantomRole => JackalDoll.GetSideKickCount() > JackalDoll.NowSideKickCount;
        public void OnClick(ref bool AdjustKillCooldown, ref bool? ResetCooldown)
        {
            AdjustKillCooldown = true;
            if (!CanSideKick) return;

            if (JackalDoll.GetSideKickCount() <= JackalDoll.NowSideKickCount)
            {
                CanSideKick = false;
                return;
            }
            var target = Player.GetKillTarget(true);
            if (target == null)
            {
                ResetCooldown = false;
                return;
            }
            var targetrole = target.GetCustomRole();
            if (target == null || (targetrole is CustomRoles.King or CustomRoles.Jackal or CustomRoles.JackalAlien or CustomRoles.Jackaldoll or CustomRoles.JackalMafia or CustomRoles.Merlin) || ((targetrole.IsImpostor() || targetrole is CustomRoles.Egoist) && !OptionImpostorCanSidekick.GetBool()))
            {
                ResetCooldown = false;
                return;
            }
            if (SuddenDeathMode.NowSuddenDeathTemeMode)
            {
                target.SideKickChangeTeam(Player);
            }
            CanSideKick = false;
            Player.RpcProtectedMurderPlayer(target);
            target.RpcProtectedMurderPlayer(Player);
            target.RpcProtectedMurderPlayer(target);
            UtilsGameLog.AddGameLog($"SideKick", string.Format(GetString("log.Sidekick"), UtilsName.GetPlayerColor(target, true) + $"({UtilsRoleText.GetTrueRoleName(target.PlayerId)})", UtilsName.GetPlayerColor(Player, true)));
            target.RpcSetCustomRole(CustomRoles.Jackaldoll);
            if (!Utils.RoleSendList.Contains(target.PlayerId)) Utils.RoleSendList.Add(target.PlayerId);
            JackalDoll.Sidekick(target, Player);
            UtilsOption.MarkEveryoneDirtySettings();
            UtilsGameLog.LastLogRole[target.PlayerId] += "<b>⇒" + Utils.ColorString(UtilsRoleText.GetRoleColor(target.GetCustomRole()), GetString($"{target.GetCustomRole()}")) + "</b>";
        }

        public bool CanUseKillButton()
        {
            if (PlayerState.AllPlayerStates == null) return false;
            int livingImpostorsNum = 0;
            foreach (var pc in PlayerCatch.AllAlivePlayerControls)
            {
                if (pc.PlayerId == Player.PlayerId) continue;
                if (pc.Is(CountTypes.Jackal)) livingImpostorsNum++;
            }
            return livingImpostorsNum <= 0;
        }
        public override bool OnCheckMurderAsTarget(MurderInfo info)
        {
            (var killer, var target) = info.AttemptTuple;
            if (killer.Is(CountTypes.Jackal) && !OptionJJackalCanKillMafia.GetBool())
            {
                info.DoKill = false;
                killer.SetKillCooldown();
                return false;
            }
            return true;
        }
        public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, ref bool enabled, ref Color roleColor, ref string roleText, ref bool addon)
        {
            addon = false;
            if ((seen.Is(CustomRoles.Jackal) || seen.Is(CustomRoles.JackalMafia) || seen.Is(CustomRoles.JackalAlien)) && JackalMafiaCanAlsoBeExposedToJackal)
                enabled = true;
        }
        public override void OverrideDisplayRoleNameAsSeen(PlayerControl seen, ref bool enabled, ref Color roleColor, ref string roleText, ref bool addon)
        {
            addon = false;
            if ((seen.Is(CustomRoles.Jackal) || seen.Is(CustomRoles.JackalMafia) || seen.Is(CustomRoles.JackalAlien)) && JackalCanAlsoBeExposedToJMafia)
                enabled = true;
        }
        public override string GetAbilityButtonText() => GetString("Sidekick");
        public override bool OverrideAbilityButton(out string text)
        {
            text = "SideKick";
            return true;
        }
        public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
        {
            seen ??= seer;
            if (seen.PlayerId != seer.PlayerId || isForMeeting || !Player.IsAlive()
            || JackalDoll.GetSideKickCount() <= JackalDoll.NowSideKickCount || !CanSideKick) return "";

            if (isForHud) return GetString("PhantomButtonSideKick");
            return $"<size=50%>{GetString("PhantomButtonSideKick")}</size>";
        }
    }
}