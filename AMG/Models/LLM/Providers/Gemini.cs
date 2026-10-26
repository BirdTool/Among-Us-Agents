using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using AMG.Enums;

namespace AMG.Models.LLM.Providers
{
    public class GeminiClient(string key, uint index, string clientId) : LlmClientBase(key, index, clientId)
    {
        private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/";

        public override LlmProvider Provider => LlmProvider.Gemini;

        // Note: on thinking models, MaxTokens (maxOutputTokens) also covers the thinking tokens.
        protected override string DefaultModel => "gemini-3.5-flash-lite";

        protected override HttpRequestMessage BuildHttpRequest(LlmCall call)
        {
            var contents = new List<object>();

            foreach (var message in call.Messages)
            {
                contents.Add(new Dictionary<string, object>
                {
                    ["role"] = message.Role == LlmRole.Assistant ? "model" : "user",
                    ["parts"] = new List<object> { new Dictionary<string, object> { ["text"] = message.Content } }
                });
            }

            var generationConfig = new Dictionary<string, object> { ["maxOutputTokens"] = call.MaxTokens };

            if (call.Temperature.HasValue)
                generationConfig["temperature"] = call.Temperature.Value;

            if (call.JsonMode)
                generationConfig["responseMimeType"] = "application/json";

            var body = new Dictionary<string, object>
            {
                ["contents"] = contents,
                ["generationConfig"] = generationConfig
            };

            if (!string.IsNullOrWhiteSpace(call.SystemPrompt))
            {
                body["systemInstruction"] = new Dictionary<string, object>
                {
                    ["parts"] = new List<object> { new Dictionary<string, object> { ["text"] = call.SystemPrompt } }
                };
            }

            string url = BaseUrl + Uri.EscapeDataString(call.Model) + ":generateContent";

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
            };

            // Header, not "?key=": keeps the key out of URLs (and out of exception messages/logs).
            request.Headers.Add("x-goog-api-key", Key);
            return request;
        }

        protected override LlmProviderResult ParseHttpResponse(string body)
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            int? input = null;
            int? output = null;

            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("usageMetadata", out var usage)
                && usage.ValueKind == JsonValueKind.Object)
            {
                input = GetInt(usage, "promptTokenCount");

                int? candidates = GetInt(usage, "candidatesTokenCount");
                int? thoughts = GetInt(usage, "thoughtsTokenCount"); // billed as output

                if (candidates.HasValue || thoughts.HasValue)
                    output = (candidates ?? 0) + (thoughts ?? 0);
            }

            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("promptFeedback", out var feedback))
            {
                string blockReason = GetString(feedback, "blockReason");

                if (!string.IsNullOrEmpty(blockReason))
                    throw new LlmProviderException("Blocked", $"Prompt blocked by Gemini: {blockReason}.");
            }

            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("candidates", out var candidatesArray)
                || candidatesArray.ValueKind != JsonValueKind.Array
                || candidatesArray.GetArrayLength() == 0)
                throw new LlmProviderException("EmptyResponse", "Response has no candidates.", retryable: true);

            var candidate = candidatesArray[0];
            string finish = GetString(candidate, "finishReason");
            var text = new StringBuilder();

            if (candidate.TryGetProperty("content", out var content)
                && content.ValueKind == JsonValueKind.Object
                && content.TryGetProperty("parts", out var parts)
                && parts.ValueKind == JsonValueKind.Array)
            {
                foreach (var part in parts.EnumerateArray())
                {
                    // Skip thought summaries; only the answer counts.
                    if (part.TryGetProperty("thought", out var thought) && thought.ValueKind == JsonValueKind.True)
                        continue;

                    text.Append(GetString(part, "text"));
                }
            }

            if (text.Length == 0)
            {
                if (finish == "MAX_TOKENS")
                    throw new LlmProviderException("Truncated",
                        "MaxTokens reached before any text (thinking tokens count too); raise MaxTokens.");

                if (finish is "SAFETY" or "RECITATION" or "BLOCKLIST" or "PROHIBITED_CONTENT" or "SPII")
                    throw new LlmProviderException("Blocked", $"Response blocked by Gemini: {finish}.");

                throw new LlmProviderException("EmptyResponse", $"Response has no text (finishReason: {finish ?? "none"}).", retryable: true);
            }

            return new LlmProviderResult(text.ToString(), input, output, finish);
        }
    }
}
