using System.Linq;
using AmongUs.GameOptions;
using Hazel;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Linq;
using InnerNet;
using Mathf = UnityEngine.Mathf;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.AddOns.Impostor;
using TownOfHost.Roles.AddOns.Neutral;
using TownOfHost.Roles.Ghost;
using static TownOfHost.Options;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.AddOns.Common;

namespace TownOfHost.Modules
{
    public class PlayerGameOptionsSender : GameOptionsSender
    {
        public static void SetDirty(PlayerControl player) => SetDirty(player.PlayerId);
        public static void SetDirty(byte playerId) =>
            AllSenders.OfType<PlayerGameOptionsSender>()
            .Where(sender => sender.player.PlayerId == playerId)
            .ToList().ForEach(sender => sender.SetDirty());
        public static void SetDirtyToAll() =>
            AllSenders.OfType<PlayerGameOptionsSender>()
            .ToList().ForEach(sender => sender.SetDirty());

        public override IGameOptions BasedGameOptions =>
            Main.RealOptionsData.Restore(new NormalGameOptionsV08(new UnityLogger().Cast<ILogger>()).Cast<IGameOptions>());
        public override bool IsDirty { get; protected set; }

        public PlayerControl player;

        public PlayerGameOptionsSender(PlayerControl player)
        {
            this.player = player;
        }
        public void SetDirty() => IsDirty = true;

        public override void SendGameOptions()
        {
            if (player.AmOwner)
            {
                var opt = BuildGameOptions();
                foreach (var com in GameManager.Instance.LogicComponents)
                {
                    if (com.TryCast<LogicOptions>(out var lo))
                        lo.SetGameOptions(opt);
                }
                GameOptionsManager.Instance.CurrentGameOptions = opt;
            }
            else base.SendGameOptions();
        }

