using System.Collections.Generic;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Neutral;

public sealed class Chef : RoleBase, IKiller, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Chef),
            player => new Chef(player),
            CustomRoles.Chef,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            13900,
            SetUpOptionItem,
            "ch",
            "#c79b2c",
            (3, 1),
            true,
            introSound: () => GetIntroSound(RoleTypes.Crewmate),
            from: From.TownOfHost_K
        );
    public Chef(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        ChefTarget = new(GameData.Instance.PlayerCount);
        addwincheck = false;

        CanSeeNowAlivePlayerCount = OptionCanSeeNowAlivePlayer.GetBool();
        TurnStartCooldown = OptionTurnStartCooldown.GetFloat();
    }

    public bool CanKill { get; private set; } = false;
    public List<byte> ChefTarget;
    static OptionItem OptionCanSeeNowAlivePlayer; static bool CanSeeNowAlivePlayerCount;
    static OptionItem OptionTurnStartCooldown; static float TurnStartCooldown;
    public static void SetUpOptionItem()
    {
        SoloWinOption.Create(RoleInfo, 9, defo: 1);
        OptionCanSeeNowAlivePlayer = BooleanOptionItem.Create(RoleInfo, 11, "ArsonistCanSeeAllplayer", false, false);
        OptionTurnStartCooldown = FloatOptionItem.Create(RoleInfo, 12, "ChefTurnStartCooldown", new(0, 180, 0.5f), 10, false).SetValueFormat(OptionFormat.Seconds);
        OverrideKilldistance.Create(RoleInfo, 10);
    }
    bool addwincheck;
    public override bool NotifyRolesCheckOtherName => true;
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => false;
    public bool CanUseKillButton() => true;
    public override void Add()
    {
        if (SuddenDeathMode.NowSuddenDeathTemeMode)
        {
            PlayerCatch.AllPlayerControls.DoIf(pc => SuddenDeathMode.IsSameteam(pc.PlayerId, Player.PlayerId),
            pc => ChefTarget.Add(pc.PlayerId));
        }
    }
    public bool OverrideKillButtonText(out string text)
    {
        text = GetString("ChefButtonText");
        return true;
    }
    public float CalculateKillCooldown() => TurnStartCooldown;
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(false);
    }
    private void SendRPC(byte targetid)
    {
        using var sender = CreateSender();
        sender.Writer.Write(targetid);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        ChefTarget.Add(reader.ReadByte());
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (ChefTarget.Contains(target.PlayerId))
        {
            info.DoKill = false;
            return;
        }
        killer.SetKillCooldown(1);
        ChefTarget.Add(target.PlayerId);
        SendRPC(target.PlayerId);
        UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player);
        Logger.Info($"Player: {Player.name},Target: {target.name}", "Chef");
        info.DoKill = false;
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        if (seer == seen)
        {
            var chefdata = GetCheftargetCount();
            return Player.IsAlive() && chefdata.Item1 == chefdata.Item2 ? "<color=#dddd00>★</color>" : "";
        }
        else
            if (ChefTarget.Contains(seen.PlayerId))
                return Utils.ColorString(RoleInfo.RoleColor, "▲");
            else return "";
    }
    public override string GetProgressText(bool comms = false, bool gamelog = false)
    {
        var chefdata = GetCheftargetCount();
        var Denominator = "?";
        if (CanSeeNowAlivePlayerCount || GameStates.CalledMeeting) Denominator = $"{chefdata.Item2}";
        return Utils.ColorString(RoleInfo.RoleColor.ShadeColor(0.25f), $"({chefdata.Item1}/{Denominator})");
    }
    public (int givencount, int allplayercount) GetCheftargetCount()
    {
        int given = 0, all = 0;
        foreach (var pc in PlayerCatch.AllAlivePlayerControls)
        {
            if (pc.PlayerId == Player.PlayerId) continue;

            all++;
            if (ChefTarget.Contains(pc.PlayerId))
                given++;
        }
        return (given, all);
    }
    public bool CheckWin(ref CustomRoles winnerRole)
    {
        if (addwincheck) return false;
        var chefdata = GetCheftargetCount();
        if (Player.IsAlive() && chefdata.Item1 == chefdata.Item2)
        {
            Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[0]);
            return true;
        }
        return false;
    }
    public override void OnExileWrapUp(NetworkedPlayerInfo exiled, ref bool DecidedWinner)
    {
        if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;
        if (!AmongUsClient.Instance.AmHost || Player.PlayerId != exiled.PlayerId) return;
        var chefdata = GetCheftargetCount();
        if (chefdata.Item1 != chefdata.Item2) return;

        addwincheck = CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Chef, Player.PlayerId);
        if (addwincheck)
        {
            CustomWinnerHolder.NeutralWinnerIds.Add(Player.PlayerId);
        }
        Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[1]);
        if (Main.NormalOptions.MapId is 5) Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[2]);
        DecidedWinner = true;
    }
    public bool OverrideKillButton(out string text)
    {
        text = "Chef_Kill";
        return true;
    }
    public static Dictionary<int, Achievement> achievements = new();
    [Attributes.PluginModuleInitializer]
    public static void Load()
    {
        var n1 = new Achievement(RoleInfo, 0, 1, 0, 0);
        var l1 = new Achievement(RoleInfo, 1, 1, 0, 1);
        var sp1 = new Achievement(RoleInfo, 2, 1, 0, 2, true);
        achievements.Add(0, n1);
        achievements.Add(1, l1);
        achievements.Add(2, sp1);
    }
}