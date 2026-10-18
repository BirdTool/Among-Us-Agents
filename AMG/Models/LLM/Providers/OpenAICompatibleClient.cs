using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AMG.Enums;

namespace AMG.Models.LLM.Providers
{
    // Chat Completions format ("/chat/completions"), shared by OpenAI and DeepSeek.
    public abstract class OpenAICompatibleClient(string key, uint index, string clientId) : LlmClientBase(key, index, clientId)
    {
        protected abstract string BaseUrl { get; }

        // "max_tokens" for DeepSeek; OpenAI's current models require "max_completion_tokens".
        protected virtual string MaxTokensField => "max_tokens";

        protected override HttpRequestMessage BuildHttpRequest(LlmCall call)
        {
            string system = call.SystemPrompt;

            // JSON mode is rejected (OpenAI) / unreliable (DeepSeek) unless the word "json" appears in the prompt.
            if (call.JsonMode && !MentionsJson(call))
                system = (string.IsNullOrWhiteSpace(system) ? string.Empty : system + "\n\n")
                         + "Respond with a single valid JSON object.";

            var messages = new List<object>();

            if (!string.IsNullOrWhiteSpace(system))
                messages.Add(new Dictionary<string, object> { ["role"] = "system", ["content"] = system });

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
                ["messages"] = messages,
                [MaxTokensField] = call.MaxTokens
            };

            if (call.Temperature.HasValue)
                body["temperature"] = call.Temperature.Value;

            if (call.JsonMode)
                body["response_format"] = new Dictionary<string, object> { ["type"] = "json_object" };

            var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl.TrimEnd('/') + "/chat/completions")
            {
                Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Key);
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
                input = GetInt(usage, "prompt_tokens");
                output = GetInt(usage, "completion_tokens");
            }

            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("choices", out var choices)
                || choices.ValueKind != JsonValueKind.Array
                || choices.GetArrayLength() == 0)
                throw new LlmProviderException("EmptyResponse", "Response has no choices.", retryable: true);

            var choice = choices[0];
            string finish = GetString(choice, "finish_reason");
            string text = null;

            if (choice.TryGetProperty("message", out var message))
                text = GetString(message, "content");

            if (string.IsNullOrEmpty(text))
            {
                if (finish == "content_filter")
                    throw new LlmProviderException("Blocked", "Response blocked by the content filter.");

                if (finish == "length")
                    throw new LlmProviderException("Truncated",
                        "MaxTokens reached before any text (reasoning models spend tokens thinking); raise MaxTokens.");

                // DeepSeek documents that JSON mode can occasionally return empty content.
                throw new LlmProviderException("EmptyResponse", "Model returned empty content.", retryable: true);
            }

            return new LlmProviderResult(text, input, output, finish);
        }

        private static bool MentionsJson(LlmCall call)
        {
            if (call.SystemPrompt != null && call.SystemPrompt.Contains("json", StringComparison.OrdinalIgnoreCase))
                return true;

            foreach (var message in call.Messages)
            {
                if (message.Content.Contains("json", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
