using System;
using System.Collections.Generic;
using System.Linq;
using Hazel;
using TownOfHost.Roles;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost;

class Twins
{
    public static Dictionary<byte, byte> TwinsList = new();
    public static List<byte> DieTwinsList = new();

    public static void Init()
    {
        SubRoleRPCSender.AddHandler(CustomRoles.Twins, ReceiveRPC);
    }

    public static void AssingAndReset()
    {
        TwinsList = new();
        DieTwinsList = new();
        var Sets = CustomRoles.Twins.GetRealCount();

        if (Sets <= 0) return;
        List<PlayerControl> AssingPlayers = new();

        foreach (var pc in PlayerCatch.AllPlayerControls)
        {
            var role = pc.GetCustomRole();
            if (role is CustomRoles.GM or CustomRoles.BakeCat || role.IsImpostor()) continue;
            if (pc.IsNeutralKiller()) continue;

            if (role.IsNeutral() && !OptionCanAssingCantKillNeutral.GetBool()) continue;
            if (role.IsMadmate() && !OptionCanAssingMadmate.GetBool()) continue;

            AssingPlayers.Add(pc);
        }

        if (AssingPlayers.Count < 2) return;

        for (var i = 0; i < Sets; i++)
        {
            if (AssingPlayers.Count < 2) break;

            var list = AssingPlayers.OrderBy(x => Guid.NewGuid()).ToArray();
            var pc = list[IRandom.Instance.Next(list.Count())];
            AssingPlayers.Remove(pc);

            var list2 = AssingPlayers.OrderBy(x => Guid.NewGuid()).ToArray();
            var pc2 = list2[IRandom.Instance.Next(list2.Count())];
            AssingPlayers.Remove(pc2);

            if (pc is null || pc2 is null) break;

            TwinsList.Add(pc.PlayerId, pc2.PlayerId);
            TwinsList.Add(pc2.PlayerId, pc.PlayerId);
            PlayerState.GetByPlayerId(pc.PlayerId).SetSubRole(CustomRoles.Twins);
            PlayerState.GetByPlayerId(pc2.PlayerId).SetSubRole(CustomRoles.Twins);

            RpcSetTwins(pc.PlayerId, pc2.PlayerId);

            Logger.Info($"{pc.GetRealName()} & {pc2.GetRealName()}", "Twins");
        }
    }

    #region  Options
    public static OptionItem OptionCanAssingMadmate;
    public static OptionItem OptionCanAssingCantKillNeutral;
    public static OptionItem OptionTwinsDiefollow;
    public static OptionItem OptionTwinsAddWin;
    public static void SetUpTwinsOptions()
    {
        SetupRoleOptions(19200, TabGroup.Combinations, CustomRoles.Twins, new(1, 7, 1));
        OptionCanAssingMadmate = BooleanOptionItem.Create(76110, "CanAssingMadmate", false, TabGroup.Combinations, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Twins]).SetParentRole(CustomRoles.Twins);
        OptionCanAssingCantKillNeutral = BooleanOptionItem.Create(76111, "CanAssingCantKillNeutral", false, TabGroup.Combinations, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Twins]).SetParentRole(CustomRoles.Twins);
        OptionTwinsDiefollow = BooleanOptionItem.Create(76121, "TwinsDiefollow", false, TabGroup.Combinations, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Twins]).SetParentRole(CustomRoles.Twins);
        OptionTwinsAddWin = BooleanOptionItem.Create(76122, "TwinsAddWin", false, TabGroup.Combinations, false)
            .SetParent(CustomRoleSpawnChances[CustomRoles.Twins]).SetParentRole(CustomRoles.Twins);
    }
    #endregion

    public static void CheckAddWin()
    {
        if (!OptionTwinsAddWin.GetBool()) return;
        if (Modules.SuddenDeathMode.NowSuddenDeathMode) return;

        bool flug = false;
        foreach (var twins in TwinsList)
        {
            //キョーセイ負け or 勝利済みなら除外
            if (CustomWinnerHolder.CantWinPlayerIds.Contains(twins.Key) && CustomWinnerHolder.WinnerTeam.IsLovers()) continue;
            if (CustomWinnerHolder.WinnerIds.Contains(twins.Key)) continue;
            if (CustomWinnerHolder.WinnerRoles.Contains(twins.Key.GetPlayerControl()?.GetCustomRole() ?? CustomRoles.Emptiness)) continue;

            //相方が勝利してるなら
            if (CustomWinnerHolder.WinnerIds.Contains(twins.Value) || CustomWinnerHolder.WinnerRoles.Contains(twins.Value.GetPlayerControl()?.GetCustomRole() ?? CustomRoles.Emptiness))
            {   // Id追加して勝利
                Logger.Info($"{twins.Key}:相方勝利に相乗り勝利", "Twins");
                CustomWinnerHolder.WinnerIds.Add(twins.Key);
                if (!flug)
                {
                    flug = true;
                    CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.Twins);
                }
            }
        }
    }
    public static void TwinsReset(byte leftid)
    {
        if (TwinsList.TryGetValue(leftid, out var id))
        {
            TwinsList.Remove(leftid);
            TwinsList.Remove(id);
        }
    }
    public static void TwinsSuicide(bool isExiled = false)
    {
        if (!OptionTwinsDiefollow.GetBool() || !CustomRoles.Twins.IsPresent()) return;
        isExiled |= AntiBlackout.IsCached || GameStates.CalledMeeting || GameStates.ExiledAnimate;
        var list = TwinsList.Where(x => !DieTwinsList.Contains(x.Key));
        foreach (var twins in list)
        {
            var Partner = PlayerCatch.GetPlayerById(twins.Value);
            if (!Partner.IsAlive())
            {
                var twin = PlayerCatch.GetPlayerById(twins.Key);

                if (twin.IsAlive())
                {
                    PlayerState.GetByPlayerId(twins.Key).DeathReason = CustomDeathReason.FollowingSuicide;
                    if (isExiled)
                    {
                        twin.RpcExileV2();
                    }
                    else
                    {
                        twin.RpcMurderPlayerV2(twin);
                    }
                }
                DieTwinsList.Add(twins.Key);
                DieTwinsList.Add(twins.Value);
            }
        }
    }

    public static void RpcSetTwins(byte playerId, byte playerId2)
    {
        using var sender = new SubRoleRPCSender(CustomRoles.Twins, playerId);
        sender.Writer.Write(playerId2);
    }

    public static void ReceiveRPC(MessageReader reader, byte playerId)
    {
        var playerId2 = reader.ReadByte();

        TwinsList.Add(playerId, playerId2);
        TwinsList.Add(playerId2, playerId);
        PlayerState.GetByPlayerId(playerId).SetSubRole(CustomRoles.Twins);
        PlayerState.GetByPlayerId(playerId2).SetSubRole(CustomRoles.Twins);
    }
}