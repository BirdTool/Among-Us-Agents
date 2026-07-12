using AMG.AI.Navigation;
using AMG.AI.TasksWork;
using AMG.AI.Tools;
using AMG.Enums.AgentEnums;
using AMG.Interfaces;
using AMG.Utilities;
using System.Collections.Generic;
using System.Linq;

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

            bool stepFinished = false;

            if (AITasks.TryGetValue(taskId, out ITaskWork aiTask))
            {
                stepFinished = aiTask.Execute();

                if (stepFinished)
                {
                    PlayerTask gameTask = myAgent.myTasks.ToArray().FirstOrDefault(p => p.Id == taskId);
                    var normalTask = gameTask?.TryCast<NormalPlayerTask>();

                    if (normalTask != null)
                    {
                        normalTask.taskStep++;

                        if (normalTask.taskStep >= normalTask.MaxStep)
                        {
                            normalTask.taskStep = normalTask.MaxStep;

                            myAgent.myTasks.Remove(gameTask);
                            AITasks.Remove(taskId);

                            if (GameData.Instance != null)
                            {
                                GameData.Instance.CompletedTasks++;

                                if (HudManager.Instance != null)
                                    HudManager.Instance.taskDirtyTimer = 0f;

                                LogManager.LogDebug($"[TaskRunner] Task {taskId} ({gameTask.TaskType}) concluída. {GameData.Instance.CompletedTasks}/{GameData.Instance.TotalTasks}");
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
                SetState(AgentState.Calculating);
            }
            bool success = TryExecuteTask(currentLocalTask.Id);
            if (success)
            {
                SetState(AgentState.Calculating);
            }
        }

        private void UpdateFixingSabotage()
        {
            if (!Utils.IsAnySabotageActive || currentSabotageStep == null)
            {
                currentSabotageStep = null;
                SetState(AgentState.Calculating);
                return;
            }

            if (sabotageTimer.IsOver())
            {
                currentSabotageStep.CompleteStep(this);
                sabotageTimer.StartDelay(0.5f);
            }
        }
    }
}