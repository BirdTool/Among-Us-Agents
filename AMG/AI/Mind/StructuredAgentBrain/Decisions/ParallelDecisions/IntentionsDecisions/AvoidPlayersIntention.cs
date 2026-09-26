using System.Collections.Generic;
using System.Linq;
using AMG.AI.Navigation;
using AMG.AI.Tools;
using AMG.Interfaces;
using AMG.Patches.Gameplay;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Mind.StructuredAgentBrain.Decisions.ParallelDecisions.IntentionsDecisions
{
    public class AvoidPlayersIntention : IParallelDecision
    {
        private const float StoppedSpeedThreshold = 0.05f;

        private const float OppositeDirectionDotThreshold = 0f;

        private readonly Dictionary<byte, byte> _avoiding = []; // Brain PlayerId -> Player

        public void Evaluate(StructuredAgentBrain brain)
        {
            if (brain.Intentions.AvoidAllPlayers)
            {
                var players = brain.NearbyPlayersInVision;
                if (players.Any())
                    RunAwayFrom(brain, brain.Agent.PlayerId, [.. players.Select(p => (Vector2)p.transform.position)]);
                return;
            }
            if (!brain.Intentions.AvoidPlayers.Any()) return;


            var nearby = brain.NearbyPlayersInVision;
            var playersToAvoid = nearby.Where(p => brain.Intentions.AvoidPlayers.Contains(p.PlayerId)).ToList();

            if (!playersToAvoid.Any())
            {
                _avoiding.Remove(brain.Agent.PlayerId);
                return;
            }

            if (playersToAvoid.Count == 1)
                AvoidOnePlayerLogic(brain, playersToAvoid[0]);
            else
                AvoidMultiplePlayersLogic(brain, playersToAvoid);
        }

        private void AvoidOnePlayerLogic(StructuredAgentBrain brain, PlayerControl playerToAvoid)
        {
            var selfMotion = PlayerMotionTrackerPatches.GetTracker(brain.Agent);
            var playerMotion = PlayerMotionTrackerPatches.GetTracker(playerToAvoid);
            if (selfMotion == null || playerMotion == null) return;

            if (_avoiding.TryGetValue(brain.Agent.PlayerId, out var currentTarget) && currentTarget == playerToAvoid.PlayerId)
                return;

            var playerVelocity = playerMotion.Velocity;
            var playerIsStopped = playerVelocity.sqrMagnitude < StoppedSpeedThreshold * StoppedSpeedThreshold;

            if (playerIsStopped)
            {
                RunAwayFrom(brain, playerToAvoid.PlayerId, [(Vector2)playerToAvoid.transform.position]);
                return;
            }

            var selfVelocity = selfMotion.Velocity;
            if (selfVelocity.sqrMagnitude < StoppedSpeedThreshold * StoppedSpeedThreshold)
                return; 

            var selfDir = selfVelocity.normalized;
            var playerDir = playerVelocity.normalized;
            var alignment = Vector2.Dot(selfDir, playerDir);

            if (alignment < OppositeDirectionDotThreshold)
            {
                _avoiding.Remove(brain.Agent.PlayerId);
                return;
            }

            RunAwayFrom(brain, playerToAvoid.PlayerId, [(Vector2)playerToAvoid.transform.position]);
        }

        private void AvoidMultiplePlayersLogic(StructuredAgentBrain brain, List<PlayerControl> playersToAvoid)
        {
            var closestThreat = playersToAvoid
                .OrderBy(p => Vector2.Distance(brain.Agent.transform.position, p.transform.position))
                .First();

            if (_avoiding.TryGetValue(brain.Agent.PlayerId, out var currentTarget) && currentTarget == closestThreat.PlayerId)
                return;

            var threatPositions = playersToAvoid.Select(p => (Vector2)p.transform.position).ToList();
            RunAwayFrom(brain, closestThreat.PlayerId, threatPositions);
        }

        private void RunAwayFrom(StructuredAgentBrain brain, byte trackedPlayerId, List<Vector2> threatPositions)
        {
            var destination = FindFarthestWaypoint(threatPositions);
            if (destination == null) return;

            var selfPos = brain.WaypointPosition;
            var path = Pathfinder.FindPath(selfPos, destination, out float _);
            if (path == null || path.Count == 0) return;

            brain.CommandGoToPath(path);
            _avoiding[brain.Agent.PlayerId] = trackedPlayerId;
        }

        private Waypoint FindFarthestWaypoint(List<Vector2> threatPositions)
        {
            Waypoint best = null;
            var bestMinDistance = float.MinValue;

            foreach (var waypoint in WaypointManager.AllWaypoints)
            {
                var minDistanceToThreat = threatPositions.Min(t => Vector2.Distance(waypoint.Position, t));
                if (minDistanceToThreat <= bestMinDistance) continue;

                bestMinDistance = minDistanceToThreat;
                best = waypoint;
            }

            return best;
        }
    }
}