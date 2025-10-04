using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Hazel;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Modules;
using static TownOfHost.OverrideKilldistance;

namespace TownOfHost.Roles.Impostor;

public sealed class Puppeteer : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Puppeteer),
            player => new Puppeteer(player),
            CustomRoles.Puppeteer,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            4900,
            SetUpOption,
            "pup",
            OptionSort: (4, 3),
            from: From.TownOfHost
        );
    public Puppeteer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        PuppetCooltime.Clear();
        CustomRoleManager.OnFixedUpdateOthers.Add(OnFixedUpdateOthers);
    }
    static OptionItem PuppetCool;
    static OptionItem KillCooldown;
    static OptionItem PuppetKillDis;
    enum Op { PuppeteerPuppetCool }
    public static void SetUpOption()
    {
        KillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0, 180, 0.5f), 25f, false).SetValueFormat(OptionFormat.Seconds);
        PuppetCool = FloatOptionItem.Create(RoleInfo, 11, Op.PuppeteerPuppetCool, new(0, 100, 0.5f), 5f, false).SetValueFormat(OptionFormat.Seconds);
        Create(RoleInfo, 12);
        PuppetKillDis = StringOptionItem.Create(RoleInfo, 13, "Killdistance", EnumHelper.GetAllNames<KillDistance>(), 0, false);
        PuppetKillDis.ReplacementDictionary = new() { { "%role%", GetString("Puppet") } };
    }
    public override bool NotifyRolesCheckOtherName => true;
    /// <summary>
    /// Key: ターゲットのPlayerId, Value: パペッティア
    /// </summary>
    private static Dictionary<byte, Puppeteer> Puppets = new(15);
    private static Dictionary<byte, float> PuppetCooltime = new(15);
    public override void OnDestroy()
    {
        Puppets.Clear();
        PuppetCooltime.Clear();
    }
    public float CalculateKillCooldown() => KillCooldown.GetFloat();
    private void SendRPC(byte targetId, byte typeId)
    {
        using var sender = CreateSender();

        sender.Writer.Write(typeId);
        sender.Writer.Write(targetId);
    }
    public override void ReceiveRPC(MessageReader reader)
    {
        var typeId = reader.ReadByte();
        var targetId = reader.ReadByte();

        switch (typeId)
        {
            case 0: //Dictionaryのクリア
                Puppets.Clear();
                PuppetCooltime.Clear();
                break;
            case 1: //Dictionaryに追加
                Puppets[targetId] = this;
                PuppetCooltime[targetId] = 0;
                break;
            case 2: //DictionaryのKey削除
                Puppets.Remove(targetId);
                PuppetCooltime.Remove(targetId);
                break;
        }
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        var (puppeteer, target) = info.AttemptTuple;

        Puppets[target.PlayerId] = this;
        PuppetCooltime[target.PlayerId] = 0;
        SendRPC(target.PlayerId, 1);
        puppeteer.SetKillCooldown();
        UtilsNotifyRoles.NotifyRoles(SpecifySeer: puppeteer);
        info.DoKill = false;
    }
    public override void OnReportDeadBody(PlayerControl _, NetworkedPlayerInfo __)
    {
        Puppets.Clear();
        PuppetCooltime.Clear();
        SendRPC(byte.MaxValue, 0);
    }
    public static void OnFixedUpdateOthers(PlayerControl puppet)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        if (Puppets.TryGetValue(puppet.PlayerId, out var puppeteer))
        {
            if (PuppetCooltime.TryGetValue(puppet.PlayerId, out float pu))
            {
                PuppetCooltime[puppet.PlayerId] += Time.fixedDeltaTime;
            }
            else PuppetCooltime.Add(puppet.PlayerId, 0);
            puppeteer.CheckPuppetKill(puppet, pu);
        }
    }
    private void CheckPuppetKill(PlayerControl puppet, float cool)
    {
        if (!puppet.IsAlive())
        {
            Puppets.Remove(puppet.PlayerId);
            SendRPC(puppet.PlayerId, 2);
        }
        else
        {
            if (cool < PuppetCool.GetFloat()) return;
            var puppetPos = puppet.transform.position;//puppetの位置
            Dictionary<PlayerControl, float> targetDistance = new();
            foreach (var pc in PlayerCatch.AllAlivePlayerControls.ToArray())
            {
                if (pc.PlayerId != puppet.PlayerId && SuddenDeathMode.NowSuddenDeathMode)
                {
                    if (!SuddenDeathMode.NowSuddenDeathTemeMode || !SuddenDeathMode.IsSameteam(pc.PlayerId, Player.PlayerId))
                    {
                        var dis = Vector2.Distance(puppetPos, pc.transform.position);
                        targetDistance.Add(pc, dis);
                    }
                    continue;
                }

                if (pc.PlayerId != puppet.PlayerId && !pc.Is(CountTypes.Impostor))
                {
                    var dis = Vector2.Distance(puppetPos, pc.transform.position);
                    targetDistance.Add(pc, dis);
                }
            }
            if (targetDistance.Keys.Count <= 0) return;

            var min = targetDistance.OrderBy(c => c.Value).FirstOrDefault();//一番値が小さい
            var target = min.Key;
            var KillRange = NormalGameOptionsV10.KillDistances[Mathf.Clamp(PuppetKillDis.GetValue(), 0, 2)];
            if (min.Value <= KillRange && puppet.CanMove && target.CanMove)
            {
                PuppetCooltime.Remove(puppet.PlayerId);
                if (CustomRoleManager.OnCheckMurder(Player, target, puppet, target, true, false, 1))
                {
                    RPC.PlaySoundRPC(Player.PlayerId, Sounds.KillSound);
                }
                UtilsOption.MarkEveryoneDirtySettings();
                Puppets.Remove(puppet.PlayerId);
                SendRPC(puppet.PlayerId, 2);
            }
        }
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool _ = false)
    {
        //seenが省略の場合seer
        seen ??= seer;

        if (!(Puppets.ContainsValue(this) &&
            Puppets.ContainsKey(seen.PlayerId))) return "";

        return Utils.ColorString(RoleInfo.RoleColor, "◆");
    }
    public bool OverrideKillButtonText(out string text)
    {
        text = GetString("PuppeteerOperateButtonText");
        return true;
    }
    public bool OverrideKillButton(out string text)
    {
        text = "Puppeteer_Kill";
        return true;
    }
}