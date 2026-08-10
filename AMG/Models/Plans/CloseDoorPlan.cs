using AMG.AI.Mind;
using AMG.Interfaces;

namespace AMG.Models.Plans
{
    public class CloseDoorPlan(SystemTypes doorRoom) : IPlan
    {
        private readonly SystemTypes _doorRoom = doorRoom;

        public bool IsDone { get; set; } = false;

        public void Execute(AgentBrain brain)
        {
            brain.SafeCloseDoor(_doorRoom);
            IsDone = true;
        }
    }
}
