using System;
using System.Collections.Generic;
using System.IO;
using AMG.AI.Tools;
using AMG.Utilities;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace AMG.AI.Navigation
{
    public enum WaypointType { NODE, TASK, VENT, SABOTAGE } // Deprecated

    public class Waypoint
    {
        public WaypointType Type;
        public Vector2 Position;
        public List<Waypoint> Neighbors = [];
        private int stuckHot = 0;

        public SystemTypes Room = SystemTypes.Hallway;

        public HashSet<SystemTypes> NeighborRooms = [];
        public SystemTypes? ClosestNeighborRoom = null;

        public void IncreaseStuckHot()
        {
            if (stuckHot < 0) return;

            stuckHot++;

            if (stuckHot >= 30)
            {
                stuckHot = -9999;

                if (HudManager.Instance == null || HudManager.Instance.gameObject == null) return;

                var waypointRecorder = HudManager.Instance.gameObject.GetComponent<WaypointRecorder>();
                if (waypointRecorder != null)
                {
                    waypointRecorder.RemoveNode(this);
                }
            }
        }
    }

    public static class WaypointManager
    {
        public static MapNames CurrentMap => (MapNames)Utils.GetCurrentMapID();

        public static List<Waypoint> AllWaypoints
        {
            get
            {
                if (!WaypointsByMap.ContainsKey(CurrentMap))
                {
                    WaypointsByMap[CurrentMap] = [];
                }
                return WaypointsByMap[CurrentMap];
            }
        }

        private static readonly Dictionary<MapNames, List<Waypoint>> WaypointsByMap = [];

        public static void LoadWaypoints()
        {
            if (AllWaypoints.Count > 0) return;
            string filePath = Path.Combine(Application.dataPath, $"AI_{CurrentMap}_Waypoints.txt");
            if (!File.Exists(filePath)) return;

            string[] lines = File.ReadAllLines(filePath);

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] parts = line.Split('|');
                if (parts.Length >= 3)
                {
                    string cleanX = parts[1].Replace(',', '.');
                    string cleanY = parts[2].Replace(',', '.');

                    if (float.TryParse(cleanX, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x) &&
                        float.TryParse(cleanY, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y))
                    {
                        if (Enum.TryParse(parts[0], out WaypointType type))
                        {
                            AllWaypoints.Add(new Waypoint { Type = type, Position = new Vector2(x, y) });
                        }
                    }
                }
            }

            float connectionRadius = 0.68f;
            int totalConnections = 0;

            for (int i = 0; i < AllWaypoints.Count; i++)
            {
                for (int j = i + 1; j < AllWaypoints.Count; j++)
                {
                    if (Vector2.Distance(AllWaypoints[i].Position, AllWaypoints[j].Position) <= connectionRadius)
                    {
                        AllWaypoints[i].Neighbors.Add(AllWaypoints[j]);
                        AllWaypoints[j].Neighbors.Add(AllWaypoints[i]);
                        totalConnections++;
                    }
                }
            }

            MapWaypointsToRooms();
            ComputeHallwayRoomAdjacency();

            LogManager.LogDebug($"[AI Nav] Malha gerada! {AllWaypoints.Count} Pontos e {totalConnections} Conexões criadas.");
        }

        public static void ComputeHallwayRoomAdjacency()
        {
            var visited = new HashSet<Waypoint>();

            foreach (var waypoint in AllWaypoints)
            {
                if (waypoint.Room != SystemTypes.Hallway || visited.Contains(waypoint)) continue;

                var segment = new List<Waypoint>();
                var queue = new Queue<Waypoint>();
                queue.Enqueue(waypoint);
                visited.Add(waypoint);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    segment.Add(current);

                    foreach (var neighbor in current.Neighbors)
                    {
                        if (neighbor.Room == SystemTypes.Hallway && visited.Add(neighbor))
                        {
                            queue.Enqueue(neighbor);
                        }
                    }
                }

                var neighborRooms = new HashSet<SystemTypes>();
                foreach (var point in segment)
                {
                    foreach (var neighbor in point.Neighbors)
                    {
                        if (neighbor.Room != SystemTypes.Hallway)
                        {
                            neighborRooms.Add(neighbor.Room);
                        }
                    }
                }

                foreach (var point in segment)
                {
                    point.NeighborRooms = neighborRooms;
                }
                
                foreach (var point in segment)
                {
                    point.ClosestNeighborRoom = FindClosestNonHallwayRoom(point);
                }

                LogManager.LogDebug($"[AI Nav] Corredor com {segment.Count} pontos faz fronteira com: {string.Join(", ", neighborRooms)}");
            }
        }

        private static SystemTypes? FindClosestNonHallwayRoom(Waypoint start)
        {
            var bestDist = new Dictionary<Waypoint, float> { [start] = 0f };
            var queue = new PriorityQueue<Waypoint, float>();
            queue.Enqueue(start, 0f);

            var closed = new HashSet<Waypoint>();

            while (queue.Count > 0)
            {
                queue.TryDequeue(out var current, out float currentDist);
                if (!closed.Add(current)) continue;

                if (current.Room != SystemTypes.Hallway)
                {
                    return current.Room;
                }

                foreach (var neighbor in current.Neighbors)
                {
                    float newDist = currentDist + Vector2.Distance(current.Position, neighbor.Position);

                    if (!bestDist.TryGetValue(neighbor, out float existing) || newDist < existing)
                    {
                        bestDist[neighbor] = newDist;
                        queue.Enqueue(neighbor, newDist);
                    }
                }
            }

            return null;
        }

        public static void MapWaypointsToRooms()
        {
            if (ShipStatus.Instance == null || ShipStatus.Instance.AllRooms == null) return;

            Dictionary<SystemTypes, int> pointsTracked = [];

            foreach (var room in ShipStatus.Instance.AllRooms)
            {
                if (room.roomArea == null)
                {
                    LogManager.LogError($"[AI Nav] Sala {room.RoomId} não tem uma área.");
                    continue;
                }

                foreach (var waypoint in AllWaypoints)
                {
                    if (room.roomArea.OverlapPoint(waypoint.Position))
                    {
                        waypoint.Room = room.RoomId;

                        if (!pointsTracked.ContainsKey(room.RoomId))
                            pointsTracked.Add(room.RoomId, 0);

                        pointsTracked[room.RoomId]++;
                    }
                }
            }

            LogManager.LogDebug($"[AI Nav] Todos os nós foram mapeados para suas respectivas salas!");

            foreach (var (room, count) in pointsTracked)
            {
                LogManager.LogDebug($"[AI Nav] Sala {room} tem {count} pontos.");
            }
        }
    }
}