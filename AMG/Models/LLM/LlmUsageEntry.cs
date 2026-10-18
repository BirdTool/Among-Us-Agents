using System;
using AMG.Enums;

namespace AMG.Models.LLM
{
    public class LlmUsageEntry
    {
        public string RequestId { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;

        public LlmProvider Provider { get; set; }
        public LlmConsumer Consumer { get; set; }

        public DateTime TimestampUtc { get; set; }

        public int? InputTokens { get; set; }
        public int? OutputTokens { get; set; }

        public double DurationMs { get; set; }

        public bool Success { get; set; }

        public string ErrorCode { get; set; } = string.Empty;

        public string ReasoningId { get; set; } = string.Empty;

        public static LlmUsageEntry Create(
            string requestId,
            string clientId,
            LlmProvider provider,
            LlmConsumer consumer,
            bool success,
            int? inputTokens = null,
            int? outputTokens = null,
            double durationMs = 0,
            string errorCode = null,
            string reasoningId = null)
        {
            durationMs = Math.Max(0, durationMs);

            return new LlmUsageEntry
            {
                RequestId = requestId,
                ClientId = clientId,
                Provider = provider,
                Consumer = consumer,

                TimestampUtc = DateTime.UtcNow.AddMilliseconds(
                    -durationMs
                ),

                InputTokens = inputTokens,
                OutputTokens = outputTokens,
                DurationMs = durationMs,

                Success = success,
                ErrorCode = errorCode ?? string.Empty,
                ReasoningId = reasoningId ?? string.Empty
            };
        }
    }
}