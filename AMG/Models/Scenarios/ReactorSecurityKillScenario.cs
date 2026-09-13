using System.Linq;
using AMG.AI.Mind.StructuredAgentBrain;
using AMG.AI.Mind.StructuredAgentBrain.Plans;
using AMG.AI.Navigation;
using AMG.Interfaces;
using AMG.Models.Plans;
using AMG.Utilities;
using AMG.Utilities.MapUtils;
using UnityEngine;

namespace AMG.Models.Scenarios
{
    public class ReactorSecurityKillScenario : IScenario
    {
        public float CheckTime { get; set; } = 2f;
        public float LastCheckTime { get; set; } = 0;

        private static readonly SystemTypes[] AllowedRooms =
        [
            SystemTypes.Reactor, SystemTypes.UpperEngine, SystemTypes.Security, SystemTypes.LowerEngine
        ];

        public float CalculateScore(StructuredAgentBrain brain)
        {
            if (SimpleChecks(brain)) return 0;

            if (brain.WaypointPosition.Room != SystemTypes.Hallway) return 0;
            if (!brain.WaypointPosition.NeighborRooms.Contains(SystemTypes.Reactor)) return 0;

            var nearbyCrew = brain.GetNearbyPlayersOutsideVision(10f, 2f)
                .Where(p => !p.Data.Role.IsImpostor)
                .ToList();

            var playersInReactor = nearbyCrew
                .Where(p => p.GetTruePosition().GetClosestNode().Room == SystemTypes.Reactor)
                .ToList();

            if (playersInReactor.Count != 1) return 0;

            var playersInSecurity = nearbyCrew
                .Where(p => p.GetTruePosition().GetClosestNode().Room == SystemTypes.Security)
                .ToList();

            bool hasOutsideWitness = nearbyCrew
                .Any(p => !AllowedRooms.Contains(p.GetTruePosition().GetClosestNode().Room));

            if (hasOutsideWitness) return 0;

            float score = 10f;

            var targetPosition = playersInReactor[0].GetTruePosition();
            var distFromUpperVent = Pathfinder.GetStraightDistance(targetPosition, SkeldVents.UpperReactorVent.transform.position);

            if (distFromUpperVent < 4f) score += 25f;
            else score -= 15f;

            if (playersInSecurity.Count == 1) score += 20f;
            else if (playersInSecurity.Count == 0) score += 4f;
            else score -= 10f;

            return score;
        }

        private bool SimpleChecks(StructuredAgentBrain brain)
        {
            if (!Utils.IsSkeldMap) return true;
            if (!brain.IsImpostor) return true;
            if (brain.IsDead) return true;

            if (brain.Agent.killTimer > 0f || !KillCooldownManager.CanKill(brain.Agent.PlayerId)) return true;
            if (brain.SafeCloseDoorNotExecute(SystemTypes.Security) != Enums.SafeRpcEnums.CloseDoorRoomEnums.SUCCESS ||
                brain.SafeCloseDoorNotExecute(SystemTypes.UpperEngine) != Enums.SafeRpcEnums.CloseDoorRoomEnums.SUCCESS ||
                brain.SafeCloseDoorNotExecute(SystemTypes.LowerEngine) != Enums.SafeRpcEnums.CloseDoorRoomEnums.SUCCESS)
            {
                return true;
            }

            return false;
        }

