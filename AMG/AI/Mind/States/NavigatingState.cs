using AMG.AI.Tools;
using AMG.Enums.AgentEnums;

namespace AMG.AI.Mind
{
    public partial class AgentBrain
    {
        private void UpdateNavigating()
        {
            ReplaceNameTag(DefaultTags.States.Navigating);

            bool hasReachedDestination = ProcessPathMovement();

            if (hasReachedDestination)
            {
                ResetPath();

                if (currentLocalTask != null)
                {
                    SetState(AgentState.DoingTask);
                }
                else
                {
                    SetState(AgentState.Calculating);
                }
            }
        }
    }
}
