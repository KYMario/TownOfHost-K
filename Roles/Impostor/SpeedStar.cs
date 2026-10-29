using System.Collections.Generic;
using AmongUs.GameOptions;
using TownOfHost.Attributes;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;

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
            OptionSort: (6, 12)
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
        Event.OptionLoad.Add("Cakeshop");
        Event.OptionLoad.Add("VegaandAltair");
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