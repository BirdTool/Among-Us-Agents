using System.Collections.Generic;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Navigation
{
    public static partial class Pathfinder
    {
        // ── Path cache ───────────────────────────────────────────────────────
        // Shared across all agents. Invalidated when any door opens/closes.
        // Also expires after PATH_CACHE_TTL seconds as a safety net.
        // Usado só pelo FindAlgorithPath (busca em linha reta é barata o bastante pra não precisar de cache).
        private const float PATH_CACHE_TTL = 5f;

        private readonly struct CachedPath(List<Waypoint> path, float dist)
        {
            public readonly List<Waypoint> Path = path;
            public readonly float TotalDistance = dist;
            public readonly float CachedAt = Time.time;

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

        public static float GetStraightDistance(Vector2 pointA, Vector2 pointB)
        {
            return Vector2.Distance(pointA, pointB);
        }

        public static List<Waypoint> FindPath(Waypoint startNode, Waypoint targetNode, out float totalDistance)
        {
            if (startNode == null || targetNode == null)
            {
                totalDistance = 0f;
                return null;
            }

            var straight = FindStraightPath(startNode, targetNode, out totalDistance, returnNullIfCantProgress: true);
            if (straight != null) return straight;

            return FindAlgorithPath(startNode, targetNode, out totalDistance);
        }
    }
}