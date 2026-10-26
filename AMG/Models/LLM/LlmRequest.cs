using System.Collections.Generic;
using AMG.Enums;
using AMG.Interfaces;

namespace AMG.Models.LLM
{
    public sealed class LlmRequest<T> : ILLMRequest<T>
    {
        public string Prompt { get; init; } = string.Empty;
        public int MaxTokens { get; init; } = 1024;
        public bool Stream { get; init; }

        public string SystemPrompt { get; init; }
        public IReadOnlyList<LlmMessage> Messages { get; init; }
        public string Model { get; init; }
        public float? Temperature { get; init; }
        public bool JsonMode { get; init; }
        public LlmConsumer Consumer { get; init; } = LlmConsumer.ChatService;
    }
}
