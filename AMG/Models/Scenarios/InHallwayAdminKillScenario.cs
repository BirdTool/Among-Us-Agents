using System;
using System.Collections.Generic;
using System.Linq;
using AMG.AI.Mind.StructuredAgentBrain;
using AMG.AI.Mind.StructuredAgentBrain.Plans;
using AMG.Interfaces;
using AMG.Models.Plans;
using AMG.Utilities;
using AMG.Utilities.MapUtils;
using Il2CppSystem.Reflection;

namespace AMG.Models.Scenarios
{
    public class InHallwayAdminKillScenario : IScenario
    {
        public float CheckTime { get; set; } = 2f;
        public float LastCheckTime { get; set; } = 0;

        public float CalculateScore(StructuredAgentBrain brain)
        {
            if (SimpleChecks(brain)) return 0;

            if (brain.WaypointPosition.Room != SystemTypes.Hallway) return 0;

            List<SystemTypes> borders = [SystemTypes.Shields, SystemTypes.Nav, SystemTypes.Weapons, SystemTypes.LifeSupp];

            foreach (var border in borders)
            {
                if (!brain.WaypointPosition.NeighborRooms.Contains(border)) return 0;
            }

            int crewmatesInArea = 0;
            PlayerControl target = null;

            var playersInAdmin = Utils.Players.GetAllAlivePlayersInARoom(SystemTypes.Admin);
            foreach (var p in playersInAdmin)
            {
                if (!p.Data.Role.IsImpostor)
                {
                    crewmatesInArea++;
                    target = p;
                }
            }

            var playersInHallways = Utils.Players.GetAllAlivePlayersInARoom(SystemTypes.Hallway);
            foreach (var p in playersInHallways)
            {
                if (p.Data.Role.IsImpostor) continue;

                var waypoint = p.GetTruePosition().GetClosestNode();
                if (waypoint.NeighborRooms.Contains(SystemTypes.Admin))
                {
                    crewmatesInArea++;
                    target = p;
                }
            }

            if (crewmatesInArea != 1 || target == null) return 0;

            float score = 10f;
            bool isCoastClear = true;
            var nearbyPlayers = brain.GetNearbyPlayersOutsideVision(15, 3);
            foreach (var p in nearbyPlayers)
            {
                if (!p.Data.Role.IsImpostor && p.PlayerId != target.PlayerId)
                {
                    isCoastClear = false;
                    break;
                }
            }

            if (!isCoastClear) score -= 20f;
            else score += 35f;

            return score;
        }

        private bool SimpleChecks(StructuredAgentBrain brain)
        {
            if (!Utils.IsSkeldMap) return true;
            if (!brain.IsImpostor) return true;
            if (brain.Agent.Data.IsDead) return true;
            
            if (brain.Agent.killTimer > 0f || !KillCooldownManager.CanKill(brain.Agent.PlayerId)) return true;
            
            if (brain.SafeCloseDoorNotExecute(SystemTypes.Storage) != Enums.SafeRpcEnums.CloseDoorRoomEnums.SUCCESS ||
                brain.SafeCloseDoorNotExecute(SystemTypes.Cafeteria) != Enums.SafeRpcEnums.CloseDoorRoomEnums.SUCCESS)
            {
                return true; 
            }

            return false;
        }

        public AgentPlanManager GeneratePlan(StructuredAgentBrain brain)
        {
            PlayerControl target = null;
            
            var playersInAdmin = Utils.Players.GetAllAlivePlayersInARoom(SystemTypes.Admin);
            foreach (var p in playersInAdmin)
            {
                if (!p.Data.Role.IsImpostor) target = p;
            }

            if (target == null)
            {
                var playersInHallways = Utils.Players.GetAllAlivePlayersInARoom(SystemTypes.Hallway);
                foreach (var p in playersInHallways)
                {
                    if (p.Data.Role.IsImpostor) continue;
                    var waypoint = p.GetTruePosition().GetClosestNode();
                    if (waypoint.NeighborRooms.Contains(SystemTypes.Admin)) target = p;
                }
            }

            if (target == null) return null;

            AgentPlanManager plan = new(brain);

            plan.AddPlan(new CloseDoorPlan(SystemTypes.Cafeteria));
            plan.AddPlan(new CloseDoorPlan(SystemTypes.Storage));

            var bigYVent = SkeldVents.BigYVent;
            var adminVent = SkeldVents.AdminVent;

            plan.AddPlan(new VentingPlan(bigYVent, () => { 
                brain.SafeVentGoLeft(bigYVent);
                brain.SafeLeaveVent(adminVent);
                brain.ClearCurrentVent();
                return true;
            }));

            plan.AddPlan(new KillPlan(target));
            
            plan.AddPlan(new VentingPlan(adminVent, () => { 
                brain.SafeVentGoRight(adminVent);
                
                var nearbyPlayers = brain.NearbyPlayersInVision;
                if (nearbyPlayers.Any(x => !x.Data.Role.IsImpostor)) return false;

                brain.SafeLeaveVent(bigYVent);
                return true;
            }));
            
            plan.SetCalculatingAtEnd(true);
            return plan;
        }
    }
}