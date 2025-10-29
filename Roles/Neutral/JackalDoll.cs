using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using UnityEngine;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.AddOns.Neutral;
using Hazel;

namespace TownOfHost.Roles.Neutral;

public sealed class JackalDoll : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(JackalDoll),
            player => new JackalDoll(player),
            CustomRoles.Jackaldoll,
            () => CanVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            13200,
            SetupOptionItem,
            "jacd",
            "#00b4eb",
            (1, 3),
                assignInfo: new RoleAssignInfo(CustomRoles.Jackaldoll, CustomRoleTypes.Neutral)
                {
                    AssignCountRule = new(0, 15, 1)
                }
        );
    public JackalDoll(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        BossAndSidekicks.Clear();
        CanPromotion = false;
        ExiledPlayerInfo = null;
    }
    static OptionItem OptionJackaldieMode;
    static OptionItem OptionChangeRole;
    static OptionItem OptionSideKickMaxmim;
    static OptionItem CanVent;
    static OptionItem VentCool;
    static OptionItem VentIntime;
    static OptionItem CanVentMove;
    static NetworkedPlayerInfo ExiledPlayerInfo;
    enum Option
    {
        JackaldolldieMode, JackaldollRoleChe, SideKickJackaldollMacCount
    }
    enum Diemode
    {
        NoProcessing,
        FollowingSuicide,
        ChangeRole,
    };
    public static int NowSideKickCount;
    bool CanPromotion;
    /// <summary>
    /// key→Sidekick
    /// Va→Owner
    /// </summary>
    /// <returns></returns>
    public static Dictionary<byte, (byte Owner, CustomRoles Ownerrole)> BossAndSidekicks = new();
    public static readonly CustomRoles[] ChangeRoles =
    {
        CustomRoles.Crewmate, CustomRoles.Madmate , CustomRoles.Jester, CustomRoles.Opportunist,CustomRoles.Monochromer
    };
    public static int GetSideKickCount()
    {
        if ((CustomRoles.Jackal.IsEnable() && Jackal.OptionCanMakeSidekick.GetBool())
        || (CustomRoles.JackalAlien.IsEnable() && JackalAlien.OptionCanMakeSidekick.GetBool())
        || (CustomRoles.JackalMafia.IsEnable() && JackalMafia.OptionCanMakeSidekick.GetBool()))
        {
            // 0%以上なら確認する。
            if (Options.GetRoleChance(CustomRoles.Jackaldoll) > 0)
            {
                return OptionSideKickMaxmim.GetInt();
            }
            // 0%でサイドキックあり得るなら1は返してあげる。
            return 1;
        }
        else return 0;
    }
    private static void SetupOptionItem()
    {
        var cRolesString = ChangeRoles.Select(x => x.ToString()).ToArray();
        OptionSideKickMaxmim = IntegerOptionItem.Create(RoleInfo, 9, Option.SideKickJackaldollMacCount, new(0, 15, 1), 1, false);
        OptionJackaldieMode = StringOptionItem.Create(RoleInfo, 10, Option.JackaldolldieMode, EnumHelper.GetAllNames<Diemode>(), 0, false);
        OptionChangeRole = StringOptionItem.Create(RoleInfo, 15, Option.JackaldollRoleChe, cRolesString, 3, false)
        .SetEnabled(() => OptionJackaldieMode.GetValue() is 2);
        CanVent = BooleanOptionItem.Create(RoleInfo, 16, GeneralOption.CanVent, false, false);
        VentCool = FloatOptionItem.Create(RoleInfo, 17, GeneralOption.Cooldown, new(0f, 180f, 0.5f), 0f, false, CanVent).SetValueFormat(OptionFormat.Seconds);
        VentIntime = FloatOptionItem.Create(RoleInfo, 18, GeneralOption.EngineerInVentCooldown, new(0f, 180f, 0.5f), 0f, false, CanVent).SetZeroNotation(OptionZeroNotation.Infinity).SetValueFormat(OptionFormat.Seconds);
        CanVentMove = BooleanOptionItem.Create(RoleInfo, 19, "MadmateCanMovedByVent", false, false, CanVent);
        RoleAddAddons.Create(RoleInfo, 20, MadMate: true);
    }
    public override bool CanVentMoving(PlayerPhysics physics, int ventId) => CanVentMove.GetBool();
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = VentCool.GetFloat();
        AURoleOptions.EngineerInVentMaxTime = VentIntime.GetFloat();
    }
    public static void Sidekick(PlayerControl doll, PlayerControl owner)
    {
        NowSideKickCount++;
        if (BossAndSidekicks.ContainsKey(doll.PlayerId))
        {
            BossAndSidekicks.Remove(doll.PlayerId);
        }

        var state = PlayerState.GetByPlayerId(doll.PlayerId);

        if (owner.Is(CustomRoles.Jackal))
        {
            if (Jackal.OptionSidekickPromotion.GetBool())
            {
                Main.AllPlayerKillCooldown[doll.PlayerId] = Jackal.OptionKillCooldown.GetFloat();
                BossAndSidekicks.Add(doll.PlayerId, (owner.PlayerId, CustomRoles.Jackal));

                (doll.GetRoleClass() as JackalDoll).SetSidekick(owner.PlayerId, CustomRoles.Jackal);
            }
            if (Jackal.OptionSidekickCanSeeOldImpostorTeammates.GetBool())
            {
                foreach (var imp in PlayerCatch.AllPlayerFirstTypes.Where(x => x.Value is CustomRoleTypes.Impostor))
                {
                    if (state.TargetColorData.ContainsKey(imp.Key)) NameColorManager.Remove(doll.PlayerId, imp.Key);
                    NameColorManager.Add(doll.PlayerId, imp.Key, "ffffff");
                }
            }
            if (Jackal.OptionImpostorCanSeeNameColor.GetBool())
            {
                foreach (var imp in PlayerCatch.AllPlayerFirstTypes.Where(x => x.Value is CustomRoleTypes.Impostor))
                {
                    var impostorstate = PlayerState.GetByPlayerId(imp.Key);
                    if (impostorstate.TargetColorData.ContainsKey(doll.PlayerId)) NameColorManager.Remove(imp.Key, doll.PlayerId);
                    NameColorManager.Add(imp.Key, doll.PlayerId, "ffffff");
                }
            }
        }
        if (owner.Is(CustomRoles.JackalMafia))
        {
            if (JackalMafia.OptionSidekickPromotion.GetBool())
            {
                Main.AllPlayerKillCooldown[doll.PlayerId] = JackalMafia.OptionKillCooldown.GetFloat();
                BossAndSidekicks.Add(doll.PlayerId, (owner.PlayerId, CustomRoles.JackalMafia));
                (doll.GetRoleClass() as JackalDoll).SetSidekick(owner.PlayerId, CustomRoles.JackalMafia);
            }
            if (JackalMafia.OptionSidekickCanSeeOldImpostorTeammates.GetBool())
            {
                foreach (var imp in PlayerCatch.AllPlayerFirstTypes.Where(x => x.Value is CustomRoleTypes.Impostor))
                {
                    if (state.TargetColorData.ContainsKey(imp.Key)) NameColorManager.Remove(doll.PlayerId, imp.Key);
                    NameColorManager.Add(doll.PlayerId, imp.Key, "ffffff");
                }
            }
            if (JackalMafia.OptionImpostorCanSeeNameColor.GetBool())
            {
                foreach (var imp in PlayerCatch.AllPlayerFirstTypes.Where(x => x.Value is CustomRoleTypes.Impostor))
                {
                    var impostorstate = PlayerState.GetByPlayerId(imp.Key);
                    if (impostorstate.TargetColorData.ContainsKey(doll.PlayerId)) NameColorManager.Remove(imp.Key, doll.PlayerId);
                    NameColorManager.Add(imp.Key, doll.PlayerId, "ffffff");
                }
            }
        }
        if (owner.Is(CustomRoles.JackalAlien))
        {
            if (JackalAlien.OptionSidekickPromotion.GetBool())
            {
                Main.AllPlayerKillCooldown[doll.PlayerId] = JackalAlien.OptionKillCooldown.GetFloat();
                BossAndSidekicks.Add(doll.PlayerId, (owner.PlayerId, CustomRoles.JackalAlien));
                (doll.GetRoleClass() as JackalDoll).SetSidekick(owner.PlayerId, CustomRoles.JackalAlien);
            }
            if (JackalAlien.OptionSidekickCanSeeOldImpostorTeammates.GetBool())
            {
                foreach (var imp in PlayerCatch.AllPlayerFirstTypes.Where(x => x.Value is CustomRoleTypes.Impostor))
                {
                    if (state.TargetColorData.ContainsKey(imp.Key)) NameColorManager.Remove(doll.PlayerId, imp.Key);
                    NameColorManager.Add(doll.PlayerId, imp.Key, "ffffff");
                }
            }
            if (JackalAlien.OptionImpostorCanSeeNameColor.GetBool())
            {
                foreach (var imp in PlayerCatch.AllPlayerFirstTypes.Where(x => x.Value is CustomRoleTypes.Impostor))
                {
                    var impostorstate = PlayerState.GetByPlayerId(imp.Key);
                    if (impostorstate.TargetColorData.ContainsKey(doll.PlayerId)) NameColorManager.Remove(imp.Key, doll.PlayerId);
                    NameColorManager.Add(imp.Key, doll.PlayerId, "ffffff");
                }
            }
        }

        doll.RpcSetRole(CanVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate, true);

        //サイドキックがガード等発動しないため。
        if (RoleAddAddons.GetRoleAddon(CustomRoles.Jackaldoll, out var addondate, doll, subrole: [CustomRoles.Guarding]))
        {
            if (addondate.GiveGuarding.GetBool()) doll.GetPlayerState().HaveGuard[1] += addondate.Guard.GetInt();
        }
        foreach (var jackals in PlayerCatch.AllPlayerControls)
        {
            if (jackals.Is(CountTypes.Jackal))
            {
                NameColorManager.Add(jackals.PlayerId, doll.PlayerId, UtilsRoleText.GetRoleColorCode(CustomRoles.Jackaldoll));
                NameColorManager.Add(doll.PlayerId, jackals.PlayerId, UtilsRoleText.GetRoleColorCode(CustomRoles.Jackaldoll));
            }
        }
        //どっちにしろ更新を
        UtilsNotifyRoles.NotifyRoles();
    }
    public override bool VotingResults(ref NetworkedPlayerInfo Exiled, ref bool IsTie, Dictionary<byte, int> vote, byte[] mostVotedPlayers, bool ClearAndExile)
    {
        ExiledPlayerInfo = Exiled;
        return false;
    }
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        var (killer, target) = info.AppearanceTuple;
        if (BossAndSidekicks.TryGetValue(target.PlayerId, out var data))
        {//サイドキックされて親分に殺されそうとか言う事になるとキルガード
            if (data.Owner == killer.PlayerId)
            {
                info.CanKill = false;
                killer.RpcProtectedMurderPlayer(target);
                return false;
            }
        }
        return true;
    }
    public override void AfterMeetingTasks()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (BossAndSidekicks.ContainsKey(Player.PlayerId)) return;

        var id = ExiledPlayerInfo?.PlayerId ?? byte.MaxValue;

        if (PlayerCatch.AllAlivePlayerControls.Any(x => (x.Is(CustomRoles.Jackal) || x.Is(CustomRoles.JackalMafia) || x.Is(CustomRoles.JackalAlien) || BossAndSidekicks.ContainsKey(x.PlayerId)) && x.PlayerId != id)) return;

        foreach (var Jd in PlayerCatch.AllAlivePlayerControls.Where(x => x.Is(CustomRoles.Jackaldoll) && !BossAndSidekicks.ContainsKey(x.PlayerId)))
        {
            switch ((Diemode)OptionJackaldieMode.GetValue())
            {
                case Diemode.FollowingSuicide:
                    //ガードなどは無視
                    PlayerState.GetByPlayerId(Jd.PlayerId).DeathReason = CustomDeathReason.FollowingSuicide;
                    Jd.RpcExileV2();
                    PlayerState.GetByPlayerId(Jd.PlayerId).SetDead();
                    break;
                case Diemode.ChangeRole:
                    UtilsGameLog.AddGameLog($"JackalDool", UtilsName.GetPlayerColor(Jd) + ":  " + string.Format(GetString("Executioner.ch"), Utils.ColorString(UtilsRoleText.GetRoleColor(CustomRoles.Jackal), GetString("Jackal")), Translator.GetRoleString($"{ChangeRoles[OptionChangeRole.GetValue()]}").Color(UtilsRoleText.GetRoleColor(ChangeRoles[OptionChangeRole.GetValue()]))));
                    if (!Utils.RoleSendList.Contains(Player.PlayerId)) Utils.RoleSendList.Add(Player.PlayerId);
                    Jd.RpcSetCustomRole(ChangeRoles[OptionChangeRole.GetValue()], log: null);
                    UtilsNotifyRoles.NotifyRoles();
                    break;
            }
        }
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!player.IsAlive()) return;
        if (!AmongUsClient.Instance.AmHost) return;

        if (BossAndSidekicks.TryGetValue(player.PlayerId, out var data))
        {
            var oya = PlayerCatch.GetPlayerById(data.Owner);
            if ((!oya.IsAlive() || oya.GetCustomRole() != data.Ownerrole) && !CanPromotion)
            {
                MyState.SetCountType(CountTypes.Jackal);
                CanPromotion = true;
                if (!Utils.RoleSendList.Contains(Player.PlayerId)) Utils.RoleSendList.Add(Player.PlayerId);
                player.RpcSetCustomRole(data.Ownerrole, true);

                //徒党が存在していて、ジャッカルの徒党がON
                if (PlayerCatch.AllPlayerControls.Any(pc => pc.Is(CustomRoles.Faction)) && Faction.OptionRole.TryGetValue(CustomRoles.Jackal, out var option))
                {
                    if (option.GetBool())
                    {
                        player.RpcSetCustomRole(CustomRoles.Faction);
                        Logger.Info($"{player.Data.GetLogPlayerName()} => 昇格徒党", "Faction");
                    }
                }
            }
            CanPromotion = false;
        }
    }
    public override void OverrideTrueRoleName(ref Color roleColor, ref string roleText)
    {
        if (BossAndSidekicks.ContainsKey(Player.PlayerId))
        {
            roleText = $"☆" + GetString("Jackaldoll");
        }
    }
    public override string GetProgressText(bool comms = false, bool GameLog = false)
    {
        if (GameLog) return "";

        if (BossAndSidekicks.TryGetValue(Player.PlayerId, out var data))
        {
            return Utils.ColorString(Main.PlayerColors[data.Owner], "◎");
        }
        return "";
    }
    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, ref bool enabled, ref Color roleColor, ref string roleText, ref bool addon)
    {
        addon = false;
        if (seen.Is(CountTypes.Jackal))
            enabled = true;
    }
    public override void OverrideDisplayRoleNameAsSeen(PlayerControl seen, ref bool enabled, ref Color roleColor, ref string roleText, ref bool addon)
    {
        addon = false;
        if (seen.Is(CountTypes.Jackal))
            enabled = true;
    }

    public void SetSidekick(byte ownerId, CustomRoles ownerRole)
    {
        using var sender = CreateSender();
        sender.Writer.Write(NowSideKickCount);
        sender.Writer.Write(ownerId);
        sender.Writer.Write((int)ownerRole);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        NowSideKickCount = reader.ReadInt32();
        var ownerId = reader.ReadByte();
        var ownerRole = (CustomRoles)reader.ReadInt32();
        BossAndSidekicks[Player.PlayerId] = (ownerId, ownerRole);
    }
}