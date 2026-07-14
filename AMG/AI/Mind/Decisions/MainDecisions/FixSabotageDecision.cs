using AMG.AI.Navigation;
using AMG.Interfaces;
using AMG.Utilities;
using AMG.AI.Mind.Decisions.Sabotages;
using System.Collections.Generic;
using UnityEngine;

namespace AMG.AI.Mind.Decisions.MainDecisions
{
    internal class FixSabotageDecision : IMainDecision
    {
        private readonly Dictionary<byte, float> _sabotageTimers = [];

        public float CalculateUtility(AgentBrain brain)
        {
            if (!Utils.IsAnySabotageActive || brain.IsDead) return 0f;
            float points = 100f;
            if (brain.IsCrewmate) { points += 70f; if (brain.AITasks.Count < 2) points += 35f; }
            if (brain.IsImpostor) { points -= 30f; if (Utils.SecondsSinceShipStart < 60) points += 20f; if (Utils.RemainingTasks < 6) points -= 30f; if (brain.GetNearbyPlayers().Count > 3) points += 35f; }
            return points;
        }

        public bool Execute(AgentBrain brain)
        {
            if (Utils.CurrentSabotage == null) return false;

            ISabotage activeSabotage = Utils.CurrentSabotage;
            /*
            if (!Utils.IsAnySabotageActive) return false;

            ISabotage activeSabotage = SabotageManager.GetActiveManualSabotage();
            if (activeSabotage == null) return false;
            */

            Vector2 myPos = brain.transform.position;
            SabotageStep bestStep = null;
            Waypoint bestWaypoint = null;
            float bestScore = float.MaxValue;

            foreach (var step in activeSabotage.GetSteps())
            {
                if (step.Locations == null || step.Locations.Count == 0) continue;

                foreach (var target in step.Locations)
                {
                    float score = Vector2.Distance(myPos, target.Position);

                    foreach (var player in PlayerControl.AllPlayerControls)
                    {
                        if (player == null || player.Data == null || player.Data.IsDead) continue;
                        if (player.PlayerId == brain.AgentControl.PlayerId) continue;

                        float otherDist = Vector2.Distance(player.transform.position, target.Position);
                        if (otherDist < score) score += 25f;
                    }

                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestWaypoint = target;
                        bestStep = step;
                    }
                }
            }

            if (bestWaypoint == null || bestStep == null) return false;

            var start = Pathfinder.GetClosestNode(myPos);
            if (start == null) return false;

            var path = Pathfinder.FindPath(start, bestWaypoint, out float totalDistance);

            if (path == null || path.Count == 0)
            {
                if (Vector2.Distance(myPos, bestWaypoint.Position) < 1.8f)
                    path = new List<Waypoint> { bestWaypoint };
                else
                    return false;
            }

            LogManager.LogDebug($"[FixSabotageDecision] SUCESSO! Agente comandado para consertar sabotagem.");
            brain.currentSabotageStep = bestStep;
            brain.CommandGoToPath(path);

            return true;
        }
    }
}