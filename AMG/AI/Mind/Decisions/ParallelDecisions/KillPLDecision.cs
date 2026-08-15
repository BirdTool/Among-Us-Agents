using System.Linq;
using AMG.AI.Tools;
using AMG.Interfaces;
using AMG.Utilities;
using UnityEngine;
using AMG.AI.Mind.Plans;
using System.Collections.Generic;

namespace AMG.AI.Mind.Decisions.ParallelDecisions
{
    internal class KillPLDecision : IParallelDecision
    {
        private readonly Dictionary<byte, CooldownTimer> _killDecisionTimers = [];

        private CooldownTimer GetKillTimer(byte agentId)
        {
            if (!_killDecisionTimers.ContainsKey(agentId))
            {
                _killDecisionTimers[agentId] = new CooldownTimer();
            }
            return _killDecisionTimers[agentId];
        }

        public void Evaluate(AgentBrain brain)
        {
            if (brain.IsDead || brain.IsCrewmate) return;
            if (brain.AgentControl.killTimer > 0f || !KillCooldownManager.CanKill(brain.AgentControl.PlayerId)) return;

            var timer = GetKillTimer(brain.AgentControl.PlayerId);

            if (timer.IsRunning) return; // Wait until cooldown finishes before deciding again

            var nearbyTarget = brain.NearbyPlayers
                .Where(p => !p.Data.Role.IsImpostor)
                .OrderBy(p => Vector2.Distance(p.transform.position, brain.Vector2Position))
                .FirstOrDefault();

            if (nearbyTarget == null) return;

            // Check if kill is mechanically possible first
            if (brain.SafeKillNotExecute(nearbyTarget.PlayerId) != AMG.Enums.SafeRpcEnums.SafeKillRpcEnums.SUCCESS) return;

            var currentRoom = brain.WaypointPosition.Room;
            var playersInTheRoom = Utils.Players.GetAllAlivePlayersInARoom(currentRoom);

            int impostorsInRoom = playersInTheRoom.Count(p => p.Data.Role.IsImpostor);
            int crewmatesInRoom = playersInTheRoom.Count() - impostorsInRoom;

            var nearbyPlayers = brain.NearbyPlayers;
            int nearbyImpostors = nearbyPlayers.Count(p => p.Data.Role.IsImpostor);
            int nearbyCrewmates = nearbyPlayers.Count - nearbyImpostors;

            int chance = 0;

            // Crowd check: abort if too many witnesses
            if (crewmatesInRoom > 2 || nearbyCrewmates > 2)
            {
                timer.StartDelay(1.5f);
                return;
            }

            // Crowd check 2: if 2 crewmates, we MUST have at least 2 impostors
            if ((crewmatesInRoom == 2 || nearbyCrewmates == 2) && impostorsInRoom < 2)
            {
                timer.StartDelay(1.5f);
                return;
            }

            // Alone with exactly 1 crewmate (the target) and no other crewmates nearby
            if (crewmatesInRoom <= 1 && nearbyCrewmates <= 1) 
            {
                chance = 100;
            }
            // Double kill opportunity (at least 2 impostors and exactly 2 crewmates)
            else if (impostorsInRoom >= 2 && crewmatesInRoom == 2 && nearbyCrewmates == 2) 
            {
                chance = 100;
            }

            if (chance > 0)
            {
                if (Utils.ExecuteProbability(chance))
                {
                    // Perform kill
                    brain.SafeKill(nearbyTarget.PlayerId);

                    // Queue escape plan
                    brain.AddPlan(new EscapeAfterKillPlan(currentRoom));
                    
                    // Add a delay before next kill consideration to avoid spamming
                    timer.StartDelay(5f);
                }
                else
                {
                    // Failed probability, wait a bit before checking again
                    timer.StartDelay(2f);
                }
            }
            else
            {
                // No good opportunity right now
                timer.StartDelay(1f);
            }
        }
    }
}