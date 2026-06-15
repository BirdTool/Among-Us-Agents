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

        private bool ProcessPathMovement()
        {
            if (currentPath == null || currentPathIndex >= currentPath.Count) return true;

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
    }
}
