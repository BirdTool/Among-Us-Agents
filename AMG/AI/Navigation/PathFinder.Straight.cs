using System.Collections.Generic;
using AMG.Enums.GameEnums;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Navigation
{
    public static partial class Pathfinder
    {
        private const float DefaultAgentRadius = 0.35f;
        private const float StraightPathStep = 0.5f;

        private static readonly int MovementMask =
            (1 << (int)LayersEnum.Ship) | (1 << (int)LayersEnum.Objects) | (1 << (int)LayersEnum.ShortObjects);

        public static List<Waypoint> FindStraightPath(
            Vector2 start,
            Vector2 end,
            out float totalDistance,
            bool returnNullIfCantProgress = false,
            float agentRadius = DefaultAgentRadius)
        {
            Vector2 delta = end - start;
            float maxDistance = delta.magnitude;
            Vector2 direction = maxDistance > 0f ? delta / maxDistance : Vector2.zero;

            Vector2 raisedOrigin = new(start.x, start.y + 0.1f);

            var hit = CircleCastIgnoringTriggers(raisedOrigin, agentRadius, direction, maxDistance, MovementMask);

            float physicalDistance = maxDistance;
            if (hit.HasValue)
            {
                physicalDistance = Mathf.Max(0f, hit.Value.distance - 0.1f);
            }

            var path = new List<Waypoint>();
            SystemTypes previousRoom = GetRoomAtPosition(start);
            float actualDistance = 0f;

            int steps = Mathf.Max(1, Mathf.CeilToInt(physicalDistance / StraightPathStep));

            for (int i = 1; i <= steps; i++)
            {
                float t = Mathf.Min(i * StraightPathStep, physicalDistance);
                Vector2 pos = start + direction * t;
                SystemTypes room = GetRoomAtPosition(pos);

                if (room != previousRoom && (Utils.IsRoomClosed(previousRoom) || Utils.IsRoomClosed(room)))
                    break;

                path.Add(new Waypoint { Position = pos, Room = room });
                actualDistance = t;
                previousRoom = room;

                if (t >= physicalDistance) break;
            }

            bool blocked = hit.HasValue && actualDistance < maxDistance - 0.05f;
            if (blocked && returnNullIfCantProgress)
            {
                totalDistance = 0f;
                return null;
            }

            totalDistance = actualDistance;
            return path;
        }

        public static List<Waypoint> FindStraightPath(
            Waypoint startNode,
            Waypoint targetNode,
            out float totalDistance,
            bool returnNullIfCantProgress = false,
            float agentRadius = DefaultAgentRadius)
        {
            totalDistance = 0f;
            if (startNode == null || targetNode == null) return null;

            return FindStraightPath(startNode.Position, targetNode.Position, out totalDistance, returnNullIfCantProgress, agentRadius);
        }

        private static RaycastHit2D? CircleCastIgnoringTriggers(Vector2 origin, float radius, Vector2 direction, float maxDistance, int mask)
        {
            var hits = Physics2D.CircleCastAll(origin, radius, direction, maxDistance, mask);

            RaycastHit2D? closest = null;
            foreach (var h in hits)
            {
                if (h.collider == null || h.collider.isTrigger) continue;
                if (closest == null || h.distance < closest.Value.distance)
                    closest = h;
            }
            return closest;
        }

        private static SystemTypes GetRoomAtPosition(Vector2 pos)
        {
            if (ShipStatus.Instance == null || ShipStatus.Instance.AllRooms == null) return SystemTypes.Hallway;

            foreach (var room in ShipStatus.Instance.AllRooms)
            {
                if (room.roomArea != null && room.roomArea.OverlapPoint(pos))
                    return room.RoomId;
            }

            return SystemTypes.Hallway;
        }
    }
}