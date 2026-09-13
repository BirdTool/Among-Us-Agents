using System.Linq;
using AMG.AI.Mind.StructuredAgentBrain;
using AMG.AI.Mind.StructuredAgentBrain.Plans;
using AMG.Interfaces;
using AMG.Models.Plans;
using AMG.Utilities;
using AMG.Utilities.MapUtils;

namespace AMG.Models.Scenarios
{
    public class MedbayElectricalKillScenario : IScenario
    {
        public float CheckTime { get; set; } = 3.2f;
        public float LastCheckTime { get; set; } = 0f;

        public float CalculateScore(StructuredAgentBrain brain)
        {
            if (SimpleChecks(brain)) return 0;

            var playersInMedbay = Utils.Players.GetAllAlivePlayersInARoom(SystemTypes.MedBay).Where(p => !p.Data.Role.IsImpostor);
            var playersInElectrical = Utils.Players.GetAllAlivePlayersInARoom(SystemTypes.Electrical).Where(p => !p.Data.Role.IsImpostor);

            if (playersInMedbay.Count() != 1 && playersInElectrical.Any()) return 0;

            float score = 20f;

            return score;
        }

        private bool SimpleChecks(StructuredAgentBrain brain)
        {
            if (!Utils.IsSkeldMap) return true;
            if (!brain.IsImpostor) return true;
            if (brain.IsDead) return true;
            if (brain.WaypointPosition.Room != SystemTypes.Electrical) return true;
            
            if (brain.Agent.killTimer > 0f || !KillCooldownManager.CanKill(brain.Agent.PlayerId)) return true;
            
            if (brain.SafeCloseDoorNotExecute(SystemTypes.Electrical) != Enums.SafeRpcEnums.CloseDoorRoomEnums.SUCCESS ||
                brain.SafeCloseDoorNotExecute(SystemTypes.MedBay) != Enums.SafeRpcEnums.CloseDoorRoomEnums.SUCCESS)
            {
                return true; 
            }

            return false;
        }

        public AgentPlanManager GeneratePlan(StructuredAgentBrain brain)
        {
            AgentPlanManager plan = new(brain);

            var target = Utils.Players.GetAllAlivePlayersInARoom(SystemTypes.MedBay).FirstOrDefault(p => !p.Data.Role.IsImpostor);

            if (target == null) return null;

            plan.AddPlan(new CloseDoorPlan(SystemTypes.Electrical, SystemTypes.MedBay));

            plan.AddPlan(new VentingPlan(SkeldVents.ElecVent, () => {
                brain.SafeVentGoRight(SkeldVents.ElecVent);
                brain.SafeLeaveVent(SkeldVents.MedVent);
                brain.ClearCurrentVent();
                return true;
            }));

            plan.AddPlan(new KillPlan(target));

            plan.AddPlan(new VentingPlan(SkeldVents.MedVent, () => {
                brain.SafeVentGoRight(SkeldVents.MedVent);
                brain.SafeLeaveVent(SkeldVents.ElecVent);
                brain.ClearCurrentVent();
                return true;
            }));

            plan.SetCalculatingAtEnd(true);

            return plan;
        }
    }
}