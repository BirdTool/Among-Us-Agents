using AMG.AI.Navigation;
using AMG.Enums.AgentEnums;
using AMG.Utilities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AMG.AI.Mind
{
    public partial class AgentBrain
    {
        public List<Waypoint> currentPath { get; private set; } = null;
        public int currentPathIndex { get; private set; } = 0;
        private float speed = 3.2f;

        private Vector2 lastPosition = Vector2.zero;
        private float stuckTimer = 0f;
        private bool isEvading = false;
        private float evadeTimer = 0f;
        private Vector2 evadeDirection = Vector2.zero;

        private float lastEvasionSign = 1f;
        private bool _stopForced = false;

        // True if the path is completed
        private bool? ProcessPathMovement()
        {
            if (_stopForced) 
            { 
                _stopForced = false; 
                return null; 
            }
            
            if (currentPath == null || currentPathIndex >= currentPath.Count) return true;

            Waypoint currentStep = currentPath[currentPathIndex];
            Vector2 currentPos = transform.position;

            if (currentPathIndex > 0)
            {
                Waypoint previousStep = currentPath[currentPathIndex - 1];
                if (previousStep.Room != currentStep.Room &&
                    (Utils.IsRoomClosed(previousStep.Room) || Utils.IsRoomClosed(currentStep.Room)))
                {
                    currentPath = null;

                    if (myAgent.MyPhysics?.body != null)
                        myAgent.MyPhysics.body.velocity = Vector2.zero;

                    return null;
                }
            }

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
                        currentStep?.IncreaseStuckHot();

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

            var nextStep = currentPath.ElementAtOrDefault(currentPathIndex + 1);
            if (nextStep != null && currentStep.Room != nextStep.Room)
            {
                if (Utils.IsRoomClosed(currentStep.Room) || Utils.IsRoomClosed(nextStep.Room))
                {
                    currentPath = null; 
                    
                    return null; // It's not completed, but there's no path to follow anyway
                }
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

        public void ResetPath(bool isForced = false)
        {
            currentPath = null;
            currentPathIndex = 0;
            isEvading = false;

            if (myAgent.MyPhysics?.body != null)
                myAgent.MyPhysics.body.velocity = Vector2.zero;

            if (isForced) _stopForced = true;
        }
    }
}