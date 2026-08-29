using AMG.AI.Mind;
using AMG.AI.Mind.StructuredAgentBrain;

namespace AMG.Interfaces
{
    public interface IMainDecision
    {
        float CalculateUtility(StructuredAgentBrain brain);
        bool Execute(StructuredAgentBrain brain);
    }
}
