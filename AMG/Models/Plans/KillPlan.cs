using AMG.AI.Mind.StructuredAgentBrain;
using AMG.AI.Navigation;
using AMG.Enums;
using AMG.Interfaces;
using AMG.Utilities;
using UnityEngine;

namespace AMG.Models.Plans
{
    public class KillPlan(PlayerControl player) : IPlan
    {
        private readonly PlayerControl _player = player;

        public bool IsDone { get; set; } = false;
        public bool DidSuccess { get; private set; } = false;
        public bool IsRunning { get; private set; } = false;
        public float StartedAt { get; private set; } = 0;
        
        public void Execute(StructuredAgentBrain brain)
        {
            if (IsRunning) return;

            var result = brain.SafeKill(_player.PlayerId);
            if (result == Enums.SafeRpcEnums.SafeKillRpcEnums.SUCCESS) 
            {
                IsDone = true;
                IsRunning = false;
                DidSuccess = true;
                return;
            }

            if (result == Enums.SafeRpcEnums.SafeKillRpcEnums.ERROR_TargetTooFar)
            {
                var path = Pathfinder.FindPath(brain.WaypointPosition, Pathfinder.GetClosestNode(_player.transform.position), out float pathDist);
                if (pathDist > 24)
                {
                    IsDone = true;
                    IsRunning = false;
                    DidSuccess = false;
                    return;
                }

                brain.CommandGoToPath(path);
                StartedAt = Time.time;
                IsRunning = true;

                if (!brain.tempParallelDecisions.Exists(x => x.ID == TempParallelDecisionsIDsEnum.KillPlan))
                {   
                    brain.tempParallelDecisions.Add(new AgentTempParallelDecision(TempParallelDecisionsIDsEnum.KillPlan, () => {
                        if (brain.Agent.killTimer > 0f)
                        {
                            IsDone = true;
                            IsRunning = false;
                            DidSuccess = true;
                            return true;
                        }
                        if (Time.time - StartedAt > 15)
                        {
                            IsDone = true;
                            IsRunning = false;
                            DidSuccess = false;
                            brain.ResetDestinations();
                            brain.SetState(Enums.AgentEnums.AgentState.Calculating);
                            return true;
                        }
                        brain.SafeKill(_player.PlayerId);
                        return false;
                    }));
                }
                
            }
        }
    }
}
