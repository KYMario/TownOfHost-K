using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Hazel;
using HarmonyLib;
using UnityEngine;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.PlayerCatch;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Neutral;

public sealed class Monika : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Monika),
            player => new Monika(player),
            CustomRoles.Monika,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            29500,
            SetupOptionItem,
            "monika",
            "#ffb6c1",
            (6, 2),
            from: From.SuperNewRoles,
            isDesyncImpostor: true
        );

    public Monika(PlayerControl player) : base(RoleInfo, player)
    {
        ErasureCooldown = OptErasureCooldown.GetFloat();
        StealButton = OptStealButton.GetBool();
        CanSeeTrash = OptCanSeeTrash.GetBool();
    }

    public static List<byte> TrashPlayers = new();
    public static bool IsSpecialMeeting = false;

    static OptionItem OptErasureCooldown;
    static float ErasureCooldown;
    public static OptionItem OptStealButton;
    public static bool StealButton;
    public static OptionItem OptCanSeeTrash;
    public static bool CanSeeTrash;

    enum OptionName
    {
        MonikaErasureCooldown,
        MonikaStealButton,
        MonikaCanSeeTrash
    }

    private static void SetupOptionItem()
    {
        OptErasureCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.MonikaErasureCooldown, new(0f, 60f, 0.5f), 25f, false).SetValueFormat(OptionFormat.Seconds);
        OptStealButton = BooleanOptionItem.Create(RoleInfo, 11, OptionName.MonikaStealButton, true, false);
        OptCanSeeTrash = BooleanOptionItem.Create(RoleInfo, 12, OptionName.MonikaCanSeeTrash, true, false);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        TrashPlayers.Clear();
        IsSpecialMeeting = false;
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(false);
    }

    public float CalculateKillCooldown() => ErasureCooldown;
    public bool CanUseKillButton() => Player.IsAlive();
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => false;

    public bool OverrideKillButtonText(out string text)
    {
        text = "抹消";
        return true;
    }

    public bool OverrideKillButton(out string text)
    {
        text = "Monika_Erasure";
        return true;
    }

    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        info.DoKill = false;
        var (killer, target) = info.AttemptTuple;

        if (TrashPlayers.Contains(target.PlayerId)) return;

        if (target.GetCustomRole() == CustomRoles.Monika)
        {
            CustomRoleManager.OnCheckMurder(Player, target, target, target, true, deathReason: CustomDeathReason.Kill);
        }
        else
        {
            TrashPlayers.Add(target.PlayerId);
            SendRpc(target.PlayerId);
            Utils.SendMessage($"<color=#ffb6c1>{target.Data.PlayerName}をゴミ箱に送りました</color>", Player.PlayerId);
            UtilsNotifyRoles.NotifyRoles(ForceLoop: true);
        }

        Player.ResetKillCooldown();
        Player.SetKillCooldown(ErasureCooldown);
    }

    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        foreach (var p in AllPlayerControls)
        {
            if (TrashPlayers.Contains(p.PlayerId) && p.Data.IsDead)
            {
                TrashPlayers.Remove(p.PlayerId);
                SendRpcClear(p.PlayerId);
                UtilsNotifyRoles.NotifyRoles(ForceLoop: true);
            }
        }

        var monikas = AllAlivePlayerControls.Where(p => p.GetCustomRole() == CustomRoles.Monika).ToList();
        var nonTrashSurvivors = AllAlivePlayerControls.Where(p => !TrashPlayers.Contains(p.PlayerId) && p.GetCustomRole() != CustomRoles.Monika).ToList();

        if (monikas.Count == 1 && nonTrashSurvivors.Count == 2 && !GameStates.IsMeeting && GameStates.IsInTask)
        {
            IsSpecialMeeting = true;
            player.CmdReportDeadBody(null);
            Utils.SendMessage("<color=#ffb6c1>特殊会議が発生しました。モニカは追加勝利させる生存者を投票で選んでください。</color>");
        }
    }

    public override void CheckWinner(GameOverReason reason)
    {
        base.CheckWinner(reason);
        if (!AmongUsClient.Instance.AmHost) return;

        var monikas = AllAlivePlayerControls.Where(p => p.GetCustomRole() == CustomRoles.Monika).ToList();
        var nonTrashSurvivors = AllAlivePlayerControls.Where(p => !TrashPlayers.Contains(p.PlayerId) && p.GetCustomRole() != CustomRoles.Monika).ToList();

        if (monikas.Count == 1 && nonTrashSurvivors.Count <= 1)
        {
            CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Monika);
            CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Monika);
            CustomWinnerHolder.NeutralWinnerIds.Add(monikas[0].PlayerId);

            foreach (var survivor in nonTrashSurvivors)
            {
                CustomWinnerHolder.WinnerIds.Add(survivor.PlayerId);
            }
        }
    }

    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (TrashPlayers.Contains(voter.PlayerId))
        {
            return false;
        }

        if (IsSpecialMeeting && Is(voter))
        {
            if (votedForId != byte.MaxValue && votedForId != voter.PlayerId)
            {
                var target = GetPlayerById(votedForId);
                if (target != null && !TrashPlayers.Contains(target.PlayerId))
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Monika);
                    CustomWinnerHolder.WinnerRoles.Add(CustomRoles.Monika);
                    CustomWinnerHolder.NeutralWinnerIds.Add(Player.PlayerId);
                    CustomWinnerHolder.WinnerIds.Add(votedForId);
                    GameManager.Instance.RpcEndGame(GameOverReason.ImpostorsByVote, false);
                }
            }
            return false;
        }

        return true;
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        byte type = reader.ReadByte();
        if (type == 0)
        {
            byte target = reader.ReadByte();
            if (!TrashPlayers.Contains(target)) TrashPlayers.Add(target);
        }
        else if (type == 1)
        {
            byte target = reader.ReadByte();
            if (TrashPlayers.Contains(target)) TrashPlayers.Remove(target);
        }
    }

    private void SendRpc(byte targetId)
    {
        using var sender = CreateSender();
        sender.Writer.Write((byte)0);
        sender.Writer.Write(targetId);
    }

    private void SendRpcClear(byte targetId)
    {
        using var sender = CreateSender();
        sender.Writer.Write((byte)1);
        sender.Writer.Write(targetId);
    }

    public override bool GetTemporaryName(ref string name, ref bool NoMarker, bool isForMeeting, PlayerControl seer, PlayerControl seen = null)
    {
        seen ??= seer;
        if (TrashPlayers.Contains(seen.PlayerId))
        {
            name = $"<color=#ffb6c1>[ゴミ箱]</color>\n{seen.Data.PlayerName}";
            return true;
        }
        return false;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CmdReportDeadBody))]
