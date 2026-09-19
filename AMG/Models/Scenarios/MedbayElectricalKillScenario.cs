using System.Linq;
using AMG.AI.Mind.StructuredAgentBrain;
using AMG.AI.Mind.StructuredAgentBrain.Plans;
using AMG.AI.Navigation;
using AMG.Interfaces;
using AMG.Models.Plans;
using AMG.Utilities;
using AMG.Utilities.MapUtils;

namespace AMG.Models.Scenarios
{
    public class MedbayElectricalKillScenario : IScenario
    {
        public float CheckTime { get; set; } = 4.3f;
        public float LastCheckTime { get; set; } = 0f;

        public float CalculateScore(StructuredAgentBrain brain)
        {
            if (SimpleChecks(brain)) return 0;

            var playersInMedbay = Utils.Players.GetAllAlivePlayersInARoom(SystemTypes.MedBay).Where(p => !p.Data.Role.IsImpostor);
            var playersInElectrical = Utils.Players.GetAllAlivePlayersInARoom(SystemTypes.Electrical).Where(p => !p.Data.Role.IsImpostor);

            if (playersInMedbay.Count() != 1 && playersInElectrical.Any()) return 0;

            float score = 15f;

            var playersInStorage = Utils.Players.GetAllAlivePlayersInARoom(SystemTypes.Storage).Where(p => !p.Data.Role.IsImpostor);
            var playersInLowerEngine = Utils.Players.GetAllAlivePlayersInARoom(SystemTypes.LowerEngine).Where(p => !p.Data.Role.IsImpostor);
            var playersInHallways = Utils.Players.GetAllAlivePlayersInARoom(SystemTypes.Hallway).Where(p => !p.Data.Role.IsImpostor);
            
            var playersBetweenStorageAndLower = playersInHallways.Where(p => {
                var node = Pathfinder.GetClosestNode(p.transform.position);
                return node.NeighborRooms.Contains(SystemTypes.Storage) && node.NeighborRooms.Contains(SystemTypes.LowerEngine);
            });

            if (playersInStorage.Count() > 1) score -= 5f;
            else score += 2.5f;

            if (playersInLowerEngine.Count() > 1) score -= 5f;
            else score += 2.5f;

            if (playersBetweenStorageAndLower.Count() > 1) score -= 10f;
            else score += 3f;

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