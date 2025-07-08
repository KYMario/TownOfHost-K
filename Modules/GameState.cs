using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Crewmate;

namespace TownOfHost
{
    public class PlayerState
    {
        byte PlayerId;
        public CustomRoles MainRole;
        public List<CustomRoles> SubRoles;
        public CustomRoles GhostRole;
        public CountTypes CountType { get; private set; }
        public bool IsDead { get; set; }
        public CustomDeathReason DeathReason { get; set; }
        public TaskState taskState;
        public bool IsBlackOut { get; set; }
        private bool _canUseMovingPlatform = true;
        public string KillRoom;
        public bool CanUseMovingPlatform
        {
            get => _canUseMovingPlatform;
            set
            {
                Logger.Info($"ID: {PlayerId} の昇降機可用性を {value} に設定", nameof(PlayerState));
                _canUseMovingPlatform = value;
            }
        }
        public (DateTime dateTime, byte killerid) RealKiller;
        public PlainShipRoom LastRoom;
        /// <summary>会議等の後に湧いた後かどうか<br/>ホスト以外は正しい値にならないので注意</summary>
        public bool HasSpawned { get; set; } = false;
        public Dictionary<byte, string> TargetColorData;
        public float NumberOfRemainingButtons = 0;
        public PlayerState(byte playerId)
        {
            MainRole = CustomRoles.NotAssigned;
            SubRoles = new();
            GhostRole = CustomRoles.NotAssigned;
            CountType = CountTypes.OutOfGame;
            PlayerId = playerId;
            IsDead = false;
            DeathReason = CustomDeathReason.etc;
            taskState = new();
            IsBlackOut = false;
            RealKiller = (DateTime.MinValue, byte.MaxValue);
            LastRoom = null;
            TargetColorData = new();
            NumberOfRemainingButtons = Main.NormalOptions.NumEmergencyMeetings;
            KillRoom = "";
        }
        public CustomRoles GetCustomRole()
        {
            var RoleInfo = PlayerCatch.GetPlayerInfoById(PlayerId);
            return RoleInfo.Role == null
                ? MainRole
                : RoleInfo.Role.Role switch
                {
                    RoleTypes.Crewmate => CustomRoles.Crewmate,
                    RoleTypes.Engineer => CustomRoles.Engineer,
                    RoleTypes.Scientist => CustomRoles.Scientist,
                    RoleTypes.Tracker => CustomRoles.Tracker,
                    RoleTypes.Noisemaker => CustomRoles.Noisemaker,
                    RoleTypes.GuardianAngel => CustomRoles.GuardianAngel,
                    RoleTypes.Impostor => CustomRoles.Impostor,
                    RoleTypes.Shapeshifter => CustomRoles.Shapeshifter,
                    RoleTypes.Phantom => CustomRoles.Phantom,
                    _ => CustomRoles.Crewmate,
                };
        }
        public void SetMainRole(CustomRoles role, [System.Runtime.CompilerServices.CallerMemberName] string name = "")
        {
            MainRole = role;

            CountType = CustomRoleManager.GetRoleInfo(role) is SimpleRoleInfo roleInfo ?
                roleInfo.CountType :
                role switch
                {
                    CustomRoles.GM => CountTypes.OutOfGame,
                    CustomRoles.HASFox or
                    CustomRoles.HASTroll => CountTypes.None,
                    CustomRoles.TaskPlayerB => CountTypes.TaskPlayer,
                    _ => role.IsImpostor() ? CountTypes.Impostor : CountTypes.Crew,
                };
        }
        public void SetSubRole(CustomRoles role, bool AllReplace = false)
        {
            if (AllReplace)
                SubRoles.ToArray().Do(role => SubRoles.Remove(role));

            if (!SubRoles.Contains(role))
            {
                SubRoles.Add(role);
                CustomRoleManager.OtherRolesAdd(PlayerCatch.GetPlayerById(PlayerId), role);
            }
        }
        public void RemoveSubRole(CustomRoles role)
        {
            if (SubRoles.Contains(role))
                SubRoles.Remove(role);
        }
        public void SetGhostRole(CustomRoles role) => GhostRole = role;
        public void SetDead()
        {
            IsDead = true;
            if (AmongUsClient.Instance.AmHost)
            {
                RPC.SendDeathReason(PlayerId, DeathReason);
            }
        }
        public bool IsSuicide() { return DeathReason == CustomDeathReason.Suicide; }
        public TaskState GetTaskState() { return taskState; }
        public void InitTask(PlayerControl player)
        {
            taskState.Init(player);
        }
        public void UpdateTask(PlayerControl player)
        {
            taskState.Update(player);
        }

