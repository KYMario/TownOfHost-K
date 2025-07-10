using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AmongUs.GameOptions;
using TownOfHost.Attributes;
using TownOfHost.Roles;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost;

static class Event
{
    public static bool IsChristmas = DateTime.Now.Month == 12 && DateTime.Now.Day is 24 or 25;
    public static bool White = DateTime.Now.Month == 3 && DateTime.Now.Day is 14;
    public static bool IsInitialRelease = DateTime.Now.Month == 10 && DateTime.Now.Day is 31;
    public static bool IsHalloween = (DateTime.Now.Month == 10 && DateTime.Now.Day is 31) || (DateTime.Now.Month == 11 && DateTime.Now.Day is 1 or 2 or 3 or 4 or 5 or 6 or 7);
    public static bool GoldenWeek = DateTime.Now.Month == 5 && DateTime.Now.Day is 3 or 4 or 5;
    public static bool April = DateTime.Now.Month == 4 && DateTime.Now.Day is 1 or 2 or 3 or 4 or 5 or 6 or 7 or 8;
    public static bool Tanabata = DateTime.Now.Month == 7 && DateTime.Now.Day is > 6 and < 15;
    public static bool IsEventDay => IsChristmas || White || IsInitialRelease || IsHalloween || GoldenWeek || April;
    public static bool Special = false;
    public static bool NowRoleEvent => DateTime.Now.Month == 6 && DateTime.Now.Day is 15 or 16 or 17 or 18 or 19 or 20 or 21 or 22;
    public static List<string> OptionLoad = new();
    public static bool IsE(this CustomRoles role) => role is CustomRoles.SpeedStar or CustomRoles.Chameleon;

    /// <summary>
    /// 期間限定ロールかをチェックします
    /// 通常ロールはtrueを返します
    /// </summary>
    /// <returns>ロールが使用可能ならtrueを返します</returns>
    public static bool CheckRole(CustomRoles role) => !EventRoles.TryGetValue(role, out var check) || check.Invoke();
    private static Dictionary<CustomRoles, Func<bool>> EventRoles = new()
    {
        {CustomRoles.Altair,() => Tanabata},
        {CustomRoles.Vega,() => Tanabata},
        {CustomRoles.Assassin , () => DebugModeManager.AmDebugger},
        {CustomRoles.Merlin , () => DebugModeManager.AmDebugger},
        {CustomRoles.SpeedStar , () => Special},
        {CustomRoles.Chameleon , () => Special},
        {CustomRoles.Cakeshop , () => NowRoleEvent}
    };
}


























