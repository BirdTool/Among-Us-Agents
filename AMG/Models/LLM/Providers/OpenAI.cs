using System;
using System.Collections.Generic;
using AMG.Enums;
using AMG.Interfaces;

namespace AMG.Models.LLM.Providers
{
    public class OpenAIClient(string key, uint index, string clientId) : ILLMClient
    {
        public uint Index { get; } = index;
        public string Key { get; } = key;
        public string ClientId { get; } = clientId;
        public LlmProvider Provider => LlmProvider.OpenAI;
        
        public ILLMResponse<string, T> Request<T>(ILLMRequest<T> llm)
        {
            throw new NotImplementedException();
        }
    }
}