using AMG.AI.Mind;
using AMG.AI.Navigation;
using System.Collections.Generic;

namespace AMG.Interfaces
{
    public interface ISabotage
    {
        bool StepComplete { get; } // true if need two players in both sides, false if one player can go to both sides complete it
        List<SabotageStep> GetSteps();
    }

    public abstract class SabotageStep
    {
        public virtual bool IsCompleted { get; set; } = false;
        public abstract List<Waypoint> Locations { get; }
        public virtual void CompleteStep(AgentBrain brain) { }
    }
}
