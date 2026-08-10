using System.Linq;
using AMG.Interfaces;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Mind.Decisions.ParallelDecisions
{
    internal class KillPLDecision : IParallelDecision
    {
        public void Evaluate(AgentBrain brain)
        {
            if (brain.IsDead || brain.IsCrewmate) return;
            if (brain.AgentControl.killTimer > 0f) return;

            var currentRoom = brain.WaypointPosition.Room;
            var playersInTheRoom = Utils.Players.GetAllAlivePlayersInARoom(currentRoom);

            int impostors = playersInTheRoom.Count(p => p.Data.Role.IsImpostor);
            int crewmates = playersInTheRoom.Count() - impostors;

            if (crewmates < impostors) return;

            var nearbyTarget = brain.GetNearbyPlayers()
                .Where(p => !p.Data.Role.IsImpostor)
                .OrderBy(p => Vector2.Distance(p.transform.position, brain.Vector2Position))
                .FirstOrDefault();

            if (nearbyTarget == null) return;

            brain.SafeKill(nearbyTarget.PlayerId);
        }
    }
}