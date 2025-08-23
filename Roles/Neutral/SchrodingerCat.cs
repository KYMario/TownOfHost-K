using System.Collections.Generic;
using System.Linq;

using Hazel;
using UnityEngine;

using AmongUs.GameOptions;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.Crewmate;

namespace TownOfHost.Roles.Neutral;

// マッドが属性化したらマッド状態時の特別扱いを削除する
public sealed class SchrodingerCat : RoleBase, IAdditionalWinner, IDeathReasonSeeable, IKillFlashSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(SchrodingerCat),
            player => new SchrodingerCat(player),
            CustomRoles.SchrodingerCat,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            15600,
            SetupOptionItem,
            "sc",
            "#696969",
            (7, 2),
            introSound: () => GetIntroSound(RoleTypes.Impostor),
            from: From.TOR_GM_Haoming_Edition
        );
    public SchrodingerCat(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CanWinTheCrewmateBeforeChange = OptionCanWinTheCrewmateBeforeChange.GetBool();
        ChangeTeamWhenExile = OptionChangeTeamWhenExile.GetBool();
        CanSeeKillableTeammate = OptionCanSeeKillableTeammate.GetBool();
    }
    static OptionItem OptionCanWinTheCrewmateBeforeChange;
    static OptionItem OptionChangeTeamWhenExile;
    static OptionItem OptionCanSeeKillableTeammate;

    enum OptionName
    {
        CanBeforeSchrodingerCatWinTheCrewmate,
        SchrodingerCatExiledTeamChanges,
        SchrodingerCatCanSeeKillableTeammate,
    }
    static bool CanWinTheCrewmateBeforeChange;
    static bool ChangeTeamWhenExile;
    static bool CanSeeKillableTeammate;

    /// <summary>
    /// 自分をキルしてきた人のロール
    /// </summary>
    private ISchrodingerCatOwner owner = null;
    private TeamType _team = TeamType.None;
    /// <summary>
    /// 現在の所属陣営<br/>
    /// 変更する際は特段の事情がない限り<see cref="RpcSetTeam"/>を使ってください
    /// </summary>
    public TeamType Team
    {
        get => _team;
        private set
        {
            logger.Info($"{Player.GetRealName()}の陣営を{value}に変更");
            _team = value;
        }
    }
    public bool AmMadmate => Team == TeamType.Mad;
    public Color DisplayRoleColor => GetCatColor(Team);
    private static LogHandler logger = Logger.Handler(nameof(SchrodingerCat));

    public static void SetupOptionItem()
    {
        OptionCanWinTheCrewmateBeforeChange = BooleanOptionItem.Create(RoleInfo, 10, OptionName.CanBeforeSchrodingerCatWinTheCrewmate, false, false);
        OptionChangeTeamWhenExile = BooleanOptionItem.Create(RoleInfo, 11, OptionName.SchrodingerCatExiledTeamChanges, false, false);
        OptionCanSeeKillableTeammate = BooleanOptionItem.Create(RoleInfo, 12, OptionName.SchrodingerCatCanSeeKillableTeammate, false, false);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        owner?.ApplySchrodingerCatOptions(opt);
    }
    /// <summary>
    /// マッド猫用のオプション構築
    /// </summary>
    public static void ApplyMadCatOptions(IGameOptions opt)
    {
        opt.SetVision(true);
    }
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        var killer = info.AttemptKiller;

        //自殺ならスルー
        if (info.IsSuicide) return true;

        if (killer.Is(CustomRoles.GrimReaper) || killer.Is(CustomRoles.BakeCat))
        {
            return true;
        }
        else
        if (Team == TeamType.None)
        {
            info.CanKill = false;
            ChangeTeamOnKill(killer);
            return false;
        }
        return true;
    }
    /// <summary>
    /// キルしてきた人に応じて陣営の状態を変える
    /// </summary>
    public void ChangeTeamOnKill(PlayerControl killer)
    {
        killer.RpcProtectedMurderPlayer(Player);
        if (killer.GetRoleClass() is ISchrodingerCatOwner catOwner)
        {
            catOwner.OnSchrodingerCatKill(this);
            RpcSetTeam(catOwner.SchrodingerCatChangeTo);
            owner = catOwner;
        }
        else
        {
            logger.Warn($"未知のキル役職からのキル: {killer.GetNameWithRole().RemoveHtmlTags()}");
        }

        RevealNameColors(killer);

        UtilsNotifyRoles.NotifyRoles();
        UtilsOption.MarkEveryoneDirtySettings();
    }
    /// <summary>
    /// キルしてきた人とオプションに応じて名前の色を開示する
    /// </summary>
    private void RevealNameColors(PlayerControl killer)
    {
        if (CanSeeKillableTeammate)
        {
            var killerRoleId = killer.GetCustomRole();
            var killerTeam = PlayerCatch.AllPlayerControls.Where(player => (AmMadmate && (player.Is(CustomRoleTypes.Impostor) || player.Is(CustomRoles.WolfBoy))) || player.Is(killerRoleId));
            foreach (var member in killerTeam)
            {
                if (member.GetCustomRole().IsMadmate()) continue;
                var rolecolor = RoleInfo.RoleColorCode;
                if (member.Is(CustomRoles.WolfBoy))
                {
                    if (killerRoleId is not CustomRoles.WolfBoy) continue;
                    rolecolor = WolfBoy.Shurenekodotti.GetBool() ? UtilsRoleText.GetRoleColorCode(CustomRoles.Impostor) : "#ffffff";
                }
                NameColorManager.Add(member.PlayerId, Player.PlayerId, rolecolor);
                NameColorManager.Add(Player.PlayerId, member.PlayerId);
            }
        }
        else
        {
            var rolecolor = RoleInfo.RoleColorCode;
            if (killer.Is(CustomRoles.WolfBoy))
            {
                rolecolor = WolfBoy.Shurenekodotti.GetBool() ? UtilsRoleText.GetRoleColorCode(CustomRoles.Impostor) : "#ffffff";
            }
            NameColorManager.Add(killer.PlayerId, Player.PlayerId, rolecolor);
            NameColorManager.Add(Player.PlayerId, killer.PlayerId);
        }
        UtilsGameLog.AddGameLog($"SchrodingerCat", UtilsName.GetPlayerColor(Player) + ":  " + string.Format(GetString("SchrodingerCat.Ch"), UtilsName.GetPlayerColor(killer, true) + $"(<b>{UtilsRoleText.GetTrueRoleName(killer.PlayerId, false)}</b>)"));
    }
    public override void OverrideTrueRoleName(ref Color roleColor, ref string roleText)
    {
        // 陣営変化前なら上書き不要
        if (Team == TeamType.None)
        {
            return;
        }
        roleColor = DisplayRoleColor;
    }
    public override void OnExileWrapUp(NetworkedPlayerInfo exiled, ref bool DecidedWinner)
    {
        if (exiled.PlayerId != Player.PlayerId || Team != TeamType.None || !ChangeTeamWhenExile)
        {
            return;
        }
        ChangeTeamRandomly();
    }
    /// <summary>
    /// ゲームに存在している陣営の中からランダムに自分の陣営を変更する
    /// </summary>
    private void ChangeTeamRandomly()
    {
        var rand = IRandom.Instance;
        List<TeamType> candidates = new(4)
        {
            TeamType.Crew,
            TeamType.Mad,
        };
        if (CustomRoles.Egoist.IsPresent())
        {
            candidates.Add(TeamType.Egoist);
        }
        if (CustomRoles.Jackal.IsPresent() || CustomRoles.JackalMafia.IsPresent() || CustomRoles.JackalAlien.IsPresent())
        {
            candidates.Add(TeamType.Jackal);
        }
        var team = candidates[rand.Next(candidates.Count)];
        RpcSetTeam(team);
    }
    public bool CheckWin(ref CustomRoles winnerRole)
    {
        bool? won = Team switch
        {
            TeamType.None => CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate && CanWinTheCrewmateBeforeChange,
            TeamType.Mad => CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor,
            TeamType.Crew => CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate,
            TeamType.Jackal => CustomWinnerHolder.WinnerTeam == CustomWinner.Jackal,
            TeamType.Egoist => CustomWinnerHolder.WinnerTeam == CustomWinner.Egoist,
            TeamType.CountKiller => CustomWinnerHolder.WinnerTeam == CustomWinner.CountKiller,
            TeamType.Remotekiller => CustomWinnerHolder.WinnerTeam == CustomWinner.Remotekiller,
            TeamType.DoppelGanger => CustomWinnerHolder.WinnerTeam == CustomWinner.DoppelGanger,
            TeamType.MilkyWay => CustomWinnerHolder.WinnerTeam == CustomWinner.MilkyWay,
            TeamType.Betrayer => CustomWinnerHolder.WinnerTeam == CustomWinner.MadBetrayer,
            _ => null,
        };
        if (!won.HasValue)
        {
            logger.Warn($"不明な猫の勝利チェック: {Team}");
            return false;
        }
        return won.Value;
    }
    public void RpcSetTeam(TeamType team)
    {
        Team = team;
        if (AmongUsClient.Instance.AmHost)
        {
            using var sender = CreateSender();
            sender.Writer.Write((byte)team);
        }
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        Team = (TeamType)reader.ReadByte();
    }

    // マッド属性化までの間マッド状態時に特別扱いするための応急処置的個別実装
    // マッドが属性化したらマッド状態のシュレ猫にマッド属性を付与することで削除
    // 上にあるApplyMadCatOptions，MeetingHudPatchにある道連れ処理，ShipStatusPatchにあるサボ直しキャンセル処理も同様 - Hyz-sui
    public bool? CheckSeeDeathReason(PlayerControl seen) => AmMadmate && Options.MadmateCanSeeDeathReason.GetBool();
    public bool? CheckKillFlash(MurderInfo info) => AmMadmate && Options.MadmateCanSeeKillFlash.GetBool();

    /// <summary>
    /// 陣営状態
    /// </summary>
    public enum TeamType : byte
    {
        /// <summary>
        /// どこの陣営にも属していない状態
        /// </summary>
        None = 0,

        // 10-49 シェリフキルオプションを作成しない変化先

        /// <summary>
        /// インポスター陣営に所属する状態
        /// </summary>
        Mad = 10,
        /// <summary>
        /// クルー陣営に所属する状態
        /// </summary>
        Crew,

        // 50- シェリフキルオプションを作成する変化先

        /// <summary>
        /// ジャッカル陣営に所属する状態
        /// </summary>
        Jackal = 50,
        /// <summary>
        /// エゴイスト陣営に所属する状態
        /// </summary>
        Egoist,
        /// <summary>
        /// カウントキラーに所属する状態
        /// </summary>
        CountKiller,
        /// <summary>
        /// リモートキラーに所属する状態
        /// </summary>
        Remotekiller,
        /// <summary>
        /// ドッペルゲンガーに所属する状態
        /// </summary>
        DoppelGanger,
        /// <summary>
        /// 天の川陣営に所属する状態
        /// </summary>
        MilkyWay,
    }
    public static Color GetCatColor(TeamType catType)
    {
        Color? color = catType switch
        {
            TeamType.None => RoleInfo.RoleColor,
            TeamType.Mad => UtilsRoleText.GetRoleColor(CustomRoles.Madmate),
            TeamType.Crew => UtilsRoleText.GetRoleColor(CustomRoles.Crewmate),
            TeamType.Jackal => UtilsRoleText.GetRoleColor(CustomRoles.Jackal),
            TeamType.Egoist => UtilsRoleText.GetRoleColor(CustomRoles.Egoist),
            TeamType.Remotekiller => UtilsRoleText.GetRoleColor(CustomRoles.Remotekiller),
            TeamType.CountKiller => UtilsRoleText.GetRoleColor(CustomRoles.CountKiller),
            TeamType.DoppelGanger => UtilsRoleText.GetRoleColor(CustomRoles.DoppelGanger),
            TeamType.MilkyWay => StringHelper.CodeColor(Vega.TeamColor),
            TeamType.Betrayer => UtilsRoleText.GetRoleColor(CustomRoles.MadBetrayer),
            _ => null,
        };
        if (!color.HasValue)
        {
            logger.Warn($"不明な猫に対する色の取得: {catType}");
            return UtilsRoleText.GetRoleColor(CustomRoles.Crewmate);
        }
        return color.Value;
    }
}
