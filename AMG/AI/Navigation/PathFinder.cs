using System.Collections.Generic;
using UnityEngine;
using AMG.Utilities;


namespace AMG.AI.Navigation
{
    public static class Pathfinder
    {
        // ── Path cache ───────────────────────────────────────────────────────
        // Shared across all agents. Invalidated when any door opens/closes.
        // Also expires after PATH_CACHE_TTL seconds as a safety net.
        private const float PATH_CACHE_TTL = 5f;

        private readonly struct CachedPath
        {
            public readonly List<Waypoint> Path;
            public readonly float TotalDistance;
            public readonly float CachedAt;

            public CachedPath(List<Waypoint> path, float dist)
            {
                Path = path;
                TotalDistance = dist;
                CachedAt = Time.time;
            }

            public bool IsExpired => Time.time - CachedAt > PATH_CACHE_TTL;
        }

        private static readonly Dictionary<(Waypoint, Waypoint), CachedPath> _pathCache = [];

        /// <summary>Wires the cache invalidation to door-state events. Called once on startup.</summary>
        public static void Initialize()
        {
            Utils.OnDoorStateChanged += InvalidateCache;
        }

        public static void InvalidateCache()
        {
            _pathCache.Clear();
            LogManager.LogDebug("[AI GPS] Cache de caminhos invalidado (estado de porta alterado).");
        }

        public static Waypoint GetClosestNode(Vector2 pos)
        {
            Waypoint best = null;
            float minDist = float.MaxValue;
            foreach (var wp in WaypointManager.AllWaypoints)
            {
                float dist = Vector2.Distance(pos, wp.Position);
                if (dist < minDist)
                {
                    minDist = dist;
                    best = wp;
                }
            }
            return best;
        }

        public static Waypoint GetClosestNode(Vector2 pos, float maxDistance)
        {
            Waypoint best = null;
            float minDist = maxDistance;
            foreach (var wp in WaypointManager.AllWaypoints)
            {
                float dist = Vector2.Distance(pos, wp.Position);
                if (dist < minDist)
                {
                    minDist = dist;
                    best = wp;
                }
            }
            return best;
        }

        public static List<Waypoint> FindPath(Waypoint startNode, Waypoint targetNode, out float totalDistance)
        {
            totalDistance = 0f;

            if (startNode == null || targetNode == null) return null;

            // ── Cache lookup ─────────────────────────────────────────────────
            var cacheKey = (startNode, targetNode);
            if (_pathCache.TryGetValue(cacheKey, out var cached) && !cached.IsExpired)
            {
                totalDistance = cached.TotalDistance;
                // Return a copy so callers can't mutate the cached list
                return cached.Path != null ? new List<Waypoint>(cached.Path) : null;
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

                    return new List<Waypoint>(path);
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

        public static float GetStraightDistance(Vector2 pointA, Vector2 pointB) // Calcula a distância em linha reta
        {
            return Vector2.Distance(pointA, pointB);
        }
    }
}