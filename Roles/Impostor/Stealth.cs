using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using UnityEngine;
using HarmonyLib;
using Hazel;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;

public sealed class Stealth : RoleBase, IImpostor, IUsePhantomButton
{
    public Stealth(PlayerControl player) : base(RoleInfo, player)
    {
        excludeImpostors = optionExcludeImpostors.GetBool();
        darkenDuration = darkenTimer = optionDarkenDuration.GetFloat();
        darkenedPlayers = null;
        adddarkenroom.Clear();
    }
    public static readonly SimpleRoleInfo RoleInfo = SimpleRoleInfo.Create(
        typeof(Stealth),
        player => new Stealth(player),
        CustomRoles.Stealth,
        () => optionAddDarkenRoom.GetBool() ? RoleTypes.Phantom : RoleTypes.Impostor,
        CustomRoleTypes.Impostor,
        6100,
        SetupOptionItems,
        "st",
        OptionSort: (6, 6),
        introSound: () => GetIntroSound(RoleTypes.Phantom),
        from: From.TownOfHost);
    private static LogHandler logger = Logger.Handler(nameof(Stealth));

    #region カスタムオプション
    private static BooleanOptionItem optionExcludeImpostors;
    private static FloatOptionItem optionDarkenDuration;
    static OptionItem optionAddDarkenRoom;
    static OptionItem optionmax;
    static OptionItem optioncooldown;
    private enum OptionName { StealthExcludeImpostors, StealthDarkenDuration, StealthAddDarkenRoom, StateAddRoomMax }
    private static void SetupOptionItems()
    {
        optionExcludeImpostors = BooleanOptionItem.Create(RoleInfo, 10, OptionName.StealthExcludeImpostors, true, false);
        optionDarkenDuration = FloatOptionItem.Create(RoleInfo, 20, OptionName.StealthDarkenDuration, new(0.5f, 30f, 0.5f), 1f, false);
        optionDarkenDuration.SetValueFormat(OptionFormat.Seconds);
        optionAddDarkenRoom = BooleanOptionItem.Create(RoleInfo, 23, OptionName.StealthAddDarkenRoom, false, false);
        optionmax = IntegerOptionItem.Create(RoleInfo, 24, OptionName.StateAddRoomMax, (1, 20, 1), 1, false, optionAddDarkenRoom);
        optioncooldown = FloatOptionItem.Create(RoleInfo, 25, GeneralOption.Cooldown, (0, 180, 0.5f), 25f, false, optionAddDarkenRoom).SetValueFormat(OptionFormat.Seconds);
    }
    #endregion

    private bool excludeImpostors;
    private float darkenDuration;
    /// <summary>暗転解除までのタイマー</summary>
    private float darkenTimer;
    /// <summary>今暗転させているプレイヤー 暗転効果が発生してないときはnull</summary>
    private PlayerControl[] darkenedPlayers;
    /// <summary>暗くしている部屋</summary>
    private SystemTypes? darkenedRoom = null;
    List<SystemTypes> adddarkenroom = new();
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        // キルできない，もしくは普通のキルじゃないならreturn
        if (!info.CanKill || !info.DoKill || info.IsSuicide || info.IsAccident || info.IsFakeSuicide)
        {
            return;
        }
        var adddarken = AddDarkRoomPlayer(PlayerCatch.AllAlivePlayerControls);

        var playersToDarken = FindPlayersInSameRoom(info.AttemptTarget, adddarken);

