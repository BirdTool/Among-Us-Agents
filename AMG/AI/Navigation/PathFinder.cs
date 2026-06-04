using System.Collections.Generic;
using UnityEngine;
using AMG.Utilities;

namespace AMG.AI.Navigation
{
    public static class Pathfinder
    {
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
        public static List<Waypoint> FindPath(Waypoint startNode, Waypoint targetNode)
        {
            if (startNode == null || targetNode == null) return null;

            List<Waypoint> openSet = new List<Waypoint> { startNode };
            HashSet<Waypoint> closedSet = new HashSet<Waypoint>();
            Dictionary<Waypoint, Waypoint> cameFrom = new Dictionary<Waypoint, Waypoint>();

            Dictionary<Waypoint, float> gScore = new Dictionary<Waypoint, float>();
            Dictionary<Waypoint, float> fScore = new Dictionary<Waypoint, float>();

            foreach (var wp in WaypointManager.AllWaypoints)
            {
                gScore[wp] = float.MaxValue;
                fScore[wp] = float.MaxValue;
            }

            gScore[startNode] = 0;
            fScore[startNode] = Vector2.Distance(startNode.Position, targetNode.Position);

            int emergencyBreak = 0;
            while (openSet.Count > 0 && emergencyBreak < 5000)
            {
                emergencyBreak++;

                Waypoint current = openSet[0];
                for (int i = 1; i < openSet.Count; i++)
                {
                    if (fScore[openSet[i]] < fScore[current])
                        current = openSet[i];
                }

                if (current == targetNode)
                {
                    List<Waypoint> path = new List<Waypoint> { current };
                    while (cameFrom.ContainsKey(current))
                    {
                        current = cameFrom[current];
                        path.Add(current);
                    }
                    path.Reverse();
                    return path;
                }

                openSet.Remove(current);
                closedSet.Add(current);

                foreach (var neighbor in current.Neighbors)
                {
                    if (closedSet.Contains(neighbor)) continue;

                    float tentativeGScore = gScore[current] + Vector2.Distance(current.Position, neighbor.Position);

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                    else if (tentativeGScore >= gScore[neighbor])
                        continue;

                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = gScore[neighbor] + Vector2.Distance(neighbor.Position, targetNode.Position);
                }
            }

            LogManager.LogWarning("[AI GPS] Não foi possível conectar esses dois pontos no grafo.");
            return null;
        }
    }
}