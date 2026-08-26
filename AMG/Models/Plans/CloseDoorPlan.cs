using AMG.AI.Control.AgentController;
using AMG.AI.Mind;
using AMG.AI.Mind.StructuredAgentBrain;
using AMG.Interfaces;

namespace AMG.Models.Plans
{
    public class CloseDoorPlan(SystemTypes doorRoom) : IPlan
    {
        private readonly SystemTypes _doorRoom = doorRoom;

        public bool IsDone { get; set; } = false;

        public void Execute(StructuredAgentBrain brain)
        {
            brain.SafeCloseDoor(_doorRoom);
            IsDone = true;
        }
    }
}