public static class MonikaEmergencyMeetingPatch
{
    public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] NetworkedPlayerInfo target)
    {
        if (target != null) return;

        if (__instance.GetCustomRole() == CustomRoles.Monika && Monika.StealButton)
        {
            var victims = AllAlivePlayerControls.Where(p => p.PlayerId != __instance.PlayerId && p.RemainingEmergencies > 0).ToList();
            if (victims.Count > 0)
            {
                var victim = victims[IRandom.Instance.Next(victims.Count)];
                victim.RemainingEmergencies--;
                __instance.RemainingEmergencies++;
            }
        }
    }
}

[HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
public static class MonikaChatPatch
{
    public static bool Prefix(ChatController __instance, PlayerControl sourcePlayer, string chatText)
    {
        if (sourcePlayer == null || PlayerControl.LocalPlayer == null) return true;

        bool localIsTrash = Monika.TrashPlayers.Contains(PlayerControl.LocalPlayer.PlayerId);
        bool sourceIsTrash = Monika.TrashPlayers.Contains(sourcePlayer.PlayerId);
        bool localIsDead = PlayerControl.LocalPlayer.Data.IsDead;

        if (localIsDead) return true;

        if (!localIsTrash && sourceIsTrash) return false;

        return true;
    }
}

[HarmonyPatch(typeof(CustomRoleManager), nameof(CustomRoleManager.OnCheckMurder))]
public static class MonikaTrashTargetPatch
{
    public static bool Prefix(PlayerControl killer, PlayerControl target)
    {
        if (Monika.TrashPlayers.Contains(killer.PlayerId) && target.GetCustomRole() == CustomRoles.Monika)
        {
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
public static class MonikaTrashVisionPatch
{
    public static void Postfix(PlayerControl __instance)
    {
        if (PlayerControl.LocalPlayer == null) return;

        bool isMonika = PlayerControl.LocalPlayer.GetCustomRole() == CustomRoles.Monika;
        bool isTrash = Monika.TrashPlayers.Contains(__instance.PlayerId);

        if (isMonika && isTrash)
        {
            if (!Monika.CanSeeTrash)
            {
                __instance.Visible = false;
            }
        }
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
public static class MonikaMeetingHudPatch
{
    public static void Postfix(MeetingHud __instance)
    {
        foreach (var state in __instance.playerStates)
        {
            if (Monika.TrashPlayers.Contains(state.TargetPlayerId))
            {
                state.NameText.color = new Color32(255, 182, 193, 255);
                state.Overlay.gameObject.SetActive(true);
            }
        }
    }
}