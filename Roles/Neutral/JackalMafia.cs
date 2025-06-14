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
                () => CanmakeSK.GetBool() ? RoleTypes.Phantom : RoleTypes.Impostor,
                CustomRoleTypes.Neutral,
                30500,
                SetupOptionItem,
                "jm",
                "#00b4eb",
                true,
                countType: CountTypes.Jackal,
                assignInfo: new RoleAssignInfo(CustomRoles.JackalMafia, CustomRoleTypes.Neutral)
                {
                    AssignCountRule = new(1, 1, 1)
                },
                Desc: () =>
                {
                    return GetString("JackalMafiaInfoLong") + (CanmakeSK.GetBool() ? string.Format(GetString("JackalDescSidekick"), !CanImpSK.GetBool() ? GetString("JackalDescImpostorSideKick") : "") : "");
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
            SK = CanmakeSK.GetBool();
            Fall = false;
        }

        public static OptionItem OptionKillCooldown;
        private static OptionItem OptionCooldown;
        public static OptionItem OptionCanVent;
        public static OptionItem OptionCanUseSabotage;
        static OptionItem OptionHasImpostorVision;
        private static OptionItem OptionJackalCanAlsoBeExposedToJMafia;
        private static OptionItem OptionJJackalMafiaCanAlsoBeExposedToJackal;
        private static OptionItem OptionJJackalCanKillMafia;
        static OptionItem CanImpSK;
        //サイドキックが元仲間の色を見える
        public static OptionItem SKcanImp;
        //元仲間impがサイドキック相手の名前の色を見える
        public static OptionItem SKimpwocanimp;
        public static OptionItem CanmakeSK;
        public static OptionItem OptionDoll;
        private static float KillCooldown;
        private static float Cooldown;
        public static bool CanVent;
        public static bool CanUseSabotage;
        private static bool JackalCanAlsoBeExposedToJMafia;
        private static bool JackalMafiaCanAlsoBeExposedToJackal;
        bool SK;
        bool Fall;
        public enum JackalOption
        {
            JackalCanAlsoBeExposedToJMafia,
            JackalMafiaCanAlsoBeExposedToJackal,
            JackalCanKillMafia,
            JackaldollCanimp,
            JackalbeforeImpCanSeeImp,
            Jackaldollimpgaimpnimieru,
            dool,
            JackaldollShoukaku
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
            CanmakeSK = BooleanOptionItem.Create(RoleInfo, 18, GeneralOption.CanCreateSideKick, true, false);
            CanImpSK = BooleanOptionItem.Create(RoleInfo, 19, JackalOption.JackaldollCanimp, false, false, CanmakeSK);
            SKcanImp = BooleanOptionItem.Create(RoleInfo, 20, JackalOption.JackalbeforeImpCanSeeImp, false, false, CanImpSK);
            SKimpwocanimp = BooleanOptionItem.Create(RoleInfo, 22, JackalOption.Jackaldollimpgaimpnimieru, false, false, CanImpSK);
            OptionCooldown = FloatOptionItem.Create(RoleInfo, 23, GeneralOption.Cooldown, new(0f, 180f, 0.5f), 30f, false, CanmakeSK)
            .SetValueFormat(OptionFormat.Seconds);
            OptionDoll = BooleanOptionItem.Create(RoleInfo, 24, JackalOption.JackaldollShoukaku, false, false, CanmakeSK);
            RoleAddAddons.Create(RoleInfo, 25, NeutralKiller: true);
        }   //↑あってるかは知らない、
        public SchrodingerCat.TeamType SchrodingerCatChangeTo => SchrodingerCat.TeamType.Jackal;
        public float CalculateKillCooldown() => KillCooldown;
        public bool CanUseSabotageButton() => CanUseSabotage;
        public bool CanUseImpostorVentButton() => CanVent;
        public override bool OnInvokeSabotage(SystemTypes systemType) => CanUseSabotage;
        public override void ApplyGameOptions(IGameOptions opt)
        {
            opt.SetVision(OptionHasImpostorVision.GetBool());
            AURoleOptions.PhantomCooldown = JackalDoll.GetSideKickCount() <= JackalDoll.side ? 200f : (Fall ? 0f : Cooldown);
        }
        public void ApplySchrodingerCatOptions(IGameOptions option) => ApplyGameOptions(option);
        public bool UseOneclickButton => SK;
        public override bool CanUseAbilityButton() => SK;
        public override void AfterMeetingTasks()
        {
            Fall = false;
            Player.MarkDirtySettings();
        }
        bool IUsePhantomButton.IsPhantomRole => JackalDoll.GetSideKickCount() > JackalDoll.side;
        public void OnClick(ref bool resetkillcooldown, ref bool? fall)
        {
            resetkillcooldown = false;
            if (!SK) return;

            if (JackalDoll.GetSideKickCount() <= JackalDoll.side)
            {
                SK = false;
                return;
            }
            var ch = Fall;
            var target = Player.GetKillTarget(true);
            if (target == null)
            {
                fall = true;
                return;
            }
            var targetrole = target.GetCustomRole();
            if (target == null || (targetrole is CustomRoles.King or CustomRoles.Jackal or CustomRoles.JackalAlien or CustomRoles.Jackaldoll or CustomRoles.JackalMafia or CustomRoles.Merlin) || ((targetrole.IsImpostor() || targetrole is CustomRoles.Egoist) && !CanImpSK.GetBool()))
            {
                fall = true;
                return;
            }
            if (SuddenDeathMode.NowSuddenDeathTemeMode)
            {
                if (SuddenDeathMode.TeamRed.Contains(Player.PlayerId))
                {
                    SuddenDeathMode.TeamRed.Add(target.PlayerId);
                    SuddenDeathMode.TeamBlue.Remove(target.PlayerId);
                    SuddenDeathMode.TeamYellow.Remove(target.PlayerId);
                    SuddenDeathMode.TeamGreen.Remove(target.PlayerId);
                    SuddenDeathMode.TeamPurple.Remove(target.PlayerId);
                }
                if (SuddenDeathMode.TeamBlue.Contains(Player.PlayerId))
                {
                    SuddenDeathMode.TeamRed.Remove(target.PlayerId);
                    SuddenDeathMode.TeamBlue.Add(target.PlayerId);
                    SuddenDeathMode.TeamYellow.Remove(target.PlayerId);
                    SuddenDeathMode.TeamGreen.Remove(target.PlayerId);
                    SuddenDeathMode.TeamPurple.Remove(target.PlayerId);
                }
                if (SuddenDeathMode.TeamYellow.Contains(Player.PlayerId))
                {
                    SuddenDeathMode.TeamRed.Remove(target.PlayerId);
                    SuddenDeathMode.TeamBlue.Remove(target.PlayerId);
                    SuddenDeathMode.TeamYellow.Add(target.PlayerId);
                    SuddenDeathMode.TeamGreen.Remove(target.PlayerId);
                    SuddenDeathMode.TeamPurple.Remove(target.PlayerId);
                }
                if (SuddenDeathMode.TeamGreen.Contains(Player.PlayerId))
                {
                    SuddenDeathMode.TeamRed.Remove(target.PlayerId);
                    SuddenDeathMode.TeamBlue.Remove(target.PlayerId);
                    SuddenDeathMode.TeamYellow.Remove(target.PlayerId);
                    SuddenDeathMode.TeamGreen.Add(target.PlayerId);
                    SuddenDeathMode.TeamPurple.Remove(target.PlayerId);
                }
                if (SuddenDeathMode.TeamPurple.Contains(Player.PlayerId))
                {
                    SuddenDeathMode.TeamRed.Remove(target.PlayerId);
                    SuddenDeathMode.TeamBlue.Remove(target.PlayerId);
                    SuddenDeathMode.TeamYellow.Remove(target.PlayerId);
                    SuddenDeathMode.TeamGreen.Remove(target.PlayerId);
                    SuddenDeathMode.TeamPurple.Add(target.PlayerId);
                }
            }
            SK = false;
            Player.RpcProtectedMurderPlayer(target);
            target.RpcProtectedMurderPlayer(Player);
            target.RpcProtectedMurderPlayer(target);
            UtilsGameLog.AddGameLog($"SideKick", string.Format(GetString("log.Sidekick"), Utils.GetPlayerColor(target, true) + $"({UtilsRoleText.GetTrueRoleName(target.PlayerId)})", Utils.GetPlayerColor(Player, true)));
            target.RpcSetCustomRole(CustomRoles.Jackaldoll);
            if (!Utils.RoleSendList.Contains(target.PlayerId)) Utils.RoleSendList.Add(target.PlayerId);
            JackalDoll.Sidekick(target, Player);
            UtilsOption.MarkEveryoneDirtySettings();
            JackalDoll.side++;
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
            || JackalDoll.GetSideKickCount() <= JackalDoll.side || !SK) return "";

            if (isForHud) return GetString("PhantomButtonSideKick");
            return $"<size=50%>{GetString("PhantomButtonSideKick")}</size>";
        }
    }
}