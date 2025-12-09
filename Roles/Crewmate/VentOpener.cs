using AmongUs.GameOptions;
using Hazel;
using System.Collections.Generic;
using UnityEngine;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;

public sealed class VentOpener : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(VentOpener),
            player => new VentOpener(player),
            CustomRoles.VentOpener,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            12700,
            SetupOptionItem,
            "vo",
            "#fbe000",
            (9, 5),
            introSound: () => GetIntroSound(RoleTypes.Engineer)
        );
    public VentOpener(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CustomRoleManager.OnEnterVentOthers.Add(OnEnterVentOthers);
        currentVent = new();
        count = OptionCount.GetInt();
        cooldown = OptionCooldown.GetInt();
        Defo = count is 0;
        fuhatu = OptinoFuhatu.GetBool();
        Imp = OptionImp.GetBool();
        Mad = OptionMad.GetBool();
        Crew = OptionCrew.GetBool();
        Neutral = OptionNeutral.GetBool();
        taskc = OptionCanTaskcount.GetFloat();
        BlockKill = OptionBlockKill.GetBool();
        //BlockVent = OptionBlockVent.GetBool();
        //BlockSabotage = OptionBlockSabotage.GetBool();

        expelledPlayers = new();
    }

    private static OptionItem OptionCount;
    private static OptionItem OptionCooldown;
    private static OptionItem OptinoFuhatu;
    private static OptionItem OptionImp;
    private static OptionItem OptionCrew;
    private static OptionItem OptionMad;
    private static OptionItem OptionNeutral;
    private static OptionItem OptionCanTaskcount;
    public static OptionItem OptionBlockKill;
    public static OptionItem OptionBlockVent;
    public static OptionItem OptionBlockSabotage;

    static int cooldown;
    static bool fuhatu;
    static bool Imp;
    static bool Crew;
    static bool Mad;
    static bool Neutral;
    static bool Defo;
    static float taskc;
    static bool BlockKill;
    // static bool BlockVent;
    //static bool BlockSabotage;
    int count;

    static Dictionary<byte, int> currentVent;
    List<byte> expelledPlayers;

    enum OptionName
    {
        Cooldown,
        cantaskcount,
        VentOpenerFuhatu,
        VentOpenerImp,
        VentOpenerMad,
        VentOpenerCrew,
        VentOpenerNeutral,
        VentOpenerCount,
        VentOpenerBlockKill,
        VentOpenerBlockVent,
        VentOpenerBlockSabotage
    }

    private static void SetupOptionItem()
    {
        OptionCount = IntegerOptionItem.Create(RoleInfo, 10, OptionName.VentOpenerCount, new(0, 30, 1), 3, false).SetZeroNotation(OptionZeroNotation.Infinity);
        OptionCooldown = IntegerOptionItem.Create(RoleInfo, 11, OptionName.Cooldown, new(0, 999, 1), 30, false);
        OptinoFuhatu = BooleanOptionItem.Create(RoleInfo, 12, OptionName.VentOpenerFuhatu, false, false);
        OptionImp = BooleanOptionItem.Create(RoleInfo, 13, OptionName.VentOpenerImp, true, false);
        OptionMad = BooleanOptionItem.Create(RoleInfo, 14, OptionName.VentOpenerMad, true, false);
        OptionCrew = BooleanOptionItem.Create(RoleInfo, 15, OptionName.VentOpenerCrew, true, false);
        OptionNeutral = BooleanOptionItem.Create(RoleInfo, 16, OptionName.VentOpenerNeutral, true, false);
        OptionCanTaskcount = IntegerOptionItem.Create(RoleInfo, 17, OptionName.cantaskcount, new(0, 99, 1), 5, false);
        OptionBlockKill = BooleanOptionItem.Create(RoleInfo, 18, OptionName.VentOpenerBlockKill, false, false);
        //OptionBlockVent = BooleanOptionItem.Create(RoleInfo, 19, OptionName.VentOpenerBlockVent, false, false);
        // OptionBlockSabotage = BooleanOptionItem.Create(RoleInfo, 20, OptionName.VentOpenerBlockSabotage, false, false);
    }

    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (!CanUseAbility) return false;
        bool check = false;
        foreach (var (playerid, id) in currentVent)
        {
            var pc = PlayerCatch.GetPlayerById(playerid);
            if (pc == null) continue;
            var role = pc.GetCustomRole();
            if (pc.inVent //ベントに入ってるか ↓設定とかのチェック
                && ((role.IsImpostor() && Imp)
                || (role.IsMadmate() && Mad)
                || (role.IsCrewmate() && Crew)
                || (role.IsNeutral() && Neutral)))
            {
                pc.MyPhysics?.RpcBootFromVent(id);
                expelledPlayers.Add(pc.PlayerId);
                check = true;
            }
        }
        if (check)
        {
            Player.KillFlash(false);
        }
        if ((check || fuhatu) && count > 0)
        {
            count--;
            SendRPC();
        }
        if (!CanUseAbility)
        {
            Player.MarkDirtySettings();
        }

        UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player);
        return false;
    }
    public override bool OnCompleteTask(uint taskid)
    {
        if (IsTaskCompleted)
            Player.RpcProtectedMurderPlayer();
        return true;
    }

    private void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(count);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        count = reader.ReadInt32();
    }

    public override bool CanVentMoving(PlayerPhysics physics, int ventId) => false;
    public override string GetProgressText(bool comms = false, bool gamelog = false) => Defo ? "" : Utils.ColorString(CanUseAbility ? RoleInfo.RoleColor : IsTaskCompleted ? Color.red : Color.gray, $"({count})");
    public bool CanUseAbility => (Defo || count > 0) && IsTaskCompleted;
    public bool IsTaskCompleted => MyTaskState.HasCompletedEnoughCountOfTasks(taskc);
    public override bool CanClickUseVentButton => CanUseAbility;

    public static bool OnEnterVentOthers(PlayerPhysics physics, int ventId)
    {
        currentVent[physics.myPlayer.PlayerId] = ventId;
        return true;
    }
    public override void OnVentilationSystemUpdate(PlayerControl user, VentilationSystem.Operation Operation, int ventId)
    {
        if (Operation == VentilationSystem.Operation.Exit)
            currentVent.Remove(user.PlayerId);
        if (Operation == VentilationSystem.Operation.Move)
            currentVent[user.PlayerId] = ventId;
    }

    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        if (BlockKill && expelledPlayers.Contains(info.AttemptKiller.PlayerId))
        {
            info.GuardPower = 1;
        }
        return true;
    }

    //public override bool OnSabotage(PlayerControl player, SystemTypes systemType) => !BlockSabotage || !expelledPlayers.Contains(player.PlayerId);
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target) => expelledPlayers.Clear();

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = cooldown;
        AURoleOptions.EngineerInVentMaxTime = 1;
    }

    public override string GetAbilityButtonText() => GetString("VentOpenerAbility");
    public override bool OverrideAbilityButton(out string text)
    {
        text = "VentOpener_Ability";
        return true;
    }
}
