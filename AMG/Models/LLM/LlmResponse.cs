using AMG.Interfaces;

namespace AMG.Models.LLM
{
    public sealed class LlmResponse<T> : ILLMResponse<string, T>
    {
        public float Time { get; init; }

        public int TokenCost { get; init; }

        public T Response { get; init; }
        public string Error { get; init; }

        public bool HasError => !string.IsNullOrEmpty(Error);
        public bool HasResponse => !HasError;

        public string RequestId { get; init; } = string.Empty;
        public string ErrorCode { get; init; } = string.Empty;
        public string Model { get; init; } = string.Empty;
        public string FinishReason { get; init; } = string.Empty;
        public int? InputTokens { get; init; }
        public int? OutputTokens { get; init; }
        public int Attempts { get; init; }

        public string RawText { get; init; } = string.Empty;
    }
}
