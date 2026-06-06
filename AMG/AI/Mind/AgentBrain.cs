using AMG.AI.Navigation;
using AMG.AI.Tools;
using AMG.Enums;
using AMG.Enums.AgentEnums;
using AMG.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace AMG.AI.Mind
{

    public class AgentTag
    {
        public string Tag;
        public string ColourHex;
        public string Size;
        public IdentifierEnum Identifier;
        public float? ExpiresAt = null;
    }

    public class AgentBrain : MonoBehaviour
    {
        public AgentBrain(IntPtr ptr) : base(ptr) { }

        private PlayerControl myAgent;
        private string baseName;
        private TextMeshPro nameTextComp;
        private SpriteRenderer spriteRenderer;
        public PlayerTask currentLocalTask = null;

        public AgentState currentState = AgentState.Stopped;

        public List<AgentTag> tags = [];

        // Wandering
        private Vector2 currentDirection = Vector2.zero;
        private float directionChangeTimer = 0f;

        // Chamado
        private Waypoint targetNode = null;
        private float waitTimer = 0f;

        // Navegação
        private List<Waypoint> currentPath = null;
        private int currentPathIndex = 0;
        private float speed = 3.2f;

        // Anti Travamento
        private Vector2 lastPosition = Vector2.zero;
        private float stuckTimer = 0f;
        private bool isEvading = false;
        private float evadeTimer = 0f;
        private Vector2 evadeDirection = Vector2.zero;

        public Func<bool> updateAction = null;
        public bool isOnlyPredefinedAction = false;

        public bool sawABody = false; // Defined as false every meeting

        void Awake()
        {
            myAgent = this.GetComponent<PlayerControl>();
            nameTextComp = this.GetComponentInChildren<TextMeshPro>();
            spriteRenderer = this.GetComponent<SpriteRenderer>();

            tags = new List<AgentTag>();
            speed = 3.2f;

            if (nameTextComp != null)
            {
                nameTextComp.alignment = TMPro.TextAlignmentOptions.Bottom;
                nameTextComp.rectTransform.pivot = new Vector2(0.5f, 0f);
            }

            var playerInfo = GameData.Instance?.GetPlayerById(myAgent.PlayerId);
            baseName = playerInfo?.PlayerName ?? "AI";

            ChangeRandomDirection();
        }

        public void CommandGoToPath(List<Waypoint> path)
        {
            if (path == null || path.Count == 0) return;

            currentPath = path;
            currentPathIndex = 0;

            if (myAgent.MyPhysics?.body != null)
                myAgent.MyPhysics.body.velocity = Vector2.zero;

            SetState(AgentState.Navigating);
        }

        void Update()
        {
            if (myAgent == null || myAgent.MyPhysics?.body == null) return;

            if (updateAction != null)
            {
                bool isActionFinished = updateAction.Invoke();

                if (isActionFinished)
                {
                    updateAction = null;
                    isOnlyPredefinedAction = false;
                }
                else if (isOnlyPredefinedAction)
                {
                    return;
                }
            }

            foreach (var tag in tags)
            {
                if (tag.ExpiresAt != null && Time.time > tag.ExpiresAt)
                {
                    RemoveNameTag(tag);
                }
            }

            if (!sawABody && Utils.Round.CurrentRoundDeadBodies != null && Utils.Round.CurrentRoundDeadBodies.Count > 0)
            {
                List<RoundDeadBody> bodies = Utils.Round.CurrentRoundDeadBodies;
                List<RoundDeadBody> nearbyBodies = new List<RoundDeadBody>();

                Waypoint start = Pathfinder.GetClosestNode(myAgent.transform.position);

                foreach (RoundDeadBody body in bodies)
                {
                    Waypoint end = Pathfinder.GetClosestNode(body.Position);

                    List<Waypoint> path = Pathfinder.FindPath(start, end, out float distance);
                    if (path != null)
                    {
                        if (distance < 4f)
                        {
                            nearbyBodies.Add(body);
                        }
                    }
                }

                if (nearbyBodies.Count > 0)
                {
                    SawABody(nearbyBodies);
                    sawABody = true;
                }
            }

            switch (currentState)
            {
                case AgentState.Wandering:
                    UpdateWandering();
                    break;
                case AgentState.Stopped:
                    UpdateStopped();
                    break;
                case AgentState.Navigating:
                    UpdateNavigating();
                    break;
            }
        }

        private void SetState(AgentState newState)
        {
            if (currentState != newState)
            {
                currentState = newState;
                switch (newState)
                {
                    case AgentState.Wandering:
                        ReplaceNameTag(DefaultTags.States.Wandering);
                        break;
                    case AgentState.Stopped:
                        ReplaceNameTag(DefaultTags.States.Stopped);
                        break;
                    case AgentState.Navigating:
                        ReplaceNameTag(DefaultTags.States.Navigating);
                        break;
                }
            }
        }

        private void UpdateWandering()
        {
            directionChangeTimer -= Time.deltaTime;
            if (directionChangeTimer <= 0f) ChangeRandomDirection();

            Vector2 velocity = currentDirection * speed;
            myAgent.MyPhysics.body.velocity = velocity;
            FlipSprite(currentDirection);

            ReplaceNameTag(DefaultTags.States.Wandering);
        }

        private void UpdateStopped()
        {
            if (waitTimer > 0f)
            {
                waitTimer -= Time.deltaTime;

                if (currentLocalTask != null)
                {
                    ReplaceNameTag(DefaultTags.Thoughts.DoingTask);
                    ReplaceNameTag(IdentifierEnum.Think, $"Progresso: {Mathf.CeilToInt(waitTimer)}s", "#66FF66");
                }
                else
                {
                    ReplaceNameTag(DefaultTags.States.Stopped);
                }
            }
            else
            {
                RemoveNameTag(IdentifierEnum.Think);

                if (currentLocalTask != null)
                {
                    try
                    {
                        myAgent.RpcCompleteTask(currentLocalTask.Id);
                        LogManager.LogDebug($"[AI Task] {baseName} concluiu a task {currentLocalTask.TaskType}!");
                    }
                    catch (Exception e)
                    {
                        LogManager.LogWarning($"[AI Task] Erro ao avançar task: {e.Message}");
                    }

                    currentLocalTask = null;
                }

                currentState = AgentState.Wandering;
            }
        }

        private void UpdateNavigating()
        {
            if (currentPath == null || currentPathIndex >= currentPath.Count)
            {
                myAgent.MyPhysics.body.velocity = Vector2.zero;
                currentPath = null;
                currentPathIndex = 0;
                if (currentLocalTask != null)
                {
                    StartSimulatedTask(currentLocalTask, 5f);
                }
                else
                {
                    SetState(AgentState.Wandering);
                }
                return;
            }
            ReplaceNameTag(DefaultTags.States.Navigating);

            Waypoint currentStep = currentPath[currentPathIndex];
            Vector2 currentPos = transform.position;

            if (isEvading)
            {
                evadeTimer -= Time.deltaTime;

                myAgent.MyPhysics.body.velocity = evadeDirection * (speed * 1.5f);
                FlipSprite(evadeDirection);

                if (evadeTimer <= 0f)
                {
                    isEvading = false;
                    stuckTimer = 0f;
                }
                return;
            }

            float dist = Vector2.Distance(currentPos, currentStep.Position);

            if (dist > 0.15f)
            {
                Vector2 direction = (currentStep.Position - currentPos).normalized;
                myAgent.MyPhysics.body.velocity = direction * speed;
                FlipSprite(direction);

                if (Vector2.Distance(currentPos, lastPosition) < 0.005f)
                {
                    stuckTimer += Time.deltaTime;

                    if (stuckTimer > 0.4f)
                    {
                        isEvading = true;
                        evadeTimer = 0.3f;

                        float sign = UnityEngine.Random.value > 0.5f ? 1f : -1f;
                        evadeDirection = new Vector2(-direction.y * sign, direction.x * sign).normalized;

                        LogManager.LogWarning($"[AI Brain] {baseName} travou na quina! Executando Manobra Evasiva.");
                    }
                }
                else
                {
                    stuckTimer = 0f;
                }
                lastPosition = currentPos;
            }
            else
            {
                currentPathIndex++;
                stuckTimer = 0f;
            }
        }

        private void ChangeRandomDirection()
        {
            float angle = UnityEngine.Random.Range(0f, 360f);
            currentDirection = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            directionChangeTimer = UnityEngine.Random.Range(1.2f, 3.5f);
        }

        private void FlipSprite(Vector2 direction)
        {
            if (spriteRenderer != null && Mathf.Abs(direction.x) > 0.01f)
            {
                spriteRenderer.flipX = direction.x < 0;
            }
        }

        public void AddNameTag(string tag, string hexColour, IdentifierEnum identifier, string size = "80%", float? expiresAt = null)
        {
            tags.Add(new AgentTag { Tag = tag, ColourHex = hexColour, Size = size, Identifier = identifier, ExpiresAt = expiresAt });
            RefreshNameTag();
        }

        public void AddNameTag(AgentTag tag)
        {
            tags.Add(tag);
            RefreshNameTag();
        }

        public void RemoveNameTag(IdentifierEnum identifier)
        {
            tags.RemoveAll(t => t.Identifier == identifier);
            RefreshNameTag();
        }

        public void RemoveNameTag(AgentTag tag)
        {
            tags.RemoveAll(t => t == tag);
            RefreshNameTag();
        }

        public void ReplaceNameTag(IdentifierEnum identifier, string newTag, string newHexColour, string newSize = "80%", float? expiresAt = null)
        {
            var existing = tags.Find(t => t.Identifier == identifier);
            if (existing != null)
            {
                existing.Tag = newTag;
                existing.ColourHex = newHexColour;
                existing.Size = newSize;
                if (expiresAt != null)
                {
                    existing.ExpiresAt = expiresAt;
                }
                else
                {
                    if (existing.ExpiresAt != null)
                    {
                        existing.ExpiresAt = null;
                    }
                }
                RefreshNameTag();
            }
            else
            {
                AddNameTag(newTag, newHexColour, identifier, newSize);
            }
        }

        public void ReplaceNameTag(IdentifierEnum identifier, AgentTag tag)
        {
            var existing = tags.Find(t => t.Identifier == identifier);
            if (existing != null)
            {
                existing.Tag = tag.Tag;
                existing.ColourHex = tag.ColourHex;
                existing.Size = tag.Size;
                RefreshNameTag();
            }
            else
            {
                AddNameTag(tag);
            }
        }

        public void ReplaceNameTag(AgentTag tag)
        {
            var existing = tags.Find(t => t.Identifier == tag.Identifier);
            if (existing != null)
            {
                existing.Tag = tag.Tag;
                existing.ColourHex = tag.ColourHex;
                existing.Size = tag.Size;
                RefreshNameTag();
            }
            else
            {
                AddNameTag(tag);
            }
        }

        public void SetTags(List<AgentTag> newTags)
        {
            tags = newTags;
            RefreshNameTag();
        }

        private void RefreshNameTag()
        {
            if (nameTextComp != null)
            {
                StringBuilder builder = new();
                
                var states = tags.FindAll(t => t.Identifier == IdentifierEnum.State);
                var emotions = tags.FindAll(t => t.Identifier == IdentifierEnum.Emotion);
                var thoughts = tags.FindAll(t => t.Identifier == IdentifierEnum.Think);

                StringBuilder append(AgentTag tag) => builder.Append($"<color={tag.ColourHex}><size={tag.Size}>{tag.Tag}</size></color>\n");

                foreach (AgentTag tag in thoughts) append(tag);
                foreach (AgentTag tag in emotions) append(tag);
                foreach (AgentTag tag in states) append(tag);

                nameTextComp.text = $"{builder}{baseName}\n";
            }
        }

        public void StartSimulatedTask(PlayerTask task, float duration)
        {
            if (task == null) return;

            currentLocalTask = task;
            waitTimer = duration;
            currentState = AgentState.Stopped;

            if (myAgent.MyPhysics?.body != null)
                myAgent.MyPhysics.body.velocity = Vector2.zero;

            ReplaceNameTag(IdentifierEnum.Emotion, "Focused", "#FFFF00");
        }
    
        private void SawABody(List<RoundDeadBody> bodies)
        {
            if (myAgent.Data.IsDead) return;
            LogManager.LogDebug("Agente viu um corpo!");
            if (bodies.Count < 1)
            {
                LogManager.LogDebug("Não há corpos na função");
                return;
            }
            
            ReplaceNameTag(DefaultTags.Emotions.Scared);
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

                if (pathDistance < 10f)
                {
                    isThereSomeoneNearby = true;
                    break;
                }
            }

            if (isThereSomeoneNearby) shouldLookAround += 0.45;

            var playerInfoToReport = GameData.Instance.GetPlayerById(mostRecentBody.PlayerId);
            if (!Utils.ExecuteProbability(shouldLookAround))
            {
                LogManager.LogDebug("Reportará sem olhar ao redor");

                if (playerInfoToReport != null)
                {
                    bool canReport = CanReportBody(mostRecentBody.Position);
                    if (!canReport)
                    {
                        var end = Pathfinder.GetClosestNode(mostRecentBody.Position);

                        var path = Pathfinder.FindPath(start, end, out float dist);
                        CommandGoToPath(path);

                        isOnlyPredefinedAction = false;
                        updateAction = () =>
                        {
                            bool canReport = CanReportBody(mostRecentBody.Position);
                            if (canReport)
                            {
                                myAgent.CmdReportDeadBody(playerInfoToReport);
                                return true;
                            }
                            return false;
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
                float patrolRadius = 7f;

                Vector2[] directions =
                [
                    Vector2.up,                     // Cima
                    Vector2.down,                   // Baixo
                    Vector2.left,                   // Esquerda
                    Vector2.right,                  // Direita
                    new Vector2( 1,  1).normalized, // Nordeste (Diagonal)
                    new Vector2(-1,  1).normalized, // Noroeste (Diagonal)
                    new Vector2( 1, -1).normalized, // Sudeste (Diagonal)
                    new Vector2(-1, -1).normalized  // Sudoeste (Diagonal)
                ];

                List<Waypoint> patrolPoints = [];
                Waypoint agentNodeStart = Pathfinder.GetClosestNode(myAgent.transform.position);

                foreach (Vector2 dir in directions)
                {
                    Vector2 targetPosition = bodyPos + (dir * patrolRadius);
                    Waypoint node = Pathfinder.GetClosestNode(targetPosition, 1.5f);

                    if (node != null)
                    {
                        if (Vector2.Distance(myAgent.transform.position, node.Position) < 2f)
                            continue;

                        Pathfinder.FindPath(agentNodeStart, node, out float walkingDistToNode);
                        if (walkingDistToNode > 12f)
                            continue;

                        bool isRedundant = false;
                        foreach (Waypoint existingPoint in patrolPoints)
                        {
                            if (Vector2.Distance(node.Position, existingPoint.Position) < 2.5f)
                            {
                                isRedundant = true;
                                break;
                            }
                        }

                        if (!isRedundant)
                        {
                            patrolPoints.Add(node);
                        }
                    }
                }

                LogManager.LogDebug($"[Agente {myAgent.PlayerId}] Encontrou {patrolPoints.Count} pontos válidos na mesma área para patrulhar.");

                isOnlyPredefinedAction = false;
                updateAction = () =>
                {
                    if (patrolPoints.Count == 0)
                    {
                        if (playerInfoToReport != null)
                        {
                            bool canReport = CanReportBody(bodyPos);
                            if (!canReport)
                            {
                                var start = Pathfinder.GetClosestNode(myAgent.transform.position);
                                var end = Pathfinder.GetClosestNode(bodyPos);

                                var path = Pathfinder.FindPath(start, end, out float dist);
                                CommandGoToPath(path);
                            }
                            else
                            {
                                myAgent.CmdReportDeadBody(playerInfoToReport);
                                currentPath = null;
                                currentPathIndex = 0;
                            }
                        }
                        else
                            LogManager.LogError($"[Agente {myAgent.PlayerId}] Tentou reportar corpo do ID {mostRecentBody.PlayerId}, mas o registro não existe mais!");
                        return true;
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
                            var backupTarget = patrolPoints[0];
                            List<Waypoint> backupPath = Pathfinder.FindPath(currentAgentNode, backupTarget, out _);
                            CommandGoToPath(backupPath);
                            patrolPoints.RemoveAt(0);
                        }
                    }
                    return false;
                };
            }
        }

        public bool CanReportBody(Vector2 bodyPosition)
        {
            var start = Pathfinder.GetClosestNode(myAgent.transform.position);
            var end = Pathfinder.GetClosestNode(bodyPosition);

            var path = Pathfinder.FindPath(start, end, out float dist);
            var minimumDist = 5.4f;
            
            return minimumDist > dist;
        }
    }
}