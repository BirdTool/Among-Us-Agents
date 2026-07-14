using AMG.AI.Navigation;
using AMG.Interfaces;
using AMG.Utilities;
using System.Collections.Generic;
using UnityEngine;

// TODO: Some locations are wrong, need to fix them.
// TODO: The agents are doing the sabotages too fast, and they need to be smart, if someone is already doing that sabotage, so it needs to do another step.

namespace AMG.AI.Mind.Decisions.Sabotages
{
    public class SkeldReactorSabotagedStep(List<Waypoint> locations) : GenericSabotageStepBothSides(locations) { }
    public class SkeldO2SabotagedStep(List<Waypoint> locations) : GenericSabotageStep(locations) { }
    public class SkeldLightsSabotagedStep(List<Waypoint> locations) : GenericSabotageStep(locations) { }
    public class SkeldCommsSabotagedStep(List<Waypoint> locations) : GenericSabotageStep(locations) { }

    // -- //

    public class SkeldReactorSabotage : ISabotage
    {
        public bool StepComplete { get; } = true;
        
        private readonly List<SabotageStep> _steps = [
            new SkeldReactorSabotagedStep([new Vector2(-14.3f, -5.2f).GetClosestNode()]),
            new SkeldReactorSabotagedStep([new Vector2(-14.3f, -7.4f).GetClosestNode()])
        ];

        public List<SabotageStep> GetSteps() => _steps;
    }

    public class SkeldO2Sabotage : ISabotage
    {
        public bool StepComplete { get; } = false;

        private readonly List<SabotageStep> _steps = [
            new SkeldO2SabotagedStep([new Vector2(6.2f, -7.1f).GetClosestNode()]),
            new SkeldO2SabotagedStep([new Vector2(6.5f, -2.5f).GetClosestNode()])
        ];

        public List<SabotageStep> GetSteps() => _steps;
    }

    public class SkeldLightsSabotage : ISabotage
    {
        public bool StepComplete { get; } = false;

        private readonly List<SabotageStep> _steps = [
            new SkeldLightsSabotagedStep([new Vector2(-7.2f, -8.3f).GetClosestNode()])
        ];

        public List<SabotageStep> GetSteps() => _steps;
    }

    public class SkeldCommsSabotage : ISabotage
    {
        public bool StepComplete { get; } = false;

        private readonly List<SabotageStep> _steps = [
            new SkeldCommsSabotagedStep([new Vector2(4.3f, -15.5f).GetClosestNode()])
        ];

        public List<SabotageStep> GetSteps() => _steps;
    }
}