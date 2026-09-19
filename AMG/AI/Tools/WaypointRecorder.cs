using System;
using System.Collections.Generic;
using System.IO;
using AMG.AI.Control;
using AMG.AI.Control.AgentController;
using AMG.AI.Debug;
using AMG.AI.Mind.ReactiveAgentBrain;
using AMG.AI.Mind.StructuredAgentBrain;
using AMG.AI.Navigation;
using AMG.Utilities;
using AMG.Utilities.KeyDown;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

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
                ClassInjector.RegisterTypeInIl2Cpp<KeyDownManager>();
                ClassInjector.RegisterTypeInIl2Cpp<WaypointRecorder>();
                ClassInjector.RegisterTypeInIl2Cpp<AgentController>();
                ClassInjector.RegisterTypeInIl2Cpp<StructuredAgentBrain>();
                ClassInjector.RegisterTypeInIl2Cpp<ReactiveAgentBrain>();
                ClassInjector.RegisterTypeInIl2Cpp<AgentVisionESP>();
                _isRegistered = true;
                LogManager.LogDebug("[AI GPS] Classes registradas com sucesso!");
            }

            if (__instance.gameObject.GetComponent<KeyDownManager>() == null)
            {
                AMGPlugin.KeyDownManager = __instance.gameObject.AddComponent<KeyDownManager>();
            }

            if (__instance.gameObject.GetComponent<WaypointRecorder>() == null)
            {
                __instance.gameObject.AddComponent<WaypointRecorder>();
            }

            if (__instance.gameObject.GetComponent<AgentVisionESP>() == null)
            {
                __instance.gameObject.AddComponent<AgentVisionESP>();
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

        void Start()
        {
            if (AMGPlugin.KeyDownManager == null)
            {
                LogManager.LogWarning("[AI GPS] KeyDownManager não encontrado — R/P não serão registrados.");
                return;
            }

            AMGPlugin.KeyDownManager.RegisterKeyDown(KeyCode.R, ToggleRecording);
            AMGPlugin.KeyDownManager.RegisterKeyDown(KeyCode.P, SaveBufferToFile);
        }

        private void ToggleRecording()
        {
            isRecording = !isRecording;
            LogManager.LogDebug(isRecording ? "[AI GPS] Gravação Contínua: LIGADA!" : "[AI GPS] Gravação Contínua: DESLIGADA.");
        }

        void Update()
        {
            if (PlayerControl.LocalPlayer == null) return;

            if (isRecording)
            {
                TrySaveNode(PlayerControl.LocalPlayer.transform.position);
                foreach (var agent in AgentManager.Agents)
                {
                    TrySaveNode(agent.Control.transform.position);
                }
            }
        }

        public void RemoveNode(Waypoint node)
        {
            if (node == null) return;

            foreach (var neighbor in node.Neighbors)
            {
                if (neighbor != null && neighbor.Neighbors.Contains(node))
                {
                    neighbor.Neighbors.Remove(node);
                }
            }

            WaypointManager.AllWaypoints.Remove(node);

            existingNodes.RemoveAll(pos => Vector2.Distance(pos, node.Position) < 0.05f);

            ResetAndSaveNodes();

            LogManager.LogWarning($"[AI GPS] AUTO-LIMPEZA: Nó ruim em {node.Position} foi erradicado pela IA!");
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

        public void ResetAndSaveNodes()
        {
            newLinesBuffer.Clear();

            foreach (var node in existingNodes)
            {
                BufferPoint("NODE", node);
            }

            File.WriteAllLines(filePath, newLinesBuffer);

            newLinesBuffer.Clear();
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