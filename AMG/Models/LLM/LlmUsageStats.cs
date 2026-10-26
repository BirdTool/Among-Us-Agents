using System.Collections.Generic;
using System.Linq;

namespace AMG.Models.LLM
{
    public class LlmUsageStats
    {
        public int RequestCount { get; private set; }
        public int AttemptCount { get; private set; }

        public int SuccessCount { get; private set; }
        public int FailureCount { get; private set; }

        public long TotalInputTokens { get; private set; }
        public long TotalOutputTokens { get; private set; }

        public int InputTokenSamples { get; private set; }
        public int OutputTokenSamples { get; private set; }

        public double TotalDurationMs { get; private set; }
        public double AverageDurationMs { get; private set; }

        internal static LlmUsageStats From(
            IEnumerable<LlmUsageEntry> source)
        {
            var entries = source.ToList();

            return new LlmUsageStats
            {
                RequestCount = entries
                    .Where(e => !string.IsNullOrWhiteSpace(e.RequestId))
                    .Select(e => e.RequestId)
                    .Distinct()
                    .Count(),

                AttemptCount = entries.Count,

                SuccessCount = entries.Count(e => e.Success),
                FailureCount = entries.Count(e => !e.Success),

                TotalInputTokens = entries.Sum(
                    e => (long)(e.InputTokens ?? 0)
                ),

                TotalOutputTokens = entries.Sum(
                    e => (long)(e.OutputTokens ?? 0)
                ),

                InputTokenSamples = entries.Count(
                    e => e.InputTokens.HasValue
                ),

                OutputTokenSamples = entries.Count(
                    e => e.OutputTokens.HasValue
                ),

                TotalDurationMs = entries.Sum(e => e.DurationMs),

                AverageDurationMs = entries.Count == 0
                    ? 0
                    : entries.Average(e => e.DurationMs)
            };
        }
    }
}