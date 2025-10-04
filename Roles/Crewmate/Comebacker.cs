using System.Linq;
using System.Collections.Generic;

using AmongUs.GameOptions;
using UnityEngine;
using HarmonyLib;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;

public sealed class Comebacker : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Comebacker),
            player => new Comebacker(player),
            CustomRoles.Comebacker,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            12200,
            SetupOptionItem,
            "cb",
            "#ff9966",
            (9, 0)
        );
    public Comebacker(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Cooldown = OptionCooldown.GetFloat();
        OldPosition = new(999f, 999f);
        ComebackPosString = "";
    }
    private static OptionItem OptionCooldown;
    enum OptionName
    {
        Cooldown
    }
    private static float Cooldown;
    private Vector2 OldPosition;
    private string ComebackPosString;
    private static void SetupOptionItem()
    {
        OptionCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.Cooldown, new(0f, 180f, 0.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = Cooldown;
        AURoleOptions.EngineerInVentMaxTime = 1.5f;
    }
    public override bool CanVentMoving(PlayerPhysics physics, int ventId) => false;
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (OldPosition != new Vector2(999f, 999f))
        {
            var tp = OldPosition;
            _ = new LateTask(() =>
            {
                Player.RpcSnapToForced(tp + new Vector2(0f, 0.1f));
                Logger.Info("ベントに飛ぶよ!", "Comebacker");
            }, 1f, "TP");
        }
        ShipStatus.Instance.AllVents.DoIf(vent => vent.Id == ventId, vent => OldPosition = (Vector2)vent.transform.position);
        Logger.Info("ベントを設定するよ!", "Comebacker");

        ComebackPosString = Player.GetShipRoomName();

        UtilsNotifyRoles.NotifyRoles(Player, OnlyMeName: true);
        return true;
    }
    public override string GetAbilityButtonText() => GetString("CamebackerAbility");
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;

        if (isForMeeting || !Player.IsAlive() || ComebackPosString == "") return "";

        if (isForHud) return $"<color={RoleInfo.RoleColorCode}>{string.Format(GetString("ComebackLowerText"), ComebackPosString)}</color>";
        return $"<size=50%><color={RoleInfo.RoleColorCode}>{string.Format(GetString("ComebackLowerText"), ComebackPosString)}</color></size>";
    }
}
