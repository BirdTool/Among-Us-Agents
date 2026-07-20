using AMG.AI.Navigation;
using AMG.Interfaces;
using AMG.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace AMG.AI.Mind.Decisions.Sabotages
{
    public class SkeldReactorSabotagedStep(List<Waypoint> locations) : GenericSabotageStepBothSides(locations) { }
    public class SkeldO2SabotagedStep(List<Waypoint> locations, int consoleId) : GenericSabotageStep(locations) 
    { 
        public int ConsoleId { get; } = consoleId;
        
        public override bool IsCompleted
        {
            get
            {
                if (base.IsCompleted) return true;
                
                // For O2, checking distance isn't enough because players walk away after fixing
                // So we check the actual system state if it has been fixed
                if (Utils.Sabotages.IsO2ConsoleCompleted(ShipStatus.Instance, ConsoleId))
                {
                    return true;
                }
                
                return false;
            }
            set => base.IsCompleted = value;
        }
    }
    public class SkeldLightsSabotagedStep(List<Waypoint> locations) : GenericSabotageStep(locations) { }
    public class SkeldCommsSabotagedStep(List<Waypoint> locations) : GenericSabotageStep(locations) { }

    // -- //

    public class SkeldReactorSabotage : ISabotage
    {
        public bool StepComplete { get; } = true;

        private readonly List<SabotageStep> _steps = [
            new SkeldReactorSabotagedStep([new Vector2(-21.220f, -1.688f).GetClosestNode()]) { TimeToFix = 1.85f }, // up
            new SkeldReactorSabotagedStep([new Vector2(-21.3464f, -8.617f).GetClosestNode()]) { TimeToFix = 1.85f } // down
        ];

        public List<SabotageStep> GetSteps() => _steps;
    }

    public class SkeldO2Sabotage : ISabotage
    {
        public bool StepComplete { get; } = false;

        private readonly List<SabotageStep> _steps = [
            new SkeldO2SabotagedStep([new Vector2(6.804f, -3.03f).GetClosestNode()], 0) { TimeToFix = 3.68f }, // O2
            new SkeldO2SabotagedStep([new Vector2(6.565f, -6.754f).GetClosestNode()], 1) { TimeToFix = 3.68f } // Admin
        ];

        public List<SabotageStep> GetSteps() => _steps;
    }

    public class SkeldLightsSabotage : ISabotage
    {
        public bool StepComplete { get; } = false;

        private readonly List<SabotageStep> _steps = [
            new SkeldLightsSabotagedStep([
                new Vector2(-9.894f, -10.238f).GetClosestNode(),
                new Vector2(-9.399f, -10.233f).GetClosestNode()
            ]) { TimeToFix = 4.78f }
        ];

        public List<SabotageStep> GetSteps() => _steps;
    }

    public class SkeldCommsSabotage : ISabotage
    {
        public bool StepComplete { get; } = false;

        private readonly List<SabotageStep> _steps = [
            new SkeldCommsSabotagedStep([
                new Vector2(4.899f, -16.418f).GetClosestNode(),
                new Vector2(4.185f, -16.380f).GetClosestNode(),
                new Vector2(3.365f, -16.542f).GetClosestNode()
            ]) { TimeToFix = 5.90f }
        ];

        public List<SabotageStep> GetSteps() => _steps;
    }
}