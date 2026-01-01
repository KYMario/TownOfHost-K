using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AmongUs.GameOptions;
using Hazel;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor
{
    public sealed class Bomber : RoleBase, IImpostor, IUsePhantomButton
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Bomber),
                player => new Bomber(player),
                CustomRoles.Bomber,
                () => RoleTypes.Phantom,
                CustomRoleTypes.Impostor,
                3800,
                SetupOptionItem,
                "bb",
                OptionSort: (3, 1),
                Desc: () => string.Format(GetString("BomberDesc"), OptionBomberExplosion.GetInt(), OptionKillDelay.GetFloat())
            );
        public Bomber(PlayerControl player)
        : base(
            RoleInfo,
            player
        )
        {
            KillDelay = OptionKillDelay.GetFloat();
            Blastrange = OptionBlastrange.GetFloat();
            BomberExplosionPlayers.Clear();
            BomberExplosion = OptionBomberExplosion.GetInt();
            Cooldown = OptionCooldown.GetFloat();
        }

        static OptionItem OptionKillDelay;
        static OptionItem OptionBlastrange;
        static OptionItem OptionBomberExplosion;
        static OptionItem OptionCooldown;
        enum OptionName
        {
            BomberKillDelay,
            blastrange,
            BomberExplosion
        }

        static float KillDelay;
        static float Blastrange;
        static float Cooldown;
        int BomberExplosion;

        public bool CanBeLastImpostor { get; } = false;
        Dictionary<byte, float> BomberExplosionPlayers = new(14);

        private static void SetupOptionItem()
        {
            OptionKillDelay = FloatOptionItem.Create(RoleInfo, 10, OptionName.BomberKillDelay, new(1f, 1000f, 1f), 10f, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionBlastrange = FloatOptionItem.Create(RoleInfo, 11, OptionName.blastrange, new(0.5f, 30f, 0.5f), 1f, false).SetValueFormat(OptionFormat.Multiplier);
            OptionBomberExplosion = IntegerOptionItem.Create(RoleInfo, 12, OptionName.BomberExplosion, new(1, 99, 1), 2, false);
            OptionCooldown = FloatOptionItem.Create(RoleInfo, 13, GeneralOption.Cooldown, new(0f, 999f, 0.5f), 15f, false).SetValueFormat(OptionFormat.Seconds);
        }

        private void SendRPC()
        {
            using var sender = CreateSender();
            sender.Writer.Write(BomberExplosion);
        }
        public override void ReceiveRPC(MessageReader reader)
        {
            BomberExplosion = reader.ReadInt32();
        }
        public void OnClick(ref bool AdjustKillCooldown, ref bool? ResetCooldown)
        {
            if (BomberExplosion <= 0) return;
            ResetCooldown = true;
            var target = Player.GetKillTarget(true);
            Logger.Info($"{Player?.Data?.GetLogPlayerName() ?? "???"} => {target?.Data?.GetLogPlayerName() ?? "失敗"}", "Bomber");
            if (target == null || BomberExplosionPlayers.ContainsKey(target?.PlayerId ?? byte.MaxValue)) return;

            AdjustKillCooldown = false;
            if (!BomberExplosionPlayers.TryAdd(target.PlayerId, 0f)) return;
            BomberExplosion--;
            SendRPC();
            Player.RpcResetAbilityCooldown(Sync: true);
            Player.SetKillCooldown(target: target);
            UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player);
        }
        bool IUsePhantomButton.IsPhantomRole => BomberExplosion > 0;
        public override string GetProgressText(bool comms = false, bool gamelog = false) => Utils.ColorString(0 < BomberExplosion ? Color.red : Color.gray, $"({BomberExplosion})");
        public override void OnFixedUpdate(PlayerControl _)
        {
            if (!AmongUsClient.Instance.AmHost || !GameStates.IsInTask || !Player.IsAlive()) return;

            foreach (var (targetId, timer) in BomberExplosionPlayers.ToArray())
            {
                if (KillDelay <= timer)
                {
                    var target = PlayerCatch.GetPlayerById(targetId);
                    if (target.IsAlive())
                    {
                        var pos = target.transform.position;
                        foreach (var target2 in PlayerCatch.AllAlivePlayerControls)
                        {
                            var dis = Vector2.Distance(pos, target2.transform.position);
                            if (dis > Blastrange) continue;
                            if (CustomRoleManager.OnCheckMurder(Player, target2, target2, target2, true, true, 1, deathReason: CustomDeathReason.Bombed))
                            {
                                RPC.PlaySoundRPC(Player.PlayerId, Sounds.KillSound);
                                Logger.Info($"{target2.name}を爆発させました。", "bomber");
                            }
                        }
                    }

                    BomberExplosionPlayers.Remove(targetId);
                }
                else
                {
                    BomberExplosionPlayers[targetId] += Time.fixedDeltaTime;
                }
            }
        }

        public override void OnReportDeadBody(PlayerControl _, NetworkedPlayerInfo __)
        {
            BomberExplosionPlayers.Clear();
        }
        public override bool OverrideAbilityButton(out string text)
        {
            text = "Bomber_Ability";
            return true;
        }
        public override string GetAbilityButtonText()
            => GetString("BomberAbilitytext");
        public override void ApplyGameOptions(IGameOptions opt)
        {
            AURoleOptions.PhantomCooldown = BomberExplosion <= 0 ? 200f : Cooldown;
        }
        public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
        {
            seen ??= seer;
            if (seen.PlayerId != seer.PlayerId || isForMeeting || BomberExplosion <= 0 || !Player.IsAlive()) return "";

            if (isForHud) return GetString("PhantomButtonKilltargetLowertext");
            return $"<size=50%>{GetString("PhantomButtonKilltargetLowertext")}</size>";
        }
    }
}
