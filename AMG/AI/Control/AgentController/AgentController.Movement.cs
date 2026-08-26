using System.Collections.Generic;
using System.Linq;
using AMG.AI.Navigation;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Control.AgentController
{
    public partial class AgentController
    {
        public List<Waypoint> CurrentPath { get; protected set; } = null;
        public int CurrentPathIndex { get; protected set; } = 0;
        protected const float ComfortFactor = 0.93f;
        protected float Speed => Agent.MyPhysics.Speed * 1.75f * ComfortFactor;

        protected Vector2 LastPosition = Vector2.zero;
        protected float StuckTimer = 0f;
        protected bool IsEvading = false;
        protected float EvadeTimer = 0f;
        protected Vector2 EvadeDirection = Vector2.zero;

        protected float LastEvasionSign = 1f;
        protected bool StopForced = false;

        protected int StuckCount = 0;
        protected float? LastTimeStucked = null;

        // True if the path is completed
        protected virtual bool? ProcessPathMovement()
        {
            if (StopForced) 
            { 
                StopForced = false; 
                return null; 
            }
            
            if (CurrentPath == null || CurrentPathIndex >= CurrentPath.Count) return true;

            Waypoint currentStep = CurrentPath[CurrentPathIndex];
            Vector2 currentPos = transform.position;

            if (CurrentPathIndex > 0)
            {
                Waypoint previousStep = CurrentPath[CurrentPathIndex - 1];
                if (previousStep.Room != currentStep.Room &&
                    (Utils.IsRoomClosed(previousStep.Room) || Utils.IsRoomClosed(currentStep.Room)))
                {
                    ResetPath();

                    return null;
                }
            }

            if (IsEvading)
            {
                EvadeTimer -= Time.deltaTime;

                var newVelocity = EvadeDirection * (Speed * 1.5f);
                SetVelocity(newVelocity);
                FlipSprite(EvadeDirection);

                if (EvadeTimer <= 0f)
                {
                    IsEvading = false;
                    StuckCount++;

                    LastTimeStucked = Time.time;
                    StuckTimer = 0f;
                }
                return false;
            }

            if (StuckCount > 2)
            {
                if (LastTimeStucked == null)
                {
                    StuckCount = 0;
                }
                else
                {
                    float secondsSinceLastStuck = Time.time - LastTimeStucked.Value;
                    if (secondsSinceLastStuck > 8)
                    {
                        StuckCount = 0;
                        LastTimeStucked = null;
                        ResetPath(true);
                        OnStuckedInPath?.Invoke();
                    }
                }
            }

            float dist = Vector2.Distance(currentPos, currentStep.Position);

            if (dist > 0.15f)
            {
                Vector2 direction = (currentStep.Position - currentPos).normalized;
                var newVelocity = direction * Speed;
                SetVelocity(newVelocity);
                FlipSprite(direction);

                if (Vector2.Distance(currentPos, LastPosition) < 0.005f)
                {
                    StuckTimer += Time.deltaTime;

                    if (StuckTimer > 0.4f)
                    {
                        currentStep?.IncreaseStuckHot();

                        IsEvading = true;
                        EvadeTimer = 0.3f;

                        LastEvasionSign = -LastEvasionSign;
                        EvadeDirection = new Vector2(-direction.y * LastEvasionSign, direction.x * LastEvasionSign).normalized;

                        LogManager.LogWarning($"[AI Brain] {BaseName} travou na quina! Executando Manobra Evasiva alternada.");
                    }
                }
                else
                {
                    StuckTimer = 0f;
                }
                LastPosition = currentPos;
            }
            else
            {
                CurrentPathIndex++;
                StuckTimer = 0f;
            }

            var nextStep = CurrentPath.ElementAtOrDefault(CurrentPathIndex + 1);
            if (nextStep != null && currentStep.Room != nextStep.Room)
            {
                if (Utils.IsRoomClosed(currentStep.Room) || Utils.IsRoomClosed(nextStep.Room))
                {
                    CurrentPath = null; 
                    
                    return null; // It's not completed, but there's no path to follow anyway
                }
            }

            bool isCompleted = CurrentPathIndex >= CurrentPath.Count;
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

        public virtual void CommandGoToPath(List<Waypoint> path)
        {
            if (path == null || path.Count == 0) return;

            CurrentPath = path;
            CurrentPathIndex = 0;

            SetVelocity(Vector2.zero);
        }

        protected virtual void FlipSprite(Vector2 direction)
        {
            if (SpriteRenderer != null && Mathf.Abs(direction.x) > 0.01f)
            {
                SpriteRenderer.flipX = direction.x < 0;
            }
        }

        public virtual void ResetPath(bool isForced = false)
        {
            CurrentPath = null;
            CurrentPathIndex = 0;
            IsEvading = false;

            SetVelocity(Vector2.zero);

            if (isForced) StopForced = true;
        }
    }
}