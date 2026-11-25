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

        // Extra persona set in game with the .persona chat command (saved in the BepInEx config). It is added to the agents
        // that speak in chat and is read on every step, so a change applies at once.
        public static string CustomPersona { get; set; } = string.Empty;

        // The effort new meetings start with (the menu in UI/Tabs/AgentsTab changes it).
        public static LlmEffortEnum DefaultEffort { get; set; } = LlmEffortEnum.Medium;

        // The effort of the current meeting: DefaultEffort as it was when the meeting started.
        public LlmEffortEnum EffortLevel { get; private set; } = DefaultEffort;

        private sealed record EffortProfile(
            int MaxSteps,
            float DeadlineSeconds,
            float MinInterval = 2f,
            List<Func<LlmDefaultAgentClass>> Agents = null,
            int MaxRequestsPerMeeting = 40   // per agent: when it is spent the agent stops waking (a last call to vote still goes)
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
