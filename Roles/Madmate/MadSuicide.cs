using System.Linq;
using AmongUs.GameOptions;
using Hazel;
using InnerNet;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Madmate;
public sealed class MadSuicide : RoleBase, IKiller, IUseTheShButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(MadSuicide),
            player => new MadSuicide(player),
            CustomRoles.MadSuicide,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Madmate,
            11300,
            SetupOptionItem,
            "MSu",
            isDesyncImpostor: true,
            introSound: () => GetIntroSound(RoleTypes.Impostor),
            from: From.SuperNewRoles
        );
    public MadSuicide(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
    }
    static OptionItem OptionKillCoolDown;
    static OptionItem OptionKillDeathreason;
    static OptionItem OptionAbilityCoolDown;
    static OptionItem OptionAbilityDeathreason;
    static OptionItem OptionCanusevent;
    static OptionItem OptionKillSuicidetargetkiller;
    public static readonly CustomDeathReason[] deathReasons =
    {
        CustomDeathReason.Kill,CustomDeathReason.Suicide,CustomDeathReason.Misfire,CustomDeathReason.Revenge1
    };
    enum OptionName
    {
        MadSuicideKillDeathreason,
        MadSuicideAbilityDeathreason,
        MadSuicideKillSuicidetargetkiller
    }

    static void SetupOptionItem()
    {
        var cRolesString = deathReasons.Select(x => $"DeathReason.{x}".ToString()).ToArray();
        OptionKillCoolDown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, OptionBaseCoolTime, 20f, false).SetValueFormat(OptionFormat.Seconds);
        OptionKillDeathreason = StringOptionItem.Create(RoleInfo, 11, OptionName.MadSuicideKillDeathreason, cRolesString, 2, false);
        OptionKillSuicidetargetkiller = BooleanOptionItem.Create(RoleInfo, 12, OptionName.MadSuicideKillSuicidetargetkiller, false, false);
        OptionAbilityCoolDown = FloatOptionItem.Create(RoleInfo, 15, GeneralOption.Cooldown, OptionBaseCoolTime, 20f, false).SetValueFormat(OptionFormat.Seconds);
        OptionAbilityDeathreason = StringOptionItem.Create(RoleInfo, 16, OptionName.MadSuicideAbilityDeathreason, cRolesString, 0, false);
        OptionCanusevent = BooleanOptionItem.Create(RoleInfo, 20, GeneralOption.CanVent, true, false);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterCooldown = OptionAbilityCoolDown.GetFloat();
    }
    public bool CanUseKillButton() => true;
    public bool CanUseImpostorVentButton() => OptionCanusevent.GetBool();
    public bool CanUseSabotageButton() => false;
    public float CalculateKillCooldown() => OptionKillCoolDown.GetFloat();
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        info.DoKill = false;

        if (!Player.IsAlive()) return;
        MyState.DeathReason = deathReasons[OptionKillDeathreason.GetValue()];
        if (OptionKillSuicidetargetkiller.GetBool())
        {
            foreach (var seer in PlayerCatch.AllPlayerControls)
            {
                if (seer == null) return;
                if (!GameStates.InGame) break;

                if (seer.AmOwner)
                {
                    if (target == seer) killer.MurderPlayer(killer, MurderResultFlags.Succeeded);
                    else target.MurderPlayer(killer, MurderResultFlags.Succeeded);
                }
                else
                {
                    var t = target;
                    if (target == seer) t = killer;//ターゲットとキラーを入れ替えてるだけ★
                    //ターゲット視点は自爆に見える
                    MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(t.NetId, (byte)RpcCalls.MurderPlayer, SendOption.Reliable, seer.GetClientId());
                    messageWriter.WriteNetObject(killer);
                    messageWriter.Write((int)MurderResultFlags.Succeeded);
                    AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
                }
            }
            Player.SetRealKiller(Player);
            return;
        }
        Player.SetRealKiller(Player);
        killer.RpcMurderPlayer(killer);
    }
    public void OnClick()
    {
        if (!Player.IsAlive()) return;
        MyState.DeathReason = deathReasons[OptionAbilityDeathreason.GetValue()];
        Player.RpcMurderPlayer(Player);
    }
    public bool OverrideKillButtonText(out string text) { text = Translator.GetString("Suicide"); return true; }
}