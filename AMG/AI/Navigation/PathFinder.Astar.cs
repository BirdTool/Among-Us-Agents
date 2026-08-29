using System.Collections.Generic;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Navigation
{
    public static partial class Pathfinder
    {
        public static List<Waypoint> FindAlgorithPath(Waypoint startNode, Waypoint targetNode, out float totalDistance)
        {
            totalDistance = 0f;

            if (startNode == null || targetNode == null) return null;

            // ── Cache lookup ─────────────────────────────────────────────────
            var cacheKey = (startNode, targetNode);
            if (_pathCache.TryGetValue(cacheKey, out var cached) && !cached.IsExpired)
            {
                totalDistance = cached.TotalDistance;
                // Return a copy so callers can't mutate the cached list
                return cached.Path != null ? [.. cached.Path] : null;
            }

            // ── A* search ────────────────────────────────────────────────────
            var cameFrom = new Dictionary<Waypoint, Waypoint>();
            var gScore = new Dictionary<Waypoint, float> { [startNode] = 0f };
            var closedSet = new HashSet<Waypoint>();

            var openSet = new PriorityQueue<Waypoint, float>();
            openSet.Enqueue(startNode, Vector2.Distance(startNode.Position, targetNode.Position));

            int emergencyBreak = 0;
            while (openSet.Count > 0 && emergencyBreak < 5000)
            {
                emergencyBreak++;

                Waypoint current = openSet.Dequeue();

                if (!closedSet.Add(current)) continue;

                if (current == targetNode)
                {
                    totalDistance = gScore[current];

                    List<Waypoint> path = [current];
                    while (cameFrom.ContainsKey(current))
                    {
                        current = cameFrom[current];
                        path.Add(current);
                    }
                    path.Reverse();

                    // Store in cache (both directions since the graph is undirected)
                    var entry = new CachedPath(path, totalDistance);
                    _pathCache[cacheKey] = entry;
                    _pathCache[(targetNode, startNode)] = entry;

                    return [.. path];
                }

                foreach (var neighbor in current.Neighbors)
                {
                    if (closedSet.Contains(neighbor)) continue;

                    if (current.Room != neighbor.Room)
                    {
                        if (Utils.IsRoomClosed(current.Room)) continue;
                        if (Utils.IsRoomClosed(neighbor.Room)) continue;
                    }

                    float tentativeGScore = gScore[current] + Vector2.Distance(current.Position, neighbor.Position);

                    if (gScore.TryGetValue(neighbor, out float existingGScore) && tentativeGScore >= existingGScore)
                        continue;

                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    float neighborFScore = tentativeGScore + Vector2.Distance(neighbor.Position, targetNode.Position);
                    openSet.Enqueue(neighbor, neighborFScore);
                }
            }

            // Cache the null result too — prevents hammering a blocked graph
            _pathCache[cacheKey] = new CachedPath(null, 0f);

            LogManager.LogWarning("[AI GPS] Não foi possível conectar esses dois pontos no grafo.");
            return null;
        }
    }
}