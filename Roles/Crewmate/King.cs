using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Impostor;

namespace TownOfHost.Roles.Crewmate;

public sealed class King : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(King),
            player => new King(player),
            CustomRoles.King,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            21100,
            SetupOptionItem,
            "k",
            "#FFD700",
            (5, 4)
        );
    public King(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        IsDead = false;
        IsExiled = false;
    }
    static OptionItem OptExileVoteCount;
    static OptionItem OptExiledInvolvementCrewmates;
    static OptionItem OptDeathReason;
    static OptionItem OptExiledRemoveAddonPlayercount;
    static OptionItem OptExiledRemoveRolePlayercount;
    public static OptionItem OptIsGuessTarget;
    public static readonly CustomDeathReason[] deathReasons =
    {
        CustomDeathReason.Kill,CustomDeathReason.Suicide,CustomDeathReason.Revenge,CustomDeathReason.FollowingSuicide
    };
    bool IsDead;
    bool IsExiled;
    enum OptionName
    {
        KingExileVoteCount,
        KingExileCrewDies,
        KingDeathReason,
        KingAddon,
        KingRole,
        KingCanGuesser
    }
    static void SetupOptionItem()
    {
        var cRolesString = deathReasons.Select(x => x.ToString()).ToArray();
        OptExileVoteCount = IntegerOptionItem.Create(RoleInfo, 10, OptionName.KingExileVoteCount, new(1, 15, 1), 3, false).SetValueFormat(OptionFormat.Votes);
        OptExiledInvolvementCrewmates = IntegerOptionItem.Create(RoleInfo, 11, OptionName.KingExileCrewDies, new(0, 15, 1), 5, false).SetValueFormat(OptionFormat.Players);
        OptDeathReason = StringOptionItem.Create(RoleInfo, 12, OptionName.KingDeathReason, cRolesString, 3, false);
        OptExiledRemoveAddonPlayercount = IntegerOptionItem.Create(RoleInfo, 13, OptionName.KingAddon, new(0, 15, 1), 5, false).SetValueFormat(OptionFormat.Players);
        OptExiledRemoveRolePlayercount = IntegerOptionItem.Create(RoleInfo, 14, OptionName.KingRole, new(0, 15, 1), 5, false).SetValueFormat(OptionFormat.Players);
        OptIsGuessTarget = BooleanOptionItem.Create(RoleInfo, 15, OptionName.KingCanGuesser, true, false);
    }
    public override bool? CheckGuess(PlayerControl killer)
    {
        return OptIsGuessTarget.GetBool();
    }
    public override bool VotingResults(ref NetworkedPlayerInfo Exiled, ref bool IsTie, Dictionary<byte, int> vote, byte[] mostVotedPlayers, bool ClearAndExile)
    {
        if (vote.TryGetValue(Player.PlayerId, out var count))
        {
            if (count >= OptExileVoteCount.GetInt())
            {
                IsTie = false;
                Exiled = Player.Data;
                IsExiled = true;
                return true;
            }
        }
        return false;
    }
    public override void OnLeftPlayer(PlayerControl player)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            if (player == Player)
                if (IsExiled && !IsDead)
                {
                    _ = new LateTask(() => CrewMateAbooooon(), 20f, "KingExdie");
                }
        }
        if (player == Player) IsDead = true;
    }
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        info.GuardPower = 9;
        var (killer, target) = info.AppearanceTuple;
        if (killer.GetRoleClass() is BountyHunter bountyHunter)
        {
            bountyHunter.OnKingKill(this);
        }
        killer.SetKillCooldown(target: target);
        return false;
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (GameStates.ExiledAnimate) return;
        if (!IsExiled)
        {
            if (IsDead) return;

            if (player.Data.Disconnected && MyState.DeathReason is CustomDeathReason.Disconnected) return;
        }
        if (!player.IsAlive())
        {
            CrewMateAbooooon();
            IsExiled = false;
            IsDead = true;
        }
    }
    public override void OverrideDisplayRoleNameAsSeen(PlayerControl seer, ref bool enabled, ref UnityEngine.Color roleColor, ref string roleText, ref bool addon)
    {
        seer ??= Player;
        if (seer == Player) return;
        if (seer.Is(CustomRoleTypes.Crewmate) || seer.Is(CustomRoles.BakeCat))
        {
            enabled = true;
            roleColor = StringHelper.CodeColor("#FFD700");
            roleText = GetString("King");
            addon = false;
        }
    }
    void CrewMateAbooooon()
    {
        if (IsDead && !IsExiled) return;
        if (AmongUsClient.Instance.AmHost)
        {
            var rand = IRandom.Instance;
            int Count = OptExiledInvolvementCrewmates.GetInt();

            List<PlayerControl> crews = new();

            //対象者
            foreach (var pc in PlayerCatch.AllAlivePlayerControls)
            {
                if (!pc) continue;
                if (pc == Player) continue;
                if (!pc.IsAlive() || !pc.Is(CustomRoleTypes.Crewmate)) continue;
                if (!crews.Contains(pc)) crews.Add(pc);
            }

            if (!GameStates.CalledMeeting)
            {
                for (var i = 0; i < Count; i++)
                {
                    if (crews.Count == 0) break;
                    var pc = crews[rand.Next(0, crews.Count)];

                    if (pc == null)
                    {
                        i--;
                        continue;
                    }
                    if (!pc.IsAlive())
                    {
                        i--;
                        continue;
                    }

                    PlayerState state = PlayerState.GetByPlayerId(pc.PlayerId);
                    state.DeathReason = deathReasons[OptDeathReason.GetValue()];
                    CustomRoleManager.OnCheckMurder(Player, pc, pc, pc, true, true, 999);
                    Logger.Info($"{pc.name}が巻き込まれちゃった！", "Kingaboooooon");
                    crews.Remove(pc);
                }
            }
            else
            {
                for (var i = 0; i < Count; i++)
                {
                    if (crews.Count == 0) break;
                    var pc = crews[rand.Next(0, crews.Count)];

                    if (pc == null)
                    {
                        i--;
                        continue;
                    }
                    if (!pc.IsAlive())
                    {
                        i--;
                        continue;
                    }

                    PlayerState state = PlayerState.GetByPlayerId(pc.PlayerId);
                    state.DeathReason = deathReasons[OptDeathReason.GetValue()];
                    Player.RpcExileV2();
                    state.SetDead();
                    ReportDeadBodyPatch.IgnoreBodyids[Player.PlayerId] = false;

                    Logger.Info($"{pc.name}が後追いしちゃった！", "KingEx");
                    crews.Remove(pc);
                }
            }

            //役職 & 属性ぼっしゅー

            var addoncount = OptExiledRemoveAddonPlayercount.GetInt();
            if (addoncount != 0)
            {
                for (var i = 0; i < addoncount; i++)
                {
                    if (crews.Count == 0) break;
                    var pc = crews[rand.Next(0, crews.Count)];

                    if (pc == null)
                    {
                        i--;
                        continue;
                    }
                    if (!pc.IsAlive())
                    {
                        i--;
                        continue;
                    }

                    var ps = PlayerState.GetByPlayerId(pc.PlayerId);
                    List<CustomRoles> remove = new();
                    if (pc.GetCustomSubRoles() != null)
                        foreach (var addon in pc.GetCustomSubRoles())
                            if (addon.IsBuffAddon())
                            {
                                if (!remove.Contains(addon)) remove.Add(addon);
                                Logger.Info($"{pc.name}の{addon}ぼっしゅー", "KingAddon");
                            }

                    if (remove == null && remove?.Count != 0)
                    {
                        foreach (var addon in remove)
                            ps.RemoveSubRole(addon);
                    }
                }
            }

            var rolecount = OptExiledRemoveRolePlayercount.GetInt();
            if (rolecount != 0)
            {
                for (var i = 0; i < rolecount; i++)
                {
                    if (crews.Count == 0) break;
                    var pc = crews[rand.Next(0, crews.Count)];

                    if (pc == null)
                    {
                        i--;
                        continue;
                    }
                    if (!pc.IsAlive())
                    {
                        i--;
                        continue;
                    }

                    pc.RpcSetCustomRole(CustomRoles.Crewmate, true, null);
                    Logger.Info($"{pc.name}の役職クルーな！！ハハハ!!", "KingRoles");
                }
            }
            _ = new LateTask(() => UtilsNotifyRoles.NotifyRoles(), 0.4f, "KingResetNotify");
        }
        IsDead = true;
        IsExiled = false;
    }
}