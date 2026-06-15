using AMG.AI.Navigation;
using AMG.AI.Tools;
using AMG.Enums;
using AMG.Enums.AgentEnums;
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

        // Anti Travamento
        private Vector2 lastPosition = Vector2.zero;
        private float stuckTimer = 0f;
        private bool isEvading = false;
        private float evadeTimer = 0f;
        private Vector2 evadeDirection = Vector2.zero;

        public Func<bool> updateAction = null;
        public bool isOnlyPredefinedAction = false;

        public bool sawABody = false; // Defined as false every meeting

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

            ChangeRandomDirection();
        }

        void Update()
        {
            if (myAgent == null || myAgent.MyPhysics?.body == null) return;

            if (updateAction != null)
            {
                bool isActionFinished = updateAction.Invoke();

                if (isActionFinished)
                {
                    updateAction = null;
                    isOnlyPredefinedAction = false;
                }
                else if (isOnlyPredefinedAction)
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

            if (!sawABody && Utils.Round.CurrentRoundDeadBodies != null && Utils.Round.CurrentRoundDeadBodies.Count > 0)
            {
                List<RoundDeadBody> bodies = Utils.Round.CurrentRoundDeadBodies;
                List<RoundDeadBody> nearbyBodies = [];

                foreach (var body in bodies)
                {
                    var origin = myAgent.transform.position;
                    var target = body.Position;

                    Vector2 origin2D = new Vector2(origin.x, origin.y + 0.5f);

                    float distToBody = Vector2.Distance(origin2D, target);
                    if (distToBody > 5f) continue;

                    var canSee = Utils.CanSeeTheTarget(origin2D, target, distToBody);
                    if (canSee) nearbyBodies.Add(body);
                }

                if (nearbyBodies.Count > 0)
                {
                    SawABody(nearbyBodies);
                    sawABody = true;
                }
            }

            if (Utils.IsMeeting && currentState != AgentState.OnMeeting) { currentState = AgentState.OnMeeting; }

            switch (currentState)
            {
                case AgentState.Wandering:
                    UpdateWandering();
                    break;
                case AgentState.Stopped:
                    UpdateStopped();
                    break;
                case AgentState.Navigating:
                    UpdateNavigating();
                    break;
                case AgentState.OnMeeting:
                    UpdateMeetingState();
                    break;
                case AgentState.SmartWandering:
                    UpdateSmartWandering();
                    break;
            }
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