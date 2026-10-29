using AmongUs.GameOptions;
using Hazel;
using UnityEngine;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using System.Collections.Generic;

namespace TownOfHost.Roles.Impostor;

public sealed class Bloodlust : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Bloodlust),
            player => new Bloodlust(player),
            CustomRoles.Bloodlust,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            25300,
            SetupOptionItem,
            "bl",
            OptionSort: (8, 3),
            Desc: () => string.Format(GetString("BloodlustDesc"), OptionTimeUntilRed.GetFloat())
        );

    public Bloodlust(PlayerControl player)
        : base(RoleInfo, player)
    {
        Bloodlusts = new();
    }
    static HashSet<Bloodlust> Bloodlusts;
    static OptionItem OptionKillCooldown;
    static OptionItem OptionTimeUntilRed;

    enum OptionName
    {
        BloodlustTimeUntilRed,
    }

    float _elapsedSinceReset;
    int _lastElapsedHalfSecondSent = -1;
    public bool IsNameExposed { get; private set; }

    private static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 0.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionTimeUntilRed = FloatOptionItem.Create(RoleInfo, 11, OptionName.BloodlustTimeUntilRed, new(5f, 600f, 1f), 45f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }

    public float CalculateKillCooldown() => OptionKillCooldown.GetFloat();

    public override void ApplyGameOptions(IGameOptions opt)
    {
        if (IsNameExposed || !Player.IsAlive())
        {
            AURoleOptions.ShapeshifterCooldown = 255f;
        }
        else
        {
            AURoleOptions.ShapeshifterCooldown = OptionTimeUntilRed.GetFloat() + Main.LagTime;
        }
        AURoleOptions.ShapeshifterDuration = 1f;
    }

    public override bool CheckShapeshift(PlayerControl target, ref bool shouldAnimate)
    {
        shouldAnimate = false;
        return false;
    }

    public override bool CanUseAbilityButton() => Player.IsAlive();

    public override string GetAbilityButtonText()
    {
        if (IsNameExposed) return GetString("BloodlustAbilityExposed");
        return GetString("BloodlustAbilityTimer");
    }

    public override void Add()
    {
        CustomRoleManager.MarkOthers.Add(OtherMark);
        if (AmongUsClient.Instance.AmHost)
            ResetTimerAndExposeState(sendRpc: true);
        Bloodlusts.Add(this);
    }

    public override void StartGameTasks()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        ResetTimerAndExposeState(sendRpc: true);
    }

    public override void AfterMeetingTasks()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        ResetTimerAndExposeState(sendRpc: true);
    }

    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost || !Player.IsAlive() || IsNameExposed) return;
        if (GameStates.IsMeeting) return;

        _elapsedSinceReset += Time.fixedDeltaTime;
        if (_elapsedSinceReset >= OptionTimeUntilRed.GetFloat())
        {
            SetExposed();
            return;
        }

        var halfSecTick = Mathf.FloorToInt(_elapsedSinceReset * 2f);
        if (halfSecTick != _lastElapsedHalfSecondSent)
        {
            _lastElapsedHalfSecondSent = halfSecTick;
            SendRPC();
        }
    }

    void IKiller.OnMurderPlayerAsKiller(MurderInfo info)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!Is(info.AttemptKiller)) return;
        if (!info.IsCanKilling || info.IsFakeSuicide || info.IsSuicide) return;

        ResetTimerAndExposeState(sendRpc: true);
    }

    void ResetTimerAndExposeState(bool sendRpc)
    {
        _elapsedSinceReset = 0f;
        _lastElapsedHalfSecondSent = -1;
        if (IsNameExposed)
        {
            IsNameExposed = false;
        }
        if (sendRpc)
        {
            SendRPC();
            _lastElapsedHalfSecondSent = Mathf.FloorToInt(_elapsedSinceReset * 2f);
        }
        // 先に設定秒数へ同期してからクールダウンを戻さないと、残り時間のままリセットされる
        Player?.RpcResetAbilityCooldown(Sync: true);
        UtilsNotifyRoles.NotifyRoles();
    }

    void SetExposed()
    {
        if (IsNameExposed) return;
        IsNameExposed = true;
        SendRPC();
        _lastElapsedHalfSecondSent = Mathf.FloorToInt(_elapsedSinceReset * 2f);
        Player?.MarkDirtySettings();
        UtilsNotifyRoles.NotifyRoles();
    }
    public static string OtherMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (isForMeeting || seen.IsAlive() is false) return "";
        foreach (var blood in Bloodlusts)
        {
            if (blood.IsNameExposed && seen.PlayerId == blood.Player.PlayerId)
            {
                return "<#ff1919>★</color>";
            }
        }
        return "";
    }

    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(IsNameExposed);
        sender.Writer.Write(_elapsedSinceReset);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        IsNameExposed = reader.ReadBoolean();
        _elapsedSinceReset = reader.ReadSingle();
        Player?.MarkDirtySettings();
    }
}
