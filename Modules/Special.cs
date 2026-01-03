using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Hazel;
using TownOfHost.Attributes;
using TownOfHost.Modules;
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
    public static bool NowRoleEvent => false;
    public static List<string> OptionLoad = new();
    public static bool IsE(this CustomRoles role) => role is CustomRoles.SpeedStar or CustomRoles.Chameleon or CustomRoles.Fortuner;

    /// <summary>
    /// 期間限定ロールが使用可能かをチェックします<br/>
    /// 通常ロールはtrueを返します
    /// </summary>
    /// <returns>ロールが使用可能ならtrueを返します</returns>
    public static bool CheckRole(CustomRoles role, bool useApiData = true)
    {
        //イベント役職以外はtrueを返す!!
        if (!EventRoles.TryGetValue(role, out var check)) return true;

        //キャッシュ済みならそっちを使う
        if (cachedEventFlags.TryGetValue((role, useApiData), out bool cachedData))
            return cachedData;

        bool result = check.Invoke();
        if (!useApiData || VersionInfoManager.version?.Events == null)
        {
            //apiが使えない状態orフラグがfalseの場合はローカルの状態を使用
            cachedEventFlags[(role, false)] = result;
            return result;
        }

        var roleId = (int)role;
        var events = VersionInfoManager.version.Events;
        var allverevents = VersionInfoManager.allversion.Events;

        //全イベント情報をチェック
        foreach (var data in events)
        {
            if (result) continue;
            result |= data.RoleId == roleId && data.Period.IsActive;
        }
        foreach (var data in allverevents)
        {
            if (result) continue;
            result |= data.RoleId == roleId && data.Period.IsActive;
        }

        cachedEventFlags[(role, true)] = result;
        return result;
    }
    public static Dictionary<CustomRoles, Func<bool>> EventRoles = new()
    {
        {CustomRoles.Altair,() => Tanabata},
        {CustomRoles.Vega,() => Tanabata},
        {CustomRoles.SpeedStar , () => Special},
        {CustomRoles.Chameleon , () => Special},
        {CustomRoles.Fortuner , () => Special},
        {CustomRoles.Cakeshop , () => NowRoleEvent}
    };

    public static Dictionary<(CustomRoles role, bool isApiData), bool> cachedEventFlags = new(EventRoles.Count);
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
            SetUpOptionItem,
            "SS",
            OptionSort: (0, 51),
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
        Event.OptionLoad.Add("Fortuner");
    }
    public override void ApplyGameOptions(IGameOptions opt) => AURoleOptions.PhantomCooldown = cooldown;
    public void OnClick(ref bool AdjustKillCooldown, ref bool? ResetCooldown)
    {
        ResetCooldown = true;
        AdjustKillCooldown = true;
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
public sealed class Fortuner : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Fortuner),
            player => new Fortuner(player),
            CustomRoles.Fortuner,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            24100,
            SetUpOptionItem,
            "fo",
            "#34f098",
            OptionSort: (0, 51),
            from: From.Speyrp
        );
    public Fortuner(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.True
    )
    {
        UseCount = OptionUseCount.GetInt();
        cooldown = OptionCooldown.GetFloat();
        giveplayerid = new();
    }
    List<byte> giveplayerid = new();
    int UseCount; static OptionItem OptionUseCount;
    float cooldown; static OptionItem OptionCooldown;
    public static void SetUpOptionItem()
    {
        OptionUseCount = IntegerOptionItem.Create(RoleInfo, 10, GeneralOption.OptionCount, new(1, 50, 1), 2, false);
        OptionCooldown = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.Cooldown, OptionBaseCoolTime, 30, false);
        OverrideTasksData.Create(RoleInfo, 12);
    }
    public override RoleTypes? AfterMeetingRole => MyTaskState.IsTaskFinished ? RoleTypes.Impostor : RoleTypes.Crewmate;
    bool IKiller.CanUseSabotageButton() => false;
    bool IKiller.CanUseImpostorVentButton() => false;
    bool IKiller.IsKiller => false;
    bool IKiller.CanUseKillButton() => MyTaskState.IsTaskFinished && UseCount > 0;
    float IKiller.CalculateKillCooldown() => cooldown;
    void IKiller.OnCheckMurderAsKiller(MurderInfo info)
    {
        giveplayerid.Add(info.AppearanceTarget.PlayerId);
        Logger.Info($"{info.AppearanceTarget.PlayerId}", "F<EGTfaAr>or<canoacw>tu<na!>n<ruanor1>er".RemoveHtmlTags());
        info.DoKill = false;
        UseCount--;
        Player.SetKillCooldown(target: info.AppearanceTarget);
        SendRpc();
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(false);
    }
    public override string GetProgressText(bool comms = false, bool GameLog = false)
        => MyTaskState.IsTaskFinished ? $"<{RoleInfo.RoleColorCode}> ({UseCount})</color>" : "";
    public override CustomRoles Misidentify() => MyTaskState.IsTaskFinished ? CustomRoles.NotAssigned : CustomRoles.Crewmate;
    public override bool OnCompleteTask(uint taskid)
    {
        if (Player.IsAlive() && MyTaskState.IsTaskFinished)
        {
            Player.RpcSetRoleDesync(RoleTypes.Impostor, Player.GetClientId());
            _ = new LateTask(() =>
            {
                Player.SetKillCooldown();
                UtilsNotifyRoles.NotifyRoles();
            }, 0.2f);
        }
        return true;
    }
    CustomRoles[] givel1addons =
    {
        CustomRoles.Lighting,
        CustomRoles.Moon,
        CustomRoles.Tiebreaker,
        CustomRoles.Opener,
        CustomRoles.Management,
        CustomRoles.Autopsy
    };
    CustomRoles[] givel2addons =
    {
        CustomRoles.Autopsy,
        CustomRoles.Lighting,
        CustomRoles.Moon,
        CustomRoles.Tiebreaker,
        CustomRoles.Opener,
        CustomRoles.Management,
        CustomRoles.MagicHand,
        CustomRoles.Serial
    };
    CustomRoles[] givel3addons =
    {
        CustomRoles.Autopsy,
        CustomRoles.Lighting,
        CustomRoles.Moon,
        CustomRoles.Guesser,
        CustomRoles.Tiebreaker,
        CustomRoles.Opener,
        CustomRoles.Management,
        CustomRoles.Speeding,
        CustomRoles.MagicHand,
        CustomRoles.Serial,
        CustomRoles.PlusVote,
        CustomRoles.Seeing
    };
    enum Fortune
    {
        GiveLv1Addon = 40,
        GiveLv2Addon = 75,
        GiveLv3Addon = 100
    }
    public override void OnStartMeeting()
    {
        foreach (var id in giveplayerid)
        {
            var player = id.GetPlayerControl();
            var role = CustomRoles.NotAssigned;
            if (player.IsAlive())
            {
                var chance = IRandom.Instance.Next(100);
                if (chance < (int)Fortune.GiveLv1Addon)
                {
                    var roles = givel1addons.Where(role => !player.Is(role)).ToList();
                    if (roles.Count <= 0) continue;
                    role = roles[IRandom.Instance.Next(roles.Count)];
                    player.RpcSetCustomRole(role);
                }
                else if (chance < (int)Fortune.GiveLv2Addon)
                {
                    var roles = givel2addons.Where(role => !player.Is(role)).ToList();
                    if (roles.Count <= 0) continue;
                    role = roles[IRandom.Instance.Next(roles.Count)];
                    player.RpcSetCustomRole(role);
                }
                else
                {
                    var roles = givel3addons.Where(role => !player.Is(role)).ToList();
                    if (roles.Count <= 0) continue;
                    role = roles[IRandom.Instance.Next(roles.Count)];
                    player.RpcSetCustomRole(role);
                }
            }
            if (role is not CustomRoles.NotAssigned)
                _ = new LateTask(() => Utils.SendMessage(string.Format(GetString("For<deador>tu<life>ne_Meg".RemoveHtmlTags()), UtilsRoleText.GetRoleColorAndtext(role))), 0.5f, "forsendmeg", null);
        }
        giveplayerid.Clear();
    }
    void SendRpc()
    {
        using var sender = CreateSender();
        sender.Writer.Write(UseCount);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        UseCount = reader.ReadInt32();
    }
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
            SetUpOptionItem,
            "Ch",
            "#357a39",
            OptionSort: (0, 50),
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
        if (AmongUsClient.Instance.AmHost is false) return;
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

        SendRpc();
        if (oldteam != NowTeam) UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player);
    }
    void SendRpc()
    {
        using var sender = CreateSender();
        sender.Writer.WritePacked((int)NowTeam);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        NowTeam = (CustomRoles)reader.ReadPackedInt32();
    }
    public override void OverrideTrueRoleName(ref UnityEngine.Color roleColor, ref string roleText) => roleText = Translator.GetString($"{NowTeam}").Color(UtilsRoleText.GetRoleColor(NowTeam)) + Translator.GetString("Chameleon");
    public override void AfterMeetingTasks() => _ = new LateTask(() => { if (!GameStates.CalledMeeting) ChengeTeam(); }, 5f, "", true);
    public bool CheckWin(ref CustomRoles winnerRole) => ((CustomRoles)CustomWinnerHolder.WinnerTeam == NowTeam) || CustomWinnerHolder.AdditionalWinnerRoles.Contains(NowTeam);
}