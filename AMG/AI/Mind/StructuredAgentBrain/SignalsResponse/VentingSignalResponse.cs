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

            LogManager.LogDebug($"[Vent] {AMG.AI.Services.ChatService.Llm.LlmMeetingSession.Colour(ventSignal.Player)} {(ventSignal.IsLeaving ? "left" : "entered")} a vent, "
                + $"{UnityEngine.Vector2.Distance(Vector2Position, ventSignal.Player.transform.position):0.0} units away (I am at {Vector2Position}).");

            var memory = GetOrCreateMemory(playerId);
            
            memory.SawVenting = true;
            memory.IncreaseSuspiciusPercentage(100);
            memory.VentRoom = ventSignal.Player.transform.position.GetClosestNode().Room;
        }
    }
}