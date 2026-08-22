using AMG.AI.Mind;
using AMG.AI.Navigation;
using AMG.Enums;
using AMG.Enums.AgentEnums;
using AMG.Interfaces;
using AMG.Models;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Mind.Plans
{
    public class EngineerSpyPlan : IPlan
    {
        public bool IsDone { get; set; } = false;
        private bool _hasStarted = false;
        private Vent _nearestVent = null;
        public float StartedAt { get; private set; } = 0;

        public void Execute(AgentBrain brain)
        {
            if (IsDone || brain.IsDead) return;

            if (!_hasStarted)
            {
                _hasStarted = true;
                _nearestVent = VentManager.GetNearestVent(brain.Vector2Position);

                if (_nearestVent != null)
                {
                    var path = Pathfinder.FindPath(brain.WaypointPosition, _nearestVent.transform.position.GetClosestNode(), out _);
                    brain.CommandGoToPath(path);
                    brain.currentVentToEnter = _nearestVent;
                    
                    brain.TargetRoomForVent = _nearestVent.GetRoom(); // Setting target room to the same room implies they want to stay here
                    brain.AvoidWitnessesWhenVenting = true; // Wait for a safe moment to exit

                    StartedAt = Time.time;
                    
                    if (!brain.tempParallelDecisions.Exists(d => d.ID == TempParallelDecisionsIDsEnum.EngineerSpyPlan))
                    {
                        brain.tempParallelDecisions.Add(new AgentTempParallelDecision(TempParallelDecisionsIDsEnum.EngineerSpyPlan, () => {
                            if (brain.AgentControl.inVent)
                            {
                                brain.SetState(AgentState.InVent);
                                IsDone = true;
                                return true;
                            }
                            if (Time.time - StartedAt > 15)
                            {
                                IsDone = true;
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
                else
                {
                    IsDone = true;
                }
            }
        }
    }
}
