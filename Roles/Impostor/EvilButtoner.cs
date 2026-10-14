using AmongUs.GameOptions;
using Hazel;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor
{
    public sealed class EvilButtoner : RoleBase, IImpostor, IUsePhantomButton, IDoubleTrigger
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(EvilButtoner),
                player => new EvilButtoner(player),
                CustomRoles.EvilButtoner,
                () => RoleTypes.Phantom,
                CustomRoleTypes.Impostor,
                25400,
                SetupOptionItem,
                "Evb",
                OptionSort: (2, 4)
            );
        public EvilButtoner(PlayerControl player)
        : base(
            RoleInfo,
            player
        )
        {
            killcooldown = OptionKillCoolDown.GetFloat();
            abilitycooldown = OptionAbilityCooldown.GetFloat();
            CanUseMeCall = OptionCanUseMeCall.GetBool();
            Limit = OptionMaximum.GetInt();
            Player.AddDoubleTrigger();
        }
        static OptionItem OptionKillCoolDown; static float killcooldown;
        static OptionItem OptionMaximum; int Limit;
        static OptionItem OptionAbilityCooldown; static float abilitycooldown;
        static OptionItem OptionCanUseMeCall; static bool CanUseMeCall;
        byte TargetId;
        static void SetupOptionItem()
        {
            OptionKillCoolDown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, OptionBaseCoolTime, 30, false).SetValueFormat(OptionFormat.Seconds);
            OptionMaximum = IntegerOptionItem.Create(RoleInfo, 12, GeneralOption.OptionCount, new(1, 99, 1), 2, false);
            OptionAbilityCooldown = FloatOptionItem.Create(RoleInfo, 13, GeneralOption.Cooldown, OptionBaseCoolTime, 30, false).SetValueFormat(OptionFormat.Seconds);
            OptionCanUseMeCall = BooleanOptionItem.Create(RoleInfo, 14, "EvilButtonerCanUseMeCall", true, false);
        }
        public override void Add()
        {
            SetTarget(Player, false);
        }
        public float CalculateKillCooldown() => killcooldown;
        public override void ApplyGameOptions(IGameOptions opt)
        {
            AURoleOptions.PhantomCooldown = abilitycooldown;
        }
        public override string GetAbilityButtonText() => GetString("EvilButtoner_AbilityText");
        public override bool CanUseAbilityButton() => 0 < Limit && TargetId != byte.MaxValue;
        public bool SingleAction(PlayerControl killer, PlayerControl target)
        {
            if (Is(killer) is false) return true;
            SetTarget(target);
            return false;
        }
        public bool DoubleAction(PlayerControl killer, PlayerControl target) => true;
        void SetTarget(PlayerControl target, bool IsSendRpc = true)
        {
            if (Limit <= 0 || Player.IsAlive() is false)
            {
                if (TargetId != byte.MaxValue)
                {
                    TargetId = byte.MaxValue;
                    SendRpc();
                }
                return;
            }
            if (target == null)
            {
                TargetId = byte.MaxValue;
            }
            else
            {
                var targetstate = target.GetPlayerState();
                if (Is(target))
                {
                    if (CanUseMeCall && 0 < targetstate.NumberOfRemainingButtons)
                    {
                        TargetId = target.PlayerId;
                    }
                    else
                    {
                        TargetId = byte.MaxValue;
                    }
                }
                else
                {
                    if (0 < targetstate.NumberOfRemainingButtons)
                        TargetId = target.PlayerId;
                }
            }
            if (IsSendRpc)
            {
                SendRpc();
                UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player);
            }
        }
        public void OnClick(ref bool AdjustKillCooldown, ref bool? ResetCooldown)
        {
            AdjustKillCooldown = true;
            ResetCooldown = true;
            if (TargetId == byte.MaxValue || Limit <= 0) return;

            var target = PlayerCatch.GetPlayerById(TargetId);
            Limit--;
            ReportDeadBodyPatch.ExReportDeadBody(target, null);
        }
        public override void OnStartMeeting()
        {
            SetTarget(Player, false);
        }
        public override string GetProgressText(bool comms = false, bool GameLog = false) => $"({Limit})";
        public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
        {
            seen ??= seer;
            if (isForMeeting || seer.IsAlive() is false) return "";

            var targetname = GetString("Unsettled");
            var target = TargetId.GetPlayerControl();
            if (target) targetname = target.GetRealName(true);
            return string.Format(GetString("EvilButtoner_LowerText"), targetname);
        }
        bool IUsePhantomButton.IsresetAfterKill => false;
        public override void OnFixedUpdate(PlayerControl player)
        {
            if (AmongUsClient.Instance.AmHost is false || TargetId is byte.MaxValue || player.IsAlive() is false) return;
            var target = TargetId.GetPlayerControl();
            if (target.IsAlive() is false) SetTarget(player);
        }
        void SendRpc()
        {
            using var sender = CreateSender();
            sender.Writer.Write(TargetId);
            sender.Writer.Write(Limit);
        }
        public override void ReceiveRPC(MessageReader reader)
        {
            TargetId = reader.ReadByte();
            Limit = reader.ReadInt32();
        }
    }
}