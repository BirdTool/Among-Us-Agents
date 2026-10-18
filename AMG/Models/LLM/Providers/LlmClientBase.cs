using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using AMG.AI.LLM;
using AMG.Enums;
using AMG.Interfaces;
using AMG.Utilities;

namespace AMG.Models.LLM.Providers
{
    public abstract class LlmClientBase(string key, uint index, string clientId) : ILLMClient
    {
        internal static HttpClient Http { get; set; } = new() { Timeout = Timeout.InfiniteTimeSpan };

        internal static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            Converters = { new JsonStringEnumConverter() }
        };

        private const int MaxErrorTextLength = 300;
        private static readonly TimeSpan MaxRetryAfter = TimeSpan.FromSeconds(15);

        public uint Index { get; } = index;
        public string Key { get; } = key;
        public string ClientId { get; } = clientId;
        public abstract LlmProvider Provider { get; }

        protected abstract string DefaultModel { get; }
        protected virtual int MaxAttempts => 3;
        protected virtual TimeSpan RequestTimeout => TimeSpan.FromSeconds(60);

        protected abstract HttpRequestMessage BuildHttpRequest(LlmCall call);

        protected abstract LlmProviderResult ParseHttpResponse(string body);

        public ILLMResponse<string, T> Request<T>(ILLMRequest<T> llm)
        {
            return Task.Run(() => RequestAsync(llm)).GetAwaiter().GetResult();
        }

        public async Task<ILLMResponse<string, T>> RequestAsync<T>(ILLMRequest<T> llm, CancellationToken cancellationToken = default)
        {
            if (llm == null)
                throw new ArgumentNullException(nameof(llm));

            string requestId = Guid.NewGuid().ToString("N");
            var total = Stopwatch.StartNew();

            if (llm.Stream)
                LogManager.LogWarning($"[LLM] {ClientId}: Stream is not supported yet; waiting for the full response.");

            if (!TryBuildCall(llm, out LlmCall call, out string buildError))
            {
                LogManager.LogWarning($"[LLM] {ClientId}: invalid request: {buildError}");
                return Fail<T>(requestId, "InvalidRequest", buildError, call?.Model, total, 0);
            }

            LlmProviderResult result = null;
            LlmProviderException lastError = null;
            int attempts = 0;

            for (int attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                attempts = attempt;
                var attemptWatch = Stopwatch.StartNew();

                try
                {
                    result = await SendOnceAsync(call, cancellationToken).ConfigureAwait(false);
                    lastError = null;
                    Track(requestId, llm.Consumer, true, result.InputTokens, result.OutputTokens, attemptWatch, null);
                    break;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    Track(requestId, llm.Consumer, false, null, null, attemptWatch, "Cancelled");
                    return Fail<T>(requestId, "Cancelled", "Request cancelled.", call.Model, total, attempts);
                }
                catch (LlmProviderException ex)
                {
                    lastError = ex;
                    Track(requestId, llm.Consumer, false, null, null, attemptWatch, ex.Code);

                    if (!ex.Retryable || attempt >= MaxAttempts)
                        break;

                    TimeSpan delay = ex.RetryAfter ?? Backoff(attempt);
                    LogManager.LogWarning($"[LLM] {ClientId}: {ex.Code} (attempt {attempt}/{MaxAttempts}), retrying in {delay.TotalSeconds:0.0}s.");

                    try
                    {
                        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        return Fail<T>(requestId, "Cancelled", "Request cancelled.", call.Model, total, attempts);
                    }
                }
            }

            if (result == null)
            {
                string code = lastError?.Code ?? "Unknown";
                string message = lastError?.Message ?? "Request failed.";
                LogManager.LogWarning($"[LLM] {ClientId}: failed after {attempts} attempt(s): {code}: {message}");
                return Fail<T>(requestId, code, message, call.Model, total, attempts);
            }

            int? input = result.InputTokens;
            int? output = result.OutputTokens;

            if (!TryConvert(result.Text, out T value, out string convertError))
            {
                LogManager.LogWarning($"[LLM] {ClientId}: {convertError}");
                return new LlmResponse<T>
                {
                    Time = (float)total.Elapsed.TotalSeconds,
                    TokenCost = (input ?? 0) + (output ?? 0),
                    Error = convertError,
                    ErrorCode = "ParseError",
                    RequestId = requestId,
                    ClientId = ClientId,
                    Model = call.Model,
                    FinishReason = result.FinishReason,
                    InputTokens = input,
                    OutputTokens = output,
                    Attempts = attempts,
                    RawText = result.Text ?? string.Empty
                };
            }

            return new LlmResponse<T>
            {
                Time = (float)total.Elapsed.TotalSeconds,
                TokenCost = (input ?? 0) + (output ?? 0),
                Response = value,
                RequestId = requestId,
                    ClientId = ClientId,
                Model = call.Model,
                FinishReason = result.FinishReason,
                InputTokens = input,
                OutputTokens = output,
                Attempts = attempts,
                RawText = result.Text ?? string.Empty
            };
        }

