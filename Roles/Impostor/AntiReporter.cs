using AmongUs.GameOptions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Hazel;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;

public sealed class AntiReporter : RoleBase, IImpostor, IUsePhantomButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(AntiReporter),
            player => new AntiReporter(player),
            CustomRoles.AntiReporter,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Impostor,
            5500,
            SetupOptionItem,
            "anr",
            OptionSort: (6, 2),
            Desc: () => string.Format(GetString("AntiReporterDesc"), OptionAntiReporterResetse.GetString() is "∞" ? "" : OptionAntiReporterResetse.GetString(), OptionMax.GetInt())
        );
    public AntiReporter(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        ReportCrashTimers.Clear();
        Cooldown = OptionColldown.GetFloat();
        Use = OptionMax.GetInt();
        AntiReporterResetMeeting = OptionAntiReporterResetMeeting.GetBool();
        AntiReporterResetse = OptionAntiReporterResetse.GetFloat();
    }
    Dictionary<byte, float> ReportCrashTimers = new(14);
    static OptionItem OptionColldown;
    static OptionItem OptionMax;
    static OptionItem OptionAntiReporterResetMeeting;
    static OptionItem OptionAntiReporterResetse;
    enum OptionName
    {
        Cooldown,
        AntiReporterMaximum,
        AntiReporterResetMeeting,
        AntiReporterResetse
    }
    static float Cooldown;
    static int Use;
    static bool AntiReporterResetMeeting;
    static float AntiReporterResetse;
    private static void SetupOptionItem()
    {
        OptionColldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.Cooldown, new(1f, 1000f, 1f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionMax = IntegerOptionItem.Create(RoleInfo, 11, OptionName.AntiReporterMaximum, new(1, 1000, 1), 3, false)
            .SetValueFormat(OptionFormat.Times);
        OptionAntiReporterResetMeeting = BooleanOptionItem.Create(RoleInfo, 12, OptionName.AntiReporterResetMeeting, true, false);
        OptionAntiReporterResetse = IntegerOptionItem.Create(RoleInfo, 13, OptionName.AntiReporterResetse, new(0, 999, 1), 20, false)
            .SetValueFormat(OptionFormat.Seconds).SetZeroNotation(OptionZeroNotation.Infinity);
    }
    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(Use);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        Use = reader.ReadInt32();
    }
    public void OnClick(ref bool AdjustKillCooldown, ref bool? ResetCooldown)
    {
        AdjustKillCooldown = true;
        ResetCooldown = false;
        var target = Player.GetKillTarget(true);
        if (target == null) return;
        if (!CanUseAbilityButton() || ReportCrashTimers.ContainsKey(target.PlayerId)) return;
        ResetCooldown = true;
        ReportCrashTimers.Add(target.PlayerId, 0f);
        Use--;
        Player.RpcProtectedMurderPlayer(target);
        Logger.Info($"{target.name}のメガホンワンクリックだから間違えて壊しちゃった☆ ﾃﾍｯ", "AntiReporter");
        SendRPC();
        UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player);
    }
    public override string GetProgressText(bool comms = false, bool gamelog = false) => Utils.ColorString(Use > 0 ? Color.red : Color.gray, $"({Use})");
    public override bool CancelReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target, ref DontReportreson reportreson)
    {
        if (ReportCrashTimers.ContainsKey(reporter.PlayerId))
        {
            reportreson = DontReportreson.Impostor;
            return true;
        }
        return false;
    }
    public override string GetAbilityButtonText()
    {
        return AntiReporterResetse == 0 ? GetString("DestroyButtonText") : GetString("DisableButtonText");
    }

    public override void OnStartMeeting()
    {
        if (AntiReporterResetMeeting == true) ReportCrashTimers.Clear();
    }
    public override bool CanUseAbilityButton() => Use > 0;
    bool IUsePhantomButton.IsPhantomRole => Use > 0;
    public override void OnFixedUpdate(PlayerControl _)
    {
        if (!AmongUsClient.Instance.AmHost || AntiReporterResetse == 0) return;

        foreach (var (targetId, timer) in ReportCrashTimers.ToArray())
        {
            if (timer >= AntiReporterResetse)
            {
                ReportCrashTimers.Remove(targetId);
            }
            else
            {
                ReportCrashTimers[targetId] += Time.fixedDeltaTime;
            }
        }
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.PhantomCooldown = Use > 0 ? Cooldown : 200f;
    }
    public override bool OverrideAbilityButton(out string text)
    {
        text = "AntiReporter_Ability";
        return true;
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (seen.PlayerId != seer.PlayerId || isForMeeting || !(Use > 0) || !Player.IsAlive()) return "";

        if (isForHud) return GetString("PhantomButtonKilltargetLowertext");
        return $"<size=50%>{GetString("PhantomButtonKilltargetLowertext")}</size>";
    }
}