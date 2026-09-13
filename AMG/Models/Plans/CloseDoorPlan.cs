using AMG.AI.Mind.StructuredAgentBrain;
using AMG.Interfaces;

namespace AMG.Models.Plans
{
    public class CloseDoorPlan(params SystemTypes[] doorRoom) : IPlan
    {
        public string Name { get; set; } = "CloseDoorPlan";
        private readonly SystemTypes[] _doorRoom = doorRoom;

        public bool IsDone { get; set; } = false;

        public void Execute(StructuredAgentBrain brain)
        {
            foreach (var door in _doorRoom)
            {
                brain.SafeCloseDoor(door);
            }
            IsDone = true;
        }
    }
}
