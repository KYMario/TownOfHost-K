using System;
using System.Collections.Generic;
using AmongUs.GameOptions;

using Hazel;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using UnityEngine;

namespace TownOfHost.Roles.Neutral;

public sealed class JackalWolf : RoleBase, ILNKiller, IUsePhantomButton, IDoubleTrigger, IKillFlashSeeable, ISidekickable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(JackalWolf),
            player => new JackalWolf(player),
            CustomRoles.JackalWolf,
            () => OptionHaveRole.GetRole().GetRoleInfo()?.BaseRoleType?.Invoke() ?? RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            24700,
            SetupOptionItem,
            "Jaw",
            "#00b4eb",
            (1, 3),
            true,
            countType: CountTypes.Jackal,
            assignInfo: new RoleAssignInfo(CustomRoles.JackalWolf, CustomRoleTypes.Neutral)
            {
                AssignCountRule = new(1, 1, 1)
            });
    public JackalWolf(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CanVent = OptionCanVent.GetBool();
        CanUseSabotage = OptionCanUseSabotage.GetBool();
        JackalCanAlsoBeExposedToJMafia = OptionJackalCanAlsoBeExposedToJMafia.GetBool();
        JackalMafiaCanAlsoBeExposedToJackal = OptionJJackalMafiaCanAlsoBeExposedToJackal.GetBool();
    }
    static OptionItem OptionCanVent;
    static OptionItem OptionCanUseSabotage;
    static OptionItem OptionHasImpostorVision;
    static OptionItem OptionJackalCanAlsoBeExposedToJMafia;
    static OptionItem OptionJJackalMafiaCanAlsoBeExposedToJackal;
    static OptionItem OptionJJackalCanKillMafia;
    private static bool JackalCanAlsoBeExposedToJMafia;
    private static bool JackalMafiaCanAlsoBeExposedToJackal;
    public static bool CanVent;
    public static bool CanUseSabotage;
    static FilterOptionItem OptionHaveRole; static CustomRoles haverole;
    RoleBase AddRole;

    static CustomRoles[] InvalidRoles()
    {
        List<CustomRoles> InvalidRoles = new();
        foreach (var data in CustomRoleManager.AllRolesInfo)
        {
            if (data.Key.IsImpostor() is false && data.Key is not CustomRoles.NotAssigned) continue;
            if (data.Key is CustomRoles.AlienHijack or CustomRoles.EvilSatellite or CustomRoles.ConnectSaver
            or CustomRoles.Limiter or CustomRoles.Assassin or CustomRoles.Amnesiac)
            {
                InvalidRoles.Add(data.Key);
                continue;
            }
        }
        return InvalidRoles.ToArray();
    }
    enum OptionName
    {
        AssassinHaveRole
    }
    public static void SetupOptionItem()
    {
        OptionHaveRole = FilterOptionItem.Create(RoleInfo, 17, OptionName.AssassinHaveRole, 0, false, null, true, false, false, false, false, () => InvalidRoles());

        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.CanVent, true, false);
        OptionCanUseSabotage = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.CanUseSabotage, false, false);
        OptionHasImpostorVision = BooleanOptionItem.Create(RoleInfo, 13, GeneralOption.ImpostorVision, true, false);
        OptionJJackalCanKillMafia = BooleanOptionItem.Create(RoleInfo, 14, JackalMafia.JackalOption.JackalCanKillMafia, false, false);
        OptionJJackalMafiaCanAlsoBeExposedToJackal = BooleanOptionItem.Create(RoleInfo, 15, JackalMafia.JackalOption.JackalMafiaCanAlsoBeExposedToJackal, false, false);
        OptionJackalCanAlsoBeExposedToJMafia = BooleanOptionItem.Create(RoleInfo, 16, JackalMafia.JackalOption.JackalCanAlsoBeExposedToJMafia, true, false);
    }
    public override void Add()
    {
        haverole = OptionHaveRole.GetBool() ? OptionHaveRole.GetRole() : CustomRoles.Impostor;
        if (CustomRoleManager.AllRolesInfo.TryGetValue(haverole, out var roleinfo))
        {
            AddRole = roleinfo.CreateInstance(Player);
            Logger.Info($"JackalWolf + {haverole}", "JackalWolf");
        }
        AddRole?.Add();
        Player.AddDoubleTrigger();
    }
    public override void OnDestroy()
    {
        if (AddRole is not null)
        {
            AddRole?.OnDestroy();
        }
        AddRole = null;
        haverole = CustomRoles.NotAssigned;
    }
    public override void OnFixedUpdate(PlayerControl player) => AddRole?.OnFixedUpdate(player);
    public override void OnSpawn(bool initialState = false) => AddRole?.OnSpawn(initialState);
    public override void OnStartMeeting() => AddRole?.OnStartMeeting();
    public override bool VotingResults(ref NetworkedPlayerInfo Exiled, ref bool IsTie, Dictionary<byte, int> vote, byte[] mostVotedPlayers, bool ClearAndExile)
        => AddRole?.VotingResults(ref Exiled, ref IsTie, vote, mostVotedPlayers, ClearAndExile) ?? false;
    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
        => AddRole?.CheckVoteAsVoter(votedForId, voter) ?? true;
    public override void OverrideTrueRoleName(ref Color roleColor, ref string roleText)
    {
        if (AddRole is null || haverole is CustomRoles.Impostor) return;

        roleText += $"<size=50%>{GetString($"{haverole}")}</size>";
    }
    public override CustomRoles HaveAddRole() => haverole;
    public override bool OnEnterVent(PlayerPhysics physics, int ventId) => AddRole?.OnEnterVent(physics, ventId) ?? true;
    public override bool CanVentMoving(PlayerPhysics physics, int ventId) => AddRole?.CanVentMoving(physics, ventId) ?? true;
    public override bool CanUseAbilityButton() => AddRole?.CanUseAbilityButton() ?? false;
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(OptionHasImpostorVision.GetBool());
        AddRole?.ApplyGameOptions(opt);
    }
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;
        if (killer.Is(CountTypes.Jackal) && !OptionJJackalCanKillMafia.GetBool())
        {
            info.DoKill = false;
            killer.SetKillCooldown();
            return false;
        }
        return AddRole?.OnCheckMurderAsTarget(info) ?? true;
    }
    public override void OnMurderPlayerAsTarget(MurderInfo info) => AddRole?.OnMurderPlayerAsTarget(info);
    public override void OnShapeshift(PlayerControl target) => AddRole?.OnShapeshift(target);
    public override bool CheckShapeshift(PlayerControl target, ref bool shouldAnimate) => AddRole?.CheckShapeshift(target, ref shouldAnimate) ?? true;
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target) => AddRole?.OnReportDeadBody(reporter, target);
    public override bool CanClickUseVentButton => AddRole?.CanClickUseVentButton ?? true;
    public override string MeetingAddMessage() => AddRole?.MeetingAddMessage() ?? "";
    public override (byte? votedForId, int? numVotes, bool doVote) ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional) => AddRole?.ModifyVote(voterId, sourceVotedForId, isIntentional) ?? (null, null, true);
    public override void OnExileWrapUp(NetworkedPlayerInfo exiled, ref bool DecidedWinner) => AddRole?.OnExileWrapUp(exiled, ref DecidedWinner);
    public override void AfterMeetingTasks() => AddRole?.AfterMeetingTasks();
    public override void StartGameTasks() => AddRole?.StartGameTasks();
    public override bool OnInvokeSabotage(SystemTypes systemType) => AddRole?.OnInvokeSabotage(systemType) ?? true;
    public override bool OnSabotage(PlayerControl player, SystemTypes systemType) => AddRole?.OnSabotage(player, systemType) ?? true;
    public override void AfterSabotage(SystemTypes systemType) => AddRole?.AfterSabotage(systemType);
    public override bool NotifyRolesCheckOtherName => AddRole?.NotifyRolesCheckOtherName ?? false;
    public override void OverrideProgressTextAsSeer(PlayerControl seen, ref bool enabled, ref string text)
    => AddRole?.OverrideProgressTextAsSeer(seen, ref enabled, ref text);
    public override void OverrideProgressTextAsSeen(PlayerControl seer, ref bool enabled, ref string text)
    => AddRole?.OverrideProgressTextAsSeen(seer, ref enabled, ref text);
    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, ref bool enabled, ref Color roleColor, ref string roleText, ref bool addon)
    {
        if ((seen.Is(CustomRoles.Jackal) || seen.Is(CustomRoles.JackalMafia) || seen.Is(CustomRoles.JackalAlien) || seen.Is(CustomRoles.JackalWolf)) && JackalMafiaCanAlsoBeExposedToJackal)
        {
            enabled = true;
            addon = false;
        }
        AddRole?.OverrideDisplayRoleNameAsSeer(seen, ref enabled, ref roleColor, ref roleText, ref addon);
    }
    public override void OverrideDisplayRoleNameAsSeen(PlayerControl seen, ref bool enabled, ref Color roleColor, ref string roleText, ref bool addon)
    {
        if ((seen.Is(CustomRoles.Jackal) || seen.Is(CustomRoles.JackalMafia) || seen.Is(CustomRoles.JackalAlien) || seen.Is(CustomRoles.JackalWolf)) && JackalCanAlsoBeExposedToJMafia)
        {
            enabled = true;
            addon = false;
        }
        AddRole?.OverrideDisplayRoleNameAsSeen(seen, ref enabled, ref roleColor, ref roleText, ref addon);
    }

    public override string GetProgressText(bool comms = false, bool GameLog = false) => AddRole?.GetProgressText(comms, GameLog);
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false) => AddRole?.GetMark(seer, seen, isForMeeting);
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false) => AddRole?.GetLowerText(seer, seen, isForMeeting, isForHud);
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false) => AddRole?.GetSuffix(seer, seen, isForMeeting);
    public override string GetAbilityButtonText() => AddRole?.GetAbilityButtonText();
    public override bool OverrideAbilityButton(out string text)
    {
        if (AddRole?.OverrideAbilityButton(out var abilitytext) ?? false)
        {
            text = abilitytext;
            return true;
        }
        text = default;
        return false;
    }
    public override bool CancelReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target, ref DontReportreson reason)
        => AddRole?.CancelReportDeadBody(reporter, target, ref reason) ?? false;
    public override CustomRoles TellResults(PlayerControl player) => AddRole?.TellResults(player) ?? CustomRoles.NotAssigned;
    public override RoleTypes? AfterMeetingRole => AddRole?.AfterMeetingRole ?? null;
    public override void CheckWinner(GameOverReason reason) => AddRole?.CheckWinner(reason);
    public bool CanKill => AddRole is IKiller killer ? killer.CanKill : true;
    public bool IsKiller => AddRole is IKiller killer ? killer.IsKiller : true;
    public bool CanUseKillButton() => AddRole is IKiller killer ? killer.CanUseKillButton() : CanKill;
    public float CalculateKillCooldown() => AddRole is IKiller killer ? killer.CalculateKillCooldown() : Options.DefaultKillCooldown;
    public bool CanUseSabotageButton() => CanUseSabotage && (AddRole is IKiller killer ? killer.CanUseSabotageButton() : true);
    public bool CanUseImpostorVentButton() => CanVent && (AddRole is IKiller killer ? killer.CanUseImpostorVentButton() : true);
    public void OnCheckMurderAsKiller(MurderInfo info) { if (AddRole is IKiller killer) killer.OnCheckMurderAsKiller(info); }
    public void OnCheckMurderDontKill(MurderInfo info) { if (AddRole is IKiller killer) killer.OnCheckMurderDontKill(info); }
    public void OnMurderPlayerAsKiller(MurderInfo info) { if (AddRole is IKiller killer) killer.OnCheckMurderAsKiller(info); }
    public bool OverrideKillButtonText(out string text)
    {
        if ((AddRole as IKiller)?.OverrideKillButtonText(out var killbuttontext) is true)
        {
            text = killbuttontext;
            return true;
        }
        text = default;
        return false;
    }
    public bool OverrideKillButton(out string text)
    {
        if ((AddRole as IImpostor)?.OverrideKillButton(out var killbutton) is true)
        {
            text = killbutton;
            return true;
        }
        text = default;
        return false;
    }
    public bool OverrideImpVentButton(out string text)
    {
        if ((AddRole as IImpostor)?.OverrideImpVentButton(out var ventbutton) is true)
        {
            text = ventbutton;
            return true;
        }
        text = default;
        return false;
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        try
        {
            AddRole.ReceiveRPC(reader);
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex}", "Assassin");
        }
    }
    bool IUsePhantomButton.IsPhantomRole => AddRole is IUsePhantomButton iusephantom && iusephantom?.IsPhantomRole is true;
    bool IUsePhantomButton.UseOneclickButton => AddRole is IUsePhantomButton iusephantom && iusephantom?.UseOneclickButton is true;
    public void OnClick(ref bool AdjustKillCooldown, ref bool? ResetCooldown)
    {
        if (AddRole is IUsePhantomButton iusephantom)
        {
            iusephantom.OnClick(ref AdjustKillCooldown, ref ResetCooldown);
        }
    }
    public bool CheckAction => (AddRole as IDoubleTrigger)?.CheckAction ?? false;
    public bool SingleAction(PlayerControl killer, PlayerControl target)
    {
        return (AddRole as IDoubleTrigger)?.SingleAction(killer, target) ?? true;
    }

    public bool DoubleAction(PlayerControl killer, PlayerControl target)
    {
        return (AddRole as IDoubleTrigger)?.DoubleAction(killer, target) ?? true;
    }
    public bool? CheckKillFlash(MurderInfo info) => (AddRole as IKillFlashSeeable)?.CheckKillFlash(info) ?? false;
}