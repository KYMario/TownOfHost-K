
using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TownOfHost.Roles;
using TownOfHost.Roles.Core;

using static TownOfHost.Options;

namespace TownOfHost;

class ColorLovers
{
    public static Dictionary<CustomRoles, ColorLovers> Alldatas = new();
    public CustomRoles LoverRole { get; private set; }
    public int Id { get; private set; }
    public List<PlayerControl> LoverPlayer = new();
    public bool IsLoversDead = true;
    public OptionItem LoverCanSeeRole;
    public OptionItem LoverCanAddWin;
    public OptionItem Win3player;
    public OptionItem LoverSetRole;
    public FilterOptionItem LoversRole1;
    public FilterOptionItem LoversRole2;
    public OptionItem AssingImpostor;
    public OptionItem AssingMadmate;
    public OptionItem AssingCrewmate;
    public OptionItem AssingNeutral;

    static readonly CustomRoles[] Loverremove =
    {
        CustomRoles.Limiter,
        CustomRoles.Madonna,
        CustomRoles.King,
        CustomRoles.GM,
        CustomRoles.Vega,
        CustomRoles.Altair
    };
    public ColorLovers(CustomRoles role, int id)
    {
        LoverRole = role;
        Id = id;
        SetupRoleOptions(id, TabGroup.Combinations, role, assignCountRule: new(2, 2, 2), fromtext: role is CustomRoles.Lovers ? "<color=#000000>From:</color><color=#ff6be4>Love Couple Mod</color></size>" : "");
        LoverCanSeeRole = BooleanOptionItem.Create(id + 5, "LoversRole", false, TabGroup.Combinations, false).SetParent(CustomRoleSpawnChances[role]).SetParentRole(role);
        LoverCanAddWin = BooleanOptionItem.Create(id + 6, "LoversRoleAddwin", false, TabGroup.Combinations, false).SetParent(CustomRoleSpawnChances[role]).SetParentRole(role);
        SoloWinOption.Create(id + 7, TabGroup.Combinations, role, () => !LoverCanAddWin.GetBool(), defo: 6);
        Win3player = BooleanOptionItem.Create(id + 8, "LoverSoloWin3players", false, TabGroup.Combinations, false).SetParent(CustomRoleSpawnChances[role]).SetParentRole(role);
        LoverSetRole = BooleanOptionItem.Create(id + 9, "FixedRole", false, TabGroup.Combinations, false).SetParent(CustomRoleSpawnChances[role]).SetParentRole(role);
        LoversRole1 = (FilterOptionItem)FilterOptionItem.Create(id + 10, "Role", 0, TabGroup.Combinations, false, true, true, true, true, Loverremove).SetParent(LoverSetRole).SetParentRole(role);
        LoversRole2 = (FilterOptionItem)FilterOptionItem.Create(id + 11, "Role", 0, TabGroup.Combinations, false, true, true, true, true, Loverremove).SetParent(LoverSetRole).SetParentRole(role);
        AssingImpostor = BooleanOptionItem.Create(id + 12, "AssingroleType", true, TabGroup.Combinations, false).SetParent(CustomRoleSpawnChances[role]).SetParentRole(role).SetCansee(() => !LoversRole1.GetBool() || !LoversRole2.GetBool());
        AssingMadmate = BooleanOptionItem.Create(id + 13, "AssingroleType", true, TabGroup.Combinations, false).SetParent(CustomRoleSpawnChances[role]).SetParentRole(role).SetCansee(() => !LoversRole1.GetBool() || !LoversRole2.GetBool());
        AssingCrewmate = BooleanOptionItem.Create(id + 14, "AssingroleType", true, TabGroup.Combinations, false).SetParent(CustomRoleSpawnChances[role]).SetParentRole(role).SetCansee(() => !LoversRole1.GetBool() || !LoversRole2.GetBool());
        AssingNeutral = BooleanOptionItem.Create(id + 15, "AssingroleType", true, TabGroup.Combinations, false).SetParent(CustomRoleSpawnChances[role]).SetParentRole(role).SetCansee(() => !LoversRole1.GetBool() || !LoversRole2.GetBool());
        AssingImpostor.ReplacementDictionary = new Dictionary<string, string> { { "%roletype%", Utils.ColorString(Palette.ImpostorRed, Translator.GetString("TeamImpostor")) } };
        AssingMadmate.ReplacementDictionary = new Dictionary<string, string> { { "%roletype%", Utils.ColorString(Palette.ImpostorRed, Translator.GetString("Madmate")) } };
        AssingCrewmate.ReplacementDictionary = new Dictionary<string, string> { { "%roletype%", Utils.ColorString(Palette.CrewmateBlue, Translator.GetString("TeamCrewmate")) } };
        AssingNeutral.ReplacementDictionary = new Dictionary<string, string> { { "%roletype%", Utils.ColorString(Palette.AcceptedGreen, Translator.GetString("Neutral")) } };

        if (!Alldatas.TryAdd(role, this)) Logger.Error($"{role}重複したColorLovers", "ColorLovers");
    }
    public void Reset()
    {
        LoverPlayer.Clear();
        IsLoversDead = false;
    }
    #region  Assing
    public void AssingSetRole()
    {
        LoverPlayer.Clear();
        IsLoversDead = false;
        if (!LoverSetRole.GetBool()) return;
        if (!LoverRole.IsPresent()) return;

        var count = Math.Clamp(LoverRole.GetRealCount(), 0, PlayerCatch.AllPlayerControls.Where(pc => !Loverremove.Contains(pc.GetCustomRole()) && !pc.IsRiaju()).Count());
        if (count <= 0) return;

        List<PlayerControl> Assingplayer1 = new();
        List<PlayerControl> Assingplayer2 = new();
        foreach (var pc in PlayerCatch.AllPlayerControls)
        {
            var role = pc.GetCustomRole();

            if (pc.IsRiaju()) continue;
            if (Loverremove.Contains(role)) continue;

            if (role == LoversRole1.GetRole()) Assingplayer1.Add(pc);
            if (role == LoversRole2.GetRole()) Assingplayer2.Add(pc);
        }
        if (Assingplayer1.Count is not 0)
        {
            var pc = Assingplayer1[IRandom.Instance.Next(Assingplayer1.Count)];

            Lovers.HaveLoverDontTaskPlayers.Add(pc.PlayerId);
            PlayerState.GetByPlayerId(pc.PlayerId).SetSubRole(LoverRole);
            LoverPlayer.Add(pc);
            Assingplayer2.Remove(pc);
            Logger.Info($"役職設定1{pc.Data.GetLogPlayerName()} => {LoverRole}", "Lover");
        }
        if (Assingplayer2.Count is not 0)
        {
            var pc = Assingplayer2[IRandom.Instance.Next(Assingplayer2.Count)];

            Lovers.HaveLoverDontTaskPlayers.Add(pc.PlayerId);
            PlayerState.GetByPlayerId(pc.PlayerId).SetSubRole(LoverRole);
            LoverPlayer.Add(pc);
            Logger.Info($"役職設定2{pc.Data.GetLogPlayerName()} => {LoverRole}", "Lover");
        }
    }
    public void AssingOther()
    {
        if (!LoverRole.IsPresent()) return;
        if (2 <= LoverPlayer.Count) return;
        var count = Math.Clamp(LoverRole.GetRealCount(), 0, PlayerCatch.AllPlayerControls.Where(pc => !Loverremove.Contains(pc.GetCustomRole()) && !pc.IsRiaju()).Count());
        if (count <= 0) return;

        List<PlayerControl> AssingTarget = new();
        foreach (var pc in PlayerCatch.AllPlayerControls)
        {
            if (pc.IsRiaju()) continue;
            if (Loverremove.Contains(pc.GetCustomRole())) continue;

            if (!AssingImpostor.GetBool() && !AssingMadmate.GetBool() && !AssingCrewmate.GetBool() && !AssingNeutral.GetBool())
            { }
            else
            {
                var team = pc.GetCustomRole().GetCustomRoleTypes();
                switch (team)
                {
                    case CustomRoleTypes.Crewmate: if (!AssingCrewmate.GetBool()) continue; break;
                    case CustomRoleTypes.Impostor: if (!AssingImpostor.GetBool()) continue; break;
                    case CustomRoleTypes.Neutral: if (!AssingNeutral.GetBool()) continue; break;
                    case CustomRoleTypes.Madmate: if (!AssingMadmate.GetBool()) continue; break;
                }
            }
            AssingTarget.Add(pc);
        }
        if (AssingTarget.Count < (2 - LoverPlayer.Count)) return;

        {
            var pc = AssingTarget[IRandom.Instance.Next(AssingTarget.Count)];

            Lovers.HaveLoverDontTaskPlayers.Add(pc.PlayerId);
            PlayerState.GetByPlayerId(pc.PlayerId).SetSubRole(LoverRole);
            LoverPlayer.Add(pc);
            Logger.Info($"役職設定{pc.Data.GetLogPlayerName()} => {LoverRole}", "Lover");
            AssingTarget.Remove(pc);
        }
        if (AssingTarget.Count > 0 && LoverPlayer.Count == 1)
        {
            var pc = AssingTarget[IRandom.Instance.Next(AssingTarget.Count)];

            Lovers.HaveLoverDontTaskPlayers.Add(pc.PlayerId);
            PlayerState.GetByPlayerId(pc.PlayerId).SetSubRole(LoverRole);
            LoverPlayer.Add(pc);
            Logger.Info($"役職設定{pc.Data.GetLogPlayerName()} => {LoverRole}", "Lover");
            AssingTarget.Remove(pc);
        }
    }
    public void AssingCheck()
    {
        if (LoverPlayer.Count is 0)
        {
            IsLoversDead = true;
        }
        if (LoverPlayer.Count is 1)
        {
            foreach (var pc in LoverPlayer)
            {
                pc.GetPlayerState().RemoveSubRole(LoverRole);
                Logger.Info($"削除{pc.Data.GetLogPlayerName()} => {LoverRole}", "Lover");
            }
            LoverPlayer.Clear();
            IsLoversDead = true;
        }
        if (LoverPlayer.Count is 2)
        {
            IsLoversDead = false;
            RPC.SyncLoversPlayers(LoverRole);
        }
    }
    #endregion
    #region Suicide
    public void LoversSuicide(byte deathId = 0x7f, bool isExiled = false)
    {
        if (LoverRole.IsPresent() && IsLoversDead == false)
        {
            foreach (var loversPlayer in LoverPlayer)
            {
                //生きていて死ぬ予定でなければスキップ
                if (!loversPlayer.Data.IsDead && loversPlayer.PlayerId != deathId) continue;

                IsLoversDead = true;
                foreach (var partnerPlayer in LoverPlayer)
                {
                    //本人ならスキップ
                    if (loversPlayer.PlayerId == partnerPlayer.PlayerId) continue;

                    //残った恋人を全て殺す(2人以上可)
                    //生きていて死ぬ予定もない場合は心中
                    if (partnerPlayer.PlayerId != deathId && !partnerPlayer.Data.IsDead)
                    {
                        PlayerState.GetByPlayerId(partnerPlayer.PlayerId).DeathReason = CustomDeathReason.FollowingSuicide;
                        if (isExiled || GameStates.IsMeeting || AntiBlackout.IsSet || GameStates.Tuihou)
                        {
                            MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.FollowingSuicide, partnerPlayer.PlayerId);
                            ReportDeadBodyPatch.Musisuruoniku[loversPlayer.PlayerId] = false;
                        }
                        else
                            partnerPlayer.RpcMurderPlayer(partnerPlayer, true);
                    }
                }
            }
        }
    }
    #endregion
    #region Dis
    public void Disconnected(PlayerControl player)
    {
        if (player.Is(LoverRole) && !player.Data.IsDead)
        {
            IsLoversDead = true;
            foreach (var lv in LoverPlayer)
            {
                lv.GetPlayerState().RemoveSubRole(LoverRole);
            }
            LoverPlayer.Clear();
        }
    }
    #endregion
    #region Win
    public void SoloWin(ref GameOverReason reason)
    {
        if (LoverCanAddWin.GetBool()) return;
        //2人以下の勝利は～
        if (CustomWinnerHolder.WinnerTeam == (CustomWinner)LoverRole) return;

        if (LoverPlayer.Count > 0 && LoverPlayer.ToArray().All(p => p.IsAlive()))
        {
            if (CustomWinnerHolder.ResetAndSetAndChWinner((CustomWinner)LoverRole, byte.MaxValue))
            {
                PlayerCatch.AllPlayerControls
                    .Where(p => p.Is(LoverRole) && p.IsAlive())
                    .Do(p =>
                {
                    CustomWinnerHolder.WinnerIds.Add(p.PlayerId);
                    CustomWinnerHolder.IdRemoveLovers.Remove(p.PlayerId);
                });
                reason = GameOverReason.ImpostorsByKill;
            }
        }
    }
    public void AddWin()
    {
        if (CustomWinnerHolder.WinnerTeam == (CustomWinner)LoverRole) return;
        if (!LoverCanAddWin.GetBool()) return;

        if (LoverPlayer.Count > 0 && LoverPlayer.ToArray().All(p => p.IsAlive()))
        {
            CustomWinnerHolder.AdditionalWinnerRoles.Add(LoverRole);
            PlayerCatch.AllPlayerControls
                .Where(p => p.Is(LoverRole) && p.IsAlive())
                .Do(p =>
                {
                    CustomWinnerHolder.WinnerIds.Add(p.PlayerId);
                    CustomWinnerHolder.IdRemoveLovers.Remove(p.PlayerId);
                });
        }
    }
    public bool CheckCountWin()
    {
        if (PlayerCatch.AllAlivePlayerControls.All(p => p.Is(LoverRole)) ||
            (Win3player.GetBool() && PlayerCatch.AllAlivePlayersCount <= 3 && LoverPlayer.Count != 0 && LoverPlayer.All(pc => pc.IsAlive())))
        {
            if (CustomWinnerHolder.ResetAndSetAndChWinner((CustomWinner)LoverRole, byte.MaxValue))
            {
                PlayerCatch.AllPlayerControls
                    .Where(p => p.Is(LoverRole) && p.IsAlive())
                    .Do(p =>
                        {
                            CustomWinnerHolder.WinnerIds.Add(p.PlayerId);
                            CustomWinnerHolder.IdRemoveLovers.Remove(p.PlayerId);
                        });
            }
            return true;
        }

        return false;
    }
    #endregion

    public bool CheckCanSeeRole(PlayerControl seer, PlayerControl seen)
    {
        if (seer.Is(LoverRole) && seen.Is(LoverRole)) return LoverCanSeeRole.GetBool();
        return false;
    }
}