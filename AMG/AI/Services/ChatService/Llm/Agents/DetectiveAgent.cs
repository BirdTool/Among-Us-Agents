using AMG.Enums.LLMEnums;
using AMG.Models.LLM;

namespace AMG.AI.Services.ChatService.Llm.Agents
{
    public class DetectiveAgent(LlmEffortEnum effort) : LlmDefaultAgentClass(effort)
    {
        public override int MaxOutputTokens { get; } = effort switch
        {
            LlmEffortEnum.Low => 2048,
            LlmEffortEnum.Medium => 4096,
            LlmEffortEnum.High => 8192,
            _ => 4096
        };

        public override bool SeesChat => false;

        // The detective is the one investigating, so it rests only briefly between turns (it wakes on chat and on team messages).
        public override float RestSeconds => 2f;

        public override string Description => "Investigates who the impostor is and is the main voter. Tells you what to get across in chat (an intent and facts, never the words).";

        // Nothing to investigate at second 0: it wakes with the first chat message or team message (or the last call to vote).
        public override bool WakesOnMeetingStart => false;

        protected override string StartPrompt => $"""
            You are the DetectiveAgent: you find out who the impostor is in an Among Us meeting, and you decide the vote.

            {AgentPrompts.Investigation}

            {AgentPrompts.DetectiveTeamwork}

            {AgentPrompts.Voting}
            """;

        protected override string Persona => string.Empty;
    }
}
