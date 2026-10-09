using System.Collections.Generic;

namespace AMG.AI.Services.ChatService
{
    public static class ReplicatedChat
    {
        private static readonly Queue<(PlayerControl sourcePlayer, string chatText)> _chatQueue = [];

        public static void Enqueue(PlayerControl sourcePlayer, string chatText) =>
            _chatQueue.Enqueue((sourcePlayer, chatText));
        public static void Clear() => _chatQueue.Clear();
        public static void Dequeue() => _chatQueue.Dequeue();
        public static (PlayerControl sourcePlayer, string chatText) GetCurrentOnQueue() => _chatQueue.Peek();
        public static bool IsEmpty() => _chatQueue.Count == 0;

        public static void Proccess()
        {
            if (IsEmpty()) return;
            
            
        }
    }
}