        public byte GetRealKiller()
            => IsDead && RealKiller.Item1 != DateTime.MinValue ? RealKiller.Item2 : byte.MaxValue;
        public int GetKillCount(bool ExcludeSelfKill = false)
        {
            int count = 0;
            foreach (var state in AllPlayerStates.Values)
                if (!(ExcludeSelfKill && state.PlayerId == PlayerId) && state.GetRealKiller() == PlayerId)
                    count++;
            return count;
        }
        public void SetCountType(CountTypes countType) => CountType = countType;

        private static Dictionary<byte, PlayerState> allPlayerStates = new(15);
        public static IReadOnlyDictionary<byte, PlayerState> AllPlayerStates => allPlayerStates;

        public static PlayerState GetByPlayerId(byte playerId) => AllPlayerStates.TryGetValue(playerId, out var state) ? state : null;
        public static void Clear() => allPlayerStates.Clear();
        public static void Create(byte playerId)
        {
            if (allPlayerStates.ContainsKey(playerId))
            {
                Logger.Warn($"重複したIDのPlayerStateが作成されました: {playerId}", nameof(PlayerState));
                return;
            }
            allPlayerStates[playerId] = new(playerId);
        }
    }
    public class TaskState
    {
        public static int InitialTotalTasks;
        public int AllTasksCount;
        public int NeedTaskCount;//必要なタスク数。マッドスニッチ等
        public int CompletedTasksCount;
        public bool hasTasks;
        public int RemainingTasksCount => AllTasksCount - CompletedTasksCount;
        public bool DoExpose => RemainingTasksCount <= Options.SnitchExposeTaskLeft && hasTasks;
        public bool IsTaskFinished => RemainingTasksCount <= 0 && hasTasks;
        public TaskState()
        {
            this.AllTasksCount = -1;
            this.NeedTaskCount = -1;
            this.CompletedTasksCount = 0;
            this.hasTasks = false;
        }

