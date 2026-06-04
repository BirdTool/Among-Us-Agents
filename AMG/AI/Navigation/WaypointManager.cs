using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using AMG.Utilities;

namespace AMG.AI.Navigation
{
    public enum WaypointType { NODE, TASK, VENT, SABOTAGE }

    public class Waypoint
    {
        public WaypointType Type;
        public Vector2 Position;
    }

    public static class WaypointManager
    {
        public static List<Waypoint> AllWaypoints = new List<Waypoint>();

        public static void LoadWaypoints()
        {
            string filePath = Path.Combine(Application.dataPath, "AI_Skeld_Waypoints.txt");

            if (!File.Exists(filePath))
            {
                LogManager.LogError("[AI Nav] Arquivo de rotas não encontrado! O Agente não saberá andar.");
                return;
            }

            string[] lines = File.ReadAllLines(filePath);
            AllWaypoints.Clear();

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
                            AllWaypoints.Add(new Waypoint
                            {
                                Type = type,
                                Position = new Vector2(x, y)
                            });
                        }
                    }
                }
            }

            LogManager.LogDebug($"[AI Nav] Mapa do cérebro carregado! Total de pontos gravados: {AllWaypoints.Count}");
        }
    }
}