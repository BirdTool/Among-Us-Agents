using System.Collections.Generic;
using AMG.Enums.AgentEnums;
using AMG.Interfaces;
using UnityEngine;

namespace AMG.AI.Mind.StructuredAgentBrain.Decisions.ParallelDecisions.IntentionsDecisions
{
    public class AvoidBeAloneIntention : IParallelDecision
    {
        private const float CheckTime = 1f;
        private readonly Dictionary<byte, float> _timeSinceLastCheck = [];
        private readonly Dictionary<byte, bool> _isWandering = [];
        
        public void Evaluate(StructuredAgentBrain brain)
        {
            if (_isWandering.ContainsKey(brain.AgentId) && _isWandering[brain.AgentId])
            {
                if (brain.NearbyPlayersInVision.Count > 0)
                {
                    _isWandering[brain.AgentId] = false;
                    _timeSinceLastCheck.Remove(brain.AgentId);
                }
                return;
            }

            if (!brain.Intentions.AvoidBeAlone) return;

            if (!_timeSinceLastCheck.ContainsKey(brain.AgentId)) _timeSinceLastCheck.Add(brain.AgentId, 0);
            var now = Time.time;
            var isOver = (now - _timeSinceLastCheck[brain.AgentId]) > CheckTime;
            
            if (isOver) 
                _timeSinceLastCheck[brain.AgentId] = now;
            else 
                return;

            var isAlone = brain.NearbyPlayersInVision.Count == 0;
            if (!isAlone) return;

            if (!CanBeInterrupted(brain.currentState)) return;

            brain.SetState(AgentState.Wandering);
            _isWandering.Add(brain.AgentId, true);
        }

        private static bool CanBeInterrupted(AgentState state) => state switch
        {
            AgentState.Navigating or AgentState.Calculating or AgentState.Wandering or
            AgentState.SmartWandering or AgentState.Stopped or AgentState.Observing => true,
            _ => false
        };
    }
}