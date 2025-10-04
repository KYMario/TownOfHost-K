using AmongUs.GameOptions;
using System.Linq;

using TownOfHost.Roles.Core;
using TownOfHost.Attributes;
using static TownOfHost.Modules.SelfVoteManager;

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
            14400,
            SetupOptionItem,
            "Ma",
            "#f09199",
            (5, 0),
            introSound: () => GetIntroSound(RoleTypes.Scientist),
            assignInfo: new RoleAssignInfo(CustomRoles.Madonna, CustomRoleTypes.Neutral)
            {
                AssignCountRule = new(1, 1, 1)
            },
            Desc: () => string.Format(GetString("MadonnaDesc"), GetString(ChangeRoles[OptionLoverChenge.GetValue()].ToString()), Optionlimit.GetInt())
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
        IsNonLover = true;
        Vindictive = false;
        Breakup = false;
    }
    private static OptionItem Optionlimit;
    private static OptionItem OptionLoverChenge;
    public static CustomRoles LoverChenge;
    public static OptionItem MadonnaLoverAddwin;
    public static OptionItem MaLoversSolowin3players;
    public float limit;
    bool IsNonLover;
    bool Vindictive;
    bool Breakup;
    enum Option
    {
        Madonnalimit,
        MadonnaFallChenge,
        LoversRoleAddwin,
        LoverSoloWin3players
    }

    [GameModuleInitializer]
    public static void Mareset()
    {
        Lovers.MaMadonnaLoversPlayers.Clear();
        Lovers.isMadonnaLoversDead = false;
    }
    public static readonly CustomRoles[] ChangeRoles =
    {
            CustomRoles.Crewmate, CustomRoles.Jester, CustomRoles.Opportunist,CustomRoles.Madmate,CustomRoles.Monochromer
    };
    private static void SetupOptionItem()
    {
        SoloWinOption.Create(RoleInfo, 9, CustomRoles.MadonnaLovers, () => !MadonnaLoverAddwin.GetBool(), defo: 13);
        var cRolesString = ChangeRoles.Select(x => x.ToString()).ToArray();
        Optionlimit = IntegerOptionItem.Create(RoleInfo, 10, Option.Madonnalimit, new(1, 10, 1), 3, false).SetValueFormat(OptionFormat.day);
        OptionLoverChenge = StringOptionItem.Create(RoleInfo, 11, Option.MadonnaFallChenge, cRolesString, 4, false);
        MadonnaLoverAddwin = BooleanOptionItem.Create(RoleInfo, 12, Option.LoversRoleAddwin, false, false);
        MaLoversSolowin3players = BooleanOptionItem.Create(RoleInfo, 13, Option.LoverSoloWin3players, false, false);
    }

    public override void Add() => AddSelfVotes(Player);
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (isForMeeting && Player.IsAlive() && seer.PlayerId == seen.PlayerId && Canuseability() && IsNonLover)
        {
            var mes = $"<color={RoleInfo.RoleColorCode}>{GetString("SelfVoteRoleInfoMeg")}</color>";
            return isForHud ? mes : $"<size=40%>{mes}</size>";
        }
        return "";
    }
    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (!Canuseability()) return true;
        if (!Player.Is(CustomRoles.MadonnaLovers) && IsNonLover && Is(voter))
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
        var target = PlayerCatch.GetPlayerById(votedForId);
        if (!target.IsAlive()) return;
        if (target.Is(CustomRoles.OneLove))
        {
            IsNonLover = false;
            Vindictive = true;
            Logger.Info($"Player: {Player.name},Target: {target.name}　相手が片思いで断わられた{LoverChenge}に役職変更。", "Madonna");
            Utils.SendMessage(string.Format(GetString("Skill.MadonnaOneLover"), UtilsName.GetPlayerColor(target, true), GetString($"{LoverChenge}")) + GetString("VoteSkillFin"), Player.PlayerId);
            Utils.SendMessage(string.Format(GetString("Skill.MadonnaOneLoverNo"), UtilsName.GetPlayerColor(Player, true)), target.PlayerId);

            UtilsGameLog.AddGameLog($"Madonna", string.Format(GetString("Log.MadoonaFa"), UtilsName.GetPlayerColor(Player, true), UtilsName.GetPlayerColor(target, true)));
            target.RpcProtectedMurderPlayer();
        }
        else
        if (Lovers.OneLovePlayer.BelovedId == target.PlayerId)
        {
            IsNonLover = false;
            Vindictive = true;
            Logger.Info($"Player: {Player.name},Target: {target.name}　視線が凄いからやめといた{LoverChenge}に役職変更。", "Madonna");
            Utils.SendMessage(string.Format(GetString("Skill.MadonnnaOneLovertarget"), UtilsName.GetPlayerColor(target, true), GetString($"{LoverChenge}")) + GetString("VoteSkillFin"), Player.PlayerId);

            UtilsGameLog.AddGameLog($"Madonna", string.Format(GetString("Log.MadoonaFa"), UtilsName.GetPlayerColor(Player, true), UtilsName.GetPlayerColor(target, true)));
        }
        else
        if (!target.IsLovers() && !target.Is(CustomRoles.Vega) && !target.Is(CustomRoles.Altair))
        {
            IsNonLover = false;
            Logger.Info($"Player: {Player.name},Target: {target.name}", "Madonna");
            Utils.SendMessage(string.Format(GetString("Skill.MadoonnaMyCollect"), UtilsName.GetPlayerColor(target, true)) + GetString("VoteSkillFin"), Player.PlayerId);
            Utils.SendMessage(string.Format(GetString("Skill.MadoonnaCollect"), UtilsName.GetPlayerColor(Player, true)), target.PlayerId);
            target.RpcSetCustomRole(CustomRoles.MadonnaLovers);
            Player.RpcSetCustomRole(CustomRoles.MadonnaLovers);
            Lovers.MaMadonnaLoversPlayers.Add(Player);
            Lovers.MaMadonnaLoversPlayers.Add(target);
            Lovers.HaveLoverDontTaskPlayers.Add(Player.PlayerId);
            Lovers.HaveLoverDontTaskPlayers.Add(target.PlayerId);
            RPC.SyncMadonnaLoversPlayers();
            UtilsGameLog.AddGameLog($"Madonna", string.Format(GetString("Log.MadonnaCo"), UtilsName.GetPlayerColor(Player, true), UtilsName.GetPlayerColor(target, true)));

            target.RpcProtectedMurderPlayer();
        }
        else
        {
            IsNonLover = false;
            Vindictive = true;
            Logger.Info($"Player: {Player.name},Target: {target.name}　相手がラバーズなので断わられた{LoverChenge}に役職変更。", "Madonna");
            Utils.SendMessage(string.Format(GetString("Skill.MadoonnaMynotcollect"), UtilsName.GetPlayerColor(target, true), GetString($"{LoverChenge}")) + GetString("VoteSkillFin"), Player.PlayerId);
            Utils.SendMessage(string.Format(GetString("Skill.MadonnaOneLoverNo"), UtilsName.GetPlayerColor(Player, true)), target.PlayerId);

            UtilsGameLog.AddGameLog($"Madonna", string.Format(GetString("Log.MadoonaFa"), UtilsName.GetPlayerColor(Player, true), UtilsName.GetPlayerColor(target, true)));
            target.RpcProtectedMurderPlayer();
        }
    }
    public override void AfterMeetingTasks()
    {
        if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;
        if (Player.Is(CustomRoles.MadonnaLovers))
        {
            Breakup = true;//リア充ならバクハフラグを立てる
        }
        else
        if (Player.IsAlive() && !Player.Is(CustomRoles.MadonnaLovers) && Breakup)
        {//生きててラバーズ状態が解消されててる状態なら実行
            Utils.SendMessage(string.Format(GetString("Skill.MadoonnaHAMETU"), GetString($"{LoverChenge}")), Player.PlayerId);
            Breakup = false;
            if (!Utils.RoleSendList.Contains(Player.PlayerId)) Utils.RoleSendList.Add(Player.PlayerId);
            Player.RpcSetCustomRole(LoverChenge, true, log: true);
        }
        if (Vindictive)
        {
            Vindictive = false;
            Player.RpcSetCustomRole(LoverChenge, true, log: true);
        }
        else
        if (limit <= (UtilsGameLog.day - 1) && IsNonLover && Player.IsAlive())
        {
            Player.RpcExileV2();
            MyState.SetDead();
            MyState.DeathReason = CustomDeathReason.Suicide;
            ReportDeadBodyPatch.IgnoreBodyids[Player.PlayerId] = false;
            UtilsGameLog.AddGameLog($"Madonna", string.Format(GetString("log.AM"), UtilsName.GetPlayerColor(PlayerCatch.GetPlayerById(Player.PlayerId))));
            Logger.Info($"{Player.GetNameWithRole().RemoveHtmlTags()}は指定ターン経過したため自殺。", "Madonna");
        }
    }
    public override string GetProgressText(bool comms = false, bool GameLog = false)
    {
        if (!Player.IsAlive() || !IsNonLover || Player.Is(CustomRoles.MadonnaLovers)) return "";

        return $" <color={(UtilsGameLog.day == limit ? "#ff1919" : $"{RoleInfo.RoleColorCode}")}>({UtilsGameLog.day}/{limit})</color>";
    }
}