using AmongUs.GameOptions;
using Hazel;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Neutral;

public sealed class CountKiller : RoleBase, ILNKiller, ISchrodingerCatOwner, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(CountKiller),
            player => new CountKiller(player),
            CustomRoles.CountKiller,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            13300,
            SetupOptionItem,
            "ck",
            "#FF1493",
            (2, 0),
            true,
            assignInfo: new RoleAssignInfo(CustomRoles.CountKiller, CustomRoleTypes.Neutral)
            {
                AssignCountRule = new(1, 1, 1)
            },
            Desc: () =>
            {
                return string.Format(GetString("CountKillerDesc"), OptionVictoryCount.GetInt(), OptionAddWin.GetBool() ? GetString("AddWin") : GetString("SoloWin"));
            }
        );
    public CountKiller(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        VictoryCount = OptionVictoryCount.GetInt();
        KillCooldown = OptionKillCooldown.GetFloat();
        CanVent = OptionCanVent.GetBool();
        KillCount = 0;
        WinFlag = false;
    }
    static OptionItem OptionKillCooldown;
    static OptionItem OptionAddWin;
    private static OptionItem OptionVictoryCount;
    public static OptionItem OptionCanVent;

    enum OptionName
    {
        CountKillerVictoryCount, CountKillerAddWin
    }
    private int VictoryCount;
    public static bool CanVent;
    private static float KillCooldown;
    int KillCount = 0;
    bool WinFlag;
    private static void SetupOptionItem()
    {
        SoloWinOption.Create(RoleInfo, 9, defo: 1);
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 0.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionVictoryCount = IntegerOptionItem.Create(RoleInfo, 11, OptionName.CountKillerVictoryCount, new(1, 10, 1), 5, false)
        .SetValueFormat(OptionFormat.Times);
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.CanVent, true, false);
        OptionAddWin = BooleanOptionItem.Create(RoleInfo, 13, OptionName.CountKillerAddWin, true, false);
        RoleAddAddons.Create(RoleInfo, 15);
    }
    public ISchrodingerCatOwner.TeamType SchrodingerCatChangeTo => ISchrodingerCatOwner.TeamType.CountKiller;
    public float CalculateKillCooldown() => KillCooldown;
    public override void Add()
    {
        var playerId = Player.PlayerId;
        KillCooldown = OptionKillCooldown.GetFloat();

        VictoryCount = OptionVictoryCount.GetInt();
        Logger.Info($"{PlayerCatch.GetPlayerById(playerId)?.GetNameWithRole().RemoveHtmlTags()} : 後{VictoryCount - KillCount}発", "CountKiller");
    }
    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(KillCount);
        sender.Writer.Write(WinFlag);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        KillCount = reader.ReadInt32();
        WinFlag = reader.ReadBoolean();
    }
    public bool CanUseKillButton() => Player.IsAlive() && VictoryCount > 0;
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => CanVent;
    public void OnMurderPlayerAsKiller(MurderInfo info)
    {
        if (Is(info.AttemptKiller) && !info.IsSuicide)
        {
            (var killer, var target) = info.AttemptTuple;
            if (target.Is(CustomRoles.SchrodingerCat))
            {
                return;
            }
            KillCount++;
            Logger.Info($"{killer.GetNameWithRole()} : 残り{VictoryCount - KillCount}発", "CountKiller");
            SendRPC();
            killer.ResetKillCooldown();

            if (KillCount >= VictoryCount)
            {
                Win();
                WinFlag = true;
            }
        }
        return;
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if ((seen == seer) && WinFlag && OptionAddWin.GetBool()) return "<color=#dddd00>★</color>";
        return "";
    }
    public void Win()
    {
        if (OptionAddWin.GetBool()) return;
        if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.CountKiller, Player.PlayerId))
        {
            CustomWinnerHolder.NeutralWinnerIds.Add(Player.PlayerId);
        }
    }
    public override string GetProgressText(bool comms = false, bool gamelog = false)
    => Utils.ColorString(RoleInfo.RoleColor, $"({KillCount}/{VictoryCount})");
    public bool CheckWin(ref CustomRoles winnerRole) => OptionAddWin.GetBool() && WinFlag;
}