using AMG.AI.Navigation;
using AMG.AI.TasksWork.CommonTasks;
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

        public void MapGameTasksToAILogic()
        {
            AITasks.Clear();

            foreach (var gameTask in myAgent.myTasks)
            {
                switch (gameTask.TaskType)
                {
                    case TaskTypes.SwipeCard:
                        AITasks.Add(gameTask.Id, new CardTask());
                        break;
                    default:
                        break;
                }
            }
        }

        public void TryExecuteTask(uint taskId)
        {
            if (AITasks.TryGetValue(taskId, out ITaskWork aiTask))
            {
                bool finished = aiTask.Execute();

                if (finished)
                {
                    LogManager.LogDebug($"[AI] O Agente completou a task!");
                    myAgent.RpcCompleteTask(taskId);

                    AITasks.Remove(taskId);
                }
                else if (CanExecuteTask(taskId)) { TryExecuteTask(taskId); }
            }
        }

        private bool CanExecuteTask(uint taskId)
        {
            if (Utils.IsMeeting || Utils.IsExiling) return false;
            PlayerTask task = myAgent.myTasks.ToArray().ToList().Find(p => p.Id == taskId);
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
    }
}