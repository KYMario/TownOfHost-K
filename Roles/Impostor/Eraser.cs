using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Hazel;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;

public sealed class Eraser : RoleBase, IImpostor, IUsePhantomButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Eraser),
            player => new Eraser(player),
            CustomRoles.Eraser,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Impostor,
            5700,
            SetupOptionItem,
            "Er",
            OptionSort: (6, 3)
        );
    public Eraser(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCooldown = OptionKillCoolDown.GetFloat();
        AbilityCoolDown = OptionAbilityCoolDown.GetFloat();
        MaxUseCount = OptionUseCount.GetInt();
        CanDelCrew = OptionCanDelCrewmate.GetBool();
        CanDelSheriffRole = OptionCanDelSheriffRole.GetBool();
        CanDelMad = OptionCanDelMadmate.GetBool();
        CanDelNeu = OptionCanDelNeutral.GetBool();
        DeltimingAfterMeeting = OptionDeltimingAfterMeeting.GetBool();

        UseCount = 0;
        EraseTargets.Clear();
        EraseMarkTargets.Clear();
        TryedEraseds.Clear();
    }
    static OptionItem OptionKillCoolDown; static float KillCooldown;
    static OptionItem OptionAbilityCoolDown; static float AbilityCoolDown;
    static OptionItem OptionUseCount; static int MaxUseCount;
    static OptionItem OptionCanDelCrewmate; static bool CanDelCrew;
    static OptionItem OptionCanDelSheriffRole; static bool CanDelSheriffRole;
    static OptionItem OptionCanDelMadmate; static bool CanDelMad;
    static OptionItem OptionCanDelNeutral; static bool CanDelNeu;

    static OptionItem OptionDeltimingAfterMeeting; static bool DeltimingAfterMeeting;

    int UseCount;//使用回数
    List<byte> EraseTargets = new();//消す用。
    List<byte> EraseMarkTargets = new();//消す予定の人のマーク
    List<byte> TryedEraseds = new();//消したと思ってる人のマーク

    static List<CustomRoles> IsSheriffRole =
    [CustomRoles.Sheriff, CustomRoles.SwitchSheriff, CustomRoles.MeetingSheriff, CustomRoles.WolfBoy];

    enum OptionName
    {
        EraserDelTimingAfterMeeting,
        EraserCanDelCrewmate,
        EraserCanDelSheriffRole,
        EraserCanDelMadmate,
        EraserCanDelNeutral
    }

    private static void SetupOptionItem()
    {
        OptionKillCoolDown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, OptionBaseCoolTime, 20f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionAbilityCoolDown = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.Cooldown, OptionBaseCoolTime, 20f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionUseCount = IntegerOptionItem.Create(RoleInfo, 12, GeneralOption.OptionCount, new(1, 20, 1), 2, false).SetValueFormat(OptionFormat.Times);
        OptionDeltimingAfterMeeting = BooleanOptionItem.Create(RoleInfo, 13, OptionName.EraserDelTimingAfterMeeting, false, false);
        OptionCanDelCrewmate = BooleanOptionItem.Create(RoleInfo, 14, OptionName.EraserCanDelCrewmate, true, false);
        OptionCanDelSheriffRole = BooleanOptionItem.Create(RoleInfo, 15, OptionName.EraserCanDelSheriffRole, true, false);
        OptionCanDelMadmate = BooleanOptionItem.Create(RoleInfo, 16, OptionName.EraserCanDelMadmate, false, false);
        OptionCanDelNeutral = BooleanOptionItem.Create(RoleInfo, 17, OptionName.EraserCanDelNeutral, false, false);
    }
    public float CalculateKillCooldown() => KillCooldown;
    public override void ApplyGameOptions(IGameOptions opt) => AURoleOptions.PhantomCooldown = AbilityCoolDown;
    public void OnClick(ref bool AdjustKillCooldown, ref bool? ResetCooldown)
    {
        AdjustKillCooldown = true;
        ResetCooldown = false;

        var target = Player.GetKillTarget(true);
        if (!target.IsAlive()) return;
        if (EraseMarkTargets.Contains(target.PlayerId)) return;
        if (MaxUseCount <= UseCount) return;

        AdjustKillCooldown = false;
        ResetCooldown = true;
        UseCount++;

        EraseMarkTargets.Add(target.PlayerId);//マークつける用
        EraseTargets.Add(target.PlayerId);//消す予定の人

        RpcUseAbility(target.PlayerId);

        _ = new LateTask(() =>
            Player.SetKillCooldown(target: target), Main.LagTime, "EraserNoyatu", true);

        if (!DeltimingAfterMeeting) RpcErasePlayer();

        UtilsNotifyRoles.NotifyRoles(Player);
    }
    bool IUsePhantomButton.IsPhantomRole => MaxUseCount > UseCount;
    void ErasePlayer()
    {
        if (!Player.IsAlive()) return;
        foreach (var player in PlayerCatch.AllPlayerControls.Where(x => EraseTargets.Contains(x.PlayerId)))
        {
            if (player == null) continue;
            Logger.Info($"{Player?.Data?.GetLogPlayerName()} => {player?.Data?.GetLogPlayerName()}を消そうとしてる！", "Eraser");

            var role = player.GetCustomRole();
            //インポスターならキャンセル
            if (role.IsImpostor() && !SuddenDeathMode.NowSuddenDeathMode) continue;
            if (player.IsAlive() is false) continue;//既に死んでたら役職を消さない。

            //消したと思ってるリスト
            TryedEraseds.Add(player.PlayerId);
            //クルーでクルー消せないなら
            if (role.IsCrewmate() && !CanDelCrew) continue;
            //シェリフ系役職で消せないなら
            if (IsSheriffRole.Contains(role) && !CanDelSheriffRole) continue;
            //マッドメイト系でマッドメイト消せないなら
            if (role.IsMadmate() && !CanDelMad) continue;
            //ニュートラルでニュートラル消せないなら
            if (role.IsNeutral() && !CanDelNeu) continue;

            UtilsGameLog.AddGameLog("Eraser", string.Format(GetString("EraserMeg"), UtilsName.GetPlayerColor(Player), UtilsName.GetPlayerColor(player)));
            Logger.Info($"{Player?.Data?.GetLogPlayerName()} => {player?.Data?.GetLogPlayerName()}をのロールをクルーに", "Eraser");

            //ホストのみ実行
            if (AmongUsClient.Instance.AmHost)
            {
                player.RpcSetCustomRole(SuddenDeathMode.NowSuddenDeathMode ? CustomRoles.Impostor : CustomRoles.Crewmate, true, null);
            }
        }
        EraseTargets.Clear();
    }

    public override void AfterMeetingTasks()
    {
        if (DeltimingAfterMeeting) ErasePlayer();
    }
    public override string GetProgressText(bool comms = false, bool GameLog = false) => $"<color=#{(MaxUseCount <= UseCount ? "758593" : "ff1919")}>({UseCount}/{MaxUseCount})</color>";
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;

        //消された人
        if (TryedEraseds.Contains(seen.PlayerId)) return "<color=#ff1919>□</color>";

        //死んでたら消す予定の人の処理をしない
        if (!Player.IsAlive()) return "";

        //消す予定の人
        if (EraseMarkTargets.Contains(seen.PlayerId)) return "<color=#ff1919>■</color>";
        return "";
    }
    public override string GetAbilityButtonText() => GetString("EraserAbility");
    public override bool OverrideAbilityButton(out string text)
    {
        text = "Eraser_Ability";
        return true;
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (seen.PlayerId != seer.PlayerId || isForMeeting || MaxUseCount <= UseCount || !Player.IsAlive()) return "";

        if (isForHud) return GetString("PhantomButtonKilltargetLowertext");
        return $"<size=50%>{GetString("PhantomButtonKilltargetLowertext")}</size>";
    }

    void RpcErasePlayer()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        using var sender = CreateSender();
        sender.Writer.WritePacked((int)RPC_Types.ErasePlayer);
        ErasePlayer();
    }

    void RpcUseAbility(byte targetId)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        using var sender = CreateSender();
        sender.Writer.WritePacked((int)RPC_Types.UseAbility);
        sender.Writer.Write(UseCount);
        sender.Writer.Write(targetId);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        switch ((RPC_Types)reader.ReadPackedInt32())
        {
            case RPC_Types.ErasePlayer:
                ErasePlayer(); break;
            case RPC_Types.UseAbility:
                var targetId = reader.ReadByte();
                UseCount = reader.ReadInt32();
                EraseMarkTargets.Add(targetId);//マークつける用
                EraseTargets.Add(targetId);//消す予定の人
                break;
        }
    }

    enum RPC_Types
    {
        ErasePlayer,
        UseAbility,
    }
}