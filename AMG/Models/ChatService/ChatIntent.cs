using AMG.Enums.ChatServiceEnums;

namespace AMG.Models.ChatService
{
    public class ChatIntent
    {
        public ChatIntentEnum Type;
        public float Urgency = 0.5f;
        public float Confidence = 0.5f;

        public byte? Target;
        public byte? Subject;
        public SystemTypes? Room;
        public EvidenceEnum? Evidence;
        public byte? Witness;
    }
}