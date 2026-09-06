using System;
using System.Collections.Generic;
using System.IO;
using AMG.AI.Control;
using AMG.AI.Control.AgentController;
using AMG.AI.Debug;
using AMG.AI.Mind;
using AMG.AI.Mind.ReactiveAgentBrain;
using AMG.AI.Mind.StructuredAgentBrain;
using AMG.AI.Navigation;
using AMG.Utilities;
using AmongUs.GameOptions;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

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
                ClassInjector.RegisterTypeInIl2Cpp<AgentController>();
                ClassInjector.RegisterTypeInIl2Cpp<StructuredAgentBrain>();
                ClassInjector.RegisterTypeInIl2Cpp<ReactiveAgentBrain>();
                ClassInjector.RegisterTypeInIl2Cpp<AgentVisionESP>();
                HudManager.Instance.gameObject.AddComponent<AgentVisionESP>();
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

            if (Input.GetKeyDown(KeyCode.M))
            {
                var currentPos = PlayerControl.LocalPlayer.transform.position;
                LogManager.Log($"[Vector2] Current Position: x: {currentPos.x}, y: {currentPos.y}");
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                LogManager.LogDebug("[AI Command] Chamando todos os agentes!");

                Vector2 myPosition = PlayerControl.LocalPlayer.transform.position;
                Waypoint target = Pathfinder.GetClosestNode(myPosition);

                var agents = AgentManager.Agents;
                foreach (var agent in agents)
                {
                    var brain = agent.Control.GetComponent<StructuredAgentBrain>();
                    if (brain != null)
                    {
                        Waypoint start = Pathfinder.GetClosestNode(agent.Control.transform.position);
                        List<Waypoint> path = Pathfinder.FindPath(start, target, out _);
                        brain.CommandGoToPath(path);
                    }
                }

                // AgentsControl.MakeAllAgentsDoTask();
            }

            if (isRecording)
            {
                TrySaveNode(PlayerControl.LocalPlayer.transform.position);
                foreach (var agent in AgentManager.Agents)
                {
                    TrySaveNode(agent.Control.transform.position);
                }
            }

            if (Input.GetKeyDown(KeyCode.T))
            {
                AgentManager.WillBeImpostor = !AgentManager.WillBeImpostor;
                LogManager.LogDebug($"[AI Manager] Will be impostor: {AgentManager.WillBeImpostor}");
            }

            if (Input.GetKeyDown(KeyCode.K))
            {
                AgentsCommander.SetAllAgentAsCalculating();
            }

            if (Input.GetKeyDown(KeyCode.L))
            {
                var allVentObjects = UnityEngine.Object.FindObjectsOfType<Vent>();
                var groupedById = new Dictionary<int, List<Vent>>();

                foreach (var v in allVentObjects)
                {
                    if (!groupedById.ContainsKey(v.Id))
                        groupedById[v.Id] = [];
                    groupedById[v.Id].Add(v);
                }

                LogManager.LogDebug($"[VENT-DUP] Total objetos Vent na cena: {allVentObjects.Length} | AllVents.Length: {ShipStatus.Instance.AllVents.Length}");

                foreach (var kvp in groupedById)
                {
                    if (kvp.Value.Count > 1)
                    {
                        LogManager.LogDebug($"[VENT-DUP] Id={kvp.Key} tem {kvp.Value.Count} objetos disputando:");
                        foreach (var v in kvp.Value)
                        {
                            bool isRegistered = ShipStatus.Instance.AllVents[kvp.Key] == v;
                            LogManager.LogDebug($"    -> name={v.name} pos={v.transform.position} parent={v.transform.parent?.name} registradoEmAllVents={isRegistered}");
                        }
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.L))
            {
                static void DumpAllFields(Vent v, string label)
                {
                    LogManager.LogDebug($"--- Dump completo: {label} (id={v.Id}, name={v.name}) ---");
                    var type = v.GetType();

                    foreach (var field in type.GetFields())
                    {
                        try
                        {
                            LogManager.LogDebug($"    {field.Name} = {field.GetValue(v)}");
                        }
                        catch (Exception ex)
                        {
                            LogManager.LogDebug($"    {field.Name} = <erro: {ex.Message}>");
                        }
                    }

                    foreach (var prop in type.GetProperties())
                    {
                        try
                        {
                            LogManager.LogDebug($"    {prop.Name} (prop) = {prop.GetValue(v)}");
                        }
                        catch (Exception ex)
                        {
                            LogManager.LogDebug($"    {prop.Name} (prop) = <erro: {ex.Message}>");
                        }
                    }
                }

                DumpAllFields(ShipStatus.Instance.AllVents[4], "LEngineVent (quebrado)");
                DumpAllFields(ShipStatus.Instance.AllVents[9], "REngineVent (funciona)");
            }

            if (Input.GetKeyDown(KeyCode.J))
            {
                var allBrains = Utils.GetAllStructuredAgentBrain();

                foreach (var brain in allBrains)
                {
                    Vent closestVent = null;
                    float minDistance = float.MaxValue;

                    foreach (var vent in ShipStatus.Instance.AllVents)
                    {
                        float distance = Vector2.Distance(brain.Vector2Position, vent.transform.position);

                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            closestVent = vent;
                        }
                    }

                    if (closestVent != null)
                    {
                        brain.ResetDestinations();
                        brain.currentVentToEnter = closestVent;
                        brain.CommandGoToPath(Pathfinder.FindPath(brain.WaypointPosition, closestVent.transform.position.GetClosestNode(), out float _));
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.N))
            {
                if (AmongUsClient.Instance == null || AmongUsClient.Instance.PlayerPrefab == null) return;

                PlayerControl agentComponent = Utils.Players.LocalPlayer;
                AgentData agentData = new() { Name = agentComponent.Data.PlayerName };
                AgentManager.AddAgent(agentComponent, agentData);
                agentComponent.gameObject.AddComponent<StructuredAgentBrain>();
                var brain = agentComponent.gameObject.GetComponent<StructuredAgentBrain>();
                AgentController.AgentControlsRealPlayer = true;
                brain.MapGameTasksToAILogic();
            }
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

            // LogManager.LogDebug($"[AI GPS] Em Memória: {line}");
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