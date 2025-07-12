using AmongUs.GameOptions;
using TownOfHost.Roles.Core;
using UnityEngine;

namespace TownOfHost.Roles.Crewmate;

public sealed class Secom : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Secom),
            player => new Secom(player),
            CustomRoles.Secom,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            999999,  //(仮)
            SetupOptionItem,
            "Seco",
            "#8a99b7",
            (1, 0),
            false
        );
    public Secom(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Secom_Target = 255;
        RemainingMonitoring = OptionMaxMonitoring.GetFloat(); // 初期回数設定
    }
    public float RemainingMonitoring { get; private set; }
    public byte Secom_Target { get; private set; }
    public static OptionItem OptionMaxMonitoring;
    private static void SetupOptionItem()
    {
        OptionMaxMonitoring = FloatOptionItem.Create(RoleInfo, 10, Option.MaxMonitoring, new(0f, 99f, 1f), 1f, false)
            .SetValueFormat(OptionFormat.Times);
    }
    enum Option
    {
        MaxMonitoring, // Secomがキル検知できる回数
    }
    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        // Secom本人かつ、残回数が1以上なら→投票先をSecom_Targetに設定
        if (Is(voter) && RemainingMonitoring >= 1f)
        {
            Secom_Target = votedForId;
        }

        return true;
    }
}