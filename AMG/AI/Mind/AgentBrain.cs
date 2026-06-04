using System;
using UnityEngine;
using Il2CppInterop.Runtime.Injection;
using AMG.Utilities;
using AMG.AI.Navigation;
using TMPro;

namespace AMG.AI.Mind
{
    public class AgentBrain : MonoBehaviour
    {
        public AgentBrain(IntPtr ptr) : base(ptr) { }

        private Waypoint currentTarget = null;
        private System.Collections.Generic.List<Waypoint> currentPath = null;
        private int currentPathIndex = 0;

        private PlayerControl myAgent;
        private string baseName;
        private TextMeshPro nameTextComp;
        private SpriteRenderer spriteRenderer;

        private Vector2 lastPosition = Vector2.zero;
        private float stuckTimer = 0f;
        private float retryTimer = 0f;

        private LineRenderer pathRenderer;

        void Awake()
        {
            myAgent = this.GetComponent<PlayerControl>();
            nameTextComp = this.GetComponentInChildren<TextMeshPro>();
            spriteRenderer = this.GetComponent<SpriteRenderer>();

            pathRenderer = this.gameObject.AddComponent<LineRenderer>();
            pathRenderer.startWidth = 0.05f;
            pathRenderer.endWidth = 0.05f;
            pathRenderer.positionCount = 0;
            pathRenderer.material = new Material(Shader.Find("Sprites/Default"));
            pathRenderer.startColor = Color.yellow;
            pathRenderer.endColor = Color.yellow;
        }

        public void SetTarget(Waypoint target)
        {
            currentTarget = target;
            var playerInfo = GameData.Instance.GetPlayerById(myAgent.PlayerId);
            if (baseName == null) baseName = playerInfo.PlayerName;

            currentPath = AMG.AI.Navigation.Pathfinder.FindPath(transform.position, target);
            currentPathIndex = 0;
            stuckTimer = 0f;

            if (currentPath == null || currentPath.Count == 0)
            {
                if (nameTextComp != null) nameTextComp.text = $"<color=#FF0000><size=80%>Rota Falhou</size></color>\n{baseName}";
                if (pathRenderer != null) pathRenderer.positionCount = 0;
                return;
            }

            if (pathRenderer != null)
            {
                pathRenderer.positionCount = currentPath.Count + 1;
                pathRenderer.SetPosition(0, new Vector3(transform.position.x, transform.position.y, -1f));
                for (int i = 0; i < currentPath.Count; i++)
                {
                    pathRenderer.SetPosition(i + 1, new Vector3(currentPath[i].Position.x, currentPath[i].Position.y, -1f));
                }
            }

            string tagText = target.Type switch { WaypointType.TASK => "Indo fazer Task", WaypointType.VENT => "Indo para Duto", _ => "Andando" };
            if (nameTextComp != null) nameTextComp.text = $"<color=#FFFF00><size=80%>{tagText}</size></color>\n{baseName}";
        }

        void Update()
        {
            if (currentTarget == null || myAgent == null) return;

            if (pathRenderer != null && currentPath != null && pathRenderer.positionCount > 0)
            {
                pathRenderer.SetPosition(0, new Vector3(transform.position.x, transform.position.y, -1f));
            }

            if (currentPath == null || currentPathIndex >= currentPath.Count)
            {
                retryTimer -= Time.deltaTime;
                if (retryTimer <= 0f)
                {
                    SetTarget(currentTarget);
                    if (currentPath == null) retryTimer = 2.0f;
                }

                if (myAgent.MyPhysics != null && myAgent.MyPhysics.body != null) myAgent.MyPhysics.body.velocity = Vector2.zero;
                return;
            }

            Waypoint currentStep = currentPath[currentPathIndex];
            Vector2 currentPos = transform.position;
            float dist = Vector2.Distance(currentPos, currentStep.Position);

            if (dist > 0.1f)
            {
                Vector2 direction = (currentStep.Position - currentPos).normalized;
                if (spriteRenderer != null) spriteRenderer.flipX = (direction.x < 0);

                float speed = 3.0f;
                if (myAgent.MyPhysics != null && myAgent.MyPhysics.body != null) myAgent.MyPhysics.body.velocity = direction * speed;

                if (Vector2.Distance(currentPos, lastPosition) < 0.005f)
                {
                    stuckTimer += Time.deltaTime;
                    if (stuckTimer > 0.7f)
                    {
                        LogManager.LogWarning("[AI Brain] Preso na quina! Apagando rota para recalcular.");
                        currentPath = null;
                        stuckTimer = 0f;
                        retryTimer = 0f;
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

                if (currentPathIndex >= currentPath.Count)
                {
                    currentPath = null;
                    currentTarget = null;
                    if (pathRenderer != null) pathRenderer.positionCount = 0; // Apaga o laser

                    if (myAgent.MyPhysics != null && myAgent.MyPhysics.body != null) myAgent.MyPhysics.body.velocity = Vector2.zero;
                    if (nameTextComp != null) nameTextComp.text = $"<color=#00FF00><size=80%>Concluído!</size></color>\n{baseName}";
                }
            }
        }
    }
}