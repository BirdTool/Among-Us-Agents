using System.Collections.Generic;
using AMG.AI.Services.ChatService.Llm.Agents;
using AMG.Enums.LLMEnums;
using AMG.Models.LLM;

namespace AMG.AI.Services.ChatService.Llm
{
    public sealed partial class LlmChatService
    {
        public const int MaxCommandsPerStep = 5;

        public LlmEffortEnum EffortLevel = LlmEffortEnum.Medium;

        private sealed record EffortProfile(
            int MaxSteps, // Max steps per reasoning    
            float DeadlineSeconds, // Max deadline per reasoning in seconds
            int MaxTokens = 1024,     // output cap per request (Only work if the model hasn't a specific one)
            float MinInterval = 2f,  // minimum pause between chains
            List<LlmDefaultAgentClass> Agents = null // agents to use in this reasoning loop
        )
        {
            public List<LlmDefaultAgentClass> Agents { get; init; } = Agents ?? [];
        }

        private static readonly Dictionary<LlmEffortEnum, EffortProfile> Profiles = new()
        {
            [LlmEffortEnum.Low] = new(3, 20f, MinInterval: 8f, Agents: [new AllWorkAgent(LlmEffortEnum.Medium)]),
            [LlmEffortEnum.Medium] = new(6, 40f, MaxTokens: 2048, MinInterval: 8f, Agents: [new CommunicatorAgent(LlmEffortEnum.Medium), new DetectiveAgent(LlmEffortEnum.Medium)]),
            [LlmEffortEnum.High] = new(12, 90f, MaxTokens: 4096, MinInterval: 8f, Agents: [new CommunicatorAgent(LlmEffortEnum.High), new DetectiveAgent(LlmEffortEnum.High)])
        };
    }
}