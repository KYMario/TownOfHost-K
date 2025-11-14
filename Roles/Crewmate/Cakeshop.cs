using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.AddOns.Common;

namespace TownOfHost.Roles.Crewmate;

public sealed class Cakeshop : RoleBase, INekomata
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Cakeshop),
            player => new Cakeshop(player),
            CustomRoles.Cakeshop,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            21200,
            null,
            "cs",
            "#aacbff",
            (0, 15),
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );
    public Cakeshop(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Addedaddons.Clear();
        CustomRoleManager.LowerOthers.Add(GetLowerTextOthers);
    }

    public Dictionary<byte, CustomRoles> Addedaddons = new();


    public override void AfterMeetingTasks()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (Event.CheckRole(CustomRoles.Cakeshop) is false)
        {
            Logger.Info($"真っ黒こげのケーキ", "CakeShop");
            Player.RpcSetCustomRole(CustomRoles.Emptiness);
            return;
        }
        Logger.Info("あっ、ケーキの効果が切れちゃった!!", nameof(Cakeshop));
        foreach (var gu in Addedaddons.Where(v => v.Value is CustomRoles.Guarding))
        {
            var state = PlayerCatch.GetPlayerState(gu.Key);

            state.HaveGuard[1] -= Guarding.HaveGuard;
        }
        PlayerState.AllPlayerStates.DoIf(
            x => Addedaddons.ContainsKey(x.Key),
            state => PlayerCatch.GetPlayerById(state.Key).RpcReplaceSubRole(Addedaddons[state.Key], true));

        if (!Player.IsAlive())
        {
            Addedaddons.Clear();
            return;
        }
        Logger.Info("ケーキちょうど焼けたからあげる!!", nameof(Cakeshop));
        PlayerCatch.AllAlivePlayerControls.Do(pc =>
        {
            if (pc == null) return;
            var addons = GetAddons(pc.GetCustomRole().GetCustomRoleTypes());
            if (addons == null) return;
            var addon = addons.Where(x => !pc.GetCustomSubRoles().Contains(x) && x is not CustomRoles.Amnesia and not CustomRoles.Amanojaku)
                              .OrderBy(x => Guid.NewGuid())
                              .FirstOrDefault();
            Addedaddons[pc.PlayerId] = addon;

            if (addon is CustomRoles.Guarding)
            {
                var state = pc.GetPlayerState();
                state.HaveGuard[1] += Guarding.HaveGuard;
            }
            pc.RpcSetCustomRole(addon);
        });
    }

    CustomRoles[] GetAddons(CustomRoleTypes type)
    {
        return type switch
        {
            CustomRoleTypes.Impostor => AddOns.Common.AddOnsAssignData.AllData.Values.Where(x => x.ImpostorMaximum != null).Select(x => x.Role).ToArray(),
            CustomRoleTypes.Madmate => AddOns.Common.AddOnsAssignData.AllData.Values.Where(x => x.MadmateMaximum != null).Select(x => x.Role).ToArray(),
            CustomRoleTypes.Crewmate => AddOns.Common.AddOnsAssignData.AllData.Values.Where(x => x.CrewmateMaximum != null).Select(x => x.Role).ToArray(),
            CustomRoleTypes.Neutral => AddOns.Common.AddOnsAssignData.AllData.Values.Where(x => x.NeutralMaximum != null).Select(x => x.Role).ToArray(),
            _ => null,
        };
    }

    public string GetLowerTextOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (!Player.IsAlive()) return "";
        if (seen != seer) return "";
        if (isForMeeting) return "";
        return Addedaddons.TryGetValue(seen.PlayerId, out var role) ? $"<size=50%>{Utils.ColorString(UtilsRoleText.GetRoleColor(role), GetString($"{role}Info"))}</size>" : "";
    }

    public bool DoRevenge(CustomDeathReason deathReason)
        => true;

    public bool IsCandidate(PlayerControl player)
        => true;

    public override void OnDestroy()
    {
        if (CustomWinnerHolder.WinnerTeam is not CustomWinner.Default) return;

        AfterMeetingTasks();
    }
}
