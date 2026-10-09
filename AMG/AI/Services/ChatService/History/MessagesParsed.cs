using System.Collections.Generic;
using AMG.AI.Control.AgentController;
using AMG.Enums.ChatServiceEnums;
using AMG.Models.ChatService;
using UnityEngine;

namespace AMG.AI.Services.ChatService.History
{
    public static class MessagesParsedHistory
    {
        public static readonly List<MessageParsedData> MessagesParsed = [];

        public static void AddMessage(string content, byte senderId, string senderName)
        {
            MessagesParsed.Add(new MessageParsedData
            {
                Content = content,
                SenderId = senderId,
                SenderName = senderName,
                TimeStamp = Time.time
            });
        }

        public static void AddMessage(MessageParsedData data)
        {
            MessagesParsed.Add(data);
        }

        public static void AddMessage(AgentController brain, string content)
        {
            AddMessage(content, brain.AgentId, brain.BaseName);
        }
    }

    public class MessageParsedData : MessageSentData
    {
        public ChatIntent Intent { get; set; }
        public ChatContextEnum Context { get; set; }
    }
}