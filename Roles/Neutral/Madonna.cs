using AmongUs.GameOptions;
using TownOfHost.Roles.Core;
using static TownOfHost.Modules.SelfVoteManager;
using static TownOfHost.Translator;
using System.Linq;
using TownOfHost.Attributes;
using TownOfHost.Roles.Madmate;
using System;

namespace TownOfHost.Roles.Neutral;
public sealed class Madonna : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Madonna),
            player => new Madonna(player),
            CustomRoles.Madonna,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            52000,
            SetupOptionItem,
            "Ma",
            "#f09199",
            introSound: () => GetIntroSound(RoleTypes.Scientist),
            assignInfo: new RoleAssignInfo(CustomRoles.Madonna, CustomRoleTypes.Neutral)
            {
                AssignCountRule = new(1, 1, 1)
            }
        );
    public Madonna(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        limit = Optionlimit.GetFloat();
        LoverChenge = ChangeRoles[OptionLoverChenge.GetValue()];
        Limitd = true;
        Hangyaku = false;
        Wakarero = false;
    }
    private static OptionItem Optionlimit;
    private static OptionItem OptionLoverChenge;
    public static CustomRoles LoverChenge;
    public float limit;
    bool Limitd;
    bool Hangyaku;
    bool Wakarero;
    enum Option
    {
        Madonnalimit,
        MadonnaFallChenge
    }

    [GameModuleInitializer]
    public static void Mareset()
    {
        Main.MaMaLoversPlayers.Clear();
        Main.isMaLoversDead = false;
    }
    public static readonly CustomRoles[] ChangeRoles =
    {
            CustomRoles.Crewmate, CustomRoles.Jester, CustomRoles.Opportunist,CustomRoles.Madmate
    };
    private static void SetupOptionItem()
    {
        var cRolesString = ChangeRoles.Select(x => x.ToString()).ToArray();
        Optionlimit = FloatOptionItem.Create(RoleInfo, 10, Option.Madonnalimit, new(1f, 10f, 1f), 3f, false).SetValueFormat(OptionFormat.day);
        OptionLoverChenge = StringOptionItem.Create(RoleInfo, 11, Option.MadonnaFallChenge, cRolesString, 4, false);
    }

    public override void Add()
        => AddS(Player);
    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (MadAvenger.Skill) return true;
        if (!Player.Is(CustomRoles.MaLovers) && Limitd && Is(voter))
        {
            if (CheckSelfVoteMode(Player, votedForId, out var status))
            {
                if (status is VoteStatus.Self)
                    Utils.SendMessage(string.Format(GetString("SkillMode"), GetString("Mode.Madoonna"), GetString("Vote.Madonna")) + GetString("VoteSkillMode"), Player.PlayerId);
                if (status is VoteStatus.Skip)
                    Utils.SendMessage(GetString("VoteSkillFin"), Player.PlayerId);
                if (status is VoteStatus.Vote)
                    MadonnaL(votedForId);
                SetMode(Player, status is VoteStatus.Self);
                return false;
            }
        }
        return true;
    }
    public void MadonnaL(byte votedForId)
    {
        var target = Utils.GetPlayerById(votedForId);
        if (!target.IsAlive()) return;
        if (!target.Is(CustomRoles.ALovers) && !target.Is(CustomRoles.BLovers) && !target.Is(CustomRoles.CLovers) && !target.Is(CustomRoles.DLovers) &&
            !target.Is(CustomRoles.ELovers) && !target.Is(CustomRoles.FLovers) && !target.Is(CustomRoles.GLovers))
        {
            Limitd = false;
            Logger.Info($"Player: {Player.name},Target: {target.name}", "Madonna");
            Utils.SendMessage(string.Format(GetString("Skill.MadoonnaMyCollect"), Utils.GetPlayerColor(target, true)) + GetString("VoteSkillFin"), Player.PlayerId);
            Utils.SendMessage(string.Format(GetString("Skill.MadoonnaCollect"), Utils.GetPlayerColor(Player, true)), target.PlayerId);
            target.RpcSetCustomRole(CustomRoles.MaLovers);
            Player.RpcSetCustomRole(CustomRoles.MaLovers);
            Main.MaMaLoversPlayers.Add(Player);
            Main.MaMaLoversPlayers.Add(target);
            Main.gamelog += $"\n{System.DateTime.Now:HH.mm.ss} [Madonna]　" + string.Format(GetString("Log.MadonnaCo"), Utils.GetPlayerColor(Player, true), Utils.GetPlayerColor(target, true));

            target.RpcProtectedMurderPlayer();
        }
        else
        {
            Limitd = false;
            Hangyaku = true;
            Logger.Info($"Player: {Player.name},Target: {target.name}　相手がラバーズなので断わられた{LoverChenge}に役職変更。", "Madonna");
            Utils.SendMessage(string.Format(GetString("Skill.MadoonnaMynotcollect"), Utils.GetPlayerColor(target, true), GetString($"{LoverChenge}")) + GetString("VoteSkillFin"), Player.PlayerId);
            Utils.SendMessage(string.Format(GetString("Skill.Madoonnanotcollect"), Utils.GetPlayerColor(Player, true)), target.PlayerId);
            Main.gamelog += $"\n{System.DateTime.Now:HH.mm.ss} [Madonna]　" + string.Format(GetString("Log.MadoonaFa"), Utils.GetPlayerColor(Player, true), Utils.GetPlayerColor(target, true));
            target.RpcProtectedMurderPlayer();
        }
    }
    public override void AfterMeetingTasks()
    {
        if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;
        if (Player.Is(CustomRoles.MaLovers))
        {
            Wakarero = true;//リア充ならバクハフラグを立てる
        }
        else
        if (Player.IsAlive() && !Player.Is(CustomRoles.MaLovers) && Wakarero)
        {//生きててラバーズ状態が解消されててる状態なら実行
            Utils.SendMessage(string.Format(GetString("Skill.MadoonnaHAMETU"), GetString($"{LoverChenge}")), Player.PlayerId);
            Player.RpcSetCustomRole(LoverChenge, true);
            Wakarero = false;
        }
        if (Hangyaku)
        {
            Hangyaku = false;
            Player.RpcSetCustomRole(LoverChenge, true);
        }
        else
        if (limit <= Main.day && Limitd && Player.IsAlive())
        {
            PlayerState state = PlayerState.GetByPlayerId(Player.PlayerId);
            Player.RpcExileV2();
            state.SetDead();
            state.DeathReason = CustomDeathReason.Suicide;
            ReportDeadBodyPatch.Musisuruoniku[Player.PlayerId] = false;
            Main.gamelog += $"\n{DateTime.Now:HH.mm.ss} [Madonna]　" + string.Format(GetString("log.AM"), Utils.GetPlayerColor(Utils.GetPlayerById(Player.PlayerId)), Utils.GetTrueRoleName(Player.PlayerId, false));
            Logger.Info($"{Player.GetNameWithRole().RemoveHtmlTags()}は指定ターン経過したため自殺。", "Madonna");
        }
    }
}