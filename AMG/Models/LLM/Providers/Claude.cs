using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using AMG.Enums;

namespace AMG.Models.LLM.Providers
{
    public class ClaudeClient(string key, uint index, string clientId) : LlmClientBase(key, index, clientId)
    {
        private const string Endpoint = "https://api.anthropic.com/v1/messages";
        private const string ApiVersion = "2023-06-01";

        public override LlmProvider Provider => LlmProvider.Claude;

        protected override string DefaultModel => "claude-haiku-5-5";

        protected override HttpRequestMessage BuildHttpRequest(LlmCall call)
        {
            string system = call.SystemPrompt;

            // The Messages API has no JSON mode: ask for it in the system prompt (fences are stripped later anyway).
            if (call.JsonMode)
                system = (string.IsNullOrWhiteSpace(system) ? string.Empty : system + "\n\n")
                         + "Respond only with a single valid JSON value. No prose and no markdown code fences.";

            var messages = new List<object>();

            foreach (var message in call.Messages)
            {
                messages.Add(new Dictionary<string, object>
                {
                    ["role"] = message.Role == LlmRole.Assistant ? "assistant" : "user",
                    ["content"] = message.Content
                });
            }

            var body = new Dictionary<string, object>
            {
                ["model"] = call.Model,
                ["max_tokens"] = call.MaxTokens, // required by this API
                ["messages"] = messages
            };

            if (!string.IsNullOrWhiteSpace(system))
                body["system"] = system;

            if (call.Temperature.HasValue)
                body["temperature"] = call.Temperature.Value;

            var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
            };

            request.Headers.Add("x-api-key", Key);
            request.Headers.Add("anthropic-version", ApiVersion);
            return request;
        }

        protected override LlmProviderResult ParseHttpResponse(string body)
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            int? input = null;
            int? output = null;

            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("usage", out var usage)
                && usage.ValueKind == JsonValueKind.Object)
            {
                input = GetInt(usage, "input_tokens");
                output = GetInt(usage, "output_tokens");
            }

            string stopReason = GetString(root, "stop_reason");
            var text = new StringBuilder();

            if (root.ValueKind == JsonValueKind.Object
                && root.TryGetProperty("content", out var content)
                && content.ValueKind == JsonValueKind.Array)
            {
                foreach (var block in content.EnumerateArray())
                {
                    if (GetString(block, "type") == "text")
                        text.Append(GetString(block, "text"));
                }
            }

            if (text.Length == 0)
            {
                if (stopReason == "refusal")
                    throw new LlmProviderException("Refused", "The model refused to answer.");

                throw new LlmProviderException("EmptyResponse", $"Response has no text (stop_reason: {stopReason ?? "none"}).");
            }

            return new LlmProviderResult(text.ToString(), input, output, stopReason);
        }
    }
}
