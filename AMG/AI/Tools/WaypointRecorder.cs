using System;
using System.Collections.Generic;
using AMG.AI.Control;
using AMG.AI.Control.AgentController;
using AMG.AI.Debug;
using AMG.AI.Mind.ReactiveAgentBrain;
using AMG.AI.Mind.StructuredAgentBrain;
using AMG.AI.Navigation;
using AMG.Utilities;
using AMG.Utilities.KeyDown;
using AMG.Utilities.MapUtils.TasksUtils;
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
            TaskConsoleRegistry.Initialize();

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
    
    public class WaypointRecorder(IntPtr ptr) : MonoBehaviour(ptr)
    {
        private const float DistanceBetweenNodes = 0.5f;
        private const float MinSqrDistance = DistanceBetweenNodes * 0.9f * DistanceBetweenNodes * 0.9f;
        private const float RemoveSqrTolerance = 0.05f * 0.05f;

        private readonly List<WaypointData> _nodes = [];

        private MapNames _map;
        private bool _isRecording;
        private bool _loadFailed; // the file exists but couldn't be parsed: never record/save over it
        private int _unsavedCount;

        void Awake()
        {
            _map = WaypointManager.CurrentMap;
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
            AMGPlugin.KeyDownManager.RegisterKeyDown(KeyCode.P, SaveNodes);
        }

        void Update()
        {
            if (!_isRecording || _loadFailed || PlayerControl.LocalPlayer == null) return;

            TryAddNode(PlayerControl.LocalPlayer.transform.position);
            foreach (var agent in AgentManager.Agents)
            {
                TryAddNode(agent.Control.transform.position);
            }
        }

        private void ToggleRecording()
        {
            if (_loadFailed)
            {
                LogManager.LogError($"[AI GPS] Gravação bloqueada: não consegui ler {WaypointManager.GetJsonPath(_map)}. Corrija o arquivo e reinicie o jogo.");
                return;
            }

            _isRecording = !_isRecording;
            LogManager.LogDebug(_isRecording ? "[AI GPS] Gravação Contínua: LIGADA!" : $"[AI GPS] Gravação Contínua: DESLIGADA. {_unsavedCount} ponto(s) ainda não salvos (P para salvar).");
        }

        public void RemoveNode(Waypoint node)
        {
            if (node == null) return;

            foreach (var neighbor in node.Neighbors)
            {
                neighbor?.Neighbors.Remove(node);
            }

            foreach (var goldNeighbor in node.GoldNeighbors)
            {
                goldNeighbor?.GoldNeighbors.Remove(node);
            }

            WaypointManager.AllWaypoints.Remove(node);

            _nodes.RemoveAll(n => SqrDistance(n, node.Position) < RemoveSqrTolerance);

            Persist();

            LogManager.LogWarning($"[AI GPS] AUTO-LIMPEZA: Nó ruim em {node.Position} foi erradicado pela IA!");
        }

        private void LoadExistingNodes()
        {
            var data = WaypointManager.ReadData(_map);

            if (data == null)
            {
                _loadFailed = true;
                return;
            }

            _nodes.AddRange(data);
            LogManager.LogDebug($"[AI GPS] {_nodes.Count} nós carregados de {WaypointManager.GetJsonPath(_map)} para prevenção de duplicatas.");
        }

        private void TryAddNode(Vector2 pos)
        {
            foreach (var node in _nodes)
            {
                if (SqrDistance(node, pos) < MinSqrDistance) return;
            }

            _nodes.Add(new WaypointData { X = Round2(pos.x), Y = Round2(pos.y), IsGold = false });
            _unsavedCount++;
        }

        // P key: only writes when there is something new.
        private void SaveNodes()
        {
            if (!_loadFailed && _unsavedCount == 0)
            {
                LogManager.LogWarning("[AI GPS] Nenhum ponto novo na memória para salvar.");
                return;
            }

            Persist();
        }

        private void Persist()
        {
            if (_loadFailed)
            {
                LogManager.LogError("[AI GPS] Não vou salvar: o arquivo de waypoints não pôde ser lido e seria sobrescrito.");
                return;
            }

            if (!WaypointManager.WriteData(_map, _nodes)) return;

            LogManager.LogDebug($"[AI GPS] SUCESSO: {_nodes.Count} pontos salvos ({_unsavedCount} novos) em {WaypointManager.GetJsonPath(_map)}.");
            _unsavedCount = 0;
        }

        private static float SqrDistance(WaypointData node, Vector2 pos)
        {
            float dx = node.X - pos.x;
            float dy = node.Y - pos.y;
            return dx * dx + dy * dy;
        }

        private static float Round2(float value) => Mathf.Round(value * 100f) / 100f;
    }
}
