using System;
using System.Collections.Generic;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using Hazel;

namespace TownOfHost.Roles.Impostor
{
    public sealed class Limiter : RoleBase, IImpostor
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Limiter),
                player => new Limiter(player),
                CustomRoles.Limiter,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Impostor,
                6700,
                SetupOptionItem,
                "Lm",
                OptionSort: (7, 3)
            );
        public Limiter(PlayerControl player)
        : base(
            RoleInfo,
            player
        )
        {
            LimiterTurnLimit = OptionLimiterTurnLimit.GetFloat();
            blastrange = Optionblastrange.GetFloat();
            KillCooldown = OptionKillCooldown.GetFloat();
            LimitTimer = OptionLimitTimer.GetFloat() != 0;
            Timer = 0;
            killcount = 0;
        }

        static OptionItem OptionLimiterTurnLimit;
        static OptionItem OptionLastTurnKillcool;
        static OptionItem Optionblastrange;
        static OptionItem OptionKillCooldown;
        static OptionItem OptionLimitTimer;
        static OptionItem OptionLimitKill;
        static OptionItem OptionLimitMeeting;
        enum OptionName
        {
            LimiterTurnLimit,
            LimiterLastTurnKillCool,
            LimiterTimeLimit,
            LimiterKillLimit,
            LimiterLimitMeeting,
            blastrange,
        }
        static bool LimitTimer;
        static float LimiterTurnLimit;
        static float blastrange;
        static float KillCooldown;
        bool Limit;
        float Timer;
        int killcount;

        public bool CanBeLastImpostor { get; } = false;

        private static void SetupOptionItem()
        {
            OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 9, GeneralOption.KillCooldown, new(0f, 180f, 0.5f), 25f, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionLastTurnKillcool = FloatOptionItem.Create(RoleInfo, 10, OptionName.LimiterLastTurnKillCool, new(0f, 180f, 0.5f), 25f, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionLimitMeeting = BooleanOptionItem.Create(RoleInfo, 15, OptionName.LimiterLimitMeeting, false, false);
            OptionLimiterTurnLimit = IntegerOptionItem.Create(RoleInfo, 11, OptionName.LimiterTurnLimit, new(0, 15, 1), 3, false).SetZeroNotation(OptionZeroNotation.Off).SetValueFormat(OptionFormat.day);
            OptionLimitTimer = FloatOptionItem.Create(RoleInfo, 13, OptionName.LimiterTimeLimit, new(0, 300, 5), 180, false).SetZeroNotation(OptionZeroNotation.Infinity)
                .SetValueFormat(OptionFormat.Seconds);
            OptionLimitKill = IntegerOptionItem.Create(RoleInfo, 14, OptionName.LimiterKillLimit, new(0, 15, 1), 4, false).SetZeroNotation(OptionZeroNotation.Off);
            Optionblastrange = FloatOptionItem.Create(RoleInfo, 12, OptionName.blastrange, new(0.5f, 20f, 0.5f), 5f, false);
        }
        public float CalculateKillCooldown() => KillCooldown;
        public override void OnFixedUpdate(PlayerControl player)
        {
            if (GameStates.Intro || GameStates.CalledMeeting) return;
            if (Limit) return;
            if (!player.IsAlive()) return;
            if (!LimitTimer) return;
            if (AddOns.Common.Amnesia.CheckAbilityreturn(player)) return;

            Timer += Time.fixedDeltaTime;

            if (Timer > OptionLimitTimer.GetFloat())
            {
                Limit = true;

                if (!AmongUsClient.Instance.AmHost) return;

                _ = new LateTask(() =>
                {
                    player.SetKillCooldown(OptionLastTurnKillcool.GetFloat(), delay: true);
                    UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player);
                }, 0.3f, "Limiter Time Limit");
            }
        }
        public void OnCheckMurderAsKiller(MurderInfo info)
        {
            var Targets = new List<PlayerControl>(PlayerCatch.AllAlivePlayerControls);//.Where(pc => !Player)
            if (Limit)
            {
                info.DoKill = false;
                foreach (var target in Targets)
                {
                    var distance = Vector3.Distance(Player.transform.position, target.transform.position);
                    if (distance > blastrange) continue;
                    if (target.PlayerId == Player.PlayerId)
                    {
                        PlayerState.GetByPlayerId(Player.PlayerId).DeathReason = CustomDeathReason.Bombed;
                        Player.RpcMurderPlayer(Player);
                        continue;
                    }
                    if (CustomRoleManager.OnCheckMurder(Player, target, target, target, true, true, 10, CustomDeathReason.Bombed))
                    {
                        target.SetRealKiller(target);
                    }
                }
            }
        }
        public void OnMurderPlayerAsKiller(MurderInfo info)
        {
            var (killer, target) = info.AppearanceTuple;
            if (killer.PlayerId == Player.PlayerId)
            {
                if (Limit) return;
                if (!Player.IsAlive()) return;
                if (OptionLimitKill.GetInt() == 0) return;
                if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;

                killcount++;
                if (OptionLimitKill.GetInt() <= killcount)
                {
                    Limit = true;

                    _ = new LateTask(() =>
                    {
                        Player.SetKillCooldown(OptionLastTurnKillcool.GetFloat(), delay: true);
                        UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player);
                    }, 0.3f, "Limiter Kill Limit");
                }
                SendRPC();
            }
        }
        public bool OverrideKillButtonText(out string text)
        {
            text = GetString("FireWorksBomberExplosionButtonText");
            return Limit;
        }
        public bool OverrideKillButton(out string text)
        {
            text = "Limiter_Kill";
            return Limit;
        }

        public override void AfterMeetingTasks()//一旦はアムネシア中なら回避してるけどリミッターは削除してあげてもいいかも
        {
            if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;
            if (LimiterTurnLimit == 0) return;

            if (UtilsGameLog.day >= LimiterTurnLimit && Player.IsAlive())
            {
                Limit = true;
                _ = new LateTask(() => Player.SetKillCooldown(OptionLastTurnKillcool.GetFloat()), 5f, "Limiter Limit Kill cool");
            }
        }
        public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
        {
            seen ??= seer;
            if (seen != seer) return "";
            if (isForMeeting) return "";
            if (Limit && Player.IsAlive())
            {
                return Utils.ColorString(Color.red, GetString("LimiterBom"));
            }
            else
                return "";
        }
        public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
        {
            seen ??= seer;
            if (seen != seer) return "";

            if (isForMeeting && Limit && Player.IsAlive())
                return $"<size=60%>{Utils.ColorString(Color.red, GetString("LimiterBom"))}</size>";

            if (isForMeeting)
            {
                var Limittext = "";
                if (!Player.IsAlive()) return "";
                if (LimitTimer)
                {
                    int now = (int)Math.Round(Timer);
                    int nokori = (int)(OptionLimitTimer.GetFloat() - now);
                    Limittext += $"(Ⓣ{nokori}s)";
                }
                if (LimiterTurnLimit != 0)
                    Limittext += $"(Ⓓ{UtilsGameLog.day}/{LimiterTurnLimit})";
                if (OptionLimitKill.GetInt() != 0)
                    Limittext += $"(Ⓚ{killcount}/{OptionLimitKill.GetInt()})";
                return $"<size=60%>{Utils.ColorString(ModColors.MadMateOrenge, Limittext)}</size>";
            }
            return "";
        }
        public override void OnReportDeadBody(PlayerControl repo, NetworkedPlayerInfo target)
        {
            if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;
            if (OptionLimitMeeting.GetBool()) return;
            if (Limit && Player.IsAlive())
            {
                MyState.DeathReason = CustomDeathReason.Bombed;
                Player.SetRealKiller(Player);
                Player.RpcMurderPlayer(Player, true);
            }
        }

        public void SendRPC()
        {
            using var sender = CreateSender();
            sender.Writer.Write(Limit);
            sender.Writer.Write(killcount);
        }

        public override void ReceiveRPC(MessageReader reader)
        {
            Limit = reader.ReadBoolean();
            killcount = reader.ReadInt32();
        }
    }
}
