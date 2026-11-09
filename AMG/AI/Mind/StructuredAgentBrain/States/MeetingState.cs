using AMG.AI.Services.ChatService;
using AMG.AI.Services.ChatService.Local;
using AMG.AI.Services.MeetingService.StructuredBrain;
using AMG.Enums.AgentEnums;
using AMG.Utilities;

namespace AMG.AI.Mind.StructuredAgentBrain
{
    public partial class StructuredAgentBrain
    {
        public MeetingServiceForStructuredBrain MeetingService { get; private set; }
        public ChatServiceClass ChatService { get; private set; }

        private void UpdateMeetingState()
        {
            if (IsDead) return;

            if (Utils.IsMeeting)
            {
                SetState(AgentState.OnMeeting);

                return;
            }
            else if (Utils.IsExiling)
            {
                ChatService.Clear();
                KnownBodyRoom = null;
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