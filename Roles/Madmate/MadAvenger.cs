using System.Linq;
using AmongUs.GameOptions;
using System.Collections.Generic;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Translator;

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
            10500,
            SetupOptionItem,
            "mjb",
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
        Cooldown = OptionCooldown.GetFloat(); ;
        Skill = false;
        Guessd = new(GameData.Instance.PlayerCount);
    }
    private static bool canSeeKillFlash;
    private static bool canSeeDeathReason;
    private static Options.OverrideTasksData Tasks;
    private static OptionItem OptionCooldown;
    private static OptionItem OptionCount;
    private static OptionItem OptionVent;
    public static bool Skill;
    float Cooldown;
    float Count;
    enum OptionName { TaskBattleVentCooldown, MRCount, kakumeimaevento }

    public bool CheckKillFlash(MurderInfo info) => canSeeKillFlash;
    public bool CheckSeeDeathReason(PlayerControl seen) => canSeeDeathReason;

    public static void SetupOptionItem()
    {
        OptionCooldown = FloatOptionItem.Create(RoleInfo, 13, OptionName.TaskBattleVentCooldown, new(0f, 180f, 2.5f), 45f, false).SetValueFormat(OptionFormat.Seconds);
        OptionCount = FloatOptionItem.Create(RoleInfo, 14, OptionName.MRCount, new(1, 15, 1), 8, false).SetValueFormat(OptionFormat.Players);
        OptionVent = BooleanOptionItem.Create(RoleInfo, 15, OptionName.kakumeimaevento, true, false);
        Tasks = Options.OverrideTasksData.Create(RoleInfo, 20);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = MyTaskState.CompletedTasksCount <= MyTaskState.AllTasksCount - 1 ? Options.MadmateVentCooldown.GetFloat() + 1 : Cooldown;
        AURoleOptions.EngineerInVentMaxTime = MyTaskState.CompletedTasksCount <= MyTaskState.AllTasksCount - 1 ? Options.MadmateVentMaxTime.GetFloat() : 1;
    }

    public override bool OnCompleteTask()
    {
        Player.RpcResetAbilityCooldown();
        Player.MarkDirtySettings();
        if (IsTaskFinished)
        {
            Player.RpcProtectedMurderPlayer();
        }
        return true;
    }
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (!IsTaskFinished && Main.AllAlivePlayerControls.Count() >= Count) return OptionVent.GetBool();
        if (Main.AliveImpostorCount != 0)
        {
            PlayerState.GetByPlayerId(Player.PlayerId).DeathReason = CustomDeathReason.Suicide;
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
            PlayerState.GetByPlayerId(Player.PlayerId).DeathReason = CustomDeathReason.Suicide;
            Player.RpcMurderPlayer(Player);
        }
        Skill = false;
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        //seeおよびseenが自分である場合以外は関係なし
        if (!Is(seer) || !Is(seen)) return "";

        return Utils.ColorString(IsTaskFinished && Main.AllAlivePlayerControls.Count() >= Count ? Palette.ImpostorRed : Palette.DisabledGrey, IsTaskFinished && Main.AllAlivePlayerControls.Count() >= Count ? "\nご主人が全滅していたら革命会議を起こせるぞ。" : "\nその時が来るまで準備しろ");
    }
    public override void OnReportDeadBody(PlayerControl ___, GameData.PlayerInfo __)
    {
        if (!Skill) return;
        _ = new LateTask(() => Utils.SendMessage("なにか雰囲気がいつもと違う..."), 3.0f, "Kakumeikaigi");
        _ = new LateTask(() => Utils.SendMessage("そう思った時にはもう手遅れだった。"), 4.5f, "Kakumeikaigi");
        _ = new LateTask(() => Utils.SendMessage("この中の船員が今...革命を...起こそうとしているっ!!!"), 6.0f, "Kakumeikaigi");
        _ = new LateTask(() => Utils.SendMessage("<size=175%><b>＿人人人人人人＿\n＞　</b><color=#ff1919>革命会議</color><b>　＜\n￣ＹＹＹＹＹＹ￣</b>\n\n<size=75%><line-height=1.8pic>★革命会議です。\n革命会議中はマッドアベンジャー以外投票,\n会議中の能力使用はできません。\nマッドアベンジャーが生存しているすべての\n死神,ジャッカル,ジャッカルマフィア\nカウントキラー,エゴイスト,リモートキラー\nに投票するとインポスター陣営の勝利です。\n上記の役職以外に投票すると自殺し会議が終了します。", title: " <color=#ff1919>【===革命の時間だ。===】"), 6.5f, "Kakumeikaigi");
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
                Utils.SendMessage("おっと...今革命を起こしている途中なんだ。\n部外者は大人しく観戦しな。\n", title: "<color=#ff1919>【===革命の時間だ。===】");
                return false;
            }
            if (Is(voter)) //革命家の投票
            {
                if (votedForId == 253 || votedForId == Player.PlayerId) //
                {
                    Utils.SendMessage("おっと...君は今革命を起こしている途中なんだ。\nこの会議で生存しているニュートラルのキル人外を全員当てればインポスター陣営の勝利だ。\n", Player.PlayerId, "<color=#ff1919>【===革命の時間だ。===】");
                    return false;
                }
                else
                {
                    var pc = Utils.GetPlayerById(votedForId);
                    if (pc.IsNeutralKiller() || pc.Is(CustomRoles.GrimReaper))
                    {
                        if (Guessd.Contains(pc))
                        {
                            Utils.SendMessage("そいつはもう推測に成功したよ。\nまだニュートラルキラーが残ってるから他を殺るんだ。", Player.PlayerId, "<color=#ff1919>【===革命の時間だ。===】");
                            return false;
                        }
                        Guessd.Add(pc);
                        Player.RpcProtectedMurderPlayer();
                        Utils.SendMessage("良く当てた!\nそいつはニュートラルキラーだ。", Player.PlayerId, "<color=#ff1919>【===革命の時間だ。===】");
                        foreach (var Guessdpc in Guessd)
                        {
                            var pc1 = Main.AllAlivePlayerControls.Where(pc1 => pc1.IsNeutralKiller() || pc1.Is(CustomRoles.GrimReaper)).Count();
                            if (Guessd.Count == pc1)
                            {
                                //革命成功
                                _ = new LateTask(() => Utils.SendMessage("ふっふっふ...", title: $"<color=#ff1919>{GetString("MadAvenger")}　{Utils.ColorString(Main.PlayerColors[Player.PlayerId], $"{Player.name}</b>")}"), 0.5f, "Kakumeiseikou");
                                _ = new LateTask(() => Utils.SendMessage("君たちに良いお知らせがある。", title: $"<color=#ff1919>{GetString("MadAvenger")}　{Utils.ColorString(Main.PlayerColors[Player.PlayerId], $"{Player.name}</b>")}"), 1.5f, "Kakumeiseikou");
                                _ = new LateTask(() => Utils.SendMessage("この船は私が乗っ取らせてもらった!!", title: $"<color=#ff1919>{GetString("MadAvenger")}　{Utils.ColorString(Main.PlayerColors[Player.PlayerId], $"{Player.name}</b>")}"), 3.0f, "Kakumeiseikou");
                                _ = new LateTask(() => Utils.SendMessage("君たちに明日は訪れない。", title: $"<color=#ff1919>{GetString("MadAvenger")}　{Utils.ColorString(Main.PlayerColors[Player.PlayerId], $"{Player.name}</b>")}"), 4.5f, "Kakumeiseikou");
                                _ = new LateTask(() =>//殺害処理
                                {
                                    foreach (var pc in Main.AllAlivePlayerControls)
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
                                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Impostor);
                                    CustomWinnerHolder.WinnerIds.Add(Player.PlayerId);
                                }, 7f, "Kakumeiseikou");
                                return true;
                            }
                        }
                        return false;
                    }
                    else
                    {
                        PlayerState state;
                        if (AmongUsClient.Instance.AmHost)
                        {
                            state = PlayerState.GetByPlayerId(Player.PlayerId);
                            Player.RpcExileV2();
                            state.DeathReason = CustomDeathReason.Misfire;
                            state.SetDead();
                            Utils.SendMessage(Utils.GetPlayerColor(Player) + GetString("Meetingkill"), title: GetString("MSKillTitle"));
                            MeetingVoteManager.Instance.ClearAndExile(Player.PlayerId, 253);
                            hudManager.ShowKillAnimation(Player.Data, Player.Data);
                            SoundManager.Instance.PlaySound(Player.KillSfx, false, 0.8f);
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
}