        public AgentPlanManager GeneratePlan(StructuredAgentBrain brain)
        {
            PlayerControl target = null;

            var playersInReactor = Utils.Players.GetAllAlivePlayersInARoom(SystemTypes.Reactor);
            foreach (var p in playersInReactor)
            {
                if (!p.Data.Role.IsImpostor) target = p;
            }

            if (target == null) return null;

            byte targetId = target.PlayerId;
            AgentPlanManager plan = new(brain);

            var ventNode = SkeldVents.UpperEngineVent.transform.position.GetClosestNode();
            var upperEngineEntryNode = WaypointManager.AllWaypoints
                .Where(w => w.Room == SystemTypes.UpperEngine && w != ventNode)
                .OrderBy(w => Vector2.Distance(ventNode.Position, w.Position))
                .FirstOrDefault() ?? ventNode;

            plan.AddPlan(new WalkingPlan(upperEngineEntryNode) { Name = "WalkingPlan-upperEngineEntryNode" });

            plan.AddPlan(new CheckPlan(() =>
            {
                return !Utils.Players.GetAllAlivePlayersInARoom(SystemTypes.UpperEngine).Any(p => !p.Data.Role.IsImpostor);
            })  {
                Name = "CheckPlan-is-there-someone-in-upper"
            });

            plan.AddPlan(new CloseDoorPlan(SystemTypes.Security, SystemTypes.UpperEngine, SystemTypes.LowerEngine) {
                Name = "CloseDoorPlan-security-upper-lower"
            });

            plan.AddPlan(new VentingPlan(SkeldVents.UpperEngineVent, () =>
            {
                var goResult = brain.SafeVentGoLeft(SkeldVents.UpperEngineVent);
                LogManager.LogDebug($"[ReactorSecurityKillScenario-debug] SafeVentGoLeft(UpperEngineVent) -> {goResult}");

                var leaveResult = brain.SafeLeaveVent(SkeldVents.UpperReactorVent);
                LogManager.LogDebug($"[ReactorSecurityKillScenario-debug] SafeLeaveVent(UpperReactorVent) -> {leaveResult}");

                brain.ClearCurrentVent();
                return true;
            })  {
                Name = "VentingPlan-upperEngineVent to UpperReactorVent"
            });

            plan.AddPlan(new CheckPlan(() =>
            {
                bool targetStillThere = Utils.Players.GetAllAlivePlayersInARoom(SystemTypes.Reactor)
                    .Any(p => p.PlayerId == targetId);

                if (!targetStillThere) return false;

                var nearbyPlayers = brain.NearbyPlayersInVision;
                return !nearbyPlayers.Any(p => !p.Data.Role.IsImpostor && p.PlayerId != targetId);
            })  {
                Name = "CheckPlan-is-there-someone-in-reactor"
            });

            plan.AddPlan(new KillPlan(target) {
                Name = "KillPlan-reactor"
            });

            plan.AddPlan(new VentingPlan(SkeldVents.UpperReactorVent, () =>
            {
                var goResult = brain.SafeVentGoRight(SkeldVents.UpperReactorVent);
                LogManager.LogDebug($"[ReactorSecurityKillScenario-debug] SafeVentGoRight(UpperReactorVent) -> {goResult}");

                var nearbyPlayers = brain.NearbyPlayersInVision;
                if (nearbyPlayers.Any(x => !x.Data.Role.IsImpostor)) return false;

                var leaveResult = brain.SafeLeaveVent(SkeldVents.UpperEngineVent);
                LogManager.LogDebug($"[ReactorSecurityKillScenario-debug] SafeLeaveVent(UpperEngineVent) -> {leaveResult}");
                return true;
            })  {
                Name = "VentingPlan-upperReactorVent to UpperEngineVent"
            });

            plan.AddPlan(new WaitConditionPlan( () =>
            {
                return !Utils.Sabotages.IsDoorClosed(SystemTypes.UpperEngine);
            })  {
                Name = "WaitConditionPlan-upper-engine-door-is-closed"
            });

            var cafeteriaHallwayNode = new Vector2(-8.804977f, 1.1465533f).GetClosestNode();

            if (cafeteriaHallwayNode != null)
            {
                plan.AddPlan(new WalkingPlan(cafeteriaHallwayNode) {
                    Name = "WalkingPlan-cafeteria-hallway-node"
                });

                plan.AddPlan(new WaitConditionPlan(() =>
                {
                    return !Utils.Sabotages.IsDoorClosed(SystemTypes.Cafeteria);
                })  {
                    Name = "WaitConditionPlan-cafeteria-door-is-closed"
                });
            }

            var cafeteriaNode = WaypointManager.AllWaypoints.Where(w => w.Room == SystemTypes.Cafeteria).ToList().GetRandomItemSecureOrDefault();
            if (cafeteriaNode == null) return null;

            plan.AddPlan(new WalkingPlan(cafeteriaNode) {
                Name = "WalkingPlan-cafeteria"
            });

            plan.SetCalculatingAtEnd(true);
            return plan;
        }
    }
}