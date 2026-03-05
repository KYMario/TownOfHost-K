using AmongUs.GameOptions;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Crewmate;

public sealed class Seer : RoleBase, IKillFlashSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Seer),
            player => new Seer(player),
            CustomRoles.Seer,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            11000,
            SetupOptionItem,
            "se",
            "#61b26c",
            (6, 2),
            from: From.TheOtherRoles
        );
    public Seer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        ActiveComms = OptionActiveComms.GetBool();
        DelayMode = OptionDelay.GetBool();
        Maxdelay = OptionMaxdelay.GetFloat();
        MinDelay = OptionMindelay.GetFloat();

        Receivedcount = 0;
    }
    private static bool ActiveComms;
    private static OptionItem OptionActiveComms;
    static OptionItem OptionDelay; static bool DelayMode;
    static OptionItem OptionMindelay; static float MinDelay;
    static OptionItem OptionMaxdelay; static float Maxdelay;
    int Receivedcount;

    enum OptionName
    {
        SeerDelayMode,
        SeerMindelay,
        SeerMaxdelay
    }
    private static void SetupOptionItem()
    {
        OptionActiveComms = BooleanOptionItem.Create(RoleInfo, 10, GeneralOption.CanUseActiveComms, true, false);
        OptionDelay = BooleanOptionItem.Create(RoleInfo, 11, OptionName.SeerDelayMode, false, false);
        OptionMindelay = FloatOptionItem.Create(RoleInfo, 12, OptionName.SeerMindelay, new(0, 60, 0.5f), 3f, false, OptionDelay).SetValueFormat(OptionFormat.Seconds);
        OptionMaxdelay = FloatOptionItem.Create(RoleInfo, 13, OptionName.SeerMaxdelay, new(0, 60, 0.5f), 3f, false, OptionDelay).SetValueFormat(OptionFormat.Seconds);
    }
    public bool? CheckKillFlash(MurderInfo info) // IKillFlashSeeable
    {
        var canseekillflash = !Utils.IsActive(SystemTypes.Comms) || ActiveComms;

        if (DelayMode && canseekillflash)
        {
            var tien = 0f;
            //小数対応
            if (Maxdelay > 0)
            {
                int ti = IRandom.Instance.Next(0, (int)Maxdelay * 10);
                tien = ti * 0.1f;
                Logger.Info($"{Player?.Data?.GetLogPlayerName()} => {tien}sの追加遅延発生!!", "Seer");
            }
            _ = new LateTask(() =>
            {
                if (GameStates.CalledMeeting || !Player.IsAlive())
                {
                    Logger.Info($"{info?.AppearanceTarget?.Data?.GetLogPlayerName() ?? "???"}のフラッシュを受け取ろうとしたけどなんかし防いだぜ", "seer");
                    return;
                }
                Receivedcount++;
                Player.KillFlash();
            }, tien + MinDelay, "SeerDelayKillFlash", null);
            return null;
        }
        return canseekillflash;
    }
    public override void CheckWinner(GameOverReason reason)
    {
        Achievements.RpcCompleteAchievement(Player.PlayerId, 1, achievements[0], Receivedcount);
        Achievements.RpcCompleteAchievement(Player.PlayerId, 1, achievements[1], Receivedcount);
        Achievements.RpcCompleteAchievement(Player.PlayerId, 1, achievements[2], Receivedcount);
    }
    public static System.Collections.Generic.Dictionary<int, Achievement> achievements = new();
    [Attributes.PluginModuleInitializer]
    public static void Load()
    {
        var n1 = new Achievement(RoleInfo, 0, 5, 0, 0);
        var l1 = new Achievement(RoleInfo, 1, 15, 0, 1);
        var sp1 = new Achievement(RoleInfo, 2, 50, 0, 2, true);
        achievements.Add(0, n1);
        achievements.Add(1, l1);
        achievements.Add(2, sp1);
    }
}