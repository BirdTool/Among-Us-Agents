using System.Collections.Generic;
using System.Linq;
using AMG.AI.Navigation;
using AMG.AI.TasksWork;
using AMG.AI.Tools;
using AMG.Interfaces;
using AMG.Utilities;

namespace AMG.AI.Control.AgentController
{
    public partial class AgentController
    {
        public Dictionary<uint, ITaskWork> AITasks = [];
        protected CooldownTimer taskTimer = new();

        public void MapGameTasksToAILogic()
        {
            AITasks.Clear();

            foreach (var gameTask in Agent.myTasks)
            {
                AITasks.Add(gameTask.Id, TasksGroup.GetTaskOrGeneric(gameTask.TaskType));
            }
        }

        public bool TryExecuteTask(uint taskId)
        {
            if (!taskTimer.IsOver()) return false;

            bool stepFinished = false;

            if (AITasks.TryGetValue(taskId, out ITaskWork aiTask))
            {
                stepFinished = aiTask.Execute();

                if (stepFinished)
                {
                    PlayerTask gameTask = Agent.myTasks.ToArray().FirstOrDefault(p => p.Id == taskId);
                    var normalTask = gameTask?.TryCast<NormalPlayerTask>();

                    if (normalTask != null)
                    {
                        // normalTask.taskStep++;
                        normalTask.NextStep();

                        if (normalTask.taskStep >= normalTask.MaxStep)
                        {
                            normalTask.taskStep = normalTask.MaxStep;

                            if (GameData.Instance != null)
                            {
                                GameData.Instance.CompletedTasks++;

                                if (HudManager.Instance != null)
                                    HudManager.Instance.taskDirtyTimer = 0f;

                                // LogManager.LogDebug($"[TaskRunner] Task {taskId} ({gameTask.TaskType}) concluída. {GameData.Instance.CompletedTasks}/{GameData.Instance.TotalTasks}");
                            }
                        }
                        else
                        {
                            AITasks[taskId] = TasksGroup.GetTaskOrGeneric(gameTask.TaskType);
                        }
                    }
                }
                else if (CanExecuteTask(taskId))
                {
                    taskTimer.StartDelay(RandomizerExtensions.GetSecureRandomFloat(0.02f, 0.10f));
                }
            }

            return stepFinished;
        }

        private bool CanExecuteTask(uint taskId)
        {
            if (Utils.IsMeeting || Utils.IsExiling) return false;
            PlayerTask task = Agent.myTasks.ToArray().FirstOrDefault(p => p.Id == taskId);
            if (task.Locations.Count > 0)
            {
                List<Waypoint> tasksLocations = [];
                foreach (var location in task.Locations)
                {
                    tasksLocations.Add(Pathfinder.GetClosestNode(location));
                }
                return Utils.IsCloseToAnyLocation(tasksLocations, WaypointPosition, 2f);
            }

            return true;
        }
    }
}