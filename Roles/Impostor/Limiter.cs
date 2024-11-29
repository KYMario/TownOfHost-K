using System.Collections.Generic;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Translator;
using System;

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
                6100,
                SetupOptionItem,
                "Lm"
            );
        public Limiter(PlayerControl player)
        : base(
            RoleInfo,
            player
        )
        {
            LimiterTarnLimit = OptionLimiterTarnLimit.GetFloat();
            blastrange = Optionblastrange.GetFloat();
            KillCooldown = OptionKillCooldown.GetFloat();
            LimitTimer = OptionLimitTimer.GetFloat() != 0;
            Timer = 0;
            killcount = 0;
        }

        static OptionItem OptionLimiterTarnLimit;
        static OptionItem OptionLastTarnKillcool;
        static OptionItem Optionblastrange;
        static OptionItem OptionKillCooldown;
        static OptionItem OptionLimitTimer;
        static OptionItem OptionLimitKill;
        static OptionItem OptionLimitMeeting;
        enum OptionName
        {
            LimiterTarnLimit,
            LimiterLastTarnKillCool,
            LimiterTimeLimit,
            LimiterKillLimit,
            LimiterLimitMeeting,
            blastrange,
        }
        static bool LimitTimer;
        float LimiterTarnLimit;
        float blastrange;
        float KillCooldown;
        bool Limit;
        float Timer;
        int killcount;

        public bool CanBeLastImpostor { get; } = false;

        private static void SetupOptionItem()
        {
            OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 9, GeneralOption.KillCooldown, new(0f, 180f, 0.5f), 25f, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionLastTarnKillcool = FloatOptionItem.Create(RoleInfo, 10, OptionName.LimiterLastTarnKillCool, new(0f, 180f, 0.5f), 25f, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionLimitMeeting = BooleanOptionItem.Create(RoleInfo, 15, OptionName.LimiterLimitMeeting, false, false);
            OptionLimiterTarnLimit = FloatOptionItem.Create(RoleInfo, 11, OptionName.LimiterTarnLimit, new(0f, 15f, 1f), 3f, false, infinity: null).SetValueFormat(OptionFormat.day);
            OptionLimitTimer = FloatOptionItem.Create(RoleInfo, 13, OptionName.LimiterTimeLimit, new(0f, 300f, 5f), 180f, false, infinity: true)
                .SetValueFormat(OptionFormat.Seconds);
            OptionLimitKill = FloatOptionItem.Create(RoleInfo, 14, OptionName.LimiterKillLimit, new(0f, 15f, 1f), 4f, false, infinity: null);
            Optionblastrange = FloatOptionItem.Create(RoleInfo, 12, OptionName.blastrange, new(0.5f, 20f, 0.5f), 5f, false);
        }
        public float CalculateKillCooldown() => KillCooldown;
        public override void OnFixedUpdate(PlayerControl player)
        {
            if (!AmongUsClient.Instance.AmHost) return;

            if (GameStates.Intro || GameStates.Meeting) return;
            if (Limit) return;
            if (!player.IsAlive()) return;
            if (!LimitTimer) return;
            if (AddOns.Common.Amnesia.CheckAbilityreturn(player)) return;

            Timer += Time.fixedDeltaTime;

            if (Timer > OptionLimitTimer.GetFloat())
            {
                Limit = true;

                _ = new LateTask(() =>
                {
                    player.SetKillCooldown(OptionLastTarnKillcool.GetFloat(), delay: true);
                    UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player);
                }, 0.3f, "Limiter Time Limit");
            }
        }
        public void OnCheckMurderAsKiller(MurderInfo info)
        {
            var Targets = new List<PlayerControl>(PlayerCatch.AllAlivePlayerControls);//.Where(pc => !Player)
            if (Limit)
                foreach (var tage in Targets)
                {
                    info.DoKill = false;
                    var distance = Vector3.Distance(Player.transform.position, tage.transform.position);
                    if (distance > blastrange) continue;
                    PlayerState.GetByPlayerId(tage.PlayerId).DeathReason = CustomDeathReason.Bombed;
                    tage.SetRealKiller(tage);
                    tage.RpcMurderPlayer(tage, true);
                    RPC.PlaySoundRPC(tage.PlayerId, Sounds.KillSound);
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
                        Player.SetKillCooldown(OptionLastTarnKillcool.GetFloat(), delay: true);
                        UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player);
                    }, 0.3f, "Limiter Kill Limit");
                }
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
            if (LimiterTarnLimit == 0) return;

            if (UtilsGameLog.day >= LimiterTarnLimit && Player.IsAlive())
            {
                Limit = true;
                _ = new LateTask(() => Player.SetKillCooldown(OptionLastTarnKillcool.GetFloat()), 5f, "Limiter Limit Kill cool");
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
                if (LimiterTarnLimit != 0)
                    Limittext += $"(Ⓓ{UtilsGameLog.day}/{LimiterTarnLimit})";
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
                RPC.PlaySoundRPC(Player.PlayerId, Sounds.KillSound);
            }
            if (Limit && repo == Player)
            {
                ReportDeadBodyPatch.DieCheckReport(Player, target);
            }
        }
    }
}
