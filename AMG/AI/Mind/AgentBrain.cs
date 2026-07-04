using AMG.AI.Mind.Decisions;
using AMG.AI.Tools;
using AMG.Enums;
using AMG.Enums.AgentEnums;
using AMG.Models;
using AMG.Utilities;
using System;
using System.Collections.Generic;
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

        public float delayTime = 0.3f;
        public float delayDisturb = 0f; // Range of delayTime's disturb

        public AgentState currentState = AgentState.Stopped;

        // Chamado
        // private Waypoifnt targetNode = null;
        // private float waitTimer = 0f;

        public AgentUpdateAction updateAction = null;

        public bool sawABody = false; // Defined as false every meeting

        private Dictionary<AgentState, Action> _updateActions;
        private Dictionary<AgentState, AgentTag> _updateTags;

        public PlayerControl AgentControl => myAgent;

        void Awake()
        {
            myAgent = this.GetComponent<PlayerControl>();
            nameTextComp = this.GetComponentInChildren<TextMeshPro>();
            spriteRenderer = this.GetComponent<SpriteRenderer>();

            tags = new List<AgentTag>();
            speed = 3.2f;

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
                [AgentState.Calculating] = UpdateCalculating
            };

            _updateTags = new()
            {
                [AgentState.Wandering] = DefaultTags.States.Wandering,
                [AgentState.Stopped] = DefaultTags.States.Stopped,
                [AgentState.Navigating] = DefaultTags.States.Navigating,
                [AgentState.OnMeeting] = DefaultTags.States.Stopped,
                [AgentState.SmartWandering] = DefaultTags.States.SmartWandering,
                [AgentState.DoingTask] = DefaultTags.States.DoingTask,
                [AgentState.Calculating] = DefaultTags.States.Calculating
            };

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
                    updateAction = null;
                    updateAction.IsOnlyPredefinedAction = false;
                }
                else if (updateAction.IsOnlyPredefinedAction)
                {
                    return;
                }
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

            if (Utils.IsMeeting && currentState != AgentState.OnMeeting) { currentState = AgentState.OnMeeting; }

            _updateActions[currentState]?.Invoke();
        }

        private void UpdateStopped()
        {
            ReplaceNameTag(DefaultTags.States.Stopped);
        }
    }
}