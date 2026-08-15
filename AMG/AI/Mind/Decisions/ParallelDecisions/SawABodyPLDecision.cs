using System.Collections.Generic;
using AMG.AI.Navigation;
using AMG.AI.Tools;
using AMG.Enums.SafeRpcEnums;
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
            byte id = brain.AgentControl.PlayerId;

            if (brain.sawABody) 
            {
                if (_agentsPendingBodiesToReact.ContainsKey(id))
                {
                    _agentsPendingBodiesToReact.Remove(id);
                    GetAgentCognitiveTimer(id).Stop();
                }
                return;
            }
            var cognitiveTimer = GetAgentCognitiveTimer(id);
            var pendingBodiesToReact = GetAgentPendingBodies(id);

            var nearbyBodies = brain.NearbyBodiesInVision;

            if (nearbyBodies.Count > 0)
            {
                if (!cognitiveTimer.IsRunning && pendingBodiesToReact == null)
                {
                    var reactionTime = brain.ReactionTime;
                    cognitiveTimer.StartDelay(reactionTime);
                    _agentsPendingBodiesToReact[id] = nearbyBodies;

                    brain.bodiesSeenDead.AddRange(nearbyBodies);

                    return;
                }
            }

            if (pendingBodiesToReact != null)
            {
                if (cognitiveTimer.Consume())
                {
                    brain.sawABody = true;

                    SawABodyAction(pendingBodiesToReact, brain);

                    _agentsPendingBodiesToReact.Remove(id);
                    return;
                }
            }

            return;
        }

        private void SawABodyAction(List<RoundDeadBody> bodies, AgentBrain brain)
        {
            if (brain.IsDead) return;

            if (bodies.Count < 1) return;

            brain.currentLocalTask = null;
            brain.isGoingToFixASabotage = false;

            brain.ReplaceNameTag(DefaultTags.Emotions.Scared, 20f);

            double shouldLookAround = 0;
            if (bodies.Count > 1) shouldLookAround += 0.7;
            RoundDeadBody mostRecentBody = null;
            foreach (RoundDeadBody body in bodies)
            {
                if (mostRecentBody == null || body.TimeOfDeath > mostRecentBody.TimeOfDeath)
                {
                    mostRecentBody = body;
                }
            }

            if (mostRecentBody.TimeSinceDeath < 7) shouldLookAround += 0.6;

            // Check for someone else nearby
            bool isThereSomeoneNearby = brain.NearbyPlayers.Count > 0;

            if (isThereSomeoneNearby) shouldLookAround += 0.45;

            if (brain.IsImpostor)
            {
                if (mostRecentBody.TimeSinceDeath < 30f)
                {
                    // Too fresh to self report safely
                    shouldLookAround = 1.0;
                }
                else
                {
                    // It's been a while, we can self report to create an alibi
                    shouldLookAround = 0.0;
                }
            }

            var start = brain.WaypointPosition;

            if (!Utils.ExecuteProbability(shouldLookAround))
            {
                var reportResult = brain.SafeReportBody(mostRecentBody);

                if (reportResult == ReportDeadBodyRpcEnums.ERROR_BodyDoesNotExist)
                {
                    LogManager.LogError($"[Agente {brain.AgentControl.PlayerId}] Tentou reportar corpo do ID {mostRecentBody.PlayerId}, mas o registro não existe mais!");
                }
                else if (reportResult != ReportDeadBodyRpcEnums.SUCCESS)
                {
                    var end = Pathfinder.GetClosestNode(mostRecentBody.Position);
                    var path = Pathfinder.FindPath(start, end, out float dist);

                    brain.CommandGoToPath(path);

                    brain.updateAction = new AgentUpdateAction(() =>
                    {
                        var actionResult = brain.SafeReportBody(mostRecentBody);

                        if (actionResult == ReportDeadBodyRpcEnums.ERROR_BodyDoesNotExist)
                        {
                            return true;
                        }

                        return actionResult == ReportDeadBodyRpcEnums.SUCCESS;
                    })
                    {
                        ExecuteOnMeeting = false,
                        DeleteOnMeeting = true,
                        IsOnlyPredefinedAction = false,
                    };
                }
            }
            else
            {
                Vector2 bodyPos = mostRecentBody.Position;

                List<Waypoint> patrolPoints = [];
                Waypoint agentNodeStart = Pathfinder.GetClosestNode(brain.AgentControl.transform.position);

                Vector2 agentPos2D = brain.AgentControl.transform.position;
                Vector2 agentToBodyDir = (bodyPos - agentPos2D).normalized;

                List<Waypoint> candidateNodes = [];
                foreach (var wp in WaypointManager.AllWaypoints)
                {
                    float distToBody = Vector2.Distance(bodyPos, wp.Position);

                    if (distToBody >= 2.5f && distToBody <= 12f)
                    {
                        if (distToBody < 0.1f) continue;

                        Vector2 bodyToNodeDir = (wp.Position - bodyPos).normalized;

                        if (Vector2.Dot(agentToBodyDir, bodyToNodeDir) >= -0.4f)
                        {
                            candidateNodes.Add(wp);
                        }
                    }
                }

                candidateNodes.Sort((a, b) =>
                    Vector2.Distance(bodyPos, a.Position).CompareTo(Vector2.Distance(bodyPos, b.Position))
                );

                foreach (Waypoint node in candidateNodes)
                {
                    bool isRedundant = false;
                    foreach (Waypoint existingPoint in patrolPoints)
                    {
                        if (Vector2.Distance(node.Position, existingPoint.Position) < 2.5f)
                        {
                            isRedundant = true;
                            break;
                        }
                    }

                    if (isRedundant) continue;

                    var testPath = Pathfinder.FindPath(agentNodeStart, node, out float walkingDistToNode);

                    if (testPath == null || walkingDistToNode > 18f)
                        continue;

                    patrolPoints.Add(node);

                    if (patrolPoints.Count >= 2)
                        break;
                }

                brain.updateAction = new AgentUpdateAction(() =>
                {

                    if (patrolPoints.Count == 0)
                    {
                        var reportStatus = brain.SafeReportBody(mostRecentBody.PlayerId);

                        if (reportStatus == ReportDeadBodyRpcEnums.ERROR_BodyDoesNotExist)
                        {
                            return true;
                        }
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