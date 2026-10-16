using System;
using System.Collections.Generic;
using AMG.Enums;
using AMG.Interfaces;

namespace AMG.Models.LLM.Providers
{
    public class DeepSeekClient(string key, uint index) : ILLMClient
    {
        public uint Index { get; } = index;
        public string Key { get; } = key;
        public LlmProvider Provider => LlmProvider.DeepSeek;
        
        public ILLMResponse<string, T> Request<T>(ILLMRequest<T> llm)
        {
            throw new NotImplementedException();
        }
    }
}