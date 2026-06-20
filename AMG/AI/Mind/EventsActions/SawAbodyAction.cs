using AMG.AI.Navigation;
using AMG.AI.Tools;
using AMG.Models;
using AMG.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace AMG.AI.Mind
{
    public partial class AgentBrain
    {
        private CooldownTimer _cognitiveTimer = new();
        private List<RoundDeadBody> _pendingBodiesToReact = null;

        private void SawABodyAction(List<RoundDeadBody> bodies)
        {
            if (myAgent.Data.IsDead) return;
            LogManager.LogDebug("Agente viu um corpo!");
            if (bodies.Count < 1)
            {
                LogManager.LogDebug("Não há corpos na função");
                return;
            }

            ReplaceNameTag(DefaultTags.Emotions.Scared, 20f);
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
                    bool canReport = CanReportBody(mostRecentBody.Position);
                    if (!canReport)
                    {
                        var end = Pathfinder.GetClosestNode(mostRecentBody.Position);

                        var path = Pathfinder.FindPath(start, end, out float dist);
                        CommandGoToPath(path);

                        updateAction = new AgentUpdateAction(() =>
                        {
                            bool canReport = CanReportBody(mostRecentBody.Position);
                            if (canReport)
                            {
                                myAgent.CmdReportDeadBody(playerInfoToReport);
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
                        myAgent.CmdReportDeadBody(playerInfoToReport);
                    }
                }
                else
                    LogManager.LogError($"[Agente {myAgent.PlayerId}] Tentou reportar corpo do ID {mostRecentBody.PlayerId}, mas o registro não existe mais!");
            }
            else
            {
                LogManager.LogDebug($"Olhará ao redor, chance: {shouldLookAround}");
                Vector2 bodyPos = mostRecentBody.Position;

                List<Waypoint> patrolPoints = [];
                Waypoint agentNodeStart = Pathfinder.GetClosestNode(myAgent.transform.position);

                Vector2 agentPos2D = myAgent.transform.position;
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

                LogManager.LogDebug($"[Agente {myAgent.PlayerId}] Encontrou {patrolPoints.Count} pontos válidos na mesma área para patrulhar.");

                updateAction = new AgentUpdateAction(() =>
                {
                    if (patrolPoints.Count == 0)
                    {
                        if (playerInfoToReport != null)
                        {
                            bool canReport = CanReportBody(bodyPos);
                            if (!canReport)
                            {
                                if (currentPath == null)
                                {
                                    var startNode = Pathfinder.GetClosestNode(myAgent.transform.position);
                                    var endNode = Pathfinder.GetClosestNode(bodyPos);
                                    var path = Pathfinder.FindPath(startNode, endNode, out float dist);
                                    CommandGoToPath(path);
                                }
                                return false;
                            }
                            else
                            {
                                myAgent.CmdReportDeadBody(playerInfoToReport);
                                currentPath = null;
                                currentPathIndex = 0;
                                return true;
                            }
                        }
                        else
                        {
                            LogManager.LogError($"[Agente {myAgent.PlayerId}] Registro do corpo sumiu!");
                            return true;
                        }
                    }

                    if (currentPath == null)
                    {
                        Waypoint currentAgentNode = Pathfinder.GetClosestNode(myAgent.transform.position);
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
                            CommandGoToPath(bestPath);
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

        private List<RoundDeadBody> GetNearbyBodies()
        {
            List<RoundDeadBody> nearbyBodies = [];

            if (!sawABody && Utils.Round.CurrentRoundDeadBodies != null && Utils.Round.CurrentRoundDeadBodies.Count > 0)
            {
                List<RoundDeadBody> bodies = Utils.Round.CurrentRoundDeadBodies;

                foreach (var body in bodies)
                {
                    var origin = myAgent.transform.position;
                    var target = body.Position;

                    Vector2 origin2D = new(origin.x, origin.y + 0.5f);

                    float distToBody = Vector2.Distance(origin2D, target);
                    if (distToBody > 5f) continue;

                    var canSee = Utils.CanSeeTheTarget(origin2D, target, distToBody);
                    if (canSee) nearbyBodies.Add(body);
                }
            }

            return nearbyBodies;
        }

        private bool ExecuteHaveSeenNearbyBodiesAction()
        {
            if (sawABody) return false;

            var nearbyBodies = GetNearbyBodies();

            if (nearbyBodies.Count > 0)
            {
                if (!_cognitiveTimer.IsRunning && _pendingBodiesToReact == null)
                {
                    var reactionTime = GetReactionTime();
                    _cognitiveTimer.StartDelay(reactionTime);
                    _pendingBodiesToReact = nearbyBodies;

                    return false;
                }
            }

            if (_pendingBodiesToReact != null)
            {
                if (_cognitiveTimer.Consume())
                {
                    sawABody = true;

                    SawABodyAction(_pendingBodiesToReact);

                    _pendingBodiesToReact = null;
                    return true;
                }
            }

            return false;
        }
    }
}
