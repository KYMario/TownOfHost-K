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
using TownOfHost.Roles.Vanilla;

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

        public IGameOptions cachedGameOptions = null;
        public override IGameOptions BasedGameOptions =>
            Main.RealOptionsData.Restore(cachedGameOptions ?? (cachedGameOptions = new NormalGameOptionsV10(new UnityLogger().Cast<ILogger>()).Cast<IGameOptions>()));
        public override bool IsDirty { get; protected set; }

        public PlayerControl player;
        public string OldOptionstext;

        public PlayerGameOptionsSender(PlayerControl player)
        {
            this.player = player;
            this.OldOptionstext = "";
        }
        public void SetDirty() => IsDirty = true;

        public override void SendGameOptions()
        {
            var opt = BuildGameOptions();
            if (player.AmOwner)
            {
                foreach (var com in GameManager.Instance.LogicComponents)
                {
                    if (com.TryCast<LogicOptions>(out var lo))
                        lo.SetGameOptions(opt);
                }
                GameOptionsManager.Instance.CurrentGameOptions = opt;
            }
            else
            {
                var IsAlive = player.IsAlive();
                var killCooldown = IsAlive ? opt.GetFloat(FloatOptionNames.KillCooldown) : 0f;
                var killDistance = IsAlive ? opt.GetInt(Int32OptionNames.KillDistance) : 0;
                var impostorLight = IsAlive ? opt.GetFloat(FloatOptionNames.ImpostorLightMod) : 0f;
                var crewLight = IsAlive ? opt.GetFloat(FloatOptionNames.CrewLightMod) : 0f;
                var playerSpeed = opt.GetFloat(FloatOptionNames.PlayerSpeedMod);
                var numEmergency = IsAlive ? opt.GetInt(Int32OptionNames.NumEmergencyMeetings) : 0;
                var emergencyCooldown = IsAlive ? opt.GetInt(Int32OptionNames.EmergencyCooldown) : 0;
                var discussionTime = opt.GetInt(Int32OptionNames.DiscussionTime);
                var votingTime = opt.GetInt(Int32OptionNames.VotingTime);
                var anonymousVotes = opt.GetBool(BoolOptionNames.AnonymousVotes);
                var numCommonTasks = opt.GetInt(Int32OptionNames.NumCommonTasks);
                var numLongTasks = opt.GetInt(Int32OptionNames.NumLongTasks);
                var numShortTasks = opt.GetInt(Int32OptionNames.NumShortTasks);
                var visualTasks = IsAlive ? opt.GetBool(BoolOptionNames.VisualTasks) : false;
                var taskBarMode = IsAlive ? opt.GetInt(Int32OptionNames.TaskBarMode) : 0;
                var confirmImpostor = opt.GetBool(BoolOptionNames.ConfirmImpostor);
                var engcooldown = IsAlive ? opt.GetFloat(FloatOptionNames.EngineerCooldown) : 0;
                var engmaxtime = IsAlive ? opt.GetFloat(FloatOptionNames.EngineerInVentMaxTime) : 0;
                var scicooldown = IsAlive ? opt.GetFloat(FloatOptionNames.ScientistCooldown) : 0;
                var scibattery = IsAlive ? opt.GetFloat(FloatOptionNames.ScientistBatteryCharge) : 0;
                var trackercool = IsAlive ? opt.GetFloat(FloatOptionNames.TrackerCooldown) : 0;
                var trackerdelay = IsAlive ? opt.GetFloat(FloatOptionNames.TrackerDelay) : 0;
                var tarckduration = IsAlive ? opt.GetFloat(FloatOptionNames.TrackerDuration) : 0;
                var noisealert = opt.GetFloat(FloatOptionNames.NoisemakerAlertDuration);
                var noiseimp = opt.GetBool(BoolOptionNames.NoisemakerImpostorAlert);
                var shapecool = IsAlive ? opt.GetFloat(FloatOptionNames.ShapeshifterCooldown) : 0;
                var ShapeshifterDuration = opt.GetFloat(FloatOptionNames.ShapeshifterDuration);
                var shapeskin = IsAlive ? opt.GetBool(BoolOptionNames.ShapeshifterLeaveSkin) : false;
                var phantom = IsAlive ? opt.GetFloat(FloatOptionNames.PhantomCooldown) : 0;
                var detective = IsAlive ? opt.GetFloat(FloatOptionNames.DetectiveSuspectLimit) : 0;
                var guardancool = opt.GetFloat(FloatOptionNames.GuardianAngelCooldown);
                var vip = opt.GetFloat(FloatOptionNames.ViperDissolveTime);

                string NowOption = $"{killCooldown},{killDistance},{impostorLight},{crewLight},{playerSpeed},{numEmergency},{emergencyCooldown},{discussionTime},{votingTime},{anonymousVotes},{numCommonTasks},{numLongTasks},{numShortTasks},{visualTasks},{taskBarMode},{confirmImpostor}";
                NowOption += $"{engcooldown},{engmaxtime},{scicooldown},{scibattery},{trackercool},{trackerdelay},{tarckduration},{noisealert},{noiseimp},{shapecool},{ShapeshifterDuration},{shapeskin},{phantom},{detective},{vip},{guardancool}";
                if (OldOptionstext == NowOption)
                {
                    return;
                }
                OldOptionstext = GameStates.CalledMeeting ? "" : NowOption;
                base.SendGameOptions();
            }
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
                if (GameOptionsManager.Instance.CurrentGameOptions == null)
                {
                    Logger.Error($"CurrentGameOptions縺系ull縺", "Pl.BuildGameOptions");
                    return BasedGameOptions;
                }
                Main.RealOptionsData = new OptionBackupData(GameOptionsManager.Instance.CurrentGameOptions);
            }

            var opt = BasedGameOptions;
            if (BasedGameOptions == null)
            {
                Logger.Error($"BasedGameOptions縺系ull縺", "Pl.BuildGameOptions");
                return opt;
            }

            AURoleOptions.SetOpt(opt);

            AURoleOptions.ShapeshifterLeaveSkin = false;
            AURoleOptions.NoisemakerImpostorAlert = true;
            AURoleOptions.NoisemakerAlertDuration = Noisemaker.NoisemakerAlertDuration.GetFloat();
            AURoleOptions.ViperDissolveTime = Viper.ViperDissolveTime;

            if (player == null)
            {
                Logger.Error($"player縺系ull縺", "Pl.BuildGameOptions");
                return opt;
            }
            var state = PlayerState.GetByPlayerId(player.PlayerId);
            if (state == null)
            {
                Logger.Error($"state縺系ull繧・〒", "Pl.BuildGameOptions");
                return opt;
            }
            if (CurrentGameMode == CustomGameMode.TaskBattle)
            {
                opt.SetFloat(FloatOptionNames.CrewLightMod, 5f);
                opt.SetFloat(FloatOptionNames.EngineerCooldown, TaskBattle.TaskBattleVentCooldown.GetFloat());
                AURoleOptions.EngineerInVentMaxTime = 0;
                return opt;
            }

            CustomRoles role = player.GetCustomRole();
            var HasRoleAddon = RoleAddAddons.GetRoleAddon(role, out var data, player, subrole: [CustomRoles.Lighting, CustomRoles.Moon, CustomRoles.Sunglasses, CustomRoles.Watching, CustomRoles.Speeding]);

            if (player.IsAlive())
            {
                opt.SetFloat(FloatOptionNames.EngineerCooldown, DefaultEngineerCooldown.GetFloat());
                opt.SetFloat(FloatOptionNames.EngineerInVentMaxTime, DefaultEngineerInVentMaxTime.GetFloat());
                opt.SetInt(Int32OptionNames.NumEmergencyMeetings, (int)state.NumberOfRemainingButtons);

                var HasLithing = player.Is(CustomRoles.Lighting);
                var HasMoon = player.Is(CustomRoles.Moon);
                var HasSunglasses = player.Is(CustomRoles.Sunglasses);
                opt.SetFloat(FloatOptionNames.ImpostorLightMod, Main.DefaultImpostorVision);
                opt.SetFloat(FloatOptionNames.CrewLightMod, Main.DefaultCrewmateVision);

                switch (role.GetCustomRoleTypes())
                {
                    case CustomRoleTypes.Impostor:
                        AURoleOptions.ShapeshifterCooldown = DefaultShapeshiftCooldown.GetFloat();
                        AURoleOptions.ShapeshifterDuration = DefaultShapeshiftDuration.GetFloat();
                        break;
                    case CustomRoleTypes.Madmate:
                        AURoleOptions.EngineerCooldown = MadmateVentCooldown.GetFloat();
                        AURoleOptions.EngineerInVentMaxTime = MadmateVentMaxTime.GetFloat();
                        HasLithing |= MadmateHasLighting.GetBool();
                        HasMoon |= MadmateHasMoon.GetBool();
                        if (MadmateCanSeeOtherVotes.GetBool())
                            opt.SetBool(BoolOptionNames.AnonymousVotes, false);
                        break;
                }
                if (role is CustomRoles.Egoist)
                {
                    AURoleOptions.ShapeshifterCooldown = DefaultShapeshiftCooldown.GetFloat();
                    AURoleOptions.ShapeshifterDuration = DefaultShapeshiftDuration.GetFloat();
                }
                var roleClass = player.GetRoleClass();
                if (roleClass == null)
                {
                    Logger.Error($"roleClass縺系ull縺", "Pl.BuildGameOptions");
                    //    return opt;
                }

                if (player.Is(CustomRoles.MagicHand))
                    opt.SetInt(Int32OptionNames.KillDistance, MagicHand.KillDistance.GetInt());

                if (OverrideKilldistance.AllData.TryGetValue(role, out var killdistance))
                    opt.SetInt(Int32OptionNames.KillDistance, killdistance.Killdistance.GetInt());

                if (Amnesia.CheckAbility(player))
                    roleClass?.ApplyGameOptions(opt);

                foreach (var subRole in player.GetCustomSubRoles())
                {
                    switch (subRole)
                    {
                        case CustomRoles.LastImpostor:
                            if (LastImpostor.GiveWatching.GetBool()) opt.SetBool(BoolOptionNames.AnonymousVotes, false);
                            if (OverrideKilldistance.AllData.TryGetValue(CustomRoles.LastImpostor, out var kd))
                                opt.SetInt(Int32OptionNames.KillDistance, kd.Killdistance.GetInt());
                            break;
                        case CustomRoles.LastNeutral:
                            if (LastNeutral.GiveWatching.GetBool()) opt.SetBool(BoolOptionNames.AnonymousVotes, false);
                            if ((roleClass is ILNKiller || LastNeutral.ChKilldis.GetBool()) && OverrideKilldistance.AllData.TryGetValue(CustomRoles.LastNeutral, out var killd))
                                opt.SetInt(Int32OptionNames.KillDistance, killd.Killdistance.GetInt());

                            break;
                        case CustomRoles.Watching:
                            opt.SetBool(BoolOptionNames.AnonymousVotes, false);
                            break;
                    }
                }

                if (HasRoleAddon)
                {
                    //Wac
                    if (data.GiveWatching.GetBool()) opt.SetBool(BoolOptionNames.AnonymousVotes, false);
                    if (!data.IsImpostor)
                    {
                        HasLithing |= data.GiveLighting.GetBool();
                        HasMoon |= data.GiveMoon.GetBool();
                    }
                }

                var isElectrical = Utils.IsActive(SystemTypes.Electrical);

                //Moon
                if (HasMoon)
                    if (isElectrical)
                    {
                        opt.SetFloat(FloatOptionNames.CrewLightMod, Main.DefaultCrewmateVision * AURoleOptions.ElectricalCrewVision);
                        opt.SetFloat(FloatOptionNames.ImpostorLightMod, Main.DefaultImpostorVision);
                    }

                //Lighting
                if (HasLithing)
                {
                    if (isElectrical && HasMoon)
                    {
                        opt.SetFloat(FloatOptionNames.CrewLightMod, Main.DefaultImpostorVision * (role.GetRoleTypes().IsCrewmate() ? AURoleOptions.ElectricalCrewVision : 5f));
                        opt.SetFloat(FloatOptionNames.ImpostorLightMod, Main.DefaultImpostorVision);
                    }
                    else
                        if (isElectrical)
                        {
                            opt.SetFloat(FloatOptionNames.CrewLightMod, Main.DefaultCrewmateVision);
                            opt.SetFloat(FloatOptionNames.ImpostorLightMod, Main.DefaultCrewmateVision);
                        }
                        else
                        {
                            opt.SetFloat(FloatOptionNames.CrewLightMod, Main.DefaultImpostorVision);
                            opt.SetFloat(FloatOptionNames.ImpostorLightMod, Main.DefaultImpostorVision);
                        }
                }

                if (Main.AllPlayerKillCooldown.TryGetValue(player.PlayerId, out var killCooldown))
                {
                    AURoleOptions.KillCooldown = Mathf.Max(((roleClass as IKiller)?.CanUseKillButton() == true) ? 0.00000000000000000000000000000000000000000001f : 0f, killCooldown);
                }

                state.taskState.hasTasks = UtilsTask.HasTasks(player.Data, false);

                if (AdditionalEmergencyCooldown.GetBool() && AdditionalEmergencyCooldownThreshold.GetInt() <= PlayerCatch.AllAlivePlayersCount)
                {
                    opt.SetInt(Int32OptionNames.EmergencyCooldown, AdditionalEmergencyCooldownTime.GetInt());
                }
                if (SyncButtonMode.GetBool() && SyncedButtonCount.GetValue() <= UsedButtonCount)
                {
                    opt.SetInt(Int32OptionNames.EmergencyCooldown, 3600);
                    opt.SetInt(Int32OptionNames.NumEmergencyMeetings, 0);
                }

                if (CurrentGameMode is CustomGameMode.MurderMystery && MurderMystery.sabotage)
                {
                    opt.SetFloat(FloatOptionNames.ImpostorLightMod, role.IsImpostor() ? MurderMystery.ImpostorVision : MurderMystery.Crewvision);
                    opt.SetFloat(FloatOptionNames.CrewLightMod, role.IsImpostor() ? MurderMystery.ImpostorVision : MurderMystery.Crewvision);
                }
                if (data.GiveSunglasses.GetBool())
                {
                    opt.SetFloat(FloatOptionNames.ImpostorLightMod, opt.GetFloat(FloatOptionNames.ImpostorLightMod) * data.SunglassesVisionmagnification.GetFloat() * 0.01f);
                    opt.SetFloat(FloatOptionNames.CrewLightMod, opt.GetFloat(FloatOptionNames.CrewLightMod) * data.SunglassesVisionmagnification.GetFloat() * 0.01f);
                }
                else if (HasSunglasses)
                {
                    opt.SetFloat(FloatOptionNames.ImpostorLightMod, opt.GetFloat(FloatOptionNames.ImpostorLightMod) * Sunglasses.SunglassesVisionmagnification.GetFloat() * 0.01f);
                    opt.SetFloat(FloatOptionNames.CrewLightMod, opt.GetFloat(FloatOptionNames.CrewLightMod) * Sunglasses.SunglassesVisionmagnification.GetFloat() * 0.01f);
                }
                AURoleOptions.EngineerCooldown = Mathf.Max(1.2f, AURoleOptions.EngineerCooldown);
                opt.BlackOut(state.IsBlackOut);
            }
            else
            {
                bool HaveWatching = player.Is(CustomRoles.Watching);

                if (HasRoleAddon && data.GiveWatching.GetBool()) HaveWatching = true;
                if ((GhostCanSeeOtherVotes.GetBool() || !GhostOptions.GetBool()) && !player.Is(CustomRoles.AsistingAngel) && (!player.IsGhostRole() || GhostRoleCanSeeOtherVotes.GetBool()))
                    HaveWatching |= true;
                if (HaveWatching is false)
                {
                    foreach (var subrole in player.GetCustomSubRoles())
                    {
                        switch (subrole)
                        {
                            case CustomRoles.LastImpostor: HaveWatching |= LastImpostor.GiveWatching.GetBool(); break;
                            case CustomRoles.LastNeutral: HaveWatching |= LastImpostor.GiveWatching.GetBool(); break;
                        }
                    }
                    if (role.IsMadmate() && MadmateCanSeeOtherVotes.GetBool()) HaveWatching = true;
                }

                if (HaveWatching) opt.SetBool(BoolOptionNames.AnonymousVotes, false);

                if (player.IsGhostRole())
                {
                    var gr = PlayerState.GetByPlayerId(player.PlayerId).GhostRole;
                    switch (gr)
                    {
                        case CustomRoles.Ghostbuttoner: AURoleOptions.GuardianAngelCooldown = CoolDown(Ghostbuttoner.CoolDown.GetFloat()); break;
                        case CustomRoles.GhostNoiseSender: AURoleOptions.GuardianAngelCooldown = CoolDown(GhostNoiseSender.CoolDown.GetFloat()); break;
                        case CustomRoles.GhostReseter: AURoleOptions.GuardianAngelCooldown = CoolDown(GhostReseter.CoolDown.GetFloat()); break;
                        case CustomRoles.GhostRumour: AURoleOptions.GuardianAngelCooldown = CoolDown(GhostRumour.CoolDown.GetFloat()); break;
                        case CustomRoles.GuardianAngel: AURoleOptions.GuardianAngelCooldown = CoolDown(GuardianAngel.CoolDown.GetFloat()); break;
                        case CustomRoles.DemonicTracker: AURoleOptions.GuardianAngelCooldown = CoolDown(DemonicTracker.CoolDown.GetFloat()); break;
                        case CustomRoles.DemonicCrusher: AURoleOptions.GuardianAngelCooldown = CoolDown(DemonicCrusher.CoolDown.GetFloat()); break;
                        case CustomRoles.DemonicVenter: AURoleOptions.GuardianAngelCooldown = CoolDown(DemonicVenter.CoolDown.GetFloat()); break;
                        case CustomRoles.AsistingAngel: AURoleOptions.GuardianAngelCooldown = CoolDown(AsistingAngel.GetNowCoolDown()); break;
                    }
                }
            }

            if (Main.AllPlayerSpeed.TryGetValue(player.PlayerId, out var speed))
            {
                if (0.25f <= speed)
                {
                    var addspeed = player.Is(CustomRoles.Speeding) ? Speeding.Speed : 0;
                    if (HasRoleAddon) addspeed = data.GiveSpeeding.GetBool() ? data.Speed.GetFloat() : addspeed;
                    speed += addspeed;
                }
                if (state.CanMove is false) speed = Main.MinSpeed;
                var minSpeed = TownOfHost.Roles.Impostor.Reverser.AllowNegativeSpeed ? -10f : Main.MinSpeed;
                AURoleOptions.PlayerSpeedMod = Mathf.Clamp(speed, minSpeed, 10f);
            }

            if ((CurrentGameMode == CustomGameMode.HideAndSeek || IsStandardHAS) && HideAndSeekKillDelayTimer > 0)
            {
                opt.SetFloat(FloatOptionNames.ImpostorLightMod, 0f);
                if (player.Is(CountTypes.Impostor))
                {
                    AURoleOptions.PlayerSpeedMod = Main.MinSpeed;
                }
            }

            MeetingTimeManager.ApplyGameOptions(opt);

            AURoleOptions.ShapeshifterCooldown = Mathf.Max(1f, AURoleOptions.ShapeshifterCooldown);
            AURoleOptions.PhantomCooldown = Mathf.Max(1f, AURoleOptions.PhantomCooldown);
            AURoleOptions.ProtectionDurationSeconds = 0f;
            AURoleOptions.ImpostorsCanSeeProtect = false;

            return opt;

            float CoolDown(float cool) => Mathf.Max(1f, cool);
        }

        public override bool AmValid()
        {
            try
            {
                return base.AmValid() && player is not null && !SelectRolesPatch.Disconnected.Contains(player.PlayerId) && Main.RealOptionsData != null;
            }
            catch
            {
                Logger.Error($"{player?.Data?.GetLogPlayerName() ?? "???"} - Error", "PlayerGameOptionsSender.AmValid");
                return false;
            }
        }
    }
}