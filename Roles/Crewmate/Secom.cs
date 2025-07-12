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
    }
    private static void SetupOptionItem()
    {
    }
}