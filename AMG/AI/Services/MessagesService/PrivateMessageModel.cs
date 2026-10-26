using System;

namespace AMG.AI.Services.MessagesService
{
    public class PrivateMessageModel
    {
        public string Content { get; set; }
        public DateTime SentAt { get; set; }
        public PrivateMessageSenderEnum Sender { get; set; }
        public bool Read { get; set; }
    }

    public enum PrivateMessageSenderEnum
    {
        LLM, Developer
    }
}