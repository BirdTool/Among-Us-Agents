using System;
using System.IO;
using AMG.AI.Control.AgentController;
using UnityEngine;
using UnityEngine.UIElements.UIR;

namespace AMG.AI.Mind.MapTrackerBrain
{
    internal enum TrackerBrainStates { RandomWalking, Navigating, Stopped };

    public partial class MapTrackerBrain(IntPtr ptr) : AgentController(ptr)
    {
        private Vector2 currentDirection = Vector2.zero;
        private float directionChangeTimer = 0f;
        private TrackerBrainStates currentState = TrackerBrainStates.RandomWalking;

        protected override void Awake()
        {
            base.Awake();

            ChangeRandomDirection();
        }

        void Update()
        {
            if (CurrentPath != null && CurrentPath.Count > 0)
                currentState = TrackerBrainStates.Navigating;
            else
                currentState = TrackerBrainStates.RandomWalking;
                
            switch(currentState)
            {
                case TrackerBrainStates.Stopped:
                    break;

                case TrackerBrainStates.Navigating:
                    bool? hasReachedDestination = ProcessPathMovement();

                    if (hasReachedDestination == true || hasReachedDestination == null)
                    {
                        ResetPath();

                        currentState = TrackerBrainStates.Stopped;
                    }
                    break;

                case TrackerBrainStates.RandomWalking:
                    directionChangeTimer -= Time.deltaTime;
                    if (directionChangeTimer <= 0f) ChangeRandomDirection();
                    Vector2 velocity = currentDirection * Speed;
                SetVelocity(velocity);
                    break;
            }
        }

        private void ChangeRandomDirection()
        {
            float angle;

            if (UnityEngine.Random.value < 0.75f)
            {
                int randomOrthogonal = UnityEngine.Random.Range(0, 4);
                angle = randomOrthogonal * 90f;
            }
            else
            {
                angle = UnityEngine.Random.Range(0f, 360f);
            }

            currentDirection = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            directionChangeTimer = UnityEngine.Random.Range(1.2f, 3.5f);
        }
    }
}