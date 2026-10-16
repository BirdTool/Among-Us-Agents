using System;
using System.Collections.Generic;
using AMG.Enums;
using AMG.Interfaces;
using AMG.Models.LLM.Providers;
using AMG.Utilities;

namespace AMG.AI.LLM
{
    internal static class LlmProviderClient
    {
        internal static readonly List<ILLMClient> Clients = [];

        internal static void AddClient(ILLMClient client)
        {
            foreach (var existing in Clients)
            {
                if (existing.Provider == client.Provider && string.Equals(existing.Key, client.Key, StringComparison.Ordinal))
                {
                    LogManager.LogWarning($"[LLM] Duplicate key ignored for {client.Provider}.");
                    return;
                }
            }

            Clients.Add(client);
        }

        internal static ILLMClient CreateClient(LlmProvider provider, string key, uint index)
        {
            return provider switch
            {
                LlmProvider.OpenAI => new OpenAIClient(key, index),
                LlmProvider.Gemini => new GeminiClient(key, index),
                LlmProvider.Claude => new ClaudeClient(key, index),
                LlmProvider.DeepSeek => new DeepSeekClient(key, index),
                _ => null
            };
        }
    }
}