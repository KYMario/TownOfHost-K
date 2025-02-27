using AmongUs.GameOptions;
using System;
using System.Linq;
using UnityEngine;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Neutral;
public sealed class Turncoat : RoleBase, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Turncoat),
            player => new Turncoat(player),
            CustomRoles.Turncoat,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            38400,
            SetupOptionItem,
            "Tu",
            "#371a1a"
        );
    public Turncoat(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
    }
    static OptionItem OptionCanTargetImpostor;
    static OptionItem OptionCanTargetNeutral;
    static OptionItem OptionCanTargetMadmate;

    enum OptionName
    {
        TurncoatCanTargetImpostor,
        TurncoatCanTargetMadmate,
        TurncoatCanTargetNeutral
    }
    byte Target;
    bool TargetDie;
    string TargetColorcode;

    private static void SetupOptionItem()
    {
        OptionCanTargetImpostor = BooleanOptionItem.Create(RoleInfo, 10, OptionName.TurncoatCanTargetImpostor, false, false);
        OptionCanTargetMadmate = BooleanOptionItem.Create(RoleInfo, 11, OptionName.TurncoatCanTargetMadmate, false, false);
        OptionCanTargetNeutral = BooleanOptionItem.Create(RoleInfo, 12, OptionName.TurncoatCanTargetNeutral, false, false);

        RoleAddAddons.Create(RoleInfo, 20);
    }
    //ターゲットの選択
    public override void Add()
    {
        Target = byte.MaxValue;
        TargetDie = false;
        //設定に準じた奴。
        var list = PlayerCatch.AllPlayerControls.Where(pc =>
        {
            if (pc.PlayerId == Player.PlayerId) return false;
            if (pc.Is(CustomRoles.GM)) return false;

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
        if (list.Count() <= 0)
            list = PlayerCatch.AllPlayerControls.Where(pc => pc.PlayerId != Player.PlayerId && !pc.Is(CustomRoles.GM));

        //それでも0の場合、ターゲット無しにする
        if (list.Count() <= 0)
        {
            Logger.Error($"{Player?.Data?.PlayerName ?? "???"}のターゲットが存在しません", "Turncoat");
            return;
        }

        //シャッフル！
        list = list.OrderBy(_ => Guid.NewGuid()).ToArray();

        //リストの中からランダムでプレイヤーを選び、その人をターゲットに
        PlayerControl RandomPlayer = list.ToArray()[IRandom.Instance.Next(list.Count())];
        Target = RandomPlayer.PlayerId;
        TargetColorcode = Palette.PlayerColors[RandomPlayer.cosmetics.ColorId].ColorCode();
        Logger.Info($"{Player?.Data?.PlayerName ?? "???"} => {RandomPlayer?.Data?.PlayerName ?? "???"}", "Turncoat");
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost || AntiBlackout.IsCached || TargetDie || !Player.IsAlive()) return;

        PlayerControl target = PlayerCatch.GetPlayerById(Target);

        //回線切断時。いくらなんでもなのでオポチュに
        if (target?.Data?.Disconnected is true or null)
        {
            Player.RpcSetCustomRole(CustomRoles.Opportunist, true, true);
            return;
        }
        //死亡時
        if (!target.IsAlive())
        {
            TargetDie = true;
            UtilsNotifyRoles.NotifyRoles(Player);
        }
    }
    public bool CheckWin(ref CustomRoles winnerRole)
    {
        //生きてないなら負け。
        if (!Player.IsAlive()) return false;

        //勝利IDに含まれていないかつ、勝利役職に含まれてない場合 → かち！
        if (!CustomWinnerHolder.WinnerIds.Contains(Target)
        && !CustomWinnerHolder.WinnerRoles.Contains(Target.GetPlayerControl()?.GetCustomRole() ?? CustomRoles.Emptiness)) return true;

        //何が何でも負けるリストに含まれている場合 → かち！
        if (CustomWinnerHolder.IdRemoveLovers.Contains(Target)) return true;

        //上記2つに含まれないなら負け。
        return false;
    }
    //死亡時だけ役職情報を開示する
    //スタンダードなら白位置にねじ込んでキルさせる、強引に吊りに行く等誘導してもらいたい
    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, ref bool enabled, ref Color roleColor, ref string roleText, ref bool addon)
    {
        if (seen.PlayerId == Target && TargetDie)
        {
            enabled = true;
            addon |= false;
        }
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;

        if (Is(seer) && seen.PlayerId == Target) return $"<color={RoleInfo.RoleColorCode}>★</color>";
        //if (seer.PlayerId == seen.PlayerId) return $"<color={TargetColorcode}>★</color>";

        return "";
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (!Player.IsAlive() || isForMeeting) return "";

        var targetpc = PlayerCatch.GetPlayerById(Target);
        var targetname = Utils.GetPlayerColor(targetpc, true);
        if (TargetDie)
        {
            var role = targetpc.GetCustomRole();
            targetname += $"{Utils.ColorString(UtilsRoleText.GetRoleColor(role), $"({GetString($"{role}")})")}";
        }
        return $"{string.Format(GetString("TurncoatLowerText"), targetname)}";
    }
    public override string GetProgressText(bool comms = false, bool GameLog = false) => $"<color={TargetColorcode}>★</color>";

}