using AMG.Enums.ChatServiceEnums;
using UnityEngine;

namespace AMG.Models.ChatService
{
    public class ChatIntent
    {
        public ChatIntentEnum Type;
        public string PhraseId = ""; // PhraseGroup.Id, e.g. "Kill_Culprit"
        public ChatContextEnum? Context; // null = any context
        public float Urgency = 0.5f;    // 0..1: higher = sent first, but also goes stale faster
        public float Confidence = 0.5f;

        public byte? Target;
        public byte? Subject;
        public SystemTypes? Room;
        public EvidenceEnum? Evidence; // no longer used to look up phrases (PhraseId does that)
        public byte? Witness;

        // Time.time when the intent was created
        public float CreatedAt = Time.time;

        // Seconds the intent stays valid. Negative = derived from Urgency (20s when calm, 8s when urgent
        public float MaxAge = -1f;

        public bool IsExpired =>
            Time.time - CreatedAt > (MaxAge >= 0f ? MaxAge : Mathf.Lerp(20f, 8f, Mathf.Clamp01(Urgency)));
    }
}