        private async Task<LlmProviderResult> SendOnceAsync(LlmCall call, CancellationToken cancellationToken)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(RequestTimeout);

            try
            {
                using var request = BuildHttpRequest(call);
                using var response = await Http.SendAsync(request, timeout.Token).ConfigureAwait(false);
                string body = await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                    throw CreateHttpError(response, body);

                return ParseHttpResponse(body);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new LlmProviderException("Timeout", $"No response after {RequestTimeout.TotalSeconds:0}s.", retryable: true);
            }
            catch (HttpRequestException ex)
            {
                throw new LlmProviderException("NetworkError", Sanitize(ex.Message), retryable: true);
            }
            catch (JsonException ex)
            {
                throw new LlmProviderException("BadResponse", $"Response is not valid JSON: {Sanitize(ex.Message)}", retryable: true);
            }
            catch (Exception ex) when (ex is not LlmProviderException && ex is not OperationCanceledException)
            {
                throw new LlmProviderException("InternalError", $"{ex.GetType().Name}: {Sanitize(ex.Message)}");
            }
        }

        private LlmProviderException CreateHttpError(HttpResponseMessage response, string body)
        {
            int status = (int)response.StatusCode;
            string apiMessage = ExtractApiErrorMessage(body);
            string detail = Sanitize(string.IsNullOrWhiteSpace(apiMessage) ? body : apiMessage);

            bool retryable = status is 408 or 429 or 500 or 502 or 503 or 504 or 529;

            // Out of quota/credit will not fix itself in 2 seconds.
            if (status == 429 && body != null && body.IndexOf("insufficient_quota", StringComparison.OrdinalIgnoreCase) >= 0)
                retryable = false;

            TimeSpan? retryAfter = null;
            var header = response.Headers.RetryAfter;
            if (header != null)
            {
                if (header.Delta.HasValue)
                    retryAfter = header.Delta.Value;
                else if (header.Date.HasValue)
                    retryAfter = header.Date.Value - DateTimeOffset.UtcNow;
            }

            if (retryAfter.HasValue)
            {
                if (retryAfter.Value < TimeSpan.Zero)
                    retryAfter = TimeSpan.Zero;

                if (retryAfter.Value > MaxRetryAfter)
                    retryable = false;
            }

            return new LlmProviderException($"HTTP_{status}", $"HTTP {status}: {detail}", retryable, retryAfter);
        }

        private bool TryBuildCall<T>(ILLMRequest<T> llm, out LlmCall call, out string error)
        {
            call = null;
            error = null;

            var messages = new List<LlmMessage>();

            if (llm.Messages != null)
            {
                foreach (var message in llm.Messages)
                {
                    if (message != null && !string.IsNullOrWhiteSpace(message.Content))
                        messages.Add(message);
                }
            }

            if (!string.IsNullOrWhiteSpace(llm.Prompt))
                messages.Add(new LlmMessage(LlmRole.User, llm.Prompt));

            if (messages.Count == 0)
            {
                error = "Request has no prompt or messages.";
                return false;
            }

            if (messages[^1].Role != LlmRole.User)
            {
                error = "The last message must be a user message.";
                return false;
            }

            if (llm.MaxTokens <= 0)
            {
                error = "MaxTokens must be greater than zero.";
                return false;
            }

            call = new LlmCall
            {
                Model = string.IsNullOrWhiteSpace(llm.Model) ? DefaultModel : llm.Model.Trim(),
                SystemPrompt = llm.SystemPrompt,
                Messages = messages,
                MaxTokens = llm.MaxTokens,
                Temperature = llm.Temperature,
                JsonMode = llm.JsonMode
            };

            return true;
        }

        private static bool TryConvert<T>(string text, out T value, out string error)
        {
            error = null;

            if (typeof(T) == typeof(string))
            {
                value = (T)(object)(text ?? string.Empty);
                return true;
            }

            value = default;
            string json = ExtractJson(text);

            if (string.IsNullOrWhiteSpace(json))
            {
                error = "ParseError: the model returned no JSON.";
                return false;
            }

            try
            {
                value = JsonSerializer.Deserialize<T>(json, JsonOptions);

                if (value == null)
                {
                    error = "ParseError: the model returned JSON null.";
                    return false;
                }

                return true;
            }
            catch (JsonException ex)
            {
                error = $"ParseError: {ex.Message}";
                return false;
            }
        }

        internal static string ExtractJson(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            string s = text.Trim();

            if (s.StartsWith("```", StringComparison.Ordinal))
            {
                int firstLineEnd = s.IndexOf('\n');
                if (firstLineEnd >= 0)
                    s = s[(firstLineEnd + 1)..];

                int closing = s.LastIndexOf("```", StringComparison.Ordinal);
                if (closing >= 0)
                    s = s[..closing];

                s = s.Trim();
            }

            if (s.Length > 0 && s[0] != '{' && s[0] != '[')
            {
                int start = s.IndexOfAny(['{', '[']);
                int end = s.LastIndexOfAny(['}', ']']);

                if (start >= 0 && end > start)
                    s = s.Substring(start, end - start + 1);
            }

            return s;
        }

        private LlmResponse<T> Fail<T>(string requestId, string code, string message, string model, Stopwatch total, int attempts)
        {
            return new LlmResponse<T>
            {
                Time = (float)total.Elapsed.TotalSeconds,
                Error = message,
                ErrorCode = code,
                RequestId = requestId,
                    ClientId = ClientId,
                Model = model ?? string.Empty,
                Attempts = attempts
            };
        }

        private void Track(string requestId, LlmConsumer consumer, bool success, int? input, int? output, Stopwatch watch, string errorCode)
        {
            var entry = LlmUsageEntry.Create(
                requestId, ClientId, Provider, consumer, success,
                input, output, watch.Elapsed.TotalMilliseconds, errorCode,
                LlmReasoningContext.CurrentId);

            _ = LlmUsageTracker.RecordAsync(entry);
        }

        private static TimeSpan Backoff(int attempt)
        {
            double seconds = Math.Min(8, Math.Pow(2, attempt - 1));
            return TimeSpan.FromSeconds(seconds) + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 250));
        }

        protected string Sanitize(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            if (!string.IsNullOrEmpty(Key))
                text = text.Replace(Key, "***", StringComparison.Ordinal);

            return text.Length <= MaxErrorTextLength ? text : text[..MaxErrorTextLength] + "...";
        }

        protected static string ExtractApiErrorMessage(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return null;

            try
            {
                using var doc = JsonDocument.Parse(body);

                if (doc.RootElement.ValueKind == JsonValueKind.Object
                    && doc.RootElement.TryGetProperty("error", out var error))
                {
                    if (error.ValueKind == JsonValueKind.String)
                        return error.GetString();

                    if (error.ValueKind == JsonValueKind.Object
                        && error.TryGetProperty("message", out var message)
                        && message.ValueKind == JsonValueKind.String)
                        return message.GetString();
                }
            }
            catch (JsonException)
            {
            }

            return null;
        }

        protected static string GetString(JsonElement element, string name)
        {
            return element.ValueKind == JsonValueKind.Object
                && element.TryGetProperty(name, out var value)
                && value.ValueKind == JsonValueKind.String
                    ? value.GetString()
                    : null;
        }

        protected static int? GetInt(JsonElement element, string name)
        {
            return element.ValueKind == JsonValueKind.Object
                && element.TryGetProperty(name, out var value)
                && value.ValueKind == JsonValueKind.Number
                && value.TryGetInt32(out int number)
                    ? number
                    : null;
        }
    }
}
