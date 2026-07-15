using AMG.AI.Mind;
using AMG.AI.Navigation;
using AMG.Utilities;
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
        public virtual float TimeToFix { get; set; } = 2f;
        public virtual bool IsCompleted { get; set; } = false;
        public abstract List<Waypoint> Locations { get; }
        
        public virtual void CompleteStep(AgentBrain brain) 
        {
            if (IsCompleted) return;
            IsCompleted = true;

            var sabotage = Utils.CurrentSabotage;
            sabotage?.CompleteSabotage(ShipStatus.Instance);
        }
    }
}