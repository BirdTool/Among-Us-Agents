using System.Collections.Generic;
using System.Linq;
using AMG.AI.Navigation;
using AMG.Enums.AgentEnums;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Mind.StructuredAgentBrain
{
    public partial class StructuredAgentBrain
    {
        public float ReactionTime => Utils.GetDisturbTime(delayTime, delayDisturb);
        public float AvoidPathsUntil = 0f;

        public void SetState(AgentState newState)
        {
            if (currentState != newState)
            {
                LogManager.LogDebug($"[STATE] Mudando estado de {currentState} para {newState}");
                currentState = newState;
                _updateTags.TryGetValue(newState, out var tag);
                if (tag != null)
                {
                    ReplaceNameTag(tag);
                }
            }
        }

        public bool PathCrossesAvoidedRoom(List<Waypoint> path)
        {
            if (Time.time >= AvoidPathsUntil || path == null) return false;

            var rooms = Intentions.AvoidRooms;
            if (!rooms.Any()) return false;

            foreach (var w in path)
            {
                foreach (var room in rooms)
                {
                    if (w.Room == room) return true;
                }
            }
            return false;
        }
    }
}
