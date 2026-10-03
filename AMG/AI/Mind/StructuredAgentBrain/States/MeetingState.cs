using AMG.Enums.AgentEnums;
using AMG.Utilities;

namespace AMG.AI.Mind.StructuredAgentBrain
{
    public partial class StructuredAgentBrain
    {
        private void UpdateMeetingState()
        {
            if (Agent.Data.IsDead) return;

            if (Utils.IsMeeting || Utils.IsExiling)
            {
                SetState(AgentState.OnMeeting);
                return;
            }
            else
            {
                SetState(AgentState.Calculating);
            }
        }
    }
}