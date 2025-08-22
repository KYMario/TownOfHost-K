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
    }

    public static OptionItem KillCooldown;
    public static OptionItem ShotLimitOpt;
    public static OptionItem CanKillAllAlive;
    public static OptionItem Shurenekodotti;
    public static OptionItem ImpostorVision;
    enum OptionName
    {
        SheriffShotLimit,
        SheriffCanKillAllAlive,
        WolfBoySchrodingerCatTime
    }
    public int ShotLimit = 0;
    public float CurrentKillCooldown = 30;
    bool HasImpV;

    public ISchrodingerCatOwner.TeamType SchrodingerCatChangeTo => Shurenekodotti.GetBool() ? ISchrodingerCatOwner.TeamType.Mad : ISchrodingerCatOwner.TeamType.Crew;
    public override CustomRoles TellResults(PlayerControl player) => CustomRoles.Impostor;
    private static void SetupOptionItem()
    {
        KillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 0.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OverrideKilldistance.Create(RoleInfo, 8);
        ShotLimitOpt = IntegerOptionItem.Create(RoleInfo, 11, OptionName.SheriffShotLimit, new(1, 15, 1), 15, false)
            .SetValueFormat(OptionFormat.Times);
        CanKillAllAlive = BooleanOptionItem.Create(RoleInfo, 12, OptionName.SheriffCanKillAllAlive, true, false);
        ImpostorVision = BooleanOptionItem.Create(RoleInfo, 13, GeneralOption.ImpostorVision, true, false);
        Shurenekodotti = BooleanOptionItem.Create(RoleInfo, 14, OptionName.WolfBoySchrodingerCatTime, false, false);
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
                           target.Is(CustomRoles.Tairou) && Tairou.TairoDeathReason ? CustomDeathReason.Revenge1 :
                           target.Is(CustomRoles.Alien) && Alien.TairoDeathReason ? CustomDeathReason.Revenge1 :
                           (target.Is(CustomRoles.JackalAlien) && JackalAlien.TairoDeathReason ? CustomDeathReason.Revenge1 :
                           (target.Is(CustomRoles.AlienHijack) && Alien.TairoDeathReason ? CustomDeathReason.Revenge1 : CustomDeathReason.Misfire));
                killer.RpcMurderPlayer(killer);
                info.DoKill = false;
                return;
            }

            killer.ResetKillCooldown();
        }
        return;
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
    public bool OverrideKillButton(out string text)
    {
        text = "WolfBoy_Kill";
        return true;
    }
}