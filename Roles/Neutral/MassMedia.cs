using System.Collections.Generic;
using System.Linq;

using AmongUs.GameOptions;
using UnityEngine;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using Hazel;

namespace TownOfHost.Roles.Neutral;

public sealed class MassMedia : RoleBase, IKiller, IKillFlashSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(MassMedia),
            player => new MassMedia(player),
            CustomRoles.MassMedia,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            14500,
            SetupOptionItem,
            "MM",
            "#512513",
            (5, 1),
            true,
            introSound: () => GetIntroSound(RoleTypes.Shapeshifter),
            from: From.None
        );
    public MassMedia(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        CustomRoleManager.OnMurderPlayerOthers.Add(TageKillCh);
    }
    public bool CanKill { get; private set; } = false;
    static OptionItem OptionKillCoolDown;
    static OptionItem OptionBlackVision;
    static OptionItem OptionMeetingTargetReset;
    static OptionItem OptionCanSeeKillflash;
    static OptionItem OptionCriminalprofile;
    List<byte> Suspects;
    bool SuspectsSearch;
    static bool MeetingTargetReset;
    static float KillCooldown;
    static bool Canseekillflash;
    public byte Targetid;
    byte Guees;
    bool IsBlackOut;
    bool GuessMode;
    bool Win;
    Vector3 TargetPosition;
    public static HashSet<MassMedia> MassMedias = new();
    enum Option
    {
        MassMediaShikai,
        MassMediaMeetingTargetReset,
        MassMediaCanSeeKillflash,
        MassMediaCriminalprofile
    }
    public override void Add()
    {
        KillCooldown = OptionKillCoolDown.GetFloat();
        MeetingTargetReset = OptionMeetingTargetReset.GetBool();
        Canseekillflash = OptionCanSeeKillflash.GetBool();
        Targetid = byte.MaxValue;
        IsBlackOut = false;
        TargetPosition = new Vector3(999f, 999f);
        GuessMode = false;
        Win = false;
        Guees = byte.MaxValue;
        Suspects = new();
        SuspectsSearch = false;

        MassMedias.Add(this);
    }
    public override void OnDestroy() => MassMedias.Clear();
    private static void SetupOptionItem()
    {
        SoloWinOption.Create(RoleInfo, 9, defo: 1);
        OptionKillCoolDown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.Cooldown, new(0f, 180f, 0.5f), 20f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionBlackVision = FloatOptionItem.Create(RoleInfo, 11, Option.MassMediaShikai, new(0f, 0.20f, 0.02f), 0.04f, false)
                .SetValueFormat(OptionFormat.Multiplier);
        OptionMeetingTargetReset = BooleanOptionItem.Create(RoleInfo, 12, Option.MassMediaMeetingTargetReset, false, false);
        OptionCanSeeKillflash = BooleanOptionItem.Create(RoleInfo, 13, Option.MassMediaCanSeeKillflash, false, false);
        OptionCriminalprofile = BooleanOptionItem.Create(RoleInfo, 14, Option.MassMediaCriminalprofile, false, false);
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!player.IsAlive()) return;

        var target = PlayerCatch.GetPlayerById(Targetid);
        if (target == null) return;

        //範囲
        Vector2 GSpos = player.transform.position;
        var Mieruhani = 6.5f * Main.DefaultCrewmateVision;
        if (player.Is(CustomRoles.Lighting)) Mieruhani = 6.5f * Main.DefaultImpostorVision;

        //position
        float HitoDistance = Vector2.Distance(GSpos, target.transform.position);
        if (!target.IsAlive())
        {
            HitoDistance = Vector2.Distance(GSpos, TargetPosition);
            Mieruhani *= 0.4f;
        }

        if (HitoDistance <= Mieruhani)//更新があるなら～
        {
            if (!IsBlackOut)
            {
                IsBlackOut = true;
                Player.MarkDirtySettings();
            }
        }
        else
        {
            if (IsBlackOut)
            {
                IsBlackOut = false;
                Player.MarkDirtySettings();
            }
        }

        if (OptionCriminalprofile.GetBool() && target.IsAlive())
        {
            foreach (var otherpc in PlayerCatch.AllAlivePlayerControls.Where(pc => pc.PlayerId != Targetid && pc.PlayerId != Player.PlayerId))
            {
                if (Suspects.Contains(otherpc.PlayerId)) continue;
                float distance = Vector2.Distance(target.transform.position, otherpc.GetTruePosition());
                if (distance <= 4.5f && SuspectsSearch)
                {
                    Suspects.Add(otherpc.PlayerId);
                }
            }
        }
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;
        if (Is(killer))
        {
            if (Targetid != byte.MaxValue)
            {
                info.DoKill = false;
                return;
            }
            Targetid = target.PlayerId;
            SendRPC();
            info.DoKill = false;
            Main.AllPlayerKillCooldown[killer.PlayerId] = 999;
            killer.SyncSettings();
            killer.RpcProtectedMurderPlayer();
            TargetArrow.Add(Player.PlayerId, target.PlayerId);
        }
    }
    public void TageKillCh(MurderInfo info)//こぉれは特ダネだぁ!!
    {
        var (killer, target) = info.AttemptTuple;

        if (target.PlayerId == Targetid && Player.Is(CustomRoles.MassMedia))
        {
            GetArrow.Add(Player.PlayerId, target.transform.position);
            TargetPosition = target.transform.position;
            Guees = killer.PlayerId;
            if (AmongUsClient.Instance.AmHost) SendRPC();
        }
    }
    public override void OnReportDeadBody(PlayerControl repo, NetworkedPlayerInfo tg)
    {
        if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;
        if (Is(repo) && Player.Is(CustomRoles.MassMedia))//自分が通報したならチャンスだよ!!
        {
            //死体通報なら～
            if (tg != null && tg.PlayerId == Targetid)
            {
                GuessMode = true;
            }
        }
        //リセット
        if (MeetingTargetReset || !PlayerCatch.GetPlayerById(Targetid).IsAlive())
        {
            TargetArrow.Remove(Player.PlayerId, Targetid);
            GetArrow.Remove(Player.PlayerId, TargetPosition);
            TargetPosition = new Vector3(999f, 999f);
            Targetid = byte.MaxValue;
        }

        SendRPC();
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (isForMeeting && Suspects.Contains(seen.PlayerId))
        {
            return "<#512513>〇</color>";
        }

        if (Targetid == byte.MaxValue) return "";

        if (seen == seer && Is(seen))
        {
            if (PlayerCatch.GetPlayerById(Targetid).IsAlive())
                return "<color=#512513>" + TargetArrow.GetArrows(Player, Targetid) + "</color>";
            else return "<color=#512513>" + GetArrow.GetArrows(Player, TargetPosition) + "</color>";
        }
        if (seen.PlayerId == Targetid) return "<color=#512513>★</color>";

        return "";
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (!seer.IsAlive() ||
            !GuessMode ||
            !isForMeeting ||
            Targetid == byte.MaxValue
        ) return "";

        return GetString("MassMediaChance");
    }
    public bool? CheckKillFlash(MurderInfo info) => info.AppearanceTarget.PlayerId == Targetid && Canseekillflash;
    public override bool CheckVoteAsVoter(byte votedForId, PlayerControl voter)
    {
        if (!SelfVoteManager.Canuseability()) return true;
        if (Is(voter) && GuessMode && Player.Is(CustomRoles.MassMedia))
        {
            if (votedForId == 253)
            {
                GuessMode = false;
                SendRPC();
                return true;
            }
            if (votedForId == Guees)
            {
                //勝利判定
                Win = true;
                MeetingVoteManager.Instance.ClearAndExile(Player.PlayerId, Player.PlayerId);
                return true;
            }
            else
            {
                //違うなら消えてもらおうか。
                MeetingVoteManager.Instance.ClearAndExile(Player.PlayerId, Player.PlayerId);
                MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.Misfire, Player.PlayerId);

                UtilsGameLog.AddGameLog($"MassMedia", string.Format(GetString("MassMedia.log"), UtilsName.GetPlayerColor(Player)));
                return true;
            }
        }
        return true;
    }
    public override void AfterMeetingTasks()
    {
        if (AddOns.Common.Amnesia.CheckAbilityreturn(Player)) return;
        if (Win && AmongUsClient.Instance.AmHost)
        {
            if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.MassMedia, Player.PlayerId, true))
            {
                CustomWinnerHolder.NeutralWinnerIds.Add(Player.PlayerId);
                foreach (var crew in PlayerCatch.AllAlivePlayerControls.Where(x => x.GetCustomRole().IsCrewmate()))
                {
                    crew.SetRealKiller(Player);
                    crew.RpcMurderPlayer(crew);
                    var state = PlayerState.GetByPlayerId(crew.PlayerId);
                    state.DeathReason = CustomDeathReason.Misfire;
                    state.SetDead();
                }
            }
        }
        Main.AllPlayerKillCooldown[Player.PlayerId] = KillCooldown;
        IsBlackOut = false;
        Suspects = new();
        if (AmongUsClient.Instance.AmHost) Player.SyncSettings();
        _ = new LateTask(() => SuspectsSearch = true, 10, "MassMediaSearch");
    }
    public bool CanUseKillButton() => true;
    public bool CanUseImpostorVentButton() => false;
    public bool CanUseSabotageButton() => false;
    public override void ApplyGameOptions(IGameOptions opt)
    {
        if (IsBlackOut)
        {
            opt.SetFloat(Player == PlayerControl.LocalPlayer ? FloatOptionNames.CrewLightMod : FloatOptionNames.ImpostorLightMod, OptionBlackVision.GetFloat());
        }
        else
        {
            opt.SetVision(false);
        }
    }
    public float CalculateKillCooldown() => KillCooldown;

    public bool OverrideKillButton(out string text)
    {
        text = "MassMedia_Kill";
        return true;
    }

    public void SendRPC()
    {
        var isPos999 = TargetPosition == new Vector3(999f, 999f);
        using var sender = CreateSender();
        sender.Writer.Write(GuessMode);
        sender.Writer.Write(Targetid);
        sender.Writer.Write(isPos999);
        if (!isPos999) NetHelpers.WriteVector2(TargetPosition, sender.Writer);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        GuessMode = reader.ReadBoolean();
        var newTargetId = reader.ReadByte();
        Vector2 newTargetPosition = reader.ReadBoolean() ? new Vector3(999f, 999f) : NetHelpers.ReadVector2(reader);

        //idが更新されたときのみ処理
        if (newTargetId != Targetid)
        {
            if (Targetid != byte.MaxValue)
                TargetArrow.Remove(Player.PlayerId, Targetid);

            if (newTargetId != byte.MaxValue)
                TargetArrow.Add(Player.PlayerId, newTargetId);
        }

        //posが更新されたときのみ処理
        if (newTargetPosition != (Vector2)TargetPosition)
        {
            if (TargetPosition != new Vector3(999f, 999f))
                GetArrow.Remove(Player.PlayerId, TargetPosition);

            if (newTargetPosition != new Vector2(999f, 999f))
                GetArrow.Add(Player.PlayerId, newTargetPosition);
        }

        Targetid = newTargetId;
        TargetPosition = newTargetPosition;
    }
}