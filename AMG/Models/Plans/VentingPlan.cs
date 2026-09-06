using System;
using AMG.AI.Mind.StructuredAgentBrain;
using AMG.AI.Navigation;
using AMG.Enums;
using AMG.Interfaces;
using AMG.Utilities;
using UnityEngine;

namespace AMG.Models.Plans
{
    public class VentingPlan(Vent ventToEnter, Func<bool> InVentAction) : IPlan
    {
        private readonly Vent _ventToEnter = ventToEnter;
        private readonly Func<bool> _InVentAction = InVentAction;

        public bool IsDone { get; set; } = false;
        public bool IsRunning { get; private set; } = false;
        public float StartedAt { get; private set; } = 0;
        
        public void Execute(StructuredAgentBrain brain)
        {
            if (IsRunning) return;
            
            Vector2 ventLocation = _ventToEnter.transform.position;
            var path = Pathfinder.FindPath(brain.WaypointPosition, ventLocation.GetClosestNode(), out float _);
            brain.CommandGoToPath(path);
            brain.currentVentToEnter = _ventToEnter;

            StartedAt = Time.time;
            IsRunning = true;
            
            if (!brain.tempParallelDecisions.Exists(d => d.ID == TempParallelDecisionsIDsEnum.VentingPlan))
            {
                brain.tempParallelDecisions.Add(new AgentTempParallelDecision(TempParallelDecisionsIDsEnum.VentingPlan, () => {
                    if (brain.Agent.inVent)
                    {
                        IsDone = true;
                        IsRunning = false;
                        return true;
                    }
                    if (Time.time - StartedAt > 15)
                    {
                        IsDone = true;
                        IsRunning = false;
                        return true;
                    }
                    return false;
                })
                {
                    DeleteOnMeeting = true,
                    SecondsTimeLimit = 16
                });
            }

            brain.InVentLogic = _InVentAction;
        }
    }
}
