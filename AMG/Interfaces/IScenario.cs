using AMG.AI.Mind.StructuredAgentBrain;
using AMG.AI.Mind.StructuredAgentBrain.Plans;

namespace AMG.Interfaces
{
    public interface IScenario
    {
        public float CalculateScore(StructuredAgentBrain brain);
        public AgentPlanManager GeneratePlan(StructuredAgentBrain brain);
    }
}