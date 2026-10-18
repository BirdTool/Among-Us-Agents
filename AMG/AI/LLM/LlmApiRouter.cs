using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AMG.Enums;
using AMG.Interfaces;
using AMG.Models.LLM;
using AMG.Utilities;

namespace AMG.AI.LLM
{
    internal sealed class LlmRouteReport
    {
        internal List<string> KeysTried { get; } = [];
        internal int HttpAttempts { get; set; }
    }

    // Picks which API key answers a request.
    //  - Priority is the order of the keys in config.cfg (top first), whatever the provider.
    //  - A request starts at the top key and moves down while a key fails.
    //  - After FailuresBeforeCooldown consecutive failures a key is left alone for a while:
    //      with other keys available: MultiKeyCooldown (10 min);
    //      as the only key: 1 min after the 3rd error, +1 min for each further error.
    //  - One success clears the key's failure count.
    // Health lives in memory only: restarting the game clears it.
    public static class LlmApiRouter
    {
        public const int FailuresBeforeCooldown = 3;

        public static readonly TimeSpan MultiKeyCooldown = TimeSpan.FromMinutes(10);
        public static readonly TimeSpan SingleKeyCooldownStep = TimeSpan.FromMinutes(1);
        public static readonly TimeSpan SingleKeyMaxCooldown = TimeSpan.FromMinutes(30);

        internal static Func<DateTime> UtcNow { get; set; } = () => DateTime.UtcNow;

        private enum FailureKind
        {
            // The key (or its provider) is the problem: count it and try the next key.
            Key,

            // This provider can't take this request (wrong model name, format...): try the next key, don't punish this one.
            Skip,

            // The request itself is the problem or the user cancelled: another key would not help. Stop.
            Request
        }

        private sealed class KeyHealth
        {
            internal int ConsecutiveFailures;
            internal string LastErrorCode = string.Empty;
            internal DateTime? NextAttemptUtc;
        }

        private static readonly object Sync = new();
        private static readonly Dictionary<string, KeyHealth> Health = new(StringComparer.Ordinal);

        public static LlmReasoning BeginReasoning(LlmConsumer consumer, string label = null, int maxSteps = 0)
            => new(consumer, label, maxSteps);

        // One-shot request: it is a reasoning with a single step.
        public static async Task<ILLMResponse<string, T>> RequestAsync<T>(ILLMRequest<T> request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            using var reasoning = BeginReasoning(request.Consumer, "single");
            return await reasoning.RequestAsync(request, cancellationToken).ConfigureAwait(false);
        }

        // Blocking version: never call it from the Unity main thread.
        public static ILLMResponse<string, T> Request<T>(ILLMRequest<T> request)
            => Task.Run(() => RequestAsync(request)).GetAwaiter().GetResult();

        public static IReadOnlyList<LlmKeyStatus> GetKeyStatuses()
        {
            ILLMClient[] clients = [.. LlmProviderClient.Clients];
            DateTime now = UtcNow();
            var result = new List<LlmKeyStatus>(clients.Length);

            lock (Sync)
            {
                foreach (var client in clients)
                {
                    Health.TryGetValue(client.ClientId, out var health);

                    result.Add(new LlmKeyStatus
                    {
                        ClientId = client.ClientId,
                        Provider = client.Provider,
                        ConsecutiveFailures = health?.ConsecutiveFailures ?? 0,
                        LastErrorCode = health?.LastErrorCode ?? string.Empty,
                        NextAttemptUtc = health?.NextAttemptUtc,
                        IsAvailable = health?.NextAttemptUtc is not { } next || next <= now
                    });
                }
            }

            return result;
        }

        // For a "retry now" button, e.g. after the player fixes a key.
        public static void ResetKey(string clientId)
        {
            lock (Sync)
            {
                Health.Remove(clientId);
            }
        }

        public static void ResetAll()
        {
            lock (Sync)
            {
                Health.Clear();
            }
        }

