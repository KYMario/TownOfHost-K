using System.Collections.Generic;
using System.Linq;
using Hazel;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Modules;

namespace TownOfHost.Roles.Impostor;

public sealed class Sniper : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Sniper),
            player => new Sniper(player),
            CustomRoles.Sniper,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            6200,
            SetupOptionItem,
            "snp",
            OptionSort: (3, 10),
            from: From.NebulaontheShip,
            Desc: () =>
            {
                var adddesc = "";
                if (SniperAimAssist.GetBool()) adddesc += GetString("SniperDescAimAssist");
                if (OpShowArrowTime.GetBool()) adddesc += string.Format(GetString("SniperDescArrow"), OpShowArrowTime.GetFloat());
                if (OpCankill.GetBool() is false) adddesc += GetString("SniperDescCantKill");

                return string.Format(GetString("SniperDesc"), SniperBulletCount.GetInt()) + adddesc;
            }
        );
    public Sniper(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        MaxBulletCount = SniperBulletCount.GetInt();
        PrecisionShooting = SniperPrecisionShooting.GetBool();
        AimAssist = SniperAimAssist.GetBool();
        AimAssistOneshot = SniperAimAssistOnshot.GetBool();
        Cankill = OpCankill.GetBool();
        CanNomalShape = OpCanShape.GetBool();
        ShapeCooldown = OpShapeCool.GetFloat();
        ShapeDuration = OpShapeDuration.GetFloat();

        CustomRoleManager.SuffixOthers.Add(GetMarkOthers);
    }

    public override void OnDestroy()
    {
        Snipers.Clear();
    }
    static OptionItem SniperBulletCount;
    static OptionItem SniperPrecisionShooting;
    static OptionItem SniperAimAssist;
    static OptionItem SniperAimAssistOnshot;
    static OptionItem OpCanShape;
    static OptionItem OpCankill;
    static OptionItem OpShapeDuration;
    static OptionItem OpShapeCool;
    static OptionItem OpShowArrowTime;
    static OptionItem OpFriendlyFire;
    enum OptionName
    {
        SniperBulletCount,
        SniperPrecisionShooting,
        SniperAimAssist,
        SniperAimAssistOneshot,
        SniperCanKill,
        SniperCanShapeshift,
        SniperShowArrowTime,
        SniperFriendlyFire
    }
    Vector3 SnipeBasePosition;
    Vector3 LastPosition;
    int BulletCount;
    List<byte> ShotNotify = new();
    bool IsAim;
    float AimTime;
    float ShowArrowTime;

    static HashSet<Sniper> Snipers = new();

    int MaxBulletCount;
    bool PrecisionShooting;
    bool AimAssist;
    bool AimAssistOneshot;
    bool Cankill;
    bool CanNomalShape;
    float ShapeCooldown;
    float ShapeDuration;
    bool MeetingReset;
    public static void SetupOptionItem()
    {
        SniperBulletCount = IntegerOptionItem.Create(RoleInfo, 10, OptionName.SniperBulletCount, new(1, 99, 1), 2, false)
            .SetValueFormat(OptionFormat.Pieces);
        OpShapeCool = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.Cooldown, new(0f, 180f, 0.5f), 40f, false).SetValueFormat(OptionFormat.Seconds);
        OpShapeDuration = FloatOptionItem.Create(RoleInfo, 12, GeneralOption.Duration, new(0f, 180f, 0.5f), 10f, false).SetZeroNotation(OptionZeroNotation.Infinity).SetValueFormat(OptionFormat.Seconds);
        SniperPrecisionShooting = BooleanOptionItem.Create(RoleInfo, 13, OptionName.SniperPrecisionShooting, false, false);
        SniperAimAssist = BooleanOptionItem.Create(RoleInfo, 14, OptionName.SniperAimAssist, false, false);
        SniperAimAssistOnshot = BooleanOptionItem.Create(RoleInfo, 15, OptionName.SniperAimAssistOneshot, false, false, SniperAimAssist);
        OpCanShape = BooleanOptionItem.Create(RoleInfo, 16, OptionName.SniperCanShapeshift, false, false);
        OpCankill = BooleanOptionItem.Create(RoleInfo, 17, OptionName.SniperCanKill, true, false);
        OpShowArrowTime = FloatOptionItem.Create(RoleInfo, 18, OptionName.SniperShowArrowTime, new(0f, 60f, 1f), 10f, false).SetZeroNotation(OptionZeroNotation.Off).SetValueFormat(OptionFormat.Seconds);
        OpFriendlyFire = BooleanOptionItem.Create(RoleInfo, 19, OptionName.SniperFriendlyFire, true, false);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterDuration = ShapeDuration;
        AURoleOptions.ShapeshifterCooldown = ShapeCooldown;
    }
    public override void Add()
    {
        Logger.Disable("Sniper");

        SnipeBasePosition = new();
        LastPosition = new();
        BulletCount = MaxBulletCount;
        ShotNotify.Clear();
        IsAim = false;
        AimTime = 0f;
        MeetingReset = false;
        ShowArrowTime = 0f;

        Snipers.Add(this);
    }
    private void SendRPC()
    {
        Logger.Info($"{Player.GetNameWithRole().RemoveHtmlTags()}:SendRPC", "Sniper");
        using var sender = CreateSender();

        var snList = ShotNotify;
        sender.Writer.Write(snList.Count);
        foreach (var sn in snList)
        {
            sender.Writer.Write(sn);
        }
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        ShotNotify.Clear();
        var count = reader.ReadInt32();
        while (count > 0)
        {
            ShotNotify.Add(reader.ReadByte());
            count--;
        }
        Logger.Info($"{Player.GetNameWithRole().RemoveHtmlTags()}:ReceiveRPC", "Sniper");
    }
    public bool CanUseKillButton()
    {
        if (!Player.IsAlive()) return false;
        if (Cankill) return true;
        return BulletCount <= 0;
    }
    public override bool CheckShapeshift(PlayerControl target, ref bool animate)
    {
        if (!Player.IsAlive() || (BulletCount <= 0 && !CanNomalShape)) return false;
        return true;
    }
    /// <summary>
    /// 狙撃の場合死因設定
    /// </summary>
    /// <param name="info"></param>
    public void OnMurderPlayerAsKiller(MurderInfo info)
    {
        //AttemptKillerは自分確定
        //スナイパーがAppearanceKillerだった場合は狙撃じゃない
        //ターゲットが自殺扱いなら狙撃
        if (!Is(info.AppearanceKiller) && info.IsFakeSuicide)
        {
            PlayerState.GetByPlayerId(info.AttemptTarget.PlayerId).DeathReason = CustomDeathReason.Sniped;
        }
    }

    Dictionary<PlayerControl, float> GetSnipeTargets()
    {
        var targets = new Dictionary<PlayerControl, float>();
        //変身開始地点→解除地点のベクトル
        var snipeBasePos = SnipeBasePosition;
        var snipePos = Player.transform.position;
        var dir = (snipePos - snipeBasePos).normalized;

        //至近距離で外す対策に一歩後ろから判定を開始する
        snipePos -= dir;

        foreach (var target in PlayerCatch.AllAlivePlayerControls)
        {
            //自分には当たらない
            if (target.PlayerId == Player.PlayerId) continue;
            //FriendlyFireがOFFかつサドンデスモードでないならImpostorを除外
            if (!OpFriendlyFire.GetBool() && target.GetCustomRole().IsImpostor() && !SuddenDeathMode.NowSuddenDeathMode) continue;
            //FriendlyFireがOffかつ、チームかつ、同陣営なら
            if (!OpFriendlyFire.GetBool() && SuddenDeathMode.NowSuddenDeathTemeMode && SuddenDeathMode.IsSameteam(target.PlayerId, Player.PlayerId)) continue;
            //死んでいない対象の方角ベクトル作成
            var target_pos = target.transform.position - snipePos;
            //自分より後ろの場合はあたらない
            if (target_pos.magnitude < 1) continue;
            //正規化して
            var target_dir = target_pos.normalized;
            //内積を取る
            var target_dot = Vector3.Dot(dir, target_dir);
            Logger.Info($"{target?.Data?.GetLogPlayerName()}:pos={target_pos} dir={target_dir}", "Sniper");
            Logger.Info($"  Dot={target_dot}", "Sniper");

            //ある程度正確なら登録
            if (target_dot < 0.995) continue;

            if (PrecisionShooting)
            {
                //射線との誤差確認
                //単位ベクトルとの外積をとれば大きさ=誤差になる。
                var err = Vector3.Cross(dir, target_pos).magnitude;
                Logger.Info($"  err={err}", "Sniper");
                if (err < 0.5)
                {
                    //ある程度正確なら登録
                    targets.Add(target, err);
                }
            }
            else
            {
                //近い順に判定する
                var err = target_pos.magnitude;
                Logger.Info($"  err={err}", "Sniper");
                targets.Add(target, err);
            }
        }
        return targets;

    }
    public override void OnShapeshift(PlayerControl target)
    {
        var shapeshifting = Player.PlayerId != target.PlayerId;

        if (BulletCount <= 0) return;

        //弾が残ってたら
        if (shapeshifting)
        {
            //Aim開始
            MeetingReset = false;

            //スナイプ地点の登録
            SnipeBasePosition = Player.transform.position;

            LastPosition = Player.transform.position;
            IsAim = true;
            AimTime = 0f;

            return;
        }

        //エイム終了
        IsAim = false;
        AimTime = 0f;

        //ミーティングによる変身解除なら射撃しない
        if (MeetingReset)
        {
            MeetingReset = false;
            return;
        }

        //一発消費して
        BulletCount--;

        //命中判定はホストのみ行う
        if (!AmongUsClient.Instance.AmHost) return;

        var targets = GetSnipeTargets();

        if (targets.Count != 0)
        {
            //一番正確な対象がターゲット
            var snipedTarget = targets.OrderBy(c => c.Value).First().Key;
            CustomRoleManager.OnCheckMurder(
                Player, snipedTarget,       // sniperがsnipedTargetを打ち抜く
                snipedTarget, snipedTarget, true, Killpower: 1 // 表示上はsnipedTargetの自爆
            );

            //あたった通知
            if (CanUseKillButton()) Player.SetKillCooldown();
            else Player.RpcProtectedMurderPlayer(target);

            //スナイプが起きたことを聞こえそうな対象に通知したい
            targets.Remove(snipedTarget);
            var snList = ShotNotify;
            snList.Clear();
            foreach (var otherPc in targets.Keys)
            {
                snList.Add(otherPc.PlayerId);
                UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: otherPc);
            }
            SendRPC();
            _ = new LateTask(() =>
                {
                    snList.Clear();
                    if (targets.Count != 0)
                    {
                        foreach (var otherPc in targets.Keys)
                        {
                            UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: otherPc);
                        }
                        SendRPC();
                    }
                },
                0.5f, "Sniper shot Notify");
        }
        _ = new LateTask(() =>
        {
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                GetArrow.Add(pc.PlayerId, SnipeBasePosition);
                ShowArrowTime = OpShowArrowTime.GetFloat();
            }
        }, Main.LagTime, "", true);
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!Player.IsAlive()) return;

        if (ShowArrowTime > 0 && !GameStates.CalledMeeting)
        {
            ShowArrowTime -= Time.fixedDeltaTime;
            if (ShowArrowTime <= 0)
            {
                foreach (var pc in PlayerCatch.AllPlayerControls)
                    GetArrow.Remove(pc.PlayerId, SnipeBasePosition);
                _ = new LateTask(() => UtilsNotifyRoles.NotifyRoles(), 0.35f, "", true);
            }
        }

        if (!AimAssist) return;

        if (!IsAim) return;

        if (GameStates.CalledMeeting)
        {
            //エイム終了
            IsAim = false;
            AimTime = 0f;
            foreach (var pc in PlayerCatch.AllPlayerControls)
                GetArrow.Remove(pc.PlayerId, SnipeBasePosition);
            return;
        }

        var pos = Player.transform.position;
        if (pos != LastPosition)
        {
            AimTime = 0f;
            LastPosition = pos;
        }
        else
        {
            AimTime += Time.fixedDeltaTime;
            UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player);
        }
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        MeetingReset = true;
        foreach (var pc in PlayerCatch.AllPlayerControls)
            GetArrow.Remove(pc.PlayerId, SnipeBasePosition);
    }
    public override string GetProgressText(bool comms = false, bool gamelog = false)
    {
        return Utils.ColorString(Color.yellow, $"({BulletCount})");
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (!Is(seer) || !Is(seen)) return "";

        if (AimAssist)
        {
            //エイムアシスト中のスナイパー
            if (0.5f < AimTime && (!AimAssistOneshot || AimTime < 1.0f))
            {
                if (GetSnipeTargets().Count > 0)
                {
                    return $"<size=200%>{Utils.ColorString(Palette.ImpostorRed, "◎")}</size>";
                }
            }
        }
        return "";
    }
    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        var arrow = "";
        if (isForMeeting) return "";

        //各スナイパーから
        foreach (var sniper in Snipers)
        {
            if (sniper.ShowArrowTime > 0 && OpShowArrowTime.GetFloat() != 0)
                arrow += "<size=90%><color=red>" + GetArrow.GetArrows(seer, sniper.SnipeBasePosition) + "</color></size>";

            //射撃音が聞こえるプレイヤー
            var snList = sniper.ShotNotify;
            if (snList.Count > 0 && snList.Contains(seer.PlayerId))
            {
                return seer == seen ? $"<size=200%>{Utils.ColorString(Palette.ImpostorRed, "!" + arrow)}</size>" : "";
            }
        }

        return seer == seen ? arrow : "";
    }
    public override string GetAbilityButtonText()
    {
        return GetString(BulletCount <= 0 ? "DefaultShapeshiftText" : "SniperSnipeButtonText");
    }
}