using AMG.AI.Navigation;
using AMG.Interfaces;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Mind.Decisions.MainDecisions
{
    internal class FixSabotageDecision : IMainDecision
    {
        public float CalculateUtility(AgentBrain brain)
        {
            if (!Utils.IsAnySabotageActive || brain.IsDead) return 0f;

            float points = 100f;

            if (brain.IsCrewmate)
            {
                points += 70f;
                if (brain.AITasks.Count < 2) points += 35f;
            }

            if (brain.IsImpostor)
            {
                points -= 30f;
                if (Utils.SecondsSinceShipStart < 60) points += 20f;
                if (Utils.RemainingTasks < 6) points -= 30f;
                if (brain.GetNearbyPlayers().Count > 3) points += 35f;
            }

            return points;
        }

        // Still unworkable, the agent doens't go to the sabotage task, and I'm lazy do fix it in this commit
        // I'll track every sabotage position manually
        // I'm tired of using AI to help me, I'll do it myself, even if I just want to sleep
        public bool Execute(AgentBrain brain)
        {
            if (!Utils.IsAnySabotageActive) return false;

            var currentShip = ShipStatus.Instance;
            var positions = Utils.Sabotages.GetActiveSabotageLocations(currentShip);
            if (positions == null || positions.Count == 0) return false;

            Vector2 myPos = brain.transform.position;

            foreach (var pos in positions)
            {
                if (Vector2.Distance(myPos, pos) < 1.8f)
                {
                    brain.isGoingToFixASabotage = true;
                    brain.ResetPath();
                    brain.currentState = Enums.AgentEnums.AgentState.Stopped;
                    return true;
                }
            }

            Vector2 bestPosition = Vector2.zero;
            float bestScore = float.MaxValue;
            bool foundAny = false;

            foreach (var pos in positions)
            {
                float score = Vector2.Distance(myPos, pos);

                foreach (var player in PlayerControl.AllPlayerControls)
                {
                    if (player == null || player.Data == null || player.Data.IsDead) continue;
                    if (player.PlayerId == brain.AgentControl.PlayerId) continue;

                    float otherDist = Vector2.Distance(player.transform.position, pos);
                    if (otherDist < score)
                    {
                        score += 25f;
                    }
                }

                if (score < bestScore)
                {
                    bestScore = score;
                    bestPosition = pos;
                    foundAny = true;
                }
            }

            if (!foundAny) return false;

            Waypoint targetNode = GetGuaranteedClosestNode(bestPosition);
            if (targetNode == null) return false;

            var start = brain.WaypointPosition;
            if (start == null) return false;

            var path = Pathfinder.FindPath(start, targetNode, out float totalDistance);

            if (path == null || path.Count == 0) return false;

            brain.isGoingToFixASabotage = true;
            brain.CommandGoToPath(path);
            return true;
        }

        private Waypoint GetGuaranteedClosestNode(Vector2 targetPos)
        {
            Waypoint closest = null;
            float minDistance = float.MaxValue;

            foreach (var wp in WaypointManager.AllWaypoints)
            {
                float dist = Vector2.Distance(targetPos, wp.Position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closest = wp;
                }
            }
            return closest;
        }
    }
}