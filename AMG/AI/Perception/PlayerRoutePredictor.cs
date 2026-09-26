using System.Collections.Generic;
using AMG.AI.Navigation;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Perception
{
    public readonly struct RoomPrediction(SystemTypes room, float score, float distance, Waypoint closestWaypoint)
    {
        public SystemTypes Room { get; } = room;
        public float Score { get; } = score;
        public float Distance { get; } = distance;
        public Waypoint ClosestWaypoint { get; } = closestWaypoint;
    }

    public static class PlayerRoutePredictor
    {
        private const float DefaultMaxPlayerSpeed = 2.5f * 1.75f;
        private const float MinTurnScoreMultiplier = 0.3f;

        private const float MinScoreToExpand = 0.02f;

        public static List<RoomPrediction> Predict(
            Waypoint lastSeenWaypoint,
            Vector2 lastKnownDirection,
            float lastKnownSpeed,
            float elapsedSeconds,
            float? maxPlayerSpeedOverride = null)
        {
            var results = new List<RoomPrediction>();
            if (lastSeenWaypoint == null || elapsedSeconds <= 0f) return results;

            float maxSpeed = maxPlayerSpeedOverride ?? DefaultMaxPlayerSpeed;
            float expectedDistance = lastKnownSpeed * elapsedSeconds;
            float maxDistance = Mathf.Max(maxSpeed * elapsedSeconds, expectedDistance);

            if (lastKnownDirection.sqrMagnitude > 0.0001f)
                lastKnownDirection.Normalize();

            var bestScoreAtNode = new Dictionary<Waypoint, float> { [lastSeenWaypoint] = 1f };

            var bestPerRoom = new Dictionary<SystemTypes, RoomPrediction>();

            var frontier = new PriorityQueue<SearchNode, float>();
            frontier.Enqueue(new SearchNode(lastSeenWaypoint, lastKnownDirection, 0f, 1f, isFirstHop: true), 0f);

            while (frontier.Count > 0)
            {
                var node = frontier.Dequeue();

                if (bestScoreAtNode.TryGetValue(node.Waypoint, out var recorded) && recorded > node.Score + 1e-4f)
                    continue;

                RegisterRoomCandidate(bestPerRoom, node);

                if (node.Score < MinScoreToExpand) continue;

                foreach (var neighbor in node.Waypoint.Neighbors)
                {
                    if (neighbor == null) continue;

                    Vector2 edgeDelta = (Vector2)neighbor.Position - (Vector2)node.Waypoint.Position;
                    float edgeLength = edgeDelta.magnitude;
                    if (edgeLength <= 0.0001f) continue;

                    float newDistance = node.Distance + edgeLength;
                    if (newDistance > maxDistance) continue;
                    if (IsBlockedByClosedDoor(node.Waypoint, neighbor)) continue;

                    Vector2 edgeDirection = edgeDelta / edgeLength;

                    float directionScore = node.IsFirstHop
                        ? DirectionAlignmentScore(lastKnownDirection, edgeDirection, strict: true)
                        : DirectionAlignmentScore(node.IncomingDirection, edgeDirection, strict: false);
                    if (directionScore <= 0f) continue;

                    float distanceScore = DistanceDecay(newDistance, expectedDistance, maxDistance);

                    float newScore = node.Score * directionScore * distanceScore;
                    if (newScore < MinScoreToExpand) continue;

                    if (bestScoreAtNode.TryGetValue(neighbor, out var existing) && existing >= newScore)
                        continue;

                    bestScoreAtNode[neighbor] = newScore;
                    frontier.Enqueue(
                        new SearchNode(neighbor, edgeDirection, newDistance, newScore, isFirstHop: false),
                        1f - newScore);
                }
            }

            results.AddRange(bestPerRoom.Values);
            results.Sort((a, b) => b.Score.CompareTo(a.Score));
            return results;
        }

        private static void RegisterRoomCandidate(Dictionary<SystemTypes, RoomPrediction> bestPerRoom, SearchNode node)
        {
            if (node.Waypoint.Room == SystemTypes.Hallway) return;

            if (!bestPerRoom.TryGetValue(node.Waypoint.Room, out var existing) || node.Score > existing.Score)
            {
                bestPerRoom[node.Waypoint.Room] =
                    new RoomPrediction(node.Waypoint.Room, node.Score, node.Distance, node.Waypoint);
            }
        }

        private static float DirectionAlignmentScore(Vector2 fromDirection, Vector2 toDirection, bool strict)
        {
            float dot = Vector2.Dot(fromDirection, toDirection);

            if (strict)
            {
                return dot <= 0f ? 0f : dot;
            }

            float t = (dot + 1f) * 0.5f;
            return Mathf.Lerp(MinTurnScoreMultiplier, 1f, t);
        }

        private static float DistanceDecay(float distance, float expectedDistance, float maxDistance)
        {
            if (distance <= expectedDistance) return 1f;
            if (maxDistance <= expectedDistance) return 1f;

            float t = (distance - expectedDistance) / (maxDistance - expectedDistance); // 0..1
            return Mathf.Clamp01(1f - t);
        }

        private static bool IsBlockedByClosedDoor(Waypoint from, Waypoint to)
        {
            if (from.Room != SystemTypes.Hallway && Utils.IsRoomClosed(from.Room)) return true;
            if (to.Room != SystemTypes.Hallway && Utils.IsRoomClosed(to.Room)) return true;
            return false;
        }

        private readonly struct SearchNode(Waypoint waypoint, Vector2 incomingDirection, float distance, float score, bool isFirstHop)
        {
            public Waypoint Waypoint { get; } = waypoint;
            public Vector2 IncomingDirection { get; } = incomingDirection;
            public float Distance { get; } = distance;
            public float Score { get; } = score;
            public bool IsFirstHop { get; } = isFirstHop;
        }
    }
}