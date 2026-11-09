using AMG.Models.Signals;
using AMG.Utilities;

namespace AMG.AI.Mind.StructuredAgentBrain
{
    public partial class StructuredAgentBrain
    {
        private void OnVentSignalReceived(VentSignal ventSignal)
        {
            byte playerId = ventSignal.Player.PlayerId;
            if (playerId == AgentId)
                return;

            var role = ventSignal.Player.Data?.Role;

            // A partner using a vent is nothing to report: an impostor knows who the impostors are.
            if (IsImpostor && role != null && role.IsImpostor)
                return;

            var memory = GetOrCreateMemory(playerId);

            memory.VentRoom = ventSignal.Player.transform.position.GetClosestNode().Room;
            VentRoleCheck.Apply(memory, ventSignal.Player);
        }
    }
}