        public override void SendOptionsArray(Il2CppStructArray<byte> optionArray)
        {
            for (byte i = 0; i < GameManager.Instance.LogicComponents.Count; i++)
            {
                if (GameManager.Instance.LogicComponents[i].TryCast<LogicOptions>(out _))
                {
                    SendOptionsArray(optionArray, i, player.GetClientId());
                }
            }
        }
        public static void RemoveSender(PlayerControl player)
        {
            var sender = AllSenders.OfType<PlayerGameOptionsSender>()
            .FirstOrDefault(sender => sender.player.PlayerId == player.PlayerId);
            if (sender == null) return;
            sender.player = null;
            AllSenders.Remove(sender);
        }
        public override IGameOptions BuildGameOptions()
        {
            if (Main.RealOptionsData == null)
            {
                Main.RealOptionsData = new OptionBackupData(GameOptionsManager.Instance.CurrentGameOptions);
            }

            var opt = BasedGameOptions;

            AURoleOptions.ShapeshifterLeaveSkin = false;//スキンはデフォではOFFにする

            AURoleOptions.SetOpt(opt);

            AURoleOptions.NoisemakerImpostorAlert = true;
            AURoleOptions.NoisemakerAlertDuration = GhostNoiseSender.NoisTime.GetFloat();

            var state = PlayerState.GetByPlayerId(player.PlayerId);
            opt.BlackOut(state.IsBlackOut);

            var HasLithing = player.Is(CustomRoles.Lighting);
            var HasMoon = player.Is(CustomRoles.Moon);

            CustomRoles role = player.GetCustomRole();
            switch (role.GetCustomRoleTypes())
            {
                case CustomRoleTypes.Impostor: //アプデ対応用  [いらないかも]
                    AURoleOptions.ShapeshifterCooldown = Options.DefaultShapeshiftCooldown.GetFloat();
                    break;
                case CustomRoleTypes.Madmate:
                    AURoleOptions.EngineerCooldown = Options.MadmateVentCooldown.GetFloat();
                    AURoleOptions.EngineerInVentMaxTime = Options.MadmateVentMaxTime.GetFloat();
                    HasLithing |= MadmateHasLighting.GetBool();
                    HasMoon |= MadmateHasMoon.GetBool();
                    if (Options.MadmateCanSeeOtherVotes.GetBool())
                        opt.SetBool(BoolOptionNames.AnonymousVotes, false);
                    break;
            }

            var roleClass = player.GetRoleClass();
            if (Roles.AddOns.Common.Amnesia.CheckAbility(player))
                roleClass?.ApplyGameOptions(opt);

            foreach (var subRole in player.GetCustomSubRoles())
            {
                switch (subRole)
                {
                    case CustomRoles.LastImpostor:
                        if (LastImpostor.GiveWatching.GetBool()) opt.SetBool(BoolOptionNames.AnonymousVotes, false);
                        break;
                    case CustomRoles.LastNeutral:
                        if (LastNeutral.GiveWatching.GetBool()) opt.SetBool(BoolOptionNames.AnonymousVotes, false);
                        break;
                    case CustomRoles.watching:
                        opt.SetBool(BoolOptionNames.AnonymousVotes, false);
                        break;
                }
            }

            //書く役職の処
            if (RoleAddAddons.AllData.TryGetValue(role, out var data) && data.GiveAddons.GetBool())
            {
                //Wac
                if (data.GiveWatching.GetBool()) opt.SetBool(BoolOptionNames.AnonymousVotes, false);
                HasLithing |= data.GiveLighting.GetBool();
                HasMoon |= data.GiveMoon.GetBool();
            }

            //Moon
            if (HasMoon)
                if (Utils.IsActive(SystemTypes.Electrical)) { opt.SetFloat(FloatOptionNames.CrewLightMod, Main.DefaultCrewmateVision * 5f); }

            //Lighting
            if (HasLithing)
            {
                if (Utils.IsActive(SystemTypes.Electrical) && HasMoon) { opt.SetFloat(FloatOptionNames.CrewLightMod, Main.DefaultImpostorVision * 5f); }
                else//停電時はクルー視界
                if (Utils.IsActive(SystemTypes.Electrical)) { opt.SetFloat(FloatOptionNames.CrewLightMod, Main.DefaultCrewmateVision); }
                else opt.SetFloat(FloatOptionNames.CrewLightMod, Main.DefaultImpostorVision);
            }

            //キルクール0に設定+修正する設定をONにしたと気だけ呼び出す。
            if (Options.FixZeroKillCooldown.GetBool() && AURoleOptions.KillCooldown == 0 && Main.AllPlayerKillCooldown.TryGetValue(player.PlayerId, out var ZerokillCooldown))
            {//0に限りなく近い小数にしてキルできない状態回避する
                AURoleOptions.KillCooldown = Mathf.Max(0.00000000000000000000000000000000000000000001f, ZerokillCooldown);
            }
            else
            if (Main.AllPlayerKillCooldown.TryGetValue(player.PlayerId, out var killCooldown))
            {
                AURoleOptions.KillCooldown = Mathf.Max(0f, killCooldown);
            }
            if (Main.AllPlayerSpeed.TryGetValue(player.PlayerId, out var speed))
            {
                AURoleOptions.PlayerSpeedMod = Mathf.Clamp(speed, Main.MinSpeed, 10f);
            }

            state.taskState.hasTasks = Utils.HasTasks(player.Data, false);
            if (Options.GhostCanSeeOtherVotes.GetBool() && player.Data.IsDead && !player.Is(CustomRoles.AsistingAngel) && (!player.IsGorstRole() || GRCanSeeOtherVotes.GetBool()))
                opt.SetBool(BoolOptionNames.AnonymousVotes, false);
            if (Options.AdditionalEmergencyCooldown.GetBool() &&
                Options.AdditionalEmergencyCooldownThreshold.GetInt() <= Utils.AllAlivePlayersCount)
            {
                opt.SetInt(
                    Int32OptionNames.EmergencyCooldown,
                    Options.AdditionalEmergencyCooldownTime.GetInt());
            }
            if (Options.SyncButtonMode.GetBool() && Options.SyncedButtonCount.GetValue() <= Options.UsedButtonCount)
            {
                opt.SetInt(Int32OptionNames.EmergencyCooldown, 3600);
            }
            if ((Options.CurrentGameMode == CustomGameMode.HideAndSeek || Options.IsStandardHAS) && Options.HideAndSeekKillDelayTimer > 0)
            {
                if (!Main.HnSFlag)
                {
                    opt.SetFloat(FloatOptionNames.ImpostorLightMod, 0f);
                    if (player.Is(CountTypes.Impostor))
                    {
                        AURoleOptions.PlayerSpeedMod = Main.MinSpeed;
                    }
                }
            }
            if (Options.CurrentGameMode == CustomGameMode.TaskBattle && Options.TaskBattleCanVent.GetBool())
            {
                opt.SetFloat(FloatOptionNames.EngineerCooldown, Options.TaskBattleVentCooldown.GetFloat());
                AURoleOptions.EngineerInVentMaxTime = 0;
            }
            MeetingTimeManager.ApplyGameOptions(opt);

            AURoleOptions.ShapeshifterCooldown = Mathf.Max(1f, AURoleOptions.ShapeshifterCooldown);
            AURoleOptions.ProtectionDurationSeconds = 0f;
            AURoleOptions.ImpostorsCanSeeProtect = false;

            //キルレンジ
            if (OverrideKilldistance.AllData.TryGetValue(role, out var killdistance))
                opt.SetInt(Int32OptionNames.KillDistance, killdistance.Killdistance.GetInt());

            if (player.Is(CustomRoles.LastImpostor) && OverrideKilldistance.AllData.TryGetValue(CustomRoles.LastImpostor, out var kd))
                opt.SetInt(Int32OptionNames.KillDistance, kd.Killdistance.GetInt());

            if (player.Is(CustomRoles.LastNeutral))
                if ((player.GetRoleClass() is ILNKiller || LastNeutral.ChKilldis.GetBool()) && OverrideKilldistance.AllData.TryGetValue(CustomRoles.LastNeutral, out var killd))
                    opt.SetInt(Int32OptionNames.KillDistance, killd.Killdistance.GetInt());

            if (state != null)
            {
                opt.SetInt(Int32OptionNames.NumEmergencyMeetings, (int)state.NumberOfRemainingButtons);
            }
            //幽霊役職用の奴
            if (player.IsGorstRole())
            {
                var gr = PlayerState.GetByPlayerId(player.PlayerId).GhostRole;

                switch (gr)
                {
                    case CustomRoles.Ghostbuttoner: AURoleOptions.GuardianAngelCooldown = CoolDown(Ghostbuttoner.CoolDown.GetFloat()); break;
                    case CustomRoles.GhostNoiseSender: AURoleOptions.GuardianAngelCooldown = CoolDown(GhostNoiseSender.CoolDown.GetFloat()); break;
                    case CustomRoles.GhostReseter: AURoleOptions.GuardianAngelCooldown = CoolDown(GhostReseter.CoolDown.GetFloat()); break;
                    case CustomRoles.DemonicTracker: AURoleOptions.GuardianAngelCooldown = CoolDown(DemonicTracker.CoolDown.GetFloat()); break;
                    case CustomRoles.DemonicCrusher: AURoleOptions.GuardianAngelCooldown = CoolDown(DemonicCrusher.CoolDown.GetFloat()); break;
                    case CustomRoles.DemonicVenter: AURoleOptions.GuardianAngelCooldown = CoolDown(DemonicVenter.CoolDown.GetFloat()); break;
                    case CustomRoles.AsistingAngel: AURoleOptions.GuardianAngelCooldown = CoolDown(AsistingAngel.CoolDown.GetFloat()); break;
                }
            }
            return opt;

            float CoolDown(float cool) => cool == 0f ? 0.1f : cool;
        }

        public override bool AmValid()
        {
            //キルクとか反映されないから～
            return base.AmValid() && player != null && (!player.Data.Disconnected || !SelectRolesPatch.Disconnected.Contains(player.PlayerId)) && Main.RealOptionsData != null;
        }
    }
}