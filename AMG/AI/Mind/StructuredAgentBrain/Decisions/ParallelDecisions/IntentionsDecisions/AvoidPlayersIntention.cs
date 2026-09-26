using System.Collections.Generic;
using System.Linq;
using AMG.AI.Navigation;
using AMG.AI.Tools;
using AMG.Interfaces;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Mind.StructuredAgentBrain.Decisions.ParallelDecisions.IntentionsDecisions
{
    public class AvoidPlayersIntention : IParallelDecision
    {
        private readonly Dictionary<byte, byte> _avoiding = []; // Brain, player to avoid

        public void Evaluate(StructuredAgentBrain brain)
        {
            if (!brain.Intentions.AvoidPlayers.Any()) return;

            var nearby = brain.NearbyPlayersInVision;
            var playersToAvoid = nearby.Where(p => brain.Intentions.AvoidPlayers.Contains(p.PlayerId)).ToList();

            if (!playersToAvoid.Any()) return;

            
        }
    }
}