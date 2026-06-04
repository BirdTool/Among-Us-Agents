using System;
using System.Collections.Generic;
using UnityEngine;
using AMG.Utilities;
using AMG.AI.Navigation;
using TMPro;

namespace AMG.AI.Mind
{
    public enum AgentState { Wandering, Waiting, Navigating }

    public class AgentBrain : MonoBehaviour
    {
        public AgentBrain(IntPtr ptr) : base(ptr) { }

        private PlayerControl myAgent;
        private string baseName;
        private TextMeshPro nameTextComp;
        private SpriteRenderer spriteRenderer;

        public AgentState currentState = AgentState.Wandering;

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

        void Awake()
        {
            myAgent = this.GetComponent<PlayerControl>();
            nameTextComp = this.GetComponentInChildren<TextMeshPro>();
            spriteRenderer = this.GetComponent<SpriteRenderer>();

            var playerInfo = GameData.Instance.GetPlayerById(myAgent.PlayerId);
            baseName = playerInfo?.PlayerName ?? "AI";

            ChangeRandomDirection();
        }

        public void CommandGoToPosition(Vector2 callerPosition)
        {
            targetNode = Pathfinder.GetClosestNode(callerPosition);

            if (targetNode != null)
            {
                currentState = AgentState.Waiting;
                waitTimer = 10f;

                if (myAgent.MyPhysics?.body != null) myAgent.MyPhysics.body.velocity = Vector2.zero;

                LogManager.LogDebug($"[AI Brain] Recebeu chamado! Aguardando 10s para ir até {targetNode.Position}");
            }
        }

        void Update()
        {
            if (myAgent == null || myAgent.MyPhysics?.body == null) return;

            switch (currentState)
            {
                case AgentState.Wandering:
                    UpdateWandering();
                    break;
                case AgentState.Waiting:
                    UpdateWaiting();
                    break;
                case AgentState.Navigating:
                    UpdateNavigating();
                    break;
            }
        }

        private void UpdateWandering()
        {
            directionChangeTimer -= Time.deltaTime;
            if (directionChangeTimer <= 0f) ChangeRandomDirection();

            Vector2 velocity = currentDirection * speed;
            myAgent.MyPhysics.body.velocity = velocity;
            FlipSprite(currentDirection);

            UpdateNameTag("Wandering", "#00FFFF");
        }

        private void UpdateWaiting()
        {
            waitTimer -= Time.deltaTime;
            UpdateNameTag($"Aguardando {Mathf.CeilToInt(waitTimer)}s", "#FFA500");

            if (waitTimer <= 0f)
            {
                Waypoint startNode = AMG.AI.Navigation.Pathfinder.GetClosestNode(transform.position);
                currentPath = AMG.AI.Navigation.Pathfinder.FindPath(startNode, targetNode);
                currentPathIndex = 0;

                if (currentPath != null && currentPath.Count > 0)
                {
                    currentState = AgentState.Navigating;
                }
                else
                {
                    UpdateNameTag("Rota Impossível", "#FF0000");
                    currentState = AgentState.Wandering;
                }
            }
        }

        private void UpdateNavigating()
        {
            UpdateNameTag("Seguindo Jogador", "#00FF00");

            if (currentPath == null || currentPathIndex >= currentPath.Count)
            {
                myAgent.MyPhysics.body.velocity = Vector2.zero;
                currentState = AgentState.Wandering;
                return;
            }

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

        private void UpdateNameTag(string status, string hexColor)
        {
            if (nameTextComp != null)
            {
                nameTextComp.text = $"<color={hexColor}><size=80%>{status}</size></color>\n{baseName}";
            }
        }
    }
}