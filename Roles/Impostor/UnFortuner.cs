using System.Collections.Generic;
using System.Linq;
using Hazel;

using AmongUs.GameOptions;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.Crewmate;
using TownOfHost.Roles.AddOns.Common;
using TownOfHost.Roles.AddOns.Neutral;
using System;

namespace TownOfHost.Roles.Impostor;

public sealed class UnFortuner : RoleBase, IImpostor, IUsePhantomButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(UnFortuner),
            player => new UnFortuner(player),
            CustomRoles.UnFortuner,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Impostor,
            24500,
            SetupOptionItem,
            "Uf",
            OptionSort: (6, 11)
        );
    public UnFortuner(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCooldown = OptionKillCoolDown.GetFloat();
        UseCount = OptionUseCount.GetInt();
        cooldown = OptionCooldown.GetFloat();
        IsGiveOne = OptionGiveOne.GetBool();

        GiveAddons = OptionGiveAddons.GetNowRoleValue();
        giveplayerid = new();
    }
    static float KillCooldown; static OptionItem OptionKillCoolDown;
    List<byte> giveplayerid = new();
    int UseCount; static OptionItem OptionUseCount;
    float cooldown; static OptionItem OptionCooldown;
    static bool IsGiveOne; static OptionItem OptionGiveOne;
    static List<CustomRoles> GiveAddons = new(); static AssignOptionItem OptionGiveAddons;
    enum OptionName
    {
        FortunerAddGiveAddon,
        FortunerGiveOne
    }
    static void SetupOptionItem()
    {
        OptionKillCoolDown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, OptionBaseCoolTime, 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionUseCount = IntegerOptionItem.Create(RoleInfo, 11, GeneralOption.OptionCount, new(1, 30, 1), 1, false);
        OptionCooldown = FloatOptionItem.Create(RoleInfo, 12, GeneralOption.Cooldown, OptionBaseCoolTime, 30, false).SetValueFormat(OptionFormat.Seconds);
        OptionGiveOne = BooleanOptionItem.Create(RoleInfo, 13, OptionName.FortunerGiveOne, true, false);
        OptionGiveAddons = AssignOptionItem.Create(RoleInfo, 14, OptionName.FortunerAddGiveAddon, 0, false, null, false, false, false, false, true, Fortuner.RemoveAddon);
    }
    float IKiller.CalculateKillCooldown() => KillCooldown;
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.PhantomCooldown = cooldown;
    }
    public override string GetProgressText(bool comms = false, bool GameLog = false)
        => $"<{(UseCount > 0 ? RoleInfo.RoleColorCode : "#cccccc")}> ({UseCount})</color>";
    bool IUsePhantomButton.UseOneclickButton => 0 < UseCount;
    bool IUsePhantomButton.IsPhantomRole => 0 < UseCount;
    public override bool CanUseAbilityButton() => 0 < UseCount;
    void IUsePhantomButton.OnClick(ref bool AdjustKillCooldown, ref bool? ResetCooldown)
    {
        if (UseCount <= 0)
        {
            AdjustKillCooldown = true;
            ResetCooldown = null;
            return;
        }
        var target = Player.GetKillTarget(true);
        if (target is null)
        {
            AdjustKillCooldown = true;
            ResetCooldown = false;
            return;
        }
        AdjustKillCooldown = false;
        ResetCooldown = true;
        giveplayerid.Add(target.PlayerId);
        Logger.Info($"{target}-{UseCount}", "UnFortuner");
        UseCount--;
        UtilsNotifyRoles.NotifyRoles();
        SendRpc();

        /* キルク保持しつつ、ターゲットに守護天使ばりあを */
        float TurnTimer = 0;
        IUsePhantomButton.IPPlayerKillCooldown.TryGetValue(Player.PlayerId, out TurnTimer);
        Main.AllPlayerKillCooldown.TryGetValue(Player.PlayerId, out var killcool);
        if (MeetingStates.FirstMeeting && !Options.FixFirstKillCooldown.GetBool() && killcool > 10 &&
        (PlayerState.GetByPlayerId(Player.PlayerId)?.Is10secKillButton == true))
            killcool = 10;
        float cooldown = killcool - TurnTimer;
        if (cooldown <= 1) cooldown = 0.005f;

        Player.SetKillCooldown(cooldown, target);
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
                    role = Fortuner.DefaltAddon[IRandom.Instance.Next(Fortuner.DefaltAddon.Count())];
                    player.RpcSetCustomRole(role);
                    Logger.Info($"Give({Player.PlayerId}): {player.PlayerId} + {role}", "UnFortuner");
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
                    Logger.Info($"Give({Player.PlayerId}): {player.PlayerId} + {role}", "UnFortuner");
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

                        Logger.Info($"Give({Player.PlayerId}): {player.PlayerId} + {addon}", "UnFortuner");
                    }
                }
            }
            if (role is not CustomRoles.NotAssigned || roletext != "")
            {
                if (roletext == "") roletext += UtilsRoleText.GetRoleColorAndtext(role);
                if (role.IsDebuffAddon()) Achievements.RpcCompleteAchievement(Player.PlayerId, 0, Fortuner.achievements[1]);
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
            Achievements.RpcCompleteAchievement(Player.PlayerId, 1, Fortuner.achievements[0], giveplayerid.Count);
            Achievements.RpcCompleteAchievement(Player.PlayerId, 1, Fortuner.achievements[2], giveplayerid.Count);
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
    public override string GetAbilityButtonText() => GetString("Fortuner_AbilityButton");
    public override bool OverrideAbilityButton(out string text)
    {
        text = "UnFortuner_Ability";
        return true;
    }
}
