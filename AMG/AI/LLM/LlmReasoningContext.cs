using System;
using System.Threading;

namespace AMG.AI.LLM
{
    // Lets the provider clients tag their usage entries with the reasoning that caused them,
    // without adding a parameter to every request.
    internal static class LlmReasoningContext
    {
        private static readonly AsyncLocal<LlmReasoning> Current = new();

        internal static string CurrentId => Current.Value?.Id ?? string.Empty;

        internal static IDisposable Enter(LlmReasoning reasoning)
        {
            var previous = Current.Value;
            Current.Value = reasoning;
            return new Scope(previous);
        }

        private sealed class Scope(LlmReasoning previous) : IDisposable
        {
            public void Dispose() => Current.Value = previous;
        }
    }
}
