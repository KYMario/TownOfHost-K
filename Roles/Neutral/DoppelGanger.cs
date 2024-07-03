using AmongUs.GameOptions;
using Hazel;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using UnityEngine;

namespace TownOfHost.Roles.Neutral;
public sealed class DoppelGanger : RoleBase, ILNKiller, ISchrodingerCatOwner, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(DoppelGanger),
            player => new DoppelGanger(player),
            CustomRoles.DoppelGanger,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Neutral,
            53000,
            SetupOptionItem,
            "dg",
            "#47266e",
            true,
            assignInfo: new RoleAssignInfo(CustomRoles.DoppelGanger, CustomRoleTypes.Neutral)
            {
                AssignCountRule = new(1, 1, 1)
            }
            );
    public DoppelGanger(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCooldown = OptionKillCooldown.GetFloat();
        Cankill = false;
        Target = byte.MaxValue;
        Afterkill = false;
        SecondsWin = false;
        Seconds = 0;
        Count = 0;
        win = false;
    }

    static OptionItem OptionKillCooldown;
    static OptionItem OptionHasImpostorVision;
    static OptionItem OptionShepeCoolDown;
    static OptionItem OptionWinCount;
    static OptionItem OptionWin;
    static float KillCooldown;
    bool Cankill;
    bool Afterkill;
    bool SecondsWin;
    float Seconds;
    int Count;
    byte Target;
    bool win;
    public SchrodingerCat.TeamType SchrodingerCatChangeTo => SchrodingerCat.TeamType.DoppelGanger;

    private static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionHasImpostorVision = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.ImpostorVision, true, false);
        OptionShepeCoolDown = FloatOptionItem.Create(RoleInfo, 12, GeneralOption.Cooldown, new(0f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionWinCount = FloatOptionItem.Create(RoleInfo, 13, "DoppelGangerWinCount", new(0f, 300f, 2.5f), 45f, false);
        OptionWin = FloatOptionItem.Create(RoleInfo, 14, "DoppelGangerWin", new(0f, 300f, 2.5f), 70f, false);
        RoleAddAddons.Create(RoleInfo, 15);
    }
    public float CalculateKillCooldown() => KillCooldown;
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => false;
    public void ApplySchrodingerCatOptions(IGameOptions option)
    {
        option.SetVision(false);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterCooldown = OptionShepeCoolDown.GetFloat();
        AURoleOptions.ShapeshifterDuration = 0f;
        AURoleOptions.ShapeshifterLeaveSkin = false;
        opt.SetVision(OptionHasImpostorVision.GetBool());
    }

    public override bool CheckShapeshift(PlayerControl target, ref bool animate)
    {
        if (Is(target))
        {
            animate = false;
            return false;
        }
        Cankill = true;
        Target = target.PlayerId;
        _ = new LateTask(() => Utils.NotifyRoles(), 1f, "");
        return true;
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        Player.RpcShapeshift(Player, false);
        Cankill = false;
        Target = byte.MaxValue;
        Afterkill = false;
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AppearanceTuple;

        if (Target == byte.MaxValue || target.PlayerId != Target || !Cankill || Afterkill)
        {
            info.DoKill = false;
            return;
        }
    }
    public void OnMurderPlayerAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AppearanceTuple;
        if (info.CanKill && info.DoKill) Afterkill = true;
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        seen ??= seer;
        if (seer == seen || seen.PlayerId == Target)
        {
            var bunbo = OptionWinCount.GetFloat();
            var b = OptionWin.GetFloat();
            if (!Player.IsAlive()) return "";
            if (SecondsWin) return Utils.ColorString(Palette.Purple.ShadeColor(-0.5f), $"({Count}/{b}) {Utils.AdditionalWinnerMark}");
            else if (Target != byte.MaxValue)
                return Utils.ColorString(Palette.Purple.ShadeColor(-0.3f), $"({Count}/{bunbo})");
            else
                return Utils.ColorString(Palette.Purple.ShadeColor(-0.1f), $"({Count}/{bunbo})");
        }
        return "";
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!player.IsAlive()) return;
        var ch = false;
        if (Afterkill)
        {
            ch = true;
            Seconds += Time.fixedDeltaTime * 0.9f;
        }
        if (Target != byte.MaxValue)
        {
            ch = true;
            Seconds += Time.fixedDeltaTime * 0.1f;
        }

        if (!ch) return;

        if (Seconds >= OptionWinCount.GetFloat()) SecondsWin = true;
        if (Seconds >= OptionWin.GetFloat())
        {
            win = true;
            CustomWinnerHolder.ResetAndSetWinner((CustomWinner)CustomRoles.DoppelGanger);
            CustomWinnerHolder.WinnerIds.Add(Player.PlayerId);
            Cankill = false;
            Target = byte.MaxValue;
            Afterkill = false;
            return;
        }
        if (Count != (int)Seconds)
        {
            Count = (int)Seconds;
            Utils.NotifyRoles();
        }
    }

    public bool CheckWin(ref CustomRoles winnerRole) => Player.IsAlive() && SecondsWin && !win;
}