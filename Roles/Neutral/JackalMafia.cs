using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Neutral
{
    public sealed class JackalMafia : RoleBase, ILNKiller, ISchrodingerCatOwner, IUseTheShButton
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(JackalMafia),
                player => new JackalMafia(player),
                CustomRoles.JackalMafia,
                () => CanmakeSK.GetBool() ? RoleTypes.Shapeshifter : RoleTypes.Impostor,
                CustomRoleTypes.Neutral,
                51100,
                SetupOptionItem,
                "jm",
                "#00b4eb",
                true,
                countType: CountTypes.Jackal,
                assignInfo: new RoleAssignInfo(CustomRoles.JackalMafia, CustomRoleTypes.Neutral)
                {
                    AssignCountRule = new(1, 1, 1)
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
            HasImpostorVision = OptionHasImpostorVision.GetBool();
            JackalCanKillMafia = OptionJJackalCanKillMafia.GetBool();
            JackalCanAlsoBeExposedToJMafia = OptionJackalCanAlsoBeExposedToJMafia.GetBool();
            JackalMafiaCanAlsoBeExposedToJackal = OptionJJackalMafiaCanAlsoBeExposedToJackal.GetBool();
            CustomRoleManager.MarkOthers.Add(GetMarkOthers);
            SK = CanmakeSK.GetBool();
            Fall = false;
        }

        public static OptionItem OptionKillCooldown;
        private static OptionItem OptionCooldown;
        public static OptionItem OptionCanVent;
        public static OptionItem OptionCanUseSabotage;
        private static OptionItem OptionHasImpostorVision;
        private static OptionItem OptionJackalCanAlsoBeExposedToJMafia;
        private static OptionItem OptionJJackalMafiaCanAlsoBeExposedToJackal;
        private static OptionItem OptionJJackalCanKillMafia;
        static OptionItem CanImpSK;
        //サイドキックが元仲間の色を見える
        public static OptionItem SKcanImp;
        //元仲間impがサイドキック相手の名前の色を見える
        public static OptionItem SKimpwocanimp;
        static OptionItem CanmakeSK;
        public static OptionItem OptionDoll;
        private static float KillCooldown;
        private static float Cooldown;
        public static bool CanVent;
        public static bool CanUseSabotage;
        private static bool HasImpostorVision;
        private static bool JackalCanAlsoBeExposedToJMafia;
        private static bool JackalMafiaCanAlsoBeExposedToJackal;
        private static bool JackalCanKillMafia;
        bool SK;
        bool Fall;
        enum JackalOption
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
            OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, false)
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
            OptionCooldown = FloatOptionItem.Create(RoleInfo, 23, GeneralOption.Cooldown, new(0f, 180f, 2.5f), 30f, false, CanmakeSK)
            .SetValueFormat(OptionFormat.Seconds);
            OptionDoll = BooleanOptionItem.Create(RoleInfo, 24, JackalOption.JackaldollShoukaku, false, false, CanmakeSK);
            RoleAddAddons.Create(RoleInfo, 25);
        }   //↑あってるかは知らない、
        public SchrodingerCat.TeamType SchrodingerCatChangeTo => SchrodingerCat.TeamType.Jackal;
        public float CalculateKillCooldown() => KillCooldown;
        public bool CanUseSabotageButton() => CanUseSabotage;
        public bool CanUseImpostorVentButton() => CanVent;
        public override bool OnInvokeSabotage(SystemTypes systemType) => CanUseSabotage;
        public override void ApplyGameOptions(IGameOptions opt)
        {
            AURoleOptions.ShapeshifterCooldown = Fall ? 1f : Cooldown;
            AURoleOptions.ShapeshifterDuration = 1f;
            opt.SetVision(HasImpostorVision);
        }
        public void ApplySchrodingerCatOptions(IGameOptions option) => ApplyGameOptions(option);
        public bool UseOCButton => SK;
        public override bool CanUseAbilityButton() => SK;
        public override void AfterMeetingTasks()
        {
            Fall = false;
            Player.SyncSettings();
        }
        public void OnClick()
        {
            if (!SK) return;
            if (JackalDoll.sidekick.GetInt() <= JackalDoll.side)
            {
                SK = false;
                return;
            }
            var target = Player.GetKillTarget();
            if (target == null || target.Is(CustomRoles.Jackaldoll) || target.Is(CustomRoles.Jackal) || target.Is(CustomRoles.JackalMafia) || ((target.GetCustomRole().IsImpostor() || target.Is(CustomRoles.Egoist)) && !CanImpSK.GetBool()))
            {
                Fall = true;
                _ = new LateTask(() => Player.MarkDirtySettings(), Main.LagTime, "SidekickFall");
                _ = new LateTask(() => Player.RpcResetAbilityCooldown(), 0.4f + Main.LagTime, "SidekickFall");
                return;
            }
            SK = false;
            Player.RpcProtectedMurderPlayer(target);
            target.RpcProtectedMurderPlayer(Player);
            target.RpcProtectedMurderPlayer(target);
            Main.gamelog += $"\n{System.DateTime.Now:HH.mm.ss} [Sidekick]　" + string.Format(Translator.GetString("log.Sidekick"), Utils.GetPlayerColor(target, true) + $"({Utils.GetTrueRoleName(target.PlayerId)})", Utils.GetPlayerColor(Player, true) + $"({Utils.GetTrueRoleName(Player.PlayerId)})");
            target.RpcSetCustomRole(CustomRoles.Jackaldoll);
            JackalDoll.Sidekick(target, Player);
            Main.FixTaskNoPlayer.Add(target);
            Utils.MarkEveryoneDirtySettings();
            Utils.NotifyRoles();
            Utils.DelTask();
            JackalDoll.side++;
            Main.LastLogRole[target.PlayerId] += "<b>⇒" + Utils.ColorString(Utils.GetRoleColor(target.GetCustomRole()), Translator.GetString($"{target.GetCustomRole()}")) + "</b>" + Utils.GetSubRolesText(target.PlayerId);
        }
        public bool CanUseKillButton()
        {
            if (PlayerState.AllPlayerStates == null) return false;
            int livingImpostorsNum = 0;
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                var role = pc.GetCustomRole();
                if (role == CustomRoles.Jackal) livingImpostorsNum++;
            }
            return livingImpostorsNum <= 0;
        }
        public override bool OnCheckMurderAsTarget(MurderInfo info)
        {
            (var killer, var target) = info.AttemptTuple;
            if (killer.Is(CustomRoles.Jackal) && !JackalCanKillMafia)
            {
                info.CanKill = false;
                return false;
            }
            return true;
        }
        public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
        {
            //seenが省略の場合seer
            seen ??= seer;
            if (seer.PlayerId == Player.PlayerId && seen.Is(CustomRoles.Jackal) && JackalMafiaCanAlsoBeExposedToJackal) return Utils.ColorString(RoleInfo.RoleColor, "★");
            else return "";
        }
        public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
        {
            seen ??= seer;
            if (seer.Is(CustomRoles.Jackal) && seen.Is(CustomRoles.JackalMafia) && JackalCanAlsoBeExposedToJMafia) return Utils.ColorString(RoleInfo.RoleColor, "★");
            else return "";
        }
    }
}