using System.Linq;
using AmongUs.GameOptions;
using Hazel;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Neutral
{
    public sealed class Jackal : RoleBase, ILNKiller, ISchrodingerCatOwner, IUsePhantomButton
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Jackal),
                player => new Jackal(player),
                CustomRoles.Jackal,
                () => OptionCanMakeSidekick.GetBool() ? RoleTypes.Phantom : RoleTypes.Impostor,
                CustomRoleTypes.Neutral,
                12900,
                SetupOptionItem,
                "jac",
                "#00b4eb",
                (1, 0),
                true,
                countType: CountTypes.Jackal,
                assignInfo: new RoleAssignInfo(CustomRoles.Jackal, CustomRoleTypes.Neutral)
                {
                    AssignCountRule = new(1, 1, 1)
                },
                from: From.TheOtherRoles,
                Desc: () =>
                {
                    return string.Format(GetString("JackalDesc"), OptionCanMakeSidekick.GetBool() ? string.Format(GetString("JackalDescSidekick"), !OptionImpostorCanSidekick.GetBool() ? GetString("JackalDescImpostorSideKick") : "") : "");
                }
            );
        public Jackal(PlayerControl player)
        : base(
            RoleInfo,
            player,
            () => HasTask.False
        )
        {
            KillCooldown = OptionKillCooldown.GetFloat();
            CanVent = OptionCanVent.GetBool();
            Cooldown = OptionCooldown.GetFloat();
            CanUseSabotage = OptionCanUseSabotage.GetBool();
            CanSideKick = OptionCanMakeSidekick.GetBool();
        }

        public static OptionItem OptionKillCooldown;
        private static OptionItem OptionCooldown;
        public static OptionItem OptionCanVent;
        public static OptionItem OptionCanUseSabotage;
        static OptionItem OptionHasImpostorVision;
        public static OptionItem OptionCanMakeSidekick;
        static OptionItem OptionImpostorCanSidekick;
        //サイドキックが元仲間の色を見える
        public static OptionItem OptionSidekickCanSeeOldImpostorTeammates;
        //元仲間impがサイドキック相手の名前の色を見える
        public static OptionItem OptionImpostorCanSeeNameColor;
        public static OptionItem OptionSidekickPromotion;
        private static float KillCooldown;
        private static float Cooldown;
        public static bool CanVent;
        public static bool CanUseSabotage;
        bool CanSideKick;

        enum OptionName { JackalSidekickPromotion, JackalImpostorCanSidekick, JackalbeforeImpCanSeeImp, Jackaldollimpgaimpnimieru }

        public ISchrodingerCatOwner.TeamType SchrodingerCatChangeTo => ISchrodingerCatOwner.TeamType.Jackal;

        private static void SetupOptionItem()
        {
            OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 0.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionCanVent = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.CanVent, true, false);
            OptionCanUseSabotage = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.CanUseSabotage, false, false);
            OptionHasImpostorVision = BooleanOptionItem.Create(RoleInfo, 13, GeneralOption.ImpostorVision, true, false);

            ObjectOptionitem.Create(RoleInfo, 9, "SideKickOption", true, null).SetOptionName(() => "Sidekick Setting");
            OptionCanMakeSidekick = BooleanOptionItem.Create(RoleInfo, 14, GeneralOption.CanCreateSideKick, true, false);
            OptionImpostorCanSidekick = BooleanOptionItem.Create(RoleInfo, 15, OptionName.JackalImpostorCanSidekick, false, false, OptionCanMakeSidekick);
            OptionSidekickCanSeeOldImpostorTeammates = BooleanOptionItem.Create(RoleInfo, 16, OptionName.JackalbeforeImpCanSeeImp, false, false, OptionImpostorCanSidekick);
            OptionImpostorCanSeeNameColor = BooleanOptionItem.Create(RoleInfo, 17, OptionName.Jackaldollimpgaimpnimieru, false, false, OptionImpostorCanSidekick);
            OptionCooldown = FloatOptionItem.Create(RoleInfo, 18, GeneralOption.Cooldown, new(0f, 180f, 0.5f), 30f, false, OptionCanMakeSidekick)
                .SetValueFormat(OptionFormat.Seconds);
            OptionSidekickPromotion = BooleanOptionItem.Create(RoleInfo, 19, OptionName.JackalSidekickPromotion, false, false, OptionCanMakeSidekick);
            RoleAddAddons.Create(RoleInfo, 20, NeutralKiller: true);
        }
        public float CalculateKillCooldown() => KillCooldown;
        public bool CanUseSabotageButton() => CanUseSabotage;
        public bool CanUseImpostorVentButton() => CanVent;
        public override void ApplyGameOptions(IGameOptions opt)
        {
            opt.SetVision(OptionHasImpostorVision.GetBool());
            AURoleOptions.PhantomCooldown = JackalDoll.GetSideKickCount() <= JackalDoll.NowSideKickCount ? 200f : Cooldown;
        }
        public void ApplySchrodingerCatOptions(IGameOptions option) => ApplyGameOptions(option);
        public bool UseOneclickButton => CanSideKick;
        public override bool CanUseAbilityButton() => CanSideKick;
        bool IUsePhantomButton.IsPhantomRole => JackalDoll.GetSideKickCount() > JackalDoll.NowSideKickCount;
        bool IUsePhantomButton.IsresetAfterKill => false;
        public void OnClick(ref bool AdjustKillCooldown, ref bool? ResetCooldown)
        {
            AdjustKillCooldown = true;
            if (!CanSideKick) return;

            if (JackalDoll.GetSideKickCount() <= JackalDoll.NowSideKickCount)
            {
                CanSideKick = false;
                SendRPC();
                return;
            }
            var target = Player.GetKillTarget(true);
            if (target == null)
            {
                ResetCooldown = false;
                return;
            }
            var targetrole = target.GetCustomRole();
            if ((targetrole is CustomRoles.King or CustomRoles.Jackal or CustomRoles.JackalAlien or CustomRoles.Jackaldoll or CustomRoles.JackalMafia or CustomRoles.Merlin)
            || ((targetrole.IsImpostor() || targetrole is CustomRoles.Egoist) && !OptionImpostorCanSidekick.GetBool()))
            {
                ResetCooldown = false;
                return;
            }
            if (SuddenDeathMode.NowSuddenDeathTemeMode)
            {
                target.SideKickChangeTeam(Player);
            }
            CanSideKick = false;
            SendRPC();
            if (target.Is(CustomRoleTypes.Impostor)) Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[1]);
            Player.RpcProtectedMurderPlayer(target);
            target.RpcProtectedMurderPlayer(Player);
            target.RpcProtectedMurderPlayer(target);
            UtilsGameLog.AddGameLog($"SideKick", string.Format(GetString("log.Sidekick"), UtilsName.GetPlayerColor(target, true) + $"({UtilsRoleText.GetTrueRoleName(target.PlayerId)})", UtilsName.GetPlayerColor(Player, true)));
            target.RpcSetCustomRole(CustomRoles.Jackaldoll, log: null);
            JackalDoll.Sidekick(target, Player);
            if (!Utils.RoleSendList.Contains(target.PlayerId)) Utils.RoleSendList.Add(target.PlayerId);
            UtilsOption.MarkEveryoneDirtySettings();
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
            if (seen.PlayerId != seer.PlayerId || isForMeeting || !Player.IsAlive() || JackalDoll.GetSideKickCount() <= JackalDoll.NowSideKickCount || !CanSideKick) return "";

            if (isForHud) return GetString("PhantomButtonSideKick");
            return $"<size=50%>{GetString("PhantomButtonSideKick")}</size>";
        }
        public void SendRPC()
        {
            using var sender = CreateSender();
            sender.Writer.Write(CanSideKick);
        }
        public override void ReceiveRPC(MessageReader reader)
        {
            CanSideKick = reader.ReadBoolean();
        }
        public override void CheckWinner(GameOverReason reason)
        {
            if (3 <= MyState.GetKillCount()) Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[0]);
            if (Player.IsWinner(CustomWinner.Jackal) && !Player.IsLovers())
            {
                foreach (var j in PlayerCatch.AllPlayerControls.Where(pc => pc.Is(CountTypes.Jackal)))
                {
                    if (j.GetPlayerState().GetKillCount() > 0) return;
                }
                Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[2]);
            }
        }
        public static System.Collections.Generic.Dictionary<int, Achievement> achievements = new();
        [Attributes.PluginModuleInitializer]
        public static void Load()
        {
            var n1 = new Achievement(RoleInfo, 0, 1, 0, 0);
            var l1 = new Achievement(RoleInfo, 1, 1, 0, 1);
            var sp1 = new Achievement(RoleInfo, 2, 1, 0, 3, true);
            achievements.Add(0, n1);
            achievements.Add(1, l1);
            achievements.Add(2, sp1);
        }
    }
}