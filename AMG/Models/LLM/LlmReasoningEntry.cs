using System;
using System.Collections.Generic;
using AMG.Enums;

namespace AMG.Models.LLM
{
    public class LlmReasoningEntry
    {
        public string ReasoningId { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public LlmConsumer Consumer { get; set; }

        public DateTime StartedUtc { get; set; }

        // Wall clock, includes the time spent running commands between steps.
        public double DurationMs { get; set; }

        // Sum of the time actually spent waiting for the LLM.
        public double RequestTimeMs { get; set; }

        // Routed requests (one per LLM turn) and HTTP attempts across all keys.
        public int Steps { get; set; }
        public int HttpAttempts { get; set; }

        // Times a request had to move to a lower-priority key.
        public int Failovers { get; set; }

        public long InputTokens { get; set; }
        public long OutputTokens { get; set; }

        public List<string> KeysUsed { get; set; } = [];

        public bool Success { get; set; }
        public string ErrorCode { get; set; } = string.Empty;
    }
}
