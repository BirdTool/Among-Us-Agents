using AMG.AI.Navigation;
using AMG.Interfaces;
using AMG.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace AMG.AI.Mind.StructuredAgentBrain.Decisions.MainDecisions
{
    internal class TaskDecision : IMainDecision
    {
        private readonly Dictionary<int, float> _utilityCache = [];
        private readonly Dictionary<int, float> _nextUpdateTime = [];
        public readonly Dictionary<int, float> _timeWithoutDoingTasks = [];

        private List<Vector2> GetSafeTaskLocations(PlayerTask task)
        {
            var locs = new List<Vector2>();

            var normalTask = task.TryCast<NormalPlayerTask>();
            if (normalTask != null)
            {
                try
                {
                    var validPositions = normalTask.FindValidConsolesPositions();
                    if (validPositions != null)
                    {
                        foreach (var pos in validPositions) locs.Add(pos);
                        if (locs.Count > 0)
                        {
                            return locs;
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    LogManager.LogDebug($"[TaskDecision] FindValidConsolesPositions falhou: {ex.Message}");
                }

                /*
                try
                {
                    var specialConsole = normalTask.FindSpecialConsole();
                    if (specialConsole != null)
                    {
                        locs.Add(specialConsole.transform.position);
                        LogManager.LogDebug($"[TaskDecision] FindSpecialConsole encontrado para {task.TaskType}");
                        return locs;
                    }
                }
                catch (System.Exception ex)
                {
                    LogManager.LogDebug($"[TaskDecision] FindSpecialConsole falhou: {ex.Message}");
                }
                */
            }

            try
            {
                foreach (var loc in task.Locations) locs.Add(loc);
                if (locs.Count > 0) return locs;
            }
            catch (System.Exception) { }

            try
            {
                if (ShipStatus.Instance != null && ShipStatus.Instance.AllConsoles != null)
                {
                    foreach (var console in ShipStatus.Instance.AllConsoles)
                    {
                        bool hasTaskType = false;
                        foreach (var t in console.TaskTypes)
                        {
                            if (t == task.TaskType) { hasTaskType = true; break; }
                        }
                        if (hasTaskType) locs.Add(console.transform.position);
                    }
                }
            }
            catch (System.Exception) { }

            return locs;
        }

        public float CalculateUtility(StructuredAgentBrain brain)
        {
            int agentId = brain.GetInstanceID();

            if (_nextUpdateTime.TryGetValue(agentId, out float nextUpdate) && Time.time < nextUpdate)
            {
                return _utilityCache.TryGetValue(agentId, out float cached) ? cached : 0f;
            }

            float utility = brain.Agent.Data.Role.IsImpostor ? ImpostorUtility(brain) : CrewmateUtility(brain);


            _utilityCache[agentId] = utility;
            _nextUpdateTime[agentId] = Time.time + 1f;

            // LogManager.LogDebug($"[TaskDecision-CalculateUtility] AgentId: {agentId}, Utility: {utility}");
            return utility;
        }

        private float CrewmateUtility(StructuredAgentBrain brain)
        {
            var tasks = brain.Agent.myTasks;
            if (tasks == null || tasks.Count == 0) return 0f;

            bool isDead = brain.Agent.Data.IsDead;
            Vector2 agentPos = brain.Agent.transform.position;
            var startNode = Pathfinder.GetClosestNode(agentPos);

            if (!isDead && startNode == null) return 0f;

            int validTasksCount = 0;
            float finalPercentage = 0f;
            int tasksNearby = 0;

            foreach (var task in tasks)
            {
                if (task.IsComplete) continue;

                var safeLocations = GetSafeTaskLocations(task);
                if (safeLocations.Count == 0) continue;

                bool hasAtLeastOneValidLocation = false;

                foreach (var location in safeLocations)
                {
                    hasAtLeastOneValidLocation = true;

                    float straightDist = Vector2.Distance(agentPos, location);
                    if (straightDist > 8f) continue;

                    if (isDead)
                    {
                        if (straightDist <= 5f) tasksNearby++;
                    }
                    else
                    {
                        var endNode = Pathfinder.GetClosestNode(location);
                        if (endNode == null) continue;

                        Pathfinder.FindPath(startNode, endNode, out float realWalkDistance);
                        if (realWalkDistance <= 5f)
                        {
                            // LogManager.LogDebug($"[TaskDecision-Crewmate] Painel de {task.TaskType} esta perto!");
                            tasksNearby++;
                            break;
                        }
                    }
                }

                if (hasAtLeastOneValidLocation) validTasksCount++;
            }

            if (validTasksCount == 0)
            {
                // LogManager.LogDebug("[TaskDecision-Crewmate] Falha: Nenhuma task pendente com localizacao valida encontada.");
                return 0f;
            }

            if (Utils.SecondsSinceShipStart.HasValue && Utils.SecondsSinceShipStart < 40f) finalPercentage += 100f;
            if (tasksNearby > 0) finalPercentage += 40f;
            if (Utils.RemainingTasks < 4) finalPercentage += 40f;
            if (isDead) finalPercentage += 60f;
            if (validTasksCount > 3) finalPercentage += 20f;

            return finalPercentage;
        }

        private float ImpostorUtility(StructuredAgentBrain brain)
        {
            var tasks = brain.Agent.myTasks;
            if (tasks == null || tasks.Count == 0) return 0f;

            bool isDead = brain.Agent.Data.IsDead;
            var startNode = Pathfinder.GetClosestNode(brain.Agent.transform.position);

            if (!isDead && startNode == null) return 0f;

            int validTasksCount = 0;
            foreach (var task in tasks)
            {
                if (task.IsComplete) continue;

                var safeLocations = GetSafeTaskLocations(task);
                if (safeLocations.Count > 0) validTasksCount++;
            }

            if (validTasksCount == 0) return 0f;

            int agentId = brain.GetInstanceID();
            if (!_timeWithoutDoingTasks.ContainsKey(agentId))
                _timeWithoutDoingTasks.Add(agentId, Time.time);

            float finalPercentage = 0f;

            if (Utils.CompletedTasks < 4) finalPercentage += 40f;
            if (Utils.SecondsSinceShipStart.HasValue && Utils.SecondsSinceShipStart < 40f) finalPercentage += 80f;

            var timeSinceLastTask = Time.time - _timeWithoutDoingTasks[agentId];
            finalPercentage += Mathf.Min(timeSinceLastTask * 1.2f, 60f);

            if (brain.ImpostorMemory.FakedTasks >= tasks.Count)
                finalPercentage -= 40f;

            return finalPercentage;
        }

        public bool Execute(StructuredAgentBrain brain)
        {
            var tasks = brain.Agent.myTasks;
            if (tasks == null || tasks.Count == 0) return false;

            var startNode = Pathfinder.GetClosestNode(brain.Agent.transform.position);
            bool isDead = brain.Agent.Data.IsDead;

            if (!isDead && startNode == null) return false;

            var validTasks = new List<(PlayerTask Task, float Dist, List<Waypoint> Path)>();

            foreach (var task in tasks)
            {
                if (task.IsComplete) continue;

                var safeLocations = GetSafeTaskLocations(task);
                if (safeLocations.Count == 0) continue;

                float minWalkDist = float.MaxValue;
                List<Waypoint> bestPath = null;

                foreach (var location in safeLocations)
                {
                    var endNode = Pathfinder.GetClosestNode(location);
                    if (endNode == null) continue;

                    var path = Pathfinder.FindPath(startNode, endNode, out float realWalkDist);

                    if (path != null && realWalkDist < minWalkDist)
                    {
                        minWalkDist = realWalkDist;
                        bestPath = path;
                    }
                }

                if (bestPath != null)
                {
                    validTasks.Add((task, minWalkDist, bestPath));
                }
            }

            if (validTasks.Count == 0) return false;

            var shortsTaskNearby = new List<(PlayerTask Task, float Dist, List<Waypoint> Path)>();
            var longTasks = ShipStatus.Instance?.LongTasks;

            foreach (var taskData in validTasks)
            {
                if (taskData.Dist > 5f) continue;

                bool isShort = true;
                if (longTasks != null)
                {
                    for (int i = 0; i < longTasks.Count; i++)
                    {
                        if (longTasks[i]?.TaskType == taskData.Task.TaskType)
                        {
                            isShort = false;
                            break;
                        }
                    }
                }

                if (isShort) shortsTaskNearby.Add(taskData);
            }

            var targetList = shortsTaskNearby.Count > 0 ? shortsTaskNearby : validTasks;

            var bestTaskData = targetList[0];
            for (int i = 1; i < targetList.Count; i++)
            {
                if (targetList[i].Dist < bestTaskData.Dist)
                {
                    bestTaskData = targetList[i];
                }
            }

            // LogManager.LogDebug($"[TaskDecision-Execute] SUCESSO! Agente comandado para task: {bestTaskData.Task.TaskType}");
            brain.currentLocalTask = bestTaskData.Task;
            brain.CommandGoToPath(bestTaskData.Path);

            return true;
        }
    }
}