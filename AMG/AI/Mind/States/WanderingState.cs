using AMG.AI.Navigation;
using AMG.AI.Tools;
using AMG.Enums.AgentEnums;
using AMG.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace AMG.AI.Mind
{
    public partial class AgentBrain
    {
        private Vector2 currentDirection = Vector2.zero;
        private float directionChangeTimer = 0f;

        private void UpdateWandering()
        {
            directionChangeTimer -= Time.deltaTime;
            if (directionChangeTimer <= 0f) ChangeRandomDirection();

            Vector2 velocity = currentDirection * speed;
            myAgent.MyPhysics.body.velocity = velocity;
            FlipSprite(currentDirection);

            ReplaceNameTag(DefaultTags.States.Wandering);
        }

        private void UpdateSmartWandering()
        {
            ReplaceNameTag(DefaultTags.States.SmartWandering);

            if (currentPath == null || currentPathIndex >= currentPath.Count)
            {
                CalculateSmartPath();
                return;
            }

            bool? hasReachedDestination = ProcessPathMovement();

            if (hasReachedDestination == true || hasReachedDestination == null)
            {
                currentPath = null;
                currentPathIndex = 0;
                myAgent.MyPhysics.body.velocity = Vector2.zero;
                SetState(AgentState.Calculating);
            }
        }

        private void CalculateSmartPath()
        {
            var agentWaypoint = Pathfinder.GetClosestNode(myAgent.transform.position);
            if (agentWaypoint == null) return;

            Vector2 agentPos = myAgent.transform.position;

            // Pre-filter by straight-line distance — cheap O(n), no A* needed here.
            // Prefer waypoints that are at least 10 units away so the agent gets a meaningful wander target.
            Waypoint target = null;
            var allWaypoints = WaypointManager.AllWaypoints;

            // Build a small candidate list of far-away waypoints and pick one at random.
            var farCandidates = new List<Waypoint>(allWaypoints.Count / 2);
            foreach (var wp in allWaypoints)
            {
                if (Vector2.Distance(agentPos, wp.Position) >= 10f)
                    farCandidates.Add(wp);
            }

            target = farCandidates.Count > 0
                ? farCandidates.GetRandomItemSecureOrDefault()
                : allWaypoints.GetRandomItemSecureOrDefault();

            if (target == null) return;

            currentPath = Pathfinder.FindPath(agentWaypoint, target, out _);
            currentPathIndex = 0;
        }

        private void ChangeRandomDirection()
        {
            float angle = UnityEngine.Random.Range(0f, 360f);
            currentDirection = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            directionChangeTimer = UnityEngine.Random.Range(1.2f, 3.5f);
        }
    }
}
