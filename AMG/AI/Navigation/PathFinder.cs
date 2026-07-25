using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using AMG.Utilities;

namespace AMG.AI.Navigation
{
    public static class Pathfinder
    {
        // ── Path cache ───────────────────────────────────────────────────────
        private const float PATH_CACHE_TTL = 5f;

        private readonly struct CachedPath
        {
            public readonly List<Waypoint> Path;
            public readonly float TotalDistance;
            
            // 🚨 MUDANÇA CRÍTICA: Time.time causa Crash fora da Main Thread!
            // Usamos DateTime.UtcNow, que é 100% Thread-Safe (nativo do C#).
            public readonly DateTime CachedAt; 

            public CachedPath(List<Waypoint> path, float dist)
            {
                Path = path;
                TotalDistance = dist;
                CachedAt = DateTime.UtcNow; 
            }

            public bool IsExpired => (DateTime.UtcNow - CachedAt).TotalSeconds > PATH_CACHE_TTL;
        }

        private static readonly ConcurrentDictionary<(Waypoint, Waypoint), CachedPath> _pathCache = new();

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
                // Vector2.Distance é uma conta matemática pura (Struct), então é Thread-Safe!
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

        // ── 🚀 NOVA VERSÃO ASSÍNCRONA (Para chamadas fáceis e diretas) ──────
        // Métodos Async não suportam parâmetros "out". Retornamos uma Tupla!
        public static Task<(List<Waypoint> Path, float Distance)> FindPathAsync(Waypoint startNode, Waypoint targetNode)
        {
            return Task.Run(() => 
            {
                var path = FindPath(startNode, targetNode, out float dist);
                return (path, dist);
            });
        }

        // ── Versão Síncrona (Agora blindada para rodar dentro de Task.Run) ──
        public static List<Waypoint> FindPath(Waypoint startNode, Waypoint targetNode, out float totalDistance)
        {
            totalDistance = 0f;

            if (startNode == null || targetNode == null) return null;

            var cacheKey = (startNode, targetNode);
            if (_pathCache.TryGetValue(cacheKey, out var cached) && !cached.IsExpired)
            {
                totalDistance = cached.TotalDistance;
                return cached.Path != null ? new List<Waypoint>(cached.Path) : null;
            }

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
                        // ⚠️ AVISO: Certifique-se que IsRoomClosed lê de uma variável (bool/dicionário)
                        // e não acesse Componentes/GameObjects da Unity diretamente!
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

            _pathCache[cacheKey] = new CachedPath(null, 0f);

            // LogManager precisa ser Thread-Safe (Debug.Log padrão da Unity mais recente é safe, 
            // mas dependendo do seu wrapper pode dar erro. Fique de olho).
            LogManager.LogWarning("[AI GPS] Não foi possível conectar esses dois pontos no grafo.");
            return null;
        }

        public static float GetStraightDistance(Vector2 pointA, Vector2 pointB) 
        {
            return Vector2.Distance(pointA, pointB);
        }
    }
}