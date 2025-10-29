using AmongUs.GameOptions;
using Hazel;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;

public sealed class EvilMaker : RoleBase, IImpostor, IUsePhantomButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(EvilMaker),
            player => new EvilMaker(player),
            CustomRoles.EvilMaker,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Impostor,
            3600,
            SetupOptionItem,
            "Em",
            OptionSort: (2, 5)
        );
    public EvilMaker(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCooldown = OptionKillCoolDown.GetFloat();
        AbilityCooldown = OptionAbilityCoolDown.GetFloat();

        Used = false;
    }
    static OptionItem OptionKillCoolDown; static float KillCooldown;
    static OptionItem OptionAbilityCoolDown; static float AbilityCooldown;
    bool Used;
    private static void SetupOptionItem()
    {
        OptionKillCoolDown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, OptionBaseCoolTime, 20f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionAbilityCoolDown = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.Cooldown, OptionBaseCoolTime, 20f, false)
                .SetValueFormat(OptionFormat.Seconds);
    }
    public float CalculateKillCooldown() => KillCooldown;
    bool IUsePhantomButton.IsPhantomRole => !Used;
    public override void ApplyGameOptions(IGameOptions opt) => AURoleOptions.PhantomCooldown = AbilityCooldown;
    public void OnClick(ref bool AdjustKillCooldown, ref bool? ResetCooldown)
    {
        ResetCooldown = true;

        if (Used) return;

        var target = Player.GetKillTarget(true);
        if (target == null) return;
        if ((target.GetCustomRole() is CustomRoles.SKMadmate or CustomRoles.King or CustomRoles.Merlin || target.GetCustomRole().IsImpostor()) && !SuddenDeathMode.NowSuddenDeathMode) return;

        Used = true;
        AdjustKillCooldown = false;
        if (SuddenDeathMode.NowSuddenDeathTemeMode)
        {
            target.SideKickChangeTeam(Player);
        }

        _ = new LateTask(() => Player.SetKillCooldown(target: target), Main.LagTime, "", true);
        target.RpcProtectedMurderPlayer(Player);
        target.RpcProtectedMurderPlayer(target);
        UtilsGameLog.AddGameLog($"SideKick", string.Format(GetString("log.Sidekick"), UtilsName.GetPlayerColor(target, true) + $"({UtilsRoleText.GetTrueRoleName(target.PlayerId)})", UtilsName.GetPlayerColor(Player, true)));
        target.RpcSetCustomRole(CustomRoles.SKMadmate);
        NameColorManager.Add(Player.PlayerId, target.PlayerId, "#ff1919");
        NameColorManager.Add(target.PlayerId, Player.PlayerId, "#ff1919");
        if (!Utils.RoleSendList.Contains(target.PlayerId)) Utils.RoleSendList.Add(target.PlayerId);

        target.RpcSetRole(Options.SkMadCanUseVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate, true);
        PlayerCatch.SKMadmateNowCount++;
        target.MarkDirtySettings();
        UtilsNotifyRoles.NotifyRoles();
        UtilsGameLog.LastLogRole[target.PlayerId] += "<b>⇒" + Utils.ColorString(UtilsRoleText.GetRoleColor(target.GetCustomRole()), GetString($"{target.GetCustomRole()}")) + "</b>" + UtilsRoleText.GetSubRolesText(target.PlayerId);
        AdjustKillCooldown = true;
        SendRPC();
    }
    public override string GetAbilityButtonText() => GetString("Sidekick");
    public override bool OverrideAbilityButton(out string text)
    {
        text = "Impostor_Side";
        return true;
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (seen.PlayerId != seer.PlayerId || isForMeeting || !Used || Options.CanMakeMadmateCount.GetInt() <= PlayerCatch.SKMadmateNowCount || !Player.IsAlive()) return "";

        if (isForHud) return GetString("PhantomButtonSideKick");
        return $"<size=50%>{GetString("PhantomButtonSideKick")}</size>";
    }

    public void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(PlayerCatch.SKMadmateNowCount);
        sender.Writer.Write(Used);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        PlayerCatch.SKMadmateNowCount = reader.ReadInt32();
        Used = reader.ReadBoolean();
    }
}
