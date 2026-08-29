using System.Linq;
using AMG.Enums.AgentEnums;

namespace AMG.AI.Mind.StructuredAgentBrain
{
    public partial class StructuredAgentBrain
    {
        private void UpdateDoingTask()
        {
            if (currentLocalTask == null ||
                currentLocalTask.IsComplete ||
                !Agent.myTasks.ToArray().Any(p => p.Id == currentLocalTask.Id))
            {
                AITasks.Remove(currentLocalTask?.Id ?? 0);
                currentLocalTask = null;
                SetState(AgentState.Calculating);
                return;
            }

            bool success = TryExecuteTask(currentLocalTask.Id);
            if (success)
            {
                currentLocalTask = null;
                SetState(AgentState.Calculating);
            }
        }
    }
}
