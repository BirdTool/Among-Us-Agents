using System.Collections.Generic;
using System.Linq;

namespace AMG.Models.LLM
{
    public class LlmReasoningStats
    {
        public int ReasoningCount { get; private set; }
        public int SuccessCount { get; private set; }
        public int FailureCount { get; private set; }

        public int TotalSteps { get; private set; }
        public double AverageSteps { get; private set; }
        public int MaxSteps { get; private set; }

        public int TotalFailovers { get; private set; }

        public long TotalInputTokens { get; private set; }
        public long TotalOutputTokens { get; private set; }

        public double AverageDurationMs { get; private set; }

        internal static LlmReasoningStats From(IEnumerable<LlmReasoningEntry> source)
        {
            var entries = source.ToList();

            return new LlmReasoningStats
            {
                ReasoningCount = entries.Count,
                SuccessCount = entries.Count(e => e.Success),
                FailureCount = entries.Count(e => !e.Success),

                TotalSteps = entries.Sum(e => e.Steps),
                AverageSteps = entries.Count == 0 ? 0 : entries.Average(e => e.Steps),
                MaxSteps = entries.Count == 0 ? 0 : entries.Max(e => e.Steps),

                TotalFailovers = entries.Sum(e => e.Failovers),

                TotalInputTokens = entries.Sum(e => e.InputTokens),
                TotalOutputTokens = entries.Sum(e => e.OutputTokens),

                AverageDurationMs = entries.Count == 0 ? 0 : entries.Average(e => e.DurationMs)
            };
        }
    }
}
