using AmongUs.GameOptions;
using Hazel;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using UnityEngine;
using static TownOfHost.Modules.SelfVoteManager;
using static TownOfHost.PlayerCatch;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Neutral;

public sealed class JackalHadouHo : RoleBase, ILNKiller, IUsePhantomButton, ISelfVoter
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(JackalHadouHo),
            player => new JackalHadouHo(player),
            CustomRoles.JackalHadouHo,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Neutral,
            26400,
            SetUpOptionItem,
            "jhh",
            "#00b4eb",
            (1, 4),
            true,
            countType: CountTypes.Jackal,
            assignInfo: new RoleAssignInfo(CustomRoles.JackalHadouHo, CustomRoleTypes.Neutral)
            {
                AssignCountRule = new(1, 1, 1)
            },
            from: From.SuperNewRoles
        );

    public JackalHadouHo(PlayerControl player)
        : base(RoleInfo, player, () => HasTask.False)
    {
        KillCooldown = OptionKillCooldown.GetFloat();
        Cooldown = OptionCoolDown.GetFloat();
        ChargeTime = OptionChargeTime.GetFloat();
        SuperChargeTime = OptionSuperChargeTime.GetFloat();
        SelfDestructOnMiss = OptionSelfDestructOnMiss.GetBool();
        KillJackal = OptionKillJackal.GetBool();
        BeamColorModeValue = OptionBeamColorMode.GetValue();
        CanVent = OptionCanVent.GetBool();
        CanSabotage = OptionCanSabotage.GetBool();
        HasImpostorVision = OptionHasImpostorVision.GetBool();
        CanSideKick = OptionCanMakeSidekick.GetBool();

        IsCharging = false;
        IsSuperCharging = false;
        chargeTimer = 0f;
        superChargeTimer = 0f;
        PlayerSpeed = 0f;
        colorchange = 0f;
        ShowBeamMark = false;
        HasHit = false;
        IsDead = false;
        IsFiring = false;
        IsLoaded = false;
        IsSuperBeam = false;

        CustomRoleManager.LowerOthers.Add(GetLowerTextOthers);

        skCandidateId = byte.MaxValue;
        skNearTimer = 0f;
        skCooldownTimer = 0f;
        skSpawnWaitTimer = -1f;
    }

    public bool IsCharging;
    public bool IsSuperCharging;
    float chargeTimer;
    float superChargeTimer;
    float PlayerSpeed;
    public bool ShowBeamMark;
    bool HasHit;
    bool BeamFacingLeft;
    bool IsDead;
    float colorchange;
    int PlayerColor;
    bool IsFiring = false;
    public bool CanSideKick;
    public bool IsLoaded;
    public bool IsSuperBeam;
    public static bool NextNoSideKick = false;

    byte skCandidateId;
    float skNearTimer;
    float skCooldownTimer;
    float skSpawnWaitTimer;
    bool SkCanApproach => skSpawnWaitTimer >= 3f;

    static OptionItem OptionKillCooldown;
    static float KillCooldown;
    static OptionItem OptionCoolDown;
    static float Cooldown;
    static OptionItem OptionChargeTime;
    static float ChargeTime;
    static OptionItem OptionSuperChargeTime;
    static float SuperChargeTime;
    static OptionItem OptionSelfDestructOnMiss;
    static bool SelfDestructOnMiss;
    static OptionItem OptionKillJackal;
    static bool KillJackal;
    static OptionItem OptionBeamColorMode;
    static int BeamColorModeValue;
    static OptionItem OptionCanVent;
    static bool CanVent;
    static OptionItem OptionCanSabotage;
    static bool CanSabotage;
    static OptionItem OptionHasImpostorVision;
    static bool HasImpostorVision;
    static OptionItem OptionCanMakeSidekick;
    static OptionItem OptionSidekickCooldown;
    static float SidekickCooldown;

    enum BeamColorMode { Rainbow, Single }

    enum OptionName
    {
        JackalHadouHoChargeTime,
        JackalHadouHoSuperChargeTime,
        JackalHadouHoSelfDestruct,
        JackalHadouHoKillJackal,
        JackalHadouHoSidekickCooldown,
    }

    static void SetUpOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 0.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionCoolDown = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.Cooldown, OptionBaseCoolTime, 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionChargeTime = FloatOptionItem.Create(RoleInfo, 12, OptionName.JackalHadouHoChargeTime, new(0.5f, 10f, 0.5f), 3f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionSuperChargeTime = FloatOptionItem.Create(RoleInfo, 13, OptionName.JackalHadouHoSuperChargeTime, new(0.5f, 15f, 0.5f), 5f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionSelfDestructOnMiss = BooleanOptionItem.Create(RoleInfo, 15, OptionName.JackalHadouHoSelfDestruct, false, false);
        OptionKillJackal = BooleanOptionItem.Create(RoleInfo, 16, OptionName.JackalHadouHoKillJackal, false, false);
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 17, GeneralOption.CanVent, true, false);
        OptionCanSabotage = BooleanOptionItem.Create(RoleInfo, 18, GeneralOption.CanUseSabotage, false, false);
        OptionHasImpostorVision = BooleanOptionItem.Create(RoleInfo, 19, GeneralOption.ImpostorVision, true, false);
        OptionCanMakeSidekick = BooleanOptionItem.Create(RoleInfo, 20, GeneralOption.CanCreateSideKick, true, false);
        OptionSidekickCooldown = FloatOptionItem.Create(RoleInfo, 21, OptionName.JackalHadouHoSidekickCooldown, new(0f, 180f, 0.5f), 30f, false, OptionCanMakeSidekick)
            .SetValueFormat(OptionFormat.Seconds);
        OptionBeamColorMode = StringOptionItem.Create(RoleInfo, 22, "JackalHadouHoBeamColorMode", new string[] { "Rainbow", "Single" }, 0, false);
        RoleAddAddons.Create(RoleInfo, 23, NeutralKiller: true);
    }

    public float CalculateKillCooldown() => KillCooldown;
    public bool CanUseSabotageButton() => CanSabotage;
    public bool CanUseImpostorVentButton() => CanVent;
    public override bool CanClickUseVentButton => CanVent;
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (IsCharging || IsSuperCharging || ShowBeamMark) return false;
        return CanVent;
    }
    public override bool CanVentMoving(PlayerPhysics physics, int ventId) => CanVent;

    public override void Add()
    {
        PlayerSpeed = Main.AllPlayerSpeed[Player.PlayerId];
        BeamColorModeValue = OptionBeamColorMode.GetValue();
        PlayerColor = Player.Data.DefaultOutfit.ColorId;
        CanSideKick = NextNoSideKick ? false : OptionCanMakeSidekick.GetBool();
        NextNoSideKick = false;
        SidekickCooldown = OptionSidekickCooldown.GetFloat();
        skCandidateId = byte.MaxValue;
        skNearTimer = 0f;
        skCooldownTimer = 0f;
        skSpawnWaitTimer = -1f;
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(HasImpostorVision);
        if (PlayerControl.LocalPlayer != null && Is(PlayerControl.LocalPlayer))
        {
            AURoleOptions.PhantomCooldown = Cooldown;
        }
    }

    public override bool CanUseAbilityButton() => true;
    bool IUsePhantomButton.IsPhantomRole => true;
    bool IUsePhantomButton.IsresetAfterKill => true;

    bool ISelfVoter.CanUseVoted() => CanSideKick && Player.IsAlive();

    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (!Is(voter)) return true;
        if (!CanSideKick || !Player.IsAlive()) return true;

        if (CheckSelfVoteMode(Player, votedForId, out var status))
        {
            if (status is VoteStatus.Self)
            {
                skCandidateId = byte.MaxValue;
                Utils.SendMessage("<color=#00b4eb>【サイドキック任命モード】</color>\n候補に投票 → 次ターン3秒近づいてSK\nスキップ → キャンセル", Player.PlayerId);
                SetMode(Player, true);
                return false;
            }
            if (status is VoteStatus.Skip)
            {
                skCandidateId = byte.MaxValue;
                Utils.SendMessage(GetString("VoteSkillFin"), Player.PlayerId);
                SetMode(Player, false);
                return false;
            }
            if (status is VoteStatus.Vote)
            {
                var target = GetPlayerById(votedForId);
                if (target == null || !target.IsAlive() || votedForId == Player.PlayerId)
                {
                    Utils.SendMessage("<color=#00b4eb>その相手はSKにできません。</color>", Player.PlayerId);
                    SetMode(Player, false);
                    return false;
                }
                var targetRole = target.GetCustomRole();
                if (targetRole is CustomRoles.King or CustomRoles.Jackal or CustomRoles.JackalAlien
                    or CustomRoles.Jackaldoll or CustomRoles.JackalMafia or CustomRoles.JackalHadouHo
                    or CustomRoles.Merlin)
                {
                    Utils.SendMessage("<color=#00b4eb>その役職はSKにできません。</color>", Player.PlayerId);
                    SetMode(Player, false);
                    return false;
                }
                skCandidateId = votedForId;
                Utils.SendMessage($"<color=#00b4eb>【SK候補設定】</color>\n{UtilsName.GetPlayerColor(target, true)} を候補に設定しました。\n次ターン、3秒近づいてSK実行！", Player.PlayerId);
                SetMode(Player, false);
                return false;
            }
        }
        return true;
    }

    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        if (info.IsSuicide) return;
        AfterKillPhantomReset();
    }

    public void AfterKillPhantomReset()
    {
        _ = new LateTask(() =>
        {
            if (!Player.IsAlive()) return;
            if (PlayerControl.LocalPlayer != null && Is(PlayerControl.LocalPlayer))
            {
                AURoleOptions.PhantomCooldown = Cooldown;
            }
            Player.RpcResetAbilityCooldown(Sync: true);
        }, 0.2f, "JHHPhantomResetOnKill", true);
    }

    public void SetLoaded(bool loaded)
    {
        IsLoaded = loaded;
        SendRpc();
        if (loaded)
        {
            Utils.SendMessage(GetString("JackalHadouHoLoaded"), Player.PlayerId);
            UtilsNotifyRoles.NotifyRoles(OnlyMeName: true, SpecifySeer: Player);
        }
    }

    void IUsePhantomButton.OnClick(ref bool AdjustKillCooldown, ref bool? ResetCooldown)
    {
        AdjustKillCooldown = false;
        ResetCooldown = false;

        if (IsFiring) return;
        if (ShowBeamMark) return;
        if (!Player.IsAlive() || IsCharging || IsSuperCharging) return;

        IsFiring = true;

        if (IsLoaded)
        {
            IsSuperCharging = true;
            superChargeTimer = 0f;
            Utils.AllPlayerKillFlash();
            StartSuperChargeFlashLoop();
        }
        else
        {
            IsCharging = true;
            chargeTimer = 0f;
            Utils.AllPlayerKillFlash();
        }
        colorchange = 0f;

        Main.AllPlayerKillCooldown[Player.PlayerId] = 60f;
        Player.SetKillCooldown(60f);
        _ = new LateTask(() => { Player.SyncSettings(); }, 0.1f, "JackalHadouHoKillTimer", true);
        Player.SyncSettings();

        Main.AllPlayerKillCooldown[Player.PlayerId] = 60f;
        Player.SyncSettings();
        UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);
        UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);
        SendRpc();
    }


    void StartSuperChargeFlashLoop()
    {
        int count = (int)(SuperChargeTime / 0.1f);
        for (int i = 1; i <= count; i++)
        {
            float t = i * 0.1f;
            _ = new LateTask(() =>
            {
                if (IsDead || !Player.IsAlive()) return;
                if (!IsSuperCharging) return;
                Utils.AllPlayerKillFlash();
            }, t, null, null);
        }
    }

    void SetRoleTextHeight(bool beaming)
    {
        var roleTextTransform = Player.cosmetics.nameText.transform.Find("RoleText");
        if (roleTextTransform != null)
        {
            var roleText = roleTextTransform.GetComponent<TMPro.TextMeshPro>();
            if (roleText != null)
            {
                if (beaming)
                {
                    roleText.text = "<alpha=#00>　</alpha>";
                    roleTextTransform.SetLocalY(0.35f);
                }
                else
                {
                    roleText.enabled = true;
                    roleTextTransform.SetLocalY(0.35f);
                }
            }
        }
    }

    private void ResetAllState()
    {
        IsCharging = false;
        IsSuperCharging = false;
        ShowBeamMark = false;
        chargeTimer = 0f;
        superChargeTimer = 0f;
        HasHit = false;
        IsFiring = false;
        IsSuperBeam = false;
        Main.AllPlayerSpeed[Player.PlayerId] = PlayerSpeed;
        Player.MarkDirtySettings();
        Player.SyncSettings();
        Player.RpcSetColor((byte)PlayerColor);
        SetRoleTextHeight(false);
    }

    public override void OnFixedUpdate(PlayerControl player)
    {
        if (player.AmOwner && Is(player))
        {
            AURoleOptions.PhantomCooldown = Cooldown;
        }

        if (GameStates.IsInTask && Player.IsAlive() && CanSideKick)
        {
            if (skSpawnWaitTimer >= 0f && skSpawnWaitTimer < 3f)
                skSpawnWaitTimer += Time.fixedDeltaTime;

            if (skCooldownTimer > 0f)
                skCooldownTimer -= Time.fixedDeltaTime;
        }

        if (IsCharging) chargeTimer += Time.fixedDeltaTime;
        if (IsSuperCharging) superChargeTimer += Time.fixedDeltaTime;

        if (!AmongUsClient.Instance.AmHost) return;

        if (GameStates.IsInTask && Player.IsAlive() && CanSideKick)
        {
            if (skCandidateId != byte.MaxValue)
            {
                if (SkCanApproach && skCooldownTimer <= 0f)
                {
                    var skTarget = GetPlayerById(skCandidateId);
                    if (skTarget == null || !skTarget.IsAlive())
                    {
                        skCandidateId = byte.MaxValue;
                        skNearTimer = 0f;
                        SendRpc();
                    }
                    else
                    {
                        float dist = Vector2.Distance(Player.GetTruePosition(), skTarget.GetTruePosition());
                        if (dist <= 1.0f)
                        {
                            skNearTimer += Time.fixedDeltaTime;
                            UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);
                            if (skNearTimer >= 3f)
                            {
                                DoSideKick(skTarget);
                                skCandidateId = byte.MaxValue;
                                skNearTimer = 0f;
                            }
                        }
                        else
                        {
                            skNearTimer = 0f;
                        }
                    }
                }
            }
        }

        if (MeetingHud.Instance != null)
        {
            if (IsCharging || IsSuperCharging || ShowBeamMark || IsFiring)
            {
                ResetAllState();
                UtilsNotifyRoles.NotifyRoles();
                SendRpc();
            }
            return;
        }

        if (!Player.IsAlive() && (IsCharging || IsSuperCharging || ShowBeamMark))
        {
            ResetAllState();
            UtilsNotifyRoles.NotifyRoles();
            SendRpc();
            return;
        }

        if ((IsCharging || IsSuperCharging || ShowBeamMark) && !IsDead && player.IsAlive())
        {
            colorchange %= 18;
            if (colorchange is >= 0 and < 1) player.RpcSetColor(8);
            else if (colorchange is >= 1 and < 2) player.RpcSetColor(1);
            else if (colorchange is >= 2 and < 3) player.RpcSetColor(10);
            else if (colorchange is >= 3 and < 4) player.RpcSetColor(2);
            else if (colorchange is >= 4 and < 5) player.RpcSetColor(11);
            else if (colorchange is >= 5 and < 6) player.RpcSetColor(14);
            else if (colorchange is >= 6 and < 7) player.RpcSetColor(5);
            else if (colorchange is >= 7 and < 8) player.RpcSetColor(4);
            else if (colorchange is >= 8 and < 9) player.RpcSetColor(17);
            else if (colorchange is >= 9 and < 10) player.RpcSetColor(0);
            else if (colorchange is >= 10 and < 11) player.RpcSetColor(3);
            else if (colorchange is >= 11 and < 12) player.RpcSetColor(13);
            else if (colorchange is >= 12 and < 13) player.RpcSetColor(7);
            else if (colorchange is >= 13 and < 14) player.RpcSetColor(15);
            else if (colorchange is >= 14 and < 15) player.RpcSetColor(6);
            else if (colorchange is >= 15 and < 16) player.RpcSetColor(12);
            else if (colorchange is >= 16 and < 17) player.RpcSetColor(9);
            else if (colorchange is >= 17 and < 18) player.RpcSetColor(16);
            colorchange += Time.fixedDeltaTime * 1.5f;
            UtilsNotifyRoles.NotifyRoles(ForceLoop: true);
        }

        if (IsCharging && !IsDead)
        {
            Main.AllPlayerSpeed[Player.PlayerId] = Main.MinSpeed;
            Player.MarkDirtySettings();
        }

        if (IsSuperCharging && !IsDead)
        {
            Main.AllPlayerSpeed[Player.PlayerId] = Main.MinSpeed;
            Player.MarkDirtySettings();
        }

        if (ShowBeamMark && Player.IsAlive())
            ApplyBeamHit();

        if (IsCharging && chargeTimer >= ChargeTime)
            FireBeam();

        if (IsSuperCharging && superChargeTimer >= SuperChargeTime)
            FireSuperBeam();
    }

    void FireBeam()
    {
        if (IsDead || !Player.IsAlive()) return;

        BeamFacingLeft = Player.cosmetics.FlipX;
        SendBeamDirection(BeamFacingLeft);

        Utils.AllPlayerKillFlash();

        IsCharging = false;
        chargeTimer = 0f;
        HasHit = false;
        ShowBeamMark = true;
        IsSuperBeam = false;

        SetRoleTextHeight(true);
        UtilsNotifyRoles.NotifyRoles(ForceLoop: true);
        SendRpc();
        ApplyBeamHit();

        _ = new LateTask(() =>
        {
            if (IsDead || !Player.IsAlive())
            {
                ShowBeamMark = false;
                IsSuperBeam = false;
                SetRoleTextHeight(false);
                IsFiring = false;
                Main.AllPlayerSpeed[Player.PlayerId] = PlayerSpeed;
                Player.MarkDirtySettings();
                Player.RpcSetColor((byte)PlayerColor);
                UtilsNotifyRoles.NotifyRoles();
                SendRpc();
                return;
            }

            ShowBeamMark = false;
            IsSuperBeam = false;
            SetRoleTextHeight(false);
            UtilsNotifyRoles.NotifyRoles(ForceLoop: true);
            SendRpc();

            if (!HasHit && SelfDestructOnMiss)
            {
                Player.RpcSetColor((byte)PlayerColor);
                Main.AllPlayerSpeed[Player.PlayerId] = PlayerSpeed;
                Player.MarkDirtySettings();
                PlayerState.GetByPlayerId(Player.PlayerId).DeathReason = CustomDeathReason.Suicide;
                Player.RpcMurderPlayerV2(Player);
                IsFiring = false;
                return;
            }

            Player.RpcSetColor((byte)PlayerColor);
            Main.AllPlayerSpeed[Player.PlayerId] = PlayerSpeed;
            Player.MarkDirtySettings();

            _ = new LateTask(() =>
            {
                if (!Player.IsAlive()) { IsFiring = false; return; }
                Main.AllPlayerKillCooldown[Player.PlayerId] = KillCooldown;
                Player.SetKillCooldown(KillCooldown);
                if (PlayerControl.LocalPlayer != null && Is(PlayerControl.LocalPlayer))
                {
                    AURoleOptions.PhantomCooldown = Cooldown;
                }
                Player.RpcResetAbilityCooldown(Sync: true);
                UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);
                _ = new LateTask(() => { IsFiring = false; }, 0.3f, "JHHResetFiring", true);
            }, 0.2f, "JHHResetKillCool", true);
        }, 3f);
    }

    void FireSuperBeam()
    {
        if (IsDead || !Player.IsAlive()) return;

        BeamFacingLeft = Player.cosmetics.FlipX;
        SendBeamDirection(BeamFacingLeft);

        Utils.AllPlayerKillFlash();

        IsSuperCharging = false;
        superChargeTimer = 0f;
        HasHit = false;
        ShowBeamMark = true;
        IsLoaded = false;
        IsSuperBeam = true;

        foreach (var pc in PlayerCatch.AllPlayerControls)
        {
            if (pc.GetRoleClass() is Tama tama && tama.OwnerId == Player.PlayerId && pc.IsAlive())
            {
                PlayerState.GetByPlayerId(pc.PlayerId).DeathReason = CustomDeathReason.etc;
                pc.RpcExileV3();
                PlayerState.GetByPlayerId(pc.PlayerId).SetDead();
                UtilsGameLog.AddGameLog("JackalHadouHo", $"<color=#ff0000>【超波動砲】</color> 弾({UtilsName.GetPlayerColor(pc, true)})を消費しました");
                break;
            }
        }

        SetRoleTextHeight(true);
        UtilsNotifyRoles.NotifyRoles(ForceLoop: true);
        SendRpc();
        ApplySuperBeamHit();

        _ = new LateTask(() =>
        {
            if (IsDead || !Player.IsAlive())
            {
                ShowBeamMark = false;
                IsSuperBeam = false;
                SetRoleTextHeight(false);
                IsFiring = false;
                Main.AllPlayerSpeed[Player.PlayerId] = PlayerSpeed;
                Player.MarkDirtySettings();
                Player.RpcSetColor((byte)PlayerColor);
                UtilsNotifyRoles.NotifyRoles();
                SendRpc();
                return;
            }

            ShowBeamMark = false;
            IsSuperBeam = false;
            SetRoleTextHeight(false);
            UtilsNotifyRoles.NotifyRoles(ForceLoop: true);
            SendRpc();

            if (!HasHit && SelfDestructOnMiss)
            {
                Player.RpcSetColor((byte)PlayerColor);
                Main.AllPlayerSpeed[Player.PlayerId] = PlayerSpeed;
                Player.MarkDirtySettings();
                PlayerState.GetByPlayerId(Player.PlayerId).DeathReason = CustomDeathReason.Suicide;
                Player.RpcMurderPlayerV2(Player);
                IsFiring = false;
                return;
            }

            Player.RpcSetColor((byte)PlayerColor);
            Main.AllPlayerSpeed[Player.PlayerId] = PlayerSpeed;
            Player.MarkDirtySettings();

            _ = new LateTask(() =>
            {
                if (!Player.IsAlive()) { IsFiring = false; return; }
                Main.AllPlayerKillCooldown[Player.PlayerId] = KillCooldown;
                Player.SetKillCooldown(KillCooldown);
                if (PlayerControl.LocalPlayer != null && Is(PlayerControl.LocalPlayer))
                {
                    AURoleOptions.PhantomCooldown = Cooldown;
                }
                Player.RpcResetAbilityCooldown(Sync: true);
                UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);
                _ = new LateTask(() => { IsFiring = false; }, 0.3f, "JHHSuperResetFiring", true);
            }, 0.2f, "JHHSuperResetKillCool", true);
        }, 3f);
    }

    void ApplyBeamHit()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!Player.IsAlive()) return;

        bool facingLeft = BeamFacingLeft;
        var myPos = Player.GetTruePosition();
        Vector2 dir = facingLeft ? Vector2.left : Vector2.right;

        foreach (var target in PlayerCatch.AllAlivePlayerControls)
        {
            if (target.PlayerId == Player.PlayerId) continue;
            if ((!KillJackal) && target.GetCustomRole() is CustomRoles.Jackal or CustomRoles.JackalMafia
                or CustomRoles.JackalAlien or CustomRoles.Jackaldoll or CustomRoles.JackalHadouHo
                or CustomRoles.Tama && !SuddenDeathMode.NowSuddenDeathMode) continue;

            var targetPos = target.GetTruePosition();
            var toTarget = targetPos - myPos;
            float dot = Vector2.Dot(toTarget, dir);
            if (dot <= 0) continue;

            var proj = dir * dot;
            var perp = toTarget - proj;
            if (perp.magnitude > 1.3f) continue;

            CustomRoleManager.OnCheckMurder(Player, target, target, target, true, deathReason: CustomDeathReason.Hit);
            HasHit = true;
            UtilsGameLog.AddGameLog("JackalHadouHo", $"<color=#00b4eb>【波動砲】</color> {UtilsName.GetPlayerColor(Player, true)} ═> {UtilsName.GetPlayerColor(target, true)}");
        }
    }

    void ApplySuperBeamHit()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!Player.IsAlive()) return;

        bool facingLeft = BeamFacingLeft;
        var myPos = Player.GetTruePosition();
        Vector2 dir = facingLeft ? Vector2.left : Vector2.right;

        foreach (var target in PlayerCatch.AllAlivePlayerControls)
        {
            if (target.PlayerId == Player.PlayerId) continue;
            if ((!KillJackal) && target.GetCustomRole() is CustomRoles.Jackal or CustomRoles.JackalMafia
                or CustomRoles.JackalAlien or CustomRoles.Jackaldoll or CustomRoles.JackalHadouHo
                or CustomRoles.Tama && !SuddenDeathMode.NowSuddenDeathMode) continue;

            var targetPos = target.GetTruePosition();
            var toTarget = targetPos - myPos;
            float dot = Vector2.Dot(toTarget, dir);
            if (dot <= 0) continue;

            var proj = dir * dot;
            var perp = toTarget - proj;
            if (perp.magnitude > 4.0f) continue;

            CustomRoleManager.OnCheckMurder(Player, target, target, target, true, deathReason: CustomDeathReason.Hit);
            HasHit = true;
            UtilsGameLog.AddGameLog("JackalHadouHo", $"<color=#ff0000>【超波動砲】</color> {UtilsName.GetPlayerColor(Player, true)} ═> {UtilsName.GetPlayerColor(target, true)}");
        }
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        ResetAllState();
        UtilsNotifyRoles.NotifyRoles(ForceLoop: true);
        SendRpc();
    }

    public override void OnStartMeeting()
    {
        ResetAllState();
    }

    public override void AfterMeetingTasks()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!Player.IsAlive()) return;

        if (PlayerControl.LocalPlayer != null && Is(PlayerControl.LocalPlayer))
        {
            AURoleOptions.PhantomCooldown = Cooldown;
            Player.RpcResetAbilityCooldown();
        }

        skSpawnWaitTimer = 0f;
        skCooldownTimer = SidekickCooldown;
        skNearTimer = 0f;
    }

    private void DoSideKick(PlayerControl target)
    {
        CanSideKick = false;

        var targetRole = target.GetCustomRole();
        if (targetRole is CustomRoles.King or CustomRoles.Jackal or CustomRoles.JackalAlien
            or CustomRoles.Jackaldoll or CustomRoles.JackalMafia or CustomRoles.JackalHadouHo
            or CustomRoles.Merlin)
        {
            Utils.SendMessage("<color=#00b4eb>この役職はSKにできません。</color>", Player.PlayerId);
            SendRpc();
            return;
        }

        Player.RpcProtectedMurderPlayer(target);
        target.RpcProtectedMurderPlayer(Player);
        target.RpcProtectedMurderPlayer(target);

        target.RpcSetCustomRole(CustomRoles.Tama, log: null);
        if (target.GetRoleClass() is Tama tama)
            tama.SetOwner(Player.PlayerId);

        if (!Utils.RoleSendList.Contains(target.PlayerId))
            Utils.RoleSendList.Add(target.PlayerId);

        UtilsGameLog.AddGameLog("JackalHadouHoSideKick",
            string.Format(GetString("log.Sidekick"),
            UtilsName.GetPlayerColor(target, true) + $"({UtilsRoleText.GetTrueRoleName(target.PlayerId)})",
            UtilsName.GetPlayerColor(Player, true)));

        UtilsOption.MarkEveryoneDirtySettings();

        if (PlayerControl.LocalPlayer != null && Is(PlayerControl.LocalPlayer))
        {
            AURoleOptions.PhantomCooldown = Cooldown;
            Player.RpcResetAbilityCooldown();
        }

        SendRpc();
        UtilsNotifyRoles.NotifyRoles();
    }

    public void SendRpc()
    {
        using var sender = CreateSender();
        sender.Writer.Write(IsCharging);
        sender.Writer.Write(IsSuperCharging);
        sender.Writer.Write(ShowBeamMark);
        sender.Writer.Write(CanSideKick);
        sender.Writer.Write(IsLoaded);
        sender.Writer.Write(IsSuperBeam);
    }

    void SendBeamDirection(bool left)
    {
        using var sender = CreateSender();
        sender.Writer.Write((byte)1);
        sender.Writer.Write(left);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        if (reader.Length - reader.Position == 2)
        {
            reader.ReadByte();
            BeamFacingLeft = reader.ReadBoolean();
            return;
        }
        IsCharging = reader.ReadBoolean();
        IsSuperCharging = reader.ReadBoolean();
        ShowBeamMark = reader.ReadBoolean();
        CanSideKick = reader.ReadBoolean();
        IsLoaded = reader.ReadBoolean();
        IsSuperBeam = reader.ReadBoolean();
    }

    public override bool GetTemporaryName(ref string name, ref bool NoMarker, bool isForMeeting, PlayerControl seer, PlayerControl seen = null)
    {
        seen ??= seer;
        if (!Player.IsAlive() || isForMeeting)
            return false;

        if (seen == seer && Is(seer) && !seer.IsModClient() && (IsCharging || IsSuperCharging || ShowBeamMark))
        {
            if (!Player.IsAlive() || isForMeeting) return false;

            if ((IsCharging || IsSuperCharging) && seen.PlayerId == Player.PlayerId)
            {
                bool facingLeft = seer.PlayerId == Player.PlayerId ? Player.cosmetics.FlipX : BeamFacingLeft;
                string bigStar = "<size=800%><color=#ffffff>★</color></size>";
                string blank = "　　　";
                string text = facingLeft ? bigStar + blank : blank + bigStar;
                name = "<line-height=1200%>\n" + text + "</line-height>";
                NoMarker = true;
                return true;
            }

            if (ShowBeamMark && seen.PlayerId == Player.PlayerId)
            {
                SetRoleTextHeight(true);
                bool facingLeft = BeamFacingLeft;
                string star = "<voffset=0.35em><size=800%><color=#ffffff>★</color></size></voffset>";
                string beamBlock = IsSuperBeam ? BuildSuperBeamBlock() : BuildBeamBlock();
                int beamRepeat = IsSuperBeam ? 4 : 2;
                string blank800 = IsSuperBeam ? "<size=1800%>　</size>" : "<size=1200%>　</size>";
                string starWithBlank = facingLeft ? star + blank800 : blank800 + star;
                string longBeam;

                if (facingLeft)
                {
                    longBeam = "";
                    for (int i = 0; i < beamRepeat; i++) longBeam += beamBlock;
                    longBeam += starWithBlank;
                }
                else
                {
                    longBeam = starWithBlank;
                    for (int i = 0; i < beamRepeat; i++) longBeam += beamBlock;
                }

                string hugeBlank = "<alpha=#00>" + new string('　', IsSuperBeam ? 52 : 10) + "</alpha>";
                string lineStart = IsSuperBeam ? "<line-height=11000%>\n" : "<line-height=4300%>\n";
                string sizeStart = IsSuperBeam ? "<size=15000%>" : "<size=5000%>";
                string sizeEnd = "</size></line-height>";

                if (facingLeft)
                    name = lineStart + $"{sizeStart}{longBeam}{sizeEnd}" + $"{sizeStart}{hugeBlank}{sizeEnd}";
                else
                    name = lineStart + $"{sizeStart}{hugeBlank}{sizeEnd}" + $"{sizeStart}{longBeam}{sizeEnd}";

                NoMarker = true;
                return true;
            }

            return false;
        }

        if (!Player.IsAlive() || isForMeeting) return false;

        if ((IsCharging || IsSuperCharging) && seen.PlayerId == Player.PlayerId)
        {
            bool facingLeft = seer.PlayerId == Player.PlayerId ? Player.cosmetics.FlipX : BeamFacingLeft;
            string bigStar = "<size=800%><color=#ffffff>★</color></size>";
            string blank = "　　　";
            string text = facingLeft ? bigStar + blank : blank + bigStar;
            name = "<line-height=1200%>\n" + text + "</line-height>";
            NoMarker = true;
            return true;
        }

        if (ShowBeamMark && seen.PlayerId == Player.PlayerId)
        {
            SetRoleTextHeight(true);
            bool facingLeft = BeamFacingLeft;
            string star = "<voffset=0.35em><size=800%><color=#ffffff>★</color></size></voffset>";
            string beamBlock = IsSuperBeam ? BuildSuperBeamBlock() : BuildBeamBlock();
            int beamRepeat = IsSuperBeam ? 4 : 2;
            string blank800 = IsSuperBeam ? "<size=1800%>　</size>" : "<size=1200%>　</size>";
            string starWithBlank = facingLeft ? star + blank800 : blank800 + star;
            string longBeam;

            if (facingLeft)
            {
                longBeam = "";
                for (int i = 0; i < beamRepeat; i++) longBeam += beamBlock;
                longBeam += starWithBlank;
            }
            else
            {
                longBeam = starWithBlank;
                for (int i = 0; i < beamRepeat; i++) longBeam += beamBlock;
            }

            string hugeBlank = "<alpha=#00>" + new string('　', IsSuperBeam ? 52 : 10) + "</alpha>";
            string lineStart = IsSuperBeam ? "<line-height=15000%>\n" : "<line-height=5300%>\n";
            string sizeStart = IsSuperBeam ? "<size=15000%>" : "<size=5000%>";
            string sizeEnd = "</size></line-height>";

            if (facingLeft)
                name = lineStart + $"{sizeStart}{longBeam}{sizeEnd}" + $"{sizeStart}{hugeBlank}{sizeEnd}";
            else
                name = lineStart + $"{sizeStart}{hugeBlank}{sizeEnd}" + $"{sizeStart}{longBeam}{sizeEnd}";

            NoMarker = true;
            return true;
        }

        return false;
    }

    string BuildBeamBlock()
    {
        switch ((BeamColorMode)BeamColorModeValue)
        {
            case BeamColorMode.Single:
                return
                    "<color=#00b4eb>━</color>" +
                    "<color=#00b4eb>━</color>" +
                    "<color=#00b4eb>━</color>" +
                    "<color=#00b4eb>━</color>" +
                    "<color=#00b4eb>━</color>" +
                    "<color=#00b4eb>━</color>" +
                    "<color=#00b4eb>━</color>";
            default:
            case BeamColorMode.Rainbow:
                return
                    "<color=#ff0000>━</color>" +
                    "<color=#ff7f00>━</color>" +
                    "<color=#ffff00>━</color>" +
                    "<color=#00ff00>━</color>" +
                    "<color=#0000ff>━</color>" +
                    "<color=#4b0082>━</color>" +
                    "<color=#8b00ff>━</color>";
        }
    }

    string BuildSuperBeamBlock()
    {
        return
            "<color=#ff0000>━━</color>" +
            "<color=#ff0000>━━</color>" +
            "<color=#ff0000>━━</color>" +
            "<color=#ff0000>━━</color>" +
            "<color=#ff0000>━━</color>" +
            "<color=#ff0000>━━</color>" +
            "<color=#ff0000>━━</color>";
    }

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (seen.PlayerId != seer.PlayerId || isForMeeting || !Player.IsAlive()) return "";
        string size = isForHud ? "" : "<size=60%>";

        if (CanSideKick)
        {
            string cdText = skCooldownTimer > 0f ? $"SK CD: {Mathf.CeilToInt(skCooldownTimer)}s" : "SK準備完了";

            if (skCandidateId != byte.MaxValue)
            {
                var skTarget = PlayerCatch.GetPlayerById(skCandidateId);
                string name = skTarget != null ? skTarget.Data.PlayerName : "???";
                if (skCooldownTimer > 0f)
                    return $"{size}<color=#00b4eb>{cdText} | 候補: {name}</color>";
                if (!SkCanApproach)
                    return $"{size}<color=#00b4eb>待機中... | 候補: {name}</color>";
                float progress = System.Math.Min(skNearTimer, 3f);
                return $"{size}<color=#00b4eb>{name}に近づき中 {progress:F1}/3.0s</color>";
            }

            if (isForMeeting) return $"{size}<color=#00b4eb>自投票→SK候補を投票で指定</color>";
            return $"{size}<color=#00b4eb>{cdText} | 【会議で自投票→候補指定】</color>";
        }

        if (IsLoaded) return $"{size}<color=#00b4eb>【装填済】ファントムボタン → 超波動砲チャージ！</color>";
        if (!IsCharging && !IsSuperCharging) return $"{size}<color=#ff0000>ファントムボタン → チャージ発射</color>";
        if (IsSuperCharging)
        {
            var remaining = Mathf.Max(0f, SuperChargeTime - superChargeTimer);
            return $"{(isForHud ? "" : "<size=60%>")}<color=#ff0000>超チャージ中... {remaining:F1}s</color>";
        }
        var rem = Mathf.Max(0f, ChargeTime - chargeTimer);
        return $"{(isForHud ? "" : "<size=60%>")}<color=#ff0000>チャージ中... {rem:F1}s</color>";
    }

    public string GetLowerTextOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (seen != seer) return "";
        if (isForMeeting) return "";
        if (!Player.IsAlive()) return "";

        if (IsSuperCharging && seer.PlayerId != Player.PlayerId)
        {
            var remaining = Mathf.Max(0f, SuperChargeTime - superChargeTimer);
            return $"<color=#ff0000>超チャージ中... {(int)remaining}s</color>";
        }
        if (IsCharging && seer.PlayerId != Player.PlayerId)
        {
            var remaining = Mathf.Max(0f, ChargeTime - chargeTimer);
            return $"<color=#ff0000>チャージ中... {(int)remaining}s</color>";
        }
        if (ShowBeamMark && seer.PlayerId != Player.PlayerId)
            return "<color=#ff0000>ビーム中</color>";

        return "";
    }

    public override string GetAbilityButtonText() => IsLoaded ? "超発射" : "発射";
    public override bool OverrideAbilityButton(out string text)
    {
        text = IsLoaded ? "JackalHadouHo_SuperFire" : "JackalHadouHo_Fire";
        return true;
    }
}