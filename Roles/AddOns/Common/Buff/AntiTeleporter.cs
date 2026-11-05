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
                MeetingStartPositions[pc.PlayerId] = pc.transform.position;
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
            pc.RpcSnapToForced(GetSafeReturnPosition(position));
        }

        public static Vector2 GetSafeReturnPosition(Vector2 basePosition)
        {
            var start = PushAwayFromMeetingTable(basePosition);
            foreach (var candidate in EnumerateCandidatePositions(start))
            {
                if (!IsBlocked(candidate)) return candidate;
            }

            return start + Vector2.down * 0.9f;
        }

        private static Vector2 PushAwayFromMeetingTable(Vector2 position)
        {
            if (!TryGetMeetingTable(out var center, out var radius)) return position;
            var offset = position - center;
            if (offset.magnitude >= radius) return position;
            if (offset.sqrMagnitude < 0.0001f) offset = Vector2.down;
            return center + offset.normalized * radius;
        }

        private static bool TryGetMeetingTable(out Vector2 center, out float radius)
        {
            center = default;
            radius = 0f;
            var button = ShipStatus.Instance?.EmergencyButton;
            if (button == null) return false;

            center = button.transform.position;
            radius = 1.35f;

            Bounds? bounds = null;
            foreach (var collider in button.GetComponentsInChildren<Collider2D>(true))
            {
                if (collider == null || !collider.enabled) continue;
                if (bounds == null) bounds = collider.bounds;
                else
                {
                    var b = bounds.Value;
                    b.Encapsulate(collider.bounds);
                    bounds = b;
                }
            }
            if (bounds != null)
            {
                center = bounds.Value.center;
                radius = Mathf.Max(radius, Mathf.Max(bounds.Value.extents.x, bounds.Value.extents.y) + 0.4f);
                radius = Mathf.Min(radius, 1.8f);
            }
            return true;
        }

        private static IEnumerable<Vector2> EnumerateCandidatePositions(Vector2 basePosition)
        {
            yield return basePosition;

            var directions = new[]
            {
                Vector2.down,
                Vector2.up,
                Vector2.right,
                Vector2.left,
                new Vector2(1f, 1f).normalized,
                new Vector2(-1f, 1f).normalized,
                new Vector2(1f, -1f).normalized,
                new Vector2(-1f, -1f).normalized,
            };

            var radii = new[] { 0.25f, 0.45f, 0.7f, 1.0f, 1.3f, 1.6f };
            foreach (var radius in radii)
            {
                foreach (var direction in directions)
                {
                    yield return basePosition + (direction * radius);
                }
            }
        }

        private static bool IsBlocked(Vector2 position)
        {
            const float playerRadius = 0.28f;
            foreach (var collider in Physics2D.OverlapCircleAll(position, playerRadius, Constants.ShipAndAllObjectsMask))
            {
                if (collider == null || !collider.enabled) continue;
                if (collider.isTrigger) continue;
                if (collider.GetComponentInParent<PlayerControl>() != null) continue;
                if (collider.GetComponentInParent<DeadBody>() != null) continue;
                if (collider.GetComponentInParent<Vent>() != null) continue;
                return true;
            }
            return false;
        }

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
