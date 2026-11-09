using System.Collections.Generic;
using AMG.Enums.LLMEnums;

namespace AMG.AI.Services.ChatService.Llm
{
    public sealed partial class LlmMeetingSession
    {
        private const int MaxCommandsPerStep = 5;

        public LlmEffortEnum EffortLevel = LlmEffortEnum.Medium;

        private sealed record EffortProfile(
            int MaxSteps, // Max steps per reasoning    
            float DeadlineSeconds, // Max deadline per reasoning in seconds
            int MaxTokens = 1024,     // output cap per request
            float MinInterval = 2f   // minimum pause between chains
        );

        private static readonly Dictionary<LlmEffortEnum, EffortProfile> Profiles = new()
        {
            [LlmEffortEnum.Low] = new(3, 20f, MinInterval: 8f),
            [LlmEffortEnum.Medium] = new(6, 40f, MaxTokens: 2048 ,MinInterval: 8f),
            [LlmEffortEnum.High] = new(12, 90f, MaxTokens: 4096, MinInterval: 8f)
        };
    }
}