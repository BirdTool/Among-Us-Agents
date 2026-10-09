using AMG.AI.Services.MeetingService;
using AMG.Enums.AgentEnums;
using AMG.Utilities;

namespace AMG.AI.Mind.StructuredAgentBrain
{
    public partial class StructuredAgentBrain
    {
        public MeetingService MeetingService { get; private set; }

        private void UpdateMeetingState()
        {
            if (IsDead) return;

            if (Utils.IsMeeting)
            {
                SetState(AgentState.OnMeeting);
                MeetingService ??= new MeetingService();

                ChatService.Tick();

                return;
            }
            else if (Utils.IsExiling)
            {
                MeetingService = null;
                return;
            }
            else
            {
                MeetingService = null;
                SetState(AgentState.Calculating);
            }
        }
    }
}