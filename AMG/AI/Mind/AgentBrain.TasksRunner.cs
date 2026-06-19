using AMG.AI.Navigation;
using AMG.AI.TasksWork;
using AMG.AI.Tools;
using AMG.Interfaces;
using AMG.Utilities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AMG.AI.Mind
{
    public partial class AgentBrain
    {
        public Dictionary<uint, ITaskWork> AITasks = [];
        private CooldownTimer taskTimer = new CooldownTimer();

        public void MapGameTasksToAILogic()
        {
            AITasks.Clear();

            foreach (var gameTask in myAgent.myTasks)
            {
                AITasks.Add(gameTask.Id, TasksGroup.GetTaskOrGeneric(gameTask.TaskType));
            }
        }

        public bool TryExecuteTask(uint taskId)
        {
            if (!taskTimer.IsOver()) return false;

            bool finished = false;

            if (AITasks.TryGetValue(taskId, out ITaskWork aiTask))
            {
                finished = aiTask.Execute();

                if (finished)
                {
                    myAgent.RpcCompleteTask(taskId);
                    AITasks.Remove(taskId);
                }
                else if (CanExecuteTask(taskId))
                {
                    taskTimer.StartDelay(RandomizerExtensions.GetSecureRandomFloat(0.02f, 0.10f));
                }
            }

            return finished;
        }

        private bool CanExecuteTask(uint taskId)
        {
            if (Utils.IsMeeting || Utils.IsExiling) return false;
            PlayerTask task = myAgent.myTasks.ToArray().FirstOrDefault(p => p.Id == taskId);
            if (task.Locations.Count > 0)
            {
                var start = Pathfinder.GetClosestNode(myAgent.transform.position);

                float minimumDist = float.MaxValue;
                foreach (var location in task.Locations)
                {
                    var end = Pathfinder.GetClosestNode(location);
                    var path = Pathfinder.FindPath(start, end, out float dist);

                    if (path != null)
                    {
                        if (dist < minimumDist)
                        {
                            minimumDist = dist;
                        }
                    }
                }

                if (minimumDist > 2) return false;
            }

            return true;
        }

        private void UpdateDoingTask()
        {
            if (currentLocalTask == null)
            {
                RemoveNameTag(Enums.IdentifierEnum.Think);
                DecisionTest();
            }
            bool success = TryExecuteTask(currentLocalTask.Id);
            if (success)
            {
                RemoveNameTag(Enums.IdentifierEnum.Think);
                DecisionTest();
            }
        }
    }
}