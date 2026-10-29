using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Patches;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class AntiTeleporter
    {
        private static readonly int Id = 25700;
        private static readonly Color RoleColor = UtilsRoleText.GetRoleColor(CustomRoles.AntiTeleporter);
        public static string SubRoleMark = Utils.ColorString(RoleColor, "＠");
        private static Dictionary<byte, Vector2> MeetingStartPositions = new();
        private static Dictionary<byte, Vector2> PendingAirshipReturns = new();

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.AntiTeleporter, fromtext: UtilsOption.GetFrom(From.RevolutionaryHostRoles));
            AddOnsAssignData.Create(Id + 10, CustomRoles.AntiTeleporter, true, true, true, true);
        }

        public static void Init()
        {
            MeetingStartPositions = new();
            PendingAirshipReturns = new();
        }

        public static bool TryConsumePendingReturn(byte playerId, out Vector2 position)
        {
            if (PendingAirshipReturns.Remove(playerId, out position)) return true;
            position = default;
            return false;
        }

        public static void OnMeetingCalled()
        {
            if (!AmongUsClient.Instance.AmHost) return;

            MeetingStartPositions.Clear();
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                if (pc == null || !pc.Is(CustomRoles.AntiTeleporter)) continue;
                if (IsUsingUnsafeTransport(pc)) continue;
                MeetingStartPositions[pc.PlayerId] = pc.GetTruePosition();
            }
        }

        public static void AfterMeetingTasks()
        {
            if (!AmongUsClient.Instance.AmHost || MeetingStartPositions.Count == 0) return;

            var positions = new Dictionary<byte, Vector2>(MeetingStartPositions);
            MeetingStartPositions.Clear();

            // エアシップは湧き選択後に戻す（通常テレポートは湧き位置で上書きされる）
            if ((MapNames)Main.NormalOptions.MapId == MapNames.Airship)
            {
                PendingAirshipReturns = positions;
                _ = new LateTask(ForcePendingReturns, 15f, "AntiTeleporterAirshipFallback", true);
                return;
            }

            ScheduleReturn(positions);
        }

        private static void ScheduleReturn(Dictionary<byte, Vector2> positions)
        {
            _ = new LateTask(() =>
            {
                foreach (var (playerId, position) in positions)
                {
                    var pc = PlayerCatch.GetPlayerById(playerId);
                    if (pc == null || !pc.IsAlive()) continue;
                    ReturnPlayer(pc, position);
                }
            }, 0.8f, "AntiTeleporterReturn", true);
        }

        private static void ForcePendingReturns()
        {
            if (PendingAirshipReturns.Count == 0) return;

            var positions = new Dictionary<byte, Vector2>(PendingAirshipReturns);
            PendingAirshipReturns.Clear();
            foreach (var (playerId, position) in positions)
            {
                var pc = PlayerCatch.GetPlayerById(playerId);
                if (pc == null || !pc.IsAlive()) continue;
                ReturnPlayer(pc, position);
                PlayerState.GetByPlayerId(playerId).HasSpawned = true;
            }
        }

        private static void ReturnPlayer(PlayerControl pc, Vector2 position)
        {
            var safePosition = GetSafeReturnPosition(position);
            pc.RpcSnapToForced(safePosition);
        }

        private static Vector2 GetSafeReturnPosition(Vector2 basePosition)
        {
            foreach (var candidate in EnumerateCandidatePositions(basePosition))
            {
                if (!IsBlocked(candidate)) return candidate;
            }

            return basePosition + new Vector2(0f, 0.75f);
        }

        private static IEnumerable<Vector2> EnumerateCandidatePositions(Vector2 basePosition)
        {
            yield return basePosition + new Vector2(0f, 0.1f);

            var directions = new[]
            {
                Vector2.up,
                Vector2.right,
                Vector2.left,
                Vector2.down,
                new Vector2(1f, 1f).normalized,
                new Vector2(-1f, 1f).normalized,
                new Vector2(1f, -1f).normalized,
                new Vector2(-1f, -1f).normalized,
            };

            var radii = new[] { 0.2f, 0.35f, 0.5f, 0.65f, 0.8f };
            foreach (var radius in radii)
            {
                foreach (var direction in directions)
                {
                    yield return basePosition + new Vector2(0f, 0.1f) + (direction * radius);
                }
            }
        }

        private static bool IsBlocked(Vector2 position)
            => Physics2D.OverlapCircle(position, 0.18f, Constants.ShipAndAllObjectsMask) != null;

        private static bool IsUsingUnsafeTransport(PlayerControl pc)
        {
            if (pc?.MyPhysics?.Animations?.IsPlayingAnyLadderAnimation() == true) return true;
            if (pc?.inMovingPlat == true) return true;
            if (pc != null && PlayerControlRpcUseZiplinePatch.IsUsingZipline(pc)) return true;
            if (pc != null && pc.UseMovingPlatform()) return true;
            return false;
        }
    }
}
