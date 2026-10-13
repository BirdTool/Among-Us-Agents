using System.Collections.Generic;
using AMG.AI.Services.ChatService.History;
using AMG.Utilities;
using UnityEngine;

namespace AMG.AI.Services.ChatService
{
    // Every chat message of the meeting, in arrival order. ChatController.AddChat only enqueues here; Proccess() runs
    // once per frame (MeetingHud.Update) and turns each message into a Claim every agent can react to.
    public static class ReplicatedChat
    {
        private const int MaxPerFrame = 8;

        private static readonly Queue<(PlayerControl sourcePlayer, string chatText)> _chatQueue = [];

        public static void Enqueue(PlayerControl sourcePlayer, string chatText) =>
            _chatQueue.Enqueue((sourcePlayer, chatText));
        public static void Clear() => _chatQueue.Clear();
        public static void Dequeue() => _chatQueue.Dequeue();
        public static (PlayerControl sourcePlayer, string chatText) GetCurrentOnQueue() => _chatQueue.Peek();
        public static bool IsEmpty() => _chatQueue.Count == 0;

        public static void Proccess()
        {
            int budget = MaxPerFrame;

            while (_chatQueue.Count > 0 && budget-- > 0)
            {
                var (sender, text) = _chatQueue.Dequeue();
                if (sender == null || sender.Data == null || string.IsNullOrWhiteSpace(text)) continue;

                Handle(sender, text);
            }
        }

        private static void Handle(PlayerControl sender, string text)
        {
            var context = ClaimStore.Context;
            MessageParsedData parsed;

            // agents already know what they meant; only humans need to be parsed
            if (ChatOutbox.TryTake(sender.PlayerId, text, out var intent))
            {
                parsed = new MessageParsedData
                {
                    Content = text,
                    SenderId = sender.PlayerId,
                    SenderName = sender.Data.PlayerName,
                    TimeStamp = Time.time,
                    Intent = intent,
                    Context = context,
                    Confidence = 1f,
                    FromAgent = true,
                    Rationale = "structured (agent)",
                };
            }
            else
            {
                parsed = ChatParser.Parse(sender, text, context);
            }

            MessagesParsedHistory.AddMessage(parsed);

            if (parsed.Intent == null)
            {
                LogManager.LogDebug($"[Chat] not understood: {sender.Data.PlayerName}: \"{text}\" ({parsed.Rationale})");
                return;
            }

            var claim = ClaimStore.Register(parsed.SenderId, parsed.Intent, parsed.FromAgent, parsed.Confidence);

            LogManager.LogDebug($"[Chat] #{claim.Id} {sender.Data.PlayerName}: \"{text}\" => {parsed.Intent.Type}/{parsed.Intent.PhraseId} " +
                $"room={parsed.Intent.Room} target={parsed.Intent.Target} conf={parsed.Confidence:0.00} ({parsed.Rationale})");

            foreach (var brain in Utils.GetAllStructuredAgentBrain())
                brain.MeetingService?.OnClaim(claim);
        }
    }
}
