using System.Collections.Generic;
using AmongUs.GameOptions;
using UnityEngine;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using Hazel;

namespace TownOfHost.Roles.Impostor;

public sealed class EvilTeller : RoleBase, IImpostor, IUsePhantomButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(EvilTeller),
            player => new EvilTeller(player),
            CustomRoles.EvilTeller,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Impostor,
            3700,
            SetUpOptionItem,
            "Et",
            OptionSort: (2, 6)
        );
    public EvilTeller(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        TargetInfo = null;
        seentarget.Clear();
        nowuse = false;
        fall = false;
        usekillcool = optusekillcoool.GetBool();
        cooldown = optcooldown.GetFloat();
        killcooldown = optkillcooldown.GetFloat();
        telltime = opttelltime.GetFloat();
        distance = optDistance.GetFloat();
        tellroleteam = opttellroleteam.GetBool();
        tellrole = opttellrole.GetBool();
        maxtellcount = optCanTellCount.GetInt();
    }
    static OptionItem optcooldown;
    static OptionItem optkillcooldown;
    static OptionItem opttelltime;
    static OptionItem optDistance;
    static OptionItem opttellroleteam;
    static OptionItem opttellrole;
    static OptionItem optusekillcoool;
    static OptionItem optCanTellCount;
    static float cooldown;
    static float killcooldown;
    static float telltime;
    static float distance;
    static bool tellroleteam;
    static bool tellrole;
    static bool usekillcool;
    static int maxtellcount;
    bool nowuse;
    bool fall;
    Dictionary<byte, CustomRoles> seentarget = new();
    enum OptionName { EvilTellerTellTime, EvilTellerDistance, EvilTellertellrole, EvilTellerCanTellCount }

    private TimerInfo TargetInfo;
    public class TimerInfo
    {
        public byte TargetId;
        public float Timer;
        public TimerInfo(byte targetId, float timer)
        {
            TargetId = targetId;
            Timer = timer;
        }
    }
    static void SetUpOptionItem()
    {
        optkillcooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, OptionBaseCoolTime, 30f, false).SetValueFormat(OptionFormat.Seconds);
        optcooldown = FloatOptionItem.Create(RoleInfo, 11, GeneralOption.Cooldown, OptionBaseCoolTime, 30f, false).SetValueFormat(OptionFormat.Seconds);
        optCanTellCount = IntegerOptionItem.Create(RoleInfo, 17, OptionName.EvilTellerCanTellCount, new(1, 99, 1), 3, false);
        opttelltime = FloatOptionItem.Create(RoleInfo, 12, OptionName.EvilTellerTellTime, new(0, 100, 0.5f), 5, false).SetValueFormat(OptionFormat.Seconds);
        optDistance = FloatOptionItem.Create(RoleInfo, 13, OptionName.EvilTellerDistance, new(1f, 30f, 0.25f), 1.75f, false);
        opttellroleteam = BooleanOptionItem.Create(RoleInfo, 14, "TellRole", false, false);
        opttellrole = BooleanOptionItem.Create(RoleInfo, 15, OptionName.EvilTellertellrole, false, false);
        optusekillcoool = BooleanOptionItem.Create(RoleInfo, 16, "OptionSetKillcooldown", false, false);
    }
    public float CalculateKillCooldown() => killcooldown;
    public override void ApplyGameOptions(IGameOptions opt) => AURoleOptions.PhantomCooldown = maxtellcount <= seentarget.Count ? 200f : (fall ? 1 : (nowuse ? telltime : cooldown));
    bool IUsePhantomButton.IsPhantomRole => maxtellcount > seentarget.Count;
    public void OnClick(ref bool AdjustKillCooldown, ref bool? ResetCooldown)
    {
        AdjustKillCooldown = true;
        if (maxtellcount <= seentarget.Count) return;
        ResetCooldown = false;
        var target = Player.GetKillTarget(true);
        if (target == null) { ResetCooldown = false; return; }
        if (target.Is(CustomRoleTypes.Impostor)) { ResetCooldown = false; return; }

        if (seentarget.ContainsKey(target.PlayerId) || TargetInfo != null) { ResetCooldown = false; return; }

        TargetInfo = new(target.PlayerId, 0f);
        nowuse = true;
        ResetCooldown = false;

        RpcSetTargetInfo(target.PlayerId);
        _ = new LateTask(() =>
        {
            Player.RpcResetAbilityCooldown(Sync: true);
            UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player);
        }, 0.2f, "", true);
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (isForMeeting) return "";
        if (!seer.IsAlive()) return "";

        if (seen.PlayerId == (TargetInfo?.TargetId ?? byte.MaxValue)) return "<color=#ff1919>◆</color>";
        return "";
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target) { fall = false; TargetInfo = null; nowuse = false; }
    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, ref bool enabled, ref Color roleColor, ref string roleText, ref bool addon)
    {
        if (!seen) return;
        if (!Player.IsAlive()) return;

        if (seentarget.TryGetValue(seen.PlayerId, out var role))
        {
            enabled = true;
            addon = false;
            if (tellrole) role = seen.GetCustomRole();
            if (!tellroleteam)
            {
                switch (seen.GetCustomRole().GetCustomRoleTypes())
                {
                    case CustomRoleTypes.Crewmate:
                    case CustomRoleTypes.Madmate:
                        roleColor = Palette.CrewmateBlue;
                        roleText = GetString("Crewmate");
                        break;
                    case CustomRoleTypes.Impostor:
                        roleColor = ModColors.ImpostorRed;
                        roleText = GetString("Impostor");
                        break;
                    case CustomRoleTypes.Neutral:
                        roleColor = ModColors.NeutralGray;
                        roleText = GetString("Neutral");
                        break;
                }
            }
            roleText = GetString($"{role}");
            roleColor = UtilsRoleText.GetRoleColor(role);
        }
    }
    public override string GetProgressText(bool comms = false, bool GameLog = false) => maxtellcount <= seentarget.Count ? $"<color=#cccccc>({maxtellcount - seentarget.Count})</color>" : $"<color=#ff1919>({maxtellcount - seentarget.Count})</color>";
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        if (maxtellcount <= seentarget.Count) return;

        if (GameStates.IsInTask && TargetInfo != null)
        {
            var et_target = PlayerCatch.GetPlayerById(TargetInfo.TargetId);
            var et_time = TargetInfo.Timer;
            if (!et_target.IsAlive())
            {
                nowuse = false;
                fall = true;
                TargetInfo = null;
            }
            else if (telltime <= et_time)
            {
                nowuse = false;
                fall = false;
                TargetInfo = null;
                Player.RpcResetAbilityCooldown(Sync: true);
                if (usekillcool && !fall) Player.SetKillCooldown();
                seentarget.TryAdd(et_target.PlayerId, et_target.GetCustomRole());
                RpcAddTarget(et_target.PlayerId);

                UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player, ForceLoop: true);
            }
            else
            {
                float dis;
                dis = Vector2.Distance(Player.transform.position, et_target.transform.position);
                if (dis <= distance)
                {
                    TargetInfo.Timer += Time.fixedDeltaTime;
                }
                else
                {
                    nowuse = false;
                    TargetInfo = null;
                    fall = true;
                    UtilsNotifyRoles.NotifyRoles(SpecifySeer: Player);

                    Logger.Info($"Canceled: {Player.GetNameWithRole().RemoveHtmlTags()}", "CurseMaker");
                }
            }
        }
    }
    public override string GetAbilityButtonText() => GetString("EvliTellerAbility");
    public override bool OverrideAbilityButton(out string text)
    {
        text = "EvliTeller_Ability";
        return true;
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        seen ??= seer;
        if (seen.PlayerId != seer.PlayerId || isForMeeting || maxtellcount <= seentarget.Count || !Player.IsAlive()) return "";

        if (isForHud) return GetString("PhantomButtonKilltargetLowertext");
        return $"<size=50%>{GetString("PhantomButtonKilltargetLowertext")}</size>";
    }

    public void RpcSetTargetInfo(byte targetId)
    {
        using var sender = CreateSender();
        sender.Writer.WritePacked((int)RPC_Types.SetTargetInfo);
        sender.Writer.Write(targetId);
    }

    public void RpcAddTarget(byte targetId)
    {
        using var sender = CreateSender();
        sender.Writer.WritePacked((int)RPC_Types.AddTarget);
        sender.Writer.Write(targetId);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        switch ((RPC_Types)reader.ReadInt32())
        {
            case RPC_Types.SetTargetInfo:
                TargetInfo = new(reader.ReadByte(), 0f);
                break;
            case RPC_Types.AddTarget:
                var targetId = reader.ReadByte();
                seentarget.TryAdd(targetId, PlayerCatch.GetPlayerById(targetId).GetCustomRole());
                break;
        }
    }

    enum RPC_Types
    {
        SetTargetInfo,
        AddTarget
    }
}
