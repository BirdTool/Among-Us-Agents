using System;
using System.Collections.Generic;
using AMG.AI.Control.AgentController;
using AMG.AI.Mind.StructuredAgentBrain.Decisions;
using AMG.AI.Navigation;
using AMG.AI.Tools;
using AMG.Enums.AgentEnums;
using AMG.Interfaces;
using AMG.Models;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Mind.StructuredAgentBrain
{
    public partial class StructuredAgentBrain(IntPtr ptr) : AgentController(ptr)
    {
        public PlayerTask currentLocalTask = null;
        public bool isGoingToFixASabotage = false;
        public bool _noticedASabotage = false;

        public float delayTime = 0.3f;
        public float delayDisturb = 0f; // Range of delayTime's disturb

        public AgentState currentState = AgentState.Stopped;

        public AgentUpdateAction updateAction = null;
        public List<AgentTempParallelDecision> tempParallelDecisions = [];

        public bool sawABody = false; // Defined as false every meeting

        private Dictionary<AgentState, Action> _updateActions;
        private Dictionary<AgentState, AgentTag> _updateTags;

        public SabotageStep currentSabotageStep = null;

        protected override void Awake()
        {
            base.Awake();

            _updateActions = new()
            {
                [AgentState.Wandering] = UpdateWandering,
                [AgentState.Stopped] = UpdateStopped,
                [AgentState.Navigating] = UpdateNavigating,
                [AgentState.OnMeeting] = UpdateMeetingState,
                [AgentState.SmartWandering] = UpdateSmartWandering,
                [AgentState.DoingTask] = UpdateDoingTask,
                [AgentState.Calculating] = UpdateCalculating,
                [AgentState.FixingSabotage] = UpdateFixingSabotage,
                [AgentState.InVent] = UpdateInVent
            };

            _updateTags = new()
            {
                [AgentState.Wandering] = DefaultTags.States.Wandering,
                [AgentState.Stopped] = DefaultTags.States.Stopped,
                [AgentState.Navigating] = DefaultTags.States.Navigating,
                [AgentState.OnMeeting] = DefaultTags.States.Stopped,
                [AgentState.SmartWandering] = DefaultTags.States.SmartWandering,
                [AgentState.DoingTask] = DefaultTags.States.DoingTask,
                [AgentState.Calculating] = DefaultTags.States.Calculating,
                [AgentState.FixingSabotage] = DefaultTags.States.FixingSabotage,
                [AgentState.InVent] = DefaultTags.States.InVent
            };

            OnStuckedInPath = () => SetState(AgentState.Calculating);

            Utils.OnSabotageStarted += HandleSabotageStarted;
            Utils.OnSabotageEnded += HandleSabotageEnded;

            ChangeRandomDirection();
        }

        void Update()
        {
            if (Agent == null || Agent.MyPhysics?.body == null) return;

            if (_timeSinceStartedVenting != null)
            {
                SetState(AgentState.InVent);
                if (Time.time - _timeSinceStartedVenting > VENTING_ANIMATION_DURATION)
                {
                    _timeSinceStartedVenting = null;
                    currentVent = currentVentToEnter;
                    currentVentToEnter = null;
                    Agent.inVent = true;
                }
                return;
            }

            if (currentVent != null && currentState != AgentState.InVent)
            {
                SafeLeaveVent(currentVent);
                currentVent = null;
                Agent.Collider?.enabled = true;
                SetState(AgentState.Calculating);
            }

            if (currentVent == null && Agent.inVent == true)
            {
                Agent.inVent = false;
                SetState(AgentState.Calculating);
            }

            if (updateAction != null)
            {
                bool isActionFinished = updateAction.Execute();

                if (isActionFinished)
                {
                    updateAction.IsOnlyPredefinedAction = false;
                    updateAction = null;
                }
                else if (updateAction.IsOnlyPredefinedAction)
                {
                    return;
                }
            }

            if (tempParallelDecisions.Count > 0)
            {
                
                for (int i = tempParallelDecisions.Count - 1; i >= 0; i--)
                {
                    var decision = tempParallelDecisions[i];

                    if (decision.IsTimeLimitExpired)
                    {
                        tempParallelDecisions.Remove(decision);
                        continue;
                    }
                    if (decision.DeleteOnMeeting && Utils.IsMeeting)
                    {
                        tempParallelDecisions.Remove(decision);
                        continue;
                    }

                    var isCompleted = decision.Execute();

                    if (isCompleted)
                    {
                        tempParallelDecisions.Remove(decision);
                    }
                }
            }

            if (PlanManager != null && PlanManager.QueuePlans.Count > 0)
            {
                PlanManager.Execute();
            }

            foreach (var tag in tags)
            {
                if (tag.ExpiresAt != null && Time.time > tag.ExpiresAt)
                {
                    RemoveNameTag(tag);
                }
            }

            var parallelActions = DecisionsGroup.AllParallelDecisions;
            foreach (var parallelAction in parallelActions)
            {
                parallelAction.Evaluate(this);
            }

            if (Utils.IsMeeting && currentState != AgentState.OnMeeting) { SetState(AgentState.OnMeeting); }

            _updateActions[currentState]?.Invoke();
        }


        private void OnDestroy()
        {
            Utils.OnSabotageStarted -= HandleSabotageStarted;
            Utils.OnSabotageEnded -= HandleSabotageEnded;
        }

        private void HandleSabotageStarted(ISabotage newSabotage)
        {
            if (Agent.Data.IsDead) return;
            
            _noticedASabotage = true;
            SetState(AgentState.Calculating);
        }

        private void HandleSabotageEnded()
        {
            _noticedASabotage = false;
            isGoingToFixASabotage = false;
            currentSabotageStep = null;
            SetState(AgentState.Calculating);
        }

        private void UpdateStopped()
        {
            ReplaceNameTag(DefaultTags.States.Stopped);
        }

        public void TriggerCalculatingDelay(float delay)
        {
            SetState(AgentState.Calculating);
            _calculatingTimer.StartDelay(delay);
        }

        public void ResetDestinations()
        {
            isGoingToFixASabotage = false;
            currentLocalTask = null;
            currentSabotageStep = null;
            currentVentToEnter = null;
        }

        public override void CommandGoToPath(List<Waypoint> path)
        {
            base.CommandGoToPath(path);

            SetState(AgentState.Navigating);
        }
    }
}