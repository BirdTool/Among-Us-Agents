using System;
using AMG.Enums;

namespace AMG.Models.LLM
{
    public sealed class LlmKeyStatus
    {
        public string ClientId { get; init; } = string.Empty;
        public LlmProvider Provider { get; init; }
        public int ConsecutiveFailures { get; init; }
        public string LastErrorCode { get; init; } = string.Empty;
        public DateTime? NextAttemptUtc { get; init; }
        public bool IsAvailable { get; init; }
    }
}
