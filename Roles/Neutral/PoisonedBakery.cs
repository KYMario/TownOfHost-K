using System;
using System.Linq;
using System.Collections.Generic;
using TownOfHost.Roles.Core;
using TownOfHost.Modules;
using AmongUs.GameOptions;
using UnityEngine;
using Hazel;

namespace TownOfHost.Roles.Neutral;

public sealed class PoisonedBakery : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(PoisonedBakery),
            player => new PoisonedBakery(player),
            CustomRoles.PoisonedBakery,
            () => AmongUs.GameOptions.RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            73493,
            null,
            "poisoned_bak",
            "#FF4A5A",
            (5, 3)
        );

    private bool _firstMeeting = true;
    private static List<byte> PoisonedPlayers = new();

    public PoisonedBakery(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
    }

    public override void CheckWinner(GameOverReason reason)
    {
        base.CheckWinner(reason);

        if (!AmongUsClient.Instance.AmHost) return;

        if (IsSpecialWinner())
        {
            Debug.Log("Special winner condition met. Poisoned Bakery cannot override.");
            if (!Player.IsAlive())
            {
                PoisonedPlayers.Clear();
            }
            return;
        }

        if (!Player.IsAlive())
        {
            PoisonedPlayers.Clear();
            return;
        }

        _ = HandlePoisonedBakeryWin(ref reason);
    }

    private bool IsSpecialWinner()
    {
        var specialWinners = new HashSet<CustomWinner>
        {
            CustomWinner.Arsonist,
            CustomWinner.Workaholic,
            CustomWinner.Vulture,
            CustomWinner.Terrorist,
            CustomWinner.Chef,
            CustomWinner.Jester,
            CustomWinner.Executioner,
            CustomWinner.MassMedia
        };

        return specialWinners.Contains(CustomWinnerHolder.WinnerTeam);
    }

    private bool ShouldOverrideVictory()
    {
        return true;
    }

    public override void OnStartMeeting()
    {
        if (_firstMeeting)
        {
            Utils.SendMessage("<color=#a83232><size=120%>パン屋が毒入りパンを配るようになりました。</size></color>", Player.PlayerId);
            _firstMeeting = false;
        }

        var targetList = PlayerCatch.AllAlivePlayerControls
            .Where(p => p.PlayerId != Player.PlayerId)
            .Where(p => !PoisonedPlayers.Contains(p.PlayerId))
            .ToList();

        if (targetList.Any())
        {
            var rand = IRandom.Instance;
            var targetPlayer = targetList[rand.Next(targetList.Count)];

            PoisonedPlayers.Add(targetPlayer.PlayerId);
            Utils.SendMessage($"<color=#a83232><size=120%>{targetPlayer.GetRealName()} に毒入りパンを配布しました。</size></color>", Player.PlayerId);

            SendRPC(true, targetPlayer.PlayerId);
        }
    }

    public override string MeetingAddMessage()
    {
        if (!Player.IsAlive() || PoisonedPlayers.Count == 0) return "";

        var poisonedNames = PoisonedPlayers
            .Select(id => UtilsName.GetPlayerColor(id) + "§")
            .ToList();

        return $"<color=#a83232>毒入りパンを配布されたプレイヤー: {string.Join(", ", poisonedNames)}</color>";
    }

    public override void AfterMeetingTasks()
    {
        base.AfterMeetingTasks();

        if (Player == null)
        {
            Debug.LogError("Player is null!");
            return;
        }

        if (!AmongUsClient.Instance.AmHost)
        {
            PoisonedPlayers.Clear();
            return;
        }

        if (Player.IsAlive())
        {
            var poisonedIdList = new List<byte>();
            foreach (var poisonedPlayerId in PoisonedPlayers)
            {
                var poisonedPlayer = PlayerCatch.GetPlayerById(poisonedPlayerId);
                if (poisonedPlayer == null)
                {
                    Debug.LogWarning($"Player with ID {poisonedPlayerId} not found!");
                    continue;
                }

                if (poisonedPlayer.IsAlive())
                {
                    poisonedPlayer.SetRealKiller(Player);
                    poisonedIdList.Add(poisonedPlayer.PlayerId);
                }
            }

            if (poisonedIdList.Count > 0)
            {
                MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.Poisoned, poisonedIdList.ToArray());
            }
            else
            {
                Debug.LogWarning("No poisoned players to add to MeetingHudPatch.");
            }
        }

        PoisonedPlayers.Clear();
    }

    private void SetCustomDeathReason(PlayerControl player, CustomDeathReason reason)
    {
        if (player == null) return;

        RPC.SendDeathReason(player.PlayerId, reason);
    }

    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (PoisonedPlayers.Contains(seen.PlayerId))
        {
            var color = new Color32(168, 50, 50, 255);
            return Utils.ColorString(color, "§");
        }
        return "";
    }

    public override void Add()
    {
        base.Add();
        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        CustomRoleManager.MarkOthers.Remove(GetMarkOthers);
    }

    private void SendRPC(bool doPoison, byte target = 255)
    {
        using var sender = CreateSender();
        sender.Writer.Write(doPoison);
        sender.Writer.Write(target);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        var doPoison = reader.ReadBoolean();
        var poisonedId = reader.ReadByte();

        if (doPoison)
        {
            PoisonedPlayers.Add(poisonedId);

            foreach (var player in PlayerCatch.AllPlayerControls)
            {
                if (PoisonedPlayers.Contains(player.PlayerId))
                {
                    player.Data.PlayerName += "§";
                }
            }
        }
        else
        {
            PoisonedPlayers.Remove(poisonedId);

            var poisonedPlayer = PlayerCatch.GetPlayerById(poisonedId);
            if (poisonedPlayer != null)
            {
                poisonedPlayer.Data.PlayerName = poisonedPlayer.Data.PlayerName.Replace("§", "");
            }
        }
    }

    public static bool HandlePoisonedBakeryWin(ref GameOverReason reason)
    {
        var specialWinners = new HashSet<CustomWinner>
        {
            CustomWinner.Arsonist,
            CustomWinner.Workaholic,
            CustomWinner.Vulture,
            CustomWinner.Terrorist,
            CustomWinner.Chef,
            CustomWinner.Jester,
            CustomWinner.Executioner,
            CustomWinner.MassMedia
        };

        if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default && !specialWinners.Contains(CustomWinnerHolder.WinnerTeam))
        {
            var bakeryAlive = PlayerCatch.AllPlayerControls.Any(pc => pc.GetCustomRole() == CustomRoles.PoisonedBakery && pc.IsAlive());
            if (bakeryAlive)
            {
                Debug.Log("Overriding non-special winner to PoisonedBakery.");
                CustomWinnerHolder.ResetAndSetWinner(CustomWinner.PoisonedBakery);
                CustomWinnerHolder.WinnerRoles.Add(CustomRoles.PoisonedBakery);
                return true;
            }
        }

        return false;
    }
}