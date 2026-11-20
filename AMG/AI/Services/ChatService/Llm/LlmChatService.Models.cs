using System;
using System.Collections.Generic;
using AMG.AI.Services.ChatService.Llm.Agents;
using AMG.Enums.LLMEnums;
using AMG.Models.LLM;

namespace AMG.AI.Services.ChatService.Llm
{
    public sealed partial class LlmChatService
    {
        public const int MaxCommandsPerStep = 5;

        // Effort the meetings run with. Changes take effect at the next meeting.
        public LlmEffortEnum EffortLevel = LlmEffortEnum.Medium;

        private sealed record EffortProfile(
            int MaxSteps,
            float DeadlineSeconds,
            float MinInterval = 2f,
            List<Func<LlmDefaultAgentClass>> Agents = null
        )
        {
            public List<Func<LlmDefaultAgentClass>> Agents { get; init; } = Agents ?? [];
        }

        private static readonly Dictionary<LlmEffortEnum, EffortProfile> Profiles = new()
        {
            [LlmEffortEnum.Low] = new(8, 20f, MinInterval: 8f, Agents: [() => new AllWorkAgent(LlmEffortEnum.Medium)]),
            [LlmEffortEnum.Medium] = new(6, 40f, MinInterval: 8f, Agents: [() => new CommunicatorAgent(LlmEffortEnum.Medium), () => new DetectiveAgent(LlmEffortEnum.Medium)]),
            [LlmEffortEnum.High] = new(12, 90f, MinInterval: 8f, Agents: [() => new CommunicatorAgent(LlmEffortEnum.High), () => new DetectiveAgent(LlmEffortEnum.High)])
        };
    }
}
