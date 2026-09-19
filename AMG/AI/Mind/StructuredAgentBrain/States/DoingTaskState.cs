using System.Linq;
using AMG.Enums.AgentEnums;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Mind.StructuredAgentBrain
{
    public partial class StructuredAgentBrain
    {
        private float _doingTaskSince = -1f;
        private uint _doingTaskId;

        private void UpdateDoingTask()
        {
            if (currentLocalTask == null || currentLocalTask.IsCompleted ||
                !Agent.myTasks.ToArray().Any(p => p.Id == currentLocalTask.Id))
            {
                AITasks.Remove(currentLocalTask);
                currentLocalTask = null;
                _doingTaskSince = -1f;
                SetState(AgentState.Calculating);
                return;
            }

            if (_doingTaskSince < 0f || _doingTaskId != currentLocalTask.Id)
            {
                _doingTaskSince = Time.time;
                _doingTaskId = currentLocalTask.Id;
            }

            if (Time.time - _doingTaskSince > 20f)
            {
                LogManager.LogDebug($"[DoingTask] Timeout em {currentLocalTask.Task.TaskType} (id {currentLocalTask.Id}). Abandonando.");
                currentLocalTask.BlockedUntil = Time.time + 30f;
                currentLocalTask = null;
                _doingTaskSince = -1f;
                SetState(AgentState.Calculating);
                return;
            }

            if (TryExecuteTask(currentLocalTask))
            {
                currentLocalTask = null;
                _doingTaskSince = -1f;
                SetState(AgentState.Calculating);
            }
        }
    }
}
