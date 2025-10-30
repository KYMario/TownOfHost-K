using AmongUs.GameOptions;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using UnityEngine;

namespace TownOfHost.Roles.Neutral
{
    public sealed class GrimReaper : RoleBase, ILNKiller//死神はにゃんこを仲間にできない
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(GrimReaper),
                player => new GrimReaper(player),
                CustomRoles.GrimReaper,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Neutral,
                13500,
                SetupOptionItem,
                "gr",
                "#4b0082",
                (2, 2),
                true,
                countType: CountTypes.GrimReaper,//こいつ生存カウント分ける。(生存カウント入れないため)
                assignInfo: new RoleAssignInfo(CustomRoles.GrimReaper, CustomRoleTypes.Neutral)
                {
                    AssignCountRule = new(1, 1, 1)
                }
            );
        public GrimReaper(PlayerControl player)
        : base(
            RoleInfo,
            player,
            () => HasTask.False
        )
        {
            KillCooldown = OptionKillCooldown.GetFloat();
            CanVent = OptionCanVent.GetBool();
            CanUseSabotage = OptionCanUseSabotage.GetBool();
            GrimReaperCanButtom = OptionGrimReaperCanButtom.GetBool();
            cankill = true;
        }

        private static OptionItem OptionKillCooldown;
        public static OptionItem OptionCanVent;
        public static OptionItem OptionCanUseSabotage;
        private static OptionItem OptionGrimReaperCanButtom;
        private static OptionItem OptionGrimActiveTime;
        private static float KillCooldown;
        public static bool CanVent;
        public static bool CanUseSabotage;
        private static bool GrimReaperCanButtom;
        bool cankill;
        enum OptionName
        {
            GrimReaperCanButtom,
            GrimReaperGrimActiveTime
        }

        Dictionary<byte, float> GrimPlayers = new(14);
        private static void SetupOptionItem()
        {
            SoloWinOption.Create(RoleInfo, 9, defo: 0);
            OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 0.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionGrimActiveTime = FloatOptionItem.Create(RoleInfo, 8, OptionName.GrimReaperGrimActiveTime, new(3, 300, 1), 15, false).SetValueFormat(OptionFormat.Seconds);
            OptionCanVent = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.CanVent, true, false);
            OptionCanUseSabotage = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.CanUseSabotage, false, false);//正味サボ使用不可でもいい気がする
            OptionGrimReaperCanButtom = BooleanOptionItem.Create(RoleInfo, 14, OptionName.GrimReaperCanButtom, false, false);
            RoleAddAddons.Create(RoleInfo, 15);
        }
        public float CalculateKillCooldown() => KillCooldown;
        public bool CanUseSabotageButton() => CanUseSabotage;
        public bool CanUseImpostorVentButton() => CanVent;
        public void OnCheckMurderAsKiller(MurderInfo info)
        {
            info.DoKill = false;

            if (!info.IsSuicide)
            {
                (var kille, var taret) = info.AttemptTuple;
                {
                    Logger.Info($"{kille?.Data?.GetLogPlayerName()}:キル", "GrimReaper");
                    Main.AllPlayerKillCooldown[kille.PlayerId] = 200f;
                    kille.SyncSettings();//もう君はキルできないよ...!
                }
            }

            var (killer, target) = info.AttemptTuple;

            if (info.IsFakeSuicide) return;

            if (cankill)
            {
                if (!GrimPlayers.ContainsKey(target.PlayerId))
                {
                    killer.SetKillCooldown(delay: true);
                    GrimPlayers.Add(target.PlayerId, OptionGrimActiveTime.GetFloat());
                    cankill = false;
                    RpcAddList(target.PlayerId);
                }
                UtilsNotifyRoles.NotifyRoles(SpecifySeer: [Player]);
            }
        }
        public override void OnReportDeadBody(PlayerControl repo, NetworkedPlayerInfo __)
        {
            if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;
            foreach (var targetId in GrimPlayers.Keys)
            {
                var target = PlayerCatch.GetPlayerById(targetId);
                KillBitten(target, true);
            }
            GrimPlayers.Clear();
        }

        public override void OnStartMeeting() => GrimPlayers.Clear();//ホスト以外はこっちでリセット

        public override void AfterMeetingTasks()
        {
            cankill = true;
            if (Player.Is(CustomRoles.Amnesia) && AddOns.Common.Amnesia.OptionDefaultKillCool.GetBool()) return;
            Main.AllPlayerKillCooldown[Player.PlayerId] = KillCooldown;
            if (!AmongUsClient.Instance.AmHost) return;
            Player.SyncSettings();
        }
        public override void OnFixedUpdate(PlayerControl player)
        {
            if (!GameStates.IsInTask || GameStates.CalledMeeting) return;

            List<byte> del = new();
            foreach (var (targetId, timer) in GrimPlayers)
            {
                if (timer < 0)
                {
                    del.Add(targetId);
                    cankill = true;
                    Main.AllPlayerKillCooldown[Player.PlayerId] = KillCooldown;
                    Player.SetKillCooldown(KillCooldown * 0.5f);
                    UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player);
                }
                else
                {
                    GrimPlayers[targetId] -= Time.fixedDeltaTime;
                }
            }
            del.Do(x => GrimPlayers.Remove(x));
        }
        private void KillBitten(PlayerControl target, bool isButton = false)
        {
            if (target == null) return;
            var Grim = Player;
            if (target.IsAlive() && Player.IsAlive())
            {
                PlayerState.GetByPlayerId(target.PlayerId).DeathReason = CustomDeathReason.Grim;
                target.SetRealKiller(Grim);
                CustomRoleManager.OnCheckMurder(Grim, target, target, target, true, true, Killpower: 2);
                if (!isButton && Grim.IsAlive()) RPC.PlaySoundRPC(Grim.PlayerId, Sounds.KillSound);
            }
        }
        public override bool CancelReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target, ref DontReportreson reportreson)
        {
            if (reporter.Is(CustomRoles.GrimReaper) && target != null)//死体通報はデフォでさせない。
            {
                reportreson = DontReportreson.Other;
                return true;
            }
            if (reporter.Is(CustomRoles.GrimReaper) && target == null && !GrimReaperCanButtom)//ボタン使用不可でボタンであろう場面のみ
            {
                reportreson = DontReportreson.Other;
                return true;
            }
            return false;
        }
        public bool OverrideKillButtonText(out string text)
        {
            text = GetString("WarlockCurseButtonText");
            return true;
        }
        public bool OverrideKillButton(out string text)
        {
            text = "Grim_Kill";
            return true;
        }
        public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
        {
            seen ??= seer;
            if (isForMeeting || !Player.IsAlive() || cankill || GrimPlayers.Count == 0) return "";

            if (seen.PlayerId == Player.PlayerId)
            {
                return Utils.ColorString(Main.PlayerColors.TryGetValue(GrimPlayers?.Keys?.FirstOrDefault() ?? byte.MaxValue, out var color) ? color : Color.white, "◆");
            }

            return GrimPlayers.ContainsKey(seen.PlayerId) ? $"<{RoleInfo.RoleColorCode}>◆</color>" : "";
        }

        public void RpcAddList(byte targetId)
        {
            using var sender = CreateSender();
            sender.Writer.Write(true);
            sender.Writer.Write(targetId);
            sender.Writer.Write(cankill);
        }

        public override void ReceiveRPC(MessageReader reader)
        {
            var targetId = reader.ReadByte();
            cankill = reader.ReadBoolean();
            var result = GrimPlayers.TryAdd(targetId, OptionGrimActiveTime.GetFloat());
            if (!result)
            {
                Logger.Warn($"既に{targetId}はGrimPlayersに含まれていたため、追加に失敗しました", "GrimReaper");
            }
        }
    }
}