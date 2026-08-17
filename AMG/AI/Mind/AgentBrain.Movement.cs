using AMG.AI.Navigation;
using AMG.Enums.AgentEnums;
using AMG.Utilities;
using System.Collections.Generic;
using System.Linq;
using Unity.Services.Core.Scheduler.Internal;
using UnityEngine;

namespace AMG.AI.Mind
{
    public partial class AgentBrain
    {
        public List<Waypoint> currentPath { get; private set; } = null;
        public int currentPathIndex { get; private set; } = 0;
        private const float ComfortFactor = 0.93f;
        private float speed => myAgent.MyPhysics.Speed * 1.75f * ComfortFactor;

        private Vector2 lastPosition = Vector2.zero;
        private float stuckTimer = 0f;
        private bool isEvading = false;
        private float evadeTimer = 0f;
        private Vector2 evadeDirection = Vector2.zero;

        private float lastEvasionSign = 1f;
        private bool _stopForced = false;

        private int stuckCount = 0;
        private float? lastTimeStucked = null;

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
                    ResetPath();

                    return null;
                }
            }

            if (isEvading)
            {
                evadeTimer -= Time.deltaTime;

                var newVelocity = evadeDirection * (speed * 1.5f);
                SetVelocity(newVelocity);
                FlipSprite(evadeDirection);

                if (evadeTimer <= 0f)
                {
                    isEvading = false;
                    stuckCount++;

                    lastTimeStucked = Time.time;
                    stuckTimer = 0f;
                }
                return false;
            }

            if (stuckCount > 3)
            {
                if (lastTimeStucked == null)
                {
                    stuckCount = 0;
                }
                else
                {
                    float secondsSinceLastStuck = Time.time - lastTimeStucked.Value;
                    if (secondsSinceLastStuck > 8)
                    {
                        stuckCount = 0;
                        lastTimeStucked = null;
                        ResetPath(true);
                        SetState(AgentState.Calculating);
                    }
                }
            }

            float dist = Vector2.Distance(currentPos, currentStep.Position);

            if (dist > 0.15f)
            {
                Vector2 direction = (currentStep.Position - currentPos).normalized;
                var newVelocity = direction * speed;
                SetVelocity(newVelocity);
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

            bool isCompleted = currentPathIndex >= currentPath.Count;
            if (isCompleted)
            {
                for (int i = OnReachedTheCurrentPath.Count - 1; i >= 0; i--)
                {
                    var action = OnReachedTheCurrentPath[i];
                    action.Invoke();
                    OnReachedTheCurrentPath.RemoveAt(i);
                }
            }
            return isCompleted;
        }

        public void CommandGoToPath(List<Waypoint> path)
        {
            if (path == null || path.Count == 0) return;

            currentPath = path;
            currentPathIndex = 0;

            SetVelocity(Vector2.zero);

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

            SetVelocity(Vector2.zero);

            if (isForced) _stopForced = true;
        }
    }
}