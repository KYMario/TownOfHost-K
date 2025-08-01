using System.Collections.Generic;
using UnityEngine;
using Hazel;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Modules;

namespace TownOfHost.Roles.Neutral;

public sealed class Arsonist : RoleBase, IKiller, IUsePhantomButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Arsonist),
            player => new Arsonist(player),
            CustomRoles.Arsonist,
            () => Optionfire.GetBool() ? RoleTypes.Phantom : RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            13800,
            SetupOptionItem,
            "ar",
            "#ff6633",
            (3, 0),
            true,
            introSound: () => GetIntroSound(RoleTypes.Crewmate),
            from: From.TownOfUs
        );
    public Arsonist(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        DouseTime = OptionDouseTime.GetFloat();
        DouseCooldown = OptionDouseCooldown.GetFloat();
        if (OptionDouseCooldown.GetFloat() == 0) DouseCooldown = 0.00000000000000000001f;//0sでも塗れるように
        Distance = OptionDistance.GetFloat();

        TargetInfo = null;
        IsDoused = new(GameData.Instance.PlayerCount);
    }
    private static OptionItem OptionDouseTime;
    private static OptionItem OptionDouseCooldown;
    private static OptionItem OptionDistance;
    private static OptionItem Optionfire;
    private static OptionItem OptionCanUseVent;
    private static OptionItem OptionCanSeeNowAlivePlayer;

    enum OptionName
    {
        ArsonistDouseTime, ArsonistRange, ArsonistFireOnclick, ArsonistCanSeeAllplayer
    }
    private static float DouseTime;
    private static float DouseCooldown;
    private static float Distance;

    public class TimerInfo
    {
        public byte TargetId;
        public float Timer;
        public TimerInfo(byte targetId, float timer)
        {
            TargetId = targetId;
            Timer = timer;
        }
    }
    public bool CanKill { get; private set; } = false;
    private TimerInfo TargetInfo;
    public Dictionary<byte, bool> IsDoused;

    private static void SetupOptionItem()
    {
        SoloWinOption.Create(RoleInfo, 15, defo: 1);
        OptionCanSeeNowAlivePlayer = BooleanOptionItem.Create(RoleInfo, 8, OptionName.ArsonistCanSeeAllplayer, false, false);
        OptionCanUseVent = BooleanOptionItem.Create(RoleInfo, 9, GeneralOption.CanVent, false, false);
        OptionDouseTime = FloatOptionItem.Create(RoleInfo, 10, OptionName.ArsonistDouseTime, new(0.5f, 10f, 0.5f), 3f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionDouseCooldown = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.Cooldown, new(0f, 180f, 1f), 10f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionDistance = FloatOptionItem.Create(RoleInfo, 12, OptionName.ArsonistRange, new(1.25f, 5f, 0.25f), 1.75f, false)
        .SetValueFormat(OptionFormat.Multiplier);
        OverrideKilldistance.Create(RoleInfo, 13);
        Optionfire = BooleanOptionItem.Create(RoleInfo, 14, OptionName.ArsonistFireOnclick, false, false);
    }
    public override void Add()
    {
        foreach (var ar in PlayerCatch.AllPlayerControls)
        {
            IsDoused.Add(ar.PlayerId, false);

            if (SuddenDeathMode.NowSuddenDeathTemeMode)
            {
                if (SuddenDeathMode.IsSameteam(ar.PlayerId, Player.PlayerId))
                    IsDoused[ar.PlayerId] = true;
            }
        }
    }
    public override bool NotifyRolesCheckOtherName => true;
    public bool CanUseKillButton() => !IsDouseDone(Player);
    public bool CanUseImpostorVentButton() => IsDouseDone(Player) || OptionCanUseVent.GetBool();
    public float CalculateKillCooldown() => DouseCooldown;
    public bool CanUseSabotageButton() => false;
    public override string GetProgressText(bool comms = false, bool gamelog = false)
    {
        var doused = GetDousedPlayerCount();
        var Denominator = "?";
        if (OptionCanSeeNowAlivePlayer.GetBool() || GameStates.CalledMeeting) Denominator = $"{doused.Item2}";
        return Utils.ColorString(RoleInfo.RoleColor.ShadeColor(0.25f), $"({doused.Item1}/{Denominator})");
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(false);
        AURoleOptions.PhantomCooldown = IsDouseDone(Player) ? 1f : 255f;
    }
    enum RPC_type
    {
        SetDousedPlayer,
        SetCurrentDousingTarget
    }
    private void SendRPC(RPC_type rpcType, byte targetId = byte.MaxValue, bool isDoused = false)
    {
        using var sender = CreateSender();
        sender.Writer.Write(targetId);
        sender.Writer.Write((byte)rpcType);
        if (rpcType == RPC_type.SetDousedPlayer)
            sender.Writer.Write(isDoused);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        var targetId = reader.ReadByte();
        var rpcType = (RPC_type)reader.ReadByte();
        switch (rpcType)
        {
            case RPC_type.SetDousedPlayer:
                bool doused = reader.ReadBoolean();
                IsDoused[targetId] = doused;
                break;
            case RPC_type.SetCurrentDousingTarget:
                TargetInfo = new(targetId, 0f);
                break;
        }
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;

        Logger.Info("Arsonist start douse", "OnCheckMurderAsKiller");
        killer.SetKillCooldown(DouseTime);
        if (!IsDoused[target.PlayerId] && TargetInfo == null)
        {
            TargetInfo = new(target.PlayerId, 0f);
            UtilsNotifyRoles.NotifyRoles(SpecifySeer: killer);
            SendRPC(RPC_type.SetCurrentDousingTarget, target.PlayerId);
        }
        info.DoKill = false;
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        TargetInfo = null;
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        if (GameStates.IsInTask && TargetInfo != null)//アーソニストが誰かを塗っているとき
        {
            if (!Player.IsAlive())
            {
                TargetInfo = null;
                UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player);
                SendRPC(RPC_type.SetCurrentDousingTarget);
            }
            else
            {
                var ar_target = PlayerCatch.GetPlayerById(TargetInfo.TargetId);//塗られる人
                var ar_time = TargetInfo.Timer;//塗った時間
                if (!ar_target.IsAlive())
                {
                    TargetInfo = null;
                }
                else if (ar_time >= DouseTime)//時間以上一緒にいて塗れた時
                {
                    Player.SetKillCooldown();
                    TargetInfo = null;//塗が完了したのでTupleから削除
                    IsDoused[ar_target.PlayerId] = true;//塗り完了
                    SendRPC(RPC_type.SetDousedPlayer, ar_target.PlayerId, true);
                    UtilsNotifyRoles.NotifyRoles();//名前変更
                    SendRPC(RPC_type.SetCurrentDousingTarget);

                    Player.RpcResetAbilityCooldown(Sync: true);
                }
                else
                {
                    float dis;
                    dis = Vector2.Distance(Player.transform.position, ar_target.transform.position);//距離を出す
                    if (dis <= Distance)//一定の距離にターゲットがいるならば時間をカウント
                    {
                        TargetInfo.Timer += Time.fixedDeltaTime;
                    }
                    else//それ以外は削除
                    {
                        TargetInfo = null;
                        UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player);
                        Player.SetKillCooldown(0.1f, force: true);
                        SendRPC(RPC_type.SetCurrentDousingTarget);

                        Logger.Info($"Canceled: {Player.GetNameWithRole().RemoveHtmlTags()}", "Arsonist");
                    }
                }
            }
        }
    }
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (GameStates.IsInGame && IsDouseDone(Player) && !Optionfire.GetBool())
        {
            foreach (var pc in PlayerCatch.AllAlivePlayerControls)
            {
                if (pc.PlayerId != Player.PlayerId)
                {
                    //生存者は焼殺
                    pc.SetRealKiller(Player);
                    pc.RpcMurderPlayer(pc);
                    var state = PlayerState.GetByPlayerId(pc.PlayerId);
                    state.DeathReason = CustomDeathReason.Torched;
                    state.SetDead();
                }
                else
                    RPC.PlaySoundRPC(pc.PlayerId, Sounds.KillSound);
            }
            if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Arsonist, Player.PlayerId))
            {
                CustomWinnerHolder.NeutralWinnerIds.Add(Player.PlayerId);
            }

            return false;
        }
        return OptionCanUseVent.GetBool();
    }
    public bool OverrideKillButtonText(out string text)
    {
        text = GetString("ArsonistDouseButtonText");
        return true;
    }
    public bool OverrideKillButton(out string text)
    {
        text = "Arsonist_Kill";
        return true;
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;

        if (IsDousedPlayer(seen.PlayerId)) //seerがtargetに既にオイルを塗っている(完了)
            return Utils.ColorString(RoleInfo.RoleColor, "▲");
        if (!isForMeeting && TargetInfo?.TargetId == seen.PlayerId) //オイルを塗っている対象がtarget
            return Utils.ColorString(RoleInfo.RoleColor, "△");

        return "";
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (isForMeeting) return "";
        //seenが省略の場合seer
        seen ??= seer;
        //seeおよびseenが自分である場合以外は関係なし
        if (!Is(seer) || !Is(seen)) return "";

        return IsDouseDone(Player) ? Utils.ColorString(RoleInfo.RoleColor, GetString(Optionfire.GetBool() ? "UseOnclick" : "EnterVentToWin")) : "";
    }
    public bool IsDousedPlayer(byte targetId) => IsDoused.TryGetValue(targetId, out bool isDoused) && isDoused;
    public static bool IsDouseDone(PlayerControl player)
    {
        if (player.GetRoleClass() is not Arsonist arsonist) return false;
        var count = arsonist.GetDousedPlayerCount();
        return count.Item1 == count.Item2;
    }
    public (int, int) GetDousedPlayerCount()
    {
        int doused = 0, all = 0;
        //多分この方がMain.isDousedでforeachするより他のアーソニストの分ループ数少なくて済む
        foreach (var pc in PlayerCatch.AllAlivePlayerControls)
        {
            if (pc.PlayerId == Player.PlayerId) continue; //アーソニストは除外

            all++;
            if (IsDoused.TryGetValue(pc.PlayerId, out var isDoused) && isDoused)
                //塗れている場合
                doused++;
        }

        return (doused, all);
    }
    void IUsePhantomButton.OnClick(ref bool AdjustKillCooldown, ref bool? ResetCooldown)
    {
        if (GameStates.IsInGame && IsDouseDone(Player) && Optionfire.GetBool())
        {
            foreach (var pc in PlayerCatch.AllAlivePlayerControls)
            {
                if (pc.PlayerId != Player.PlayerId)
                {
                    //生存者は焼殺
                    pc.SetRealKiller(Player);
                    pc.RpcMurderPlayer(pc);
                    var state = PlayerState.GetByPlayerId(pc.PlayerId);
                    state.DeathReason = CustomDeathReason.Torched;
                    state.SetDead();
                }
                else
                    RPC.PlaySoundRPC(pc.PlayerId, Sounds.KillSound);
            }
            if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Arsonist, Player.PlayerId))
            {
                CustomWinnerHolder.NeutralWinnerIds.Add(Player.PlayerId);
            }
        }
    }
}