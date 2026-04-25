using System.Collections.Generic;
using AmongUs.GameOptions;
using Hazel;
using UnityEngine;
using TownOfHost.Patches;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.PlayerCatch;
using static TownOfHost.Utils;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Neutral;

public sealed class Shikigami : RoleBase, IUsePhantomButton, IKillFlashSeeable, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Shikigami),
            player => new Shikigami(player),
            CustomRoles.Shikigami,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Neutral,
            85100,
            SetupOptionItem,
            "sk",
            "#9b59b6",
            (6, 1),
            from: From.SuperNewRoles,
            isDesyncImpostor: true,
            countType: CountTypes.Crew
        );

    public byte OwnerId;
    bool isShifted;
    float suicideCooldownTimer;
    float unresolvedOwnerGraceTimer;
    bool petActionRegistered;
    readonly Dictionary<byte, Vector2> deadBodyPositions;

    enum RPCType
    {
        SyncState,
        AddDeadBodyArrow,
        RemoveDeadBodyArrow,
        ClearDeadBodyArrows
    }

    public Shikigami(PlayerControl player)
        : base(RoleInfo, player, () => HasTask.False)
    {
        OwnerId = byte.MaxValue;
        isShifted = false;
        suicideCooldownTimer = Onmyoji.GetShikigamiSuicideCooldown();
        unresolvedOwnerGraceTimer = 1.5f;
        petActionRegistered = false;
        deadBodyPositions = new();

        EnsurePetActionRegistered();
    }

    private static void SetupOptionItem()
    {
        PavlovDog.HideRoleOptions(CustomRoles.Shikigami);
    }

    public override void OnSpawn(bool initialState)
    {
        if (initialState)
        {
            isShifted = false;
            suicideCooldownTimer = Onmyoji.GetShikigamiSuicideCooldown();
            OwnerId = byte.MaxValue;
            unresolvedOwnerGraceTimer = 1.5f;
            petActionRegistered = false;
            deadBodyPositions.Clear();
        }

        Player.RpcResetAbilityCooldown();

        if (Player.AmOwner && Is(Player))
        {
            Player.SetKillCooldown(Onmyoji.GetShikigamiSuicideCooldown());
        }

        EnsurePetActionRegistered();

        if (OwnerId != byte.MaxValue)
            TargetArrow.Add(Player.PlayerId, OwnerId);
    }

    public override void OnDestroy()
    {
        if (petActionRegistered)
        {
            PetActionManager.Unregister(Player.PlayerId);
            petActionRegistered = false;
        }
        ClearDeadBodyArrows();

        if (OwnerId != byte.MaxValue)
        {
            TargetArrow.Remove(Player.PlayerId, OwnerId);
            NameColorManager.Remove(Player.PlayerId, OwnerId);
        }
    }

    public void SetOwner(byte ownerId)
    {
        if (OwnerId == ownerId) return;

        OwnerId = ownerId;
        unresolvedOwnerGraceTimer = 0f;
        EnsurePetActionRegistered();

        TargetArrow.Add(Player.PlayerId, OwnerId);
        NameColorManager.Add(Player.PlayerId, OwnerId, "#9b59b6");

        SendStateRPC();
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.PhantomCooldown = Onmyoji.GetShikigamiShiftCooldown();
    }

    public override bool CanUseAbilityButton() => true;
    bool IUsePhantomButton.IsPhantomRole => true;
    bool IUsePhantomButton.IsresetAfterKill => false;

    public override string GetAbilityButtonText() => GetString("ShikigamiTransformButtonText");
    public override bool OverrideAbilityButton(out string text)
    {
        text = "ShikigamiTransformButtonText";
        return true;
    }

    public float CalculateKillCooldown() => Onmyoji.GetShikigamiSuicideCooldown();
    public bool CanUseKillButton() => false;
    public bool CanUseSabotageButton() => false;
    public bool CanUseImpostorVentButton() => false;

    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        info.DoKill = false;
    }

    public bool OverrideKillButtonText(out string text)
    {
        text = "Petで自決";
        return true;
    }

    public bool OverrideKillButton(out string text)
    {
        text = "Shikigami_Suicide";
        return true;
    }

    public override void OnFixedUpdate(PlayerControl player)
    {
        if (suicideCooldownTimer > 0f)
        {
            suicideCooldownTimer = Mathf.Max(0f, suicideCooldownTimer - Time.fixedDeltaTime);
        }

        if (player.AmOwner && Is(player))
        {
            Main.AllPlayerKillCooldown[Player.PlayerId] = Onmyoji.GetShikigamiSuicideCooldown();
            Player.killTimer = suicideCooldownTimer;
        }

        EnsurePetActionRegistered();

        if (OwnerId != byte.MaxValue)
            TargetArrow.Add(Player.PlayerId, OwnerId);

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Player.IsAlive()) return;

        if (OwnerId == byte.MaxValue)
        {
            TryResolveOwnerFromOnmyoji();
            if (OwnerId == byte.MaxValue)
                unresolvedOwnerGraceTimer = Mathf.Max(0f, unresolvedOwnerGraceTimer - Time.fixedDeltaTime);
        }

        if (ShouldFollowOwnerDeath())
        {
            FollowOwnerDeath();
            return;
        }
    }

    public bool? CheckKillFlash(MurderInfo info)
    {
        if (!AmongUsClient.Instance.AmHost) return false;
        if (!Player.IsAlive()) return false;

        var dead = info.AppearanceTarget;
        if (dead == null) return false;

        AddDeadBodyArrow(dead.PlayerId, dead.GetTruePosition());
        return false;
    }

    void OnPet()
    {
        if (!Player.IsAlive()) return;

        if (suicideCooldownTimer > 0f)
        {
            SendMessage($"<color=#9b59b6>自決クール中: {Mathf.CeilToInt(suicideCooldownTimer)}秒</color>", Player.PlayerId);
            return;
        }

        suicideCooldownTimer = Onmyoji.GetShikigamiSuicideCooldown();

        CustomRoleManager.OnCheckMurder(Player, Player, Player, Player, true, deathReason: CustomDeathReason.Suicide);
    }

    void IUsePhantomButton.OnClick(ref bool AdjustKillCooldown, ref bool? ResetCooldown)
    {
        AdjustKillCooldown = false;
        ResetCooldown = true;

        if (!Player.IsAlive()) return;

        if (OwnerId == byte.MaxValue)
        {
            SendMessage("<color=#9b59b6>陰陽師が見つかりません。</color>", Player.PlayerId);
            return;
        }

        var owner = GetPlayerById(OwnerId);
        if (owner == null)
        {
            SendMessage("<color=#9b59b6>陰陽師が見つかりません。</color>", Player.PlayerId);
            return;
        }

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

        SendStateRPC();
        UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player);
    }

    public override void OnStartMeeting() => ClearDeadBodyArrows();
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target) => ClearDeadBodyArrows();

    public override void AfterMeetingTasks()
    {
        suicideCooldownTimer = Onmyoji.GetShikigamiSuicideCooldown();
        if (Player.AmOwner && Is(Player))
        {
            Player.SetKillCooldown(Onmyoji.GetShikigamiSuicideCooldown());
        }
        if (OwnerId == byte.MaxValue) return;
        TargetArrow.Add(Player.PlayerId, OwnerId);
    }

    public override void OnLeftPlayer(PlayerControl player)
    {
        if (player == null) return;

        if (player.PlayerId == OwnerId)
        {
            TargetArrow.Remove(Player.PlayerId, OwnerId);
            NameColorManager.Remove(Player.PlayerId, OwnerId);
            OwnerId = byte.MaxValue;
        }

        RemoveDeadBodyArrow(player.PlayerId);
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (!Is(seer) || isForMeeting || !Player.IsAlive()) return "";
        if (seer.PlayerId != seen.PlayerId) return "";

        var result = "";

        if (OwnerId != byte.MaxValue)
        {
            var owner = GetPlayerById(OwnerId);
            if (owner != null && owner.IsAlive())
                result += $"<color=#9b59b6>{TargetArrow.GetArrows(seer, OwnerId)}</color>";
        }

        if (deadBodyPositions.Count > 0)
        {
            var arrows = "";
            foreach (var pos in deadBodyPositions.Values)
                arrows += GetArrow.GetArrows(seer, pos);

            if (arrows != "") result += $"<color=#8B4513>{arrows}</color>";
        }

        return result;
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (isForMeeting || seer.PlayerId != seen.PlayerId || !Is(seer) || !Player.IsAlive()) return "";

        string size = isForHud ? "" : "<size=60%>";
        var cd = Mathf.CeilToInt(Mathf.Max(0f, suicideCooldownTimer));

        if (cd > 0)
        {
            return $"{size}<color=#9b59b6>Pet(ペット撫で): 自決 ({cd}s)</color>";
        }
        
        return $"{size}<color=#9b59b6>Pet(ペット撫で): 自決可能</color>";
    }

    void AddDeadBodyArrow(byte playerId, Vector2 pos)
        => AddDeadBodyArrow(playerId, pos, sync: true);

    void AddDeadBodyArrow(byte playerId, Vector2 pos, bool sync)
    {
        if (deadBodyPositions.TryGetValue(playerId, out var oldPos))
            GetArrow.Remove(Player.PlayerId, oldPos);

        deadBodyPositions[playerId] = pos;
        GetArrow.Add(Player.PlayerId, pos);

        if (sync) RpcAddDeadBodyArrow(playerId, pos);
    }

    void RemoveDeadBodyArrow(byte playerId)
        => RemoveDeadBodyArrow(playerId, sync: true);

    void RemoveDeadBodyArrow(byte playerId, bool sync)
    {
        if (!deadBodyPositions.TryGetValue(playerId, out var pos)) return;

        GetArrow.Remove(Player.PlayerId, pos);
        deadBodyPositions.Remove(playerId);

        if (sync) RpcRemoveDeadBodyArrow(playerId);
    }

    void ClearDeadBodyArrows()
        => ClearDeadBodyArrows(sync: true);

    void ClearDeadBodyArrows(bool sync)
    {
        foreach (var pos in deadBodyPositions.Values)
            GetArrow.Remove(Player.PlayerId, pos);

        deadBodyPositions.Clear();

        if (sync) RpcClearDeadBodyArrows();
    }

    void RpcAddDeadBodyArrow(byte playerId, Vector2 pos)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        using var sender = CreateSender();
        sender.Writer.WritePacked((int)RPCType.AddDeadBodyArrow);
        sender.Writer.Write(playerId);
        NetHelpers.WriteVector2(pos, sender.Writer);
    }

    void RpcRemoveDeadBodyArrow(byte playerId)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        using var sender = CreateSender();
        sender.Writer.WritePacked((int)RPCType.RemoveDeadBodyArrow);
        sender.Writer.Write(playerId);
    }

    void RpcClearDeadBodyArrows()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        using var sender = CreateSender();
        sender.Writer.WritePacked((int)RPCType.ClearDeadBodyArrows);
    }

    void SendStateRPC()
    {
        using var sender = CreateSender();
        sender.Writer.WritePacked((int)RPCType.SyncState);
        sender.Writer.Write(OwnerId);
        sender.Writer.Write(isShifted);
        sender.Writer.Write(suicideCooldownTimer);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        switch ((RPCType)reader.ReadPackedInt32())
        {
            case RPCType.SyncState:
                OwnerId = reader.ReadByte();
                isShifted = reader.ReadBoolean();
                suicideCooldownTimer = reader.ReadSingle();
                break;
            case RPCType.AddDeadBodyArrow:
                var addPlayerId = reader.ReadByte();
                var addPos = NetHelpers.ReadVector2(reader);
                AddDeadBodyArrow(addPlayerId, addPos, sync: false);
                break;
            case RPCType.RemoveDeadBodyArrow:
                RemoveDeadBodyArrow(reader.ReadByte(), sync: false);
                break;
            case RPCType.ClearDeadBodyArrows:
                ClearDeadBodyArrows(sync: false);
                break;
        }
    }

    void EnsurePetActionRegistered()
    {
        if (AmongUsClient.Instance == null) return;
        if (!AmongUsClient.Instance.AmHost) return;
        if (petActionRegistered) return;
        if (Player == null) return;

        PetActionManager.Register(Player.PlayerId, OnPet);
        petActionRegistered = true;
    }

    bool ShouldFollowOwnerDeath()
    {
        if (OwnerId == byte.MaxValue)
        {
            if (unresolvedOwnerGraceTimer > 0f) return false;
            return !HasOnmyojiLink();
        }

        var owner = GetPlayerById(OwnerId);
        if (owner == null) return true;
        if (!owner.IsAlive()) return true;
        if (!owner.Is(CustomRoles.Onmyoji)) return true;

        return false;
    }

    void FollowOwnerDeath()
    {
        if (!Player.IsAlive()) return;

        var state = PlayerState.GetByPlayerId(Player.PlayerId);
        if (state != null)
            state.DeathReason = CustomDeathReason.FollowingSuicide;

        var owner = OwnerId == byte.MaxValue ? null : GetPlayerById(OwnerId);
        Player.SetRealKiller(owner ?? Player);
        Player.RpcMurderPlayerV2(Player);
    }

    bool HasOnmyojiLink()
    {
        foreach (var pc in AllPlayerControls)
        {
            if (pc == null) continue;
            if (pc.GetRoleClass() is not Onmyoji onmyoji) continue;
            if (onmyoji.ShikigamiIds.Contains(Player.PlayerId)) return true;
        }
        return false;
    }

    void TryResolveOwnerFromOnmyoji()
    {
        foreach (var pc in AllPlayerControls)
        {
            if (pc == null) continue;
            if (pc.GetRoleClass() is not Onmyoji onmyoji) continue;
            if (!onmyoji.ShikigamiIds.Contains(Player.PlayerId)) continue;

            SetOwner(pc.PlayerId);
            return;
        }
    }
}