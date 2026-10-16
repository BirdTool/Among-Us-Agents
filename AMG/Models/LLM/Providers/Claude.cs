using System;
using System.Collections.Generic;
using AMG.Enums;
using AMG.Interfaces;

namespace AMG.Models.LLM.Providers
{
    public class ClaudeClient(string key, uint index) : ILLMClient
    {
        public uint Index { get; } = index;
        public string Key { get; } = key;
        public LlmProvider Provider => LlmProvider.Claude;
        
        public ILLMResponse<string, T> Request<T>(ILLMRequest<T> llm)
        {
            throw new NotImplementedException();
        }
    }
}