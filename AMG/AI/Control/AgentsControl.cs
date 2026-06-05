using AMG.AI.Mind;
using AMG.AI.Navigation;
using AMG.AI.Tools;
using AMG.Enums.AgentEnums;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AMG.AI.Control
{
    public static class AgentsControl
    {
        private static readonly Dictionary<byte, AgentControlData> AgentsActions = [];

        public static void MakeAgentDoTask(byte agentId)
        {
            var agent = AgentManager.Agents.FirstOrDefault(x => x.Control.PlayerId == agentId);
            if (agent == null) return;

            var brain = agent.Control.GetComponent<AgentBrain>();

            AgentControlData agentAction = GetAgentControlData(agentId);
            if (agentAction != null && agentAction.Action == AgentControlAction.DoingTask)
            {
                if (brain != null && brain.currentLocalTask != null) return;
            }

            if (agent.Control.myTasks == null || agent.Control.myTasks.Count == 0)
            {
                brain?.ReplaceNameTag(Enums.IdentifierEnum.Think, "Sem tarefas atribuídas!", "#ff3c3c");
                return;
            }

            PlayerTask nearbyTask = null;
            float? taskDistance = null;
            List<Waypoint> bestPath = null;

            Waypoint start = Pathfinder.GetClosestNode(agent.Control.transform.position);
            if (start == null) return;

            foreach (var task in agent.Control.myTasks)
            {
                if (task.IsComplete) continue;

                try
                {
                    foreach (var taskPosition in task.Locations)
                    {
                        Waypoint target = Pathfinder.GetClosestNode(taskPosition);
                        if (target == null) continue;

                        float pathDistance;
                        List<Waypoint> currentCalculatedPath = Pathfinder.FindPath(start, target, out pathDistance);

                        if (currentCalculatedPath != null)
                        {
                            if (!taskDistance.HasValue || pathDistance < taskDistance.Value)
                            {
                                taskDistance = pathDistance;
                                nearbyTask = task;
                                bestPath = currentCalculatedPath;
                            }
                        }
                    }
                }
                catch (System.Exception)
                {
                    continue;
                }
            }

            if (nearbyTask != null && bestPath != null)
            {
                SetAgentAction(agent, AgentControlAction.DoingTask);
                if (brain != null)
                {
                    brain.CommandGoToPath(bestPath);
                    brain.ReplaceNameTag(DefaultTags.Thoughts.DoingTask);
                    brain.currentLocalTask = nearbyTask;
                }
            }
            else
            {
                brain?.ReplaceNameTag(Enums.IdentifierEnum.Think, "Nenhuma rota válida!", "#ff3c3c");
            }
        }

        public static void MakeAllAgentsDoTask()
        {
            foreach (var agent in AgentManager.Agents)
            {
                MakeAgentDoTask(agent.Control.PlayerId);
            }
        }

        private static AgentControlData GetAgentControlData(byte agentId)
        {
            if (AgentsActions.TryGetValue(agentId, out var controlData))
            {
                return controlData;
            }
            return null;
        }

        private static void SetAgentAction(AgentListData agent, AgentControlAction action)
        {
            byte agentId = agent.Control.PlayerId;
            if (AgentsActions.ContainsKey(agentId))
            {
                AgentsActions[agentId].Action = action;
                AgentsActions[agentId].StartedAt = Time.time;
            }
            else
            {
                AgentsActions.Add(agentId, new AgentControlData
                {
                    Action = action,
                    StartedAt = Time.time
                });
            }
        }
    }

    public class AgentControlData
    {
        public float StartedAt { get; set; }
        public AgentControlAction Action { get; set; }
    }
}
