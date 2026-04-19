using UnityEngine;
using AmongUs.GameOptions;
using Hazel;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using System.Collections.Generic;

namespace TownOfHost.Roles.Impostor;

class Penguin : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Penguin),
            player => new Penguin(player),
            CustomRoles.Penguin,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            4100,
            SetupOptionItem,
            "pe",
            OptionSort: (4, 0),
            from: From.SuperNewRoles
        );
    public Penguin(PlayerControl player)
        : base(RoleInfo, player)
    {
        AbductTimerLimit = OptionAbductTimerLimit.GetFloat();
        MeetingKill = OptionMeetingKill.GetBool();

        CustomRoleManager.OnEnterVentOthers.Add(OnEnterVentOthers);
    }
    public override void OnDestroy()
    {
        Penguins = new();
        AbductVictim = null;
    }

    static OptionItem OptionAbductTimerLimit;
    static OptionItem OptionMeetingKill;

    enum OptionName
    {
        PenguinAbductTimerLimit,
        PenguinMeetingKill,
    }

    private PlayerControl AbductVictim;
    private float AbductTimer;
    private float AbductTimerLimit;
    private bool stopCount;
    private bool MeetingKill;
    static HashSet<Penguin> Penguins = new();

    //拉致中にキルしそうになった相手の能力を使わせないための処置
    public bool IsKiller => AbductVictim == null;
    public static void SetupOptionItem()
    {
        OptionAbductTimerLimit = FloatOptionItem.Create(RoleInfo, 11, OptionName.PenguinAbductTimerLimit, new(5f, 100f, 1f), 10f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionMeetingKill = BooleanOptionItem.Create(RoleInfo, 12, OptionName.PenguinMeetingKill, false, false);
    }
    public override void Add()
    {
        AbductTimer = 255f;
        stopCount = false;
        oldpos = Vector2.zero;
        movecount = 0;
        Penguins.Add(this);
    }
    public override void ApplyGameOptions(IGameOptions opt) => AURoleOptions.ShapeshifterCooldown = AbductVictim != null ? AbductTimer : 255f;
    private void SendRPC()
    {
        using var sender = CreateSender();

        sender.Writer.Write(AbductVictim?.PlayerId ?? 255);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        var victim = reader.ReadByte();
        if (victim == 255)
        {
            AbductVictim = null;
            AbductTimer = 255f;
        }
        else
        {
            AbductVictim = PlayerCatch.GetPlayerById(victim);
            AbductTimer = AbductTimerLimit;
        }
    }
    void AddVictim(PlayerControl target)
    {
        PlayerState.GetByPlayerId(target.PlayerId).CanUseMovingPlatform = MyState.CanUseMovingPlatform = false;
        CheckMurderPatch.TimeSinceLastKill[Player.PlayerId] = 0f;
        AbductVictim = target;
        AbductTimer = AbductTimerLimit;
        Player.RpcResetAbilityCooldown(Sync: true);
        SendRPC();
        movecount = 0;
        oldpos = Player.GetTruePosition();
        target.GetPlayerState().CanMove = false;
        target.MarkDirtySettings();
    }
    void RemoveVictim()
    {
        if (AbductVictim != null)
        {
            PlayerState.GetByPlayerId(AbductVictim.PlayerId).CanUseMovingPlatform = true;
            AbductVictim.GetPlayerState().CanMove = true;
            AbductVictim.MarkDirtySettings();
            AbductVictim = null;
        }
        MyState.CanUseMovingPlatform = true;
        AbductTimer = 255f;
        Player.RpcResetAbilityCooldown(Sync: true);
        SendRPC();
        movecount = 0;
        if (100 < movecount)
            Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[1]);
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        var target = info.AttemptTarget;
        if (AbductVictim != null)
        {
            if (target != AbductVictim)
            {
                //拉致中は拉致相手しか切れない
                Player.RpcMurderPlayer(AbductVictim);
                Player.ResetKillCooldown();
                info.DoKill = false;
            }
            RemoveVictim();
        }
        else
        {
            if (info.CheckHasGuard())
            {
                info.IsGuard = false;
                return;
            }
            info.DoKill = false;
            AddVictim(target);
        }
    }
    public bool OverrideKillButtonText(out string text)
    {
        if (AbductVictim != null)
        {
            text = GetString("KillButtonText");
        }
        else
        {
            text = GetString("PenguinKillButtonText");
        }
        return true;
    }
    public override string GetAbilityButtonText()
    {
        return GetString("PenguinTimerText");
    }
    public override bool OverrideAbilityButton(out string text)
    {
        text = "Penguin_Ability";
        return true;
    }
    public override bool CanUseAbilityButton()
    {
        return AbductVictim != null;
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        stopCount = true;
        if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;
        // 時間切れ状態で会議を迎えたらはしご中でも構わずキルする
        if (AbductVictim != null && AbductTimer <= 0f)
        {
            Player.RpcMurderPlayer(AbductVictim);
        }
        if (MeetingKill)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (AbductVictim == null) return;
            Player.RpcMurderPlayer(AbductVictim);
            RemoveVictim();
        }
    }
    public override void OnSpawn(bool initialState)
    {
        RestartAbduct();
    }
    public void RestartAbduct()
    {
        if (AbductVictim != null)
        {
            stopCount = false;
            state = 0;
        }
    }
    static int state = 0;
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!GameStates.IsInTask) return;

        if (!stopCount)
            AbductTimer -= Time.fixedDeltaTime;

        if (AbductVictim != null)
        {
            if (!Player.IsAlive() || !AbductVictim.IsAlive())
            {
                RemoveVictim();
                return;
            }
            if (AbductTimer <= 0f && !Player.MyPhysics.Animations.IsPlayingAnyLadderAnimation())
            {
                // 先にIsDeadをtrueにする(はしごチェイス封じ)
                AbductVictim.Data.IsDead = true;
                GameData.Instance.DirtyAllData();
                // ペンギン自身がはしご上にいる場合，はしごを降りてからキルする
                if (!AbductVictim.MyPhysics.Animations.IsPlayingAnyLadderAnimation())
                {
                    var abductVictim = AbductVictim;
                    _ = new LateTask(() =>
                    {
                        var sId = abductVictim.NetTransform.lastSequenceId + 5;
                        abductVictim.NetTransform.SnapTo(Player.transform.position, (ushort)sId);
                        Player.MurderPlayer(abductVictim);

                        var sender = CustomRpcSender.Create("PenguinMurder");
                        {
                            sender.AutoStartRpc(abductVictim.NetTransform.NetId, (byte)RpcCalls.SnapTo);
                            {
                                NetHelpers.WriteVector2(Player.transform.position, sender.stream);
                                sender.Write(abductVictim.NetTransform.lastSequenceId);
                            }
                            sender.EndRpc();
                            sender.AutoStartRpc(Player.NetId, (byte)RpcCalls.MurderPlayer);
                            {
                                sender.WriteNetObject(abductVictim);
                                sender.Write((int)ExtendedPlayerControl.SucceededFlags);
                            }
                            sender.EndRpc();
                        }
                        sender.SendMessage();
                    }, 0.3f, "PenguinMurder");
                    RemoveVictim();
                    Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[0]);
                }
            }
            // はしごの上にいるプレイヤーにはSnapToRPCが効かずホストだけ挙動が変わるため，一律でテレポートを行わない
            else if (!AbductVictim.MyPhysics.Animations.IsPlayingAnyLadderAnimation())
            {
                int div = 3;
                state++;
                if (state % div == 0)
                {
                    var position = Player.transform.position;
                    movecount += Vector2.Distance(position, oldpos);
                    oldpos = position;
                    if (Player.PlayerId != 0)
                    {
                        AbductVictim.RpcSnapToForced(position, SendOption.None);
                    }
                    else
                    {
                        _ = new LateTask(() =>
                        {
                            if (AbductVictim != null)
                            {
                                AbductVictim.RpcSnapToForced(position, SendOption.None);
                            }
                        }
                        , 0.25f, "", true);
                    }
                }
            }
        }
        else if (AbductTimer <= 100f)
        {
            AbductTimer = 255f;
            Player.RpcResetAbilityCooldown(Sync: true);
        }
    }
    public override void OnMurderPlayerAsTarget(MurderInfo info)
    {
        if (AbductVictim != null)
        {
            if (info.AttemptKiller.PlayerId == AbductVictim.PlayerId)
                Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[2]);
        }
    }
    public static bool OnEnterVentOthers(PlayerPhysics physics, int ventId)
    {
        var user = physics.myPlayer;
        foreach (var Penguin in Penguins)
        {
            if (Penguin.AbductVictim != null)
            {
                if (Penguin.AbductVictim.PlayerId == user.PlayerId)
                {
                    _ = new LateTask(() =>
                    {
                        if (user.inVent && user.IsAlive())
                            physics.RpcBootFromVent(ventId);
                    }, 0.8f, "PenginTargetnonvent", true);
                    return true;
                }
            }
        }
        return true;
    }
    Vector2 oldpos;
    float movecount;
    public static Dictionary<int, Achievement> achievements = new();
    [Attributes.PluginModuleInitializer]
    public static void Load()
    {
        var n1 = new Achievement(RoleInfo, 0, 1, 0, 0);
        var l1 = new Achievement(RoleInfo, 1, 1, 0, 1);
        var l2 = new Achievement(RoleInfo, 2, 1, 0, 1);
        achievements.Add(0, n1);
        achievements.Add(1, l1);
        achievements.Add(2, l2);
    }
}
