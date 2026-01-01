using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor
{
    public sealed class Vampire : RoleBase, IImpostor
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Vampire),
                player => new Vampire(player),
                CustomRoles.Vampire,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Impostor,
                4800,
                SetupOptionItem,
                "va",
                OptionSort: (4, 2),
                introSound: () => GetIntroSound(RoleTypes.Shapeshifter),
                from: From.TheOtherRoles
            );
        public Vampire(PlayerControl player)
        : base(
            RoleInfo,
            player
        )
        {
            KillDelay = OptionKillDelay.GetFloat();

            BittenPlayers.Clear();
            Spped = SpeedDownCount.GetFloat();
            tmpSpeed = Main.NormalOptions.PlayerSpeedMod;
        }
        static OptionItem OptionKillCool;
        static OptionItem OptionKillDelay;
        static OptionItem SpeedDown;
        static OptionItem SpeedDownCount;
        enum OptionName
        {
            VampireKillDelay, VampireSpeedDown, VampireSpeedDownCount
        }

        static float KillDelay;
        static float Spped;
        static float tmpSpeed;
        public bool CanBeLastImpostor { get; } = false;
        Dictionary<byte, float> BittenPlayers = new(14);

        private static void SetupOptionItem()
        {
            OptionKillCool = FloatOptionItem.Create(RoleInfo, 9, GeneralOption.KillCooldown, new(0f, 180f, 0.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionKillDelay = FloatOptionItem.Create(RoleInfo, 10, OptionName.VampireKillDelay, new(1f, 1000f, 0.1f), 10f, false)
                .SetValueFormat(OptionFormat.Seconds);
            SpeedDown = BooleanOptionItem.Create(RoleInfo, 11, OptionName.VampireSpeedDown, true, false);
            SpeedDownCount = FloatOptionItem.Create(RoleInfo, 12, OptionName.VampireSpeedDownCount, new(0f, 1000f, 1f), 10f, false, SpeedDown)
            .SetValueFormat(OptionFormat.Seconds);
        }

        public float CalculateKillCooldown() => OptionKillCool.GetFloat();
        public void OnCheckMurderAsKiller(MurderInfo info)
        {
            if (!info.CanKill) return; //キル出来ない相手には無効
            var (killer, target) = info.AttemptTuple;

            if (target.Is(CustomRoles.Bait)) return;
            if (target.Is(CustomRoles.InSender)) return;
            if (info.IsFakeSuicide) return;
            if (info.CheckHasGuard())
            {
                info.IsGuard = true;
                return;
            }

            //誰かに噛まれていなければ登録
            if (!BittenPlayers.ContainsKey(target.PlayerId))
            {
                killer.SetKillCooldown();
                BittenPlayers.Add(target.PlayerId, 0f);
            }
            info.DoKill = false;
        }
        public override void OnFixedUpdate(PlayerControl _)
        {
            if (!AmongUsClient.Instance.AmHost || !GameStates.IsInTask) return;

            foreach (var (targetId, timer) in BittenPlayers.ToArray())
            {
                if (timer >= KillDelay)
                {
                    var target = PlayerCatch.GetPlayerById(targetId);
                    KillBitten(target);
                    BittenPlayers.Remove(targetId);
                }
                else
                {
                    BittenPlayers[targetId] += Time.fixedDeltaTime;

                    if (SpeedDown.GetBool() && timer >= Spped)
                    {
                        var target = PlayerCatch.GetPlayerById(targetId);
                        if (target.IsAlive())
                        {
                            var x = KillDelay - Spped;
                            float Swariai = (KillDelay - Spped - (timer - Spped)) / x;
                            float Sp = tmpSpeed * Swariai;

                            if (KillDelay - timer <= 0.5f) Sp = Main.MinSpeed;//これは残り0,5sになったら静止させてｳｸﾞｯ...ｺｺﾏﾃﾞｶｯ...ってするやつ。

                            if (Sp >= Main.MinSpeed && Sp < tmpSpeed)
                            {
                                Main.AllPlayerSpeed[target.PlayerId] = Sp;
                                target.MarkDirtySettings();
                            }
                        }
                    }
                }
            }
        }
        public override void OnReportDeadBody(PlayerControl repo, NetworkedPlayerInfo __)
        {
            if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;
            foreach (var targetId in BittenPlayers.Keys)
            {
                var target = PlayerCatch.GetPlayerById(targetId);
                KillBitten(target, true);
            }
            BittenPlayers.Clear();
        }
        public bool OverrideKillButtonText(out string text)
        {
            text = GetString("VampireBiteButtonText");
            return true;
        }
        public bool OverrideKillButton(out string text)
        {
            text = "Vampire_Kill";
            return true;
        }

        private void KillBitten(PlayerControl target, bool isButton = false)
        {
            if (target == null) return;
            var vampire = Player;

            _ = new LateTask(() =>
            {
                Main.AllPlayerSpeed[target.PlayerId] = tmpSpeed;
                _ = new LateTask(() => target.MarkDirtySettings(), 0.9f, "Do-ki");
            }, 0.4f, "Modosu");

            if (target.IsAlive())
            {
                if (CustomRoleManager.OnCheckMurder(vampire, target, target, target, true, Killpower: 1, deathReason: CustomDeathReason.Bite))
                {
                    target.SetRealKiller(vampire);
                    Logger.Info($"Vampireに噛まれている{target.name}を自爆させました。", "Vampire");
                    if (!isButton && vampire.IsAlive())
                        RPC.PlaySoundRPC(vampire.PlayerId, Sounds.KillSound);
                }
                else Logger.Info($"Vampireに噛まれた{target.name}にキルが通りませんでした。", "Vampire");
            }
            else
            {
                Logger.Info($"Vampireに噛まれている{target.name}はすでに死んでいました。", "Vampire.KillBitten");
            }
        }
    }
}
