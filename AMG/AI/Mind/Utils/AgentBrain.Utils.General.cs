using AMG.AI.Navigation;
using AMG.Enums.AgentEnums;
using AMG.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace AMG.AI.Mind
{
    public partial class AgentBrain
    {
        public bool IsDead => myAgent.Data.IsDead;
        public Vector2 Vector2Position => myAgent.transform.position;
        public bool IsCrewmate => !myAgent.Data.Role.IsImpostor;
        public bool IsImpostor => myAgent.Data.Role.IsImpostor;

        // Cache for WaypointPosition — re-computed only when agent moves > 0.3 units
        private Waypoint _cachedWaypointPosition;
        private Vector2 _lastWaypointCachePosition = new(float.MinValue, float.MinValue);
        private const float WAYPOINT_CACHE_THRESHOLD = 0.3f;

        public byte AgentId => myAgent.PlayerId;

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
        public List<RoundDeadBody> NearbyBodiesInVision => AgentPerception.GetNearbyDeadBodiesInVision(myAgent);
        public List<PlayerControl> NearbyPlayers => AgentPerception.GetNearbyPlayers(Vector2Position, 6.5f);
        public List<PlayerControl> NearbyPlayersInVision => AgentPerception.GetNearbyPlayersInVision(myAgent);

        public float ReactionTime => Utils.GetDisturbTime(delayTime, delayDisturb);

        public void SetState(AgentState newState)
        {
            if (currentState != newState)
            {
                currentState = newState;
                _updateTags.TryGetValue(newState, out var tag);
                if (tag != null)
                {
                    ReplaceNameTag(tag);
                }
            }
        }
    }
}
