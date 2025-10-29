using AmongUs.GameOptions;
using Hazel;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;

public sealed class Curser : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Curser),
            player => new Curser(player),
            CustomRoles.Curser,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            6600,
            SetupCustomOption,
            "cs",
            OptionSort: (7, 2)
        );
    public Curser(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CurseKillCooldown = OptionCurseKillCooldown.GetFloat();
        KillCooldown = OptionKillCooldown.GetFloat();
        Cooldown = OptionCooldown.GetFloat();
        CurseCount = OptionCurseCount.GetInt();

        TargetId = byte.MaxValue;
    }
    private static OptionItem OptionCurseKillCooldown;
    private static OptionItem OptionKillCooldown;
    private static OptionItem OptionCurseCount;
    private static OptionItem OptionCooldown;
    private static float CurseKillCooldown;
    private static float KillCooldown;
    private static float Cooldown;
    int CurseCount;
    byte TargetId;
    enum OptionName
    {
        CurserCurseKillCooldown,
        CurserCurseCount
    }
    private static void SetupCustomOption()
    {
        OptionCurseKillCooldown = FloatOptionItem.Create(RoleInfo, 9, OptionName.CurserCurseKillCooldown, OptionBaseCoolTime, 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, OptionBaseCoolTime, 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionCooldown = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.Cooldown, OptionBaseCoolTime, 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionCurseCount = IntegerOptionItem.Create(RoleInfo, 12, OptionName.CurserCurseCount, new(1, 15, 1), 2, false);
    }
    private void SendRPC()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        using var sender = CreateSender();
        sender.Writer.Write(TargetId);
        sender.Writer.Write(CurseCount);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        TargetId = reader.ReadByte();
        CurseCount = reader.ReadInt32();
    }

    public override bool CheckShapeshift(PlayerControl target, ref bool animate)
    {
        if ((target.Is(CustomRoleTypes.Impostor) && !SuddenDeathMode.NowSuddenDeathMode) || CurseCount == 0 || target.PlayerId == TargetId) return false;

        TargetId = target.PlayerId;
        Logger.Info($"{Player.GetNameWithRole()}のターゲットを{target.GetNameWithRole()}に設定", "CurserTarget");
        Player.MarkDirtySettings();
        Player.RpcResetAbilityCooldown();
        UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player);
        CurseCount--;
        SendRPC();
        return false;
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        if (!info.IsSuicide)
        {
            (var killer, var target) = info.AttemptTuple;
            // ターゲットを設定したプレイヤーを切ったらTKillcooldownにする処理
            if (TargetId == target.PlayerId)
            {
                Logger.Info($"{Player.GetNameWithRole()}が呪い殺しました", "CurserShapeshift");
                Main.AllPlayerKillCooldown[killer.PlayerId] = CurseKillCooldown;
                TargetId = byte.MaxValue;
                Player.SyncSettings(); // キルクールダウンを同期
            }
            else
            {
                Logger.Info($"{Player.GetNameWithRole()}が通常キルしました", "CurserNormalKillCooldown");
                Main.AllPlayerKillCooldown[killer.PlayerId] = KillCooldown;
                Player.SyncSettings(); // 通常のキルクールダウンを同期
            }
        }
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (isForMeeting || !Player.IsAlive()) return "";
        if (!Is(seer) || !Is(seen)) return "";

        var target = PlayerCatch.GetPlayerById(TargetId);
        return target != null ? $"Target:{UtilsName.GetPlayerColor(target.PlayerId)}" : "";
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool _ = false)
    {
        seen ??= seer;
        return TargetId == seen.PlayerId ? Utils.ColorString(Palette.ImpostorRed, "θ") : "";
    }
    public override string GetProgressText(bool comms = false, bool gamelog = false) => Utils.ColorString(RoleInfo.RoleColor, $"({CurseCount})");
    public override void ApplyGameOptions(IGameOptions opt) => AURoleOptions.ShapeshifterCooldown = Cooldown;
    public float CalculateKillCooldown() => KillCooldown;
    public override string GetAbilityButtonText() => GetString("MadChanger_Targetset");
}
