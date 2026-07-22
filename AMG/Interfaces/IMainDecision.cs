using AMG.AI.Mind;

namespace AMG.Interfaces
{
    public interface IMainDecision
    {
        float CalculateUtility(AgentBrain brain);
        bool Execute(AgentBrain brain);
    }
}
