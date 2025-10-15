using System.Collections.Generic;
using System.Linq;
using Hazel;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.Impostor;
using TownOfHost.Roles.Neutral;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Crewmate;

public sealed class SwitchSheriff : RoleBase, IKiller, ISchrodingerCatOwner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(SwitchSheriff),
            player => new SwitchSheriff(player),
            CustomRoles.SwitchSheriff,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            8900,
            SetupOptionItem,
            "swsh",
            "#f8cd46",
            (2, 1),
            true,
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );

    public SwitchSheriff(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.True
    )
    {
        ShotLimit = ShotLimitOpt.GetInt();
        CurrentKillCooldown = KillCooldown.GetFloat();
        Taskmode = true;
        nowcool = CurrentKillCooldown;
        LastCooltime = 0;
    }

    public static OptionItem KillCooldown;
    private static OptionItem MisfireKillsTarget;
    public static OptionItem ShotLimitOpt;
    private static OptionItem CanKillAllAlive;
    public static OptionItem CanKillNeutrals;
    public static OptionItem CanKillLovers;
    /// <summary>
    /// 0そのまま1シェリフ2タスク
    /// </summary>
    public static OptionItem CommsMode;
    public bool Taskmode;
    float nowcool;
    int LastCooltime;
    enum OptionName
    {
        SheriffMisfireKillsTarget,
        SheriffShotLimit,
        SheriffCanKillAllAlive,
        SheriffCanKillNeutrals,
        SheriffCanKill,
        SwitchSheriffCommsmode,
        SheriffCanKillLovers
    }
    public static Dictionary<CustomRoles, OptionItem> KillTargetOptions = new();
    public static Dictionary<ISchrodingerCatOwner.TeamType, OptionItem> SchrodingerCatKillTargetOptions = new();
    public int ShotLimit = 0;
    public float CurrentKillCooldown = 30;
    public static readonly string[] KillOption =
    {
        "SheriffCanKillAll", "SheriffCanKillSeparately"
    };
    public static readonly string[] Mode =
    {
        "NoProcessing","SheriffMode", "TaskMode"
    };

    public ISchrodingerCatOwner.TeamType SchrodingerCatChangeTo => ISchrodingerCatOwner.TeamType.Crew;

    private static void SetupOptionItem()
    {
        KillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 990f, 1f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OverrideKilldistance.Create(RoleInfo, 8);
        MisfireKillsTarget = BooleanOptionItem.Create(RoleInfo, 11, OptionName.SheriffMisfireKillsTarget, false, false);
        ShotLimitOpt = IntegerOptionItem.Create(RoleInfo, 12, OptionName.SheriffShotLimit, new(1, 15, 1), 15, false)
            .SetValueFormat(OptionFormat.Times);
        CommsMode = StringOptionItem.Create(RoleInfo, 21, OptionName.SwitchSheriffCommsmode, Mode, 0, false);
        CanKillAllAlive = BooleanOptionItem.Create(RoleInfo, 15, OptionName.SheriffCanKillAllAlive, true, false);
        SetUpKillTargetOption(CustomRoles.Madmate, 13);
        CanKillNeutrals = StringOptionItem.Create(RoleInfo, 14, OptionName.SheriffCanKillNeutrals, KillOption, 0, false);
        SetUpNeutralOptions(30);
        CanKillLovers = BooleanOptionItem.Create(RoleInfo, 22, OptionName.SheriffCanKillLovers, true, false);
        OverrideTasksData.Create(RoleInfo, 16);
    }
    public static void SetUpNeutralOptions(int idOffset)
    {
        foreach (var neutral in CustomRolesHelper.AllStandardRoles.Where(x => x.IsNeutral()).ToArray())
        {
            if (Event.CheckRole(neutral) is false) continue;
            if (neutral is CustomRoles.SchrodingerCat) continue;
            SetUpKillTargetOption(neutral, idOffset, true, CanKillNeutrals);
            idOffset++;
        }
        foreach (var catType in EnumHelper.GetAllValues<ISchrodingerCatOwner.TeamType>())
        {
            if ((byte)catType < 50)
            {
                continue;
            }
            SetUpSchrodingerCatKillTargetOption(catType, idOffset, true, CanKillNeutrals);
            idOffset++;
        }
    }
    public static void SetUpKillTargetOption(CustomRoles role, int idOffset, bool defaultValue = true, OptionItem parent = null)
    {
        var id = RoleInfo.ConfigId + idOffset;
        if (parent == null) parent = RoleInfo.RoleOption;
        var roleName = UtilsRoleText.GetRoleName(role);
        Dictionary<string, string> replacementDic = new() { { "%role%", Utils.ColorString(UtilsRoleText.GetRoleColor(role), roleName) } };
        KillTargetOptions[role] = BooleanOptionItem.Create(id, OptionName.SheriffCanKill + "%role%", defaultValue, RoleInfo.Tab, false).SetParent(parent).SetParentRole(CustomRoles.SwitchSheriff);
        KillTargetOptions[role].ReplacementDictionary = replacementDic;
    }
    public static void SetUpSchrodingerCatKillTargetOption(ISchrodingerCatOwner.TeamType catType, int idOffset, bool defaultValue = true, OptionItem parent = null)
    {
        var id = RoleInfo.ConfigId + idOffset;
        parent ??= RoleInfo.RoleOption;
        // (%team%陣営)
        var inTeam = GetString("In%team%", new Dictionary<string, string>() { ["%team%"] = GetRoleString(catType.ToString()) });
        // シュレディンガーの猫(%team%陣営)
        var catInTeam = Utils.ColorString(SchrodingerCat.GetCatColor(catType), UtilsRoleText.GetRoleName(CustomRoles.SchrodingerCat) + inTeam);
        Dictionary<string, string> replacementDic = new() { ["%role%"] = catInTeam };
        SchrodingerCatKillTargetOptions[catType] = BooleanOptionItem.Create(id, OptionName.SheriffCanKill + "%role%", defaultValue, RoleInfo.Tab, false).SetParent(parent).SetParentRole(CustomRoles.SwitchSheriff);
        SchrodingerCatKillTargetOptions[catType].ReplacementDictionary = replacementDic;
    }
    public override void Add()
    {
        ShotLimit = ShotLimitOpt.GetInt();
        CurrentKillCooldown = KillCooldown.GetFloat();
        Logger.Info($"{PlayerCatch.GetPlayerById(Player.PlayerId)?.GetNameWithRole().RemoveHtmlTags()} : 残り{ShotLimit}発", "SwitchSheriff");
    }
    private void SendRPC()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        using var sender = CreateSender();
        sender.Writer.Write(ShotLimit);
        sender.Writer.Write(Taskmode);
        sender.Writer.Write(nowcool);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        ShotLimit = reader.ReadInt32();
        Taskmode = reader.ReadBoolean();
        nowcool = reader.ReadSingle();
    }
    public bool CanUseKillButton()
        => Player.IsAlive()
        && !Taskmode
        && (CanKillAllAlive.GetBool() || GameStates.AlreadyDied)
        && ShotLimit > 0;

    bool CanChangeMode()
    => Player.IsAlive()
        && (CanKillAllAlive.GetBool() || GameStates.AlreadyDied)
        && ShotLimit > 0;
    public bool CanUseSabotageButton() => false;

    public override bool CanUseAbilityButton() => !Is(Player);
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(false);
        AURoleOptions.EngineerCooldown = 0;
        AURoleOptions.EngineerInVentMaxTime = 0.5f;
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        if (Is(info.AttemptKiller) && !info.IsSuicide)
        {
            if (LastCooltime > 0)
            {
                info.DoKill = false;
                return;
            }

            (var killer, var target) = info.AttemptTuple;

            Logger.Info($"{killer.GetNameWithRole().RemoveHtmlTags()} : 残り{ShotLimit}発", "SwitchSheriff");
            if (ShotLimit <= 0)
            {
                info.DoKill = false;
                return;
            }
            ShotLimit--;
            var AlienTairo = false;
            var targetroleclass = target.GetRoleClass();
            if ((targetroleclass as Alien)?.CheckSheriffKill(target) == true) AlienTairo = true;
            if ((targetroleclass as JackalAlien)?.CheckSheriffKill(target) == true) AlienTairo = true;
            if ((targetroleclass as AlienHijack)?.CheckSheriffKill(target) == true) AlienTairo = true;

            if (!CanBeKilledBy(target) || AlienTairo)
            {
                //ターゲットが大狼かつ死因を変える設定なら死因を変える、それ以外はMisfire
                PlayerState.GetByPlayerId(killer.PlayerId).DeathReason =
                        target.Is(CustomRoles.Tairou) && Tairou.TairoDeathReason ? CustomDeathReason.Counter :
                        target.Is(CustomRoles.Alien) && Alien.TairoDeathReason ? CustomDeathReason.Counter :
                        (target.Is(CustomRoles.JackalAlien) && JackalAlien.TairoDeathReason ? CustomDeathReason.Counter :
                        (target.Is(CustomRoles.AlienHijack) && Alien.TairoDeathReason ? CustomDeathReason.Counter : CustomDeathReason.Misfire));

                UtilsGameLog.AddGameLog("Sheriff", string.Format(GetString("SheriffMissLog"), UtilsName.GetPlayerColor(target.PlayerId)));
                _ = new LateTask(() => killer.RpcMurderPlayer(killer), Main.LagTime, "SwSheMiss");
                if (!MisfireKillsTarget.GetBool())
                {
                    info.DoKill = false;
                    return;
                }
            }
            nowcool = CurrentKillCooldown;
            ModeSwitching(true);
            SendRPC();
            //Player.SetKillCooldown(nowcool);
            killer.RpcResetAbilityCooldown(/*Sync: true*/);
        }
        return;
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;
        if (Player.IsAlive())
        {
            ModeSwitching(true);
            SendRPC();
        }
        Player.RpcResetAbilityCooldown(Sync: true);
    }
    public override RoleTypes? AfterMeetingRole => RoleTypes.Engineer;
    public override void AfterMeetingTasks()
    {
        if (!Player.IsAlive()) return;
        if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;
        _ = new LateTask(() => nowcool = CurrentKillCooldown, Main.LagTime, "Reset-SwitchSheriff");
    }
    public override string GetProgressText(bool comms = false, bool gamelog = false)
    {
        var progress = Utils.ColorString(CanChangeMode() ? Color.yellow : Color.gray, $"({ShotLimit})");
        if (!GameStates.CalledMeeting && !gamelog) progress += Utils.ColorString(Color.yellow, Taskmode ? $" [Task]<color=#ffffff>({LastCooltime})</color>" : $"  [Sheriff]<color=#ffffff>({LastCooltime})</color>");
        return progress;
    }
    public static bool CanBeKilledBy(PlayerControl player)
    {
        var cRole = player.GetCustomRole();

        if (player.GetRoleClass() is SchrodingerCat schrodingerCat)
        {
            if (schrodingerCat.Team == ISchrodingerCatOwner.TeamType.None)
            {
                Logger.Warn($"シェリフ({player.GetRealName()})にキルされたシュレディンガーの猫のロールが変化していません", nameof(Sheriff));
                return false;
            }
            else
            {
                if (player.IsLovers() && CanKillLovers.GetBool()) return true;
            }
            return schrodingerCat.Team switch
            {
                ISchrodingerCatOwner.TeamType.Mad => KillTargetOptions.TryGetValue(CustomRoles.Madmate, out var option) && option.GetBool(),
                ISchrodingerCatOwner.TeamType.Crew => false,
                _ => CanKillNeutrals.GetValue() == 0 || (SchrodingerCatKillTargetOptions.TryGetValue(schrodingerCat.Team, out var option) && option.GetBool()),
            };
        }

        if (player.IsLovers() && CanKillLovers.GetBool()) return true;

        if (cRole == CustomRoles.Jackaldoll) return CanKillNeutrals.GetValue() == 0 || (!KillTargetOptions.TryGetValue(CustomRoles.Jackal, out var option) && option.GetBool()) || (!KillTargetOptions.TryGetValue(CustomRoles.JackalMafia, out var op) && op.GetBool());
        if (cRole == CustomRoles.SKMadmate) return KillTargetOptions.TryGetValue(CustomRoles.Madmate, out var option) && option.GetBool();
        if (player.Is(CustomRoles.Amanojaku)) return CanKillNeutrals.GetValue() == 0;

        return cRole.GetCustomRoleTypes() switch
        {
            CustomRoleTypes.Impostor => cRole is not CustomRoles.Tairou,
            CustomRoleTypes.Madmate => KillTargetOptions.TryGetValue(CustomRoles.Madmate, out var option) && option.GetBool(),
            CustomRoleTypes.Neutral => CanKillNeutrals.GetValue() == 0 || (!KillTargetOptions.TryGetValue(cRole, out var option) && option.GetBool()),
            CustomRoleTypes.Crewmate => cRole is CustomRoles.WolfBoy,
            _ => false,
        };
    }
    public bool OverrideKillButton(out string text)
    {
        text = "Sheriff_Kill";
        return true;
    }
    public bool OverrideImpVentButton(out string text)
    {
        text = "SwitchSheriff_Vent";
        return true;
    }
    public override bool CanVentMoving(PlayerPhysics physics, int ventId) => false;
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (!CanChangeMode()) return false;
        if (Taskmode && Utils.IsActive(SystemTypes.Comms)) return false;//Hostはタスクモード(エンジ)での切り替えできるからさせないようにする

        ModeSwitching();
        SendRPC();
        //_ = new LateTask(() => Player.SetKillCooldown(kill), 0.2f, "",true);
        return false;
    }
    public override bool CanTask()
    {
        if (!Player.IsAlive()) return true;
        return Taskmode;
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (GameStates.CalledMeeting || GameStates.Intro) return;

        if (!player.IsAlive()) return;

        if (nowcool > 0)
            nowcool -= Time.fixedDeltaTime;
        else nowcool = 0;
        var now = (int)nowcool;
        if (now != LastCooltime)
        {
            if (now <= 0) player.SetKillCooldown(0.5f);//相互性が取れないので～
            LastCooltime = now;
            if (player != PlayerControl.LocalPlayer)
                UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: player);
        }
    }
    public override bool OnSabotage(PlayerControl _, SystemTypes sabotage)
    {
        if (!CanChangeMode()) return true;
        if (CommsMode.GetValue() == 0) return true;
        if (AddOns.Common.Amnesia.CheckAbility(Player))
            if (sabotage == SystemTypes.Comms)
            {
                var task = CommsMode.GetValue() == 2;
                ModeSwitching(task);
                SendRPC();
            }
        return true;
    }
    private bool ModeSwitching(bool? taskMode = null)
    {
        //モードを変更
        Taskmode = taskMode ?? !Taskmode;

        //ロール変更
        if (!Is(PlayerControl.LocalPlayer))
        {
            foreach (var pc in PlayerCatch.AllAlivePlayerControls)
            {
                var role = pc.GetCustomRole();
                if (role.IsImpostor())
                    pc.RpcSetRoleDesync(Taskmode ? role.GetRoleTypes() : RoleTypes.Scientist, Player.GetClientId());
                if (Is(pc))
                    pc.RpcSetRoleDesync(Taskmode ? role.GetRoleTypes() : RoleTypes.Impostor, Player.GetClientId());
            }
        }
        //シェリフモードのみ実行
        if (!Taskmode)
        {
            Player.SetKillCooldown(Mathf.Max(LastCooltime, 0.1f), delay: true);//ラグで貯まる一瞬前にぼーんできないように
        }
        return Taskmode;
    }
}
