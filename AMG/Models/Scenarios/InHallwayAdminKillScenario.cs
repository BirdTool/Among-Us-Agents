using AMG.AI.Mind.StructuredAgentBrain;
using AMG.AI.Mind.StructuredAgentBrain.Plans;
using AMG.Interfaces;
using AMG.Utilities;
using Il2CppSystem.Reflection;

namespace AMG.Models.Scenarios
{
    public class InHallwayAdminKillScenario : IScenario
    {
        public float CalculateScore(StructuredAgentBrain brain)
        {
            if (SimpleChecks(brain)) return 0;

            float score = 0;
            return score; // TODO: Implement the logic
        }

        // Return true if the simple checks fails, making the scenario invalid
        private bool SimpleChecks(StructuredAgentBrain brain)
        {
            if (!brain.IsImpostor) return true;
            if (brain.Agent.Data.IsDead) return true;
            if (brain.Agent.killTimer > 0f || !KillCooldownManager.CanKill(brain.Agent.PlayerId)) return true;

            return false;
        }

        public AgentPlanManager GeneratePlan(StructuredAgentBrain brain)
        {
            throw new System.NotImplementedException();
        }
    }
}