using System.Collections.Generic;

using AmongUs.GameOptions;
using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;

public sealed class Psychic : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Psychic),
            player => new Psychic(player),
            CustomRoles.Psychic,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            11200,
            SetupOptionItem,
            "Ps",
            "#a34fee",
            (6, 3),
            false,
            from: From.None
        );
    public Psychic(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        callrate = OptionCallRate.GetFloat();
        taskaddrate = OptionTaskAddRate.GetBool();
    }
    public override void Add()
    {
        Awakened = !OptAwakening.GetBool();

        Psychics.Add(this);
    }
    static OptionItem OptAwakening;
    static OptionItem OptAwakeningTaskcount;
    static OptionItem OptionCallRate;
    static OptionItem OptionTaskAddRate;
    static float callrate;
    static bool taskaddrate;
    bool Awakened;
    static HashSet<Psychic> Psychics = new();
    enum OptionName
    {
        PsychicCallRate,
        PsychicTaskAddrate
    }
    private static void SetupOptionItem()
    {
        OptionCallRate = FloatOptionItem.Create(RoleInfo, 12, OptionName.PsychicCallRate, new(0, 100, 1), 50, false).SetValueFormat(OptionFormat.Percent);
        OptionTaskAddRate = BooleanOptionItem.Create(RoleInfo, 13, OptionName.PsychicTaskAddrate, false, false);
        OptAwakening = BooleanOptionItem.Create(RoleInfo, 10, GeneralOption.TaskAwakening, false, false);
        OptAwakeningTaskcount = IntegerOptionItem.Create(RoleInfo, 14, GeneralOption.AwakeningTaskcount, new(1, 255, 1), 5, false, OptAwakening);
    }
    public override bool OnCompleteTask(uint taskid)
    {
        if (MyTaskState.HasCompletedEnoughCountOfTasks(OptAwakeningTaskcount.GetInt()))
        {
            if (Awakened == false)
                if (!Utils.RoleSendList.Contains(Player.PlayerId))
                    Utils.RoleSendList.Add(Player.PlayerId);
            Awakened = true;
        }
        return true;
    }
    public override CustomRoles Misidentify() => Awakened ? CustomRoles.NotAssigned : CustomRoles.Crewmate;
    public override void OnDestroy() => Psychics.Clear();
    public float GetChance()
    {
        var MaxPercent = callrate * 100;
        float proportion = MyTaskState.CompletedTasksCount * 100 / MyTaskState.AllTasksCount;

        if (taskaddrate)
        {
            MaxPercent = callrate * proportion;
        }

        return MaxPercent / 100;
    }
    public static void CanAbility(PlayerControl target)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        foreach (var ps in Psychics)
        {
            var random = IRandom.Instance.Next(100);
            if (ps.Player.IsAlive() && ps.Awakened && ps.GetChance() > random)
            {
                if (ps.Player == PlayerControl.LocalPlayer)
                    target.StartCoroutine(target.CoSetRole(RoleTypes.Noisemaker, true));
                else
                    target.RpcSetRoleDesync(RoleTypes.Noisemaker, ps.Player.GetClientId());
                target.SyncSettings();
            }
        }
    }
    public override string GetProgressText(bool comms = false, bool GameLog = false)
    {
        if (!GameLog && comms) return "<color=#cccccc> (??)</color>";

        return $"<color={RoleInfo.RoleColorCode}>({GetChance()}%)</color>";
    }
}
