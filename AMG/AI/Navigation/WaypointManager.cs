using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using AMG.AI.Tools;
using AMG.Utilities;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace AMG.AI.Navigation
{
    public class Waypoint
    {
        public Vector2 Position;
        public List<Waypoint> Neighbors = [];
        public List<Waypoint> GoldNeighbors = [];
        private int stuckHot = 0;

        public bool IsGold = false;

        public SystemTypes Room = SystemTypes.Hallway;

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
                    WaypointsByMap[CurrentMap] = [];
                return WaypointsByMap[CurrentMap];
            }
        }

        private static readonly Dictionary<MapNames, List<Waypoint>> WaypointsByMap = [];

        private const float ConnectionRadius = 0.68f;
        private const float GoldVisionMaxDistance = 100f;

        private static string GetJsonPath(MapNames map) =>
            Path.Combine(Application.dataPath, $"AI_{map}_Waypoints.json");

        private static string GetLegacyTxtPath(MapNames map) =>
            Path.Combine(Application.dataPath, $"AI_{map}_Waypoints.txt");

        public static void LoadWaypoints()
        {
            if (AllWaypoints.Count > 0) return;

            string jsonPath = GetJsonPath(CurrentMap);

            if (!File.Exists(jsonPath))
            {
                string txtPath = GetLegacyTxtPath(CurrentMap);
                if (!File.Exists(txtPath)) return;

                LogManager.LogDebug($"[AI Nav] JSON não encontrado, convertendo {txtPath}...");
                ConvertLegacyTxtToJson(CurrentMap);
            }

            List<WaypointData> data;
            try
            {
                data = JsonSerializer.Deserialize<List<WaypointData>>(File.ReadAllText(jsonPath));
            }
            catch (Exception ex)
            {
                LogManager.LogError($"[AI Nav] Erro ao ler JSON de waypoints: {ex.Message}");
                return;
            }

            if (data == null) return;

            foreach (var wp in data)
            {
                AllWaypoints.Add(new Waypoint
                {
                    Position = new Vector2(wp.X, wp.Y),
                    IsGold = wp.IsGold
                });
            }

            int totalConnections = 0;

            for (int i = 0; i < AllWaypoints.Count; i++)
            {
                for (int j = i + 1; j < AllWaypoints.Count; j++)
                {
                    if (Vector2.Distance(AllWaypoints[i].Position, AllWaypoints[j].Position) <= ConnectionRadius)
                    {
                        AllWaypoints[i].Neighbors.Add(AllWaypoints[j]);
                        AllWaypoints[j].Neighbors.Add(AllWaypoints[i]);
                        totalConnections++;
                    }
                }
            }

            int goldConnections = BuildGoldVisionConnections();

            MapWaypointsToRooms();

            LogManager.LogDebug($"[AI Nav] Malha gerada! {AllWaypoints.Count} Pontos, {totalConnections} Conexões normais e {goldConnections} Conexões gold (visão).");
        }

        private static int BuildGoldVisionConnections()
        {
            var goldPoints = AllWaypoints.FindAll(w => w.IsGold);
            int count = 0;

            for (int i = 0; i < goldPoints.Count; i++)
            {
                for (int j = i + 1; j < goldPoints.Count; j++)
                {
                    var a = goldPoints[i];
                    var b = goldPoints[j];

                    if (a.GoldNeighbors.Contains(b)) continue;

                    if (Utils.CanWalkToTarget(a.Position, b.Position, GoldVisionMaxDistance))
                    {
                        a.GoldNeighbors.Add(b);
                        b.GoldNeighbors.Add(a);
                        count++;
                    }
                }
            }

            return count;
        }

        public static void ConvertLegacyTxtToJson(MapNames map)
        {
            string txtPath = GetLegacyTxtPath(map);
            if (!File.Exists(txtPath)) return;

            var converted = new List<WaypointData>();

            foreach (string line in File.ReadAllLines(txtPath))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] parts = line.Split('|');
                if (parts.Length < 3) continue;

                string cleanX = parts[1].Replace(',', '.');
                string cleanY = parts[2].Replace(',', '.');

                if (float.TryParse(cleanX, NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
                    float.TryParse(cleanY, NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
                {
                    converted.Add(new WaypointData { X = x, Y = y, IsGold = false });
                }
            }

            File.WriteAllText(GetJsonPath(map), JsonSerializer.Serialize(converted, new JsonSerializerOptions { WriteIndented = true }));
            LogManager.LogDebug($"[AI Nav] Convertido {converted.Count} waypoints de TXT para JSON ({map}).");
        }

        public static void ConvertAllMapsToJson()
        {
            foreach (MapNames map in Enum.GetValues(typeof(MapNames)))
            {
                if (File.Exists(GetLegacyTxtPath(map)))
                    ConvertLegacyTxtToJson(map);
            }
        }

        public static void AppendWaypoint(WaypointData data)
        {
            string jsonPath = GetJsonPath(CurrentMap);

            List<WaypointData> existing = File.Exists(jsonPath)
                ? JsonSerializer.Deserialize<List<WaypointData>>(File.ReadAllText(jsonPath)) ?? []
                : [];

            existing.Add(data);
            File.WriteAllText(jsonPath, JsonSerializer.Serialize(existing, new JsonSerializerOptions { WriteIndented = true }));
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