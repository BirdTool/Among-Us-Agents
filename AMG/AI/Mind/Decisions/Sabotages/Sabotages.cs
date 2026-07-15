using AMG.AI.Navigation;
using AMG.Interfaces;
using AMG.Utilities;
using System.Collections.Generic;

namespace AMG.AI.Mind.Decisions.Sabotages
{
    public abstract class GenericSabotageStepBothSides(List<Waypoint> locations) : SabotageStep
    {
        public override List<Waypoint> Locations { get; } = locations;

        public override void CompleteStep(AgentBrain brain)
        {
            if (IsCompleted) return;
            IsCompleted = true; 

            var sabotage = Utils.CurrentSabotage;
            
            bool allDone = true;
            foreach (var step in sabotage.GetSteps())
            {
                if (!step.IsCompleted) allDone = false;
            }

            if (allDone)
            {
                sabotage.CompleteSabotage(ShipStatus.Instance);
            }
        }
    }

    public abstract class GenericSabotageStep(List<Waypoint> locations) : SabotageStep
    {
        public override List<Waypoint> Locations { get; } = locations;

        public override void CompleteStep(AgentBrain brain)
        {
            if (IsCompleted) return;
            IsCompleted = true;

            var sabotage = Utils.CurrentSabotage;
            
            bool allDone = true;
            foreach (var step in sabotage.GetSteps())
            {
                if (!step.IsCompleted) allDone = false;
            }

            if (allDone)
            {
                sabotage.CompleteSabotage(ShipStatus.Instance);
            }
        }
    }
}