using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Hazel;
using TownOfHost.Attributes;
using TownOfHost.Roles;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Neutral;
using static TownOfHost.Options;

namespace TownOfHost;

class Lovers
{
    public static List<byte> HaveLoverDontTaskPlayers = new();
    public static List<PlayerControl> MaMadonnaLoversPlayers = new();
    public static bool isMadonnaLoversDead = false;
    public static (byte OneLove, byte Ltarget, bool doublelove) OneLovePlayer = new();
    public static bool isOneLoveDead;
    public static OptionItem OneLoveSolowin3players;
    public static OptionItem OneLoveRoleAddwin;
    public static OptionItem OneLoveLoversrect;
    static CustomRoles[] remove =
    {
        CustomRoles.Limiter,
        CustomRoles.Madonna,
        CustomRoles.King
    };
    public static void SetLoversOptions()
    {
        SetupRoleOptions(50370, TabGroup.Combinations, CustomRoles.OneLove, new(1, 1, 1));
        OneLoveRoleAddwin = BooleanOptionItem.Create(73081, "LoversRoleAddwin", false, TabGroup.Combinations, false).SetParent(CustomRoleSpawnChances[CustomRoles.OneLove]).SetParentRole(CustomRoles.OneLove);
        SoloWinOption.Create(73084, TabGroup.Combinations, CustomRoles.OneLove, () => !OneLoveRoleAddwin.GetBool(), defo: 5);
        OneLoveLoversrect = IntegerOptionItem.Create(73082, "OneLoverLovers", new(0, 100, 2), 20, TabGroup.Combinations, false).SetParent(CustomRoleSpawnChances[CustomRoles.OneLove]).SetValueFormat(OptionFormat.Percent).SetParentRole(CustomRoles.OneLove);
        OneLoveSolowin3players = BooleanOptionItem.Create(73083, "LoverSoloWin3players", false, TabGroup.Combinations, false).SetParent(CustomRoleSpawnChances[CustomRoles.OneLove]).SetParentRole(CustomRoles.OneLove);

        new ColorLovers(CustomRoles.Lovers, 50300);
        new ColorLovers(CustomRoles.RedLovers, 50400);
        new ColorLovers(CustomRoles.YellowLovers, 50500);
        new ColorLovers(CustomRoles.BlueLovers, 50600);
        new ColorLovers(CustomRoles.GreenLovers, 50700);
        new ColorLovers(CustomRoles.WhiteLovers, 50800);
        new ColorLovers(CustomRoles.PurpleLovers, 50900);
    }
    [Attributes.GameModuleInitializer]
    public static void Reset()
    {
        ColorLovers.Alldatas.Values.Do(data => data.Reset());
        HaveLoverDontTaskPlayers.Clear();
        MaMadonnaLoversPlayers.Clear();
        OneLovePlayer = (byte.MaxValue, byte.MaxValue, false);
        isOneLoveDead = false;
        isMadonnaLoversDead = false;
    }
    public static void RPCSetLovers(MessageReader reader)
    {
        CustomRoles role = (CustomRoles)reader.ReadInt32();

        if (ColorLovers.Alldatas.TryGetValue(role, out var data))
        {
            for (int i = 0; i < reader.ReadInt32(); i++)
            {
                data.LoverPlayer.Add(PlayerCatch.GetPlayerById(reader.ReadByte()));
            }
        }
    }
    public static void AssignLoversRoles(int RawCount = -1)
    {
        //全部初期化
        OneLovePlayer = (byte.MaxValue, byte.MaxValue, false);

        var allPlayers = new List<PlayerControl>();
        var rand = IRandom.Instance;

        foreach (var player in PlayerCatch.AllPlayerControls)
        {
            if (player.Is(CustomRoles.GM)) continue;
            if (player.Is(CustomRoles.Madonna)) continue;
            if (player.Is(CustomRoles.Limiter)) continue;
            if (player.Is(CustomRoles.King)) continue;
            if (player.Is(CustomRoles.Vega)) continue;
            if (player.Is(CustomRoles.Altair)) continue;
            allPlayers.Add(player);
        }

        ColorLovers.Alldatas.Do(data => data.Value.AssingSetRole());
        ColorLovers.Alldatas.Do(data => data.Value.AssingOther());
        ColorLovers.Alldatas.Do(data => data.Value.AssingCheck());

        allPlayers = allPlayers.Where(x => !x.IsRiaju()).ToList();
        if (CustomRoles.OneLove.IsPresent())
        {
            var count = Math.Clamp(RawCount, 0, allPlayers.Count);
            if (RawCount == -1) count = Math.Clamp(CustomRoles.OneLove.GetRealCount(), 0, allPlayers.Count);
            var assind = false;
            if (allPlayers.Count < 2) return;//2人居ない時は返す。
            if (count <= 0) return;
            var player = allPlayers[rand.Next(0, allPlayers.Count)];//片思いしてる人
            for (var i = 0; i < 2; i++)
            {
                if (assind)
                {
                    var d = false;
                    var p = rand.Next(0, 100);
                    var target = allPlayers[rand.Next(0, allPlayers.Count)];//片思いされてる人
                    if (p <= OneLoveLoversrect.GetInt())
                    {
                        HaveLoverDontTaskPlayers.Add(target.PlayerId);
                        d = true;
                        allPlayers.Remove(target);
                        PlayerState.GetByPlayerId(target.PlayerId).SetSubRole(CustomRoles.OneLove);
                        Logger.Info("両想いだったって！" + target?.Data?.GetLogPlayerName() + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.OneLove.ToString(), "AssignLovers");
                    }

                    Logger.Info($"{player.Data.GetLogPlayerName()} => {target.Data.GetLogPlayerName()} {d}", "OneLover");
                    OneLovePlayer = (player.PlayerId, target.PlayerId, d);
                    break;
                }
                assind = true;
                allPlayers.Remove(player);
                HaveLoverDontTaskPlayers.Add(player.PlayerId);
                PlayerState.GetByPlayerId(player.PlayerId).SetSubRole(CustomRoles.OneLove);
                Logger.Info("役職設定:" + player?.Data?.GetLogPlayerName() + " = " + player.GetCustomRole().ToString() + " + " + CustomRoles.OneLove.ToString(), "AssignLovers");
            }
        }
    }
    public static void OneLoveSuicide(byte deathId = 0x7f, bool isExiled = false)
    {
        if (CustomRoles.OneLove.IsPresent() && isOneLoveDead == false)
        {
            var (Love, target, d) = OneLovePlayer;
            if (Love == byte.MaxValue || target == byte.MaxValue) return;

            if (d)//両片思い
            {
                foreach (var loversPlayer in PlayerCatch.AllPlayerControls.Where(pc => pc.Is(CustomRoles.OneLove)))
                {
                    if (!loversPlayer.Data.IsDead && loversPlayer.PlayerId != deathId) continue;

                    isOneLoveDead = true;
                    foreach (var partnerPlayer in PlayerCatch.AllPlayerControls.Where(pc => pc.Is(CustomRoles.OneLove)))
                    {
                        if (loversPlayer.PlayerId == partnerPlayer.PlayerId) continue;
                        if (partnerPlayer.PlayerId != deathId && !partnerPlayer.Data.IsDead)
                        {
                            PlayerState.GetByPlayerId(partnerPlayer.PlayerId).DeathReason = CustomDeathReason.FollowingSuicide;
                            if (isExiled || GameStates.IsMeeting || AntiBlackout.IsSet)
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
            else//片思い
            {
                var pc = PlayerCatch.GetPlayerById(target);
                var my = PlayerCatch.GetPlayerById(Love);
                if (!pc.Data.IsDead && pc.PlayerId != deathId) return;

                isOneLoveDead = true;
                if (my.PlayerId != deathId && !my.Data.IsDead)
                {
                    PlayerState.GetByPlayerId(my.PlayerId).DeathReason = CustomDeathReason.FollowingSuicide;
                    if (isExiled || GameStates.IsMeeting || AntiBlackout.IsSet)
                    {
                        MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.FollowingSuicide, my.PlayerId);
                        ReportDeadBodyPatch.Musisuruoniku[my.PlayerId] = false;
                    }
                    else
                        my.RpcMurderPlayer(my, true);
                }
            }
        }
    }
    public static void MadonnLoversSuicide(byte deathId = 0x7f, bool isExiled = false)
    {
        if (CustomRoles.Madonna.IsPresent() && isMadonnaLoversDead == false)
        {
            foreach (var MadonnaLoversPlayer in MaMadonnaLoversPlayers)
            {
                if (!MadonnaLoversPlayer.Data.IsDead && MadonnaLoversPlayer.PlayerId != deathId) continue;

                isMadonnaLoversDead = true;
                foreach (var partnerPlayer in MaMadonnaLoversPlayers)
                {
                    if (MadonnaLoversPlayer.PlayerId == partnerPlayer.PlayerId) continue;
                    if (partnerPlayer.PlayerId != deathId && !partnerPlayer.Data.IsDead)
                    {
                        PlayerState.GetByPlayerId(partnerPlayer.PlayerId).DeathReason = CustomDeathReason.FollowingSuicide;
                        if (isExiled || GameStates.IsMeeting || AntiBlackout.IsSet)
                        {
                            MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.FollowingSuicide, partnerPlayer.PlayerId);
                            ReportDeadBodyPatch.Musisuruoniku[MadonnaLoversPlayer.PlayerId] = false;
                        }
                        else
                            partnerPlayer.RpcMurderPlayer(partnerPlayer, true);
                    }
                }
            }
        }
    }
    public static void LoverDisconnected(PlayerControl player)
    {
        var one = PlayerCatch.AllPlayerControls.Where(x => x.Is(CustomRoles.OneLove));
        if (CustomRoles.OneLove.IsPresent() && one.Any())
        {
            if (player.PlayerId == OneLovePlayer.OneLove || player.PlayerId == OneLovePlayer.Ltarget)
            {
                foreach (var pc in one)
                {
                    isOneLoveDead = true;
                    OneLovePlayer = (byte.MaxValue, byte.MaxValue, false);
                    PlayerState.GetByPlayerId(pc.PlayerId).RemoveSubRole(CustomRoles.OneLove);
                }
            }
        }

        if (player.Is(CustomRoles.MadonnaLovers) && !player.Data.IsDead)
        {
            isMadonnaLoversDead = true;
            foreach (var lv in MaMadonnaLoversPlayers)
            {
                lv.GetPlayerState().RemoveSubRole(CustomRoles.MadonnaLovers);
            }
            MaMadonnaLoversPlayers.Clear();
        }

        foreach (var data in ColorLovers.Alldatas.Values)
        {
            data.Disconnected(player);
        }
    }
    public static void LoversSoloWin(ref GameOverReason reason)
    {
        foreach (var data in ColorLovers.Alldatas.Values)
        {
            data.SoloWin(ref reason);
        }

        if (!Madonna.MadonnaLoverAddwin.GetBool() && CustomWinnerHolder.WinnerTeam is CustomWinner.MadonnaLovers && MaMadonnaLoversPlayers.Count > 0 && MaMadonnaLoversPlayers.ToArray().All(p => p.IsAlive()))
        {
            if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.MadonnaLovers, byte.MaxValue, false))
            {
                PlayerCatch.AllPlayerControls
                .Where(p => p.Is(CustomRoles.MadonnaLovers) && p.IsAlive())
                .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));
                reason = GameOverReason.ImpostorsByKill;
            }
        }
        if (CustomRoles.OneLove.IsPresent())
            if (!OneLoveRoleAddwin.GetBool() && CustomWinnerHolder.WinnerTeam is CustomWinner.OneLove && PlayerCatch.GetPlayerById(OneLovePlayer.OneLove).IsAlive() && PlayerCatch.GetPlayerById(OneLovePlayer.Ltarget).IsAlive())
            {
                if (CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.OneLove, byte.MaxValue, false))
                {
                    PlayerCatch.AllPlayerControls
                        .Where(p => p.Is(CustomRoles.OneLove) && p.IsAlive())
                        .Do(p => CustomWinnerHolder.WinnerIds.Add(p.PlayerId));
                    if (!OneLovePlayer.doublelove) CustomWinnerHolder.WinnerIds.Add(OneLovePlayer.Ltarget);//両片思いじゃなかったら追加
                    reason = GameOverReason.ImpostorsByKill;
                }
            }
    }
    public static void LoversAddWin()
    {
        ColorLovers.Alldatas.Do(data => data.Value.AddWin());
        if (CustomWinnerHolder.WinnerTeam != CustomWinner.MadonnaLovers && Madonna.MadonnaLoverAddwin.GetBool() && MaMadonnaLoversPlayers.Count > 0 && MaMadonnaLoversPlayers.ToArray().All(p => p.IsAlive()))
        {
            CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.MadonnaLovers);
            PlayerCatch.AllPlayerControls
                .Where(p => p.Is(CustomRoles.MadonnaLovers) && p.IsAlive())
                .Do(p =>
                {
                    CustomWinnerHolder.WinnerIds.Add(p.PlayerId);
                    CustomWinnerHolder.IdRemoveLovers.Remove(p.PlayerId);
                });
        }
        if (CustomRoles.OneLove.IsPresent())
            if (CustomWinnerHolder.WinnerTeam != CustomWinner.OneLove && OneLoveRoleAddwin.GetBool() && PlayerCatch.GetPlayerById(OneLovePlayer.OneLove).IsAlive() && PlayerCatch.GetPlayerById(OneLovePlayer.Ltarget).IsAlive())
            {
                CustomWinnerHolder.AdditionalWinnerRoles.Add(CustomRoles.OneLove);
                PlayerCatch.AllPlayerControls
                    .Where(p => p.Is(CustomRoles.OneLove) && p.IsAlive())
                .Do(p =>
                {
                    CustomWinnerHolder.WinnerIds.Add(p.PlayerId);
                    CustomWinnerHolder.IdRemoveLovers.Remove(p.PlayerId);
                });
                if (!OneLovePlayer.doublelove)
                {
                    CustomWinnerHolder.WinnerIds.Add(OneLovePlayer.Ltarget);
                    CustomWinnerHolder.IdRemoveLovers.Remove(OneLovePlayer.Ltarget);
                }
            }
    }
    //これに関してはゲーム終了勝利開始だから複数同時発生する訳がない...ハズ。
    public static bool CheckPlayercountWin()
    {
        foreach (var data in ColorLovers.Alldatas.Values)
        {
            if (data.CheckCountWin()) return true;
        }
        if ((PlayerCatch.AllAlivePlayersCount <= 2 && PlayerCatch.AllAlivePlayerControls.All(pc => pc.PlayerId == Lovers.OneLovePlayer.Ltarget || pc.PlayerId == Lovers.OneLovePlayer.OneLove))
        || (Lovers.OneLoveSolowin3players.GetBool() && PlayerCatch.AllAlivePlayersCount <= 3 && PlayerCatch.GetPlayerById(Lovers.OneLovePlayer.OneLove)?.IsAlive() == true && PlayerCatch.GetPlayerById(Lovers.OneLovePlayer.Ltarget)?.IsAlive() == true))
        {
            CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.OneLove, byte.MaxValue);
            if (!Lovers.OneLovePlayer.doublelove) CustomWinnerHolder.WinnerIds.Add(Lovers.OneLovePlayer.Ltarget);
            return true;
        }
        if (PlayerCatch.AllAlivePlayerControls.All(p => p.Is(CustomRoles.MadonnaLovers)) ||
        (Madonna.MaLoversSolowin3players.GetBool() && PlayerCatch.AllAlivePlayersCount <= 3 && Lovers.MaMadonnaLoversPlayers.Count != 0 && Lovers.MaMadonnaLoversPlayers.All(pc => pc.IsAlive())))
        {
            CustomWinnerHolder.ResetAndSetAndChWinner(CustomWinner.MadonnaLovers, byte.MaxValue);
            return true;
        }
        return false;
    }
}
