using System;

namespace AMG.Models.LLM
{
    public sealed class LlmProviderException(string code, string message, bool retryable = false, TimeSpan? retryAfter = null) : Exception(message)
    {
        public string Code { get; } = code;
        public bool Retryable { get; } = retryable;
        public TimeSpan? RetryAfter { get; } = retryAfter;
    }

    public sealed class LlmCall
    {
        public string Model { get; init; }
        public string SystemPrompt { get; init; }
        public System.Collections.Generic.IReadOnlyList<LlmMessage> Messages { get; init; }
        public int MaxTokens { get; init; }
        public float? Temperature { get; init; }
        public bool JsonMode { get; init; }
    }

    public sealed class LlmProviderResult(string text, int? inputTokens, int? outputTokens, string finishReason)
    {
        public string Text { get; } = text;
        public int? InputTokens { get; } = inputTokens;
        public int? OutputTokens { get; } = outputTokens;
        public string FinishReason { get; } = finishReason ?? string.Empty;
    }
}
