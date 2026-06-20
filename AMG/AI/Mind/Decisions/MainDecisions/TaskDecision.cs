using AMG.AI.Navigation;
using AMG.Interfaces;
using AMG.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// THIS CODE HAS NOT BEEN TESTED
// THIS CODE HAS NOT BEEN TESTED
// THIS CODE HAS NOT BEEN TESTED
// THIS CODE HAS NOT BEEN TESTED
// THIS CODE HAS NOT BEEN TESTED
// THIS CODE HAS NOT BEEN TESTED

namespace AMG.AI.Mind.Decisions.MainDecisions
{
    internal class TaskDecision : IMainDecision
    {
        private readonly Dictionary<int, float> _utilityCache = [];
        private readonly Dictionary<int, float> _nextUpdateTime = [];

        public float CalculateUtility(AgentBrain brain)
        {
            int agentId = brain.GetInstanceID();

            if (_nextUpdateTime.TryGetValue(agentId, out float nextUpdate))
            {
                if (Time.time < nextUpdate)
                {
                    return _utilityCache.TryGetValue(agentId, out float cached) ? cached : 0f;
                }
            }

            var tasks = brain.AgentControl.myTasks;
            if (tasks == null || tasks.Count == 0) return 0f;

            float finalPercentage = 0f;
            int tasksNearby = 0;

            var startNode = Pathfinder.GetClosestNode(brain.AgentControl.transform.position);

            if (startNode != null)
            {
                foreach (var task in tasks)
                {
                    if (!task.HasLocation) continue;

                    foreach (var location in task.Locations)
                    {
                        var endNode = Pathfinder.GetClosestNode(location);
                        if (endNode == null) continue;

                        Pathfinder.FindPath(startNode, endNode, out float realWalkDistance);

                        if (realWalkDistance <= 5f)
                        {
                            tasksNearby++;
                            break;
                        }
                    }
                }
            }

            if (tasksNearby > 0) finalPercentage += 40f;
            if (Utils.RemainingTasks < 4) finalPercentage += 40f;

            _utilityCache[agentId] = finalPercentage;
            _nextUpdateTime[agentId] = Time.time + 1f;

            return finalPercentage;
        }

        public void Execute(AgentBrain brain)
        {
            var tasks = brain.AgentControl.myTasks;
            if (tasks == null || tasks.Count == 0)
            {
                brain.SetState(Enums.AgentEnums.AgentState.Calculating);
                return;
            }

            var startNode = Pathfinder.GetClosestNode(brain.AgentControl.transform.position);
            if (startNode == null)
            {
                brain.SetState(Enums.AgentEnums.AgentState.Calculating);
                return;
            }

            var validTasks = new List<Tuple<PlayerTask, float, List<Waypoint>>>();

            foreach (var task in tasks)
            {
                if (!task.HasLocation) continue;

                float minWalkDist = float.MaxValue;
                List<Waypoint> bestPath = null;

                foreach (var location in task.Locations)
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
                    validTasks.Add(Tuple.Create(task, minWalkDist, bestPath));
                }
            }

            var shortsTaskNearby = new List<Tuple<PlayerTask, float, List<Waypoint>>>();
            var longTasks = ShipStatus.Instance?.LongTasks;

            foreach (var taskData in validTasks)
            {
                if (taskData.Item2 > 5f) continue;

                bool isShort = true;
                if (longTasks != null)
                {
                    for (int i = 0; i < longTasks.Count; i++)
                    {
                        if (longTasks[i]?.TaskType == taskData.Item1.TaskType)
                        {
                            isShort = false;
                            break;
                        }
                    }
                }

                if (isShort) shortsTaskNearby.Add(taskData);
            }

            var targetList = shortsTaskNearby.Count > 0 ? shortsTaskNearby : validTasks;

            if (targetList.Count > 0)
            {
                var bestTaskData = targetList.OrderBy(t => t.Item2).First();

                brain.currentLocalTask = bestTaskData.Item1;
                brain.CommandGoToPath(bestTaskData.Item3);
            }
            else
            {
                brain.SetState(Enums.AgentEnums.AgentState.Calculating);
            }
        }
    }
}