using System.Collections.Generic;
using System.Linq;
using AMG.AI.Navigation;
using AMG.AI.TasksWork;
using AMG.AI.Tools;
using AMG.Interfaces;
using AMG.Utilities;
using AMG.Utilities.MapUtils.SkeldTasks;
using AMG.Utilities.MapUtils.TasksUtils;

namespace AMG.AI.Control.AgentController
{
    public partial class AgentController
    {
        public List<ArtificialTask> ArtificialTasks = [];
        protected CooldownTimer taskTimer = new();
        public Dictionary<ArtificialTask, ITaskWork> AITasks = [];

        public void MapGameTasksToAILogic()
        {
            AITasks.Clear();
            foreach (var at in ArtificialTasks)
                AITasks[at] = TasksGroup.GetTaskOrGeneric(at.Task.TaskType);
        }

        public bool TryExecuteTask(ArtificialTask artificialTask)
        {
            if (!taskTimer.IsOver()) return false;
            if (!AITasks.TryGetValue(artificialTask, out var aiTask)) return false;

            bool stepFinished = aiTask.Execute();
            if (stepFinished)
            {
                artificialTask.NextStep();
                if (artificialTask.IsCompleted) AITasks.Remove(artificialTask);
                else AITasks[artificialTask] = TasksGroup.GetTaskOrGeneric(artificialTask.Task.TaskType);
            }
            else if (CanExecuteTask(artificialTask))
            {
                taskTimer.StartDelay(RandomizerExtensions.GetSecureRandomFloat(0.02f, 0.10f));
            }
            return stepFinished;
        }

        protected bool CanExecuteTask(ArtificialTask artificialTask)
        {
            if (Utils.IsMeeting || Utils.IsExiling) return false;

            if (artificialTask == null) return false;

            var taskPositions = artificialTask.GetTaskPositions();
            if (taskPositions.Count > 0)
            {
                if (artificialTask.IsImpostorTask)
                {
                    var location = artificialTask.GetCurrentStepTaskPosition();
                    if (location == null) return true;
                    return Utils.IsCloseToLocation(location.Position.GetClosestNode(), WaypointPosition, 2f);
                }
                List<Waypoint> tasksLocations = [];
                foreach (var location in taskPositions)
                {
                    tasksLocations.Add(Pathfinder.GetClosestNode(location.Position));
                }
                return Utils.IsCloseToAnyLocation(tasksLocations, WaypointPosition, 2f);
            }

            return true;
        }

        public void MapArtificialTasks()
        {
            ArtificialTasks.Clear();
            var all = SkeldTasksUtilities.GetArtificialTasks(Agent);
            ArtificialTasks.AddRange(SkeldTasksUtilities.ResolveDuplicateIds(all));
        }
    }
}