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
        public Waypoint WaypointPosition => Pathfinder.GetClosestNode(Vector2Position);
        public bool IsCrewmate => !myAgent.Data.Role.IsImpostor;
        public bool IsImpostor => myAgent.Data.Role.IsImpostor;
        

        public bool CanReportBody(Vector2 bodyPosition)
        {
            float dist = Vector2.Distance(myAgent.transform.position, bodyPosition);
            return dist < 3.4f;
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

        public float GetReactionTime()
        {
            if (delayDisturb <= 0f)
                return delayTime;

            float minDelay = Mathf.Max(0.05f, delayTime - (delayDisturb * 0.5f));

            float maxDelay = delayTime + delayDisturb;

            return RandomizerExtensions.GetSecureRandomFloat(minDelay, maxDelay);
        }

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
