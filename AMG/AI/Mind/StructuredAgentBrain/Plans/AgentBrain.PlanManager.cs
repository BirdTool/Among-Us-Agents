using System.Collections.Generic;
using AMG.Interfaces;

namespace AMG.AI.Mind.StructuredAgentBrain.Plans
{
    public class AgentPlanManager(StructuredAgentBrain brain)
    {
        private readonly StructuredAgentBrain _brain = brain;

        public readonly List<IPlan> QueuePlans = [];

        public void AddPlan(IPlan plan)
        {
            QueuePlans.Add(plan);
        }

        public void ClearPlans()
        {
            QueuePlans.Clear();
        }

        public void Execute() {
            if (QueuePlans.Count == 0) return;

            _brain.updateAction = new Models.AgentUpdateAction(() => {
                if (QueuePlans.Count == 0) return true;
                
                var plan = QueuePlans[0];
                plan.Execute(_brain);

                if (plan.IsDone)
                {
                    QueuePlans.RemoveAt(0);
                }

                return false;
            });
        }
    }
}
