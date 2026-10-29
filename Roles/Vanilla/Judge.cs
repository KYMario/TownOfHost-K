using System.Collections.Generic;
using AmongUs.GameOptions;
using Hazel;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Impostor;
using TownOfHost.Roles.Neutral;

namespace TownOfHost.Roles.Vanilla;

public sealed class Judge : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForVanilla(
            typeof(Judge),
            player => new Judge(player),
            RoleTypes.Judge,
            SetUpCustomOption,
            "#a1472c"
            , from: From.AmongUs
        );
    public Judge(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        LimitAbility = OptionCount.GetInt();
        Isfall = false;
    }
    int LimitAbility;
    bool Isfall;
    public static OptionItem OptionTaskRequirement;
    private static OptionItem OptionCanKillMadMate;
    private static OptionItem OptionCanKillNeutrals;
    private static OptionItem OptionCanKillLovers;
    private static OptionItem OptionCount;
    public static void SetUpCustomOption()
    {
        OptionTaskRequirement = IntegerOptionItem.Create(RoleInfo, 25110, GeneralOption.TaskTrigger, new(0, 99, 1), 1, false)
            .SetValueFormat(OptionFormat.Pieces);
        OptionCount = IntegerOptionItem.Create(RoleInfo, 25111, GeneralOption.OptionCount, new(1, 15, 1), 1, false);
        OptionCanKillMadMate = BooleanOptionItem.Create(RoleInfo, 25112, "MeetingSheriffCanKillMadMate", true, false);
        OptionCanKillNeutrals = BooleanOptionItem.Create(RoleInfo, 25113, "MeetingSheriffCanKillNeutrals", true, false);
        OptionCanKillLovers = BooleanOptionItem.Create(RoleInfo, 25114, "SheriffCanKillLovers", true, false);
    }
    /// <summary>
    /// 個数設定をバニラの達成率に変換する。
    /// </summary>
    public static float GetTaskRequirementPercentage(PlayerControl player)
    {
        var need = OptionTaskRequirement.GetInt();
        if (need <= 0) return 0f;

        var all = player == null ? -1 : PlayerState.GetByPlayerId(player.PlayerId)?.GetTaskState()?.AllTasksCount ?? -1;
        if (all <= 0)
        {
            all = Main.NormalOptions.NumCommonTasks
                + Main.NormalOptions.NumLongTasks
                + Main.NormalOptions.NumShortTasks;
        }
        if (all <= 0) return 0f;
        if (need >= all) return 100f;
        return need * 100f / all;
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.JudgeTaskRequirementPercentage = GetTaskRequirementPercentage(Player);
    }
    public override bool CallJudgeVote(PlayerControl voter, PlayerControl votefor, ref byte ExilePlayerid)
    {
        ExilePlayerid = byte.MaxValue;
        if (LimitAbility <= 0) return false;
        if (SelfVoteManager.Canuseability() is false) return false;
        if (votefor.IsAlive() is false) return false;

        LimitAbility--;
        var AlienTairo = false;
        var targetroleclass = votefor.GetRoleClass();
        if ((targetroleclass as Alien)?.CheckSheriffKill(votefor) == true) AlienTairo = true;
        if ((targetroleclass as JackalAlien)?.CheckSheriffKill(votefor) == true) AlienTairo = true;
        if ((targetroleclass as AlienHijack)?.CheckSheriffKill(votefor) == true) AlienTairo = true;

        if (AntiBlackout.OverrideExiledPlayer())
        {
            Utils.AllPlayerKillFlash();
        }

        if ((CanBeKilledBy(votefor.GetCustomRole()) && !AlienTairo) || (votefor.IsLovers() && OptionCanKillLovers.GetBool()) || (votefor.Is(CustomRoles.Amanojaku) && OptionCanKillNeutrals.GetBool()))
        {//成功
            ExilePlayerid = votefor.PlayerId;
            votefor.SetRealKiller(voter);
        }
        else
        {
            Isfall = true;
            ExilePlayerid = voter.PlayerId;
        }
        SendRpc();
        return true;
    }
    public override void OnExileWrapUp(NetworkedPlayerInfo exiled, ref bool DecidedWinner)
    {
        if (exiled == null) return;
        if (exiled.PlayerId == Player.PlayerId && Isfall)
        {
            MyState.DeathReason = CustomDeathReason.Misfire;
        }
    }
    bool CanBeKilledBy(CustomRoles role)
    {
        if (role == CustomRoles.SKMadmate) return OptionCanKillMadMate.GetBool();
        if (role == CustomRoles.Jackaldoll) return OptionCanKillNeutrals.GetBool();

        return role.GetCustomRoleTypes() switch
        {
            CustomRoleTypes.Impostor => role is not CustomRoles.Tairou,
            CustomRoleTypes.Madmate => OptionCanKillMadMate.GetBool(),
            CustomRoleTypes.Neutral => OptionCanKillNeutrals.GetBool(),
            CustomRoleTypes.Crewmate => role is CustomRoles.WolfBoy,
            _ => false
        };
    }/*死んだら誤爆にするか悩む。
    public override bool VotingResults(ref NetworkedPlayerInfo Exiled, ref bool IsTie, Dictionary<byte, int> vote, byte[] mostVotedPlayers, bool ClearAndExile)
    {
        if (Exiled is not null)
        {
            if (Isfall && Exiled.PlayerId == Player.PlayerId)
            {
                MyState.DeathReason = CustomDeathReason.Misfire;
            }
        }
        return false;
    }*/
    public override string GetProgressText(bool comms = false, bool GameLog = false) => $" <{(0 < LimitAbility ? RoleInfo.RoleColorCode : "#828282")}>({LimitAbility})</color>";
    void SendRpc()
    {
        using var sender = CreateSender();
        sender.Writer.Write(LimitAbility);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        LimitAbility = reader.ReadInt32();
    }
}