//やぁ。気付いちゃった...?( ᐛ )
//ファイル作っちゃうとばれちゃうからね。
//ｶ ｸ ｼ ﾃ ﾙ ﾅ ﾗ ﾏ ｧ ｺ ｺ ｼﾞ ｬ ﾝ ?
public sealed class SpeedStar : RoleBase, IImpostor, IUsePhantomButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(SpeedStar),
            player => new SpeedStar(player),
            CustomRoles.SpeedStar,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Impostor,
            20800,
            (0, 51),
            SetUpOptionItem,
            "SS",
            from: From.Speyrp
        );
    public SpeedStar(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        allplayerspeed.Clear();
        speed = optSpeed.GetFloat();
        abilitytime = optabilitytime.GetFloat();
        cooldown = optcooldown.GetFloat();
        killcooldown = optkillcooldown.GetFloat();
    }
    static OptionItem optabilitytime;
    static OptionItem optcooldown;
    static OptionItem optkillcooldown;
    static OptionItem optSpeed;
    static float speed;
    static float abilitytime;
    static float cooldown;
    static float killcooldown;
    Dictionary<byte, float> allplayerspeed = new();
    public override void StartGameTasks()
    {
        foreach (var pc in PlayerCatch.AllAlivePlayerControls)
        {
            allplayerspeed.TryAdd(pc.PlayerId, Main.AllPlayerSpeed[pc.PlayerId]);
        }
    }
    static void SetUpOptionItem()
    {
        optkillcooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, OptionBaseCoolTime, 30f, false).SetValueFormat(OptionFormat.Seconds);
        optcooldown = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.Cooldown, OptionBaseCoolTime, 30f, false).SetValueFormat(OptionFormat.Seconds);
        optabilitytime = FloatOptionItem.Create(RoleInfo, 12, "GhostNoiseSenderTime", new(1, 300, 1f), 10f, false).SetValueFormat(OptionFormat.Seconds);
        optSpeed = FloatOptionItem.Create(RoleInfo, 13, "SpeedStarSpeed", new(0, 10, 0.05f), 3f, false).SetValueFormat(OptionFormat.Multiplier);
    }
    public float CalculateKillCooldown() => killcooldown;
    [PluginModuleInitializer]
    public static void Load()
    {
        Event.OptionLoad.Add("SpeedStar");
        Event.OptionLoad.Add("Chameleon");
    }
    public override void ApplyGameOptions(IGameOptions opt) => AURoleOptions.PhantomCooldown = cooldown;
    public void OnClick(ref bool AdjustKillCoolDown, ref bool? ResetCoolDown)
    {
        ResetCoolDown = true;
        AdjustKillCoolDown = true;
        foreach (var pc in PlayerCatch.AllAlivePlayerControls)
        {
            Main.AllPlayerSpeed[pc.PlayerId] = speed;
        }
        UtilsOption.MarkEveryoneDirtySettings();
        _ = new LateTask(() =>
        {
            if (GameStates.InGame)
            {
                foreach (var pc in PlayerCatch.AllPlayerControls)
                {
                    Main.AllPlayerSpeed[pc.PlayerId] = allplayerspeed[pc.PlayerId];
                }
                _ = new LateTask(() => UtilsOption.MarkEveryoneDirtySettings(), 0.2f, "", true);
                Player.RpcResetAbilityCooldown();
            }
        }, abilitytime, "", true);
    }
    public override void OnStartMeeting()
    {
        if (GameStates.InGame)
        {
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                Main.AllPlayerSpeed[pc.PlayerId] = allplayerspeed[pc.PlayerId];
            }
            _ = new LateTask(() => UtilsOption.MarkEveryoneDirtySettings(), 0.2f, "", true);
        }
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (seen.PlayerId != seer.PlayerId || isForMeeting || !Player.IsAlive()) return "";

        if (isForHud) return GetString("PhantomButtonLowertext");
        return $"<size=50%>{GetString("PhantomButtonLowertext")}</size>";
    }
    public override string GetAbilityButtonText() => GetString("SpeedStarAbility");
}
public sealed class Chameleon : RoleBase, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Chameleon),
            player => new Chameleon(player),
            CustomRoles.Chameleon,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            20700,
            (0, 50),
            SetUpOptionItem,
            "Ch",
            "#357a39",
            from: From.Speyrp
        );
    public Chameleon(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        NowTeam = CustomRoles.NotAssigned;
        TeamList.Clear();

        var ch = true;
        foreach (var pc in PlayerCatch.AllPlayerControls)
        {
            if (pc.GetCustomRole().IsImpostor()) TeamList.Add(CustomRoles.Impostor);
            if (pc.GetCustomRole().IsCrewmate())
            {
                if (ch)
                {
                    TeamList.Add(CustomRoles.Crewmate);
                    ch = false;
                }
                else
                {
                    ch = true;
                }
            }
        }
        if (CustomRoles.Jackal.IsPresent() || CustomRoles.JackalAlien.IsPresent() || CustomRoles.JackalMafia.IsPresent())
            TeamList.Add(CustomRoles.Jackal);

        if (CustomRoles.Egoist.IsPresent())
            TeamList.Add(CustomRoles.Egoist);
        if (CustomRoles.Remotekiller.IsPresent())
            TeamList.Add(CustomRoles.Remotekiller);
        if (CustomRoles.CountKiller.IsPresent())
            TeamList.Add(CustomRoles.CountKiller);
        if (CustomRoles.DoppelGanger.IsPresent())
            TeamList.Add(CustomRoles.DoppelGanger);
        if (CustomRoles.GrimReaper.IsPresent())
            TeamList.Add(CustomRoles.GrimReaper);
        if (CustomRoles.Fox.IsPresent())
            TeamList.Add(CustomRoles.Fox);
        if (CustomRoles.Arsonist.IsPresent())
            TeamList.Add(CustomRoles.Arsonist);
    }
    CustomRoles NowTeam;
    List<CustomRoles> TeamList = new();
    static void SetUpOptionItem() => RoleAddAddons.Create(RoleInfo, 20);
    public override void StartGameTasks() => ChengeTeam();
    void ChengeTeam()
    {
        var oldteam = NowTeam;
        if (!PlayerCatch.AllAlivePlayerControls.Any(p => p.GetCustomRole() is CustomRoles.Jackal or CustomRoles.JackalAlien or CustomRoles.JackalMafia))
            TeamList.Remove(CustomRoles.Jackal);
        if (!PlayerCatch.AllAlivePlayerControls.Any(p => p.GetCustomRole() is CustomRoles.Egoist))
            TeamList.Remove(CustomRoles.Egoist);
        if (!PlayerCatch.AllAlivePlayerControls.Any(p => p.GetCustomRole() is CustomRoles.Remotekiller))
            TeamList.Remove(CustomRoles.Remotekiller);
        if (!PlayerCatch.AllAlivePlayerControls.Any(p => p.GetCustomRole() is CustomRoles.CountKiller))
            TeamList.Remove(CustomRoles.CountKiller);
        if (!PlayerCatch.AllAlivePlayerControls.Any(p => p.GetCustomRole() is CustomRoles.DoppelGanger))
            TeamList.Remove(CustomRoles.DoppelGanger);
        if (!PlayerCatch.AllAlivePlayerControls.Any(p => p.GetCustomRole() is CustomRoles.GrimReaper))
            TeamList.Remove(CustomRoles.GrimReaper);
        if (!PlayerCatch.AllAlivePlayerControls.Any(p => p.GetCustomRole() is CustomRoles.Fox))
            TeamList.Remove(CustomRoles.Fox);
        if (!PlayerCatch.AllAlivePlayerControls.Any(p => p.GetCustomRole() is CustomRoles.Arsonist))
            TeamList.Remove(CustomRoles.Arsonist);

        //リストをシャッフル! → 更にランダム！
        NowTeam = TeamList.OrderBy(x => Guid.NewGuid()).ToArray()[IRandom.Instance.Next(TeamList.Count)];

        Logger.Info($"Now : {NowTeam}", "Chameleon");

        if (oldteam != NowTeam) UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player);
    }
    public override void OverrideTrueRoleName(ref UnityEngine.Color roleColor, ref string roleText) => roleText = Translator.GetString($"{NowTeam}").Color(UtilsRoleText.GetRoleColor(NowTeam)) + Translator.GetString("Chameleon");
    public override void AfterMeetingTasks() => _ = new LateTask(() => { if (!GameStates.CalledMeeting) ChengeTeam(); }, 5f, "", true);
    public bool CheckWin(ref CustomRoles winnerRole) => ((CustomRoles)CustomWinnerHolder.WinnerTeam == NowTeam) || CustomWinnerHolder.AdditionalWinnerRoles.Contains(NowTeam);
}