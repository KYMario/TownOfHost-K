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
            SetupOptionItem, // ★ nullからSetupOptionItemに変更
            "poisoned_bak",
            "#FF4A5A",
            (5, 3)
        );

    private bool _firstMeeting = true;

    public PlayerControl PoisonedPlayer = null;
    public static List<PoisonedBakery> Bakeries = new();

    public PoisonedBakery(PlayerControl player) : base(RoleInfo, player) { }

    private static void SetupOptionItem()
    {
        HideRoleOptions(CustomRoles.PoisonedBakery);
    }

    public static void HideRoleOptions(CustomRoles role)
    {
        if (Options.CustomRoleSpawnChances != null &&
            Options.CustomRoleSpawnChances.TryGetValue(role, out var spawnOption))
        {
            spawnOption.SetHidden(true);
        }

        if (Options.CustomRoleCounts != null &&
            Options.CustomRoleCounts.TryGetValue(role, out var countOption))
        {
            countOption.SetHidden(true);
        }
    }

    public override void Add()
    {
        base.Add();
        Bakeries.Add(this);
        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        Bakeries.Remove(this);
        CustomRoleManager.MarkOthers.Remove(GetMarkOthers);
    }

    public override void CheckWinner(GameOverReason reason)
    {
        base.CheckWinner(reason);

        if (!AmongUsClient.Instance.AmHost) return;

        if (IsSpecialWinner())
        {
            Debug.Log("Special winner condition met. Poisoned Bakery cannot override.");
            return;
        }

        if (!Player.IsAlive()) return;

        _ = HandlePoisonedBakeryWin(ref reason);
    }

    private bool IsSpecialWinner()
    {
        var specialWinners = new HashSet<CustomWinner>
        {
            CustomWinner.Arsonist, CustomWinner.Workaholic, CustomWinner.Vulture,
            CustomWinner.Terrorist, CustomWinner.Chef, CustomWinner.Jester,
            CustomWinner.Executioner, CustomWinner.MassMedia
        };
        return specialWinners.Contains(CustomWinnerHolder.WinnerTeam);
    }

    public override void OnStartMeeting()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        if (_firstMeeting)
        {
            Utils.SendMessage("<color=#a83232><size=120%>パン屋が毒入りパンを配るようになりました。</size></color>", Player.PlayerId);
            _firstMeeting = false;
        }
    }

    public override string MeetingAddMessage()
    {
        if (!Player.IsAlive() || PoisonedPlayer == null) return "";
        return $"<color=#a83232>毒入りパンを配布されたプレイヤー: {UtilsName.GetPlayerColor(PoisonedPlayer.PlayerId)}§</color>";
    }

    public override void AfterMeetingTasks()
    {
        base.AfterMeetingTasks();

        if (!AmongUsClient.Instance.AmHost) return;

        // 前回の会議終了時に配った毒の処理
        if (Player.IsAlive() && PoisonedPlayer != null && PoisonedPlayer.IsAlive())
        {
            PoisonedPlayer.SetRealKiller(Player);
            MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.Poisoned, PoisonedPlayer.PlayerId);
        }

        // 自分が追放された場合などは、次のターゲットを選ばずに終了
        if (!Player.IsAlive())
        {
            PoisonedPlayer = null;
            SendRPC(false, byte.MaxValue);
            return;
        }

        // 次のターンのターゲットを決める
        var targetList = PlayerCatch.AllAlivePlayerControls
            .Where(p => p.PlayerId != Player.PlayerId)
            .Where(p => !Main.AfterMeetingDeathPlayers.ContainsKey(p.PlayerId))
            .ToList();

        if (targetList.Any())
        {
            var rand = IRandom.Instance;
            var targetPlayer = targetList[rand.Next(targetList.Count)];

            PoisonedPlayer = targetPlayer;
            Utils.SendMessage($"<color=#a83232><size=120%>{targetPlayer.GetRealName()} に毒入りパンを配布しました。</size></color>", Player.PlayerId);

            SendRPC(true, targetPlayer.PlayerId);
        }
        else
        {
            PoisonedPlayer = null;
            SendRPC(false, byte.MaxValue);
        }
    }

    public override void OnMurderPlayerAsTarget(MurderInfo info)
    {
        if (info.IsSuicide) return;

        PoisonedPlayer = null;
        if (AmongUsClient.Instance.AmHost) SendRPC(false, byte.MaxValue);
    }

    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;

        if (!Bakeries.Any(b => b.Player.IsAlive())) return "";

        if (Bakeries.Any(b => b.PoisonedPlayer != null && b.PoisonedPlayer.PlayerId == seen.PlayerId))
        {
            var color = new Color32(168, 50, 50, 255);
            return Utils.ColorString(color, "§");
        }
        return "";
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
        var targetId = reader.ReadByte();

        if (doPoison && targetId != byte.MaxValue)
        {
            PoisonedPlayer = PlayerCatch.GetPlayerById(targetId);
        }
        else
        {
            PoisonedPlayer = null;
        }
    }

    public static bool HandlePoisonedBakeryWin(ref GameOverReason reason)
    {
        var specialWinners = new HashSet<CustomWinner>
        {
            CustomWinner.Arsonist, CustomWinner.Workaholic, CustomWinner.Vulture,
            CustomWinner.Terrorist, CustomWinner.Chef, CustomWinner.Jester,
            CustomWinner.Executioner, CustomWinner.MassMedia
        };

        if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default && !specialWinners.Contains(CustomWinnerHolder.WinnerTeam))
        {
            var bakeryAlive = PlayerCatch.AllAlivePlayerControls.Any(pc => pc.GetCustomRole() == CustomRoles.PoisonedBakery);
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