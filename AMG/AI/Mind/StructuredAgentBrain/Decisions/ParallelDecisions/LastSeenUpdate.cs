using System.Collections.Generic;
using AMG.Interfaces;
using AMG.Utilities;

namespace AMG.AI.Mind.StructuredAgentBrain.Decisions.ParallelDecisions
{
    public class LastSeenUpdate : IParallelDecision
    {
        private readonly float _updateInterval = 0.3f;
        private readonly Dictionary<byte, float> _lastUpdateTimes = [];

        public void Evaluate(StructuredAgentBrain brain)
        {
            var currentTime = Utils.SecondsSinceShipStart.Value;
            var lastUpdate = _lastUpdateTimes.GetValueOrDefault(brain.Agent.PlayerId, 0f);
            if (lastUpdate + _updateInterval > currentTime) return;
            _lastUpdateTimes[brain.Agent.PlayerId] = currentTime;

            var players = brain.NearbyPlayersInVision;
            foreach (var player in players)
            {
                var memory = brain.GetOrCreateMemory(player.PlayerId);
                memory.UpdateLastSeen(currentTime, player.transform.position);
            }
        }
    }
}