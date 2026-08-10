using AMG.AI.Mind;
using AMG.AI.Navigation;
using AMG.Enums;
using AMG.Interfaces;
using AMG.Utilities;
using UnityEngine;

namespace AMG.Models.Plans
{
    public class VentingPlan(Vent ventToEnter) : IPlan
    {
        private readonly Vent _ventToEnter = ventToEnter;

        public bool IsDone { get; set; } = false;
        public bool IsRunning { get; private set; } = false;
        public float StartedAt { get; private set; } = 0;
        
        public void Execute(AgentBrain brain)
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
                    if (brain.AgentControl.inVent)
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
        }
    }
}
