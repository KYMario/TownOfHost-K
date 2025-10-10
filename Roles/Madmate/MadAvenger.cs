using System.Linq;
using AmongUs.GameOptions;
using System.Collections.Generic;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Madmate;

public sealed class MadAvenger : RoleBase, IKillFlashSeeable, IDeathReasonSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(MadAvenger),
            player => new MadAvenger(player),
            CustomRoles.MadAvenger,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Madmate,
            8200,
            SetupOptionItem,
            "mAe",
            OptionSort: (4, 0),
            introSound: () => GetIntroSound(RoleTypes.Impostor)
        );
    public MadAvenger(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.ForRecompute)
    {
        canSeeKillFlash = Options.MadmateCanSeeKillFlash.GetBool();
        canSeeDeathReason = Options.MadmateCanSeeDeathReason.GetBool();
        Count = OptionCount.GetFloat();
        Cooldown = OptionCooldown.GetFloat();
        Skill = false;
        Guessd = new(GameData.Instance.PlayerCount);
        fin = false;
        can = false;
    }
    private static bool canSeeKillFlash;
    private static bool canSeeDeathReason;
    private static OverrideTasksData Tasks;
    private static OptionItem OptionCooldown;
    private static OptionItem OptionCount;
    private static OptionItem OptionVent;
    private static OptionItem OptionCanseeimpostorCount;
    public static bool Skill;
    float Cooldown;
    float Count;
    bool fin;
    bool can;
    enum OptionName { TaskBattleVentCooldown, MadAvengerMeetingPlayerCount, MadAvengerReserveTimeCanVent, MadAvengerCanSeeImpCount }

    public bool? CheckKillFlash(MurderInfo info) => canSeeKillFlash;
    public bool? CheckSeeDeathReason(PlayerControl seen) => canSeeDeathReason;
    public override CustomRoles TellResults(PlayerControl player) => Options.MadTellOpt();

    public static void SetupOptionItem()
    {
        SoloWinOption.Create(RoleInfo, 10);
        OptionCooldown = FloatOptionItem.Create(RoleInfo, 13, OptionName.TaskBattleVentCooldown, new(0f, 180f, 0.5f), 45f, false).SetValueFormat(OptionFormat.Seconds);
        OptionCount = IntegerOptionItem.Create(RoleInfo, 14, OptionName.MadAvengerMeetingPlayerCount, new(1, 15, 1), 8, false).SetValueFormat(OptionFormat.Players);
        OptionVent = BooleanOptionItem.Create(RoleInfo, 15, OptionName.MadAvengerReserveTimeCanVent, true, false);
        OptionCanseeimpostorCount = BooleanOptionItem.Create(RoleInfo, 16, OptionName.MadAvengerCanSeeImpCount, true, false);
        Tasks = OverrideTasksData.Create(RoleInfo, 20);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = !fin ? Options.MadmateVentCooldown.GetFloat() + 1 : Cooldown;
        AURoleOptions.EngineerInVentMaxTime = !fin ? Options.MadmateVentMaxTime.GetFloat() : 1;
    }

    public override bool OnCompleteTask(uint taskid)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        if (IsTaskFinished)
        {
            fin = true;
            Player.MarkDirtySettings();
            _ = new LateTask(() =>
            {
                can = true;
                Player.RpcProtectedMurderPlayer();
                Player.RpcResetAbilityCooldown();
            }, 0.18f, "Reset");

        }
        return true;
    }
    public override string GetAbilityButtonText() => MyTaskState.IsTaskFinished && !(PlayerCatch.AllAlivePlayersCount >= Count) ? GetString("MadAvengerAbility") : GetString(StringNames.VentAbility);
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (AmongUsClient.Instance.AmHost is false) return true;
        if ((!IsTaskFinished && PlayerCatch.AllAlivePlayersCount >= Count) || !can)
        {
            return OptionVent.GetBool();
        }
        if (PlayerCatch.AliveImpostorCount > 0)
        {
            MyState.DeathReason = CustomDeathReason.Suicide;
            Player.RpcMurderPlayer(Player);
            Logger.Info("まだ生きてるんだから駄目だよ!!", "MadAvenger");
            return false;
        }
        Skill = true;
        var user = physics.myPlayer;
        physics.RpcBootFromVent(ventId);
        user?.ReportDeadBody(null);
        Logger.Info("ショータイムの時間だ。", "MadAvenger");
        return true;
    }
    public override void AfterMeetingTasks()
    {
        if (Skill)
        {
            MyState.DeathReason = CustomDeathReason.Suicide;
            Player.RpcMurderPlayer(Player);
        }
        Skill = false;
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        if (GameStates.CalledMeeting) return "";
        //seeおよびseenが自分である場合以外は関係なし
        if (!Is(seer) || !Is(seen)) return "";

        return Utils.ColorString(IsTaskFinished && PlayerCatch.AllAlivePlayersCount >= Count ? Palette.ImpostorRed : Palette.DisabledGrey, IsTaskFinished && PlayerCatch.AllAlivePlayersCount >= Count ? "\n" + GetString("MadAvengerchallengeMeeting") : "\n" + GetString("MadAvengerreserve"));
    }
    public override void OnReportDeadBody(PlayerControl ___, NetworkedPlayerInfo __)
    {
        if (!Skill) return;
        if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;
        UtilsNotifyRoles.ExtendedMeetingText = "<#ff1919><u>★</color>" + GetString("MadAvenger") + "</u>";
        _ = new LateTask(() => Utils.AllPlayerKillFlash(), 1.0f, "Kakumeikaigi");
        _ = new LateTask(() => Utils.AllPlayerKillFlash(), 2.5f, "Kakumeikaigi");
        _ = new LateTask(() => Utils.AllPlayerKillFlash(), 4.0f, "Kakumeikaigi");
        _ = new LateTask(() => Utils.SendMessage(GetString("Skill.MadAvenger1")), 3.0f, "Kakumeikaigi");
        _ = new LateTask(() => Utils.SendMessage(GetString("Skill.MadAvenger2")), 6.0f, "Kakumeikaigi");
        _ = new LateTask(() => Utils.SendMessage(GetString("Skill.MadAvenger3")), 9.0f, "Kakumeikaigi");
        _ = new LateTask(() => Utils.SendMessage("<size=175%><b>＿人人人人人人＿\n＞　</b><color=#ff1919>" + GetString("Skill.MadAvenger4") + "</color><b>　＜\n￣ＹＹＹＹＹＹ￣</b>\n\n<size=75%><line-height=1.8pic>" + GetString("Skill.MadAvengerInfo"), title: " <color=#ff1919>" + GetString("MadAvengerMeeting")), 11f, "Kakumeikaigi");
    }
    public List<PlayerControl> Guessd;
    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        var meetingHud = MeetingHud.Instance;
        var hudManager = DestroyableSingleton<HudManager>.Instance.KillOverlay;
        if (Skill)
        {
            if (!Is(voter))
            {
                Utils.SendMessage(GetString("Skill.MadAvengerCantVote"), voter.PlayerId, title: " <color=#ff1919>" + GetString("MadAvengerMeeting"));
                return false;
            }
            if (Is(voter)) //革命家の投票
            {
                if (votedForId == 253 || votedForId == Player.PlayerId) //
                {
                    Utils.SendMessage(GetString("Skill.MadAvengerCantSkip"), Player.PlayerId, title: " <color=#ff1919>" + GetString("MadAvengerMeeting"));
                    return false;
                }
                else
                {
                    var pc = PlayerCatch.GetPlayerById(votedForId);
                    if (pc.IsNeutralKiller() || pc.Is(CustomRoles.GrimReaper))
                    {
                        if (Guessd.Contains(pc))
                        {
                            Utils.SendMessage(GetString("Skill.MadAvengerGuessed"), Player.PlayerId, title: " <color=#ff1919>" + GetString("MadAvengerMeeting"));
                            return false;
                        }
                        Guessd.Add(pc);
                        Player.RpcProtectedMurderPlayer();
                        Utils.SendMessage(GetString("Skill.MadAvengersuccess"), Player.PlayerId, title: " <color=#ff1919>" + GetString("MadAvengerMeeting"));

                        foreach (var go in PlayerCatch.AllPlayerControls.Where(pc => pc != null && !pc.IsAlive()))
                        {
                            Utils.SendMessage(string.Format(GetString("MadAvengerCo"), UtilsName.GetPlayerColor(pc, true)), go.PlayerId, GetString("RMSKillTitle"));
                        }

                        foreach (var Guessdpc in Guessd)
                        {
                            var pc1 = PlayerCatch.AllAlivePlayerControls.Where(pc1 => pc1.IsNeutralKiller() || pc1.Is(CustomRoles.GrimReaper)).Count();
                            if (Guessd.Count == pc1)
                            {
                                //革命成功
                                _ = new LateTask(() => Utils.SendMessage(GetString("Skill.MadAvenger5"), title: $"<color=#ff1919>{GetString("MadAvenger")}　{Utils.ColorString(Main.PlayerColors[Player.PlayerId], $"{Player.name}</b>")}"), 0.5f, "Kakumeiseikou");
                                _ = new LateTask(() => Utils.SendMessage(GetString("Skill.MadAvenger6"), title: $"<color=#ff1919>{GetString("MadAvenger")}　{Utils.ColorString(Main.PlayerColors[Player.PlayerId], $"{Player.name}</b>")}"), 3.5f, "Kakumeiseikou");
                                _ = new LateTask(() => Utils.SendMessage(GetString("Skill.MadAvenger7"), title: $"<color=#ff1919>{GetString("MadAvenger")}　{Utils.ColorString(Main.PlayerColors[Player.PlayerId], $"{Player.name}</b>")}"), 6.5f, "Kakumeiseikou");
                                _ = new LateTask(() => Utils.SendMessage(GetString("Skill.MadAvenger8"), title: $"<color=#ff1919>{GetString("MadAvenger")}　{Utils.ColorString(Main.PlayerColors[Player.PlayerId], $"{Player.name}</b>")}"), 9.5f, "Kakumeiseikou");
                                _ = new LateTask(() =>//殺害処理
                                {
                                    foreach (var pc in PlayerCatch.AllAlivePlayerControls)
                                    {
                                        if (pc.PlayerId != Player.PlayerId)
                                        {
                                            if (pc.Is(CustomRoles.Terrorist)) continue;
                                            pc.SetRealKiller(Player);
                                            pc.RpcMurderPlayer(pc);
                                            var state = PlayerState.GetByPlayerId(pc.PlayerId);
                                            state.DeathReason = CustomDeathReason.Bombed;
                                            state.SetDead();
                                        }
                                        else
                                            RPC.PlaySoundRPC(pc.PlayerId, Sounds.KillSound);
                                    }
                                    CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.Impostor, Player.PlayerId, hantrole: CustomRoles.MadAvenger);
                                }, 15f, "Kakumeiseikou");
                                return true;
                            }
                        }
                        return false;
                    }
                    else
                    {
                        if (AmongUsClient.Instance.AmHost)
                        {
                            Player.RpcExileV2();
                            MyState.DeathReason = CustomDeathReason.Misfire;
                            MyState.SetDead();
                            Utils.SendMessage(UtilsName.GetPlayerColor(Player) + GetString("Meetingkill"), title: GetString("MSKillTitle"));
                            MeetingVoteManager.Instance.ClearAndExile(Player.PlayerId, 253);
                            hudManager.ShowKillAnimation(Player.Data, Player.Data);
                            SoundManager.Instance.PlaySound(Player.KillSfx, false, 0.8f);
                            foreach (var go in PlayerCatch.AllPlayerControls.Where(pc => pc != null && !pc.IsAlive()))
                            {
                                Utils.SendMessage(string.Format(GetString("MadAvengerDie"), UtilsName.GetPlayerColor(pc, true)), go.PlayerId, GetString("RMSKillTitle"));
                            }
                            return true;
                        }
                    }
                    return true;
                }
            }
            else return true;
        }
        else return true;
    }

    public override string GetProgressText(bool comms = false, bool GameLog = false) => !GameLog && OptionCanseeimpostorCount.GetBool() ? $"({PlayerCatch.AliveImpostorCount})" : "";
}