using System;
using TMPro;
using UnityEngine;

namespace AMG.AI.Control.AgentController
{
    public partial class AgentController(IntPtr ptr) : MonoBehaviour(ptr)
    {
        public PlayerControl Agent { get; private set; }
        protected string BaseName;
        protected TextMeshPro NameTextComp;
        protected SpriteRenderer SpriteRenderer;
        public Vector2 DesiredVelocity { get; private set; }
        public static bool AgentControlsRealPlayer = false;

        protected void SetVelocity(Vector2 v)
        {
            DesiredVelocity = v;
            if (Agent.MyPhysics?.body != null)
                Agent.MyPhysics.body.velocity = v;
        }

        protected virtual void Awake()
        {
            Agent = this.GetComponent<PlayerControl>();
            NameTextComp = this.GetComponentInChildren<TextMeshPro>();
            SpriteRenderer = this.GetComponent<SpriteRenderer>();

            if (NameTextComp != null)
            {
                NameTextComp.alignment = TMPro.TextAlignmentOptions.Bottom;
                NameTextComp.rectTransform.pivot = new Vector2(0.5f, 0f);
            }

            var playerInfo = GameData.Instance?.GetPlayerById(Agent.PlayerId);
            BaseName = playerInfo?.PlayerName ?? "AI";
        }
    }
}