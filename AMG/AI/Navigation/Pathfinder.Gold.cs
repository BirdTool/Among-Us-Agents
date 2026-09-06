using System.Collections.Generic;
using System.Text;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Navigation
{
    public static partial class Pathfinder
    {
        private const float GoldEdgeWeightMultiplier = 0.5f;

        public static List<Waypoint> FindGoldPath(Waypoint startNode, Waypoint targetNode, out float totalDistance)
        {
            totalDistance = 0f;
            if (startNode == null || targetNode == null) return null;

            var straight = FindStraightPath(startNode, targetNode, out totalDistance, returnNullIfCantProgress: true);
            if (straight != null) return straight;

            var cameFrom = new Dictionary<Waypoint, Waypoint>();
            var cameFromIsGold = new Dictionary<Waypoint, bool>();
            var gScore = new Dictionary<Waypoint, float> { [startNode] = 0f };
            var realDistance = new Dictionary<Waypoint, float> { [startNode] = 0f };
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
                    totalDistance = realDistance[current];

                    List<Waypoint> path = [current];
                    List<bool> edgeIsGold = [];
                    while (cameFrom.ContainsKey(current))
                    {
                        edgeIsGold.Add(cameFromIsGold[current]);
                        current = cameFrom[current];
                        path.Add(current);
                    }
                    path.Reverse();
                    edgeIsGold.Reverse();

                    // LogGoldPathSummary(edgeIsGold);

                    return path;
                }

                RelaxNeighbors(current, current.Neighbors, isGoldEdge: false, targetNode,
                    cameFrom, cameFromIsGold, gScore, realDistance, closedSet, openSet);

                if (current.IsGold)
                    RelaxNeighbors(current, current.GoldNeighbors, isGoldEdge: true, targetNode,
                        cameFrom, cameFromIsGold, gScore, realDistance, closedSet, openSet);
            }

            LogManager.LogWarning("[AI GPS] FindGoldPath: não foi possível conectar esses dois pontos.");
            return null;
        }

        private static void RelaxNeighbors(
            Waypoint current,
            List<Waypoint> neighbors,
            bool isGoldEdge,
            Waypoint targetNode,
            Dictionary<Waypoint, Waypoint> cameFrom,
            Dictionary<Waypoint, bool> cameFromIsGold,
            Dictionary<Waypoint, float> gScore,
            Dictionary<Waypoint, float> realDistance,
            HashSet<Waypoint> closedSet,
            PriorityQueue<Waypoint, float> openSet)
        {
            foreach (var neighbor in neighbors)
            {
                if (closedSet.Contains(neighbor)) continue;

                if (current.Room != neighbor.Room)
                {
                    if (Utils.IsRoomClosed(current.Room)) continue;
                    if (Utils.IsRoomClosed(neighbor.Room)) continue;
                }

                float distance = Vector2.Distance(current.Position, neighbor.Position);
                float weightedCost = distance * (isGoldEdge ? GoldEdgeWeightMultiplier : 1f);
                float tentativeGScore = gScore[current] + weightedCost;

                if (gScore.TryGetValue(neighbor, out float existingGScore) && tentativeGScore >= existingGScore)
                    continue;

                cameFrom[neighbor] = current;
                cameFromIsGold[neighbor] = isGoldEdge;
                gScore[neighbor] = tentativeGScore;
                realDistance[neighbor] = realDistance[current] + distance;

                float neighborFScore = tentativeGScore + Vector2.Distance(neighbor.Position, targetNode.Position);
                openSet.Enqueue(neighbor, neighborFScore);
            }
        }

        private static void LogGoldPathSummary(List<bool> edgeIsGold)
        {
            int goldSegments = 0;
            int normalSegments = 0;
            var sequence = new StringBuilder();

            for (int i = 0; i < edgeIsGold.Count; i++)
            {
                bool isGold = edgeIsGold[i];
                if (isGold) goldSegments++; else normalSegments++;

                sequence.Append(isGold ? "GOLD" : "A*");
                if (i < edgeIsGold.Count - 1) sequence.Append(" > ");
            }

            LogManager.LogDebug($"[AI GPS] FindGoldPath: {normalSegments} segmentos A* + {goldSegments} segmentos GOLD. Sequência: {sequence}");
        }
    }
}