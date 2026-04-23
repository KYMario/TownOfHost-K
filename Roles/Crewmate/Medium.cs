using System;
using System.Collections.Generic;
using AmongUs.GameOptions;
using UnityEngine;
using Hazel;
using HarmonyLib;

using TownOfHost.Roles.Core;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Crewmate;

public sealed class Medium : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Medium),
            player => new Medium(player),
            CustomRoles.Medium,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            69610,
            SetupOptionItem,
            "sp",
            "#66a6ff",
            (3, 5),
            introSound: () => GetIntroSound(RoleTypes.Scientist)
        );

    private static OptionItem OptionMaximum;
    private static OptionItem OptionShowDeathReason;
    private static OptionItem OptionRole;
    private static OptionItem OptionCanTaskcount;
    private static OptionItem Option1MeetingMaximum;
    private static OptionItem OptAwakening;

    private int usedCount;
    private int meetingUsedCount;
    private bool awakened;
    private static float cantaskcount;
    private static float onemeetingmaximum;

    private readonly Dictionary<byte, DivinationResult> divinationResults = new();

    private readonly struct DivinationResult
    {
        public CustomRoles Role { get; }
        public CustomDeathReason DeathReason { get; }

        public DivinationResult(CustomRoles role, CustomDeathReason deathReason)
        {
            Role = role;
            DeathReason = deathReason;
        }
    }

    private enum OptionName
    {
        TellMaximum,
        MediumShowDeathReason,
        TellRole,
    }

    public Medium(PlayerControl player)
        : base(RoleInfo, player)
    {
        usedCount = 0;
        meetingUsedCount = 0;
        awakened = !OptAwakening.GetBool() || OptionCanTaskcount.GetInt() < 1;
        cantaskcount = OptionCanTaskcount.GetFloat();
        onemeetingmaximum = Option1MeetingMaximum.GetFloat();
        divinationResults.Clear();
    }

    private static void SetupOptionItem()
    {
        OptionMaximum = IntegerOptionItem.Create(RoleInfo, 10, OptionName.TellMaximum, new(1, 99, 1), 3, false)
            .SetValueFormat(OptionFormat.Times);
        OptionShowDeathReason = BooleanOptionItem.Create(RoleInfo, 11, OptionName.MediumShowDeathReason, false, false);
        OptionRole = BooleanOptionItem.Create(RoleInfo, 12, OptionName.TellRole, true, false);
        OptionCanTaskcount = IntegerOptionItem.Create(RoleInfo, 13, GeneralOption.cantaskcount, new(0, 99, 1), 0, false);
        Option1MeetingMaximum = IntegerOptionItem.Create(RoleInfo, 14, GeneralOption.MeetingMaxTime, new(0, 99, 1), 0, false)
            .SetValueFormat(OptionFormat.Times).SetZeroNotation(OptionZeroNotation.Infinity);
        OptAwakening = BooleanOptionItem.Create(RoleInfo, 15, GeneralOption.AbilityAwakening, false, false);
    }

    private int RemainingCount => Math.Max(0, OptionMaximum.GetInt() - usedCount);

    private void SendRPC(byte targetId, CustomRoles role, CustomDeathReason deathReason)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        using var sender = CreateSender();
        sender.Writer.Write(usedCount);
        sender.Writer.Write(targetId);
        sender.Writer.WritePacked((int)role);
        sender.Writer.WritePacked((int)deathReason);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        usedCount = reader.ReadInt32();
        var targetId = reader.ReadByte();
        var role = (CustomRoles)reader.ReadPackedInt32();
        var deathReason = (CustomDeathReason)reader.ReadPackedInt32();
        divinationResults[targetId] = new DivinationResult(role, deathReason);
    }

    public override void OnStartMeeting() => meetingUsedCount = 0;

    public override CustomRoles Misidentify() => awakened ? CustomRoles.NotAssigned : CustomRoles.Crewmate;

    public override bool OnCompleteTask(uint taskid)
    {
        if (MyTaskState.HasCompletedEnoughCountOfTasks((int)cantaskcount))
        {
            if (!awakened)
                if (!Utils.RoleSendList.Contains(Player.PlayerId))
                    Utils.RoleSendList.Add(Player.PlayerId);
            awakened = true;
        }
        return true;
    }

    public override string GetProgressText(bool comms = false, bool gamelog = false)
    {
        var canUse = Player.IsAlive() && RemainingCount > 0 && awakened;
        return Utils.ColorString(canUse ? Color.cyan : Color.gray, $"({RemainingCount})");
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (!isForMeeting) return "";
        if (!Player.IsAlive()) return "";
        if (seer == null || seen == null) return "";
        if (seer.PlayerId != Player.PlayerId || seen.PlayerId != Player.PlayerId) return "";
        if (RemainingCount <= 0 || !awakened) return "";

        var hint = $"<color={RoleInfo.RoleColorCode}>/cmd sp ID で死亡者の役職を確認</color>";
        return isForHud ? hint : $"<size=40%>{hint}</size>";
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (!isForMeeting) return "";
        if (!Player.IsAlive()) return "";
        if (seer == null || seen == null) return "";
        if (seer.PlayerId != Player.PlayerId) return "";
        if (seen.PlayerId == seer.PlayerId || seen.IsAlive()) return "";
        return Utils.ColorString(Color.yellow, $" {seen.PlayerId}");
    }

    private static string GetTeamText(CustomRoleTypes roleType)
    {
        return roleType switch
        {
            CustomRoleTypes.Crewmate => "<#8cffff>クルー陣営</color>",
            CustomRoleTypes.Impostor or CustomRoleTypes.Madmate => "<#ff1919>インポスター陣営</color>",
            CustomRoleTypes.Neutral => "<#cccccc>ニュートラル陣営</color>",
            _ => "<#cccccc>ニュートラル陣営</color>",
        };
    }

    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;

        if (seer == null || seen == null) return "";
        if (seer.PlayerId != Player.PlayerId) return "";
        if (!divinationResults.TryGetValue(seen.PlayerId, out var result)) return "";

        var infoText = OptionRole.GetBool()
            ? $"<color={UtilsRoleText.GetRoleColorCode(result.Role)}>{GetString(result.Role.ToString())}</color>"
            : GetTeamText(result.Role.GetCustomRoleTypes());

        if (OptionShowDeathReason.GetBool())
        {
            var deathReason = Utils.GetDeathReason(result.DeathReason);
            return $"<size=65%><#ffffff>({deathReason})</color></size><size=80%>({infoText})</size>";
        }

        return $"\n<size=80%>({infoText})</size>";
    }

    private bool CanUseAbility(out string error)
    {
        if (!Player.IsAlive())
        {
            error = "死亡中は占えません。";
            return false;
        }
        if (!GameStates.IsMeeting)
        {
            error = "占いは会議中のみ使用できます。";
            return false;
        }
        if (!awakened)
        {
            error = "まだ能力が覚醒していません。";
            return false;
        }
        if (RemainingCount <= 0)
        {
            error = "残り占い回数がありません。";
            return false;
        }
        if (onemeetingmaximum > 0 && meetingUsedCount >= onemeetingmaximum)
        {
            error = "この会議での占い回数の上限に達しました。";
            return false;
        }
        error = "";
        return true;
    }

    private void UseAbility(byte targetId)
    {
        const string title = "<#66a6ff>霊媒めっせーじ</color>";

        if (!CanUseAbility(out var error))
        {
            Utils.SendMessage(error, Player.PlayerId, title);
            return;
        }

        var target = PlayerCatch.GetPlayerById(targetId);
        if (target == null || target.Data == null || target.Data.Disconnected)
        {
            Utils.SendMessage("そのIDのプレイヤーが見つかりません。", Player.PlayerId, title);
            return;
        }
        if (target.IsAlive())
        {
            Utils.SendMessage("生存者は霊媒できません。死亡者を指定してください。", Player.PlayerId, title);
            return;
        }

        usedCount++;
        meetingUsedCount++;

        var role = target.GetTellResults(Player);
        var deathReason = target.GetPlayerState()?.DeathReason ?? CustomDeathReason.etc;

        divinationResults[target.PlayerId] = new DivinationResult(role, deathReason);
        SendRPC(target.PlayerId, role, deathReason);

        string roleText;
        if (OptionRole.GetBool())
            roleText = $"<b>{Utils.ColorString(UtilsRoleText.GetRoleColor(role), GetString(role.ToString()))}</b>";
        else
            roleText = GetTeamText(role.GetCustomRoleTypes());

        string deathInfo = "";
        if (OptionShowDeathReason.GetBool())
        {
            var deathText = Utils.GetDeathReason(deathReason);
            deathInfo = $"\n死因：{deathText}";
        }

        var remaining = onemeetingmaximum > 0
            ? string.Format(GetString("RemainingOneMeetingCount"), Math.Min(onemeetingmaximum - meetingUsedCount, RemainingCount))
            : string.Format(GetString("RemainingCount"), RemainingCount);

        var text = $"{UtilsName.GetPlayerColor(target, true)}は{roleText}でした。{deathInfo}\n{remaining}";
        Utils.SendMessage(text, Player.PlayerId, title);
    }

    private static bool TryParseSpCommand(string msg, out byte targetId, out bool invalidFormat)
    {
        targetId = byte.MaxValue;
        invalidFormat = false;
        if (string.IsNullOrWhiteSpace(msg)) return false;
        var args = msg.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (args.Length < 2) return false;
        if (args[0] != "/cmd") return false;
        var command = args[1].StartsWith("/") ? args[1] : $"/{args[1]}";
        if (command != "/sp") return false;
        if (args.Length < 1.0 || !byte.TryParse(args[2], out targetId))
        {
            invalidFormat = true;
            return true;
        }
        return true;
    }

    [HarmonyPatch(typeof(GuessManager), nameof(GuessManager.GuesserMsg))]
    private static class GuessManagerGuesserMsgPatch
    {
        private static bool Prefix(PlayerControl pc, string msg, ref bool __result)
        {
            if (!TryParseSpCommand(msg, out var targetId, out var invalidFormat))
                return true;
            __result = true;
            if (!AmongUsClient.Instance.AmHost || !GameStates.IsInGame || pc == null)
                return false;
            if (pc.GetRoleClass() is not Medium medium)
            {
                Utils.SendMessage("/cmd sp は霊媒師専用です。", pc.PlayerId, "<#66a6ff>Medium</color>");
                return false;
            }
            if (invalidFormat)
            {
                Utils.SendMessage("使い方: /cmd sp (ID)", pc.PlayerId, "<#66a6ff>Medium</color>");
                return false;
            }
            medium.UseAbility(targetId);
            return false;
        }
    }
}