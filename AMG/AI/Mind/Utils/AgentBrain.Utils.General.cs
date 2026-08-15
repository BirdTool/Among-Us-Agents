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

        public List<RoundDeadBody> GetNearbyBodies()
        {
            List<RoundDeadBody> nearbyBodies = [];

            if (!sawABody && Utils.Round.CurrentRoundDeadBodies != null && Utils.Round.CurrentRoundDeadBodies.Count > 0)
            {
                List<RoundDeadBody> bodies = Utils.Round.CurrentRoundDeadBodies;

                foreach (var body in bodies)
                {
                    var origin = myAgent.transform.position;
                    var target = body.Position;

                    Vector2 origin2D = new(origin.x, origin.y + 0.5f);

                    float distToBody = Vector2.Distance(origin2D, target);
                    if (distToBody > 5f) continue;

                    var canSee = Utils.CanSeeTheTarget(origin2D, target, distToBody);
                    if (canSee) nearbyBodies.Add(body);
                }
            }

            return nearbyBodies;
        }

        public float GetReactionTime() => Utils.GetDisturbTime(delayTime, delayDisturb);

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

        public List<PlayerControl> GetNearbyPlayers()
        {
            List<PlayerControl> nearbyPlayers = [];

            foreach (var player in Utils.Players.AllAlivePlayerNotMe)
            {
                var origin = myAgent.transform.position;
                var target = player.transform.position;

                Vector2 origin2D = new(origin.x, origin.y + 0.5f);

                float distToBody = Vector2.Distance(origin2D, target);
                if (distToBody > 6.5f) continue;

                var canSee = Utils.CanSeeTheTarget(origin2D, target, distToBody);
                if (canSee) nearbyPlayers.Add(player);
            }

            return nearbyPlayers;
        }
    }
}