        internal static async Task<(ILLMResponse<string, T> Response, LlmRouteReport Report)> RouteAsync<T>(
            ILLMRequest<T> request, LlmReasoning reasoning, CancellationToken cancellationToken)
        {
            var report = new LlmRouteReport();
            var watch = Stopwatch.StartNew();

            // Snapshot: the list only changes while keys are being loaded.
            ILLMClient[] clients = [.. LlmProviderClient.Clients];

            if (clients.Length == 0)
                return (Fail<T>("NoKeysConfigured", "No API key loaded. Add one to PrivateConfig/config.cfg.", watch.Elapsed.TotalSeconds, 0), report);

            bool singleKey = clients.Length == 1;
            var errors = new List<string>();
            TimeSpan? soonest = null;
            int coolingDown = 0;

            foreach (var client in clients)
            {
                if (cancellationToken.IsCancellationRequested)
                    return (Fail<T>("Cancelled", "Request cancelled.", watch.Elapsed.TotalSeconds, report.HttpAttempts), report);

                if (!TryAcquire(client.ClientId, out TimeSpan remaining))
                {
                    coolingDown++;

                    if (soonest == null || remaining < soonest)
                        soonest = remaining;

                    continue;
                }

                report.KeysTried.Add(client.ClientId);

                ILLMResponse<string, T> response;

                using (LlmReasoningContext.Enter(reasoning))
                    response = await client.RequestAsync(request, cancellationToken).ConfigureAwait(false);

                var detail = response as LlmResponse<T>;
                report.HttpAttempts += detail?.Attempts ?? 1;

                if (!response.HasError)
                {
                    RegisterSuccess(client.ClientId);

                    if (report.KeysTried.Count > 1)
                        LogManager.LogInfo($"[LLM] {client.ClientId} answered after {report.KeysTried.Count - 1} higher-priority key(s) failed.");

                    return (response, report);
                }

                string code = detail?.ErrorCode ?? "Unknown";
                FailureKind kind = Classify(code, response.Error);

                if (kind == FailureKind.Request)
                    return (response, report);

                if (kind == FailureKind.Key)
                    RegisterFailure(client.ClientId, code, singleKey);

                errors.Add($"{client.ClientId}: {code}");
            }

            if (report.KeysTried.Count == 0)
            {
                return (Fail<T>("NoKeyAvailable",
                    $"All {clients.Length} API key(s) are cooling down after repeated errors. Next attempt in {Format(soonest ?? TimeSpan.Zero)}.",
                    watch.Elapsed.TotalSeconds, 0), report);
            }

            string suffix = coolingDown > 0 ? $" ({coolingDown} more cooling down)" : string.Empty;

            return (Fail<T>("AllKeysFailed",
                "All API keys failed: " + string.Join("; ", errors) + suffix,
                watch.Elapsed.TotalSeconds, report.HttpAttempts), report);
        }

        private static FailureKind Classify(string code, string message)
        {
            switch (code)
            {
                case "InvalidRequest":
                case "ParseError":
                case "Blocked":
                case "Refused":
                case "Truncated":
                case "Cancelled":
                    return FailureKind.Request;

                case "HTTP_400":
                    // Google answers an invalid API key with 400, not 401.
                    if (message != null && message.Contains("api key", StringComparison.OrdinalIgnoreCase))
                        return FailureKind.Key;

                    return FailureKind.Skip;

                case "HTTP_404":
                case "HTTP_413":
                case "HTTP_422":
                    return FailureKind.Skip;

                default:
                    // 401/402/403/429/5xx, Timeout, NetworkError, BadResponse, EmptyResponse, InternalError...
                    return FailureKind.Key;
            }
        }

        private static bool TryAcquire(string clientId, out TimeSpan remaining)
        {
            remaining = TimeSpan.Zero;

            lock (Sync)
            {
                if (!Health.TryGetValue(clientId, out var health) || health.NextAttemptUtc is not { } next)
                    return true;

                DateTime now = UtcNow();

                if (next <= now)
                    return true;

                remaining = next - now;
                return false;
            }
        }

        private static void RegisterSuccess(string clientId)
        {
            lock (Sync)
            {
                if (Health.TryGetValue(clientId, out var health) && health.ConsecutiveFailures >= FailuresBeforeCooldown)
                    LogManager.LogInfo($"[LLM] {clientId} recovered.");

                Health.Remove(clientId);
            }
        }

        private static void RegisterFailure(string clientId, string code, bool singleKey)
        {
            lock (Sync)
            {
                if (!Health.TryGetValue(clientId, out var health))
                    Health[clientId] = health = new KeyHealth();

                health.ConsecutiveFailures++;
                health.LastErrorCode = code;

                if (health.ConsecutiveFailures < FailuresBeforeCooldown)
                    return;

                TimeSpan cooldown = singleKey
                    ? TimeSpan.FromTicks(Math.Min(
                        SingleKeyMaxCooldown.Ticks,
                        SingleKeyCooldownStep.Ticks * (health.ConsecutiveFailures - FailuresBeforeCooldown + 1)))
                    : MultiKeyCooldown;

                health.NextAttemptUtc = UtcNow() + cooldown;

                LogManager.LogWarning($"[LLM] {clientId} failed {health.ConsecutiveFailures} times in a row ({code}). Pausing it for {Format(cooldown)}.");
            }
        }

        internal static LlmResponse<T> Fail<T>(string code, string message, double seconds, int attempts)
        {
            return new LlmResponse<T>
            {
                Time = (float)seconds,
                Error = message,
                ErrorCode = code,
                RequestId = Guid.NewGuid().ToString("N"),
                Attempts = attempts
            };
        }

        private static string Format(TimeSpan span)
        {
            return span.TotalMinutes >= 1
                ? $"{(int)span.TotalMinutes}m{span.Seconds:00}s"
                : $"{Math.Max(0, (int)Math.Ceiling(span.TotalSeconds))}s";
        }
    }
}
