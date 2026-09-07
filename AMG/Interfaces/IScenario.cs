using AMG.AI.Mind.StructuredAgentBrain;
using AMG.AI.Mind.StructuredAgentBrain.Plans;

namespace AMG.Interfaces
{
    public interface IScenario
    {
        public float CheckTime { get; set; }
        public float LastCheckTime { get; set; }

        public float CalculateScore(StructuredAgentBrain brain);
        public AgentPlanManager GeneratePlan(StructuredAgentBrain brain);
    }
}