using AMG.AI.Mind;
using AMG.AI.Navigation;
using AMG.Interfaces;
using AMG.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace AMG.AI.Mind.StructuredAgentBrain.Plans
{
    public class EscapeAfterKillPlan(SystemTypes killRoom) : IPlan
    {
        private readonly SystemTypes _killRoom = killRoom;
        private bool _doorsClosed = false;

        public bool IsDone { get; set; } = false;
        private bool _hasStarted = false;

        public void Execute(StructuredAgentBrain brain)
        {
            if (IsDone || brain.IsDead) return;

            if (!_hasStarted)
            {
                _hasStarted = true;

                // Simple escape logic: Find a random node that is far away and pathfind there
                var allWaypoints = WaypointManager.AllWaypoints;
                var farCandidates = new List<Waypoint>();
                Vector2 agentPos = brain.Vector2Position;

                foreach (var wp in allWaypoints)
                {
                    if (Vector2.Distance(agentPos, wp.Position) >= 15f)
                    {
                        farCandidates.Add(wp);
                    }
                }

                var target = farCandidates.Count > 0
                    ? farCandidates.GetRandomItemSecureOrDefault()
                    : allWaypoints.GetRandomItemSecureOrDefault();

                if (target != null)
                {
                    var path = Pathfinder.FindPath(brain.WaypointPosition, target, out _);
                    brain.CommandGoToPath(path);
                }
            }

            // If we have started escaping, check if we have left the kill room
            if (_hasStarted && !_doorsClosed)
            {
                if (brain.WaypointPosition.Room != _killRoom)
                {
                    // We left the room, close the doors!
                    brain.SafeCloseDoor(_killRoom);
                    _doorsClosed = true;
                    IsDone = true; // We can consider the plan done after we close the doors
                }
                else if (brain.currentState != AMG.Enums.AgentEnums.AgentState.Navigating)
                {
                    // If we somehow stopped navigating before leaving the room (e.g. path blocked), end the plan
                    IsDone = true;
                }
            }
        }
    }
}
