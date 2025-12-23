using System.Linq;
using AmongUs.GameOptions;

using Hazel;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.Impostor;

namespace TownOfHost.Roles.Crewmate;

public sealed class Merlin : RoleBase, IRoomTasker
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Merlin),
            player => new Merlin(player),
            CustomRoles.Merlin,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            15900,
            null,
            "mer",
            "#8cc2ff",
            (2, 1),
            combination: CombinationRoles.AssassinandMerlin
        );
    public Merlin(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => Assassin.OptionMerlinHasTask.GetBool() ? HasTask.True : HasTask.False
    )
    {
        completeroom = 0;

        worktask = Assassin.OptionMerlinHasTask.GetBool() ? Assassin.OptionMerlinWorkTask.GetInt() : 0;
        CanSeeNeutral = Assassin.OptionMerlinCanSeeNeutral?.GetBool() ?? false;
        OnlyNeutralKiller = Assassin.OptionMerlinOnlyNeutralKiller?.GetBool() ?? false;
        IsGlayColor = Assassin.OptionMerlinCantSeeNeutralColor?.GetBool() ?? false;
        CanSeeMadmate = Assassin.OptionMerlinCanSeeMadmate?.GetBool() ?? false;
    }
    public static int worktask;
    public int completeroom;

    static bool CanSeeNeutral;
    static bool IsGlayColor;
    static bool CanSeeMadmate;
    static bool OnlyNeutralKiller;

    public override void Add()
    {
        foreach (var impostor in PlayerCatch.AllPlayerControls.Where(player => player.Is(CustomRoleTypes.Impostor) || player.GetCustomRole() is CustomRoles.Egoist))
        {
            NameColorManager.Add(Player.PlayerId, impostor.PlayerId, "#ff1919");
        }
        if (CanSeeNeutral)
        {
            foreach (var neutral in PlayerCatch.AllPlayerControls.Where(player => player.Is(CustomRoleTypes.Neutral) || (player.Is(CustomRoleTypes.Madmate) && CanSeeMadmate)))
            {
                if (!OnlyNeutralKiller || neutral.IsNeutralKiller() || neutral.Is(CustomRoles.GrimReaper))
                    NameColorManager.Add(Player.PlayerId, neutral.PlayerId, IsGlayColor ? "#555555" : neutral.GetRoleColorCode());
            }
        }
        Assassin.MarlinIds.Add(Player.PlayerId);
    }
    int? IRoomTasker.GetMaxTaskCount() => worktask;
    bool IRoomTasker.IsAssignRoomTask() => completeroom != worktask;
    void CheckFin()
    {
        if (MyTaskState.CompletedTasksCount < MyTaskState.AllTasksCount) return;
        UtilsGameLog.AddGameLog("Task", string.Format(Translator.GetString("Taskfin"), UtilsName.GetPlayerColor(Player, true)));
    }
    void IRoomTasker.OnComplete(int completeroom)
    {
        this.completeroom = completeroom;
        SendRPC_CompleteRoom(completeroom);
        CheckFin();
        MyTaskState.Update(Player);
    }
    void IRoomTasker.ChangeRoom(PlainShipRoom TaskRoom)
    {
        SendRPC_ChengeRoom(TaskRoom);
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (isForMeeting || seer != seen) return "";
        return (seer.GetRoleClass() as IRoomTasker)?.GetLowerText(seer, RoleInfo.RoleColorCode) ?? "";
    }
    public void SendRPC_CompleteRoom(int completeroom)
    {
        using var sender = CreateSender();
        sender.Writer.WritePacked((int)RPC_Types.CompleteRoom);
        sender.Writer.Write(completeroom);
    }
    public void SendRPC_ChengeRoom(PlainShipRoom TaskPSR)
    {
        using var sender = CreateSender();
        sender.Writer.WritePacked((int)RPC_Types.ChengeRoom);
        sender.Writer.Write((byte)TaskPSR.RoomId);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        var iroomtasker = Player.GetRoleClass() is IRoomTasker roomTasker ? roomTasker : null;
        switch ((RPC_Types)reader.ReadPackedInt32())
        {
            case RPC_Types.ChengeRoom:
                iroomtasker?.ReceiveRoom(Player.PlayerId, reader);
                break;
            case RPC_Types.CompleteRoom:
                iroomtasker?.ReceiveCompleteRoom(Player.PlayerId, reader);
                var a = MessageReader.Get(reader);
                completeroom = a.ReadInt32();
                MyTaskState.Update(Player);
                break;
        }
    }

    enum RPC_Types
    {
        ChengeRoom,
        CompleteRoom
    }
}