using AmongUs.GameOptions;
using Hazel;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using UnityEngine;

namespace TownOfHost.Roles.Impostor;

public sealed class HadouHo : RoleBase, IImpostor, IUsePhantomButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(HadouHo),
            player => new HadouHo(player),
            CustomRoles.HadouHo,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Impostor,
            26200,
            SetUpOptionItem,
            "hh",
            OptionSort: (3, 12),
            from: From.SuperNewRoles
        );

    public HadouHo(PlayerControl player)
        : base(RoleInfo, player)
    {
        KillCooldown = OptionKillCoolDown.GetFloat();
        Cooldown = OptionCoolDown.GetFloat();
        ChargeTime = OptionChargeTime.GetFloat();
        SelfDestructOnMiss = OptionSelfDestructOnMiss.GetBool();
        KillImpostor = OptionKillImpostor.GetBool();
        BeamColorModeValue = OptionBeamColorMode.GetValue();

        IsCharging = false;
        chargeTimer = 0f;
        PlayerSpeed = 0f;
        colorchange = 0f;

        ShowBeamMark = false;
        HasHit = false;
        IsDead = false;
        IsFiring = false;

        CustomRoleManager.LowerOthers.Add(GetLowerTextOthers);
    }

    public bool IsCharging;
    float chargeTimer;
    float PlayerSpeed;
    public bool ShowBeamMark;
    bool HasHit;
    bool BeamFacingLeft;
    bool IsDead;
    float colorchange;
    int PlayerColor;
    bool IsFiring = false;
    bool spawnCooldownStarted = false;

    static OptionItem OptionCoolDown;
    static float Cooldown;
    public static float CooldownValue => Cooldown;

    static OptionItem OptionKillCoolDown;
    static float KillCooldown;

    static OptionItem OptionChargeTime;
    static float ChargeTime;

    static OptionItem OptionSelfDestructOnMiss;
    static bool SelfDestructOnMiss;

    static OptionItem OptionKillImpostor;
    static bool KillImpostor;

    static OptionItem OptionBeamColorMode;
    static int BeamColorModeValue;

    enum BeamColorMode
    {
        Rainbow,
        Single,
    }

    enum OptionName
    {
        HadouHoChargeTime,
        HadouHoSelfDestruct,
        HadouHoKillImpostor,
    }

    static void SetUpOptionItem()
    {
        OptionKillCoolDown = FloatOptionItem.Create(RoleInfo, 14, GeneralOption.KillCooldown, OptionBaseCoolTime, 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionCoolDown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.Cooldown, OptionBaseCoolTime, 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionChargeTime = FloatOptionItem.Create(RoleInfo, 11, OptionName.HadouHoChargeTime, new(0.5f, 10f, 0.5f), 3f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionSelfDestructOnMiss = BooleanOptionItem.Create(RoleInfo, 12, OptionName.HadouHoSelfDestruct, false, false);
        OptionKillImpostor = BooleanOptionItem.Create(RoleInfo, 13, OptionName.HadouHoKillImpostor, false, false);
        OptionBeamColorMode = StringOptionItem.Create(
            RoleInfo,
            20,
            "HadouHoBeamColorMode",
            new string[] { "Rainbow", "Single" },
            0,
            false
        );
    }

    public override void Add()
    {
        PlayerSpeed = Main.AllPlayerSpeed[Player.PlayerId];
        BeamColorModeValue = OptionBeamColorMode.GetValue();
        PlayerColor = Player.Data.DefaultOutfit.ColorId;
        spawnCooldownStarted = false;
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.PhantomCooldown = Cooldown;
    }

    public float CalculateKillCooldown() => KillCooldown;

    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (IsCharging || ShowBeamMark) return false;
        return true;
    }
    bool IUsePhantomButton.IsPhantomRole => true;

    void IUsePhantomButton.OnClick(ref bool AdjustKillCooldown, ref bool? ResetCooldown)
    {
        AdjustKillCooldown = false;
        ResetCooldown = false;

        if (IsFiring) return;
        if (ShowBeamMark) return;
        if (!Player.IsAlive() || IsCharging) return;

        IsFiring = true;

        IsCharging = true;
        chargeTimer = 0f;
        colorchange = 0f;

        Utils.AllPlayerKillFlash();

        Main.AllPlayerKillCooldown[Player.PlayerId] = 60f;
        Player.SetKillCooldown(60f);
        _ = new LateTask(() =>
        {
            Player.SyncSettings();
        }, 0.1f, "HadouHoKillTimer", true);
        Player.SyncSettings();

        Main.AllPlayerKillCooldown[Player.PlayerId] = 60f;
        Player.SyncSettings();
        UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);

        StartChargeFlashLoop();
        UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);
        SendRpc();
    }

    void StartChargeFlashLoop()
    {
        int count = (int)(ChargeTime / 0.1f);
        for (int i = 1; i <= count; i++)
        {
            float t = i * 0.1f;
            _ = new LateTask(() =>
            {
                if (IsDead || !Player.IsAlive()) return;
                if (!IsCharging) return;
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
                    roleText.text = "<alpha=#00>縲</alpha>";
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

    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        if (!spawnCooldownStarted && Player.IsAlive() && !GameStates.Intro && GameStates.IsInTask && !GameStates.IsMeeting)
        {
            spawnCooldownStarted = true;
            Player.RpcResetAbilityCooldown(Sync: true);
        }

        if (MeetingHud.Instance != null)
        {
            IsCharging = false;
            ShowBeamMark = false;
            IsFiring = false;

            Main.AllPlayerSpeed[Player.PlayerId] = PlayerSpeed;
            Player.MarkDirtySettings();
            SetRoleTextHeight(false);
            UtilsNotifyRoles.NotifyRoles();
            return;
        }

        if (!Player.IsAlive() && (IsCharging || ShowBeamMark))
        {
            IsCharging = false;
            ShowBeamMark = false;
            IsFiring = false;
            Main.AllPlayerSpeed[Player.PlayerId] = PlayerSpeed;
            Player.MarkDirtySettings();
            Player.RpcSetColor((byte)PlayerColor);
            SetRoleTextHeight(false);
            UtilsNotifyRoles.NotifyRoles();
            SendRpc();
            return;
        }

        if ((IsCharging || ShowBeamMark) && !IsDead && player.IsAlive())
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
            Main.AllPlayerSpeed[Player.PlayerId] = PlayerSpeed;
            Player.MarkDirtySettings();
            if (Player.IsAlive())
            {
                Main.AllPlayerSpeed[Player.PlayerId] = Main.MinSpeed;
                Player.MarkDirtySettings();
            }
        }

        if (ShowBeamMark && Player.IsAlive())
        {
            ApplyBeamHit();
        }

        if (IsCharging)
        {
            chargeTimer += Time.fixedDeltaTime;
            if (chargeTimer >= ChargeTime)
            {
                FireBeam();
            }
        }
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

        SetRoleTextHeight(true);

        UtilsNotifyRoles.NotifyRoles(ForceLoop: true);
        SendRpc();
        ApplyBeamHit();

        _ = new LateTask(() =>
        {
            if (IsDead || !Player.IsAlive())
            {
                ShowBeamMark = false;
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
                if (!Player.IsAlive())
                {
                    IsFiring = false;
                    return;
                }
                Main.AllPlayerKillCooldown[Player.PlayerId] = KillCooldown;
                Player.SetKillCooldown(KillCooldown);
                Player.RpcResetAbilityCooldown(Sync: true);
                UtilsNotifyRoles.NotifyRoles(OnlyMeName: true);

                _ = new LateTask(() =>
                {
                    IsFiring = false;
                }, 0.3f, "HadouHoResetFiring", true);
            }, 0.2f, "HadouHoResetKillCool", true);
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
            if (!KillImpostor && target.GetCustomRole().IsImpostor() && !SuddenDeathMode.NowSuddenDeathMode) continue;

            var targetPos = target.GetTruePosition();
            var toTarget = targetPos - myPos;
            float dot = Vector2.Dot(toTarget, dir);
            if (dot <= 0) continue;

            var proj = dir * dot;
            var perp = toTarget - proj;
            if (perp.magnitude > 1.3f) continue;

            CustomRoleManager.OnCheckMurder(Player, target, target, target, true, deathReason: CustomDeathReason.Hit);
            HasHit = true;
        }
    }

    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        IsCharging = false;
        ShowBeamMark = false;
        chargeTimer = 0f;
        HasHit = false;
        IsFiring = false;

        Main.AllPlayerSpeed[Player.PlayerId] = PlayerSpeed;
        Player.MarkDirtySettings();
        Player.SyncSettings();

        Player.RpcSetColor((byte)PlayerColor);
        SetRoleTextHeight(false);
        UtilsNotifyRoles.NotifyRoles(ForceLoop: true);
        SendRpc();
    }

    public override void OnStartMeeting()
    {
        IsCharging = false;
        ShowBeamMark = false;
        chargeTimer = 0f;
        HasHit = false;
        IsFiring = false;

        Main.AllPlayerSpeed[Player.PlayerId] = PlayerSpeed;
        Player.MarkDirtySettings();
        Player.SyncSettings();

        Player.RpcSetColor((byte)PlayerColor);
        SetRoleTextHeight(false);
    }

    public override void AfterMeetingTasks()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!Player.IsAlive()) return;

        AURoleOptions.PhantomCooldown = Cooldown;
        Player.RpcResetAbilityCooldown();
    }

    void SendRpc()
    {
        using var sender = CreateSender();
        sender.Writer.Write(IsCharging);
        sender.Writer.Write(ShowBeamMark);
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
        ShowBeamMark = reader.ReadBoolean();
    }

    public override bool GetTemporaryName(ref string name, ref bool NoMarker, bool isForMeeting, PlayerControl seer, PlayerControl seen = null)
    {
        seen ??= seer;

        if (!Player.IsAlive() || isForMeeting)
            return false;

        if (seen == seer && Is(seer) && !seer.IsModClient() && (IsCharging || ShowBeamMark))
        {
            if (ShowBeamMark && seen.PlayerId == Player.PlayerId)
            {
                SetRoleTextHeight(true);

                bool facingLeft = BeamFacingLeft;
                string star = "<voffset=0.35em><size=800%><color=#ffffff>★</color></size></voffset>";
                string beamBlock = BuildBeamBlock();
                string blank800 = "<size=1200%>　</size>";
                string starWithBlank = facingLeft ? star + blank800 : blank800 + star;
                string longBeam;

                if (facingLeft)
                {
                    longBeam = "";
                    for (int i = 0; i < 2; i++) longBeam += beamBlock;
                    longBeam += starWithBlank;
                }
                else
                {
                    longBeam = starWithBlank;
                    for (int i = 0; i < 2; i++) longBeam += beamBlock;
                }

                string hugeBlank = "<alpha=#00>" + new string('　', 10) + "</alpha>";
                string lineStart = "<line-height=4300%>\n";
                string sizeStart = "<size=5000%>";
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

        if (IsCharging && seen.PlayerId == Player.PlayerId)
        {
            bool facingLeft = BeamFacingLeft;
            if (seer.PlayerId == Player.PlayerId)
                facingLeft = Player.cosmetics.FlipX;

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
            string beamBlock = BuildBeamBlock();
            string blank800 = "<size=1200%>　</size>";
            string starWithBlank = facingLeft ? star + blank800 : blank800 + star;
            string longBeam;

            if (facingLeft)
            {
                longBeam = "";
                for (int i = 0; i < 2; i++) longBeam += beamBlock;
                longBeam += starWithBlank;
            }
            else
            {
                longBeam = starWithBlank;
                for (int i = 0; i < 2; i++) longBeam += beamBlock;
            }

            string hugeBlank = "<alpha=#00>" + new string('　', 10) + "</alpha>";
            string lineStart = "<line-height=5300%>\n";
            string sizeStart = "<size=5000%>";
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
                    "<color=#00CFFF>━</color>" +
                    "<color=#00CFFF>━</color>" +
                    "<color=#00CFFF>━</color>" +
                    "<color=#00CFFF>━</color>" +
                    "<color=#00CFFF>━</color>" +
                    "<color=#00CFFF>━</color>" +
                    "<color=#00CFFF>━</color>";
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

    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (seen.PlayerId != seer.PlayerId || isForMeeting || !Player.IsAlive()) return "";
        if (!IsCharging) return $"{(isForHud ? "" : "<size=60%>")}<color=#ff0000>ファントムボタン → チャージ発射</color>";
        var remaining = ChargeTime - chargeTimer;
        return $"{(isForHud ? "" : "<size=60%>")}<color=#ff0000><color=#ff0000>チャージ中... {remaining:F1}s</color>";
    }

    public string GetLowerTextOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (seen != seer) return "";
        if (isForMeeting) return "";
        if (!Player.IsAlive()) return "";

        if (IsCharging && seer.PlayerId != Player.PlayerId)
        {
            var remaining = ChargeTime - chargeTimer;
            return $"<color=#ff0000>チャージ中... {(int)remaining}s</color>";
        }

        if (ShowBeamMark && seer.PlayerId != Player.PlayerId)
        {
            return "<color=#ff0000>ビーム中/color>";
        }

        return "";
    }

    public override string GetAbilityButtonText() => "発射";
}