        if (playersToDarken == null)
        {
            logger.Info("部屋の当たり判定を取得できないため暗転を行いません");
            return;
        }
        if (excludeImpostors)
        {
            playersToDarken = playersToDarken.Where(player => !player.Is(CustomRoleTypes.Impostor));
        }
        DarkenPlayers(playersToDarken);
        adddarkenroom.Clear();
    }
    /// <summary>自分と同じ部屋にいるプレイヤー全員を取得する</summary>
    private IEnumerable<PlayerControl> FindPlayersInSameRoom(PlayerControl killedPlayer, IEnumerable<byte> players)
    {
        var room = killedPlayer.GetPlainShipRoom();
        if (room == null)
        {
            if (optionAddDarkenRoom.GetBool() && adddarkenroom?.Count is not null and not 0) return PlayerCatch.AllAlivePlayerControls.Where(player => player != Player && players.Contains(player.PlayerId));
            return null;
        }
        var roomArea = room.roomArea;
        var roomName = room.RoomId;
        RpcDarken(roomName);
        var darkenplayer = PlayerCatch.AllAlivePlayerControls.Where(player => player != Player && player.Collider.IsTouching(roomArea));

        if (optionAddDarkenRoom.GetBool() && adddarkenroom?.Count is not null and not 0)
            darkenplayer = PlayerCatch.AllAlivePlayerControls.Where(player => player != Player && (player.Collider.IsTouching(roomArea) || players.Contains(player.PlayerId)));
        return darkenplayer;
    }
    private IEnumerable<byte> AddDarkRoomPlayer(IEnumerable<PlayerControl> player)
    {
        List<byte> players = new();
        foreach (var pc in player)
        {
            var room = pc.GetPlainShipRoom();
            if (room == null) continue;
            var roomArea = room.roomArea;
            var roomName = room.RoomId;
            if (adddarkenroom.Contains(roomName) && pc.PlayerId != Player.PlayerId) players.Add(pc.PlayerId);
        }
        return players.Where(pc => pc != Player.PlayerId);
    }
    /// <summary>渡されたプレイヤーを<see cref="darkenDuration"/>秒分視界ゼロにする</summary>
    private void DarkenPlayers(IEnumerable<PlayerControl> playersToDarken)
    {
        darkenedPlayers = playersToDarken.ToArray();
        foreach (var player in playersToDarken)
        {
            PlayerState.GetByPlayerId(player.PlayerId).IsBlackOut = true;
            player.MarkDirtySettings();
        }
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost)
        {
            return;
        }
        // 誰かを暗転させているとき
        if (darkenedPlayers != null)
        {
            // タイマーを減らす
            darkenTimer -= Time.fixedDeltaTime;
            // タイマーが0になったらみんなの視界を戻してタイマーと暗転プレイヤーをリセットする
            if (darkenTimer <= 0)
            {
                ResetDarkenState();
            }
        }
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.PhantomCooldown = optioncooldown.GetFloat();
    }
    public void OnClick(ref bool AdjustKillCooldown, ref bool? ResetCooldown)
    {
        AdjustKillCooldown = true;
        ResetCooldown = false;
        var room = Player.GetPlainShipRoom();
        if (room == null) return;
        if (adddarkenroom.Contains(room.RoomId)) return;
        if (optionmax.GetInt() <= adddarkenroom.Count) return;
        ResetCooldown = true;
        adddarkenroom.Add(room.RoomId);
    }
    public override void OnStartMeeting()
    {
        if (AmongUsClient.Instance.AmHost)
        {
            ResetDarkenState();
        }
        adddarkenroom.Clear();
    }
    private void RpcDarken(SystemTypes? roomType)
    {
        logger.Info($"暗転させている部屋を{roomType?.ToString() ?? "null"}に設定");
        darkenedRoom = roomType;
        using var sender = CreateSender();
        sender.Writer.Write((byte?)roomType ?? byte.MaxValue);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        var roomId = reader.ReadByte();
        darkenedRoom = roomId == byte.MaxValue ? null : (SystemTypes)roomId;
    }
    /// <summary>発生している暗転効果を解除</summary>
    private void ResetDarkenState()
    {
        if (darkenedPlayers != null)
        {
            foreach (var player in darkenedPlayers)
            {
                PlayerState.GetByPlayerId(player.PlayerId).IsBlackOut = false;
                player.MarkDirtySettings();
            }
            darkenedPlayers = null;
        }
        darkenTimer = darkenDuration;
        RpcDarken(null);
        UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player);
    }

    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        // 会議中，自分のSuffixじゃない，どこも暗転させてなければ何も出さない
        if (isForMeeting || seer != Player || seen != Player || !darkenedRoom.HasValue)
        {
            return base.GetSuffix(seer, seen, isForMeeting);
        }
        var addroom = "";
        adddarkenroom.Do(room => addroom += $",{DestroyableSingleton<TranslationController>.Instance.GetString(room)}");
        return string.Format(GetString("StealthDarkened"), DestroyableSingleton<TranslationController>.Instance.GetString(darkenedRoom.Value) + addroom);
    }
    public override string GetAbilityButtonText() => GetString("StealthAbility");
    public override bool OverrideAbilityButton(out string text)
    {
        text = "Stealth_ability";
        return true;
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (seen.PlayerId != seer.PlayerId || !Player.IsAlive() || isForMeeting || optionmax.GetInt() <= adddarkenroom.Count || !optionAddDarkenRoom.GetBool()) return "";

        if (isForHud) return GetString("StealthLowerInfo");
        return $"<size=50%>{GetString("StealthLowerInfo")}</size>";
    }
}
