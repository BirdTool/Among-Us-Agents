using AMG.AI.Navigation;
using AMG.AI.Tools;
using AMG.Interfaces;
using AMG.Models;
using AMG.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace AMG.AI.Mind.Decisions.ParallelDecisions
{
    public class SawABodyPLDecision : IParallelDecision
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
            if (brain.sawABody) return;

            byte id = brain.AgentControl.PlayerId;

            var cognitiveTimer = GetAgentCognitiveTimer(id);
            var pendingBodiesToReact = GetAgentPendingBodies(id);

            var nearbyBodies = brain.GetNearbyBodies();

            if (nearbyBodies.Count > 0)
            {
                if (!cognitiveTimer.IsRunning && pendingBodiesToReact == null)
                {
                    var reactionTime = brain.GetReactionTime();
                    cognitiveTimer.StartDelay(reactionTime);
                    _agentsPendingBodiesToReact[id] = nearbyBodies;

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
            LogManager.LogDebug("Agente viu um corpo!");
            if (bodies.Count < 1)
            {
                LogManager.LogDebug("Não há corpos na função");
                return;
            }

            brain.ReplaceNameTag(DefaultTags.Emotions.Scared, 20f);
            LogManager.LogDebug("Tag de emoção definida como assutado");

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
            bool isThereSomeoneNearby = false;
            Waypoint start = Pathfinder.GetClosestNode(mostRecentBody.Position);
            foreach (PlayerControl player in Utils.Players.AllAlivePlayerNotMe)
            {
                Waypoint target = Pathfinder.GetClosestNode(player.transform.position);
                List<Waypoint> currentCalculatedPath = Pathfinder.FindPath(start, target, out float pathDistance);

                if (currentCalculatedPath != null && pathDistance < 10f)
                {
                    isThereSomeoneNearby = true;
                    break;
                }
            }

            if (isThereSomeoneNearby) shouldLookAround += 0.45;

            // shouldLookAround = 1; // Test mode

            var playerInfoToReport = GameData.Instance.GetPlayerById(mostRecentBody.PlayerId);
            if (!Utils.ExecuteProbability(shouldLookAround))
            {
                LogManager.LogDebug("Reportará sem olhar ao redor");
                LogManager.LogDebug($"Chance de olhar ao redor: {shouldLookAround}");

                if (playerInfoToReport != null)
                {
                    bool canReport = brain.CanReportBody(mostRecentBody.Position);
                    if (!canReport)
                    {
                        var end = Pathfinder.GetClosestNode(mostRecentBody.Position);

                        var path = Pathfinder.FindPath(start, end, out float dist);
                        brain.CommandGoToPath(path);

                        brain.updateAction = new AgentUpdateAction(() =>
                        {
                            bool canReport = brain.CanReportBody(mostRecentBody.Position);
                            if (canReport)
                            {
                                brain.AgentControl.CmdReportDeadBody(playerInfoToReport);
                                return true;
                            }
                            return false;
                        })
                        {
                            ExecuteOnMeeting = false,
                            DeleteOnMeeting = false,
                            IsOnlyPredefinedAction = false,
                        };
                    }
                    else
                    {
                        brain.AgentControl.CmdReportDeadBody(playerInfoToReport);
                    }
                }
                else
                    LogManager.LogError($"[Agente {brain.AgentControl.PlayerId}] Tentou reportar corpo do ID {mostRecentBody.PlayerId}, mas o registro não existe mais!");
            }
            else
            {
                LogManager.LogDebug($"Olhará ao redor, chance: {shouldLookAround}");
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

                LogManager.LogDebug($"[Agente {brain.AgentControl.PlayerId}] Encontrou {patrolPoints.Count} pontos válidos na mesma área para patrulhar.");

                brain.updateAction = new AgentUpdateAction(() =>
                {
                    if (patrolPoints.Count == 0)
                    {
                        if (playerInfoToReport != null)
                        {
                            bool canReport = brain.CanReportBody(bodyPos);
                            if (!canReport)
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
                                brain.AgentControl.CmdReportDeadBody(playerInfoToReport);
                                brain.ResetPath();
                                return true;
                            }
                        }
                        else
                        {
                            LogManager.LogError($"[Agente {brain.AgentControl.PlayerId}] Registro do corpo sumiu!");
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
                            LogManager.LogWarning("[AI] Nenhum ponto de patrulha acessível, abortando patrulha.");
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
