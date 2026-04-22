using System;
using System.Collections.Generic;
using System.Linq;

using AmongUs.GameOptions;
using Hazel;
using TownOfHost.Roles.AddOns.Common;
using TownOfHost.Roles.AddOns.Neutral;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Crewmate;

public sealed class Fortuner : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Fortuner),
            player => new Fortuner(player),
            CustomRoles.Fortuner,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            24100,
            SetUpOptionItem,
            "fo",
            "#34f098",
            (1, 3),
            true
        );
    public Fortuner(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.True
    )
    {
        Awakened = !OptionAwakening.GetBool() || OptionRemoveAwakenTask.GetInt() < 1;
        UseCount = OptionUseCount.GetInt();
        cooldown = OptionCooldown.GetFloat();
        RemoveAwakenTaskcount = OptionRemoveAwakenTask.GetInt();
        IsGiveOne = OptionGiveOne.GetBool();

        GiveAddons = OptionGiveAddons.GetNowRoleValue();
        giveplayerid = new();
    }
    List<byte> giveplayerid = new();
    int UseCount; static OptionItem OptionUseCount;
    float cooldown; static OptionItem OptionCooldown;
    bool Awakened; static OptionItem OptionAwakening;
    static int RemoveAwakenTaskcount; static OptionItem OptionRemoveAwakenTask;
    static bool IsGiveOne; static OptionItem OptionGiveOne;
    static List<CustomRoles> GiveAddons = new(); static AssignOptionItem OptionGiveAddons;
    enum OptionName
    {
        AwakeningTaskcount,
        FortunerAddGiveAddon,
        FortunerGiveOne
    }
    public static CustomRoles[] DefaltAddon =
    [
        CustomRoles.Autopsy,
        CustomRoles.Lighting,CustomRoles.Lighting,CustomRoles.Lighting,
        CustomRoles.Moon,CustomRoles.Moon,CustomRoles.Moon,
        CustomRoles.Guesser,
        CustomRoles.Tiebreaker,CustomRoles.Tiebreaker,
        CustomRoles.Opener,CustomRoles.Opener,CustomRoles.Opener,
        CustomRoles.Management,CustomRoles.Management,
        CustomRoles.Speeding,CustomRoles.Speeding,
        CustomRoles.MagicHand,CustomRoles.MagicHand,
        CustomRoles.Serial,CustomRoles.Serial,
        CustomRoles.Powerful, CustomRoles.Powerful,
        CustomRoles.PlusVote,
        CustomRoles.Seeing,
        CustomRoles.Sunglasses,
        CustomRoles.Elector,
        CustomRoles.Water
    ];
    public static CustomRoles[] RemoveAddon =
    [
        CustomRoles.Amnesia,
        CustomRoles.Workhorse,
        CustomRoles.LastImpostor,
        CustomRoles.LastNeutral,
        CustomRoles.Twins,
        CustomRoles.OneWolf,
        CustomRoles.Stack,
        CustomRoles.Connecting
    ];

    public static void SetUpOptionItem()
    {
        OptionUseCount = IntegerOptionItem.Create(RoleInfo, 10, GeneralOption.OptionCount, new(1, 30, 1), 1, false);
        OptionCooldown = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.Cooldown, OptionBaseCoolTime, 30, false).SetValueFormat(OptionFormat.Seconds);
        OptionAwakening = BooleanOptionItem.Create(RoleInfo, 17, GeneralOption.TaskAwakening, true, false);
        OptionRemoveAwakenTask = IntegerOptionItem.Create(RoleInfo, 18, OptionName.AwakeningTaskcount, new(0, 297, 1), 5, false, OptionAwakening);
        OptionGiveOne = BooleanOptionItem.Create(RoleInfo, 19, OptionName.FortunerGiveOne, true, false);
        OptionGiveAddons = AssignOptionItem.Create(RoleInfo, 20, OptionName.FortunerAddGiveAddon, 0, false, null, false, false, false, false, true, RemoveAddon);
        OverrideTasksData.Create(RoleInfo, 12);
    }
    public override RoleTypes? AfterMeetingRole => MyTaskState.IsTaskFinished ? RoleTypes.Impostor : RoleTypes.Crewmate;
    bool IKiller.CanUseSabotageButton() => false;
    bool IKiller.CanUseImpostorVentButton() => false;
    bool IKiller.IsKiller => false;
    bool IKiller.CanUseKillButton() => MyTaskState.IsTaskFinished && UseCount > 0;
    float IKiller.CalculateKillCooldown() => cooldown;
    void IKiller.OnCheckMurderAsKiller(MurderInfo info)
    {
        if (UseCount <= 0)
        {
            info.DoKill = false;
            return;
        }
        giveplayerid.Add(info.AppearanceTarget.PlayerId);
        Logger.Info($"{info.AppearanceTarget.PlayerId}-{UseCount}", "F<EGTfaAr>or<canoacw>tu<na!>n<ruanor1>er".RemoveHtmlTags());
        info.DoKill = false;
        UseCount--;
        Player.SetKillCooldown(target: info.AppearanceTarget);
        UtilsNotifyRoles.NotifyRoles();
        SendRpc();
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(false);
    }
    public override string GetProgressText(bool comms = false, bool GameLog = false)
        => MyTaskState.IsTaskFinished ? $"<{(UseCount > 0 ? RoleInfo.RoleColorCode : "#cccccc")}> ({UseCount})</color>" : "";
    public override CustomRoles Misidentify() => Awakened ? CustomRoles.NotAssigned : CustomRoles.Crewmate;
    public override bool OnCompleteTask(uint taskid)
    {
        if (MyTaskState.HasCompletedEnoughCountOfTasks(RemoveAwakenTaskcount) && !Awakened)
        {
            Awakened = true;
            UtilsNotifyRoles.NotifyRoles();
        }
        if (Player.IsAlive() && MyTaskState.IsTaskFinished)
        {
            Player.RpcSetRoleDesync(RoleTypes.Impostor, Player.GetClientId());
            _ = new LateTask(() =>
            {
                Player.SetKillCooldown();
                UtilsNotifyRoles.NotifyRoles();
            }, 0.2f);
        }
        return true;
    }

    public override void OnStartMeeting()
    {
        foreach (var id in giveplayerid)
        {
            var player = id.GetPlayerControl();
            var role = CustomRoles.NotAssigned;
            var roletext = "";
            if (player.IsAlive())
            {
                var giveadd = GiveAddons.Where(add => add is not CustomRoles.Amanojaku || player.Is(CustomRoleTypes.Crewmate) || player.Is(CustomRoleTypes.Neutral)).OrderBy(x => Guid.NewGuid()).ToList();
                if (giveadd.Count <= 0)
                {
                    role = DefaltAddon[IRandom.Instance.Next(DefaltAddon.Count())];
                    player.RpcSetCustomRole(role);
                    Logger.Info($"Give({Player.PlayerId}): {player.PlayerId} + {role}", "Fortuner");
                }
                else if (IsGiveOne)// いっこだけ!
                {
                    role = giveadd[IRandom.Instance.Next(giveadd.Count())];
                    player.RpcSetCustomRole(role);
                    if (role is CustomRoles.Guarding)
                    {
                        var state = PlayerCatch.GetPlayerState(player);
                        state.HaveGuard[1] += Guarding.HaveGuard;
                    }
                    if (role is CustomRoles.Amanojaku)
                        AssignAmanojaku(player);
                    Logger.Info($"Give({Player.PlayerId}): {player.PlayerId} + {role}", "Fortuner");
                }
                else//全部渡す!!
                {
                    foreach (var addon in GiveAddons)
                    {
                        if (addon is CustomRoles.Amanojaku && !player.Is(CustomRoleTypes.Crewmate) && !player.Is(CustomRoleTypes.Neutral)) continue;
                        roletext += UtilsRoleText.GetRoleColorAndtext(addon);
                        player.RpcSetCustomRole(addon);
                        role = addon;
                        if (addon is CustomRoles.Guarding)
                        {
                            var state = PlayerCatch.GetPlayerState(player);
                            state.HaveGuard[1] += Guarding.HaveGuard;
                        }
                        if (addon is CustomRoles.Amanojaku)
                            AssignAmanojaku(player);

                        Logger.Info($"Give({Player.PlayerId}): {player.PlayerId} + {addon}", "Fortuner");
                    }
                }
            }
            if (role is not CustomRoles.NotAssigned || roletext != "")
            {
                if (roletext == "") roletext += UtilsRoleText.GetRoleColorAndtext(role);
                if (role.IsDebuffAddon()) Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[1]);
                _ = new LateTask(() =>
                {
                    var send = string.Format(GetString("Fortune_Meg".RemoveHtmlTags()), roletext);
                    Utils.SendMessage(send, player.PlayerId);
                    MeetingHudPatch.StartPatch.meetingsends.Add((player.PlayerId, send, ""));
                }, 5.5f, "forsendmeg", null);
            }
        }
        if (giveplayerid.Count > 0)
        {
            Achievements.RpcCompleteAchievement(Player.PlayerId, 1, achievements[0], giveplayerid.Count);
            Achievements.RpcCompleteAchievement(Player.PlayerId, 1, achievements[2], giveplayerid.Count);
        }
        giveplayerid.Clear();

        void AssignAmanojaku(PlayerControl player)
        {
            Amanojaku.Add(player.PlayerId);
            UtilsGameLog.AddGameLog($"Amanojaku", string.Format(GetString("Log.Amanojaku"), UtilsName.GetPlayerColor(player)));
            UtilsGameLog.LastLogRole[player.PlayerId] = Utils.ColorString(UtilsRoleText.GetRoleColor(CustomRoles.Amanojaku), GetString("Amanojaku") + GetString($"{player.GetCustomRole()}"));
            if (Faction.OptionRole.TryGetValue(CustomRoles.Amanojaku, out var option) && PlayerCatch.AllPlayerControls.Any(pc => pc.Is(CustomRoles.Faction)))
            {
                if (option.GetBool())
                {
                    PlayerState.GetByPlayerId(player.PlayerId).SetSubRole(CustomRoles.Faction);
                    if (0 < UtilsGameLog.day) player.RpcSetCustomRole(CustomRoles.Faction);
                    Logger.Info($"役職設定:{player.Data.GetLogPlayerName()} + Faction", "Faction");
                }
            }
        }
    }
    void SendRpc()
    {
        using var sender = CreateSender();
        sender.Writer.Write(UseCount);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        UseCount = reader.ReadInt32();
    }
    bool IKiller.OverrideKillButtonText(out string text)
    {
        text = GetString("Fortuner_AbilityButton");
        return true;
    }
    bool IKiller.OverrideKillButton(out string text)
    {
        text = "Fortuner_Ability";
        return true;
    }
    public static Dictionary<int, Achievement> achievements = new();
    [Attributes.PluginModuleInitializer]
    public static void Load()
    {
        var n1 = new Achievement(RoleInfo, 0, 3, 0, 0);
        var n2 = new Achievement(RoleInfo, 1, 1, 0, 0);
        var l1 = new Achievement(RoleInfo, 2, 20, 0, 1);
        achievements.Add(0, n1);
        achievements.Add(1, n2);
        achievements.Add(2, l1);
    }
}