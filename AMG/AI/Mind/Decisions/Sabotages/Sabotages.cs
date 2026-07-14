using AMG.AI.Navigation;
using AMG.Interfaces;
using AMG.Utilities;
using System.Collections.Generic;

namespace AMG.AI.Mind.Decisions.Sabotages
{
    public abstract class GenericSabotageStepBothSides : SabotageStep
    {
        public override List<Waypoint> Locations { get; }

        protected GenericSabotageStepBothSides(List<Waypoint> locations)
        {
            Locations = locations;
        }

        public override void CompleteStep(AgentBrain brain)
        {
            var sabotage = Utils.CurrentSabotage;
            
            // Reator/Sísmica precisa que fiquem segurando. Alternamos o estado!
            if (sabotage.StepComplete) IsCompleted = !IsCompleted;

            if (IsCompleted) return;
            IsCompleted = true;
            
            sabotage.CompleteSabotage(ShipStatus.Instance);
        }
    }

    public abstract class GenericSabotageStep : SabotageStep
    {
        public override List<Waypoint> Locations { get; }

        protected GenericSabotageStep(List<Waypoint> locations)
        {
            Locations = locations;
        }

        public override void CompleteStep(AgentBrain brain)
        {
            // O2, Luzes e Comms resolvem de uma vez
            if (IsCompleted) return;
            IsCompleted = true;

            var sabotage = Utils.CurrentSabotage;
            sabotage.CompleteSabotage(ShipStatus.Instance);
        }
    }
}