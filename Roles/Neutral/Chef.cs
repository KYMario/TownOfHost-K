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
            34100,
            SetUpOptionItem,
            "ch",
            "#c79b2c",
            true,
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
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
    }

    public bool CanKill { get; private set; } = false;
    public List<byte> ChefTarget;
    public static void SetUpOptionItem()
    {
        Options.OverrideKilldistance.Create(RoleInfo, 10);
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
            PlayerCatch.AllPlayerControls.DoIf(p => SuddenDeathMode.IsOnajiteam(p.PlayerId, Player.PlayerId),
            p => ChefTarget.Add(p.PlayerId));
        }
    }
    public bool OverrideKillButtonText(out string text)
    {
        text = GetString("ChefButtonText");
        return true;
    }
    public float CalculateKillCooldown() => 0.1f;
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
            var c = GetCtargetCount();
            return Player.IsAlive() && c.Item1 == c.Item2 ? "<color=#dddd00>★</color>" : "";
        }
        else
        if (ChefTarget.Contains(seen.PlayerId))
            return Utils.ColorString(RoleInfo.RoleColor, "▲");
        else return "";
    }
    public override string GetProgressText(bool comms = false, bool gamelog = false)
    {
        var c = GetCtargetCount();
        return Utils.ColorString(RoleInfo.RoleColor.ShadeColor(0.25f), $"({c.Item1}/{c.Item2})");
    }
    public (int, int) GetCtargetCount()
    {
        int c = 0, all = 0;
        foreach (var pc in PlayerCatch.AllAlivePlayerControls)
        {
            if (pc.PlayerId == Player.PlayerId) continue;

            all++;
            if (ChefTarget.Contains(pc.PlayerId))
                c++;
        }
        return (c, all);
    }
    public bool CheckWin(ref CustomRoles winnerRole)
    {
        if (addwincheck) return false;
        var c = GetCtargetCount();
        return Player.IsAlive() && c.Item1 == c.Item2;
    }
    public override void OnExileWrapUp(NetworkedPlayerInfo exiled, ref bool DecidedWinner)
    {
        if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;
        if (!AmongUsClient.Instance.AmHost || Player.PlayerId != exiled.PlayerId) return;
        var c = GetCtargetCount();
        if (c.Item1 != c.Item2) return;

        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Chef);
        CustomWinnerHolder.WinnerIds.Add(exiled.PlayerId);
        DecidedWinner = true;
        addwincheck = true;
    }
    public bool OverrideKillButton(out string text)
    {
        text = "Chef_Kill";
        return true;
    }
}