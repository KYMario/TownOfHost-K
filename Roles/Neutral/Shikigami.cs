using System.Linq;
using AmongUs.GameOptions;
using Hazel;
using UnityEngine;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.PlayerCatch;
using static TownOfHost.Utils;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Neutral;

public sealed class Shikigami : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Shikigami),
            player => new Shikigami(player),
            CustomRoles.Shikigami,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Neutral,
            30100,
            SetupOptionItem,
            "sk",
            "#9b59b6",
            from: From.SuperNewRoles
        );

    public Shikigami(PlayerControl player)
    : base(RoleInfo, player, () => HasTask.False)
    {
        ShiftCooldown = OptShiftCooldown.GetFloat();
        SuicideCooldown = OptSuicideCooldown.GetFloat();
        OwnerId = byte.MaxValue;
        isShifted = false;
    }

    static OptionItem OptShiftCooldown;
    static OptionItem OptSuicideCooldown;
    static float ShiftCooldown;
    static float SuicideCooldown;

    public byte OwnerId;
    bool isShifted;

    enum OptionName
    {
        ShikigamiShiftCooldown,
        ShikigamiSuicideCooldown,
    }

    private static void SetupOptionItem()
    {
        OptShiftCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.ShikigamiShiftCooldown, new(0f, 60f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptSuicideCooldown = FloatOptionItem.Create(RoleInfo, 11, OptionName.ShikigamiSuicideCooldown, new(0f, 60f, 2.5f), 10f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }

    public void SetOwner(byte ownerId)
    {
        OwnerId = ownerId;
        SendRPC();
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterCooldown = ShiftCooldown;
        AURoleOptions.ShapeshifterDuration = 99999f; // 時間無制限
    }

    // 変身：陰陽師の姿に
    public override bool CheckShapeshift(PlayerControl target, ref bool animate)
    {
        animate = false; // アニメーション不要

        if (OwnerId == byte.MaxValue) return false;
        var owner = GetPlayerById(OwnerId);
        if (owner == null) return false;

        // 陰陽師の姿に変身
        if (!isShifted)
        {
            isShifted = true;
            Player.RpcShapeshift(owner, false);
        }
        else
        {
            isShifted = false;
            Player.RpcShapeshift(Player, false);
        }

        return false; // 変身処理は自前で行うので false
    }

    // 自決：キルボタンで自殺
    public float CalculateKillCooldown() => SuicideCooldown;
    public bool CanUseKillButton() => Player.IsAlive();
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => false;

    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        // 自分自身にのみ使用可能
        if (killer.PlayerId != target.PlayerId)
        {
            info.DoKill = false;
            return;
        }
        // 自決
        PlayerState.GetByPlayerId(Player.PlayerId).DeathReason = CustomDeathReason.Suicide;
        info.DoKill = true;
    }

    // 死体探知：茶色矢印
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (!Is(seer) || isForMeeting || !Player.IsAlive()) return "";
        if (seer.PlayerId != seen.PlayerId) return "";

        var deadBodies = ExtendedPlayerControl.GetDeadBodys();
        if (deadBodies.Count == 0) return "";

        var arrows = "";
        foreach (var body in deadBodies)
        {
            var bodyPc = GetPlayerById(body.PlayerId);
            if (bodyPc == null) continue;
            arrows += GetArrow.GetArrows(seer, bodyPc.transform.position);
        }
        return arrows == "" ? "" : $"<color=#8B4513>{arrows}</color>";
    }

    // 陰陽師探知：役職色矢印
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (!Is(seer) || isForMeeting || !Player.IsAlive()) return "";
        if (seer.PlayerId != seen.PlayerId) return "";
        if (OwnerId == byte.MaxValue) return "";

        var owner = GetPlayerById(OwnerId);
        if (owner == null || !owner.IsAlive()) return "";

        var arrow = GetArrow.GetArrows(seer, owner.GetTruePosition());
        return $"<color=#9b59b6>{arrow}</color>";
    }

    // クルー陣営として判定させるためのミスidentify
    public override CustomRoles Misidentify() => CustomRoles.Crewmate;

    public void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(OwnerId);
        sender.Writer.Write(isShifted);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        OwnerId = reader.ReadByte();
        isShifted = reader.ReadBoolean();
    }

    public bool OverrideKillButtonText(out string text)
    {
        text = GetString("ShikigamiSuicideButtonText");
        return true;
    }

    public bool OverrideKillButton(out string text)
    {
        text = "Shikigami_Suicide";
        return true;
    }
}