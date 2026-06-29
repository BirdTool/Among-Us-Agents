using AMG.AI.Navigation;
using AMG.Enums.AgentEnums;
using AMG.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace AMG.AI.Mind
{
    public partial class AgentBrain
    {
        private List<Waypoint> currentPath = null;
        private int currentPathIndex = 0;
        private float speed = 3.2f;

        private Vector2 lastPosition = Vector2.zero;
        private float stuckTimer = 0f;
        private bool isEvading = false;
        private float evadeTimer = 0f;
        private Vector2 evadeDirection = Vector2.zero;

        private float lastEvasionSign = 1f;

        private bool ProcessPathMovement()
        {
            if (currentPath == null || currentPathIndex >= currentPath.Count)
            {
                StopMoving();
                return true;
            }

            Waypoint currentStep = currentPath[currentPathIndex];
            Vector2 currentPos = transform.position;

            StartMoving();

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
                return false;
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
                        if (currentStep != null) currentStep.IncreaseStuckHot();

                        isEvading = true;
                        evadeTimer = 0.3f;

                        lastEvasionSign = -lastEvasionSign;
                        evadeDirection = new Vector2(-direction.y * lastEvasionSign, direction.x * lastEvasionSign).normalized;

                        LogManager.LogWarning($"[AI Brain] {baseName} travou na quina! Executando Manobra Evasiva alternada.");
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

            return currentPathIndex >= currentPath.Count;
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

        private void FlipSprite(Vector2 direction)
        {
            if (spriteRenderer != null && Mathf.Abs(direction.x) > 0.01f)
            {
                spriteRenderer.flipX = direction.x < 0;
            }
        }

        public void ResetPath()
        {
            currentPath = null;
            currentPathIndex = 0;
            isEvading = false;

            if (myAgent.MyPhysics?.body != null)
                myAgent.MyPhysics.body.velocity = Vector2.zero;
        }
    }
}