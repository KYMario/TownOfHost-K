using Hazel;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.Neutral;
using TownOfHost.Roles.Impostor;

namespace TownOfHost.Roles.Crewmate;

public sealed class WolfBoy : RoleBase, IKiller, ISchrodingerCatOwner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(WolfBoy),
            player => new WolfBoy(player),
            CustomRoles.WolfBoy,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Crewmate,
            9100,
            SetupOptionItem,
            "wb",
            "#727171",
            (2, 3),
            true,
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );
    public WolfBoy(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        ShotLimit = ShotLimitOpt.GetInt();
        CurrentKillCooldown = KillCooldown.GetFloat();
        HasImpV = ImpostorVision.GetBool();

        CanwinKillcount = optcanwinkillcount.GetInt();
        IsCountCrew = optcountcrew.GetBool();
        IsCountMadmate = optcountmadmate.GetBool();
        IsCountImpostor = optcountimpstor.GetBool();
        IsCountNeutral = optcountneutral.GetBool();
        NowKillcount = 0;
    }

    public static OptionItem KillCooldown;
    public static OptionItem ShotLimitOpt;
    public static OptionItem CanKillAllAlive;
    public static OptionItem Shurenekodotti;
    public static OptionItem ImpostorVision;
    public static OptionItem optcanwinkillcount; static int CanwinKillcount;
    public static OptionItem optcountcrew; static bool IsCountCrew;
    public static OptionItem optcountimpstor; static bool IsCountImpostor;
    public static OptionItem optcountmadmate; static bool IsCountMadmate;
    public static OptionItem optcountneutral; static bool IsCountNeutral;
    int NowKillcount;
    enum OptionName
    {
        SheriffShotLimit,
        SheriffCanKillAllAlive,
        WolfBoySchrodingerCatTime,
        Wolfboycanwinkillcount,
        WolfBoyCountteam
    }
    public int ShotLimit = 0;
    public float CurrentKillCooldown = 30;
    bool HasImpV;

    public ISchrodingerCatOwner.TeamType SchrodingerCatChangeTo => Shurenekodotti.GetBool() ? ISchrodingerCatOwner.TeamType.Mad : ISchrodingerCatOwner.TeamType.Crew;
    public override CustomRoles TellResults(PlayerControl player)
    {
        if (player != null) Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[1]);
        return CustomRoles.Impostor;
    }
    private static void SetupOptionItem()
    {
        KillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 0.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OverrideKilldistance.Create(RoleInfo, 8);
        optcanwinkillcount = IntegerOptionItem.Create(RoleInfo, 15, OptionName.Wolfboycanwinkillcount, new(0, 14, 1), 2, false).SetZeroNotation(OptionZeroNotation.Off).SetValueFormat(OptionFormat.Players);
        optcountcrew = BooleanOptionItem.Create(RoleInfo, 16, OptionName.WolfBoyCountteam, true, false, optcanwinkillcount);
        optcountimpstor = BooleanOptionItem.Create(RoleInfo, 17, OptionName.WolfBoyCountteam, false, false, optcanwinkillcount);
        optcountmadmate = BooleanOptionItem.Create(RoleInfo, 18, OptionName.WolfBoyCountteam, false, false, optcanwinkillcount);
        optcountneutral = BooleanOptionItem.Create(RoleInfo, 19, OptionName.WolfBoyCountteam, false, false, optcanwinkillcount);
        ShotLimitOpt = IntegerOptionItem.Create(RoleInfo, 11, OptionName.SheriffShotLimit, new(1, 15, 1), 15, false)
            .SetValueFormat(OptionFormat.Times);
        CanKillAllAlive = BooleanOptionItem.Create(RoleInfo, 12, OptionName.SheriffCanKillAllAlive, true, false);
        ImpostorVision = BooleanOptionItem.Create(RoleInfo, 13, GeneralOption.ImpostorVision, true, false);
        Shurenekodotti = BooleanOptionItem.Create(RoleInfo, 14, OptionName.WolfBoySchrodingerCatTime, false, false);

        optcountcrew.ReplacementDictionary = new() { { "%role%", UtilsRoleText.GetRoleColorAndtext(CustomRoles.Crewmate) } };
        optcountimpstor.ReplacementDictionary = new() { { "%role%", UtilsRoleText.GetRoleColorAndtext(CustomRoles.Impostor) } };
        optcountmadmate.ReplacementDictionary = new() { { "%role%", UtilsRoleText.GetRoleColorAndtext(CustomRoles.Madmate) } };
        optcountneutral.ReplacementDictionary = new() { { "%role%", $"<#cccccc>{GetString("Neutral")}</color>" } };
    }
    public override void Add()
    {
        var playerId = Player.PlayerId;
        CurrentKillCooldown = KillCooldown.GetFloat();

        ShotLimit = ShotLimitOpt.GetInt();
        Logger.Info($"{PlayerCatch.GetPlayerById(playerId)?.GetNameWithRole().RemoveHtmlTags()} : 残り{ShotLimit}発", "WolfBoy");
    }
    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(ShotLimit);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        ShotLimit = reader.ReadInt32();
    }
    public float CalculateKillCooldown() => CanUseKillButton() ? CurrentKillCooldown : 0f;
    public bool CanUseKillButton()
        => Player.IsAlive()
        && (CanKillAllAlive.GetBool() || GameStates.AlreadyDied)
        && ShotLimit > 0;
    public bool CanUseImpostorVentButton() => false;
    public bool CanUseSabotageButton() => false;
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(HasImpV);
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        if (Is(info.AttemptKiller) && !info.IsSuicide)
        {
            (var killer, var target) = info.AttemptTuple;

            Logger.Info($"{killer.GetNameWithRole().RemoveHtmlTags()} : 残り{ShotLimit}発", "WolfBoy");
            if (ShotLimit <= 0)
            {
                info.DoKill = false;
                return;
            }
            ShotLimit--;
            SendRPC();
            var AlienTairo = false;
            var targetroleclass = target.GetRoleClass();
            if ((targetroleclass as Alien)?.CheckSheriffKill(target) == true) AlienTairo = true;
            if ((targetroleclass as JackalAlien)?.CheckSheriffKill(target) == true) AlienTairo = true;
            if ((targetroleclass as AlienHijack)?.CheckSheriffKill(target) == true) AlienTairo = true;

            if (!CanBeKilledBy(target) || AlienTairo)
            {
                //ターゲットが大狼かつ死因を変える設定なら死因を変える、それ以外はMisfire
                PlayerState.GetByPlayerId(killer.PlayerId).DeathReason =
                           target.Is(CustomRoles.Tairou) && Tairou.TairoDeathReason ? CustomDeathReason.Counter :
                           target.Is(CustomRoles.Alien) && Alien.TairoDeathReason ? CustomDeathReason.Counter :
                           (target.Is(CustomRoles.JackalAlien) && JackalAlien.TairoDeathReason ? CustomDeathReason.Counter :
                           (target.Is(CustomRoles.AlienHijack) && Alien.TairoDeathReason ? CustomDeathReason.Counter : CustomDeathReason.Misfire));
                killer.RpcMurderPlayer(killer);
                info.DoKill = false;
                return;
            }
            killer.ResetKillCooldown();
        }
        return;
    }
    void IKiller.OnMurderPlayerAsKiller(MurderInfo info)
    {
        var target = info.AttemptTarget;
        if (target.Is(CustomRoleTypes.Impostor)) Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[0]);
        switch (target.GetCustomRole().GetCustomRoleTypes())
        {
            case CustomRoleTypes.Crewmate:
                if (IsCountCrew)
                    NowKillcount++;
                break;
            case CustomRoleTypes.Madmate:
                if (IsCountMadmate)
                    NowKillcount++;
                break;
            case CustomRoleTypes.Impostor:
                if (IsCountImpostor)
                    NowKillcount++;
                break;
            case CustomRoleTypes.Neutral:
                if (IsCountNeutral)
                    NowKillcount++;
                break;
        }
    }
    public override string GetProgressText(bool comms = false, bool gamelog = false) => Utils.ColorString(CanUseKillButton() ? Color.yellow : Color.gray, $"({ShotLimit})");
    public static bool CanBeKilledBy(PlayerControl player)
    {
        var cRole = player.GetCustomRole();

        if (player.GetRoleClass() is SchrodingerCat schrodingerCat)
        {
            if (schrodingerCat.Team == ISchrodingerCatOwner.TeamType.None)
            {
                Logger.Warn($"狼少年({player.GetRealName()})にキルされたシュレディンガーの猫のロールが変化していません", nameof(WolfBoy));
                return false;
            }
        }

        return cRole.GetCustomRoleTypes() switch
        {
            CustomRoleTypes.Impostor => cRole is not CustomRoles.Tairou,
            _ => true,
        };
    }
    public override void CheckWinner(GameOverReason reason)
    {
        if (CustomWinnerHolder.winners.Contains(CustomWinner.Crewmate) && Player.IsLovers() is false &&
        Player.Is(CustomRoles.Amanojaku) is false)
        {
            // 勝利に必要なキル数に届いていない場合
            if (CanwinKillcount > NowKillcount)
            {
                CustomWinnerHolder.CantWinPlayerIds.Add(Player.PlayerId);
                Logger.Info($"狼少年はキルしてないから負け！", "Wolfboy");
            }
        }
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (CanwinKillcount is 0) return "";

        var teamtext = "";
        if (IsCountImpostor) teamtext += $"<#ff1919>{GetString("Impostor")}</color>";
        if (IsCountMadmate) teamtext += $"<#ff7f50>{GetString("Madmate")}</color>";
        if (IsCountCrew) teamtext += $"<#8cffff>{GetString("Crewmate")}</color>";
        if (IsCountNeutral) teamtext += $"<#cccccc>{GetString("Neutral")}</color>";
        return string.Format(GetString("WolfBoy_LowerText"), CanwinKillcount, teamtext);
    }
    public bool OverrideKillButton(out string text)
    {
        text = "WolfBoy_Kill";
        return true;
    }
    public static System.Collections.Generic.Dictionary<int, Achievement> achievements = new();
    [Attributes.PluginModuleInitializer]
    public static void Load()
    {
        var n1 = new Achievement(RoleInfo, 0, 1, 0, 0);
        var l1 = new Achievement(RoleInfo, 1, 1, 0, 1);
        achievements.Add(0, n1);
        achievements.Add(1, l1);
    }
}