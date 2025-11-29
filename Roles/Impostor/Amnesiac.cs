using AmongUs.GameOptions;
using UnityEngine;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.Crewmate;
using Hazel;

namespace TownOfHost.Roles.Impostor;

public sealed class Amnesiac : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Amnesiac),
            player => new Amnesiac(player),
            CustomRoles.Amnesiac,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            7200,
            SetupCustomOption,
            "am",
            "#f8cd46",
            OptionSort: (8, 0),
            introSound: () => GetIntroSound(RoleTypes.Crewmate),
            isCantSeeTeammates: true
        );
    public Amnesiac(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CantKillImpostor = OptCantKillImpostor.GetBool();
        MatchSettingstoSheriff = OptMatchSettingstoSheriff.GetBool();
        CanRealize = OptRealize.GetBool();
        ImpNeedtoKill = OptImpNeedtoKill.GetBool();
        NeedtoKill = OptNeedtoKill.GetBool();
        KillsRequired = OptKillsRequired.GetInt();
        CanUseVent = OptCanUseVent.GetBool();
        CanUseSabotage = OptCanUseSabotage.GetBool();
        IsWolf = OptIsWolfBoy.GetBool();
        ShShotLimit = IsWolf ? WolfBoy.ShotLimitOpt.GetInt() : Sheriff.ShotLimitOpt.GetInt();
        ShKillCooldown = IsWolf ? WolfBoy.KillCooldown.GetFloat() : Sheriff.KillCooldown.GetFloat();
        ShCanKillAllAlive = IsWolf ? WolfBoy.CanKillAllAlive.GetBool() : Sheriff.CanKillAllAlive.GetBool();
        IsNeedLastImpostor = OptNeedtoLastImpostor.GetBool();

        Realized = false;
        KillCount = 0;
    }

    static OptionItem OptCantKillImpostor;
    static OptionItem OptMatchSettingstoSheriff;
    static OptionItem OptRealize;
    static OptionItem OptImpNeedtoKill;
    static OptionItem OptNeedtoKill;
    static OptionItem OptKillsRequired;
    static OptionItem OptCanUseVent;
    static OptionItem OptCanUseSabotage;
    static OptionItem OptNeedtoLastImpostor;
    static OptionItem OptIsWolfBoy;

    public static bool CantKillImpostor;
    public static bool MatchSettingstoSheriff;
    public static bool CanRealize;
    public static bool ImpNeedtoKill;
    public static bool NeedtoKill;
    public static int KillsRequired;
    public static bool CanUseVent;
    public static bool CanUseSabotage;
    public static int ShShotLimit;
    public static float ShKillCooldown;
    public static bool ShCanKillAllAlive;
    public static bool IsWolf;
    static bool IsNeedLastImpostor;

    public bool Realized;
    public int KillCount;

    enum Options
    {
        AmnesiacCantKillImpostor,
        AmnesiacMatchSettingstoSheriff,
        AmnesiacRealize,//←がONの状態での追加設定↓
        AmnesiacImpNeedtoKill,
        AmnesiacNeedtoKill,
        AmnesiacKillsRequired,
        AmnesiacNeedtoLastImpostor,
        AmnesiacCanUseVent,
        AmnesiacCanUseSabotage,
        AmnesiacIsWolfboy
    }

    public static void SetupCustomOption()
    {
        OptCantKillImpostor = BooleanOptionItem.Create(RoleInfo, 10, Options.AmnesiacCantKillImpostor, true, false);
        OptMatchSettingstoSheriff = BooleanOptionItem.Create(RoleInfo, 11, Options.AmnesiacMatchSettingstoSheriff, true, false);
        OptRealize = BooleanOptionItem.Create(RoleInfo, 12, Options.AmnesiacRealize, false, false);
        OptImpNeedtoKill = BooleanOptionItem.Create(RoleInfo, 13, Options.AmnesiacImpNeedtoKill, false, false, OptRealize);
        OptNeedtoKill = BooleanOptionItem.Create(RoleInfo, 14, Options.AmnesiacNeedtoKill, false, false, OptRealize);
        OptKillsRequired = IntegerOptionItem.Create(RoleInfo, 15, Options.AmnesiacKillsRequired, new(1, 6, 1), 2, false, OptNeedtoKill);
        OptNeedtoLastImpostor = BooleanOptionItem.Create(RoleInfo, 18, Options.AmnesiacNeedtoLastImpostor, false, false, OptRealize);
        OptCanUseVent = BooleanOptionItem.Create(RoleInfo, 16, Options.AmnesiacCanUseVent, false, false, OptRealize);
        OptCanUseSabotage = BooleanOptionItem.Create(RoleInfo, 17, Options.AmnesiacCanUseSabotage, false, false, OptRealize);
        OptIsWolfBoy = BooleanOptionItem.Create(RoleInfo, 20, Options.AmnesiacIsWolfboy, false, false);
    }

    public float CalculateKillCooldown() => MatchSettingstoSheriff && !Realized ? ShKillCooldown : TownOfHost.Options.DefaultKillCooldown;
    public bool CanUseImpostorVentButton() => Realized && CanUseVent;
    public bool CanUseSabotageButton() => Realized && CanUseSabotage;
    public bool CanUseKillButton()
    {
        if (!Player.IsAlive()) return false;
        if (!MatchSettingstoSheriff || Realized) return true;
        return ShCanKillAllAlive || GameStates.AlreadyDied;
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(Realized || (MatchSettingstoSheriff && IsWolf && WolfBoy.ImpostorVision.GetBool()));
    }

    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (!Is(killer)) return;

        if (CantKillImpostor && (target.GetCustomRole().IsImpostor() || target.GetCustomRole() is CustomRoles.Egoist))
            info.DoKill = false;
        if (CanRealize && ImpNeedtoKill && !Realized)
            Realize();
    }
    public void OnMurderPlayerAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (!Is(killer)) return;

        KillCount++;
        if (CanRealize && NeedtoKill && !Realized && KillCount >= KillsRequired)
            Realize();
    }

    private void Realize()
    {
        Realized = true;
        SendRPC();

        var clientId = Player.GetClientId();
        foreach (var pc in PlayerCatch.AllPlayerControls)
        {
            var role = pc.GetCustomRole();
            if (!role.IsImpostor() && role is not CustomRoles.Egoist) continue;

            RoleTypes roleTypes = role.GetRoleTypes();
            if (!pc.IsAlive()) roleTypes = RoleTypes.ImpostorGhost;
            pc.RpcSetRoleDesync(roleTypes, clientId);
        }

        if (!Utils.RoleSendList.Contains(Player.PlayerId))
            Utils.RoleSendList.Add(Player.PlayerId);
        Player.RpcProtectedMurderPlayer();
        UtilsNotifyRoles.NotifyRoles();
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost || Realized || !player.IsAlive() || !IsNeedLastImpostor || !MyState.HasSpawned) return;

        if (PlayerCatch.AliveImpostorCount is 1)
        {
            Realize();
        }
    }

    public override void OverrideDisplayRoleNameAsSeen(PlayerControl seer, ref bool enabled, ref Color roleColor, ref string roleText, ref bool addon)
    {
        seer ??= Player;
        if (seer.GetCustomRole().IsImpostor() || seer.Is(CustomRoles.Egoist))
        {
            roleColor = Vanilla.Impostor.RoleInfo.RoleColor;
            roleText = Realized ? roleText : GetString("Amnesiac");
        }
        //本人にはシェリフ、インポスターにはロールカラーを赤に
        if (Is(seer))
        {
            roleColor = IsWolf ? WolfBoy.RoleInfo.RoleColor : Sheriff.RoleInfo.RoleColor;
            roleText = Realized ? roleText : (IsWolf ? GetString(CustomRoles.WolfBoy.ToString()) : GetString(CustomRoles.Sheriff.ToString()));
        }
        if (!seer.IsAlive() && !Realized)
        {
            roleText += $"(<#ff1919>{GetString("Amnesiac")}</color>)";
        }
    }
    public bool OverrideKillButton(out string text)
    {
        text = IsWolf ? "WolfBoy_Kill" : "Sheriff_Kill";
        return true;
    }

    public override string GetProgressText(bool comms = false, bool gamelog = false) => MatchSettingstoSheriff && !Realized ? Utils.ColorString(ShShotLimit - KillCount > 0 ? Color.yellow : Color.gray, $"({ShShotLimit - KillCount})") : "";

    public void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(Realized);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        Realized = reader.ReadBoolean();
    }

}

