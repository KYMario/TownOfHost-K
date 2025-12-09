using AmongUs.GameOptions;
using System;
using System.Linq;
using UnityEngine;

using TownOfHost.Roles.Core;
using Hazel;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Neutral;

public sealed class Turncoat : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Turncoat),
            player => new Turncoat(player),
            CustomRoles.Turncoat,
            () => OptionCanShapeShift.GetBool() ? RoleTypes.Shapeshifter : RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            15500,
            SetupOptionItem,
            "Tu",
            "#371a1a",
            (7, 1)
        );
    public Turncoat(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        cooldown = OptionCooldown.GetFloat();
        duration = OptionCooldown.GetFloat();
    }
    static OptionItem OptionCanTargetImpostor;
    static OptionItem OptionCanTargetNeutral;
    static OptionItem OptionCanTargetMadmate;
    static OptionItem OptionCanShapeShift; static OptionItem OptionCooldown; static OptionItem OptionDuration;
    static float cooldown; static float duration;

    enum OptionName
    {
        TurncoatCanTargetImpostor,
        TurncoatCanTargetMadmate,
        TurncoatCanTargetNeutral
    }
    byte targetid;
    bool IsTargetDied;
    string TargetColorcode;

    private static void SetupOptionItem()
    {
        OptionCanShapeShift = BooleanOptionItem.Create(RoleInfo, 13, "JesterCanUseShapeshift", false, false);
        OptionCooldown = FloatOptionItem.Create(RoleInfo, 14, GeneralOption.Cooldown, new(0f, 180f, 0.5f), 30f, false, OptionCanShapeShift)
                .SetValueFormat(OptionFormat.Seconds);
        OptionDuration = FloatOptionItem.Create(RoleInfo, 15, GeneralOption.Duration, new(0f, 180f, 0.5f), 5f, false, OptionCanShapeShift)
                .SetZeroNotation(OptionZeroNotation.Infinity)
                .SetValueFormat(OptionFormat.Seconds);
        OptionCanTargetImpostor = BooleanOptionItem.Create(RoleInfo, 10, OptionName.TurncoatCanTargetImpostor, false, false);
        OptionCanTargetMadmate = BooleanOptionItem.Create(RoleInfo, 11, OptionName.TurncoatCanTargetMadmate, false, false);
        OptionCanTargetNeutral = BooleanOptionItem.Create(RoleInfo, 12, OptionName.TurncoatCanTargetNeutral, false, false);

        RoleAddAddons.Create(RoleInfo, 20);
    }
    //ターゲットの選択
    public override void Add()
    {
        targetid = byte.MaxValue;
        IsTargetDied = false;

        if (!AmongUsClient.Instance.AmHost) return;

        //設定に準じた奴。
        var list = PlayerCatch.AllPlayerControls.Where(pc =>
        {
            if (pc.PlayerId == Player.PlayerId) return false;
            if (pc.Is(CustomRoles.GM)) return false;
            if (pc.Is(CustomRoles.Turncoat)) return false;

            var role = pc.GetCustomRole().GetCustomRoleTypes();

            return role switch
            {
                CustomRoleTypes.Crewmate => true,
                CustomRoleTypes.Impostor => OptionCanTargetImpostor.GetBool(),
                CustomRoleTypes.Madmate => OptionCanTargetMadmate.GetBool(),
                CustomRoleTypes.Neutral => OptionCanTargetNeutral.GetBool(),
                _ => false
            };
        });

        //もし仮にターゲットリストが空の場合、自身以外全員いれる。
        if (!list.Any())
            list = PlayerCatch.AllPlayerControls.Where(pc => pc.PlayerId != Player.PlayerId && !pc.Is(CustomRoles.GM));

        //それでも0の場合、ターゲット無しにする
        if (!list.Any())
        {
            Logger.Error($"{Player?.Data?.GetLogPlayerName() ?? "???"}のターゲットが存在しません", "Turncoat");
            return;
        }

        //シャッフル！
        list = list.OrderBy(_ => Guid.NewGuid()).ToArray();

        //リストの中からランダムでプレイヤーを選び、その人をターゲットに
        PlayerControl RandomPlayer = list.ToArray()[IRandom.Instance.Next(list.Count())];
        targetid = RandomPlayer.PlayerId;
        TargetColorcode = Palette.PlayerColors[RandomPlayer.cosmetics.ColorId].ColorCode();
        Logger.Info($"{Player?.Data?.GetLogPlayerName() ?? "???"} => {RandomPlayer?.Data?.GetLogPlayerName() ?? "???"}", "Turncoat");
        SendRPC();
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost || AntiBlackout.IsCached || IsTargetDied || !Player.IsAlive()) return;

        PlayerControl target = PlayerCatch.GetPlayerById(targetid);

        //回線切断時。いくらなんでもなのでオポチュに
        if (target?.Data?.Disconnected is true or null)
        {
            Player.RpcSetCustomRole(CustomRoles.Opportunist, true, true);
            return;
        }
        //死亡時
        if (!target.IsAlive())
        {
            IsTargetDied = true;
            SendRPC();
            UtilsNotifyRoles.NotifyRoles(Player);
        }
    }
    public override void CheckWinner()
    {
        //生きてないなら負け。
        if (!Player.IsAlive()) return;

        if (targetid.GetPlayerControl() is null)//相手が回線落ちの場合の特別処理
        {
            Logger.Info($"{targetid} is null.", "Turncoat_Wincheck");
            var role = targetid.GetPlayerState().MainRole;
            var CountTypes = role.GetRoleInfo()?.CountType;
            var subroles = targetid.GetPlayerState().SubRoles;
            if (subroles.Any(role => role is CustomRoles.Amanojaku))
            {
                if (CustomWinnerHolder.WinnerTeam is CustomWinner.Crewmate)
                {
                    Win();
                }
                return;
            }

            if (role.IsCrewmate() && CustomWinnerHolder.WinnerTeam is CustomWinner.Crewmate) return;
            if ((CountTypes == TownOfHost.CountTypes.Jackal || role is CustomRoles.Jackaldoll) && CustomWinnerHolder.WinnerTeam is CustomWinner.Jackal) return;
            if (role.IsImpostorTeam() && CustomWinnerHolder.WinnerTeam is CustomWinner.Impostor) return;
            //↑陣営勝利しているかのチェック ↓ 単独勝利してるのかのチェック。
            if ((CustomWinner)role == CustomWinnerHolder.WinnerTeam && //念のため役職名とWinnerTeamが同じなのかのチェック
            role.ToString() == CustomWinnerHolder.WinnerTeam.ToString()) return;

            //どこかで除外されていない場合、勝利
            Win();
            return;
        }

        //勝利IDに含まれていないかつ、勝利役職に含まれてない場合 → かち！
        if (!CustomWinnerHolder.WinnerIds.Contains(targetid)
        && !CustomWinnerHolder.WinnerRoles.Contains(targetid.GetPlayerControl()?.GetCustomRole() ?? CustomRoles.Emptiness))
        {
            Win();
            return;
        }

        //何が何でも負けるリストに含まれている場合 → かち！
        if (CustomWinnerHolder.CantWinPlayerIds.Contains(targetid))
        {
            Win();
            return;
        }
        //上記2つに含まれないなら負け。
        return;
    }
    public void Win()
    {
        CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.Turncoat);
        CustomWinnerHolder.WinnerIds.Add(Player.PlayerId);
    }
    //死亡時だけ役職情報を開示する
    //スタンダードなら白位置にねじ込んでキルさせる、強引に吊りに行く等誘導してもらいたい
    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, ref bool enabled, ref Color roleColor, ref string roleText, ref bool addon)
    {
        if (seen.PlayerId == targetid && IsTargetDied)
        {
            enabled = true;
            addon |= false;
        }
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;

        if (Is(seer) && seen.PlayerId == targetid) return $"<color={RoleInfo.RoleColorCode}>★</color>";
        //if (seer.PlayerId == seen.PlayerId) return $"<color={TargetColorcode}>★</color>";

        return "";
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (!Player.IsAlive() || isForMeeting) return "";

        var targetstate = PlayerState.GetByPlayerId(targetid);
        var targetname = UtilsName.GetPlayerColor(targetid, true);
        if (IsTargetDied)
        {
            var role = targetstate.MainRole;
            targetname += $"{Utils.ColorString(UtilsRoleText.GetRoleColor(role), $"({GetString($"{role}")})")}";
        }
        return $"{string.Format(GetString("TurncoatLowerText"), targetname)}";
    }
    public override string GetProgressText(bool comms = false, bool GameLog = false) => $"<color={TargetColorcode}>★</color>";

    public void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(IsTargetDied);
        sender.Writer.Write(targetid);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        IsTargetDied = reader.ReadBoolean();
        targetid = reader.ReadByte();
        TargetColorcode = Palette.PlayerColors[PlayerCatch.GetPlayerById(targetid).cosmetics.ColorId].ColorCode();
    }

    bool IKiller.CanUseImpostorVentButton() => false;
    bool IKiller.CanUseKillButton() => false;
    bool IKiller.CanUseSabotageButton() => false;//サボタージュ使えてもいい気がする。
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterCooldown = cooldown;
        AURoleOptions.ShapeshifterDuration = duration;
    }
}