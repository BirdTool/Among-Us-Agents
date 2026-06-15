using AMG.AI.Navigation;
using AMG.AI.Tools;
using AMG.Utilities;
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

            bool hasReachedDestination = ProcessPathMovement();

            if (hasReachedDestination)
            {
                currentPath = null;
                currentPathIndex = 0;
                myAgent.MyPhysics.body.velocity = Vector2.zero;
            }
        }

        private void CalculateSmartPath()
        {
            var agentPosition = Pathfinder.GetClosestNode(myAgent.transform.position);
            var randomWaypoint = WaypointManager.AllWaypoints.GetRandomItemSecureOrDefault();
            var path = Pathfinder.FindPath(agentPosition, randomWaypoint, out float dist);
            var emergencyBreak = 0;

            while (dist < 10f && emergencyBreak < 500)
            {
                randomWaypoint = WaypointManager.AllWaypoints.GetRandomItemSecureOrDefault();
                path = Pathfinder.FindPath(agentPosition, randomWaypoint, out dist);
                emergencyBreak++;
            }

            currentPath = path;
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
