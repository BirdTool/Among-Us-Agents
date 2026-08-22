using AMG.AI.Mind;
using AMG.AI.Navigation;
using AMG.Enums;
using AMG.Enums.AgentEnums;
using AMG.Interfaces;
using AMG.Utilities;
using UnityEngine;

namespace AMG.Models.Plans
{
    public class TravelVentPlan(Vent ventToEnter, SystemTypes targetRoom, bool avoidWitnesses = true) : IPlan
    {
        private readonly Vent _ventToEnter = ventToEnter;
        private readonly SystemTypes _targetRoom = targetRoom;
        private readonly bool _avoidWitnesses = avoidWitnesses;

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
            brain.TargetRoomForVent = _targetRoom;
            brain.AvoidWitnessesWhenVenting = _avoidWitnesses;

            StartedAt = Time.time;
            IsRunning = true;
            
            if (!brain.tempParallelDecisions.Exists(d => d.ID == TempParallelDecisionsIDsEnum.TravelVentPlan))
            {
                brain.tempParallelDecisions.Add(new AgentTempParallelDecision(TempParallelDecisionsIDsEnum.TravelVentPlan, () => {
                    if (brain.AgentControl.inVent)
                    {
                        brain.SetState(AgentState.InVent);
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
