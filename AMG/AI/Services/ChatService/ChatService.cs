using System.Collections.Generic;
using System.Linq;
using AMG.Interfaces;
using AMG.Models.ChatService;

namespace AMG.AI.Services.ChatService
{
    public class ChatService(IChatTransport chatTransport)
    {
        private readonly Queue<ChatIntent> _intentQueue = [];
        private readonly IChatTransport _chatTransport = chatTransport;

        public void Enqueue(ChatIntent i)
        {
            _intentQueue.Enqueue(i);
        }

        public void Clear() => _intentQueue.Clear();

        public bool HasPending => _intentQueue.Count > 0;

        public void Tick()
        {
            if (!_intentQueue.Any()) return;
        }
    }
}