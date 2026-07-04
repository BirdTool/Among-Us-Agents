using AMG.AI.Mind.Decisions.MainDecisions;
using AMG.AI.Mind.Decisions.ParallelDecisions;
using AMG.Interfaces;
using System.Collections.Generic;

namespace AMG.AI.Mind.Decisions
{
    internal static class DecisionsGroup
    {
        public static List<IMainDecision> AllMainDecisions { get; } = [
                new TaskDecision()
            ];
        public static List<IParallelDecision> AllParallelMainDecisions { get; } = [
                new SawABodyPLDecision()
            ];
    }
}
