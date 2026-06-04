using System.Collections.Generic;
using UnityEngine;
using AMG.AI.Navigation;
using AMG.Utilities;

namespace AMG.AI.Navigation
{
    public static class Pathfinder
    {
        public static bool IsClearRigid(Vector2 start, Vector2 end, float width = 0.35f)
        {
            Vector2 dir = (end - start).normalized;
            Vector2 perp = new Vector2(-dir.y, dir.x);

            float[] offsets = { -width, -width / 2, 0f, width / 2, width };

            foreach (float offset in offsets)
            {
                Vector2 rayStart = start + (perp * offset);
                Vector2 rayEnd = end + (perp * offset);

                RaycastHit2D[] hits = Physics2D.LinecastAll(rayStart, rayEnd);

                foreach (var hit in hits)
                {
                    if (hit.collider == null) continue;
                    if (hit.collider.isTrigger) continue;
                    if (hit.collider.gameObject.layer == 9) continue;
                    if (hit.collider.GetComponent<PlayerControl>() != null) continue;

                    if (hit.distance < 0.05f) continue;

                    return false;
                }
            }
            return true;
        }

        public static List<Waypoint> FindPath(Vector2 startPos, Waypoint finalTarget)
        {
            List<Waypoint> openSet = new List<Waypoint>();
            HashSet<Waypoint> closedSet = new HashSet<Waypoint>();
            Dictionary<Waypoint, Waypoint> cameFrom = new Dictionary<Waypoint, Waypoint>();
            Dictionary<Waypoint, float> gScore = new Dictionary<Waypoint, float>();
            Dictionary<Waypoint, float> fScore = new Dictionary<Waypoint, float>();

            foreach (var wp in WaypointManager.AllWaypoints)
            {
                gScore[wp] = float.MaxValue;
                fScore[wp] = float.MaxValue;
            }

            bool canSeeAnyNode = false;

            foreach (var wp in WaypointManager.AllWaypoints)
            {
                if (wp.Type != WaypointType.NODE) continue;

                if (IsClearRigid(startPos, wp.Position, 0.35f))
                {
                    openSet.Add(wp);
                    gScore[wp] = Vector2.Distance(startPos, wp.Position);
                    fScore[wp] = gScore[wp] + Vector2.Distance(wp.Position, finalTarget.Position);
                    canSeeAnyNode = true;
                }
            }

            if (!canSeeAnyNode)
            {
                Waypoint bestEmergencyNode = null;
                float minDistance = float.MaxValue;

                foreach (var wp in WaypointManager.AllWaypoints)
                {
                    if (wp.Type != WaypointType.NODE) continue;

                    if (IsClearRigid(startPos, wp.Position, 0.1f))
                    {
                        float dist = Vector2.Distance(startPos, wp.Position);
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            bestEmergencyNode = wp;
                        }
                    }
                }

                if (bestEmergencyNode != null)
                {
                    openSet.Add(bestEmergencyNode);
                    gScore[bestEmergencyNode] = minDistance;
                    fScore[bestEmergencyNode] = gScore[bestEmergencyNode] + Vector2.Distance(bestEmergencyNode.Position, finalTarget.Position);
                    canSeeAnyNode = true;
                }
            }

            if (!canSeeAnyNode)
            {
                Waypoint blindNode = null;
                float minBlindDist = float.MaxValue;
                foreach (var wp in WaypointManager.AllWaypoints)
                {
                    if (wp.Type != WaypointType.NODE) continue;
                    float dist = Vector2.Distance(startPos, wp.Position);
                    if (dist < minBlindDist) { minBlindDist = dist; blindNode = wp; }
                }

                if (blindNode != null)
                {
                    LogManager.LogWarning("[AI GPS] Modo Pânico! Andando cego para me soltar da quina.");
                    return new List<Waypoint> { blindNode, finalTarget }; // Força a ida
                }
                return null;
            }

            int emergencyBreak = 0;
            while (openSet.Count > 0 && emergencyBreak < 1000)
            {
                emergencyBreak++;

                Waypoint current = openSet[0];
                for (int i = 1; i < openSet.Count; i++)
                {
                    if (fScore[openSet[i]] < fScore[current])
                        current = openSet[i];
                }

                if (current == finalTarget)
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

                foreach (var neighbor in WaypointManager.AllWaypoints)
                {
                    if (closedSet.Contains(neighbor)) continue;
                    if (neighbor.Type != WaypointType.NODE && neighbor != finalTarget) continue;

                    bool pathIsClear = (neighbor == finalTarget) ?
                                       IsClearRigid(current.Position, neighbor.Position, 0.05f) :
                                       IsClearRigid(current.Position, neighbor.Position, 0.35f);

                    if (pathIsClear)
                    {
                        float tentativeGScore = gScore[current] + Vector2.Distance(current.Position, neighbor.Position);

                        if (!openSet.Contains(neighbor)) openSet.Add(neighbor);
                        else if (tentativeGScore >= gScore[neighbor]) continue;

                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeGScore;
                        fScore[neighbor] = gScore[neighbor] + Vector2.Distance(neighbor.Position, finalTarget.Position);
                    }
                }
            }
            return null;
        }
    }
}