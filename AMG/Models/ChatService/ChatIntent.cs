using AMG.Enums.ChatServiceEnums;

namespace AMG.Models.ChatService
{
    public class ChatIntent
    {
        public ChatIntentEnum Type;
        public string PhraseId = ""; // PhraseGroup.Id, e.g. "Kill_Culprit"
        public ChatContextEnum? Context; // null = any context
        public float Urgency = 0.5f;
        public float Confidence = 0.5f;

        public byte? Target;
        public byte? Subject;
        public SystemTypes? Room;
        public EvidenceEnum? Evidence; // no longer used to look up phrases (PhraseId does that)
        public byte? Witness;
    }
}