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
        cantaskcount = OptionCanTaskcount.GetInt();
        Receivedcount = 0;
    }
    public override void Add()
    {
        Awakened = !OptAwakening.GetBool() || OptionCanTaskcount.GetInt() < 1;

        Psychics.Add(this);
    }
    static OptionItem OptAwakening;
    static OptionItem OptionCanTaskcount;
    static OptionItem OptionCallRate;
    static OptionItem OptionTaskAddRate;
    static float callrate;
    static bool taskaddrate;
    static int cantaskcount;
    bool Awakened;
    static HashSet<Psychic> Psychics = new();

    int Receivedcount;
    enum OptionName
    {
        PsychicCallRate,
        PsychicTaskAddrate
    }
    private static void SetupOptionItem()
    {
        OptionCallRate = FloatOptionItem.Create(RoleInfo, 12, OptionName.PsychicCallRate, new(0, 100, 1), 50, false).SetValueFormat(OptionFormat.Percent);
        OptionTaskAddRate = BooleanOptionItem.Create(RoleInfo, 13, OptionName.PsychicTaskAddrate, false, false);
        OptAwakening = BooleanOptionItem.Create(RoleInfo, 10, GeneralOption.AbilityAwakening, false, false);
        OptionCanTaskcount = IntegerOptionItem.Create(RoleInfo, 14, GeneralOption.cantaskcount, new(1, 255, 1), 5, false);
    }
    public override bool OnCompleteTask(uint taskid)
    {
        if (MyTaskState.HasCompletedEnoughCountOfTasks(cantaskcount))
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
        if (MyTaskState.HasCompletedEnoughCountOfTasks(cantaskcount) is false) return 0;
        var MaxPercent = callrate * 100;
        float proportion = (MyTaskState.CompletedTasksCount - cantaskcount) * 100 / (MyTaskState.AllTasksCount - cantaskcount);

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
                ps.Receivedcount++;
            }
        }
    }
    public override void CheckWinner(GameOverReason reason)
    {
        Achievements.RpcCompleteAchievement(Player.PlayerId, 1, achievements[0], Receivedcount);
        Achievements.RpcCompleteAchievement(Player.PlayerId, 1, achievements[1], Receivedcount);
    }
    public override string GetProgressText(bool comms = false, bool GameLog = false)
    {
        if (!GameLog && comms) return "<color=#cccccc> (??)</color>";

        return $"<color={RoleInfo.RoleColorCode}>({GetChance()}%)</color>";
    }
    public static Dictionary<int, Achievement> achievements = new();
    [Attributes.PluginModuleInitializer]
    public static void Load()
    {
        var n1 = new Achievement(RoleInfo, 0, 5, 0, 0);
        var l1 = new Achievement(RoleInfo, 1, 50, 0, 1);
        achievements.Add(0, n1);
        achievements.Add(1, l1);
    }
}
