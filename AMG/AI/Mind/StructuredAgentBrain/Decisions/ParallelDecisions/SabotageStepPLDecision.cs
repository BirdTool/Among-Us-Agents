using System.Collections.Generic;
using System.Linq;
using AMG.AI.Navigation;
using AMG.Interfaces;
using AMG.Utilities;
using AMG.Enums.AgentEnums;
using Utils = AMG.Utilities.Utils;
using Il2CppSystem.Linq.Expressions;
using Il2CppSystem.Security.Cryptography;
using UnityEngine.Diagnostics;
using Il2CppSystem.Buffers;
using AMG.AI.Control;

namespace AMG.AI.Mind.StructuredAgentBrain.Decisions.ParallelDecisions
{
    internal class SabotageStepPLDecision : IParallelDecision
    {
        public void Evaluate(StructuredAgentBrain brain)
        {
            if (!Utils.IsAnySabotageActive || brain.IsDead) return;
            if (brain.currentState == AgentState.FixingSabotage) return;

            var sabotage = Utils.CurrentSabotage;
            if (sabotage == null) return;

            var steps = sabotage.GetSteps();
            if (steps.Count < 1) return;

            var start = Pathfinder.GetClosestNode(brain.Agent.transform.position);
            if (start == null) return;

            SabotageStep bestStep = null;
            float minDistance = float.MaxValue;
            List<Waypoint> bestPath = null;

            foreach (var step in steps.Where(s => !s.IsCompleted))
            {
                var location = step.Locations.GetRandomItemSecureOrDefault();
                if (location == null) continue;

                var path = Pathfinder.FindPath(start, location, out float dist);
                if (path == null) continue;

                var allBrains = AMG.Utilities.Utils.GetAllAgentController();
                foreach (var otherBrain in allBrains)
                {
                    if (otherBrain == brain || otherBrain.Agent.Data.IsDead) continue;
                    
                    if (otherBrain is StructuredAgentBrain strBrain)
                    {
                        if (strBrain.currentSabotageStep == step)
                        {
                            dist += 25f; 
                        }
                    }
                }

                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestStep = step;
                    bestPath = path;
                }
            }

            if (bestStep == null || brain.currentSabotageStep == bestStep) return;

            brain.currentSabotageStep = bestStep;
            brain.isGoingToFixASabotage = true;
            brain.CommandGoToPath(bestPath);
        }
    }
}