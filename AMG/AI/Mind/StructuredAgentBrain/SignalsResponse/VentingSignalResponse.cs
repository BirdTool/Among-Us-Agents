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

            LogManager.LogDebug($"[Vent] {AMG.AI.Services.ChatService.Llm.LlmMeetingSession.Colour(ventSignal.Player)} {(ventSignal.IsLeaving ? "left" : "entered")} a vent, "
                + $"{UnityEngine.Vector2.Distance(Vector2Position, ventSignal.Player.transform.position):0.0} units away (I am at {Vector2Position}), role {role?.Role.ToString() ?? "unknown"}.");

            // A partner using a vent is nothing to report: an impostor knows who the impostors are.
            if (IsImpostor && role != null && role.IsImpostor)
                return;

            var memory = GetOrCreateMemory(playerId);

            memory.VentRoom = ventSignal.Player.transform.position.GetClosestNode().Room;
            VentRoleCheck.Apply(memory, ventSignal.Player);
        }
    }
}