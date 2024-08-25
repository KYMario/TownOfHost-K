using AmongUs.GameOptions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Hazel;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

using static TownOfHost.Translator;

namespace TownOfHost.Roles.Impostor;
public sealed class AntiReporter : RoleBase, IImpostor, IUseTheShButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(AntiReporter),
            player => new AntiReporter(player),
            CustomRoles.AntiReporter,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            60077,
            SetupOptionItem,
            "anr"
        );
    public AntiReporter(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        megaphone = false;
        mg.Clear();
        Cooldown = OptionColldown.GetFloat();
        Use = OptionMax.GetInt();
        AntiReporterResetMeeting = OptionAntiReporterResetMeeting.GetBool();
        AntiReporterResetse = OptionAntiReporterResetse.GetFloat();
    }
    bool megaphone;
    Dictionary<byte, float> mg = new(14);
    static OptionItem OptionColldown;
    static OptionItem OptionMax;
    static OptionItem OptionAntiReporterResetMeeting;
    static OptionItem OptionAntiReporterResetse;
    static OptionItem OptionOC;
    enum OptionName
    {
        Cooldown,
        AntiReporterMaximum,
        AntiReporterResetMeeting,
        AntiReporterResetse,
        UOcShButton
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
        OptionAntiReporterResetse = FloatOptionItem.Create(RoleInfo, 13, OptionName.AntiReporterResetse, new(0f, 999f, 1f), 20f, false, infinity: true)
            .SetValueFormat(OptionFormat.Seconds);
        OptionOC = BooleanOptionItem.Create(RoleInfo, 14, OptionName.UOcShButton, false, false);
    }
    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(Use);
        sender.Writer.Write(megaphone);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        Use = reader.ReadInt32();
        megaphone = reader.ReadBoolean();
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        info.CanKill = true;
        if (megaphone == false || mg.ContainsKey(target.PlayerId)) return;
        mg.Add(target.PlayerId, 0f);
        Use--;
        killer.RpcProtectedMurderPlayer(target);
        Logger.Info($"{target.name}のメガホンを間違えて壊しちゃった!! ﾃﾍ", "AntiReporter");
        megaphone = false;
        SendRPC();
        Utils.NotifyRoles();
        info.CanKill = false;
    }
    public override void OnShapeshift(PlayerControl target)
    {
        var shapeshifting = !Is(target);
        if (Use < 1 || !shapeshifting || UseOCButton) return;
        Logger.Info(megaphone ? "やっぱ破壊するのやめるね！" : $"破壊準備ok", "AntiReporter");
        megaphone = !megaphone;
        Utils.NotifyRoles();
    }
    public void OnClick()
    {
        var target = Player.GetKillTarget();
        if (target == null) return;
        if (!CanUseAbilityButton() || mg.ContainsKey(target.PlayerId)) return;
        mg.Add(target.PlayerId, 0f);
        Use--;
        Player.RpcProtectedMurderPlayer(target);
        Logger.Info($"{target.name}のメガホンワンクリックだから間違えて壊しちゃった☆ ﾃﾍｯ", "AntiReporter");
        SendRPC();
        Utils.NotifyRoles();

    }
    public bool UseOCButton => OptionOC.GetBool();
    public override string GetProgressText(bool comms = false) => megaphone ? "<color=red>◆" : "" + Utils.ColorString(Use > 0 ? Color.red : Color.gray, $"({Use})");
    public override bool CancelReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        Logger.Info("!!!", "mg");
        return mg.ContainsKey(reporter.PlayerId);
    }
    public bool OverrideKillButtonText(out string text)
    {
        text = AntiReporterResetse == 0 ? GetString("DestroyButtonText") : GetString("DisableButtonText");
        if (!megaphone) return false;
        if (OptionOC.GetBool()) return false;
        return true;
    }
    public override string GetAbilityButtonText()
    {
        if (!OptionOC.GetBool()) return "";
        return AntiReporterResetse == 0 ? GetString("DestroyButtonText") : GetString("DisableButtonText"); ;
    }

    public override void OnStartMeeting()
    {
        if (AntiReporterResetMeeting == true) mg.Clear();
    }
    public override bool CanUseAbilityButton() => Use > 0;
    public override void OnFixedUpdate(PlayerControl _)
    {
        if (!AmongUsClient.Instance.AmHost || AntiReporterResetse == 0) return;

        foreach (var (targetId, timer) in mg.ToArray())
        {
            if (timer >= AntiReporterResetse)
            {
                mg.Remove(targetId);
            }
            else
            {
                mg[targetId] += Time.fixedDeltaTime;
            }
        }
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterCooldown = Cooldown;
        AURoleOptions.ShapeshifterLeaveSkin = false;
        AURoleOptions.ShapeshifterDuration = 1;
    }
    public override bool OverrideAbilityButton(out string text)
    {
        text = "AntiReporter_Ability";
        return true;
    }
}