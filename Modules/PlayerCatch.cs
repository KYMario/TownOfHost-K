using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.AddOns.Impostor;
using TownOfHost.Roles.AddOns.Neutral;
using TownOfHost.Roles.AddOns.Common;

namespace TownOfHost
{
    public static class PlayerCatch
    {
        private static Dictionary<byte, PlayerControl> cachedPlayers = new(15);
        public static PlayerControl GetPlayerControl(this byte playerId) => GetPlayerById(playerId);
        public static PlayerControl GetPlayerById(int playerId) => GetPlayerById((byte)playerId);
        public static PlayerControl GetPlayerById(byte playerId)
        {
            if (cachedPlayers.TryGetValue(playerId, out var cachedPlayer) && cachedPlayer != null)
            {
                return cachedPlayer;
            }
            var player = PlayerCatch.AllPlayerControls.Where(pc => pc.PlayerId == playerId).FirstOrDefault();
            cachedPlayers[playerId] = player;
            return player;
        }
        public static TaskState GetPlayerTaskState(this PlayerControl player)
        {
            return PlayerState.GetByPlayerId(player.PlayerId).GetTaskState();
        }
        public static PlayerState GetPlayerState(this PlayerControl player)
        {
            return PlayerState.GetByPlayerId(player.PlayerId);
        }
        public static PlayerState GetPlayerState(this byte player)
        {
            return PlayerState.GetByPlayerId(player);
        }

        public static NetworkedPlayerInfo GetPlayerInfoById(int PlayerId) =>
            GameData.Instance.AllPlayers.ToArray().Where(info => info.PlayerId == PlayerId).FirstOrDefault();

        public static string GetLogPlayerName(this NetworkedPlayerInfo info)
        {
            if (info.Trygetname().Contains("<"))
            {
                return info.name.RemoveDeltext("Data");
            }
            return info.Trygetname();
        }
        static string Trygetname(this NetworkedPlayerInfo info)
        {
            if (info == null) return "";
            if (GameStates.IsLobby)
            {
                var pc = info?.Object?.GetClient()?.PlayerName;
                return pc == null ? info?.PlayerName : pc;
            }
            if (Camouflage.PlayerSkins.TryGetValue(info.PlayerId, out var cos)) return cos.PlayerName;

            return info?.PlayerName;
        }

        public static void CountAlivePlayers(bool sendLog = false)
        {
            var playerControls = AllAlivePlayerControls;
            int AliveImpostorCount = playerControls.Count(pc => pc.Is(CustomRoleTypes.Impostor));
            int AliveNeutalCount = playerControls.Count(pc => pc.Is(CustomRoleTypes.Neutral));

            if (PlayerCatch.AliveImpostorCount != AliveImpostorCount)
            {
                Logger.Info("生存しているインポスター:" + AliveImpostorCount + "人", "CountAliveImpostors");
                PlayerCatch.AliveImpostorCount = AliveImpostorCount;
                LastImpostor.SetSubRole();
                Amnesia.CheckImpostorCount();
            }
            if (PlayerCatch.AliveNeutalCount != AliveNeutalCount)
            {
                Logger.Info("生存しているニュートラル:" + AliveNeutalCount + "人", "CountAliveNeutral");
                PlayerCatch.AliveNeutalCount = AliveNeutalCount;
                LastNeutral.SetSubRole();
            }

            if (sendLog)
            {
                if (Options.CantUseVentMode.GetBool() && (playerControls.Count() <= Options.CantUseVentTrueCount.GetFloat()))
                    Utils.CantUseVent = true;
                else Utils.CantUseVent = false;

                var sb = new StringBuilder(100);
                foreach (var countTypes in EnumHelper.GetAllValues<CountTypes>())
                {
                    var playersCount = PlayersCount(countTypes);
                    if (playersCount == 0) continue;
                    sb.Append($"{countTypes}:{AlivePlayersCount(countTypes)}/{playersCount}, ");
                }
                sb.Append($"All:{AllAlivePlayersCount}/{AllPlayersCount}");
                Logger.Info(sb.ToString(), "CountAlivePlayers");
            }
        }
        public static int AliveImpostorCount;
        public static int AliveNeutalCount;
        public static int SKMadmateNowCount;
        public static int AllPlayersCount => PlayerState.AllPlayerStates.Values.Count(state => state.CountType != CountTypes.OutOfGame);
        public static int AllAlivePlayersCount => AllAlivePlayerControls.Count(pc => !pc.Is(CountTypes.OutOfGame));
        public static bool IsAllAlive => PlayerState.AllPlayerStates.Values.All(state => state.CountType == CountTypes.OutOfGame || !state.IsDead);
        public static int PlayersCount(CountTypes countTypes) => PlayerState.AllPlayerStates.Values.Count(state => state.CountType == countTypes);
        public static int AlivePlayersCount(CountTypes countTypes) => AllAlivePlayerControls.Count(pc => pc.Is(countTypes));
        public static Dictionary<byte, CustomRoleTypes> AllPlayerFirstTypes = new();
        public static IEnumerable<PlayerControl> AllPlayerControls => PlayerControl.AllPlayerControls.ToArray().Where(p => p != null && p.PlayerId <= 15);
        public static IEnumerable<PlayerControl> AllAlivePlayerControls => PlayerControl.AllPlayerControls.ToArray().Where(p => p != null && p.IsAlive() && p.PlayerId <= 15);
        //1ターン前に生きてた人達のリスト
        public static List<PlayerControl> OldAlivePlayerControles = new();
        public static Dictionary<byte, uint> AllPlayerNetId = new();

        public static bool CachedAnyModClient { get; private set; }
        public static bool AnyModClient() => GameStates.IsInGame ? CachedAnyModClient : CachedAnyModClient = AllPlayerControls.Any(pc => pc.PlayerId is not 0 && pc.IsModClient());
    }
}