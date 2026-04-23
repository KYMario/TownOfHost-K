using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using Hazel;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Crewmate;

public sealed class Walker : RoleBase, IRoomTasker
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Walker),
            player => new Walker(player),
            CustomRoles.Walker,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            12100,
            SetupOptionItem,
            "wa",
            "#057a2c",
            (8, 2),
            from: From.TownOfHost_K
        );
    public Walker(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        completeroom = 0;
    }
    enum OptionName
    {
        WalkerWalkTaskCount
    }
    int completeroom;
    public static OptionItem WalkTaskCount;
    static void SetupOptionItem()
    {
        WalkTaskCount = IntegerOptionItem.Create(RoleInfo, 10, OptionName.WalkerWalkTaskCount, (1, 99, 1), 5, false);
        OverrideTasksData.Create(RoleInfo, 15, tasks: (true, 1, 0, 0));
    }
    int? IRoomTasker.GetMaxTaskCount() => WalkTaskCount.GetInt();
    bool IRoomTasker.IsAssignRoomTask() => completeroom != WalkTaskCount.GetInt();
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
        Achievements.RpcCompleteAchievement(Player.PlayerId, 1, achievements[0]);
        Achievements.RpcCompleteAchievement(Player.PlayerId, 1, achievements[1]);
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
    public static System.Collections.Generic.Dictionary<int, Achievement> achievements = new();
    [Attributes.PluginModuleInitializer]
    public static void Load()
    {
        var n1 = new Achievement(RoleInfo, 0, 10, 0, 0);
        var l1 = new Achievement(RoleInfo, 1, 100, 0, 1);
        achievements.Add(0, n1);
        achievements.Add(1, l1);
    }
}