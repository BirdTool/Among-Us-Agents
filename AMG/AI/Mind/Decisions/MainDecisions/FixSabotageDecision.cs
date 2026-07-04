using AMG.AI.Navigation;
using AMG.Interfaces;
using AMG.Utilities;
using System;
using System.Collections.Generic;

namespace AMG.AI.Mind.Decisions.MainDecisions
{
    internal class FixSabotageDecision : IMainDecision
    {
        public float CalculateUtility(AgentBrain brain)
        {
            if (!Utils.IsAnySabotageActive || brain.IsDead) return 0f;

            float points = 0f;

            if (brain.IsCrewmate)
            {
                points += 60f;
                if (brain.AITasks.Count < 2) points += 35f; // AITasks is removed when the agent does its tasks, so if it has less than 2 tasks, it means it's almost done with its tasks and can focus on fixing sabotages.
            }

            if (brain.IsImpostor)
            {
                if (Utils.SecondsSinceShipStart < 60) points += 20f;
                if (Utils.RemainingTasks < 6) points -= 30f;
                if (brain.GetNearbyPlayers().Count > 3) points += 35f;
            }

            return points;
        }

        // This method has not been tested yet!
        public bool Execute(AgentBrain brain)
        {
            if (!Utils.IsAnySabotageActive) return false;

            var currentShip = ShipStatus.Instance;
            var positions = Utils.Sabotages.GetActiveSabotageLocations(currentShip);
            if (positions == null || positions.Count == 0) return false;

            var start = brain.WaypointPosition;
            List<Waypoint> path = [];
            float distance = 0f;

            foreach (var position in positions)
            {
                var end = Pathfinder.GetClosestNode(position);
                var pathToPosition = Pathfinder.FindPath(start, end, out float totalDistance);

                if (totalDistance < distance)
                {
                    path = pathToPosition;
                    distance = totalDistance;
                }
            }

            // Does not exist a code to make the agent fix the sabotage, I'll do it later
            brain.CommandGoToPath(path);
            return true;
        }
    }
}
