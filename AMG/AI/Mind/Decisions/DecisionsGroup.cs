using AMG.AI.Mind.Decisions.MainDecisions;
using AMG.Interfaces;
using System.Collections.Generic;

namespace AMG.AI.Mind.Decisions
{
    internal static class DecisionsGroup
    {
        public static List<IMainDecision> AllMainDecisions { get; } = [
                new TaskDecision()
            ];
        public static List<IParallelDecision> ParallelMainDecisions { get; } = [];
    }
}
