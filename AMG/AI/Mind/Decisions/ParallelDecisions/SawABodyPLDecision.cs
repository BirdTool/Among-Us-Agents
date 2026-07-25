using System.Collections.Generic;
using System.Threading.Tasks;
using AMG.AI.Navigation;
using AMG.AI.Tools;
using AMG.Enums.SafeRpcEnums; // Adicionado para ler os resultados do reporte
using AMG.Interfaces;
using AMG.Models;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Mind.Decisions.ParallelDecisions
{
    internal class SawABodyPLDecision : IParallelDecision
    {
        private Dictionary<byte, CooldownTimer> _agentCognitiveTimes = [];
        private Dictionary<byte, List<RoundDeadBody>> _agentsPendingBodiesToReact = [];

        private readonly struct BodyReactionInput
        {
            public Vector2 AgentPosition { get; init; }
            public Waypoint AgentStart { get; init; }
            public List<RoundDeadBody> Bodies { get; init; }
            public bool SomeoneNearby { get; init; }
        }

        private readonly struct BodyReactionPlan
        {
            public RoundDeadBody TargetBody { get; init; }
            public bool ShouldLookAround { get; init; }
            public List<Waypoint> PatrolPoints { get; init; }
        }

        // background
        private BodyReactionPlan ComputeReactionPlan(BodyReactionInput input)
        {
            RoundDeadBody mostRecentBody = null;
            double shouldLookAround = input.Bodies.Count > 1 ? 0.7 : 0;

            foreach (var body in input.Bodies)
                if (mostRecentBody == null || body.TimeOfDeath > mostRecentBody.TimeOfDeath)
                    mostRecentBody = body;

            if (mostRecentBody.TimeSinceDeath < 7) shouldLookAround += 0.6;
            if (input.SomeoneNearby) shouldLookAround += 0.45;

            if (!Utils.ExecuteProbability(shouldLookAround))
                return new BodyReactionPlan { TargetBody = mostRecentBody, ShouldLookAround = false };

            Vector2 bodyPos = mostRecentBody.Position;
            Waypoint agentNode = Pathfinder.GetClosestNode(input.AgentPosition);
            Vector2 dir = (bodyPos - input.AgentPosition).normalized;

            var candidates = new List<Waypoint>();
            foreach (var wp in WaypointManager.AllWaypoints)
            {
                float d = Vector2.Distance(bodyPos, wp.Position);
                if (d < 2.5f || d > 12f) continue;
                if (Vector2.Dot(dir, (wp.Position - bodyPos).normalized) >= -0.4f)
                    candidates.Add(wp);
            }
            candidates.Sort((a, b) => Vector2.Distance(bodyPos, a.Position).CompareTo(Vector2.Distance(bodyPos, b.Position)));

            var patrolPoints = new List<Waypoint>();
            foreach (var node in candidates)
            {
                if (patrolPoints.Exists(p => Vector2.Distance(node.Position, p.Position) < 2.5f)) continue;
                var path = Pathfinder.FindPath(agentNode, node, out float dist);
                if (path == null || dist > 18f) continue;
                patrolPoints.Add(node);
                if (patrolPoints.Count >= 2) break;
            }

            return new BodyReactionPlan { TargetBody = mostRecentBody, ShouldLookAround = true, PatrolPoints = patrolPoints };
        }

        private CooldownTimer GetAgentCognitiveTimer(byte agentId)
        {
            if (!_agentCognitiveTimes.ContainsKey(agentId))
            {
                _agentCognitiveTimes[agentId] = new CooldownTimer();
            }
            return _agentCognitiveTimes[agentId];
        }

        private List<RoundDeadBody> GetAgentPendingBodies(byte agentId)
        {
            if (!_agentsPendingBodiesToReact.ContainsKey(agentId))
            {
                _agentsPendingBodiesToReact[agentId] = null;
            }
            return _agentsPendingBodiesToReact[agentId];
        }

        public void Evaluate(AgentBrain brain)
        {
            if (brain.sawABody) return;

            byte id = brain.AgentControl.PlayerId;
            var cognitiveTimer = GetAgentCognitiveTimer(id);
            var pendingBodiesToReact = GetAgentPendingBodies(id);
            var nearbyBodies = brain.GetNearbyBodies();

            if (nearbyBodies.Count > 0 && !cognitiveTimer.IsRunning && pendingBodiesToReact == null)
            {
                cognitiveTimer.StartDelay(brain.GetReactionTime());
                _agentsPendingBodiesToReact[id] = nearbyBodies;
                return;
            }

            if (pendingBodiesToReact != null && cognitiveTimer.Consume())
            {
                brain.sawABody = true; // main thread, sem problema

                var input = new BodyReactionInput
                {
                    AgentPosition = brain.AgentControl.transform.position,
                    AgentStart = brain.WaypointPosition,
                    Bodies = pendingBodiesToReact,
                    SomeoneNearby = brain.GetNearbyPlayers().Count > 0
                };
                _agentsPendingBodiesToReact.Remove(id);

                Task.Run(() => ComputeReactionPlan(input))
                    .ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                        {
                            LogManager.LogError($"[SawABodyPLDecision] {t.Exception}");
                            return;
                        }
                        MainThreadDispatcher.Enqueue(() => ApplyReactionPlan(brain, t.Result));
                    });
            }
        }

        private void ApplyReactionPlan(AgentBrain brain, BodyReactionPlan plan)
        {
            if (brain.IsDead) return;

            brain.currentLocalTask = null;
            brain.isGoingToFixASabotage = false;
            brain.ReplaceNameTag(DefaultTags.Emotions.Scared, 20f);

            RoundDeadBody mostRecentBody = plan.TargetBody;

            if (!plan.ShouldLookAround)
            {
                var reportResult = brain.SafeReportBody(mostRecentBody);

                if (reportResult == ReportDeadBodyRpcEnums.ERROR_BodyDoesNotExist)
                {
                    LogManager.LogError($"[Agente {brain.AgentControl.PlayerId}] Tentou reportar corpo do ID {mostRecentBody.PlayerId}, mas o registro não existe mais!");
                }
                else if (reportResult != ReportDeadBodyRpcEnums.SUCCESS)
                {
                    var start = brain.WaypointPosition;
                    var end = Pathfinder.GetClosestNode(mostRecentBody.Position);
                    var path = Pathfinder.FindPath(start, end, out float dist);

                    brain.CommandGoToPath(path);

                    brain.updateAction = new AgentUpdateAction(() =>
                    {
                        var actionResult = brain.SafeReportBody(mostRecentBody);

                        if (actionResult == ReportDeadBodyRpcEnums.ERROR_BodyDoesNotExist)
                            return true;

                        return actionResult == ReportDeadBodyRpcEnums.SUCCESS;
                    })
                    {
                        ExecuteOnMeeting = false,
                        DeleteOnMeeting = false,
                        IsOnlyPredefinedAction = false,
                    };
                }
            }
            else
            {
                Vector2 bodyPos = mostRecentBody.Position;
                var patrolPoints = plan.PatrolPoints ?? new List<Waypoint>();

                brain.updateAction = new AgentUpdateAction(() =>
                {
                    if (patrolPoints.Count == 0)
                    {
                        var reportStatus = brain.SafeReportBody(mostRecentBody.PlayerId);

                        if (reportStatus == ReportDeadBodyRpcEnums.ERROR_BodyDoesNotExist)
                            return true;
                        else if (reportStatus != ReportDeadBodyRpcEnums.SUCCESS)
                        {
                            if (brain.currentPath == null)
                            {
                                var startNode = brain.WaypointPosition;
                                var endNode = Pathfinder.GetClosestNode(bodyPos);
                                var path = Pathfinder.FindPath(startNode, endNode, out float dist);
                                brain.CommandGoToPath(path);
                            }
                            return false;
                        }
                        else
                        {
                            brain.ResetPath();
                            return true;
                        }
                    }

                    if (brain.currentPath == null)
                    {
                        Waypoint currentAgentNode = brain.WaypointPosition;
                        Waypoint closestTarget = null;
                        List<Waypoint> bestPath = null;
                        float shortestDistance = float.MaxValue;

                        foreach (Waypoint point in patrolPoints)
                        {
                            List<Waypoint> testPath = Pathfinder.FindPath(currentAgentNode, point, out float pathDist);
                            if (testPath != null && pathDist < shortestDistance)
                            {
                                shortestDistance = pathDist;
                                closestTarget = point;
                                bestPath = testPath;
                            }
                        }

                        if (closestTarget != null && bestPath != null)
                        {
                            brain.CommandGoToPath(bestPath);
                            patrolPoints.Remove(closestTarget);
                        }
                        else
                        {
                            patrolPoints.Clear();
                        }
                    }
                    return false;
                })
                {
                    ExecuteOnMeeting = false,
                    DeleteOnMeeting = false,
                    IsOnlyPredefinedAction = false,
                };
            }
        }
    }
}