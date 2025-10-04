using AmongUs.GameOptions;
using HarmonyLib;
using UnityEngine;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;

public sealed class Jumper : RoleBase, IImpostor, IUsePhantomButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Jumper),
            player => new Jumper(player),
            CustomRoles.Jumper,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Impostor,
            3000,
            SetupOptionItem,
            "Jm",
            OptionSort: (1, 1),
            Desc: () => string.Format(GetString("JumperDesc"), OptionJumpdis.GetFloat(), OptionJumpcount.GetFloat())
        );
    public Jumper(PlayerControl player)
    : base(
        RoleInfo,
        player
        )
    {
        JumpToPosition = new Vector2(999f, 999f);
        UsePosition = new Vector2(999f, 999f);
        timer = 0;
        NowJumpcount = 0;
        Jumping = false;
        ShowMark = false;
        MySpeed = Main.AllPlayerSpeed[Player.PlayerId];
        PlayerColor = player.Data.DefaultOutfit.ColorId;
        Jumpdis = OptionJumpdis.GetFloat();
        Jumpcount = OptionJumpcount.GetInt();
        onecooltime = OptionOnecooltime.GetFloat();
        Jumpcooltime = OptionJumpcooltime.GetFloat();
        killcool = OptionKillCoolDown.GetFloat();
        Jumpdistance = OptionJumpDistance.GetInt();
    }
    static OptionItem OptionKillCoolDown;
    static OptionItem OptionJumpcount;
    static OptionItem OptionOnecooltime;
    static OptionItem OptionJumpcooltime;
    static OptionItem OptionJumpdis;
    static OptionItem OptionJumpDistance;
    Vector2 JumpToPosition;
    Vector2 UsePosition;
    static float killcool;
    static float onecooltime;
    static float Jumpcooltime;
    static float Jumpdis;
    static int Jumpcount;
    static int Jumpdistance;
    int PlayerColor;
    float JumpX;
    float JumpY;
    float timer;
    float MySpeed;
    int NowJumpcount;
    public bool Jumping;
    bool ShowMark;
    enum Option
    {
        JumperOneCoolTime, JumperCCoolTime, JumperJumpcount, JumperJumpDis, JumperDistance
    }

    static void SetupOptionItem()
    {
        OptionKillCoolDown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 0.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionJumpcount = IntegerOptionItem.Create(RoleInfo, 11, Option.JumperJumpcount, new(1, 30, 1), 4, false);
        OptionJumpDistance = IntegerOptionItem.Create(RoleInfo, 15, Option.JumperDistance, new(1, 3, 1), 1, false);
        OptionOnecooltime = FloatOptionItem.Create(RoleInfo, 12, Option.JumperOneCoolTime, new(0f, 180f, 0.5f), 15f, false).SetValueFormat(OptionFormat.Seconds);
        OptionJumpcooltime = FloatOptionItem.Create(RoleInfo, 13, Option.JumperCCoolTime, new(0f, 180f, 0.5f), 25f, false).SetValueFormat(OptionFormat.Seconds);
        OptionJumpdis = FloatOptionItem.Create(RoleInfo, 14, Option.JumperJumpDis, new(0.2f, 3, 0.1f), 1.5f, false).SetValueFormat(OptionFormat.Seconds);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.PhantomCooldown = JumpToPosition == new Vector2(999f, 999f) ? onecooltime : Jumpcooltime;
    }
    public float CalculateKillCooldown() => killcool;
    public override bool OnEnterVent(PlayerPhysics physics, int ventId) => !Jumping;
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!Jumping) return;

        timer += Time.fixedDeltaTime;

        if (timer > Jumpdis)
        {
            if (NowJumpcount == 1) ShowMark = true;
            UtilsNotifyRoles.NotifyRoles(ForceLoop: true);
            player.RpcSnapToForced(NowJumpcount == Jumpcount ? JumpToPosition : new Vector2(UsePosition.x + JumpX * NowJumpcount, UsePosition.y + JumpY * NowJumpcount));

            if (Jumpcount <= NowJumpcount)
            {
                Jumping = false;
                ShowMark = false;
                JumpToPosition = new Vector2(999f, 999f);
                UsePosition = new Vector2(999f, 999f);
                Main.AllPlayerSpeed[Player.PlayerId] = MySpeed;
                _ = new LateTask(() => player.RpcResetAbilityCooldown(Sync: true), 0.2f, "JumperEndResetCool", null);
                player.SetKillCooldown();
                Player.RpcSetColor((byte)PlayerColor);
                _ = new LateTask(() => UtilsNotifyRoles.NotifyRoles(ForceLoop: true), 0.2f, "JumperEndNotify", null);
            }
            else
            {
                _ = new LateTask(() =>
                    {
                        if (!GameStates.IsMeeting && player.IsAlive())
                        {
                            foreach (var target in PlayerCatch.AllAlivePlayerControls)
                            {
                                if (target.Is(CustomRoles.King) || target.PlayerId == player.PlayerId) continue;

                                float Distance = Vector2.Distance(player.transform.position, target.transform.position);
                                if (Distance <= (Jumpdistance == 1 ? 1.22f : (Jumpdistance == 2 ? 1.82f : 2.2)))
                                {
                                    if (CustomRoleManager.OnCheckMurder(player, target, target, target, true, Killpower: 3))
                                    {
                                        PlayerState.GetByPlayerId(target.PlayerId).DeathReason = CustomDeathReason.Bombed;
                                    }
                                }
                            }
                        }
                    }, Jumpdis - 0.19f, "abo-n", null);
            }
            NowJumpcount++;
            timer = 0;
        }
    }
    public override void OnStartMeeting()
    {
        timer = 0;
        Jumping = false;
        ShowMark = false;
        JumpToPosition = new Vector2(999f, 999f);
        UsePosition = new Vector2(999f, 999f);
        Main.AllPlayerSpeed[Player.PlayerId] = MySpeed;
        _ = new LateTask(() => Player.RpcResetAbilityCooldown(Sync: true), 0.2f, "JumperMeetingReset");
    }
    public override void OnReportDeadBody(PlayerControl _, NetworkedPlayerInfo __) => Player.RpcSetColor((byte)PlayerColor);
    public bool CanUseKillButton() => !Jumping;
    public void OnClick(ref bool AdjustKillCooldown, ref bool? ResetCooldown)
    {
        if (Jumping) return;
        ResetCooldown = true;
        if (JumpToPosition == new Vector2(999f, 999f))
        {
            JumpToPosition = Player.transform.position;
            AdjustKillCooldown = true;
            Player.RpcSpecificRejectShapeshift(Player, false);
            Player.RpcResetAbilityCooldown(Sync: true);
            Logger.Info($"Set:{JumpToPosition.x}-{JumpToPosition.y} (${Player.PlayerId})", "Jumper");
            _ = new LateTask(() => UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player), 0.2f, "JumperSet", true);
            return;
        }
        timer = 0;
        NowJumpcount = 1;
        AdjustKillCooldown = false;
        Jumping = true;
        ShowMark = true;
        UsePosition = Player.transform.position;
        var X = JumpToPosition.x - UsePosition.x;
        var Y = JumpToPosition.y - UsePosition.y;
        JumpX = X / OptionJumpcount.GetInt();
        JumpY = Y / OptionJumpcount.GetInt();
        Main.AllPlayerSpeed[Player.PlayerId] = Main.MinSpeed;
        Logger.Info($"{Player?.Data?.GetLogPlayerName()}Jump!", "Jumper");
        _ = new LateTask(() =>
        {
            Player.RpcSetPet("");
            Player.SyncSettings();
            int chance = IRandom.Instance.Next(0, 18);
            PlayerCatch.AllPlayerControls.Do(pc => Player.RpcChColor(pc, (byte)chance));
            UtilsNotifyRoles.NotifyRoles(ForceLoop: true);
        }, 0.4f, "Jumper0Speed", true);

    }
    public override bool GetTemporaryName(ref string name, ref bool NoMarker, PlayerControl seer, PlayerControl seen = null)
    {
        seen ??= seer;
        if (Player == seen && ShowMark && !GameStates.CalledMeeting)
        {
            name = Jumpdistance == 1 ? "<line-height=100%> \n \n \n \n \n \n \n \n \n \n \n \n<size=1200%><color=#ff1919>●</color></size></line-height>"
            : (Jumpdistance == 2 ? "<line-height=100%> \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n<size=2100%><color=#ff1919>●</color></size></line-height>"
                : "<line-height=100%> \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n \n<size=2800%><color=#ff1919>●</color></size></line-height>"
            );
            NoMarker = true;
            return true;
        }
        return false;
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (isForMeeting) return "";
        if (Player.IsAlive())
            return JumpToPosition == new Vector2(999f, 999f) ? GetString("Jumper_SetJumpPos") : GetString("Jumper_Jump");
        return "";
    }
    public override bool OverrideAbilityButton(out string text)
    {
        text = "Jumper_Ability";
        return true;
    }
    public override string GetAbilityButtonText()
    {
        return GetString("Jumpertext");
    }
}