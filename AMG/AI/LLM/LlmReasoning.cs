using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AMG.Enums;
using AMG.Interfaces;
using AMG.Models.LLM;

namespace AMG.AI.LLM
{
    // One reasoning = a chain of LLM turns (ask, run a command, ask again... final answer).
    // Complete() (or Dispose) writes ONE summary entry to the LlmReasoningTracker.
    public sealed class LlmReasoning : IDisposable
    {
        private readonly object _lock = new();
        private readonly Stopwatch _watch = Stopwatch.StartNew();
        private readonly DateTime _startedUtc = DateTime.UtcNow;
        private readonly List<string> _keysUsed = [];

        private int _steps;
        private int _httpAttempts;
        private int _failovers;
        private long _inputTokens;
        private long _outputTokens;
        private double _requestMs;
        private bool _lastStepOk;
        private string _lastErrorCode = string.Empty;
        private bool _completed;

        public string Id { get; } = Guid.NewGuid().ToString("N");
        public LlmConsumer Consumer { get; }
        public string Label { get; }

        // 0 = no limit. Guards against an agentic loop that never ends (and keeps costing money).
        public int MaxSteps { get; }

        internal LlmReasoning(LlmConsumer consumer, string label, int maxSteps)
        {
            Consumer = consumer;
            Label = label ?? string.Empty;
            MaxSteps = Math.Max(0, maxSteps);
        }

        public int Steps
        {
            get { lock (_lock) return _steps; }
        }

        public bool IsCompleted
        {
            get { lock (_lock) return _completed; }
        }

        public async Task<ILLMResponse<string, T>> RequestAsync<T>(ILLMRequest<T> request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            lock (_lock)
            {
                if (_completed)
                    return LlmApiRouter.Fail<T>("ReasoningCompleted", "This reasoning is already completed.", 0, 0);

                if (MaxSteps > 0 && _steps >= MaxSteps)
                    return LlmApiRouter.Fail<T>("MaxStepsReached", $"Reasoning reached its limit of {MaxSteps} steps.", 0, 0);
            }

            var (response, report) = await LlmApiRouter.RouteAsync(request, this, cancellationToken).ConfigureAwait(false);

            RecordStep(response, report);
            return response;
        }

        private void RecordStep<T>(ILLMResponse<string, T> response, LlmRouteReport report)
        {
            var detail = response as LlmResponse<T>;

            lock (_lock)
            {
                _steps++;
                _httpAttempts += report.HttpAttempts;
                _requestMs += response.Time * 1000.0;

                if (report.KeysTried.Count > 1)
                    _failovers += report.KeysTried.Count - 1;

                foreach (string id in report.KeysTried)
                {
                    if (!_keysUsed.Contains(id))
                        _keysUsed.Add(id);
                }

                _inputTokens += detail?.InputTokens ?? 0;
                _outputTokens += detail?.OutputTokens ?? 0;

                _lastStepOk = !response.HasError;
                _lastErrorCode = detail?.ErrorCode ?? string.Empty;
            }
        }

        // success/errorCode are optional: by default the outcome is the outcome of the last step.
        public void Complete(bool? success = null, string errorCode = null)
        {
            LlmReasoningEntry entry;

            lock (_lock)
            {
                if (_completed)
                    return;

                _completed = true;

                // Nothing was asked: nothing to record.
                if (_steps == 0)
                    return;

                bool ok = success ?? _lastStepOk;

                entry = new LlmReasoningEntry
                {
                    ReasoningId = Id,
                    Label = Label,
                    Consumer = Consumer,
                    StartedUtc = _startedUtc,
                    DurationMs = _watch.Elapsed.TotalMilliseconds,
                    RequestTimeMs = _requestMs,
                    Steps = _steps,
                    HttpAttempts = _httpAttempts,
                    Failovers = _failovers,
                    InputTokens = _inputTokens,
                    OutputTokens = _outputTokens,
                    KeysUsed = [.. _keysUsed],
                    Success = ok,
                    ErrorCode = ok ? string.Empty : errorCode ?? _lastErrorCode
                };
            }

            _ = LlmReasoningTracker.RecordAsync(entry);
        }

        public void Dispose() => Complete();
    }
}
