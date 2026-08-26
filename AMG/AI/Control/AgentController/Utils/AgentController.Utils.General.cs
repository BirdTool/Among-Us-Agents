using System.Collections.Generic;
using AMG.AI.Navigation;
using AMG.Enums.AgentEnums;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Control.AgentController
{
    public partial class AgentController
    {
        public bool IsDead => Agent.Data.IsDead;
        public Vector2 Vector2Position => Agent.transform.position;
        public bool IsCrewmate => !Agent.Data.Role.IsImpostor;
        public bool IsImpostor => Agent.Data.Role.IsImpostor;

        // Cache for WaypointPosition — re-computed only when agent moves > 0.3 units
        protected Waypoint _cachedWaypointPosition;
        protected Vector2 _lastWaypointCachePosition = new(float.MinValue, float.MinValue);
        public bool IsAuthorizedToVote { get; set; } = false;
        protected const float WAYPOINT_CACHE_THRESHOLD = 0.3f;

        public byte AgentId => Agent.PlayerId;

        public Waypoint WaypointPosition
        {
            get
            {
                if (Vector2.Distance(Vector2Position, _lastWaypointCachePosition) > WAYPOINT_CACHE_THRESHOLD)
                {
                    _cachedWaypointPosition = Pathfinder.GetClosestNode(Vector2Position);
                    _lastWaypointCachePosition = Vector2Position;
                }
                return _cachedWaypointPosition;
            }
        }

        public List<RoundDeadBody> NearbyBodies => AgentPerception.GetNearbyDeadBodies(Vector2Position, 6f);
        public List<RoundDeadBody> NearbyBodiesInVision => AgentPerception.GetNearbyDeadBodiesInVision(Agent);
        public List<PlayerControl> NearbyPlayers => AgentPerception.GetNearbyPlayers(Vector2Position, 6.5f);
        public List<PlayerControl> NearbyPlayersInVision => AgentPerception.GetNearbyPlayersInVision(Agent);
    }
}