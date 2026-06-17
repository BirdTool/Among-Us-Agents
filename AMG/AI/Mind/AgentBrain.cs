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

        public AgentState currentState = AgentState.Stopped;

        // Chamado
        // private Waypoint targetNode = null;
        private float waitTimer = 0f;

        public AgentUpdateAction updateAction = null;

        public bool sawABody = false; // Defined as false every meeting

        private Dictionary<AgentState, Action> _updateActions;

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
                [AgentState.SmartWandering] = UpdateSmartWandering
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

            ExecuteHaveSeenNearbyBodiesAction(); // Execute always when there are bodies nearby

            if (Utils.IsMeeting && currentState != AgentState.OnMeeting) { currentState = AgentState.OnMeeting; }

            _updateActions[currentState]?.Invoke();
        }

        public void SetState(AgentState newState)
        {
            if (currentState != newState)
            {
                currentState = newState;
                switch (newState)
                {
                    case AgentState.Wandering:
                        ReplaceNameTag(DefaultTags.States.Wandering);
                        break;
                    case AgentState.OnMeeting:
                    case AgentState.Stopped:
                        ReplaceNameTag(DefaultTags.States.Stopped);
                        break;
                    case AgentState.Navigating:
                        ReplaceNameTag(DefaultTags.States.Navigating);
                        break;
                    case AgentState.SmartWandering:
                        ReplaceNameTag(DefaultTags.States.SmartWandering);
                        break;
                }
            }
        }

        private void UpdateStopped()
        {
            ReplaceNameTag(DefaultTags.States.Stopped);
        }

        public void StartSimulatedTask(PlayerTask task, float duration)
        {
            if (task == null) return;

            currentLocalTask = task;
            waitTimer = duration;
            currentState = AgentState.Stopped;

            if (myAgent.MyPhysics?.body != null)
                myAgent.MyPhysics.body.velocity = Vector2.zero;

            ReplaceNameTag(IdentifierEnum.Emotion, "Focused", "#FFFF00");
        }

        public bool CanReportBody(Vector2 bodyPosition)
        {
            float dist = Vector2.Distance(myAgent.transform.position, bodyPosition);
            return dist < 3.4f;
        }
    }
}