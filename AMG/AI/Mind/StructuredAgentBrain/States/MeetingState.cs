using AMG.AI.Services.MeetingService.StructuredBrain;
using AMG.Enums.AgentEnums;
using AMG.Utilities;

namespace AMG.AI.Mind.StructuredAgentBrain
{
    public partial class StructuredAgentBrain
    {
        public MeetingServiceForStructuredBrain MeetingService { get; private set; }

        private void UpdateMeetingState()
        {
            if (IsDead) return;

            if (Utils.IsMeeting)
            {
                SetState(AgentState.OnMeeting);
                MeetingService ??= new MeetingServiceForStructuredBrain(ChatService, this);

                MeetingService.Update();
                ChatService.Tick();

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