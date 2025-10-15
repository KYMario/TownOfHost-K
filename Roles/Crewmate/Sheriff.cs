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

public sealed class Sheriff : RoleBase, IKiller, ISchrodingerCatOwner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Sheriff),
            player => new Sheriff(player),
            CustomRoles.Sheriff,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Crewmate,
            8800,
            SetupOptionItem,
            "sh",
            "#f8cd46",
            (2, 0),
            true,
            introSound: () => GetIntroSound(RoleTypes.Crewmate),
            from: From.SheriffMod,
            Desc: () => string.Format(GetString("SheriffDesc"), CanKillMadmate.GetBool() ? $"{GetString("Madmate")}、" : "", CanKillNeutrals.GetBool() ? GetString("SheriffDescCanKillNeutralOption") : GetString("Neutral"), CanKillLovers.GetBool() ? $"、{GetString("Lovers")}" : ""
            , MisfireKillsTarget.GetBool() ? GetString("SheriffDescMisfireKillsTarget") : "", CanKillAllAlive.GetBool() ? GetString("SheriffDescAllAlive") : "", ShotLimitOpt.GetInt())
        );
    public Sheriff(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        ShotLimit = ShotLimitOpt.GetInt();
        CurrentKillCooldown = KillCooldown.GetFloat();
    }

    public static OptionItem KillCooldown;
    private static OptionItem MisfireKillsTarget;
    private static OptionItem CanKillMadmate;
    public static OptionItem ShotLimitOpt;
    public static OptionItem CanKillAllAlive;
    public static OptionItem CanKillNeutrals;
    public static OptionItem CanKillLovers;
    enum OptionName
    {
        SheriffMisfireKillsTarget,
        SheriffShotLimit,
        SheriffCanKillAllAlive,
        SheriffCanKillNeutrals,
        SheriffCanKill,
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

    public ISchrodingerCatOwner.TeamType SchrodingerCatChangeTo => ISchrodingerCatOwner.TeamType.Crew;

    private static void SetupOptionItem()
    {
        KillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 990f, 0.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OverrideKilldistance.Create(RoleInfo, 8);
        MisfireKillsTarget = BooleanOptionItem.Create(RoleInfo, 11, OptionName.SheriffMisfireKillsTarget, false, false);
        ShotLimitOpt = IntegerOptionItem.Create(RoleInfo, 12, OptionName.SheriffShotLimit, new(1, 15, 1), 15, false)
            .SetValueFormat(OptionFormat.Times);
        CanKillAllAlive = BooleanOptionItem.Create(RoleInfo, 15, OptionName.SheriffCanKillAllAlive, true, false);
        CanKillMadmate = SetUpKillTargetOption(CustomRoles.Madmate, 13);
        CanKillNeutrals = StringOptionItem.Create(RoleInfo, 14, OptionName.SheriffCanKillNeutrals, KillOption, 0, false);
        SetUpNeutralOptions(30);
        CanKillLovers = BooleanOptionItem.Create(RoleInfo, 16, OptionName.SheriffCanKillLovers, true, false);
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
    public static OptionItem SetUpKillTargetOption(CustomRoles role, int idOffset, bool defaultValue = true, OptionItem parent = null)
    {
        var id = RoleInfo.ConfigId + idOffset;
        if (parent == null) parent = RoleInfo.RoleOption;
        var roleName = UtilsRoleText.GetRoleName(role);
        Dictionary<string, string> replacementDic = new() { { "%role%", Utils.ColorString(UtilsRoleText.GetRoleColor(role), roleName) } };
        var roleoptionitem = BooleanOptionItem.Create(id, OptionName.SheriffCanKill + "%role%", defaultValue, RoleInfo.Tab, false).SetParent(parent).SetParentRole(CustomRoles.Sheriff);
        KillTargetOptions[role] = roleoptionitem;
        KillTargetOptions[role].ReplacementDictionary = replacementDic;

        return roleoptionitem;
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
        SchrodingerCatKillTargetOptions[catType] = BooleanOptionItem.Create(id, OptionName.SheriffCanKill + "%role%", defaultValue, RoleInfo.Tab, false).SetParent(parent).SetParentRole(CustomRoles.Sheriff);
        SchrodingerCatKillTargetOptions[catType].ReplacementDictionary = replacementDic;
    }
    public override void Add()
    {
        var playerId = Player.PlayerId;
        CurrentKillCooldown = KillCooldown.GetFloat();

        ShotLimit = ShotLimitOpt.GetInt();
        Logger.Info($"{PlayerCatch.GetPlayerById(playerId)?.GetNameWithRole().RemoveHtmlTags()} : 残り{ShotLimit}発", "Sheriff");
    }
    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(ShotLimit);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        ShotLimit = reader.ReadInt32();
    }
    public float CalculateKillCooldown() => CanUseKillButton() ? CurrentKillCooldown : 0f;
    public bool CanUseKillButton()
        => Player.IsAlive()
        && (CanKillAllAlive.GetBool() || GameStates.AlreadyDied)
        && ShotLimit > 0;
    public bool CanUseImpostorVentButton() => false;
    public bool CanUseSabotageButton() => false;
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(false);
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        if (Is(info.AttemptKiller) && !info.IsSuicide)
        {
            (var killer, var target) = info.AttemptTuple;

            Logger.Info($"{killer.GetNameWithRole().RemoveHtmlTags()} : 残り{ShotLimit}発", "Sheriff");
            if (ShotLimit <= 0)
            {
                info.DoKill = false;
                return;
            }
            ShotLimit--;
            SendRPC();
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
                killer.RpcMurderPlayer(killer);
                UtilsGameLog.AddGameLog("Sheriff", string.Format(GetString("SheriffMissLog"), UtilsName.GetPlayerColor(target.PlayerId)));
                if (!MisfireKillsTarget.GetBool())
                {
                    info.DoKill = false;
                    return;
                }
            }

            killer.ResetKillCooldown();
        }
        return;
    }
    public override string GetProgressText(bool comms = false, bool gamelog = false) => Utils.ColorString(CanUseKillButton() ? Color.yellow : Color.gray, $"({ShotLimit})");
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
}
