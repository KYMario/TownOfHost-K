using System.Collections.Generic;
using System.Linq;
using Hazel;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Neutral;

public sealed class Executioner : RoleBase, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Executioner),
            player => new Executioner(player),
            CustomRoles.Executioner,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            14300,
            SetupOptionItem,
            "exe",
            "#611c3a",
            (4, 1),
            introSound: () => GetIntroSound(RoleTypes.Shapeshifter),
            from: From.TownOfUs
        );
    public Executioner(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => ChangeRolesAfterTargetKilled == CustomRoles.Crewmate ? HasTask.ForRecompute : HasTask.False
    )
    {
        CanTargetImpostor = OptionCanTargetImpostor.GetBool();
        CanTargetNeutralKiller = OptionCanTargetNeutralKiller.GetBool();
        ChangeRolesAfterTargetKilled = ChangeRoles[OptionChangeRolesAfterTargetKilled.GetValue()];

        Executioners.Add(this);
        CustomRoleManager.OnMurderPlayerOthers.Add(OnMurderPlayerOthers);

        TargetExiled = false;
        IsReport = false;
    }

    private static OptionItem OptionCanTargetImpostor;
    private static OptionItem OptionCanTargetNeutralKiller;
    public static OptionItem OptionChangeRolesAfterTargetKilled;
    enum OptionName
    {
        ExecutionerCanTargetImpostor,
        ExecutionerCanTargetNeutralKiller,
        ExecutionerChangeRolesAfterTargetKilled
    }

    private static bool CanTargetImpostor;
    private static bool CanTargetNeutralKiller;
    public static CustomRoles ChangeRolesAfterTargetKilled;

    public static HashSet<Executioner> Executioners = new(15);
    public byte TargetId;
    private bool TargetExiled;
    bool IsReport;
    public static readonly CustomRoles[] ChangeRoles =
    {
            CustomRoles.Crewmate, CustomRoles.Jester, CustomRoles.Opportunist,CustomRoles.Monochromer
    };

    private static void SetupOptionItem()
    {
        SoloWinOption.Create(RoleInfo, 9, defo: 1);
        var cRolesString = ChangeRoles.Select(x => x.ToString()).ToArray();
        OptionCanTargetImpostor = BooleanOptionItem.Create(RoleInfo, 10, OptionName.ExecutionerCanTargetImpostor, false, false);
        OptionCanTargetNeutralKiller = BooleanOptionItem.Create(RoleInfo, 12, OptionName.ExecutionerCanTargetNeutralKiller, false, false);
        OptionChangeRolesAfterTargetKilled = StringOptionItem.Create(RoleInfo, 11, OptionName.ExecutionerChangeRolesAfterTargetKilled, cRolesString, 1, false);
    }
    public override bool NotifyRolesCheckOtherName => true;
    public override void Add()
    {
        //ターゲット割り当て
        if (!AmongUsClient.Instance.AmHost) return;

        var playerId = Player.PlayerId;
        List<PlayerControl> targetList = new();
        var rand = IRandom.Instance;
        foreach (var target in PlayerCatch.AllPlayerControls)
        {
            if (playerId == target.PlayerId) continue;
            else if (!CanTargetImpostor && target.Is(CustomRoleTypes.Impostor)) continue;
            else if (!CanTargetNeutralKiller && target.IsNeutralKiller() || target.Is(CustomRoles.GrimReaper)) continue;
            if (target.Is(CustomRoles.GM)) continue;

            targetList.Add(target);
        }
        if (targetList.Count == 0) return;
        var SelectedTarget = targetList[rand.Next(targetList.Count)];
        TargetId = SelectedTarget.PlayerId;
        SendRPC();
        Logger.Info($"{Player.GetNameWithRole().RemoveHtmlTags()}:{SelectedTarget.GetNameWithRole().RemoveHtmlTags()}", "Executioner");
    }
    public override void OnDestroy()
    {
        Executioners.Remove(this);

        if (Executioners.Count <= 0)
        {
            CustomRoleManager.OnMurderPlayerOthers.Remove(OnMurderPlayerOthers);
        }
    }
    public void SendRPC()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        using var sender = CreateSender();
        sender.Writer.Write(TargetId);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        byte targetId = reader.ReadByte();
        TargetId = targetId;
    }
    public override void OnMurderPlayerAsTarget(MurderInfo _)
    {
        TargetId = byte.MaxValue;
        SendRPC();
    }
    public static void OnMurderPlayerOthers(MurderInfo info)
    {
        var target = info.AttemptTarget;

        foreach (var executioner in Executioners.ToArray())
        {
            if (executioner.TargetId == target.PlayerId)
            {
                executioner.ChangeRole();
                break;
            }
        }
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool _ = false)
    {
        //seenが省略の場合seer
        seen ??= seer;

        return TargetId == seen.PlayerId ? Utils.ColorString(RoleInfo.RoleColor, "♦") : "";
    }
    public override void OnExileWrapUp(NetworkedPlayerInfo exiled, ref bool DecidedWinner)
    {
        if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;
        if (!AmongUsClient.Instance.AmHost) return;
        if (Player?.IsAlive() != true) return;
        if (exiled.PlayerId != TargetId) return;

        TargetExiled = true;

        if (!DecidedWinner)
        {
            if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) return; //勝者がいるなら処理をスキップ

            if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Executioner, Player.PlayerId))
            {
                CustomWinnerHolder.NeutralWinnerIds.Add(Player.PlayerId);
                CustomWinnerHolder.WinnerIds.Add(Player.PlayerId);
                Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[0]);
                if (IsReport) Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[1]);
            }
        }
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target) => IsReport = reporter.PlayerId == Player.PlayerId;
    public bool CheckWin(ref CustomRoles winnerRole)
    {
        return TargetExiled && CustomWinnerHolder.WinnerTeam != CustomWinner.Default;
    }
    public void ChangeRole()
    {
        if (!Utils.RoleSendList.Contains(Player.PlayerId)) Utils.RoleSendList.Add(Player.PlayerId);
        UtilsGameLog.AddGameLog($"Executioner", UtilsName.GetPlayerColor(Player) + ":  " + string.Format(GetString("Executioner.ch"), UtilsName.GetPlayerColor(TargetId, true), GetString($"{ChangeRolesAfterTargetKilled}").Color(UtilsRoleText.GetRoleColor(ChangeRolesAfterTargetKilled))));
        Player.RpcSetCustomRole(ChangeRolesAfterTargetKilled, true);
        UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player);
    }

    public static void ChangeRoleByTarget(byte targetId)
    {
        foreach (var executioner in Executioners)
        {
            if (executioner.TargetId != targetId) continue;

            executioner.ChangeRole();
            break;
        }
    }
    public static Dictionary<int, Achievement> achievements = new();
    [Attributes.PluginModuleInitializer]
    public static void Load()
    {
        var n1 = new Achievement(RoleInfo, 0, 1, 0, 0);
        var l1 = new Achievement(RoleInfo, 1, 1, 0, 1);
        achievements.Add(0, n1);
        achievements.Add(1, l1);
    }
}