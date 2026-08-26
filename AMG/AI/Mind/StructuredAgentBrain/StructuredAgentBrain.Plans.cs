using AMG.AI.Mind.StructuredAgentBrain.Plans;
using AMG.Interfaces;

namespace AMG.AI.Mind.StructuredAgentBrain
{
    public partial class StructuredAgentBrain
    {
        public AgentPlanManager PlanManager = null;

        public void AddPlan(IPlan plan)
        {
            PlanManager ??= new AgentPlanManager(this);
            PlanManager.AddPlan(plan);
        }
    }
}