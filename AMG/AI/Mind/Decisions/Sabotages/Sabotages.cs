using AMG.AI.Navigation;
using AMG.Interfaces;
using AMG.Utilities;
using System.Collections.Generic;

namespace AMG.AI.Mind.Decisions.Sabotages
{
    public abstract class GenericSabotageStepBothSides(List<Waypoint> locations) : SabotageStep
    {
        public override List<Waypoint> Locations { get; } = locations;

        public override bool IsCompleted
        {
            get
            {
                if (base.IsCompleted) return true;
                
                // If a real player is holding this console, we consider it temporarily completed
                foreach (var loc in Locations)
                {
                    if (Utils.Sabotages.IsPlayerOccupyingLocation(loc.Position))
                        return true;
                }
                
                return false;
            }
            set => base.IsCompleted = value;
        }

        public override void CompleteStep(AgentBrain brain)
        {
            if (base.IsCompleted) return;
            base.IsCompleted = true; 

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

        public override bool IsCompleted
        {
            get
            {
                if (base.IsCompleted) return true;
                
                // Also check if someone is physically fixing it right now
                foreach (var loc in Locations)
                {
                    if (Utils.Sabotages.IsPlayerOccupyingLocation(loc.Position))
                        return true;
                }
                
                return false;
            }
            set => base.IsCompleted = value;
        }

        public override void CompleteStep(AgentBrain brain)
        {
            if (base.IsCompleted) return;
            base.IsCompleted = true;

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