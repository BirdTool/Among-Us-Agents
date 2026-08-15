using System.Collections.Generic;
using System.Linq;
using AMG.AI.Tools;
using AMG.Interfaces;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Mind.Decisions.ParallelDecisions
{
    internal class DoorClosedPLDecision : IParallelDecision
    {
        private readonly Dictionary<byte, float> _checkTimers = [];
        private const float CHECK_COOLDOWN = 0.25f;

        public void Evaluate(AgentBrain brain)
        {
            var id = brain.AgentControl.PlayerId;
            if (!_checkTimers.ContainsKey(id)) _checkTimers.Add(id, Time.time);
            
            if (Time.time - _checkTimers[id] < CHECK_COOLDOWN + brain.ReactionTime) return;
            _checkTimers[id] = Time.time;
            
            var path = brain.currentPath;
            if (path == null || brain.currentPathIndex >= path.Count) return;
            
            int validSteps = 7;
            bool hasAClosedDoor = false;
            
            for (int i = brain.currentPathIndex; i < path.Count; i++)
            {
                if (validSteps < 1) break;
                var waypoint = path.ElementAtOrDefault(i);
                var nextpoint = path.ElementAtOrDefault(i + 1);
                if (waypoint == null || nextpoint == null) break;

                if (waypoint.Room != nextpoint.Room && Utils.IsRoomClosed(nextpoint.Room))
                {
                    hasAClosedDoor = true;
                    break;
                }
                validSteps--;
            }

            if (hasAClosedDoor)
            {
                brain.ResetPath(true);
            }
        }
    }
}