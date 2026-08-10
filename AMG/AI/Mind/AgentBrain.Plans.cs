using AMG.AI.Mind.Plans;
using AMG.Interfaces;

namespace AMG.AI.Mind
{
    public partial class AgentBrain
    {
        public AgentPlanManager PlanManager = null;

        public void AddPlan(IPlan plan)
        {
            PlanManager ??= new AgentPlanManager(this);
            PlanManager.AddPlan(plan);
        }
    }
}