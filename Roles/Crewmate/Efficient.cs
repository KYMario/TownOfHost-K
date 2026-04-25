using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Hazel;
using TownOfHost.Roles.Core;
using UnityEngine;

namespace TownOfHost.Roles.Crewmate;

public sealed class Efficient : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Efficient),
            player => new Efficient(player),
            CustomRoles.Efficient,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            11700,
            SetupOptionItem,
            "ef",
            "#a68b96",
            (7, 2)
        );
    public Efficient(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Cooldown = 0f;
    }
    enum Option { EfficientCollectRect }
    static OptionItem CollectRect;
    float Cooldown;
    private static void SetupOptionItem()
    {
        CollectRect = FloatOptionItem.Create(RoleInfo, 10, Option.EfficientCollectRect, new(0, 100, 1), 15, false).SetValueFormat(OptionFormat.Percent);
        OverrideTasksData.Create(RoleInfo, 11);
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        Cooldown -= Time.fixedDeltaTime;
    }
    public override bool OnCompleteTask(uint taskid)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        if (MyTaskState.CompletedTasksCount >= MyTaskState.AllTasksCount) return false;
        if (Cooldown > 0f) return true;

        int chance = IRandom.Instance.Next(1, 101);

        if (CollectRect.GetFloat() > chance)
        {
            if (Cooldown > 0f) return true;

            Cooldown = 3;
            MyTaskState.Update(Player);
            Player.RpcProtectedMurderPlayer();
            Logger.Info($"{Player.name} => 効率化成功!タスクを一個減らすぞ!", "Efficient");
            SendRpc();
        }
        return true;
    }
    void SendRpc()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        using var sender = CreateSender();
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        MyTaskState.Update(Player);
    }
}