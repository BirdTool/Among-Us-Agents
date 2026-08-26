using AMG.Models.Signals;

namespace AMG.AI.Mind.StructuredAgentBrain
{
    public partial class StructuredAgentBrain
    {
        private void OnVentSignalReceived(VentSignal ventSignal)
        {
            byte playerId = ventSignal.Player.PlayerId;
            if (playerId == AgentId)
                return;

            var memory = GetOrCreateMemory(playerId);
            
            memory.SawVenting = true;
            memory.IncreaseSuspiciusPercentage(100);
        }
    }
}