        public void Init(PlayerControl player)
        {
            Logger.Info($"{player.GetNameWithRole().RemoveHtmlTags()}: InitTask", "TaskState.Init");
            if (player == null || player.Data == null || player.Data.Tasks == null) return;
            if (!UtilsTask.HasTasks(player.Data, false))
            {
                AllTasksCount = 0;
                NeedTaskCount = 0;
                return;
            }
            hasTasks = true;
            AllTasksCount = player.Data.Tasks.Count;
            if (player.Is(CustomRoles.Walker))
            {
                AllTasksCount += Walker.WalkTaskCount.GetInt();
            }
            Logger.Info($"{player.GetNameWithRole().RemoveHtmlTags()}: TaskCounts = {CompletedTasksCount}/{AllTasksCount}", "TaskState.Init");
        }
        public void Update(PlayerControl player)
        {
            Logger.Info($"{player.GetNameWithRole().RemoveHtmlTags()}: UpdateTask", "TaskState.Update");
            GameData.Instance.RecomputeTaskCounts();
            //PlayerControl.CompleteTask Prefixから呼ばれるのでGameDataとは1ずれている
            Logger.Info($"TotalTaskCounts = {GameData.Instance.CompletedTasks + 1}/{GameData.Instance.TotalTasks}", "TaskState.Update");

            //初期化出来ていなかったら初期化
            if (AllTasksCount == -1) Init(player);

            if (!hasTasks) return;

            //クリアしてたらカウントしない
            if (CompletedTasksCount >= AllTasksCount) return;

            CompletedTasksCount++;

            //調整後のタスク量までしか表示しない
            CompletedTasksCount = Math.Min(AllTasksCount, CompletedTasksCount);
            Logger.Info($"{player.GetNameWithRole().RemoveHtmlTags()}: TaskCounts = {CompletedTasksCount}/{AllTasksCount}", "TaskState.Update");
        }
        public int GetNeedCountOrAll() => NeedTaskCount < 0 ? AllTasksCount : NeedTaskCount;
        public bool HasCompletedEnoughCountOfTasks(float count) => HasCompletedEnoughCountOfTasks((int)count);
        public bool HasCompletedEnoughCountOfTasks(int count) => IsTaskFinished || CompletedTasksCount >= count;
    }
    public class PlayerVersion
    {
        public readonly Version version;
        public readonly string tag;
        public readonly string forkId;
        [Obsolete] public PlayerVersion(string ver, string tag_str) : this(Version.Parse(ver), tag_str, "") { }
        [Obsolete] public PlayerVersion(Version ver, string tag_str) : this(ver, tag_str, "") { }
        public PlayerVersion(string ver, string tag_str, string forkId) : this(Version.Parse(ver), tag_str, forkId) { }
        public PlayerVersion(Version ver, string tag_str, string forkId)
        {
            version = ver;
            tag = tag_str;
            this.forkId = forkId;
        }
        public bool IsEqual(PlayerVersion pv)
        {
            return pv.version == version && pv.tag == tag;
        }
    }
    public static class GameStates
    {
        [Attributes.GameModuleInitializer]
        public static void Reset()
        {
            MeetingStates.Sending = false;
            MeetingStates.MeetingCalled = false;
            MeetingStates.FirstMeeting = true;
            MeetingStates.First = true;
            AlreadyDied = false;
            Intro = true;
            task = false;
            AfterIntro = false;
            Meeting = false;
            Tuihou = false;
            canmusic = false;
        }
        public static bool InGame = false;
        public static bool IsOutro = false;
        public static bool AlreadyDied = false;
        public static bool IsModHost => PlayerControl.AllPlayerControls.ToArray().FirstOrDefault(x => x.PlayerId == 0 && x.IsModClient());
        public static bool IsLobby => AmongUsClient.Instance.GameState == AmongUsClient.GameStates.Joined;
        public static bool IsInGame => InGame;
        public static bool IsEnded => AmongUsClient.Instance.GameState == AmongUsClient.GameStates.Ended;
        public static bool IsNotJoined => AmongUsClient.Instance.GameState == AmongUsClient.GameStates.NotJoined;
        public static bool IsOnlineGame => AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame;
        public static bool IsLocalGame => AmongUsClient.Instance.NetworkMode == NetworkModes.LocalGame;
        public static bool IsFreePlay => AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay;
        public static bool IsInTask => task;
        public static bool Meeting;
        public static bool Tuihou;
        public static bool IsMeeting => InGame && MeetingHud.Instance;
        public static bool IsCountDown => GameStartManager.InstanceExists && GameStartManager.Instance.startState == GameStartManager.StartingStates.Countdown;
        public static bool Intro;
        public static bool AfterIntro;
        public static bool task;
        public static bool canmusic;
        public static bool introDestroyed = false;
    }
    public static class MeetingStates
    {
        public static bool Sending;
        public static DeadBody[] DeadBodies = null;
        public static NetworkedPlayerInfo ReportTarget = null;
        public static bool IsEmergencyMeeting => ReportTarget == null;
        public static bool IsExistDeadBody => DeadBodies.Length > 0;
        public static bool MeetingCalled = false;
        public static bool FirstMeeting = true;
        public static bool First = true;
    }
}