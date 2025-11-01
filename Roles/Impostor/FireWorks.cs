using System.Collections.Generic;
using UnityEngine;

using AmongUs.GameOptions;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Modules;
using Hazel;

namespace TownOfHost.Roles.Impostor;

public sealed class FireWorks : RoleBase, IImpostor, IUsePhantomButton
{
    public enum FireWorksState
    {
        Initial = 1,
        SettingFireWorks = 2,
        WaitTime = 4,
        ReadyFire = 8,
        FireEnd = 16,
        CanUseKill = Initial | FireEnd
    }
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(FireWorks),
            player => new FireWorks(player),
            CustomRoles.FireWorks,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Impostor,
            4400,
            SetupCustomOption,
            "fw",
            OptionSort: (3, 6),
            from: From.TownOfHost
        );
    public FireWorks(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        FireWorksCount = OptionFireWorksCount.GetInt();
        FireWorksRadius = OptionFireWorksRadius.GetFloat();
        Cankill = OptionCankillAlltime.GetBool();
        Cool = OptionCooldown.GetFloat();
    }

    static OptionItem OptionFireWorksCount;
    static OptionItem OptionFireWorksRadius;
    static OptionItem OptionCankillAlltime;
    static OptionItem OptionCooldown;
    static OptionItem OptionPaaaaaaaanCooldown;
    enum OptionName
    {
        FireWorksMaxCount,
        FireWorksRadius,
        FireWorksCanKillAlways,
        FireWorksPaaaaaanCoolDown
    }

    int FireWorksCount;
    float FireWorksRadius;
    float Cool;
    int NowFireWorksCount;
    bool Cankill;
    List<Vector3> FireWorksPosition = new();
    FireWorksState State = FireWorksState.Initial;

    public static void SetupCustomOption()
    {
        OptionFireWorksCount = IntegerOptionItem.Create(RoleInfo, 10, OptionName.FireWorksMaxCount, new(1, 5, 1), 1, false)
            .SetValueFormat(OptionFormat.Pieces);
        OptionFireWorksRadius = FloatOptionItem.Create(RoleInfo, 11, OptionName.FireWorksRadius, new(0.5f, 3f, 0.5f), 1f, false)
            .SetValueFormat(OptionFormat.Multiplier);
        OptionCankillAlltime = BooleanOptionItem.Create(RoleInfo, 13, OptionName.FireWorksCanKillAlways, false, false);
        OptionCooldown = FloatOptionItem.Create(RoleInfo, 14, GeneralOption.Cooldown, new(0f, 180f, 0.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionPaaaaaaaanCooldown = FloatOptionItem.Create(RoleInfo, 15, OptionName.FireWorksPaaaaaanCoolDown, new(0f, 180f, 0.5f), 15f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void Add()
    {
        NowFireWorksCount = FireWorksCount;
        FireWorksPosition.Clear();
        State = FireWorksState.Initial;
    }

    public bool CanUseKillButton()
    {
        if (Cankill) return true;
        if (!Player.IsAlive()) return false;
        return (State & FireWorksState.CanUseKill) != 0;
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.PhantomCooldown = State is FireWorksState.FireEnd ? 200f : (State is FireWorksState.ReadyFire or FireWorksState.WaitTime) ? OptionPaaaaaaaanCooldown.GetFloat() : Cool;
    }
    public override bool CanUseAbilityButton() => State != FireWorksState.FireEnd || !Player.IsAlive();
    public bool UseOneclickButton => true;
    public void OnClick(ref bool AdjustKillCooldown, ref bool? ResetCooldown)
    {
        AdjustKillCooldown = true;
        ResetCooldown = true;
        Logger.Info($"FireWorks ShapeShift", "FireWorks");
        switch (State)
        {
            case FireWorksState.Initial:
            case FireWorksState.SettingFireWorks:
                Logger.Info("花火を一個設置", "FireWorks");
                FireWorksPosition.Add(Player.transform.position);
                NowFireWorksCount--;
                if (NowFireWorksCount == 0)
                    State = PlayerCatch.AliveImpostorCount <= 1 || SuddenDeathMode.NowSuddenDeathMode
                        ? FireWorksState.ReadyFire : FireWorksState.WaitTime;
                else
                    State = FireWorksState.SettingFireWorks;
                Player.RpcResetAbilityCooldown(Sync: true);
                break;
            case FireWorksState.ReadyFire:
                Logger.Info("花火を爆破", "FireWorks");
                if (AmongUsClient.Instance.AmHost)
                {
                    //爆破処理はホストのみ
                    bool suicide = false;
                    foreach (var fireTarget in PlayerCatch.AllAlivePlayerControls)
                    {
                        foreach (var pos in FireWorksPosition)
                        {
                            var dis = Vector2.Distance(pos, fireTarget.transform.position);
                            if (dis > FireWorksRadius) continue;

                            if (fireTarget == Player)
                            {
                                //自分は後回し
                                suicide = true;
                            }
                            else
                            {
                                if (CustomRoleManager.OnCheckMurder(Player, fireTarget, fireTarget, fireTarget, true, false, 2))
                                {
                                    PlayerState.GetByPlayerId(fireTarget.PlayerId).DeathReason = CustomDeathReason.Bombed;
                                }
                            }
                        }
                    }
                    if (suicide)
                    {
                        var totalAlive = PlayerCatch.AllAlivePlayersCount;
                        //自分が最後の生き残りの場合は勝利のために死なない
                        if (totalAlive != 1)
                        {
                            MyState.DeathReason = CustomDeathReason.Misfire;
                            Player.RpcMurderPlayer(Player);
                        }
                    }
                }
                State = FireWorksState.FireEnd;

                Player.RpcResetAbilityCooldown(Sync: true);
                break;
            default:
                break;
        }
        UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player);
        SendRpc();
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        string retText = "";

        if (State == FireWorksState.WaitTime && (PlayerCatch.AliveImpostorCount <= 1 || SuddenDeathMode.NowSuddenDeathMode))
        {
            Logger.Info("爆破準備OK", "FireWorks");
            State = FireWorksState.ReadyFire;
            UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player);
        }
        switch (State)
        {
            case FireWorksState.Initial:
            case FireWorksState.SettingFireWorks:
                retText = string.Format(GetString("FireworksPutPhase"), NowFireWorksCount);
                break;
            case FireWorksState.WaitTime:
                retText = GetString("FireworksWaitPhase");
                break;
            case FireWorksState.ReadyFire:
                retText = GetString("FireworksReadyFirePhase");
                break;
            case FireWorksState.FireEnd:
                break;
        }
        return retText;
    }
    public override string GetAbilityButtonText()
    {
        if (State == FireWorksState.ReadyFire)
            return GetString("FireWorksBomberExplosionButtonText");
        else
            return GetString("FireWorksInstallAtionButtonText");
    }
    public override bool OverrideAbilityButton(out string text)
    {
        text = "FireWorks_Ability";
        return true;
    }
    void SendRpc()
    {
        using var sender = CreateSender();
        sender.Writer.Write((int)State);
        sender.Writer.Write(NowFireWorksCount);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        State = (FireWorksState)reader.ReadInt32();
        NowFireWorksCount = reader.ReadInt32();
    }
}