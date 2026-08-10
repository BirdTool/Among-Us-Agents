using AMG.AI.Mind.Decisions;
using AMG.AI.Tools;
using AMG.Enums;
using AMG.Enums.AgentEnums;
using AMG.Interfaces;
using AMG.Models;
using AMG.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace AMG.AI.Mind
{
    public partial class AgentBrain : MonoBehaviour
    {
        public AgentBrain(IntPtr ptr) : base(ptr) { }

        private PlayerControl myAgent;
        private string baseName;
        private TextMeshPro nameTextComp;
        private SpriteRenderer spriteRenderer;
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

        public PlayerControl AgentControl => myAgent;

        public static bool AgentControlsRealPlayer = false;
        public Vector2 DesiredVelocity { get; private set; } = Vector2.zero;

        public static List<Func<bool>> OnReachedTheCurrentPath = [];

        private void SetVelocity(Vector2 v)
        {
            DesiredVelocity = v;
            if (myAgent.MyPhysics?.body != null)
                myAgent.MyPhysics.body.velocity = v;
        }

        void Awake()
        {
            myAgent = this.GetComponent<PlayerControl>();
            nameTextComp = this.GetComponentInChildren<TextMeshPro>();
            spriteRenderer = this.GetComponent<SpriteRenderer>();

            tags = [];
            // speed = 3.2f;

            if (nameTextComp != null)
            {
                nameTextComp.alignment = TMPro.TextAlignmentOptions.Bottom;
                nameTextComp.rectTransform.pivot = new Vector2(0.5f, 0f);
            }

            var playerInfo = GameData.Instance?.GetPlayerById(myAgent.PlayerId);
            baseName = playerInfo?.PlayerName ?? "AI";

            _updateActions = new()
            {
                [AgentState.Wandering] = UpdateWandering,
                [AgentState.Stopped] = UpdateStopped,
                [AgentState.Navigating] = UpdateNavigating,
                [AgentState.OnMeeting] = UpdateMeetingState,
                [AgentState.SmartWandering] = UpdateSmartWandering,
                [AgentState.DoingTask] = UpdateDoingTask,
                [AgentState.Calculating] = UpdateCalculating,
                [AgentState.FixingSabotage] = UpdateFixingSabotage
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
                [AgentState.FixingSabotage] = DefaultTags.States.FixingSabotage
            };

            Utils.OnSabotageStarted += HandleSabotageStarted;
            Utils.OnSabotageEnded += HandleSabotageEnded;

            ChangeRandomDirection();
        }

        void Update()
        {
            if (myAgent == null || myAgent.MyPhysics?.body == null) return;

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

            /*
            if (!_noticedASabotage && Utils.IsAnySabotageActive)
            {
                _noticedASabotage = true;
                SetState(AgentState.Calculating);
            }
            else if (_noticedASabotage && !Utils.IsAnySabotageActive)
            {
                _noticedASabotage = false;
                isGoingToFixASabotage = false;
                currentSabotageStep = null;
                SetState(AgentState.Calculating);
            }
            */

            _updateActions[currentState]?.Invoke();
        }

        private void OnDestroy()
        {
            Utils.OnSabotageStarted -= HandleSabotageStarted;
            Utils.OnSabotageEnded -= HandleSabotageEnded;
        }

        private void HandleSabotageStarted(ISabotage newSabotage)
        {
            if (myAgent.Data.IsDead) return;
            
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
    }
}