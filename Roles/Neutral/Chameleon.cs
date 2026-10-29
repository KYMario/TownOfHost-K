using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Hazel;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Neutral;

public sealed class Chameleon : RoleBase, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Chameleon),
            player => new Chameleon(player),
            CustomRoles.Chameleon,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            20700,
            SetUpOptionItem,
            "Ch",
            "#357a39",
            OptionSort: (6, 4)
        );
    public Chameleon(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        NowTeam = CustomRoles.NotAssigned;
        TeamList.Clear();

        var ch = true;
        foreach (var pc in PlayerCatch.AllPlayerControls)
        {
            if (pc.GetCustomRole().IsImpostor()) TeamList.Add(CustomRoles.Impostor);
            if (pc.GetCustomRole().IsCrewmate())
            {
                if (ch)
                {
                    TeamList.Add(CustomRoles.Crewmate);
                    ch = false;
                }
                else
                {
                    ch = true;
                }
            }
        }
        if (CustomRoles.Jackal.IsPresent() || CustomRoles.JackalAlien.IsPresent() || CustomRoles.JackalMafia.IsPresent() || CustomRoles.JackalWolf.IsPresent())
            TeamList.Add(CustomRoles.Jackal);

        if (CustomRoles.Egoist.IsPresent())
            TeamList.Add(CustomRoles.Egoist);
        if (CustomRoles.Remotekiller.IsPresent())
            TeamList.Add(CustomRoles.Remotekiller);
        if (CustomRoles.CountKiller.IsPresent())
            TeamList.Add(CustomRoles.CountKiller);
        if (CustomRoles.DoppelGanger.IsPresent())
            TeamList.Add(CustomRoles.DoppelGanger);
        if (CustomRoles.GrimReaper.IsPresent())
            TeamList.Add(CustomRoles.GrimReaper);
        if (CustomRoles.Fox.IsPresent())
            TeamList.Add(CustomRoles.Fox);
        if (CustomRoles.Arsonist.IsPresent())
            TeamList.Add(CustomRoles.Arsonist);

    }
    CustomRoles NowTeam;
    List<CustomRoles> TeamList = new();
    static void SetUpOptionItem() => RoleAddAddons.Create(RoleInfo, 20);
    public override void StartGameTasks() => ChengeTeam();
    void ChengeTeam()
    {
        if (AmongUsClient.Instance.AmHost is false) return;
        var oldteam = NowTeam;
        if (!PlayerCatch.AllAlivePlayerControls.Any(p => p.GetCustomRole() is CustomRoles.Jackal or CustomRoles.JackalAlien or CustomRoles.JackalMafia or CustomRoles.JackalWolf))
            TeamList.Remove(CustomRoles.Jackal);
        if (!PlayerCatch.AllAlivePlayerControls.Any(p => p.GetCustomRole() is CustomRoles.Egoist))
            TeamList.Remove(CustomRoles.Egoist);
        if (!PlayerCatch.AllAlivePlayerControls.Any(p => p.GetCustomRole() is CustomRoles.Remotekiller))
            TeamList.Remove(CustomRoles.Remotekiller);
        if (!PlayerCatch.AllAlivePlayerControls.Any(p => p.GetCustomRole() is CustomRoles.CountKiller))
            TeamList.Remove(CustomRoles.CountKiller);
        if (!PlayerCatch.AllAlivePlayerControls.Any(p => p.GetCustomRole() is CustomRoles.DoppelGanger))
            TeamList.Remove(CustomRoles.DoppelGanger);
        if (!PlayerCatch.AllAlivePlayerControls.Any(p => p.GetCustomRole() is CustomRoles.GrimReaper))
            TeamList.Remove(CustomRoles.GrimReaper);
        if (!PlayerCatch.AllAlivePlayerControls.Any(p => p.GetCustomRole() is CustomRoles.Fox))
            TeamList.Remove(CustomRoles.Fox);
        if (!PlayerCatch.AllAlivePlayerControls.Any(p => p.GetCustomRole() is CustomRoles.Arsonist))
            TeamList.Remove(CustomRoles.Arsonist);

        //リストをシャッフル! → 更にランダム！
        NowTeam = TeamList.OrderBy(x => Guid.NewGuid()).ToArray()[IRandom.Instance.Next(TeamList.Count)];

        Logger.Info($"Now : {NowTeam}", "Chameleon");

        SendRpc();
        if (oldteam != NowTeam) UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player);
    }
    void SendRpc()
    {
        using var sender = CreateSender();
        sender.Writer.WritePacked((int)NowTeam);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        NowTeam = (CustomRoles)reader.ReadPackedInt32();
    }
    public override void OverrideTrueRoleName(ref UnityEngine.Color roleColor, ref string roleText) => roleText = Translator.GetString($"{NowTeam}").Color(UtilsRoleText.GetRoleColor(NowTeam)) + Translator.GetString("Chameleon");
    public override void AfterMeetingTasks() => _ = new LateTask(() => { if (!GameStates.CalledMeeting) ChengeTeam(); }, 5f, "", true);
    public bool CheckWin(ref CustomRoles winnerRole) => ((CustomRoles)CustomWinnerHolder.WinnerTeam == NowTeam) || CustomWinnerHolder.AdditionalWinnerRoles.Contains(NowTeam);
}