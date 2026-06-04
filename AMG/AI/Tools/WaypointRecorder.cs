using HarmonyLib;
using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime.Injection;
using AMG.Utilities;
using AMG.AI.Mind;

namespace AMG.AI.Tools
{
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
    public static class InjectRecorderPatch
    {
        private static bool _isRegistered = false;

        public static void Postfix(HudManager __instance)
        {
            if (!_isRegistered)
            {
                ClassInjector.RegisterTypeInIl2Cpp<WaypointRecorder>();
                ClassInjector.RegisterTypeInIl2Cpp<AgentBrain>();
                _isRegistered = true;
                LogManager.LogDebug("[AI GPS] Classes registradas com sucesso!");
            }

            if (__instance.gameObject.GetComponent<WaypointRecorder>() == null)
            {
                __instance.gameObject.AddComponent<WaypointRecorder>();
            }
        }
    }

    public class WaypointRecorder : MonoBehaviour
    {
        public WaypointRecorder(IntPtr ptr) : base(ptr) { }

        private string filePath;

        private bool isRecording = false;
        private float distanceBetweenNodes = 0.5f;

        private List<Vector2> existingNodes = new List<Vector2>();
        private List<string> newLinesBuffer = new List<string>();

        void Awake()
        {
            filePath = Path.Combine(Application.dataPath, "AI_Skeld_Waypoints.txt");
            LoadExistingNodes();
        }

        private void LoadExistingNodes()
        {
            if (!File.Exists(filePath)) return;

            string[] lines = File.ReadAllLines(filePath);
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] parts = line.Split('|');
                if (parts.Length >= 3 && parts[0] == "NODE")
                {
                    string cx = parts[1].Replace(',', '.');
                    string cy = parts[2].Replace(',', '.');

                    if (float.TryParse(cx, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x) &&
                        float.TryParse(cy, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y))
                    {
                        existingNodes.Add(new Vector2(x, y));
                    }
                }
            }
            LogManager.LogDebug($"[AI GPS] {existingNodes.Count} NODEs carregados na memória para prevenção de duplicatas.");
        }

        void Update()
        {
            if (PlayerControl.LocalPlayer == null) return;

            if (Input.GetKeyDown(KeyCode.R))
            {
                isRecording = !isRecording;
                LogManager.LogDebug(isRecording ? "[AI GPS] Gravação Contínua: LIGADA!" : "[AI GPS] Gravação Contínua: DESLIGADA.");
            }

            if (Input.GetKeyDown(KeyCode.P))
            {
                SaveBufferToFile();
            }

            if (isRecording)
            {
                TrySaveNode(PlayerControl.LocalPlayer.transform.position);
            }

            if (Input.GetKeyDown(KeyCode.T)) BufferPoint("TASK");
            if (Input.GetKeyDown(KeyCode.V)) BufferPoint("VENT");
            if (Input.GetKeyDown(KeyCode.M)) BufferPoint("SABOTAGE");
        }

        private void TrySaveNode(Vector2 pos)
        {
            foreach (Vector2 node in existingNodes)
            {
                if (Vector2.Distance(node, pos) < (distanceBetweenNodes * 0.9f)) return;
            }

            existingNodes.Add(pos);
            BufferPoint("NODE", pos);
        }

        private void BufferPoint(string type)
        {
            BufferPoint(type, PlayerControl.LocalPlayer.transform.position);
        }

        private void BufferPoint(string type, Vector2 pos)
        {
            string line = $"{type}|{pos.x:F2}|{pos.y:F2}";
            newLinesBuffer.Add(line);

            LogManager.LogDebug($"[AI GPS] Em Memória: {line}");
        }

        private void SaveBufferToFile()
        {
            if (newLinesBuffer.Count == 0)
            {
                LogManager.LogWarning("[AI GPS] Nenhum ponto novo na memória para salvar.");
                return;
            }

            File.AppendAllLines(filePath, newLinesBuffer);
            LogManager.LogDebug($"[AI GPS] SUCESSO: {newLinesBuffer.Count} novos pontos descarregados no arquivo físico!");

            newLinesBuffer.Clear();
        }
    }
}