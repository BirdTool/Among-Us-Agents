using System;
using AMG.AI.Control.AgentController;
using AMG.Interfaces;
using AMG.Utilities.MapUtils.TasksUtils;

namespace AMG.AI.Mind.LLMAgentBrain
{
    public partial class LLMAgentBrain(IntPtr ptr) : AgentController(ptr)
    {
        private ArtificialTask CurrentTask;
        private ILLMAssistence Assistence;

        void Update()
        {
            if (CurrentPath.Count > 0) ProcessPath();

            Assistence?.Update();
        }

        private void ProcessPath()
        {
            bool? hasReachedDestination = ProcessPathMovement();

            if (hasReachedDestination == true || hasReachedDestination == null) ResetPath();
        }
    }
}