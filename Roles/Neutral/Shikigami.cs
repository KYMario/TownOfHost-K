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
            (6, 1),
            from: From.SuperNewRoles,
            isDesyncImpostor: true
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
        // ★ 陰陽師への矢印
        TargetArrow.Add(Player.PlayerId, ownerId);
        // ★ 名前色
        NameColorManager.Add(Player.PlayerId, ownerId, "#9b59b6");
        SendRPC();
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterCooldown = ShiftCooldown;
        AURoleOptions.ShapeshifterDuration = 99999f;
        opt.SetVision(false);
    }

    public override bool CheckShapeshift(PlayerControl target, ref bool animate)
    {
        animate = false;

        if (OwnerId == byte.MaxValue) return false;
        var owner = GetPlayerById(OwnerId);
        if (owner == null) return false;

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

        return false;
    }

    public float CalculateKillCooldown() => SuicideCooldown;
    public bool CanUseKillButton() => Player.IsAlive();
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => true;

    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        info.DoKill = false;
        PlayerState.GetByPlayerId(Player.PlayerId).DeathReason = CustomDeathReason.Suicide;
        Player.RpcMurderPlayerV2(Player);
    }

    public override void AfterMeetingTasks()
    {
        if (OwnerId == byte.MaxValue) return;
        // ★ 会議後に矢印を再登録
        TargetArrow.Add(Player.PlayerId, OwnerId);
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (!Is(seer) || isForMeeting || !Player.IsAlive()) return "";
        if (seer.PlayerId != seen.PlayerId) return "";

        var result = "";

        // 陰陽師への矢印
        if (OwnerId != byte.MaxValue)
        {
            var owner = GetPlayerById(OwnerId);
            if (owner != null && owner.IsAlive())
                result += $"<color=#9b59b6>{TargetArrow.GetArrows(seer, OwnerId)}</color>";
        }

        // 死体探知
        var deadBodies = ExtendedPlayerControl.GetDeadBodys();
        if (deadBodies.Count > 0)
        {
            var arrows = "";
            foreach (var body in deadBodies)
            {
                var bodyPc = GetPlayerById(body.PlayerId);
                if (bodyPc == null) continue;
                arrows += GetArrow.GetArrows(seer, (Vector3)bodyPc.transform.position);
            }
            if (arrows != "") result += $"<color=#8B4513>{arrows}</color>";
        }

        return result;
    }

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