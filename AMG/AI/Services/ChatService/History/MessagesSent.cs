using System.Collections.Generic;
using AMG.AI.Control.AgentController;
using UnityEngine;

namespace AMG.AI.Services.ChatService.History
{
    public static class MessagesSentHistory
    {
        public static readonly List<MessageSentData> MessagesSent = [];

        public static void AddMessage(string content, byte senderId, string senderName)
        {
            MessagesSent.Add(new MessageSentData
            {
                Content = content,
                SenderId = senderId,
                SenderName = senderName,
                TimeStamp = Time.time
            });
        }

        public static void AddMessage(MessageSentData data)
        {
            MessagesSent.Add(data);
        }

        public static void AddMessage(AgentController brain, string content)
        {
            AddMessage(content, brain.AgentId, brain.BaseName);
        }
    }

    public class MessageSentData
    {
        public string Content { get; set; }
        public float TimeStamp { get; set; }
        public byte SenderId { get; set; }
        public string SenderName { get; set; }
    }
}